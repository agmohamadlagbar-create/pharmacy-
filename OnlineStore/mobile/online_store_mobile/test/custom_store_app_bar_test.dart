import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:online_store_mobile/widgets/custom_store_app_bar.dart';

void main() {
  testWidgets('shows cart badge only when cartCount is greater than zero',
      (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          appBar: CustomStoreAppBar(cartCount: 0),
        ),
      ),
    );

    expect(find.byIcon(Icons.shopping_bag_outlined), findsNothing);
    expect(find.text('0'), findsNothing);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          appBar: CustomStoreAppBar(
            cartCount: 3,
            onCartTap: () {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byIcon(Icons.shopping_bag_outlined), findsOneWidget);
    expect(find.text('3'), findsOneWidget);
  });

  testWidgets('calls leading, search, and cart callbacks', (tester) async {
    var leadingTapped = false;
    var cartTapped = false;
    String latestSearch = '';

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          appBar: CustomStoreAppBar(
            isRoot: true,
            cartCount: 2,
            onLeadingTap: () => leadingTapped = true,
            onCartTap: () => cartTapped = true,
            onSearchChanged: (value) => latestSearch = value,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.menu_rounded));
    await tester.pump();
    expect(leadingTapped, isTrue);

    await tester.enterText(find.byType(TextField), 'olive oil');
    await tester.pump();
    expect(latestSearch, 'olive oil');

    await tester.tap(find.byIcon(Icons.shopping_bag_outlined));
    await tester.pump();
    expect(cartTapped, isTrue);
  });

  testWidgets('uses clickable search container when tap callback is provided',
      (tester) async {
    var tapped = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          appBar: CustomStoreAppBar(
            onSearchTap: () => tapped = true,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Search products...'));
    await tester.pump();
    expect(tapped, isTrue);
  });

  testWidgets('default root leading action opens the drawer', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          drawer: Drawer(child: Text('Drawer content')),
          appBar: CustomStoreAppBar(isRoot: true),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.menu_rounded));
    await tester.pumpAndSettle();

    expect(find.text('Drawer content'), findsOneWidget);
  });
}
