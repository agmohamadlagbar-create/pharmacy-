import 'package:flutter_test/flutter_test.dart';
import 'package:online_store_mobile/utils/grouped_order_utils.dart';

void main() {
  test('countGroupedOrderItems sums quantities across all grouped stores', () {
    final stores = [
      {
        'items': [
          {'quantity': 2},
          {'quantity': 1},
        ],
      },
      {
        'items': [
          {'quantity': 4},
        ],
      },
    ];

    expect(countGroupedOrderItems(stores), 7);
  });

  test('countReadyGroupedStores only counts ready or later statuses', () {
    final stores = [
      {'status': 'Pending'},
      {'status': 'Preparing'},
      {'status': 'ReadyForPickup'},
      {'status': 'PickedUp'},
      {'status': 'OnTheWay'},
      {'status': 'Delivered'},
    ];

    expect(countReadyGroupedStores(stores), 4);
  });

  test('groupedOrderStatusHeadline describes grouped store progress', () {
    // The function returns the localization key as-is since EasyLocalization
    // is not initialised in unit tests (.tr() passes through the raw key).
    expect(
      groupedOrderStatusHeadline('Preparing', 3, 5),
      'Stores are preparing your items',
    );
  });

  test('canCancelGroupedOrder only allows early grouped statuses', () {
    expect(canCancelGroupedOrder('Pending'), isTrue);
    expect(canCancelGroupedOrder('Preparing'), isTrue);
    expect(canCancelGroupedOrder('OnTheWay'), isFalse);
  });
}
