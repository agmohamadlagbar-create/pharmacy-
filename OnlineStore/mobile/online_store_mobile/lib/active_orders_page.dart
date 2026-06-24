import 'dart:async';
import 'package:easy_localization/easy_localization.dart';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:latlong2/latlong.dart';

import 'api.dart';

import 'signalr_service.dart';
import 'utils/grouped_order_utils.dart';

class ActiveOrdersPage extends StatefulWidget {
  const ActiveOrdersPage({super.key});

  @override
  State<ActiveOrdersPage> createState() => _ActiveOrdersPageState();
}

class _ActiveOrdersPageState extends State<ActiveOrdersPage> {
  List<Map<String, dynamic>> _activeOrders = [];
  bool _isLoading = true;
  String? _error;
  String? _expandedGroupId;
  String? _cancellingGroupId;
  final SignalRService _signalRService = SignalRService();
  final Map<String, LatLng> _driverPositions = {};
  final Map<String, MapController> _mapControllers = {};
  StreamSubscription? _statusSubscription;
  StreamSubscription? _locationSubscription;

  @override
  void initState() {
    super.initState();
    _initSignalR();
    _fetchActiveOrders();
  }

  Future<void> _initSignalR() async {
    await _signalRService.connect();

    _statusSubscription = _signalRService.statusUpdates.listen((data) {
      final orderGroupId = data['orderGroupId']?.toString();
      final status = data['status']?.toString();
      if (orderGroupId != null && status != null) {
        if (mounted) {
          setState(() {
            final index = _activeOrders.indexWhere(
                (o) => o['orderGroupId']?.toString() == orderGroupId);
            if (index != -1) {
              _activeOrders[index]['status'] = status;
              if (status == 'Delivered') {
                _activeOrders.removeAt(index);
              }
            } else if (status != 'Delivered') {
              _fetchActiveOrders(showLoader: false);
            }
          });
        }
      }
    });

    _locationSubscription = _signalRService.locationUpdates.listen((data) {
      final orderGroupId = data['orderGroupId']?.toString();
      final lat = (data['latitude'] as num?)?.toDouble();
      final lng = (data['longitude'] as num?)?.toDouble();
      if (orderGroupId != null && lat != null && lng != null) {
        if (mounted) {
          final pos = LatLng(lat, lng);
          setState(() {
            _driverPositions[orderGroupId] = pos;
          });
          // Auto-center map if controller exists
          _mapControllers[orderGroupId]?.move(pos, 14.0);
        }
      }
    });
  }

  @override
  void dispose() {
    _statusSubscription?.cancel();
    _locationSubscription?.cancel();
    for (var controller in _mapControllers.values) {
      controller.dispose();
    }
    for (var order in _activeOrders) {
      final id = order['orderGroupId']?.toString();
      if (id != null) _signalRService.leaveGroupedOrderGroup(id);
    }
    super.dispose();
  }

  Future<void> _fetchActiveOrders({bool showLoader = true}) async {
    if (showLoader && mounted) {
      setState(() {
        _isLoading = true;
        _error = null;
      });
    }

    try {
      final res = await Api.get('/api/orders/active');
      final data = res['data'] is List ? res['data'] : (res is List ? res : []);

      if (!mounted) return;
      setState(() {
        _activeOrders = (data as List)
            .map((e) => Map<String, dynamic>.from(e as Map))
            .toList();
        _isLoading = false;
        _error = null;
      });

      // Join SignalR groups for all active orders
      for (var order in _activeOrders) {
        final id = order['orderGroupId']?.toString();
        if (id != null) {
          _signalRService.joinGroupedOrderGroup(id);
        }
      }
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _error = '${'Failed to load active orders:'.tr()} $e';
      });
    }
  }

  Future<void> _cancelGroupedOrder(Map<String, dynamic> order) async {
    final orderGroupId = order['orderGroupId']?.toString();
    final representativeOrderId = order['representativeOrderId'];
    if (orderGroupId == null || representativeOrderId == null) return;

    setState(() => _cancellingGroupId = orderGroupId);
    try {
      await Api.put('/api/orders/$representativeOrderId/cancel', {});
      await _fetchActiveOrders(showLoader: false);

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Order #{{id}} cancelled successfully.'.tr(namedArgs: {'id': orderGroupId}))),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('${'Failed to cancel grouped order:'.tr()} $e')),
      );
    } finally {
      if (mounted) {
        setState(() => _cancellingGroupId = null);
      }
    }
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'Pending':
        return const Color(0xFFF59E0B);
      case 'Preparing':
        return const Color(0xFF3B82F6);
      case 'ReadyForPickup':
        return const Color(0xFF22C55E);
      case 'Assigned':
        return const Color(0xFF3B82F6);
      case 'PickedUp':
        return const Color(0xFF8B5CF6);
      case 'OnTheWay':
        return const Color(0xFF06B6D4);
      case 'Delivered':
        return const Color(0xFF16A34A);
      case 'Cancelled':
        return const Color(0xFFEF4444);
      default:
        return const Color(0xFF888888);
    }
  }

  LatLng _centerForOrder(Map<String, dynamic> order, List<Map<String, dynamic>> stores) {
    final points = <LatLng>[];

    for (final store in stores) {
      final lat = (store['storeLat'] as num?)?.toDouble();
      final lng = (store['storeLng'] as num?)?.toDouble();
      if (lat != null && lng != null) {
        points.add(LatLng(lat, lng));
      }
    }

    final customerLat = (order['customerLat'] as num?)?.toDouble();
    final customerLng = (order['customerLng'] as num?)?.toDouble();
    if (customerLat != null && customerLng != null) {
      points.add(LatLng(customerLat, customerLng));
    }

    if (points.isEmpty) {
      return const LatLng(41.0082, 28.9784);
    }

    final avgLat = points.map((p) => p.latitude).reduce((a, b) => a + b) / points.length;
    final avgLng = points.map((p) => p.longitude).reduce((a, b) => a + b) / points.length;
    return LatLng(avgLat, avgLng);
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: Text(
          'Active Orders'.tr(),
          style: GoogleFonts.playfairDisplay(fontWeight: FontWeight.w600),
        ),
        actions: [
          IconButton(
            onPressed: () => _fetchActiveOrders(showLoader: false),
            icon: const Icon(Icons.refresh_rounded),
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(_error!, style: const TextStyle(color: Colors.red)),
                        const SizedBox(height: 12),
                        FilledButton(
                          onPressed: () => _fetchActiveOrders(),
                          child: Text('Retry'.tr()),
                        ),
                      ],
                    ),
                  ),
                )
              : _activeOrders.isEmpty
                  ? Center(
                      child: Padding(
                        padding: const EdgeInsets.all(24),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(
                              Icons.local_shipping_outlined,
                              size: 64,
                              color: colorScheme.onSurface.withValues(alpha: 0.28),
                            ),
                            const SizedBox(height: 16),
                            Text(
                              'No Active Orders'.tr(),
                              style: GoogleFonts.playfairDisplay(
                                fontSize: 20,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            const SizedBox(height: 8),
                            Text(
                              'Your grouped deliveries will appear here once stores start preparing them.'.tr(),
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                color: colorScheme.onSurface.withValues(alpha: 0.6),
                              ),
                            ),
                          ],
                        ),
                      ),
                    )
                  : RefreshIndicator(
                      onRefresh: () => _fetchActiveOrders(),
                      child: ListView.builder(
                        padding: const EdgeInsets.all(16),
                        itemCount: _activeOrders.length,
                        itemBuilder: (context, index) {
                          final order = _activeOrders[index];
                          return _buildGroupedOrderCard(order, colorScheme);
                        },
                      ),
                    ),
    );
  }

  Widget _buildGroupedOrderCard(Map<String, dynamic> order, ColorScheme colorScheme) {
    final orderGroupId = order['orderGroupId']?.toString() ?? 'Unknown';
    final stores = ((order['stores'] as List?) ?? [])
        .map((store) => Map<String, dynamic>.from(store as Map))
        .toList();
    final isExpanded = _expandedGroupId == orderGroupId;
    final status = order['status']?.toString() ?? 'Pending';
    final statusColor = _statusColor(status);
    final totalAmount = (order['totalAmount'] as num?)?.toDouble() ?? 0;
    final totalItems = countGroupedOrderItems(stores);
    final readyStores = countReadyGroupedStores(stores);
    final canCancel = canCancelGroupedOrder(status);
    final isCancelling = _cancellingGroupId == orderGroupId;
    final center = _centerForOrder(order, stores);

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
      child: InkWell(
        borderRadius: BorderRadius.circular(18),
        onTap: () {
          setState(() {
            _expandedGroupId = isExpanded ? null : orderGroupId;
          });
        },
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          groupedOrderStatusHeadline(status, readyStores, stores.length),
                          style: GoogleFonts.dmSans(
                            fontWeight: FontWeight.w700,
                            fontSize: 15,
                            color: colorScheme.onSurface,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Wrap(
                          spacing: 8,
                          runSpacing: 8,
                          children: [
                            _infoChip('{{count}} stores'.tr(namedArgs: {'count': stores.length.toString()})),
                            _infoChip('{{count}} items'.tr(namedArgs: {'count': totalItems.toString()})),
                            _infoChip('\$${totalAmount.toStringAsFixed(2)}'),
                          ],
                        ),
                      ],
                    ),
                  ),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                        decoration: BoxDecoration(
                          color: statusColor.withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(999),
                        ),
                        child: Text(
                          status.tr(),
                          style: GoogleFonts.dmSans(
                            fontSize: 11,
                            fontWeight: FontWeight.w700,
                            color: statusColor,
                          ),
                        ),
                      ),
                      const SizedBox(height: 8),
                      AnimatedRotation(
                        turns: isExpanded ? 0.5 : 0,
                        duration: const Duration(milliseconds: 180),
                        child: Icon(
                          Icons.keyboard_arrow_down_rounded,
                          color: colorScheme.onSurface.withValues(alpha: 0.35),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
              const SizedBox(height: 12),
              _buildStatusTrack(status, colorScheme),
              const SizedBox(height: 12),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: colorScheme.surfaceContainerHighest.withValues(alpha: 0.32),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(Icons.place_outlined, color: colorScheme.primary, size: 18),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        order['deliveryAddress']?.toString() ?? 'No delivery address'.tr(),
                        style: GoogleFonts.dmSans(
                          fontSize: 12,
                          color: colorScheme.onSurface.withValues(alpha: 0.7),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              if (isExpanded) ...[
                const SizedBox(height: 14),
                if (stores.any((store) =>
                    store['storeLat'] != null ||
                    store['storeLng'] != null ||
                    order['customerLat'] != null ||
                    order['customerLng'] != null))
                  _buildMap(center, order, stores),
                if (stores.any((store) =>
                    store['storeLat'] != null ||
                    store['storeLng'] != null ||
                    order['customerLat'] != null ||
                    order['customerLng'] != null))
                  const SizedBox(height: 14),
                Text(
                  'Pickup stops'.tr(),
                  style: GoogleFonts.dmSans(
                    fontWeight: FontWeight.w700,
                    fontSize: 14,
                  ),
                ),
                const SizedBox(height: 10),
                ...stores.map((store) => _buildStoreStopCard(store, colorScheme)),
                const SizedBox(height: 8),
                Text(
                  'Live updates enabled via SignalR. You can follow store readiness and delivery progress in real-time.'.tr(),
                  style: GoogleFonts.dmSans(
                    fontSize: 12,
                    color: colorScheme.onSurface.withValues(alpha: 0.55),
                  ),
                ),
                if (canCancel) ...[
                  const SizedBox(height: 14),
                  SizedBox(
                    width: double.infinity,
                    child: OutlinedButton.icon(
                      onPressed: isCancelling ? null : () => _cancelGroupedOrder(order),
                      icon: isCancelling
                          ? const SizedBox(
                              width: 16,
                              height: 16,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Icon(Icons.cancel_outlined),
                      label: Text(isCancelling ? 'Cancelling...'.tr() : 'Cancel This Grouped Order'.tr()),
                    ),
                  ),
                ],
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildStatusTrack(String status, ColorScheme colorScheme) {
    final steps = [
      ('Pending', 'Placed'.tr()),
      ('Preparing', 'Preparing'.tr()),
      ('ReadyForPickup', 'Ready'.tr()),
      ('Assigned', 'Assigned'.tr()),
      ('PickedUp', 'Picked up'.tr()),
      ('OnTheWay', 'On way'.tr()),
      ('Delivered', 'Delivered'.tr()),
    ];

    final currentIndex = steps.indexWhere((step) => step.$1 == status);

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: List.generate(steps.length, (index) {
          final step = steps[index];
          final isActive = currentIndex >= 0 && index <= currentIndex;
          final isCurrent = index == currentIndex;

          return Padding(
            padding: EdgeInsets.only(right: index == steps.length - 1 ? 0 : 10),
            child: Column(
              children: [
                AnimatedContainer(
                  duration: const Duration(milliseconds: 220),
                  width: isCurrent ? 16 : 12,
                  height: isCurrent ? 16 : 12,
                  decoration: BoxDecoration(
                    color: isActive ? colorScheme.primary : colorScheme.outlineVariant,
                    shape: BoxShape.circle,
                    boxShadow: isCurrent
                        ? [
                            BoxShadow(
                              color: colorScheme.primary.withValues(alpha: 0.35),
                              blurRadius: 12,
                            ),
                          ]
                        : null,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  step.$2,
                  style: GoogleFonts.dmSans(
                    fontSize: 11,
                    fontWeight: isCurrent ? FontWeight.w700 : FontWeight.w500,
                    color: isActive
                        ? colorScheme.onSurface
                        : colorScheme.onSurface.withValues(alpha: 0.38),
                  ),
                ),
              ],
            ),
          );
        }),
      ),
    );
  }

  Widget _buildMap(
    LatLng center,
    Map<String, dynamic> order,
    List<Map<String, dynamic>> stores,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final orderGroupId = order['orderGroupId']?.toString();
    if (orderGroupId != null && !_mapControllers.containsKey(orderGroupId)) {
      _mapControllers[orderGroupId] = MapController();
    }

    final markers = <Marker>[];

    for (final store in stores) {
      final lat = (store['storeLat'] as num?)?.toDouble();
      final lng = (store['storeLng'] as num?)?.toDouble();
      if (lat == null || lng == null) continue;

      markers.add(
        Marker(
          point: LatLng(lat, lng),
          width: 44,
          height: 44,
          child: Tooltip(
            message: store['storeName']?.toString() ?? 'Store'.tr(),
            child: const Icon(Icons.storefront_rounded, color: Color(0xFFF59E0B), size: 30),
          ),
        ),
      );
    }

    final customerLat = (order['customerLat'] as num?)?.toDouble();
    final customerLng = (order['customerLng'] as num?)?.toDouble();
    if (customerLat != null && customerLng != null) {
      markers.add(
        Marker(
          point: LatLng(customerLat, customerLng),
          width: 44,
          height: 44,
          child: Tooltip(
            message: 'Delivery address'.tr(),
            child: const Icon(Icons.home_rounded, color: Color(0xFFD85A30), size: 30),
          ),
        ),
      );
    }

    final driverPos = _driverPositions[orderGroupId];
    if (driverPos != null) {
      markers.add(
        Marker(
          point: driverPos,
          width: 50,
          height: 50,
          child: Container(
            decoration: BoxDecoration(
              color: colorScheme.primary.withValues(alpha: 0.2),
              shape: BoxShape.circle,
            ),
            child: Icon(Icons.delivery_dining_rounded,
                color: colorScheme.primary, size: 34),
          ),
        ),
      );
    }

    if (markers.isEmpty) {
      return const SizedBox.shrink();
    }

    return SizedBox(
      height: 220,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(16),
        child: Stack(
          children: [
            FlutterMap(
              mapController: orderGroupId != null ? _mapControllers[orderGroupId] : null,
              options: MapOptions(
                initialCenter: driverPos ?? center,
                initialZoom: 13.0,
                interactionOptions: const InteractionOptions(
                  flags: InteractiveFlag.pinchZoom | InteractiveFlag.drag,
                ),
              ),
              children: [
                TileLayer(
                  urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                  userAgentPackageName: 'com.example.online_store_mobile',
                ),
                MarkerLayer(markers: markers),
              ],
            ),
            if (driverPos != null)
              Positioned(
                top: 10,
                right: 10,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: colorScheme.primary,
                    borderRadius: BorderRadius.circular(8),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withValues(alpha: 0.2),
                        blurRadius: 4,
                      ),
                    ],
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.sensors, color: Colors.white, size: 14),
                      const SizedBox(width: 4),
                      Text(
                        'Live Tracking'.tr(),
                        style: GoogleFonts.dmSans(
                          color: Colors.white,
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildStoreStopCard(Map<String, dynamic> store, ColorScheme colorScheme) {
    final items = ((store['items'] as List?) ?? [])
        .map((item) => Map<String, dynamic>.from(item as Map))
        .toList();
    final storeName = store['storeName']?.toString() ?? store['storeUserId']?.toString() ?? 'Store'.tr();
    final storeStatus = store['status']?.toString() ?? 'Pending';

    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(14),
        color: colorScheme.surfaceContainerHighest.withValues(alpha: 0.28),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Text(
                  storeName,
                  style: GoogleFonts.dmSans(
                    fontWeight: FontWeight.w700,
                    fontSize: 14,
                    color: colorScheme.onSurface,
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: _statusColor(storeStatus).withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(999),
                ),
                child: Text(
                  storeStatus.tr(),
                  style: GoogleFonts.dmSans(
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                    color: _statusColor(storeStatus),
                  ),
                ),
              ),
            ],
          ),
          if ((store['storeLocation']?.toString() ?? '').isNotEmpty) ...[
            const SizedBox(height: 4),
            Text(
              store['storeLocation'].toString(),
              style: GoogleFonts.dmSans(
                fontSize: 12,
                color: colorScheme.onSurface.withValues(alpha: 0.58),
              ),
            ),
          ],
          if (items.isNotEmpty) ...[
            const SizedBox(height: 8),
            ...items.map(
              (item) => Padding(
                padding: const EdgeInsets.only(bottom: 4),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        '${item['productName']} x${item['quantity']}',
                        style: GoogleFonts.dmSans(
                          fontSize: 12,
                          color: colorScheme.onSurface.withValues(alpha: 0.72),
                        ),
                      ),
                    ),
                    Text(
                      '\$${(((item['unitPrice'] as num?)?.toDouble() ?? 0) * ((item['quantity'] as num?)?.toDouble() ?? 0)).toStringAsFixed(2)}',
                      style: GoogleFonts.dmSans(
                        fontSize: 12,
                        color: colorScheme.onSurface.withValues(alpha: 0.58),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _infoChip(String label) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: const Color(0xFFF4ECE8),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        label,
        style: GoogleFonts.dmSans(
          fontSize: 11,
          fontWeight: FontWeight.w700,
          color: const Color(0xFF8B3E20),
        ),
      ),
    );
  }
}
