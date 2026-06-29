import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'main.dart';

class SettingsPage extends StatelessWidget {
  const SettingsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final appState = OnlineStoreApp.of(context);
    final colorScheme = Theme.of(context).colorScheme;
    final isDark = appState?.isDark ?? false;

    return Scaffold(
      appBar: AppBar(
        title: Text('Settings'.tr(),
            style: GoogleFonts.playfairDisplay(fontWeight: FontWeight.w600)),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Appearance section
          _SectionHeader(title: 'APPEARANCE'.tr()),
          const SizedBox(height: 8),
          Card(
            child: SwitchListTile(
              title: Text('Dark Mode'.tr(),
                  style: GoogleFonts.dmSans(fontWeight: FontWeight.w600)),
              subtitle: Text(
                isDark
                    ? 'Dark theme is active'.tr()
                    : 'Light theme is active'.tr(),
                style: GoogleFonts.dmSans(
                  fontSize: 12,
                  color: colorScheme.onSurface.withValues(alpha: 0.5),
                ),
              ),
              value: isDark,
              onChanged: (bool value) {
                appState?.toggleTheme();
              },
              secondary: Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: colorScheme.primary.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(
                  isDark ? Icons.dark_mode_rounded : Icons.light_mode_rounded,
                  color: colorScheme.primary,
                ),
              ),
            ),
          ),

          const SizedBox(height: 16),

          Card(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Language'.tr(),
                          style: GoogleFonts.dmSans(
                              fontWeight: FontWeight.w600, fontSize: 16)),
                      Text('Choose your preferred language'.tr(),
                          style: GoogleFonts.dmSans(
                            fontSize: 12,
                            color: colorScheme.onSurface.withValues(alpha: 0.5),
                          )),
                    ],
                  ),
                  DropdownButton<String>(
                    value: context.locale.languageCode,
                    underline: const SizedBox(),
                    items: [
                      DropdownMenuItem(
                          value: 'en', child: Text('English'.tr())),
                      DropdownMenuItem(value: 'ar', child: Text('Arabic'.tr())),
                      DropdownMenuItem(
                          value: 'tr', child: Text('Turkish'.tr())),
                    ],
                    onChanged: (String? newLanguage) {
                      if (newLanguage != null) {
                        context.setLocale(Locale(newLanguage));
                      }
                    },
                  ),
                ],
              ),
            ),
          ),

          const SizedBox(height: 24),

          // About section
          _SectionHeader(title: 'ABOUT'.tr()),
          const SizedBox(height: 8),
          Card(
            child: Column(
              children: [
                ListTile(
                  leading: Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      color: colorScheme.primary.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Icon(Icons.info_outline_rounded,
                        color: colorScheme.primary),
                  ),
                  title: Text('App Version'.tr(),
                      style: GoogleFonts.dmSans(fontWeight: FontWeight.w600)),
                  subtitle: Text('1.0.0',
                      style: GoogleFonts.dmSans(
                        fontSize: 12,
                        color: colorScheme.onSurface.withValues(alpha: 0.5),
                      )),
                ),
                Divider(
                    height: 1,
                    color: colorScheme.onSurface.withValues(alpha: 0.06)),
                ListTile(
                  leading: Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      color: colorScheme.primary.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child:
                        Icon(Icons.store_rounded, color: colorScheme.primary),
                  ),
                  title: Text('LAGBAR SHOP',
                      style: GoogleFonts.dmSans(fontWeight: FontWeight.w600)),
                  subtitle: Text('Your one-stop marketplace'.tr(),
                      style: GoogleFonts.dmSans(
                        fontSize: 12,
                        color: colorScheme.onSurface.withValues(alpha: 0.5),
                      )),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  final String title;
  const _SectionHeader({required this.title});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(left: 4),
      child: Text(
        title,
        style: GoogleFonts.dmSans(
          fontSize: 11,
          fontWeight: FontWeight.w600,
          letterSpacing: 1.2,
          color: Theme.of(context).colorScheme.onSurface.withValues(alpha: 0.4),
        ),
      ),
    );
  }
}
