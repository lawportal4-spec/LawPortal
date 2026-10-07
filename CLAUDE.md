# Law Portal

Saudi legal-services marketplace. pnpm/turbo monorepo: four Vite/React+Tailwind v4 apps under
`apps/` (`web-client`, `web-lawyer`, `web-admin`) plus an Astro marketing site (`web-marketing`),
a .NET API (`api/`), and a Flutter app (`mobile/`).

## Design system — "Golden Gate" (navy + gold)

The identity comes from the client's brand board: night-navy surfaces, gold accents, and the
logo (a pointed gate + scales of justice + a pen nib), in `apps/*/public/favicon.svg` — the shells,
lawyer auth page and client pledge render it with `<img src="/favicon.svg">`. Single source
of truth: `packages/design-tokens/src/tokens.json`, built by `packages/design-tokens/build.js`
into `dist/theme.css` (consumed by every web app via `@import "@law-portal/design-tokens/theme.css"`)
and `dist/tokens.dart` (Flutter).

**Palette semantics — these are rules, not just colors:**
- `seal` (gold `#B8963A`, with `seal-on` navy text) is the only primary-action color. Used for buttons, active nav state, focus rings.
- `vellum` is reserved exclusively for credential artifacts — the lawyer licence facsimile
  (`CredentialChip.tsx`'s `CredentialCard`/`CredentialChipMini`) — the one ivory surface on the navy
  UI. Never use it as a generic background elsewhere; that would dilute the one place it's supposed to mean
  "official document."
- `rubric` (red) marks marginal/annotation-style labels (eyebrows, "sample data" tags) —
  **never a button fill.** Its `-tint` variant is for danger/disputed states.
- The palette is dark by design: `ink` is ivory text, `paper`/`surface`/`surface-raised` are navy.
  Modal scrims use `bg-black/60`, never `bg-ink/…` (ink is light).

**Typography roles:**
- `font-display` (`LP Cairo` 600/700) — headings only; don't reach for it in dense UI.
- `font-body` (`LP Tajawal` 400/500/700) — everything else, Arabic and Latin both.
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

**One theme.** Web and mobile both use the navy palette; the `color.dark-*` tokens mirror the
main ones so mobile's `LpColorsDark` (wired in `mobile/lib/core/theme/app_theme.dart`) looks the
same. Keep them in sync when changing a color.

**Fonts:** the real font files are subsetted (Arabic + Latin + digits + punctuation, all Arabic
shaping features kept) and compressed to `.woff2` in `packages/design-tokens/src/fonts/`, sourced
from `mobile/assets/fonts/*.ttf` (Cairo/Tajawal from Google Fonts, OFL). `build.js` copies them to `dist/fonts/` and emits `@font-face`
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
