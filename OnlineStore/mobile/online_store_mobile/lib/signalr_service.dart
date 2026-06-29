import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'api.dart';

class SignalRService {
  static final SignalRService _instance = SignalRService._internal();
  factory SignalRService() => _instance;
  SignalRService._internal();

  HubConnection? _connection;
  final _statusController = StreamController<Map<String, dynamic>>.broadcast();
  final _locationController =
      StreamController<Map<String, dynamic>>.broadcast();

  Stream<Map<String, dynamic>> get statusUpdates => _statusController.stream;
  Stream<Map<String, dynamic>> get locationUpdates =>
      _locationController.stream;

  HubConnectionState get state =>
      _connection?.state ?? HubConnectionState.Disconnected;

  Future<void> connect() async {
    if (_connection != null &&
        _connection!.state != HubConnectionState.Disconnected) {
      return;
    }

    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('token');
    if (token == null) return;

    final hubUrl = '${Api.baseUrl}/hubs/tracking?access_token=$token';

    _connection =
        HubConnectionBuilder().withUrl(hubUrl).withAutomaticReconnect().build();

    _connection!.on('order:status_updated', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        _statusController.add(data);
      }
    });

    _connection!.on('location:updated', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        _locationController.add(data);
      }
    });

    try {
      await _connection!.start();
      if (kDebugMode) print('SignalR TrackingHub Connected');
    } catch (e) {
      if (kDebugMode) print('SignalR Connection Error: $e');
    }
  }

  Future<void> disconnect() async {
    await _connection?.stop();
    _connection = null;
  }

  Future<void> joinGroupedOrderGroup(String orderGroupId) async {
    // Wait if connecting
    while (_connection?.state == HubConnectionState.Connecting) {
      await Future.delayed(const Duration(milliseconds: 100));
    }

    if (_connection?.state == HubConnectionState.Connected) {
      try {
        await _connection!
            .invoke('JoinGroupedOrderGroup', args: [orderGroupId]);
        if (kDebugMode) print('Joined group: order_group_$orderGroupId');
      } catch (e) {
        if (kDebugMode) print('Error joining group: $e');
      }
    }
  }

  Future<void> leaveGroupedOrderGroup(String orderGroupId) async {
    if (_connection?.state == HubConnectionState.Connected) {
      try {
        await _connection!
            .invoke('LeaveGroupedOrderGroup', args: [orderGroupId]);
        if (kDebugMode) print('Left group: order_group_$orderGroupId');
      } catch (e) {
        if (kDebugMode) print('Error leaving group: $e');
      }
    }
  }

  Future<void> sendGroupLocation(
      String orderGroupId, double lat, double lng) async {
    if (_connection?.state == HubConnectionState.Connected) {
      await _connection!
          .invoke('SendGroupLocation', args: [orderGroupId, lat, lng]);
    }
  }
}
