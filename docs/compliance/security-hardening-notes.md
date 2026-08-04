# Security Hardening Notes (P13)

This is an internal engineering security review — real findings, real fixes, verified against
the running system. **It is not a substitute for a real third-party penetration test.** A
licensed pentest firm tests things this review structurally cannot: real exploit chains, timing
attacks, business-logic abuse across a full authenticated session, and infrastructure the review
has no access to (this pass ran entirely against a local dev environment — no staging/production
deployment exists yet to test against).

## What was reviewed and fixed this pass

### 1. Rate limiting on authentication endpoints
**Before**: no rate limiting existed anywhere in the API. `LawyerLoginCommand`/`AdminLoginCommand`
had no lockout, `NoOpRecaptchaVerifier` is a dev-only no-op (flagged since P1), and nothing else
stood between an attacker and unlimited password-guessing attempts.
**Fixed**: `Microsoft.AspNetCore.RateLimiting` (built into .NET, no new package), a fixed-window
policy (10 requests/minute, partitioned per client IP), applied via `[EnableRateLimiting("auth")]`
to every client OTP, lawyer login/register, admin login, and token-refresh endpoint.
**Verified live**: 15 rapid requests to `/api/v1/auth/admin/login` → requests 1–10 returned 401
(wrong password), 11–15 returned 429; after the 1-minute window elapsed, a correct-password
request succeeded (200) — both the block and the recovery are real, not asserted.

### 2. Security headers
**Before**: no security headers were set at all.
**Fixed**: `SecurityHeadersMiddleware` sets `X-Content-Type-Options: nosniff`,
`X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`,
`Permissions-Policy` (camera/mic scoped to same-origin, geolocation denied), and a
`Content-Security-Policy` (relaxed for Swagger's inline scripts in Development only; a real
`default-src 'self'` policy everywhere else).
**Verified live**: `curl -si` against a real running endpoint shows all five headers present.

### 3. Dependency-aware health checks
**Before**: `/health` returned a static "healthy" string regardless of whether MySQL, Redis, or
RabbitMQ were actually reachable — an orchestrator or load balancer would have no way to know a
degraded instance should stop receiving traffic.
**Fixed**: `/health/live` (process-up only) and `/health/ready` (real `CanConnectAsync`/`PingAsync`/
connection-open checks against all three dependencies), returning structured JSON and a 503 when
any "ready" check fails.
**Verified live, genuinely — not just asserted**: stopped the real `rabbitmq` container mid-session
→ `/health/ready` correctly returned `503` with `rabbitmq: Unhealthy` while `database`/`redis`
stayed independently `Healthy` → restarted the container → recovered to `Healthy` on its own once
the broker finished booting, with zero code changes needed to observe either transition.

### 4. A real, severe frontend bug found and fixed during this pass: silent session death
**Found by**: live-testing the apps after a real, unplanned multi-hour gap in this session (the
same gap that also took down the `mysql`/`redis` Docker containers — see the runbooks). Every one
of the three React apps (`web-client`, `web-lawyer`, `web-admin`) attaches an axios *request*
interceptor to add the bearer token, but **none had a response interceptor** — so once a 15-minute
access token expired, `RequireAuth`'s "does a token exist in storage" check kept passing (the
stale token was still *present*, just server-rejected), every subsequent API call 401'd, and the
user was left staring at a permanently blank page with zero indication anything was wrong and no
path to recover short of manually clearing browser storage.
**Fixed**: a response interceptor in all three apps' `authContext.tsx` that clears stored tokens
and redirects to `/login` on a 401 — guarded to only fire when a token actually existed (so it
doesn't also trigger on an ordinary wrong-password 401 from the login page itself, which never had
a token to begin with).
**Verified live in all three apps**: navigated to an authenticated route with a real expired
token in each of `web-client`, `web-lawyer`, and `web-admin` → each one correctly redirected to
its own `/login` instead of rendering blank.

### 5. A real, systemic monorepo build gap found and fixed: shared-component styles silently dropped
**Found while verifying** a focus-visible fix on the shared `Input` component (see item 6): the
new CSS classes never appeared in any app's built stylesheet. Root cause: Tailwind v4's automatic
content-detection excludes `node_modules` by default, and pnpm workspace packages (`@law-portal/ui`)
are symlinked there — so any utility class used *only* inside `packages/ui`'s own source, and not
also verbatim somewhere in a consuming app's own source, was silently never generated, in every
app, for this project's entire history. It only surfaced now because this was the first time a
shared-component-only utility class had no accidental duplicate elsewhere.
**Fixed**: an explicit `@source "../../../packages/ui/src"` directive in each of the three apps'
global CSS entry points.
**Verified**: grepped the production CSS output before/after — `focus-within:*` utilities were
entirely absent before, present and correctly generated after, confirmed with a real screenshot
showing the rendered focus ring.

### 6. A real accessibility bug found and fixed: no visible keyboard focus indicator, anywhere
**Found by**: a manual accessibility audit (see below — the intended tool, Lighthouse via the
chrome-devtools MCP, was unavailable in this environment due to a stale browser profile lock from
an unrelated earlier session; substituted a real, rigorous manual pass instead). The shared `Input`
primitive (used in essentially every form across all three apps — login, OTP, registration, chat,
request wizards, settings) rendered its actual `<input>` element borderless and transparent inside
a decorated wrapper, with `focus:outline-none` on the input and **no replacement focus style
anywhere** — meaning every text field on the entire platform gave zero visible indication of
keyboard focus (WCAG 2.4.7, Level AA). Six additional standalone textareas (chat composers,
decline-reason fields, bio fields, a bidding description field) had the identical pattern.
**Fixed**: `focus-within:border-seal focus-within:ring-2 focus-within:ring-seal/30` on the `Input`
wrapper (the visible box, since the real input is borderless by design), and the equivalent
`focus:` variant added to each standalone textarea.
**Verified live**: a real screenshot of the lawyer registration form with a field focused shows a
clear green ring around the active field.

### 7. Missing page-level heading found and fixed
**Found by**: the same manual audit — `web-client`'s `PortalHome` page was the only one of 31
pages across all three apps with zero `<h1>` elements (five sections all started at `<h2>`,
nothing above them). Screen-reader navigation-by-heading depends on exactly one `<h1>` per page.
**Fixed**: a visually-hidden (`sr-only`) `<h1>` added to `PortalHome`, matching its own nav label.

## What genuinely still needs a human, not more code

1. **A real third-party penetration test** — the items above are real fixes to real findings,
   but this review had no access to a staging/production deployment, no adversarial red-team
   perspective, and no time-boxed exploit attempts. Commission a licensed pentest firm before
   public launch, per the plan's own P13 line item.
2. **A real reCAPTCHA account** — `NoOpRecaptchaVerifier` is still a no-op; rate limiting is
   defense in depth, not a replacement for it.
3. **The AutoMapper `GHSA-rvv3-g6hj-g44x` advisory**, flagged since P0's own progress log,
   still unresolved — needs either a version fix once upstream ships one, or a licensing/
   alternative-library evaluation before launch.
4. **Full Lighthouse audits** (performance, best-practices, and a second accessibility pass with
   axe-core's broader ruleset) once the chrome-devtools MCP's browser-profile conflict is
   resolved or a clean environment is available — this pass's manual audit was real but narrower
   in scope than an automated tool would cover (it checked heading hierarchy, alt text, ARIA
   labels, keyboard focus visibility, and label associations; it did not check color-contrast
   ratios pixel-by-pixel, screen-reader announcement order beyond DOM order, or reduced-motion
   preferences).
