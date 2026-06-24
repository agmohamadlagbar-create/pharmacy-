import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';

class FavoritesManager {
  static const _key = 'favorites_list';

  static Future<List<Map<String, dynamic>>> getFavorites() async {
    final prefs = await SharedPreferences.getInstance();
    final String? jsonStr = prefs.getString(_key);
    if (jsonStr == null || jsonStr.isEmpty) return [];
    
    try {
      final List<dynamic> decoded = jsonDecode(jsonStr);
      return decoded.map((e) => Map<String, dynamic>.from(e)).toList();
    } catch (_) {
      return [];
    }
  }

  static Future<void> toggleFavorite(Map<String, dynamic> product) async {
    final favorites = await getFavorites();
    final index = favorites.indexWhere((p) => p['id'].toString() == product['id'].toString());
    
    if (index >= 0) {
      favorites.removeAt(index);
    } else {
      favorites.add({
        'id': product['id'],
        'name': product['name'],
        'price': product['price'],
        'imageUrl': product['imageUrl'],
      });
    }
    
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_key, jsonEncode(favorites));
  }
  
  static Future<bool> isFavorite(dynamic productId) async {
    final favorites = await getFavorites();
    return favorites.any((p) => p['id'].toString() == productId.toString());
  }
}
