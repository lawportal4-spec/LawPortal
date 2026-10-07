// Single source of truth: src/tokens.json -> dist/theme.css (Tailwind v4 @theme) + dist/tokens.dart (Flutter).
// Committed output — CI fails the build if regenerating this produces a diff (see root CLAUDE.md / P0 exit criteria).
import { copyFileSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const tokens = JSON.parse(readFileSync(path.join(__dirname, "src/tokens.json"), "utf-8"));

const LIGHT_KEYS = [
  "ink", "ink-soft", "ink-faint",
  "seal", "seal-strong", "seal-tint", "seal-on",
  "paper", "surface", "surface-raised",
  "vellum", "vellum-line", "vellum-ink",
  "rubric", "rubric-tint",
  "rule", "border",
  "warning", "warning-tint",
  "success", "success-tint",
  "info", "info-tint",
];

function lightVal(key) {
  return tokens.color[key].value;
}
function darkVal(key) {
  return tokens.color[`dark-${key}`].value;
}

const categories = tokens.color.category;

// ---------------------------------------------------------------------------
// theme.css — Tailwind v4 @theme block (light defaults) + runtime overrides
// ---------------------------------------------------------------------------
// Web ships light theme only — the mobile app's dark theme (LpColorsDark in tokens.dart,
// wired up in mobile/lib/core/theme/app_theme.dart) is real and stays untouched below; only
// the web CSS output drops the dark selectors, which no web app ever set (prefers-color-scheme
// fell back to a palette missing category-token dark variants — broken, not disabled-by-design).
function buildFontFaceCss() {
  mkdirSync(path.join(__dirname, "dist/fonts"), { recursive: true });
  return tokens.fontFace
    .map(({ family, weight, file }) => {
      copyFileSync(path.join(__dirname, "src/fonts", file), path.join(__dirname, "dist/fonts", file));
      return `@font-face {
  font-family: "${family}";
  font-weight: ${weight};
  font-style: normal;
  font-display: swap;
  src: url("./fonts/${file}") format("woff2");
}`;
    })
    .join("\n\n");
}

function buildThemeCss() {
  const themeLines = LIGHT_KEYS.map((k) => `  --color-${k}: ${lightVal(k)};`);
  for (const [name, { bg, fg }] of Object.entries(categories)) {
    themeLines.push(`  --color-cat-${name}-bg: ${bg.value};`);
    themeLines.push(`  --color-cat-${name}-fg: ${fg.value};`);
  }
  const fontLines = Object.entries(tokens.font).map(
    ([name, t]) => `  --font-${name}: ${t.value};`,
  );
  const radiusLines = Object.entries(tokens.size.radius).map(
    ([name, t]) => `  --radius-${name}: ${t.value};`,
  );
  const spaceLines = Object.entries(tokens.size.space).map(
    ([name, t]) => `  --spacing-${name}: ${t.value};`,
  );
  const shadowLines = Object.entries(tokens.size.shadow).map(
    ([name, t]) => `  --shadow-${name}: ${t.value};`,
  );

  return `/* GENERATED FILE — do not edit by hand. Source: packages/design-tokens/src/tokens.json */
/* Regenerate with: pnpm --filter @law-portal/design-tokens build */

${buildFontFaceCss()}

@theme {
${themeLines.join("\n")}
${fontLines.join("\n")}
${radiusLines.join("\n")}
${spaceLines.join("\n")}
${shadowLines.join("\n")}
}

/* The palette is dark: native controls and scrollbars follow it, and browser autofill keeps the
   navy field instead of painting it light blue. */
:root { color-scheme: dark; }
input:-webkit-autofill,
textarea:-webkit-autofill,
select:-webkit-autofill {
  -webkit-text-fill-color: var(--color-ink);
  caret-color: var(--color-ink);
  box-shadow: 0 0 0 1000px var(--color-surface-raised) inset;
  transition: background-color 99999s;
}
`;
}

// ---------------------------------------------------------------------------
// tokens.dart — Flutter ThemeExtension-friendly constants
// ---------------------------------------------------------------------------
function toDartColor(hex) {
  const clean = hex.replace("#", "").toUpperCase();
  return `Color(0xFF${clean})`;
}

function buildTokensDart() {
  const lightFields = LIGHT_KEYS.map(
    (k) => `  static const Color ${toCamel(k)} = ${toDartColor(lightVal(k))};`,
  );
  const darkFields = LIGHT_KEYS.map(
    (k) => `  static const Color ${toCamel(k)} = ${toDartColor(darkVal(k))};`,
  );
  const catLightFields = Object.entries(categories).flatMap(([name, { bg, fg }]) => [
    `  static const Color cat${capitalize(name)}Bg = ${toDartColor(bg.value)};`,
    `  static const Color cat${capitalize(name)}Fg = ${toDartColor(fg.value)};`,
  ]);
  const radiusFields = Object.entries(tokens.size.radius).map(
    ([name, t]) => `  static const double radius${capitalize(name)} = ${parseFloat(t.value)};`,
  );
  const spaceFields = Object.entries(tokens.size.space).map(
    ([name, t]) => `  static const double space${name} = ${parseFloat(t.value)};`,
  );

  return `// GENERATED FILE — do not edit by hand. Source: packages/design-tokens/src/tokens.json
// Regenerate with: pnpm --filter @law-portal/design-tokens build

import 'package:flutter/material.dart';

/// Light-theme token values ("the document, made legible" — paper world).
abstract final class LpColorsLight {
${lightFields.join("\n")}
${catLightFields.join("\n")}
}

/// Dark-theme token values.
abstract final class LpColorsDark {
${darkFields.join("\n")}
}

abstract final class LpRadius {
${radiusFields.join("\n")}
}

abstract final class LpSpace {
${spaceFields.join("\n")}
}

/// Font family names — register the matching asset fonts in pubspec.yaml
/// under these exact family names (LP Cairo / LP Tajawal / LP Plex Mono).
abstract final class LpFonts {
  static const String display = 'LP Cairo';
  static const String body = 'LP Tajawal';
  static const String mono = 'LP Plex Mono';
}
`;
}

function toCamel(kebab) {
  return kebab.replace(/-([a-z])/g, (_, c) => c.toUpperCase());
}
function capitalize(s) {
  return s.charAt(0).toUpperCase() + s.slice(1);
}

mkdirSync(path.join(__dirname, "dist"), { recursive: true });
writeFileSync(path.join(__dirname, "dist/theme.css"), buildThemeCss());
writeFileSync(path.join(__dirname, "dist/tokens.dart"), buildTokensDart());

console.log("law-portal design-tokens: wrote dist/theme.css and dist/tokens.dart");
