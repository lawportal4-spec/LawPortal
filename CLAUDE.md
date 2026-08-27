# Law Portal

Saudi legal-services marketplace. pnpm/turbo monorepo: four Vite/React+Tailwind v4 apps under
`apps/` (`web-client`, `web-lawyer`, `web-admin`) plus an Astro marketing site (`web-marketing`),
a .NET API (`api/`), and a Flutter app (`mobile/`).

## Design system — "Sealed Document"

The visual identity is a legal-document concept, not a generic admin-panel look. Single source
of truth: `packages/design-tokens/src/tokens.json`, built by `packages/design-tokens/build.js`
into `dist/theme.css` (consumed by every web app via `@import "@law-portal/design-tokens/theme.css"`)
and `dist/tokens.dart` (Flutter).

**Palette semantics — these are rules, not just colors:**
- `seal` (green) is the only primary-action color. Used for buttons, active nav state, focus rings.
- `vellum` is reserved exclusively for credential artifacts — the lawyer licence facsimile
  (`CredentialChip.tsx`'s `CredentialCard`/`CredentialChipMini`). Never use it as a generic
  warm/cream background elsewhere; that would dilute the one place it's supposed to mean
  "official document."
- `rubric` (red) marks marginal/annotation-style labels (eyebrows, "sample data" tags) —
  **never a button fill.** Its `-tint` variant is for danger/disputed states.
- `category.*` tokens (consult/judiciary/notary/business/other) are light-theme only — there is
  no dark variant, which is one reason dark mode isn't supported on web (see below).

**Typography roles:**
- `font-display` (`LP Amiri`) — headings only, used with restraint. It's a Naskh serif; don't
  reach for it in dense UI (tables, form labels).
- `font-body` (`LP Plex Arabic`) — everything else, Arabic and Latin both.
- `font-mono` (`LP Plex Mono`) — every number, date, ID, licence number, phone number. Always
  wrap the value in `<Ltr>` (`packages/ui/src/components/Ltr.tsx`) so it isolates correctly
  inside RTL paragraphs — this is what prevents numbers/ranges silently reversing in Arabic.
- Section titles: use `SectionHeading` (`packages/ui`), not a hand-rolled
  `font-display text-lg font-bold`. `level={2}` for a page's section titles, `level={3}` for a
  sub-section. Per-item titles inside a repeated card (a plan name, a service name) are a
  different role — keep those at `text-lg`/card scale, don't promote them to `SectionHeading`.

**Elevation:** `shadow-card` (subtle, default for `Card`) and `shadow-raised` (lifted/interactive
surfaces — credential cards, the `Card elevated` prop). Don't reintroduce one-off
`shadow-[...]` arbitrary values; add a token instead if a third elevation step is ever needed.

**Radius/spacing:** `radius.sm/md/lg/xl/full` and `space.1–12` in tokens.json — reuse these, don't
invent new arbitrary values in component code.

**Light theme only on web.** The `color.dark-*` tokens in `tokens.json` exist and still feed
`tokens.dart` — **mobile's dark theme is real and wired** (`mobile/lib/core/theme/app_theme.dart`
builds a genuine `ThemeMode`-aware `ColorScheme` from `LpColorsDark`). Don't delete the dark-*
tokens or touch `buildTokensDart()` on the assumption dark mode is dead — it's dead on *web*
specifically, because no web app ever set `data-theme` and the `category.*` tokens have no dark
variants, so the old `prefers-color-scheme` CSS silently broke rather than working. If web dark
mode is ever built, add the missing category dark variants first.

**Fonts:** the real font files are subsetted (Arabic + Latin + digits + punctuation, all Arabic
shaping features kept) and compressed to `.woff2` in `packages/design-tokens/src/fonts/`, sourced
from `mobile/assets/fonts/*.ttf`. `build.js` copies them to `dist/fonts/` and emits `@font-face`
rules into `theme.css` from the `fontFace` array in `tokens.json` — add new weights there, not by
hand-editing generated CSS.

## Token pipeline — regenerate after every change

`dist/theme.css` and `dist/tokens.dart` are **committed generated output** (CI fails the build if
regenerating produces a diff). After editing `tokens.json` or `build.js`:

```
pnpm --filter @law-portal/design-tokens build
```

## Conventions worth following

- Reuse `packages/ui` primitives (`Button`, `Card`, `Chip`, `StatusPill`, `StatusTag`,
  `SectionHeading`, `Input`, `Avatar`, `CredentialChip`, `Ltr`) before writing new markup.
  `StatusPill` is for the fixed `RequestStatus` set; `StatusTag` colors arbitrary backend enum
  strings (payment status, verification status) by keyword and falls back to neutral for anything
  unrecognized — reach for it instead of a plain gray badge.
- UI strings live in `packages/i18n/src/locales/{ar,en}.json`, shared across all three React apps.
  Don't hardcode `isAr ? "…" : "…"` for new copy — add a key and use `t()`. The `nav.*` and
  `shell.*` sections cover app-shell chrome; `app.*` is the brand name.
- `packages/i18n/eslint/` ships `no-raw-intl-formatting` and `no-physical-rtl-classes` — keep
  `pnpm lint` green, it catches RTL regressions (physical left/right classes instead of logical
  start/end ones).
- `web-marketing`'s Astro pages duplicate Arabic/English structure page-by-page
  (`index.astro` / `en/index.astro`, etc.) rather than sharing a parameterized body component —
  known duplication, not a bug. A visual/copy change to one language's page needs the same edit
  in its counterpart.
