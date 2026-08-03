import 'dart:async';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'api.dart';
import 'product_info_page.dart';
import 'favorites_manager.dart';
import 'widgets/stitch_product_card.dart';
import 'widgets/stitch_bottom_navigation_bar.dart';
import 'utils/animation_utils.dart';

class SearchProductsPage extends StatefulWidget {
  const SearchProductsPage({super.key});

  @override
  State<SearchProductsPage> createState() => _SearchProductsPageState();
}

class _SearchProductsPageState extends State<SearchProductsPage> {
  final TextEditingController _searchCtrl = TextEditingController();
  final TextEditingController _minPriceCtrl = TextEditingController();
  final TextEditingController _maxPriceCtrl = TextEditingController();

  String? _selectedCategoryId;
  List<Map<String, dynamic>> _categories = [];

  List<Map<String, dynamic>> _products = [];
  bool _loading = false;
  String? _error;

  Timer? _debounce;
  Map<String, dynamic>? _userLocation;
  Set<String> _favoriteProductIds = {};

  @override
  void initState() {
    super.initState();
    _loadFavorites();
    _fetchCategories();
    _fetchProfileLocation().then((_) {
      _fetchProducts();
    });
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _minPriceCtrl.dispose();
    _maxPriceCtrl.dispose();
    _debounce?.cancel();
    super.dispose();
  }

  Future<void> _loadFavorites() async {
    final favs = await FavoritesManager.getFavorites();
    if (mounted) {
      setState(() {
        _favoriteProductIds = favs.map((e) => e['id'].toString()).toSet();
      });
    }
  }

  Future<void> _fetchProfileLocation() async {
    final prefs = await SharedPreferences.getInstance();
    if (prefs.getBool('isGuest') == true) return;
    try {
      final res = await Api.get('/api/me');
      if (res['city'] != null) {
        _userLocation = {'city': res['city'], 'country': res['country']};
      }
    } catch (_) {}
  }

  Future<void> _fetchCategories() async {
    try {
      final res = await Api.get('/api/categories');
      final data = res['data'];
      if (mounted && data is List) {
        setState(() {
          _categories = data.map((e) => Map<String, dynamic>.from(e)).toList();
        });
      }
    } catch (_) {}
  }

  Future<void> _fetchProducts() async {
    if (!mounted) return;
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      String url = '/api/products/approved?limit=100';

      final query = _searchCtrl.text.trim();
      if (query.isNotEmpty) url += '&search=${Uri.encodeComponent(query)}';

      final minP = _minPriceCtrl.text.trim();
      if (minP.isNotEmpty) url += '&minPrice=$minP';

      final maxP = _maxPriceCtrl.text.trim();
      if (maxP.isNotEmpty) url += '&maxPrice=$maxP';

      if (_selectedCategoryId != null && _selectedCategoryId!.isNotEmpty) {
        url += '&categoryId=$_selectedCategoryId';
      }

      if (_userLocation != null) {
        if (_userLocation!['city'] != null) {
          url += '&city=${Uri.encodeComponent(_userLocation!['city'])}';
        }
        if (_userLocation!['country'] != null) {
          url += '&country=${Uri.encodeComponent(_userLocation!['country'])}';
        }
      }

      final res = await Api.get(url);
      final items = res['data'] ?? res;
      if (mounted) {
        setState(() {
          if (items is List) {
            _products = items.map((p) => Map<String, dynamic>.from(p)).toList();
          } else {
            _products = [];
          }
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = 'Failed to load products. Please try again.'.tr();
          _loading = false;
        });
      }
    }
  }

  void _onSearchChanged(String value) {
    if (_debounce?.isActive ?? false) _debounce!.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), () {
      _fetchProducts();
    });
  }

  void _resetFilters() {
    setState(() {
      _minPriceCtrl.clear();
      _maxPriceCtrl.clear();
      _selectedCategoryId = null;
    });
    _fetchProducts();
  }

  Future<void> _toggleFavorite(Map<String, dynamic> product) async {
    await FavoritesManager.toggleFavorite(product);
    await _loadFavorites();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      appBar: AppBar(
        backgroundColor: colorScheme.surface,
        elevation: 0,
        scrolledUnderElevation: 1,
        titleSpacing: 0,
        title: Container(
          height: 40,
          margin: const EdgeInsets.only(right: 16),
          decoration: BoxDecoration(
            color: colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
            borderRadius: BorderRadius.circular(20),
          ),
          child: TextField(
            controller: _searchCtrl,
            onChanged: _onSearchChanged,
            autofocus: true,
            style: GoogleFonts.inter(color: colorScheme.onSurface, fontSize: 14),
            decoration: InputDecoration(
              hintText: 'Search products...'.tr(),
              hintStyle: GoogleFonts.inter(
                  color: colorScheme.onSurfaceVariant, fontSize: 14),
              border: InputBorder.none,
              contentPadding: const EdgeInsets.symmetric(vertical: 10),
              prefixIcon: Icon(Icons.search_rounded,
                  size: 20, color: colorScheme.onSurfaceVariant),
              suffixIcon: _searchCtrl.text.isNotEmpty
                  ? IconButton(
                      icon: Icon(Icons.clear_rounded,
                          size: 18, color: colorScheme.onSurfaceVariant),
                      onPressed: () {
                        _searchCtrl.clear();
                        _onSearchChanged('');
                      },
                    )
                  : null,
            ),
          ),
        ),
      ),
      body: Column(
        children: [
          // Filter Bar
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              color: colorScheme.surface,
              border: Border(
                  bottom: BorderSide(color: colorScheme.outline, width: 1)),
            ),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              physics: const BouncingScrollPhysics(),
              child: Row(
                children: [
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                    decoration: BoxDecoration(
                      color: colorScheme.primary.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Row(
                      children: [
                        Icon(Icons.tune_rounded,
                            size: 16, color: colorScheme.primary),
                        const SizedBox(width: 8),
                        Text('Filters'.tr(),
                            style: GoogleFonts.inter(
                                color: colorScheme.primary,
                                fontWeight: FontWeight.bold,
                                fontSize: 13)),
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),

                  // Category Selector Dropdown
                  Container(
                    height: 38,
                    padding: const EdgeInsets.symmetric(horizontal: 12),
                    decoration: BoxDecoration(
                      color: colorScheme.surfaceContainerHighest,
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: colorScheme.outline, width: 1),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButton<String>(
                        value: _selectedCategoryId,
                        dropdownColor: colorScheme.surface,
                        style: GoogleFonts.inter(
                            color: colorScheme.onSurface, fontSize: 13),
                        hint: Text('All Categories'.tr(),
                            style: GoogleFonts.inter(
                                color: colorScheme.onSurfaceVariant, fontSize: 13)),
                        items: [
                          DropdownMenuItem(
                              value: null, child: Text('All Categories'.tr())),
                          ..._categories.map((c) => DropdownMenuItem(
                                value: c['id'].toString(),
                                child: Text(c['name']),
                              ))
                        ],
                        onChanged: (val) {
                          setState(() => _selectedCategoryId = val);
                          _fetchProducts();
                        },
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),

                  // Min Price Field
                  SizedBox(
                    width: 90,
                    height: 38,
                    child: TextField(
                      controller: _minPriceCtrl,
                      keyboardType: TextInputType.number,
                      style: GoogleFonts.inter(
                          fontSize: 13, color: colorScheme.onSurface),
                      decoration: InputDecoration(
                        hintText: 'Min \$'.tr(),
                        contentPadding: const EdgeInsets.symmetric(
                            horizontal: 10, vertical: 8),
                        border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8)),
                      ),
                      onChanged: _onSearchChanged,
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 8.0),
                    child: Text('—',
                        style: TextStyle(color: colorScheme.onSurfaceVariant)),
                  ),
                  // Max Price Field
                  SizedBox(
                    width: 90,
                    height: 38,
                    child: TextField(
                      controller: _maxPriceCtrl,
                      keyboardType: TextInputType.number,
                      style: GoogleFonts.inter(
                          fontSize: 13, color: colorScheme.onSurface),
                      decoration: InputDecoration(
                        hintText: 'Max \$'.tr(),
                        contentPadding: const EdgeInsets.symmetric(
                            horizontal: 10, vertical: 8),
                        border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8)),
                      ),
                      onChanged: _onSearchChanged,
                    ),
                  ),

                  if (_selectedCategoryId != null ||
                      _minPriceCtrl.text.isNotEmpty ||
                      _maxPriceCtrl.text.isNotEmpty) ...[
                    const SizedBox(width: 12),
                    TextButton(
                      onPressed: _resetFilters,
                      child: Text('Reset'.tr()),
                    )
                  ]
                ],
              ),
            ),
          ),

          // Search Results Grid
          Expanded(
            child: _loading
                ? Center(
                    child: CircularProgressIndicator(color: colorScheme.primary))
                : _error != null
                    ? Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.error_outline_rounded,
                                size: 48, color: colorScheme.error),
                            const SizedBox(height: 12),
                            Text(_error!,
                                style: GoogleFonts.inter(
                                    color: colorScheme.onSurfaceVariant)),
                            const SizedBox(height: 16),
                            TextButton(
                              onPressed: _fetchProducts,
                              child: Text('Retry'.tr()),
                            )
                          ],
                        ),
                      )
                    : _products.isEmpty
                        ? Center(
                            child: Text(
                              'No products found matching your search.'.tr(),
                              style: GoogleFonts.inter(
                                  color: colorScheme.onSurfaceVariant),
                            ),
                          )
                        : GridView.builder(
                            padding: const EdgeInsets.all(16),
                            physics: const BouncingScrollPhysics(),
                            gridDelegate:
                                const SliverGridDelegateWithFixedCrossAxisCount(
                              crossAxisCount: 2,
                              crossAxisSpacing: 16,
                              mainAxisSpacing: 16,
                              childAspectRatio: 0.75,
                            ),
                            itemCount: _products.length,
                            itemBuilder: (context, index) {
                              final product = _products[index];
                              final String productId = product['id'].toString();
                              final bool isFav =
                                  _favoriteProductIds.contains(productId);

                              return StitchProductCard(
                                product: product,
                                isFavorite: isFav,
                                onTap: () {
                                  final pId = int.tryParse(productId) ?? 0;
                                  if (pId > 0) {
                                    Navigator.push(
                                      context,
                                      MaterialPageRoute(
                                        builder: (_) =>
                                            ProductInfoPage(productId: pId),
                                      ),
                                    );
                                  }
                                },
                                onFavoriteToggle: () => _toggleFavorite(product),
                                onAddToCart: () async {
                                  final pId = int.tryParse(productId) ?? 0;
                                  if (pId > 0) {
                                    try {
                                      await Api.addToCart(pId, 1);
                                      if (context.mounted) {
                                        showAddToCartAnimation(context);
                                      }
                                    } catch (e) {
                                      if (context.mounted) {
                                        ScaffoldMessenger.of(context)
                                            .showSnackBar(
                                          SnackBar(
                                            content: Text(
                                              '${'Failed to add to cart:'.tr()} $e',
                                            ),
                                          ),
                                        );
                                      }
                                    }
                                  }
                                },
                              );
                            },
                          ),
          ),
        ],
      ),
      bottomNavigationBar: StitchBottomNavigationBar(
        currentIndex: -1,
        onTap: (index) {
          if (index == 0) {
            Navigator.popUntil(context, ModalRoute.withName('/categories'));
          } else if (index == 1) {
            Navigator.pushNamed(context, '/favorites');
          } else if (index == 2) {
            Navigator.pushNamed(context, '/cart');
          } else if (index == 3) {
            Navigator.pushNamed(context, '/active-orders');
          } else if (index == 4) {
            Navigator.pushNamed(context, '/profile');
          }
        },
      ),
    );
  }
}
