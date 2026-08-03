import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:geolocator/geolocator.dart';
import 'api.dart';
import 'signalr_service.dart';
import 'widgets/stitch_bottom_navigation_bar.dart';
import 'dart:async';

class DeliveryJobsPage extends StatefulWidget {
  const DeliveryJobsPage({super.key});

  @override
  State<DeliveryJobsPage> createState() => _DeliveryJobsPageState();
}

class _DeliveryJobsPageState extends State<DeliveryJobsPage> {
  List<Map<String, dynamic>> _jobs = [];
  bool _loading = true;
  bool _showMine = false;
  final SignalRService _signalRService = SignalRService();
  StreamSubscription<Position>? _positionSubscription;

  @override
  void initState() {
    super.initState();
    _signalRService.connect();
    _load();
  }

  @override
  void dispose() {
    _stopLocationUpdates();
    super.dispose();
  }

  Future<void> _startLocationUpdates(String orderGroupId) async {
    await _stopLocationUpdates();

    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) return;

    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) return;
    }

    if (permission == LocationPermission.deniedForever) return;

    _positionSubscription = Geolocator.getPositionStream(
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: 5,
      ),
    ).listen((Position position) {
      _signalRService.sendGroupLocation(
        orderGroupId,
        position.latitude,
        position.longitude,
      );
    });
  }

  Future<void> _stopLocationUpdates() async {
    await _positionSubscription?.cancel();
    _positionSubscription = null;
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final path =
          _showMine ? '/api/delivery/my-jobs' : '/api/delivery/available';
      final res = await Api.get(path);
      final data =
          res is List ? res : (res['data'] is List ? res['data'] : []);

      if (!mounted) return;
      setState(() {
        _jobs =
            (data as List).map((e) => Map<String, dynamic>.from(e)).toList();
        _loading = false;
      });

      final activeJob = _jobs.firstWhere(
        (j) =>
            j['status'] == 'Assigned' ||
            j['status'] == 'PickedUp' ||
            j['status'] == 'OnTheWay',
        orElse: () => {},
      );

      if (activeJob.isNotEmpty) {
        final groupId = activeJob['orderGroupId']?.toString() ??
            activeJob['orderId']?.toString();
        if (groupId != null) {
          _startLocationUpdates(groupId);
        }
      } else {
        _stopLocationUpdates();
      }
    } catch (e) {
      if (!mounted) return;
      setState(() => _loading = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('${'Failed to load jobs:'.tr()} $e')),
      );
    }
  }

  Future<void> _acceptJob(int id) async {
    try {
      await Api.post('/api/delivery/$id/accept', {});
      if (!mounted) return;
      setState(() => _showMine = true);
      await _load();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('${'Accept failed:'.tr()} $e')),
      );
    }
  }

  Future<void> _updateStatus(int id, String status) async {
    try {
      await Api.put('/api/delivery/$id/status', {'status': status});
      await _load();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('${'Status update failed:'.tr()} $e')),
      );
    }
  }

  String? _nextStatus(String? status) {
    switch (status) {
      case 'Assigned':
        return 'PickedUp';
      case 'PickedUp':
        return 'OnTheWay';
      case 'OnTheWay':
        return 'Delivered';
      default:
        return null;
    }
  }

  Color _statusColor(String? status) {
    switch (status) {
      case 'Assigned':
        return const Color(0xFF3B82F6);
      case 'PickedUp':
        return const Color(0xFF8B5CF6);
      case 'OnTheWay':
        return const Color(0xFF06B6D4);
      case 'Delivered':
        return const Color(0xFF22C55E);
      default:
        return const Color(0xFFF59E0B);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      appBar: AppBar(
        backgroundColor: colorScheme.surface,
        elevation: 0,
        scrolledUnderElevation: 1,
        title: Text(
          'Delivery Dashboard'.tr(),
          style: GoogleFonts.inter(
              fontWeight: FontWeight.bold, color: colorScheme.onSurface),
        ),
        actions: [
          IconButton(
            onPressed: _load,
            icon: Icon(Icons.refresh_rounded, color: colorScheme.primary),
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
            child: SegmentedButton<bool>(
              segments: [
                ButtonSegment(
                  value: false,
                  label: Text('Available Jobs'.tr(),
                      style: GoogleFonts.inter(fontWeight: FontWeight.bold)),
                  icon: const Icon(Icons.local_shipping_outlined),
                ),
                ButtonSegment(
                  value: true,
                  label: Text('My Active Jobs'.tr(),
                      style: GoogleFonts.inter(fontWeight: FontWeight.bold)),
                  icon: const Icon(Icons.assignment_outlined),
                ),
              ],
              selected: {_showMine},
              onSelectionChanged: (value) {
                setState(() => _showMine = value.first);
                _load();
              },
              showSelectedIcon: false,
            ),
          ),
          Expanded(
            child: _loading
                ? Center(
                    child: CircularProgressIndicator(color: colorScheme.primary))
                : _jobs.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Container(
                              width: 80,
                              height: 80,
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                color: colorScheme.primary
                                    .withValues(alpha: 0.08),
                              ),
                              child: Icon(Icons.delivery_dining_rounded,
                                  size: 40,
                                  color: colorScheme.primary
                                      .withValues(alpha: 0.5)),
                            ),
                            const SizedBox(height: 16),
                            Text(
                              _showMine
                                  ? 'No active jobs assigned'.tr()
                                  : 'No available delivery jobs'.tr(),
                              style: GoogleFonts.inter(
                                  fontSize: 18,
                                  fontWeight: FontWeight.bold,
                                  color: colorScheme.onSurface),
                            ),
                          ],
                        ),
                      )
                    : RefreshIndicator(
                        onRefresh: _load,
                        child: ListView.separated(
                          physics: const BouncingScrollPhysics(),
                          padding: const EdgeInsets.all(16),
                          itemCount: _jobs.length,
                          separatorBuilder: (_, __) =>
                              const SizedBox(height: 14),
                          itemBuilder: (context, index) {
                            final job = _jobs[index];
                            final group = Map<String, dynamic>.from(
                              (job['orderGroup'] as Map?)
                                      ?.cast<String, dynamic>() ??
                                  {},
                            );
                            final stores = ((group['stores'] as List?) ?? [])
                                .map((e) => Map<String, dynamic>.from(e))
                                .toList();
                            final nextStatus =
                                _nextStatus(job['status']?.toString());
                            final statusColor =
                                _statusColor(job['status']?.toString());

                            return Container(
                              padding: const EdgeInsets.all(16),
                              decoration: BoxDecoration(
                                color: colorScheme.surface,
                                borderRadius: BorderRadius.circular(18),
                                border: Border.all(
                                  color: isDark
                                      ? Colors.white.withValues(alpha: 0.1)
                                      : colorScheme.outline
                                          .withValues(alpha: 0.5),
                                ),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    children: [
                                      Expanded(
                                        child: Text(
                                          'Job #${job['orderGroupId'] ?? group['orderGroupId'] ?? job['orderId']}',
                                          style: GoogleFonts.inter(
                                            fontSize: 16,
                                            fontWeight: FontWeight.bold,
                                            color: colorScheme.onSurface,
                                          ),
                                        ),
                                      ),
                                      Container(
                                        padding: const EdgeInsets.symmetric(
                                            horizontal: 10, vertical: 4),
                                        decoration: BoxDecoration(
                                          color: statusColor.withValues(
                                              alpha: 0.12),
                                          borderRadius:
                                              BorderRadius.circular(12),
                                        ),
                                        child: Text(
                                          (job['status']?.toString() ?? '')
                                              .tr(),
                                          style: GoogleFonts.inter(
                                            color: statusColor,
                                            fontWeight: FontWeight.bold,
                                            fontSize: 12,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 10),
                                  Row(
                                    children: [
                                      Icon(Icons.storefront_rounded,
                                          size: 16,
                                          color: colorScheme.primary),
                                      const SizedBox(width: 6),
                                      Text(
                                        '${stores.length} ${'pickup stops'.tr()}',
                                        style: GoogleFonts.inter(
                                          fontSize: 13,
                                          fontWeight: FontWeight.w600,
                                          color: colorScheme.onSurface,
                                        ),
                                      ),
                                    ],
                                  ),
                                  if (group['deliveryAddress'] != null) ...[
                                    const SizedBox(height: 6),
                                    Row(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.start,
                                      children: [
                                        Icon(Icons.location_on_outlined,
                                            size: 16,
                                            color: colorScheme.primary),
                                        const SizedBox(width: 6),
                                        Expanded(
                                          child: Text(
                                            group['deliveryAddress'].toString(),
                                            style: GoogleFonts.inter(
                                              fontSize: 13,
                                              color:
                                                  colorScheme.onSurfaceVariant,
                                            ),
                                          ),
                                        ),
                                      ],
                                    ),
                                  ],
                                  const SizedBox(height: 12),
                                  ...stores.map((store) => Container(
                                        margin: const EdgeInsets.only(
                                            bottom: 8),
                                        padding: const EdgeInsets.all(12),
                                        decoration: BoxDecoration(
                                          color: colorScheme.primary
                                              .withValues(alpha: 0.05),
                                          borderRadius:
                                              BorderRadius.circular(12),
                                          border: Border.all(
                                            color: colorScheme.primary
                                                .withValues(alpha: 0.15),
                                          ),
                                        ),
                                        child: Row(
                                          children: [
                                            Expanded(
                                              child: Column(
                                                crossAxisAlignment:
                                                    CrossAxisAlignment.start,
                                                children: [
                                                  Text(
                                                    store['storeName']
                                                            ?.toString() ??
                                                        store['storeUserId']
                                                            ?.toString() ??
                                                        'Store'.tr(),
                                                    style: GoogleFonts.inter(
                                                        fontWeight:
                                                            FontWeight.bold,
                                                        color: colorScheme
                                                            .onSurface,
                                                        fontSize: 14),
                                                  ),
                                                  if (store['storeLocation'] !=
                                                      null) ...[
                                                    const SizedBox(height: 2),
                                                    Text(
                                                      store['storeLocation']
                                                          .toString(),
                                                      style: GoogleFonts.inter(
                                                        fontSize: 12,
                                                        color: colorScheme
                                                            .onSurfaceVariant,
                                                      ),
                                                    ),
                                                  ],
                                                ],
                                              ),
                                            ),
                                            Container(
                                              padding:
                                                  const EdgeInsets.symmetric(
                                                      horizontal: 8,
                                                      vertical: 4),
                                              decoration: BoxDecoration(
                                                color: colorScheme.surface,
                                                borderRadius:
                                                    BorderRadius.circular(8),
                                              ),
                                              child: Text(
                                                (store['status']?.toString() ??
                                                        '')
                                                    .tr(),
                                                style: GoogleFonts.inter(
                                                  fontSize: 11,
                                                  fontWeight: FontWeight.bold,
                                                  color: colorScheme.primary,
                                                ),
                                              ),
                                            ),
                                          ],
                                        ),
                                      )),
                                  const SizedBox(height: 8),
                                  Row(
                                    mainAxisAlignment: MainAxisAlignment.end,
                                    children: [
                                      if (!_showMine &&
                                          job['status'] == 'Unassigned')
                                        ElevatedButton.icon(
                                          style: ElevatedButton.styleFrom(
                                            backgroundColor:
                                                const Color(0xFFF59E0B),
                                            foregroundColor: Colors.white,
                                            padding: const EdgeInsets.symmetric(
                                                horizontal: 20, vertical: 12),
                                            shape: RoundedRectangleBorder(
                                              borderRadius:
                                                  BorderRadius.circular(12),
                                            ),
                                          ),
                                          onPressed: () =>
                                              _acceptJob(job['id'] as int),
                                          icon: const Icon(
                                              Icons.check_circle_outline,
                                              size: 18),
                                          label: Text('Accept Job'.tr(),
                                              style: GoogleFonts.inter(
                                                  fontWeight:
                                                      FontWeight.bold)),
                                        ),
                                      if (_showMine && nextStatus != null)
                                        ElevatedButton.icon(
                                          style: ElevatedButton.styleFrom(
                                            backgroundColor:
                                                colorScheme.primary,
                                            foregroundColor: Colors.white,
                                            padding: const EdgeInsets.symmetric(
                                                horizontal: 20, vertical: 12),
                                            shape: RoundedRectangleBorder(
                                              borderRadius:
                                                  BorderRadius.circular(12),
                                            ),
                                          ),
                                          onPressed: () => _updateStatus(
                                              job['id'] as int, nextStatus),
                                          icon: const Icon(
                                              Icons.navigation_outlined,
                                              size: 18),
                                          label: Text(
                                              '${'Mark as'.tr()} ${nextStatus.tr()}',
                                              style: GoogleFonts.inter(
                                                  fontWeight:
                                                      FontWeight.bold)),
                                        ),
                                    ],
                                  ),
                                ],
                              ),
                            );
                          },
                        ),
                      ),
          ),
        ],
      ),
      bottomNavigationBar: StitchBottomNavigationBar(
        currentIndex: 1,
        isDelivery: true,
        onTap: (index) {
          if (index == 1) return;
          if (index == 0) {
            Navigator.pushReplacementNamed(context, '/categories');
          } else if (index == 2) {
            Navigator.pushReplacementNamed(context, '/notifications');
          } else if (index == 3) {
            Navigator.pushReplacementNamed(context, '/settings');
          } else if (index == 4) {
            Navigator.pushReplacementNamed(context, '/profile');
          }
        },
      ),
    );
  }
}
