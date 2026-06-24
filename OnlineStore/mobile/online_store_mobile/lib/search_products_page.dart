import 'dart:async';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'api.dart';
import 'product_info_page.dart';
import 'favorites_manager.dart';

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
  
  List<dynamic> _products = [];
  bool _loading = false;
  String? _error;
  
  Timer? _debounce;
  Map<String, dynamic>? _userLocation;

  @override
  void initState() {
    super.initState();
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
        if (_userLocation!['city'] != null) url += '&city=${Uri.encodeComponent(_userLocation!['city'])}';
        if (_userLocation!['country'] != null) url += '&country=${Uri.encodeComponent(_userLocation!['country'])}';
      }

      final res = await Api.get(url);
      final items = res['data'] ?? res;
      if (mounted) {
        setState(() {
          _products = items is List ? items : [];
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
    _debounce = Timer(const Duration(milliseconds: 500), () {
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

  Widget _placeholder() {
    return Container(
      color: Colors.grey[200],
      child: const Center(
        child: Icon(Icons.image, size: 40, color: Colors.black26),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    
    return Scaffold(
      appBar: AppBar(
        titleSpacing: 0,
        title: TextField(
          controller: _searchCtrl,
          onChanged: _onSearchChanged,
          autofocus: true,
          decoration: InputDecoration(
            hintText: 'Search products...'.tr(),
            border: InputBorder.none,
            enabledBorder: InputBorder.none,
            focusedBorder: InputBorder.none,
            prefixIcon: const Icon(Icons.search, size: 20),
            suffixIcon: _searchCtrl.text.isNotEmpty
                ? IconButton(
                    icon: const Icon(Icons.clear, size: 20),
                    onPressed: () {
                      _searchCtrl.clear();
                      _onSearchChanged('');
                    },
                  )
                : null,
          ),
        ),
      ),
      body: Column(
        children: [
          // Filters Area
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              color: colorScheme.surface,
              border: Border(bottom: BorderSide(color: colorScheme.onSurface.withValues(alpha: 0.1))),
            ),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                    decoration: BoxDecoration(
                      border: Border.all(color: colorScheme.primary.withValues(alpha: 0.3)),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(
                      children: [
                        Icon(Icons.tune, size: 16, color: colorScheme.primary),
                        const SizedBox(width: 8),
                        Text('Filters'.tr(), style: TextStyle(color: colorScheme.primary, fontWeight: FontWeight.bold)),
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),
                  // Categories
                  Container(
                    height: 40,
                    padding: const EdgeInsets.symmetric(horizontal: 12),
                    decoration: BoxDecoration(
                      border: Border.all(color: colorScheme.onSurface.withValues(alpha: 0.2)),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButton<String>(
                        value: _selectedCategoryId,
                        hint: Text('All Categories'.tr()),
                        items: [
                          DropdownMenuItem(value: null, child: Text('All Categories'.tr())),
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
                  // Min Price
                  SizedBox(
                    width: 90,
                    height: 40,
                    child: TextField(
                      controller: _minPriceCtrl,
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                        hintText: 'Min \$'.tr(),
                        contentPadding: const EdgeInsets.symmetric(horizontal: 10),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                      ),
                      onChanged: _onSearchChanged,
                    ),
                  ),
                  const Padding(
                    padding: EdgeInsets.symmetric(horizontal: 8.0),
                    child: Text('—', style: TextStyle(color: Colors.grey)),
                  ),
                  // Max Price
                  SizedBox(
                    width: 90,
                    height: 40,
                    child: TextField(
                      controller: _maxPriceCtrl,
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                        hintText: 'Max \$'.tr(),
                        contentPadding: const EdgeInsets.symmetric(horizontal: 10),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                      ),
                      onChanged: _onSearchChanged,
                    ),
                  ),
                  
                  if (_selectedCategoryId != null || _minPriceCtrl.text.isNotEmpty || _maxPriceCtrl.text.isNotEmpty) ...[
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
          
          Expanded(
            child: _loading
              ? const Center(child: CircularProgressIndicator())
              : _error != null
                ? Center(child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(_error!),
                      TextButton(onPressed: _fetchProducts, child: Text('Retry'.tr()))
                    ],
                  ))
                : _products.isEmpty
                  ? Center(child: Text('No products found matching your search.'.tr()))
                  : GridView.builder(
                      padding: const EdgeInsets.all(16),
                      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                        crossAxisCount: 2,
                        crossAxisSpacing: 16,
                        mainAxisSpacing: 16,
                        childAspectRatio: 0.65,
                      ),
                      itemCount: _products.length,
                  itemBuilder: (context, index) {
                    final product = _products[index];
                    final imgUrl = product['imageUrl'];
                    return _SearchProductCard(
                      product: product,
                      imgUrl: imgUrl,
                      placeholder: _placeholder,
                      colorScheme: colorScheme,
                    );
                  },
                    ),
          ),
        ],
      )
    );
  }
}

class _SearchProductCard extends StatefulWidget {
  final dynamic product;
  final dynamic imgUrl;
  final Widget Function() placeholder;
  final ColorScheme colorScheme;

  const _SearchProductCard({
    required this.product,
    required this.imgUrl,
    required this.placeholder,
    required this.colorScheme,
  });

  @override
  State<_SearchProductCard> createState() => _SearchProductCardState();
}

class _SearchProductCardState extends State<_SearchProductCard> {
  bool _isFavorite = false;

  @override
  void initState() {
    super.initState();
    _checkFavorite();
  }

  Future<void> _checkFavorite() async {
    final fav = await FavoritesManager.isFavorite(widget.product['id']);
    if (mounted) setState(() => _isFavorite = fav);
  }

  Future<void> _toggleFavorite() async {
    await FavoritesManager.toggleFavorite({
      'id': widget.product['id'],
      'name': widget.product['name'],
      'price': widget.product['price'],
      'imageUrl': widget.product['imageUrl'],
    });
    _checkFavorite();
  }

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Card(
          clipBehavior: Clip.antiAlias,
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          elevation: 2,
          child: InkWell(
            onTap: () async {
              await Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) =>
                        ProductInfoPage(productId: widget.product['id']),
                  ));
              _checkFavorite();
            },
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Expanded(
                  child: widget.imgUrl != null
                      ? Image.network(
                          widget.imgUrl.toString().startsWith('http')
                              ? widget.imgUrl
                              : '${Api.baseUrl}${widget.imgUrl}',
                          fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) => widget.placeholder(),
                        )
                      : widget.placeholder(),
                ),
                Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        widget.product['name'] ?? '',
                        style: GoogleFonts.inter(
                            fontWeight: FontWeight.bold, fontSize: 13),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: 4),
                      if (widget.product['price'] != null)
                        Text('\$${widget.product['price']}',
                            style: TextStyle(
                                color: widget.colorScheme.primary,
                                fontWeight: FontWeight.bold,
                                fontSize: 15)),
                    ],
                  ),
                )
              ],
            ),
          ),
        ),
        Positioned(
          top: 8,
          right: 8,
          child: InkWell(
            onTap: _toggleFavorite,
            child: Container(
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.8),
                shape: BoxShape.circle,
              ),
              child: Icon(
                _isFavorite ? Icons.favorite : Icons.favorite_border,
                size: 18,
                color: _isFavorite ? Colors.red : Colors.grey,
              ),
            ),
          ),
        ),
      ],
    );
  }
}
