import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import 'api.dart';

class PushNotificationService {
  static final PushNotificationService _instance = PushNotificationService._internal();
  factory PushNotificationService() => _instance;
  PushNotificationService._internal();

  String? _deviceToken;

  Future<void> initializeAndRegisterToken() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString('token');
      if (token == null) return;

      // In production with firebase_messaging, fetch FirebaseMessaging.instance.getToken()
      // For cross-platform dev/fallback, maintain persistent device token
      _deviceToken = prefs.getString('fcm_device_token');
      if (_deviceToken == null) {
        _deviceToken = 'mobile_fcm_token_${DateTime.now().millisecondsSinceEpoch}';
        await prefs.setString('fcm_device_token', _deviceToken!);
      }

      await registerTokenWithBackend(_deviceToken!);
    } catch (e) {
      if (kDebugMode) {
        print('Error initializing mobile push notifications: $e');
      }
    }
  }

  Future<void> registerTokenWithBackend(String deviceToken) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString('token');
      if (token == null) return;

      final platform = defaultTargetPlatform == TargetPlatform.iOS ? 'iOS' : 'Android';

      final response = await http.post(
        Uri.parse('${Api.baseUrl}/api/devicetokens'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode({
          'token': deviceToken,
          'platform': platform,
        }),
      );

      if (response.statusCode == 200) {
        if (kDebugMode) {
          print('Successfully registered mobile device token with backend API.');
        }
      } else {
        if (kDebugMode) {
          print('Failed to register mobile device token: ${response.statusCode} - ${response.body}');
        }
      }
    } catch (e) {
      if (kDebugMode) {
        print('Error registering device token with backend: $e');
      }
    }
  }

  Future<void> unregisterTokenFromBackend() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString('token');
      final deviceToken = prefs.getString('fcm_device_token');
      if (token == null || deviceToken == null) return;

      final response = await http.delete(
        Uri.parse('${Api.baseUrl}/api/devicetokens/${Uri.encodeComponent(deviceToken)}'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        if (kDebugMode) {
          print('Unregistered mobile device token from backend.');
        }
      }
    } catch (e) {
      if (kDebugMode) {
        print('Error unregistering mobile device token: $e');
      }
    }
  }
}
