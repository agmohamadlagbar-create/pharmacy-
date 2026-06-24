import 'dart:convert';

import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:online_store_mobile/favorites_page.dart';
import 'package:shared_preferences/shared_preferences.dart';

Widget _buildTestable(Widget child) {
  return EasyLocalization(
    supportedLocales: const [Locale('en'), Locale('ar'), Locale('tr')],
    path: 'assets/translations',
    fallbackLocale: const Locale('en'),
    useOnlyLangCode: true,
    child: MaterialApp(home: child),
  );
}

void main() {
  setUp(() {
    GoogleFonts.config.allowRuntimeFetching = false;
  });

  tearDown(() {
    rootBundle.clear();
  });

  testWidgets('FavoritesPage renders saved favorites and can remove one',
      (tester) async {
    SharedPreferences.setMockInitialValues({
      'favorites_list': jsonEncode([
        {
          'id': 10,
          'name': 'Premium Dates',
          'price': 12.5,
          'imageUrl': null,
        }
      ])
    });

    await tester.runAsync(() async {
      await EasyLocalization.ensureInitialized();
      await tester.pumpWidget(_buildTestable(const FavoritesPage()));
    });
    await tester.pumpAndSettle();

    expect(find.text('Premium Dates'), findsOneWidget);
    expect(find.text('\$12.5'), findsOneWidget);

    await tester.tap(find.byIcon(Icons.favorite));
    await tester.pumpAndSettle();

    expect(find.text('No favorites yet'), findsOneWidget);
    expect(find.text('Items you love will appear here'), findsOneWidget);
  });

  testWidgets('FavoritesPage empty state start shopping button pops route',
      (tester) async {
    SharedPreferences.setMockInitialValues({});

    await tester.runAsync(() async {
      await EasyLocalization.ensureInitialized();
      await tester.pumpWidget(
        _buildTestable(
          Builder(
            builder: (context) => ElevatedButton(
              onPressed: () {
                Navigator.of(context).push(
                  MaterialPageRoute(builder: (_) => const FavoritesPage()),
                );
              },
              child: const Text('open'),
            ),
          ),
        ),
      );
    });
    await tester.pumpAndSettle();

    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();
    expect(find.text('No favorites yet'), findsOneWidget);

    await tester.tap(find.text('Start Shopping'));
    await tester.pumpAndSettle();

    expect(find.text('open'), findsOneWidget);
    expect(find.text('No favorites yet'), findsNothing);
  });
}
