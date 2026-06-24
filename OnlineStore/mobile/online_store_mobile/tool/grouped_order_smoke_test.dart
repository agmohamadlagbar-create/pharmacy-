// ignore_for_file: avoid_print

import 'package:online_store_mobile/utils/grouped_order_utils.dart';

void main() {
  final stores = [
    {
      'items': [
        {'quantity': 2},
        {'quantity': 1},
      ],
      'status': 'Preparing',
    },
    {
      'items': [
        {'quantity': 4},
      ],
      'status': 'Delivered',
    },
  ];

  if (countGroupedOrderItems(stores) != 7) {
    throw StateError('countGroupedOrderItems should sum grouped quantities.');
  }

  if (countReadyGroupedStores(stores) != 1) {
    throw StateError('countReadyGroupedStores should only count ready-or-later stores.');
  }

  if (groupedOrderStatusHeadline('Preparing', 1, 2) != '1 of 2 stores are ready') {
    throw StateError('groupedOrderStatusHeadline should describe grouped preparing progress.');
  }

  if (!canCancelGroupedOrder('Pending') || canCancelGroupedOrder('OnTheWay')) {
    throw StateError('canCancelGroupedOrder should only allow early grouped statuses.');
  }

  print('Mobile grouped-order smoke tests passed.');
}
