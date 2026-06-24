import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:online_store_mobile/main.dart';
import 'package:online_store_mobile/favorites_page.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Helper wrapping the full [OnlineStoreApp] with [EasyLocalization].
Widget _buildTestableApp() {
  return EasyLocalization(
    supportedLocales: const [Locale('en'), Locale('ar'), Locale('tr')],
    path: 'assets/translations',
    fallbackLocale: const Locale('en'),
    useOnlyLangCode: true,
    child: const OnlineStoreApp(),
  );
}

void main() {
  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    GoogleFonts.config.allowRuntimeFetching = false;
  });

  tearDown(() {
    // Clear the asset bundle cache so EasyLocalization reloads
    // translations from scratch in the next test.
    rootBundle.clear();
  });

  testWidgets('Auth screen renders core login UI', (WidgetTester tester) async {
    // Run EasyLocalization init + pumpWidget inside runAsync so that
    // real I/O (asset loading) completes before we assert.
    await tester.runAsync(() async {
      await EasyLocalization.ensureInitialized();
      await tester.pumpWidget(_buildTestableApp());
    });
    await tester.pumpAndSettle();

    expect(find.text('LAGBAR SHOP'), findsOneWidget);
    expect(find.text('Login'), findsWidgets);
    expect(find.text('Continue as Guest'), findsOneWidget);
  });

  testWidgets('Settings page route renders main controls', (WidgetTester tester) async {
    await tester.runAsync(() async {
      await EasyLocalization.ensureInitialized();
      await tester.pumpWidget(_buildTestableApp());
    });
    await tester.pumpAndSettle();

    // Navigate to settings.
    final scaffold = find.byType(Scaffold);
    final context = tester.element(scaffold.first);
    Navigator.of(context).pushNamed('/settings');
    await tester.pumpAndSettle();

    expect(find.text('Settings'), findsOneWidget);
    expect(find.text('Dark Mode'), findsOneWidget);
    expect(find.byType(SwitchListTile), findsOneWidget);
  });

  testWidgets('Favorites page shows empty-state message when list is empty', (WidgetTester tester) async {
    // FavoritesPage doesn't use context.locale, so a bare MaterialApp works.
    await tester.pumpWidget(MaterialApp(home: const FavoritesPage()));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text('My Favorites'), findsOneWidget);
    expect(find.text('No favorites yet'), findsOneWidget);
    expect(find.text('Start Shopping'), findsOneWidget);
  });
}
