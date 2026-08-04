// Single source of truth: src/tokens.json -> dist/theme.css (Tailwind v4 @theme) + dist/tokens.dart (Flutter).
// Committed output — CI fails the build if regenerating this produces a diff (see root CLAUDE.md / P0 exit criteria).
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
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

  const darkOverrideLines = LIGHT_KEYS.map((k) => `    --color-${k}: ${darkVal(k)};`);

  return `/* GENERATED FILE — do not edit by hand. Source: packages/design-tokens/src/tokens.json */
/* Regenerate with: pnpm --filter @law-portal/design-tokens build */

@theme {
${themeLines.join("\n")}
${fontLines.join("\n")}
${radiusLines.join("\n")}
${spaceLines.join("\n")}
}

@media (prefers-color-scheme: dark) {
  :root {
${darkOverrideLines.join("\n")}
  }
}

:root[data-theme="dark"] {
${darkOverrideLines.join("\n")}
}

:root[data-theme="light"] {
${LIGHT_KEYS.map((k) => `  --color-${k}: ${lightVal(k)};`).join("\n")}
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
/// under these exact family names (LP Amiri / LP Plex Arabic / LP Plex Mono).
abstract final class LpFonts {
  static const String display = 'LP Amiri';
  static const String body = 'LP Plex Arabic';
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
