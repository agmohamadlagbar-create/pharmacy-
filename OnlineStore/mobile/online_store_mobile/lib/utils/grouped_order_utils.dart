import 'package:easy_localization/easy_localization.dart';

int countGroupedOrderItems(List<Map<String, dynamic>> stores) {
  return stores.fold<int>(0, (sum, store) {
    final items = ((store['items'] as List?) ?? [])
        .map((item) => Map<String, dynamic>.from(item as Map));
    return sum +
        items.fold<int>(
          0,
          (storeSum, item) => storeSum + ((item['quantity'] as num?)?.toInt() ?? 0),
        );
  });
}

int countReadyGroupedStores(List<Map<String, dynamic>> stores) {
  const readyStatuses = {
    'ReadyForPickup',
    'PickedUp',
    'OnTheWay',
    'Delivered',
  };

  return stores.where((store) => readyStatuses.contains(store['status']?.toString())).length;
}

bool canCancelGroupedOrder(String status) {
  return status == 'Pending' || status == 'Preparing';
}

String groupedOrderStatusHeadline(String status, int readyStores, int totalStores) {
  switch (status) {
    case 'Pending':
      return 'Waiting for stores to accept'.tr();
    case 'Preparing':
      return 'Stores are preparing your items'.tr();
    case 'ReadyForPickup':
      return 'All stores are ready for pickup'.tr();
    case 'Assigned':
      return 'Driver is collecting from every store'.tr();
    case 'PickedUp':
      return 'All store pickups are complete'.tr();
    case 'OnTheWay':
      return 'Driver is heading to your address'.tr();
    case 'Delivered':
      return 'Delivered successfully'.tr();
    case 'Cancelled':
      return 'This grouped order was cancelled'.tr();
    default:
      return 'Tracking your grouped order'.tr();
  }
}
