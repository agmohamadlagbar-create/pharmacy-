import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class Api {
  // Android emulator localhost => 10.0.2.2, Physical device via USB => 127.0.0.1, LAN => 192.168.1.132
  // Chrome / Web => http://localhost:5000 in debug or https://lagbar.runasp.net
  static String get baseUrl {
    if (kIsWeb) {
      if (kDebugMode) {
        return 'http://localhost:5000';
      }
      return 'https://lagbar.runasp.net';
    }
    return 'https://lagbar.runasp.net';
  }

  static String formatImageUrl(String? rawUrl) {
    if (rawUrl == null || rawUrl.trim().isEmpty) return '';
    final trimmed = rawUrl.trim();
    if (trimmed.startsWith('assets/')) return trimmed;
    if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) {
      return Uri.encodeFull(trimmed);
    }
    final path = trimmed.startsWith('/') ? trimmed : '/$trimmed';
    return Uri.encodeFull('$baseUrl$path');
  }

  static bool _isRefreshing = false;

  // ── Cached SharedPreferences ──
  static SharedPreferences? _prefs;
  static Future<SharedPreferences> get _sharedPrefs async {
    _prefs ??= await SharedPreferences.getInstance();
    return _prefs!;
  }

  static Future<Map<String, dynamic>> post(
      String path, Map<String, dynamic> body) async {
    final uri = Uri.parse('$baseUrl$path');
    final prefs = await _sharedPrefs;
    final token = prefs.getString('token');

    var res = await http.post(
      uri,
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
      body: jsonEncode(body),
    );

    if (res.statusCode == 401 &&
        !path.contains('/auth/refresh') &&
        !path.contains('/auth/login')) {
      final refreshed = await _tryRefresh();
      if (refreshed) {
        final newToken = (await _sharedPrefs).getString('token');
        res = await http.post(
          uri,
          headers: {
            'Content-Type': 'application/json',
            if (newToken != null) 'Authorization': 'Bearer $newToken',
          },
          body: jsonEncode(body),
        );
      }
    }

    return _processResponse(res);
  }

  static Future<Map<String, dynamic>> get(String path) async {
    final uri = Uri.parse('$baseUrl$path');
    final prefs = await _sharedPrefs;
    final token = prefs.getString('token');

    var res = await http.get(
      uri,
      headers: {
        if (token != null) 'Authorization': 'Bearer $token',
      },
    );

    if (res.statusCode == 401) {
      final refreshed = await _tryRefresh();
      if (refreshed) {
        final newToken = (await _sharedPrefs).getString('token');
        res = await http.get(
          uri,
          headers: {
            if (newToken != null) 'Authorization': 'Bearer $newToken',
          },
        );
      }
    }

    return _processResponse(res);
  }

  static Future<Map<String, dynamic>> put(String path, dynamic body) async {
    final uri = Uri.parse('$baseUrl$path');
    final prefs = await _sharedPrefs;
    final token = prefs.getString('token');

    var res = await http.put(
      uri,
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
      body: jsonEncode(body),
    );

    if (res.statusCode == 401) {
      final refreshed = await _tryRefresh();
      if (refreshed) {
        final newToken = (await _sharedPrefs).getString('token');
        res = await http.put(
          uri,
          headers: {
            'Content-Type': 'application/json',
            if (newToken != null) 'Authorization': 'Bearer $newToken',
          },
          body: jsonEncode(body),
        );
      }
    }

    return _processResponse(res);
  }

  static Future<Map<String, dynamic>> delete(String path) async {
    final uri = Uri.parse('$baseUrl$path');
    final prefs = await _sharedPrefs;
    final token = prefs.getString('token');

    var res = await http.delete(
      uri,
      headers: {
        if (token != null) 'Authorization': 'Bearer $token',
      },
    );

    if (res.statusCode == 401) {
      final refreshed = await _tryRefresh();
      if (refreshed) {
        final newToken = (await _sharedPrefs).getString('token');
        res = await http.delete(
          uri,
          headers: {
            if (newToken != null) 'Authorization': 'Bearer $newToken',
          },
        );
      }
    }

    return _processResponse(res);
  }

  // ── Cart ──
  static Future<Map<String, dynamic>> getCart() => get('/api/cart');
  static Future<Map<String, dynamic>> addToCart(int productId, int quantity) =>
      post('/api/cart/items', {'productId': productId, 'quantity': quantity});
  static Future<Map<String, dynamic>> updateCartQuantity(
          int productId, int quantity) =>
      put('/api/cart/items/$productId', quantity);
  static Future<Map<String, dynamic>> removeFromCart(int productId) =>
      delete('/api/cart/items/$productId');
  static Future<Map<String, dynamic>> clearCart() => delete('/api/cart');

  /// Download binary data (e.g. PDF/Excel reports) with authentication.
  static Future<List<int>> getBytes(String path) async {
    final uri = Uri.parse('$baseUrl$path');
    final prefs = await _sharedPrefs;
    final token = prefs.getString('token');

    var res = await http.get(
      uri,
      headers: {
        if (token != null) 'Authorization': 'Bearer $token',
      },
    );

    if (res.statusCode == 401) {
      final refreshed = await _tryRefresh();
      if (refreshed) {
        final newToken = (await _sharedPrefs).getString('token');
        res = await http.get(
          uri,
          headers: {
            if (newToken != null) 'Authorization': 'Bearer $newToken',
          },
        );
      }
    }

    if (res.statusCode >= 200 && res.statusCode < 300) {
      return res.bodyBytes;
    }
    throw Exception('Download failed (${res.statusCode})');
  }

  static Future<Map<String, dynamic>> upload(String path, File file) async {
    final uri = Uri.parse('$baseUrl$path');
    final prefs = await _sharedPrefs;
    final token = prefs.getString('token');

    final request = http.MultipartRequest('POST', uri);

    if (token != null) {
      request.headers['Authorization'] = 'Bearer $token';
      if (kDebugMode) {
        print('Uploading image with token: ${token.substring(0, 10)}...');
      }
    } else {
      if (kDebugMode) print('No token found in SharedPreferences for upload!');
    }

    request.files.add(await http.MultipartFile.fromPath('file', file.path));

    final streamedRes = await request.send();
    final res = await http.Response.fromStream(streamedRes);

    return _processResponse(res);
  }

  /// Try to refresh the access token using the stored refresh token.
  static Future<bool> _tryRefresh() async {
    if (_isRefreshing) return false;
    _isRefreshing = true;

    try {
      final prefs = await _sharedPrefs;
      final accessToken = prefs.getString('token');
      final refreshToken = prefs.getString('refreshToken');

      if (accessToken == null || refreshToken == null) return false;

      final uri = Uri.parse('$baseUrl/api/auth/refresh');
      final res = await http.post(
        uri,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'accessToken': accessToken,
          'refreshToken': refreshToken,
        }),
      );

      if (res.statusCode >= 200 && res.statusCode < 300) {
        final data = jsonDecode(res.body);
        await prefs.setString('token', data['token'] ?? '');
        await prefs.setString('refreshToken', data['refreshToken'] ?? '');
        if (data['accountType'] != null) {
          await prefs.setString('accountType', data['accountType']);
        }
        if (kDebugMode) print('Token refreshed successfully');
        return true;
      }

      // Refresh failed – clear tokens
      await prefs.remove('token');
      await prefs.remove('refreshToken');
      await prefs.remove('accountType');
      return false;
    } catch (e) {
      if (kDebugMode) print('Token refresh failed: $e');
      return false;
    } finally {
      _isRefreshing = false;
    }
  }

  static Map<String, dynamic> _processResponse(http.Response res) {
    final text = res.body.isNotEmpty ? res.body : '{}';
    final decoded = jsonDecode(text);

    if (res.statusCode >= 200 && res.statusCode < 300) {
      return decoded is Map<String, dynamic> ? decoded : {'data': decoded};
    }

    // normalize error message
    String message = 'Request failed (${res.statusCode})';
    if (decoded is Map<String, dynamic>) {
      if (decoded['error'] != null) message = decoded['error'].toString();
      if (decoded['errors'] is List) {
        message = (decoded['errors'] as List).join('\n');
      }
    }
    throw Exception(message);
  }
}
