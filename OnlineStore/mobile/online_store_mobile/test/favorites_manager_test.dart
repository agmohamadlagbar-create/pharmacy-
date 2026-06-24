import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:online_store_mobile/favorites_manager.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  test('getFavorites returns empty when storage is empty', () async {
    final favorites = await FavoritesManager.getFavorites();
    expect(favorites, isEmpty);
  });

  test('getFavorites returns empty when stored JSON is invalid', () async {
    SharedPreferences.setMockInitialValues(
        {'favorites_list': '{not-valid-json}'});

    final favorites = await FavoritesManager.getFavorites();
    expect(favorites, isEmpty);
  });

  test('toggleFavorite adds and removes item by id', () async {
    final product = {
      'id': 42,
      'name': 'Olive Oil',
      'price': 19.99,
      'imageUrl': '/uploads/oil.png',
    };

    await FavoritesManager.toggleFavorite(product);
    var favorites = await FavoritesManager.getFavorites();
    expect(favorites, hasLength(1));
    expect(favorites.first['id'], 42);
    expect(favorites.first['name'], 'Olive Oil');

    await FavoritesManager.toggleFavorite({'id': '42'});
    favorites = await FavoritesManager.getFavorites();
    expect(favorites, isEmpty);
  });

  test('isFavorite checks membership by id regardless of type', () async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(
      'favorites_list',
      jsonEncode([
        {'id': 7, 'name': 'Bread', 'price': 2.5, 'imageUrl': null}
      ]),
    );

    expect(await FavoritesManager.isFavorite(7), isTrue);
    expect(await FavoritesManager.isFavorite('7'), isTrue);
    expect(await FavoritesManager.isFavorite(9), isFalse);
  });
}
