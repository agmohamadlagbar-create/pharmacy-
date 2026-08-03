import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

class AppTheme {
  // Brand Colors based on Stitch Unified Commerce design tokens
  static const Color primaryLight = Color(0xFF3525CD);
  static const Color primaryLightContainer = Color(0xFF4F46E5);
  static const Color secondaryLight = Color(0xFF006C49);
  static const Color secondaryLightContainer = Color(0xFF6CF8BB);
  static const Color tertiaryLight = Color(0xFF684000);
  static const Color tertiaryLightContainer = Color(0xFF885500);

  // Light Theme Surfaces & Neutrals
  static const Color surfaceLight = Color(0xFFF9F9FF);
  static const Color surfaceDimLight = Color(0xFFD3DAEA);
  static const Color surfaceBrightLight = Color(0xFFF9F9FF);
  static const Color surfaceContainerLowestLight = Color(0xFFFFFFFF);
  static const Color surfaceContainerLowLight = Color(0xFFF0F3FF);
  static const Color surfaceContainerLight = Color(0xFFE7EEFE);
  static const Color surfaceContainerHighLight = Color(0xFFE2E8F8);
  static const Color surfaceContainerHighestLight = Color(0xFFDCE2F3);
  static const Color onSurfaceLight = Color(0xFF151C27);
  static const Color onSurfaceVariantLight = Color(0xFF464555);
  static const Color outlineLight = Color(0xFF777587);
  static const Color outlineVariantLight = Color(0xFFC7C4D8);
  static const Color errorLight = Color(0xFFBA1A1A);

  // Dark Theme Colors (Derived from Modern Commerce Nexus / Stitch Dark Mode)
  static const Color primaryDark = Color(0xFFBAC3FF);
  static const Color primaryDarkContainer = Color(0xFF3F51B5);
  static const Color secondaryDark = Color(0xFF4EDEA3);
  static const Color secondaryDarkContainer = Color(0xFF00A572);
  static const Color tertiaryDark = Color(0xFFFFB95F);
  static const Color tertiaryDarkContainer = Color(0xFF805000);

  // Dark Theme Surfaces & Neutrals
  static const Color surfaceDark = Color(0xFF0B1326);
  static const Color surfaceDimDark = Color(0xFF0B1326);
  static const Color surfaceBrightDark = Color(0xFF31394D);
  static const Color surfaceContainerLowestDark = Color(0xFF060E20);
  static const Color surfaceContainerLowDark = Color(0xFF131B2E);
  static const Color surfaceContainerDark = Color(0xFF171F33);
  static const Color surfaceContainerHighDark = Color(0xFF222A3D);
  static const Color surfaceContainerHighestDark = Color(0xFF2D3449);
  static const Color onSurfaceDark = Color(0xFFDAE2FD);
  static const Color onSurfaceVariantDark = Color(0xFFC5C5D4);
  static const Color outlineDark = Color(0xFF8F909E);
  static const Color outlineVariantDark = Color(0xFF454652);
  static const Color errorDark = Color(0xFFFFB4AB);

  // Semantic Accents
  static const Color accentEmerald = Color(0xFF10B981);
  static const Color warningAmber = Color(0xFFF59E0B);
  static const Color errorRed = Color(0xFFEF4444);
  static const Color successGreen = Color(0xFF22C55E);

  static ThemeData get lightTheme {
    final baseTextTheme = GoogleFonts.interTextTheme(ThemeData.light().textTheme);
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.light,
      scaffoldBackgroundColor: surfaceLight,
      colorScheme: const ColorScheme.light(
        primary: primaryLight,
        primaryContainer: primaryLightContainer,
        onPrimary: Colors.white,
        onPrimaryContainer: Color(0xFFDAD7FF),
        secondary: secondaryLight,
        secondaryContainer: secondaryLightContainer,
        onSecondary: Colors.white,
        onSecondaryContainer: Color(0xFF00714D),
        tertiary: tertiaryLight,
        tertiaryContainer: tertiaryLightContainer,
        onTertiary: Colors.white,
        onTertiaryContainer: Color(0xFFFFD4A4),
        surface: surfaceLight,
        surfaceContainerLowest: surfaceContainerLowestLight,
        surfaceContainerLow: surfaceContainerLowLight,
        surfaceContainer: surfaceContainerLight,
        surfaceContainerHigh: surfaceContainerHighLight,
        surfaceContainerHighest: surfaceContainerHighestLight,
        onSurface: onSurfaceLight,
        onSurfaceVariant: onSurfaceVariantLight,
        outline: outlineLight,
        outlineVariant: outlineVariantLight,
        error: errorLight,
      ),
      textTheme: baseTextTheme.copyWith(
        displayLarge: baseTextTheme.displayLarge?.copyWith(
          fontSize: 48,
          fontWeight: FontWeight.w700,
          height: 56 / 48,
          letterSpacing: -0.96,
          color: onSurfaceLight,
        ),
        headlineLarge: baseTextTheme.headlineLarge?.copyWith(
          fontSize: 32,
          fontWeight: FontWeight.w700,
          height: 40 / 32,
          letterSpacing: -0.32,
          color: onSurfaceLight,
        ),
        headlineMedium: baseTextTheme.headlineMedium?.copyWith(
          fontSize: 24,
          fontWeight: FontWeight.w600,
          height: 32 / 24,
          color: onSurfaceLight,
        ),
        headlineSmall: baseTextTheme.headlineSmall?.copyWith(
          fontSize: 20,
          fontWeight: FontWeight.w600,
          height: 28 / 20,
          color: onSurfaceLight,
        ),
        titleLarge: baseTextTheme.titleLarge?.copyWith(
          fontSize: 18,
          fontWeight: FontWeight.w600,
          height: 28 / 18,
          color: onSurfaceLight,
        ),
        titleMedium: baseTextTheme.titleMedium?.copyWith(
          fontSize: 16,
          fontWeight: FontWeight.w600,
          height: 24 / 16,
          color: onSurfaceLight,
        ),
        bodyLarge: baseTextTheme.bodyLarge?.copyWith(
          fontSize: 18,
          fontWeight: FontWeight.w400,
          height: 28 / 18,
          color: onSurfaceLight,
        ),
        bodyMedium: baseTextTheme.bodyMedium?.copyWith(
          fontSize: 16,
          fontWeight: FontWeight.w400,
          height: 24 / 16,
          color: onSurfaceLight,
        ),
        bodySmall: baseTextTheme.bodySmall?.copyWith(
          fontSize: 14,
          fontWeight: FontWeight.w400,
          height: 20 / 14,
          color: onSurfaceVariantLight,
        ),
        labelMedium: baseTextTheme.labelMedium?.copyWith(
          fontSize: 14,
          fontWeight: FontWeight.w600,
          height: 20 / 14,
          letterSpacing: 0.7,
          color: onSurfaceLight,
        ),
        labelSmall: baseTextTheme.labelSmall?.copyWith(
          fontSize: 12,
          fontWeight: FontWeight.w500,
          height: 16 / 12,
          color: onSurfaceVariantLight,
        ),
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: surfaceLight,
        elevation: 0,
        scrolledUnderElevation: 1,
        iconTheme: IconThemeData(color: onSurfaceLight),
        titleTextStyle: TextStyle(
          color: onSurfaceLight,
          fontSize: 18,
          fontWeight: FontWeight.bold,
        ),
      ),
      cardTheme: CardThemeData(
        color: surfaceContainerLowestLight,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: outlineVariantLight, width: 1),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: surfaceContainerLight,
        hintStyle: const TextStyle(color: onSurfaceVariantLight, fontSize: 14),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: outlineVariantLight),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: outlineVariantLight),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: primaryLight, width: 2),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: primaryLight,
          foregroundColor: Colors.white,
          elevation: 0,
          padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 20),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
          textStyle: GoogleFonts.inter(fontWeight: FontWeight.w600, fontSize: 15),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: primaryLight,
          side: const BorderSide(color: outlineLight),
          padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 20),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
          textStyle: GoogleFonts.inter(fontWeight: FontWeight.w600, fontSize: 15),
        ),
      ),
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: surfaceLight,
        selectedItemColor: primaryLight,
        unselectedItemColor: onSurfaceVariantLight,
        elevation: 8,
        type: BottomNavigationBarType.fixed,
      ),
    );
  }

  static ThemeData get darkTheme {
    final baseTextTheme = GoogleFonts.interTextTheme(ThemeData.dark().textTheme);
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      scaffoldBackgroundColor: surfaceDark,
      colorScheme: const ColorScheme.dark(
        primary: primaryDark,
        primaryContainer: primaryDarkContainer,
        onPrimary: Color(0xFF08218A),
        onPrimaryContainer: Color(0xFFCACFFF),
        secondary: secondaryDark,
        secondaryContainer: secondaryDarkContainer,
        onSecondary: Color(0xFF003824),
        onSecondaryContainer: Color(0xFF00311F),
        tertiary: tertiaryDark,
        tertiaryContainer: tertiaryDarkContainer,
        onTertiary: Color(0xFF472A00),
        onTertiaryContainer: Color(0xFFFFC988),
        surface: surfaceDark,
        surfaceContainerLowest: surfaceContainerLowestDark,
        surfaceContainerLow: surfaceContainerLowDark,
        surfaceContainer: surfaceContainerDark,
        surfaceContainerHigh: surfaceContainerHighDark,
        surfaceContainerHighest: surfaceContainerHighestDark,
        onSurface: onSurfaceDark,
        onSurfaceVariant: onSurfaceVariantDark,
        outline: outlineDark,
        outlineVariant: outlineVariantDark,
        error: errorDark,
      ),
      textTheme: baseTextTheme.copyWith(
        displayLarge: baseTextTheme.displayLarge?.copyWith(
          fontSize: 48,
          fontWeight: FontWeight.w700,
          height: 56 / 48,
          letterSpacing: -0.96,
          color: onSurfaceDark,
        ),
        headlineLarge: baseTextTheme.headlineLarge?.copyWith(
          fontSize: 32,
          fontWeight: FontWeight.w700,
          height: 40 / 32,
          letterSpacing: -0.32,
          color: onSurfaceDark,
        ),
        headlineMedium: baseTextTheme.headlineMedium?.copyWith(
          fontSize: 24,
          fontWeight: FontWeight.w600,
          height: 32 / 24,
          color: onSurfaceDark,
        ),
        headlineSmall: baseTextTheme.headlineSmall?.copyWith(
          fontSize: 20,
          fontWeight: FontWeight.w600,
          height: 28 / 20,
          color: onSurfaceDark,
        ),
        titleLarge: baseTextTheme.titleLarge?.copyWith(
          fontSize: 18,
          fontWeight: FontWeight.w600,
          height: 28 / 18,
          color: onSurfaceDark,
        ),
        titleMedium: baseTextTheme.titleMedium?.copyWith(
          fontSize: 16,
          fontWeight: FontWeight.w600,
          height: 24 / 16,
          color: onSurfaceDark,
        ),
        bodyLarge: baseTextTheme.bodyLarge?.copyWith(
          fontSize: 18,
          fontWeight: FontWeight.w400,
          height: 28 / 18,
          color: onSurfaceDark,
        ),
        bodyMedium: baseTextTheme.bodyMedium?.copyWith(
          fontSize: 16,
          fontWeight: FontWeight.w400,
          height: 24 / 16,
          color: onSurfaceDark,
        ),
        bodySmall: baseTextTheme.bodySmall?.copyWith(
          fontSize: 14,
          fontWeight: FontWeight.w400,
          height: 20 / 14,
          color: onSurfaceVariantDark,
        ),
        labelMedium: baseTextTheme.labelMedium?.copyWith(
          fontSize: 14,
          fontWeight: FontWeight.w600,
          height: 20 / 14,
          letterSpacing: 0.7,
          color: onSurfaceDark,
        ),
        labelSmall: baseTextTheme.labelSmall?.copyWith(
          fontSize: 12,
          fontWeight: FontWeight.w500,
          height: 16 / 12,
          color: onSurfaceVariantDark,
        ),
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: surfaceDark,
        elevation: 0,
        scrolledUnderElevation: 1,
        iconTheme: IconThemeData(color: onSurfaceDark),
        titleTextStyle: TextStyle(
          color: onSurfaceDark,
          fontSize: 18,
          fontWeight: FontWeight.bold,
        ),
      ),
      cardTheme: CardThemeData(
        color: surfaceContainerDark,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: outlineVariantDark, width: 1),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: surfaceContainerDark,
        hintStyle: const TextStyle(color: onSurfaceVariantDark, fontSize: 14),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: outlineVariantDark),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: outlineVariantDark),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: primaryDark, width: 2),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: primaryDark,
          foregroundColor: const Color(0xFF08218A),
          elevation: 0,
          padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 20),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
          textStyle: GoogleFonts.inter(fontWeight: FontWeight.w600, fontSize: 15),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: primaryDark,
          side: const BorderSide(color: outlineDark),
          padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 20),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
          textStyle: GoogleFonts.inter(fontWeight: FontWeight.w600, fontSize: 15),
        ),
      ),
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: surfaceDark,
        selectedItemColor: primaryDark,
        unselectedItemColor: onSurfaceVariantDark,
        elevation: 8,
        type: BottomNavigationBarType.fixed,
      ),
    );
  }
}
