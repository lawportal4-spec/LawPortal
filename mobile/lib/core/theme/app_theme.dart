import 'package:flutter/material.dart';
import 'tokens.dart';

/// Builds the light/dark ThemeData from the generated design tokens. Mirrors the palette and
/// type roles defined in packages/design-tokens and rendered in the design-system preview.
abstract final class AppTheme {
  static ThemeData light() => _build(
        ink: LpColorsLight.ink,
        inkSoft: LpColorsLight.inkSoft,
        seal: LpColorsLight.seal,
        sealOn: LpColorsLight.sealOn,
        paper: LpColorsLight.paper,
        surface: LpColorsLight.surface,
        border: LpColorsLight.border,
        brightness: Brightness.light,
      );

  static ThemeData dark() => _build(
        ink: LpColorsDark.ink,
        inkSoft: LpColorsDark.inkSoft,
        seal: LpColorsDark.seal,
        sealOn: LpColorsDark.sealOn,
        paper: LpColorsDark.paper,
        surface: LpColorsDark.surface,
        border: LpColorsDark.border,
        brightness: Brightness.dark,
      );

  static ThemeData _build({
    required Color ink,
    required Color inkSoft,
    required Color seal,
    required Color sealOn,
    required Color paper,
    required Color surface,
    required Color border,
    required Brightness brightness,
  }) {
    final colorScheme = ColorScheme(
      brightness: brightness,
      primary: seal,
      onPrimary: sealOn,
      secondary: seal,
      onSecondary: sealOn,
      surface: surface,
      onSurface: ink,
      error: LpColorsLight.rubric,
      onError: sealOn,
    );

    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: colorScheme,
      scaffoldBackgroundColor: paper,
      fontFamily: LpFonts.body,
      textTheme: Typography.material2021(platform: TargetPlatform.iOS)
          .black
          .apply(fontFamily: LpFonts.body, bodyColor: ink, displayColor: ink),
      cardTheme: CardThemeData(
        color: surface,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(LpRadius.radiusLg),
          side: BorderSide(color: border),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: seal,
          foregroundColor: sealOn,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(LpRadius.radiusMd)),
          textStyle: const TextStyle(fontFamily: LpFonts.body, fontWeight: FontWeight.w600),
        ),
      ),
      dividerColor: border,
    );
  }
}
