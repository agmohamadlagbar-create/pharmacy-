import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'widgets/custom_store_app_bar.dart';
import 'widgets/skeleton_loader.dart';
import 'product_info_page.dart';
import 'public_store_page.dart';
import 'favorites_manager.dart';

class CategoryProductsPage extends StatefulWidget {
  final String categoryId;
  final String categoryName;
  final int cartCount;
  final VoidCallback onNavigateCart;
  final Function(int) onAddToCart;

  const CategoryProductsPage({
    super.key,
    required this.categoryId,
    required this.categoryName,
    required this.cartCount,
    required this.onNavigateCart,
    required this.onAddToCart,
  });

  @override
  State<CategoryProductsPage> createState() => _CategoryProductsPageState();
}

class _CategoryProductsPageState extends State<CategoryProductsPage> {
  List<dynamic> _products = [];
  List<dynamic> _subcategories = [];
  bool _isLoading = true;
  bool _loadingSubcategories = true;
  String? _error;
  String? _selectedSubcategoryId;
  String? _userCountry;
  String? _userCity;

  @override
  void initState() {
    super.initState();
    _initializeData();
  }

  Future<void> _initializeData() async {
    await _fetchProfile();
    await _fetchSubcategories();
    await _fetchProducts();
  }

  Future<void> _fetchProfile() async {
    try {
      final res = await Api.get('/api/me');
      if (mounted) {
        setState(() {
          _userCountry = res['country'];
          _userCity = res['city'];
        });
      }
    } catch (_) {
      // Ignore profile errors, proceed without location filtering
    }
  }

  Future<void> _fetchSubcategories() async {
    try {
      final res = await Api.get('/api/categories/${widget.categoryId}/subcategories');
      if (mounted) {
        setState(() {
          final data = res['data'];
          _subcategories = data is List ? data : [];
          _loadingSubcategories = false;
        });
      }
    } catch (e) {
      if (mounted) setState(() => _loadingSubcategories = false);
    }
  }

  Future<void> _fetchProducts() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final targetId = _selectedSubcategoryId ?? widget.categoryId;
      String url = '/api/products/approved?categoryId=$targetId&limit=100';
      
      if (_userCountry != null) url += '&country=${Uri.encodeComponent(_userCountry!)}';
      if (_userCity != null) url += '&city=${Uri.encodeComponent(_userCity!)}';
      
      final res = await Api.get(url);
      final items = res['data'] ?? res;
      
      if (mounted) {
        setState(() {
          _products = items is List ? items : [];
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = '${'Failed to load products:'.tr()} $e';
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFFAF7F5),
      body: CustomScrollView(
        slivers: [
          SliverToBoxAdapter(
            child: CustomStoreAppBar(
              cartCount: widget.cartCount,
              onCartTap: widget.onNavigateCart,
              onSearchTap: () => Navigator.pushNamed(context, '/search'),
            ),
          ),
          
          // Header section
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 24, 20, 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: const Color(0xFFD85A30).withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: Text(
                          'Category'.tr(),
                          style: GoogleFonts.dmSans(
                            color: const Color(0xFFD85A30),
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            letterSpacing: 0.5,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text(
                    widget.categoryName,
                    style: GoogleFonts.playfairDisplay(
                      fontSize: 32,
                      fontWeight: FontWeight.bold,
                      color: const Color(0xFF1A0A00),
                    ),
                  ),
                  const SizedBox(height: 4),
                  Container(
                    width: 40,
                    height: 3,
                    decoration: BoxDecoration(
                      color: const Color(0xFFD85A30),
                      borderRadius: BorderRadius.circular(2),
                    ),
                  ),
                ],
              ),
            ),
          ),

          // Subcategories Chips
          if (!_loadingSubcategories && _subcategories.isNotEmpty)
            SliverToBoxAdapter(
              child: Container(
                height: 40,
                margin: const EdgeInsets.only(bottom: 16),
                child: ListView.builder(
                  scrollDirection: Axis.horizontal,
                  padding: const EdgeInsets.symmetric(horizontal: 20),
                  itemCount: _subcategories.length + 1,
                  itemBuilder: (context, index) {
                    final bool isAll = index == 0;
                    final sub = isAll ? null : _subcategories[index - 1];
                    final String? id = isAll ? null : sub['id'].toString();
                    final String name = isAll ? 'All'.tr() : sub['name'];
                    final bool isSelected = _selectedSubcategoryId == id;

                    return Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: ChoiceChip(
                        label: Text(name),
                        selected: isSelected,
                        onSelected: (selected) {
                          if (selected) {
                            setState(() {
                              _selectedSubcategoryId = id;
                            });
                            _fetchProducts();
                          }
                        },
                        labelStyle: GoogleFonts.dmSans(
                          fontSize: 13,
                          fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                          color: isSelected ? Colors.white : const Color(0xFF4A3F3B),
                        ),
                        selectedColor: const Color(0xFFD85A30),
                        backgroundColor: Colors.white,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(20),
                          side: BorderSide(
                            color: isSelected 
                                ? const Color(0xFFD85A30) 
                                : const Color(0xFFD85A30).withValues(alpha: 0.1),
                          ),
                        ),
                        showCheckmark: false,
                        elevation: isSelected ? 2 : 0,
                      ),
                    );
                  },
                ),
              ),
            ),

          if (_isLoading)
            SliverPadding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              sliver: SliverGrid(
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  crossAxisSpacing: 16,
                  mainAxisSpacing: 16,
                  childAspectRatio: 0.75,
                ),
                delegate: SliverChildBuilderDelegate(
                  (context, index) => const SkeletonLoader(),
                  childCount: 6,
                ),
              ),
            )
          else if (_error != null)
            SliverFillRemaining(
              child: Center(
                child: Padding(
                  padding: const EdgeInsets.all(32),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.error_outline, size: 48, color: Colors.red),
                      const SizedBox(height: 16),
                      Text(
                        _error!,
                        textAlign: TextAlign.center,
                        style: GoogleFonts.dmSans(color: const Color(0xFF6B5E59)),
                      ),
                      const SizedBox(height: 24),
                      ElevatedButton(
                        onPressed: _fetchProducts,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFFD85A30),
                          foregroundColor: Colors.white,
                        ),
                        child: Text('Retry'.tr()),
                      ),
                    ],
                  ),
                ),
              ),
            )
          else if (_products.isEmpty)
            SliverFillRemaining(
              child: Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(Icons.shopping_bag_outlined, size: 64, color: Color(0xFFA8948D)),
                    const SizedBox(height: 16),
                    Text(
                      'No products in this category'.tr(),
                      style: GoogleFonts.playfairDisplay(
                        fontSize: 20,
                        fontWeight: FontWeight.w600,
                        color: const Color(0xFF4A3F3B),
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'Try another category or search for something else.'.tr(),
                      style: GoogleFonts.dmSans(color: const Color(0xFF6B5E59)),
                    ),
                  ],
                ),
              ),
            )
          else
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(20, 0, 20, 32),
              sliver: SliverGrid(
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  crossAxisSpacing: 16,
                  mainAxisSpacing: 16,
                  childAspectRatio: 0.65,
                ),
                delegate: SliverChildBuilderDelegate(
                  (context, index) {
                    final product = _products[index];
                    return _ProductCard(
                      product: product,
                      onAddToCart: widget.onAddToCart,
                      onNavigateCart: widget.onNavigateCart,
                      cartCount: widget.cartCount,
                    );
                  },
                  childCount: _products.length,
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _ProductCard extends StatefulWidget {
  final Map<String, dynamic> product;
  final Function(int) onAddToCart;
  final VoidCallback onNavigateCart;
  final int cartCount;

  const _ProductCard({
    required this.product,
    required this.onAddToCart,
    required this.onNavigateCart,
    required this.cartCount,
  });

  @override
  State<_ProductCard> createState() => _ProductCardState();
}

class _ProductCardState extends State<_ProductCard> {
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
    final price = (widget.product['price'] as num?)?.toDouble() ?? 0.0;
    final imageUrl = widget.product['imageUrl'] as String?;
    final fullImageUrl = imageUrl != null
        ? (imageUrl.startsWith('http') ? imageUrl : '${Api.baseUrl}$imageUrl')
        : null;

    return GestureDetector(
      onTap: () async {
        if (widget.product['id'] != null) {
          await Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => ProductInfoPage(
                productId: widget.product['id'],
                onAddToCart: widget.onAddToCart,
                onNavigateCart: widget.onNavigateCart,
                cartCount: widget.cartCount,
              ),
            ),
          );
          _checkFavorite();
        }
      },
      child: Card(
        clipBehavior: Clip.antiAlias,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: const BorderSide(color: Color(0x14000000), width: 0.5),
        ),
        color: const Color(0xFFF9F5F3),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Expanded(
              child: Stack(
                children: [
                  Positioned.fill(
                    child: Container(
                      color: const Color(0xFFF0E9E6),
                      child: fullImageUrl != null
                          ? Image.network(
                              fullImageUrl,
                              fit: BoxFit.cover,
                              errorBuilder: (_, __, ___) => const Icon(
                                  Icons.image_not_supported,
                                  size: 40,
                                  color: Color(0xFFA8948D)),
                            )
                          : const Icon(Icons.image_not_supported,
                              size: 40, color: Color(0xFFA8948D)),
                    ),
                  ),
                  Positioned(
                    top: 8,
                    right: 8,
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(8),
                        boxShadow: [
                          BoxShadow(
                              color: Colors.black.withValues(alpha: 0.05),
                              blurRadius: 4,
                              offset: const Offset(0, 2))
                        ],
                      ),
                      child: Text(
                        '\$${price.toStringAsFixed(2)}',
                        style: GoogleFonts.dmSans(
                          color: const Color(0xFF993C1D),
                          fontWeight: FontWeight.w700,
                          fontSize: 12,
                        ),
                      ),
                    ),
                  ),
                  Positioned(
                    top: 8,
                    left: 8,
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
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    widget.product['name'] ?? '',
                    style: GoogleFonts.dmSans(
                        fontWeight: FontWeight.w600,
                        fontSize: 13,
                        color: const Color(0xFF1A0A00)),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 2),
                  if (widget.product['storeName'] != null &&
                      widget.product['storeName'].toString().isNotEmpty)
                    InkWell(
                      onTap: () {
                        if (widget.product['storeUserId'] != null) {
                          Navigator.push(
                              context,
                              MaterialPageRoute(
                                  builder: (_) => PublicStorePage(
                                        storeUserId:
                                            widget.product['storeUserId'],
                                        cartCount: widget.cartCount,
                                        onNavigateCart: widget.onNavigateCart,
                                        onAddToCart: widget.onAddToCart,
                                      )));
                        }
                      },
                      child: Row(
                        children: [
                          const Icon(Icons.storefront_outlined,
                              size: 12, color: Color(0xFFD85A30)),
                          const SizedBox(width: 4),
                          Expanded(
                            child: Text(
                              widget.product['storeName'],
                              style: GoogleFonts.dmSans(
                                color: const Color(0xFFD85A30),
                                fontSize: 11,
                                fontWeight: FontWeight.w500,
                                decoration: TextDecoration.underline,
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ),
                    ),
                  const SizedBox(height: 8),
                  SizedBox(
                    width: double.infinity,
                    height: 36,
                    child: ElevatedButton(
                      onPressed: (widget.product['stockQuantity'] ?? 0) > 0
                          ? () => widget.onAddToCart(widget.product['id'])
                          : null,
                      style: ElevatedButton.styleFrom(
                        padding: EdgeInsets.zero,
                        elevation: 0,
                        backgroundColor: const Color(0xFFD85A30),
                        disabledBackgroundColor:
                            const Color(0xFFA8948D).withValues(alpha: 0.3),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(8)),
                      ),
                      child: Text(
                        (widget.product['stockQuantity'] ?? 0) > 0
                            ? 'Add to Cart'.tr()
                            : 'Out of Stock'.tr(),
                        style: GoogleFonts.dmSans(
                            fontSize: 12,
                            fontWeight: FontWeight.w500,
                            color: Colors.white),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
