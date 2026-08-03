import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'api.dart';
import 'widgets/custom_store_app_bar.dart';
import 'widgets/skeleton_loader.dart';
import 'widgets/stitch_product_card.dart';
import 'widgets/stitch_bottom_navigation_bar.dart';
import 'widgets/status_chip.dart';
import 'product_info_page.dart';
import 'favorites_manager.dart';
import 'utils/animation_utils.dart';

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
  List<Map<String, dynamic>> _products = [];
  List<dynamic> _subcategories = [];
  bool _isLoading = true;
  bool _loadingSubcategories = true;
  String? _error;
  String? _selectedSubcategoryId;
  String? _userCountry;
  String? _userCity;
  Set<String> _favoriteProductIds = {};
  bool _isGuest = false;

  @override
  void initState() {
    super.initState();
    _initializeData();
  }

  Future<void> _initializeData() async {
    final prefs = await SharedPreferences.getInstance();
    if (mounted) {
      setState(() {
        _isGuest = prefs.getBool('isGuest') ?? false;
      });
    }
    await _loadFavorites();
    if (!_isGuest) await _fetchProfile();
    await _fetchSubcategories();
    await _fetchProducts();
  }

  Future<void> _loadFavorites() async {
    final favs = await FavoritesManager.getFavorites();
    if (mounted) {
      setState(() {
        _favoriteProductIds = favs.map((e) => e['id'].toString()).toSet();
      });
    }
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
    } catch (_) {}
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
          if (items is List) {
            _products = items.map((p) => Map<String, dynamic>.from(p)).toList();
          } else {
            _products = [];
          }
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
      body: CustomScrollView(
        physics: const BouncingScrollPhysics(),
        slivers: [
          // Custom App Bar with Search, Theme toggle, and Cart
          SliverToBoxAdapter(
            child: CustomStoreAppBar(
              cartCount: widget.cartCount,
              onCartTap: widget.onNavigateCart,
              onSearchTap: () => Navigator.pushNamed(context, '/search'),
            ),
          ),

          // Header Section
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      StatusChip(
                        label: 'Category'.tr().toUpperCase(),
                        type: StatusChipType.info,
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text(
                    widget.categoryName,
                    style: GoogleFonts.inter(
                      fontSize: 26,
                      fontWeight: FontWeight.bold,
                      letterSpacing: -0.5,
                      color: colorScheme.onSurface,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Container(
                    width: 40,
                    height: 3,
                    decoration: BoxDecoration(
                      color: colorScheme.primary,
                      borderRadius: BorderRadius.circular(2),
                    ),
                  ),
                ],
              ),
            ),
          ),

          // Subcategories Chips Filter
          if (!_loadingSubcategories && _subcategories.isNotEmpty)
            SliverToBoxAdapter(
              child: Container(
                height: 42,
                margin: const EdgeInsets.only(bottom: 16),
                child: ListView.builder(
                  scrollDirection: Axis.horizontal,
                  physics: const BouncingScrollPhysics(),
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
                        labelStyle: GoogleFonts.inter(
                          fontSize: 13,
                          fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                          color: isSelected
                              ? Colors.white
                              : colorScheme.onSurface,
                        ),
                        selectedColor: colorScheme.primary,
                        backgroundColor: colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(20),
                          side: BorderSide(
                            color: isSelected
                                ? colorScheme.primary
                                : colorScheme.outlineVariant.withValues(alpha: 0.5),
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

          // Loading Skeleton State
          if (_isLoading)
            SliverPadding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              sliver: SliverGrid(
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  crossAxisSpacing: 16,
                  mainAxisSpacing: 16,
                  childAspectRatio: 0.72,
                ),
                delegate: SliverChildBuilderDelegate(
                  (context, index) => Shimmer(
                    child: Skeleton(
                      borderRadius: BorderRadius.circular(16),
                    ),
                  ),
                  childCount: 6,
                ),
              ),
            )
          // Error State
          else if (_error != null)
            SliverFillRemaining(
              child: Center(
                child: Padding(
                  padding: const EdgeInsets.all(32),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.error_outline_rounded, size: 48, color: colorScheme.error),
                      const SizedBox(height: 16),
                      Text(
                        _error!,
                        textAlign: TextAlign.center,
                        style: GoogleFonts.inter(color: colorScheme.onSurfaceVariant),
                      ),
                      const SizedBox(height: 24),
                      ElevatedButton(
                        onPressed: _fetchProducts,
                        child: Text('Retry'.tr()),
                      ),
                    ],
                  ),
                ),
              ),
            )
          // Empty State
          else if (_products.isEmpty)
            SliverFillRemaining(
              child: Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.shopping_bag_outlined, size: 64, color: colorScheme.onSurfaceVariant),
                    const SizedBox(height: 16),
                    Text(
                      'No products in this category'.tr(),
                      style: GoogleFonts.inter(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: colorScheme.onSurface,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'Try another category or search for something else.'.tr(),
                      style: GoogleFonts.inter(color: colorScheme.onSurfaceVariant),
                    ),
                  ],
                ),
              ),
            )
          // Product 2-Column Grid (Stitch Product Cards)
          else
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(20, 0, 20, 32),
              sliver: SliverGrid(
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  crossAxisSpacing: 16,
                  mainAxisSpacing: 16,
                  childAspectRatio: 0.72,
                ),
                delegate: SliverChildBuilderDelegate(
                  (context, index) {
                    final product = _products[index];
                    final String productId = product['id'].toString();
                    final bool isFav = _favoriteProductIds.contains(productId);

                    return StitchProductCard(
                      product: product,
                      isFavorite: isFav,
                      onTap: () {
                        final pId = int.tryParse(productId) ?? 0;
                        if (pId > 0) {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => ProductInfoPage(productId: pId),
                            ),
                          );
                        }
                      },
                      onFavoriteToggle: () => _toggleFavorite(product),
                      onAddToCart: () {
                        final pId = int.tryParse(productId) ?? 0;
                        if (pId > 0) {
                          widget.onAddToCart(pId);
                          showAddToCartAnimation(context);
                        }
                      },
                    );
                  },
                  childCount: _products.length,
                ),
              ),
            ),
        ],
      ),
      bottomNavigationBar: StitchBottomNavigationBar(
        currentIndex: 0,
        cartCount: widget.cartCount,
        isGuest: _isGuest,
        onTap: (index) async {
          if (_isGuest) {
            if (index == 0) {
              Navigator.popUntil(context, ModalRoute.withName('/categories'));
            } else if (index == 1) {
              final prefs = await SharedPreferences.getInstance();
              await prefs.remove('isGuest');
              if (context.mounted) {
                Navigator.pushNamedAndRemoveUntil(context, '/', (route) => false);
              }
            }
            return;
          }
          if (index == 0) {
            Navigator.popUntil(context, ModalRoute.withName('/categories'));
          } else if (index == 1) {
            Navigator.pushNamed(context, '/favorites');
          } else if (index == 2) {
            widget.onNavigateCart();
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
