import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:online_store_mobile/active_orders_page.dart';
import 'package:online_store_mobile/add_card_page.dart';
import 'package:online_store_mobile/admin_page.dart';
import 'package:online_store_mobile/cart_page.dart';
import 'package:online_store_mobile/categories_page.dart';
import 'package:online_store_mobile/category_products_page.dart';
import 'package:online_store_mobile/chat/ai_chat_bottom_sheet.dart';
import 'package:online_store_mobile/checkout_page.dart';
import 'package:online_store_mobile/delivered_orders_page.dart';
import 'package:online_store_mobile/delivery_jobs_page.dart';
import 'package:online_store_mobile/edit_profile_page.dart';
import 'package:online_store_mobile/notifications_page.dart';
import 'package:online_store_mobile/payment_page.dart';
import 'package:online_store_mobile/product_info_page.dart';
import 'package:online_store_mobile/profile_page.dart';
import 'package:online_store_mobile/public_store_page.dart';
import 'package:online_store_mobile/search_products_page.dart';
import 'package:online_store_mobile/settings_page.dart';
import 'package:online_store_mobile/store_orders_page.dart';
import 'package:online_store_mobile/store_products_page.dart';
import 'package:online_store_mobile/widgets/ai_assistant_fab.dart';
import 'package:online_store_mobile/widgets/profile_ripple_avatar.dart';
import 'package:online_store_mobile/widgets/recommendation_cards_widget.dart';
import 'package:online_store_mobile/widgets/ripple_avatar.dart';
import 'package:online_store_mobile/widgets/skeleton_loader.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
    GoogleFonts.config.allowRuntimeFetching = false;
  });

  test('core mobile pages can be constructed with their public APIs', () {
    final pages = <Widget>[
      const ActiveOrdersPage(),
      const AddCardPage(),
      const AdminPage(),
      CartPage(
        cart: const {'items': [], 'totalAmount': 0.0},
        onUpdateQuantity: (_, __) {},
        onRemoveItem: (_) {},
        onCheckout: () {},
      ),
      CategoriesPage(
        onLogout: () {},
        cartCount: 0,
        onNavigateCart: () {},
        onAddToCart: (_) {},
      ),
      CategoryProductsPage(
        categoryId: '1',
        categoryName: 'Grocery',
        cartCount: 0,
        onNavigateCart: () {},
        onAddToCart: (_) {},
      ),
      CheckoutPage(cart: const {'items': [], 'totalAmount': 0.0}),
      const DeliveredOrdersPage(),
      const DeliveryJobsPage(),
      const EditProfilePage(),
      const NotificationsPage(),
      PaymentPage(
        cart: const {'items': [], 'totalAmount': 12.5},
        checkoutData: const {
          'paymentMethod': 'Cash on Delivery',
          'addressString': 'Home',
        },
      ),
      const ProductInfoPage(productId: 1),
      const ProfilePage(),
      const PublicStorePage(storeUserId: 'store-1'),
      const SearchProductsPage(),
      const SettingsPage(),
      const StoreOrdersPage(),
      const StoreProductsPage(),
      const AiChatBottomSheet(),
      const AiAssistantFab(),
    ];

    expect(pages, everyElement(isA<Widget>()));
  });

  testWidgets('avatar, skeleton, and assistant FAB widgets render offline',
      (tester) async {
    var tapped = false;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Column(
            children: [
              RippleAvatar(size: 48, onTap: () => tapped = true),
              const ProfileRippleAvatar(size: 48),
              const Skeleton(height: 12, width: 60),
            ],
          ),
          floatingActionButton: const AiAssistantFab(),
        ),
      ),
    );

    expect(find.byType(RippleAvatar), findsOneWidget);
    expect(find.byType(ProfileRippleAvatar), findsOneWidget);
    expect(find.byType(Skeleton), findsOneWidget);
    expect(find.byIcon(Icons.auto_awesome), findsOneWidget);

    await tester.tap(find.byType(RippleAvatar));
    expect(tapped, isTrue);
  });

  testWidgets('RecommendationCardsWidget renders valid product sections',
      (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SizedBox(
            height: 360,
            child: RecommendationCardsWidget(
              data: const {
                'BasedOnHistory': [
                  {
                    'ProductId': 1,
                    'ProductName': 'Olive Oil',
                    'Price': 12.5,
                    'Stock': 5,
                    'StoreName': 'Fresh Market',
                  }
                ],
              },
              onAddToCart: (_, __) {},
              onNavigateProduct: (_) {},
            ),
          ),
        ),
      ),
    );

    await tester.pump();

    expect(find.text('Recommended For You'), findsOneWidget);
    expect(find.text('Olive Oil'), findsOneWidget);
    expect(find.text('FRESH MARKET'), findsOneWidget);
  });
}
