import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:online_store_mobile/main.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    GoogleFonts.config.allowRuntimeFetching = false;
  });

  tearDown(() {
    rootBundle.clear();
  });

  Future<void> pumpMainApp(WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(430, 932));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.runAsync(() async {
      await EasyLocalization.ensureInitialized();
      await tester.pumpWidget(
        EasyLocalization(
          supportedLocales: const [Locale('en'), Locale('ar'), Locale('tr')],
          path: 'assets/translations',
          fallbackLocale: const Locale('en'),
          useOnlyLangCode: true,
          child: const OnlineStoreApp(),
        ),
      );
    });
    await tester.pumpAndSettle();
  }

  testWidgets('golden: auth screen', (tester) async {
    await pumpMainApp(tester);

    await expectLater(
      find.byType(Scaffold).first,
      matchesGoldenFile('goldens/auth_screen.png'),
    );
  });

  testWidgets('golden: settings page', (tester) async {
    await pumpMainApp(tester);

    final scaffold = find.byType(Scaffold);
    final context = tester.element(scaffold.first);
    Navigator.of(context).pushNamed('/settings');
    await tester.pumpAndSettle();

    await expectLater(
      find.byType(Scaffold).last,
      matchesGoldenFile('goldens/settings_page.png'),
    );
  });

  testWidgets('golden: favorites page empty state', (tester) async {
    await pumpMainApp(tester);

    final scaffold = find.byType(Scaffold);
    final context = tester.element(scaffold.first);
    Navigator.of(context).pushNamed('/favorites');
    await tester.pumpAndSettle();

    await expectLater(
      find.byType(Scaffold).last,
      matchesGoldenFile('goldens/favorites_page_empty.png'),
    );
  });
}
