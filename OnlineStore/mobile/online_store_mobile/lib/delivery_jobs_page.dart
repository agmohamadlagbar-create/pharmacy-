import 'package:flutter/material.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:geolocator/geolocator.dart';
import 'api.dart';
import 'signalr_service.dart';
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
      final path = _showMine ? '/api/delivery/my-jobs' : '/api/delivery/available';
      final res = await Api.get(path);
      final data = res is List ? res : (res['data'] is List ? res['data'] : []);

      if (!mounted) return;
      setState(() {
        _jobs = (data as List).map((e) => Map<String, dynamic>.from(e)).toList();
        _loading = false;
      });

      // Manage location updates for active jobs
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

  Color _statusColor(String? status, ColorScheme scheme) {
    switch (status) {
      case 'Assigned':
        return scheme.primary;
      case 'PickedUp':
        return Colors.deepPurple;
      case 'OnTheWay':
        return Colors.cyan.shade700;
      case 'Delivered':
        return Colors.green.shade700;
      default:
        return Colors.orange.shade700;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: Text('Delivery Jobs'.tr()),
        actions: [
          IconButton(
            onPressed: _load,
            icon: const Icon(Icons.refresh_rounded),
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
            child: SegmentedButton<bool>(
              segments: [
                ButtonSegment(value: false, label: Text('Available'.tr())),
                ButtonSegment(value: true, label: Text('My Jobs'.tr())),
              ],
              selected: {_showMine},
              onSelectionChanged: (value) {
                setState(() => _showMine = value.first);
                _load();
              },
            ),
          ),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : _jobs.isEmpty
                    ? Center(
                        child: Text(
                          _showMine ? 'No assigned grouped jobs.'.tr() : 'No grouped deliveries available.'.tr(),
                          style: TextStyle(color: scheme.onSurface.withValues(alpha: 0.6)),
                        ),
                      )
                    : RefreshIndicator(
                        onRefresh: _load,
                        child: ListView.builder(
                          padding: const EdgeInsets.all(16),
                          itemCount: _jobs.length,
                          itemBuilder: (context, index) {
                            final job = _jobs[index];
                            final group = Map<String, dynamic>.from(
                              (job['orderGroup'] as Map?)?.cast<String, dynamic>() ?? {},
                            );
                            final stores = ((group['stores'] as List?) ?? [])
                                .map((e) => Map<String, dynamic>.from(e))
                                .toList();
                            final nextStatus = _nextStatus(job['status']?.toString());

                            return Card(
                              margin: const EdgeInsets.only(bottom: 12),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                              child: Padding(
                                padding: const EdgeInsets.all(16),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Row(
                                      children: [
                                        Expanded(
                                          child: Text(
                                            '${'Group'.tr()} #${job['orderGroupId'] ?? group['orderGroupId'] ?? job['orderId']}',
                                            style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                                          ),
                                        ),
                                        Container(
                                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                                          decoration: BoxDecoration(
                                            color: _statusColor(job['status']?.toString(), scheme).withValues(alpha: 0.12),
                                            borderRadius: BorderRadius.circular(999),
                                          ),
                                          child: Text(
                                            (job['status']?.toString() ?? '').tr(),
                                            style: TextStyle(
                                              color: _statusColor(job['status']?.toString(), scheme),
                                              fontWeight: FontWeight.w700,
                                              fontSize: 11,
                                            ),
                                          ),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 8),
                                    Text(
                                      '${stores.length} ${'pickup stops'.tr()}',
                                      style: TextStyle(color: scheme.onSurface.withValues(alpha: 0.7)),
                                    ),
                                    if (group['deliveryAddress'] != null) ...[
                                      const SizedBox(height: 4),
                                      Text(
                                        '${'Customer:'.tr()} ${group['deliveryAddress']}',
                                        style: TextStyle(color: scheme.onSurface.withValues(alpha: 0.7)),
                                      ),
                                    ],
                                    if (group['totalAmount'] != null) ...[
                                      const SizedBox(height: 4),
                                      Text(
                                        '${'Total:'.tr()} \$${group['totalAmount']}',
                                        style: TextStyle(color: scheme.onSurface.withValues(alpha: 0.7)),
                                      ),
                                    ],
                                    const SizedBox(height: 12),
                                    ...stores.map((store) => Container(
                                          margin: const EdgeInsets.only(bottom: 8),
                                          padding: const EdgeInsets.all(12),
                                          decoration: BoxDecoration(
                                            color: scheme.surfaceContainerHighest.withValues(alpha: 0.35),
                                            borderRadius: BorderRadius.circular(12),
                                          ),
                                          child: Row(
                                            children: [
                                              Expanded(
                                                child: Column(
                                                  crossAxisAlignment: CrossAxisAlignment.start,
                                                  children: [
                                                    Text(
                                                      store['storeName']?.toString() ?? store['storeUserId']?.toString() ?? 'Store'.tr(),
                                                      style: const TextStyle(fontWeight: FontWeight.w600),
                                                    ),
                                                    const SizedBox(height: 2),
                                                    Text(
                                                      store['storeLocation']?.toString() ?? 'Location not set'.tr(),
                                                      style: TextStyle(
                                                        fontSize: 12,
                                                        color: scheme.onSurface.withValues(alpha: 0.65),
                                                      ),
                                                    ),
                                                  ],
                                                ),
                                              ),
                                              Text(
                                                (store['status']?.toString() ?? '').tr(),
                                                style: TextStyle(
                                                  fontSize: 12,
                                                  color: scheme.onSurface.withValues(alpha: 0.6),
                                                ),
                                              ),
                                            ],
                                          ),
                                        )),
                                    const SizedBox(height: 4),
                                    Row(
                                      mainAxisAlignment: MainAxisAlignment.end,
                                      children: [
                                        if (!_showMine && job['status'] == 'Unassigned')
                                          FilledButton(
                                            onPressed: () => _acceptJob(job['id'] as int),
                                            child: Text('Accept Job'.tr()),
                                          ),
                                        if (_showMine && nextStatus != null)
                                          FilledButton.tonal(
                                            onPressed: () => _updateStatus(job['id'] as int, nextStatus),
                                            child: Text('${'Mark as'.tr()} ${nextStatus.tr()}'),
                                          ),
                                      ],
                                    ),
                                  ],
                                ),
                              ),
                            );
                          },
                        ),
                      ),
          ),
        ],
      ),
    );
  }
}
