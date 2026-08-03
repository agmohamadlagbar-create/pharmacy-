import 'package:flutter/material.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:google_fonts/google_fonts.dart';
import 'api.dart';
import 'favorites_manager.dart';
import 'product_info_page.dart';
import 'widgets/stitch_product_card.dart';
import 'widgets/stitch_bottom_navigation_bar.dart';
import 'utils/animation_utils.dart';

class FavoritesPage extends StatefulWidget {
  const FavoritesPage({super.key});

  @override
  State<FavoritesPage> createState() => _FavoritesPageState();
}

class _FavoritesPageState extends State<FavoritesPage> {
  List<Map<String, dynamic>> _favorites = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadFavorites();
  }

  Future<void> _loadFavorites() async {
    final favs = await FavoritesManager.getFavorites();
    if (mounted) {
      setState(() {
        _favorites = favs;
        _isLoading = false;
      });
    }
  }

  Future<void> _toggleFavorite(Map<String, dynamic> product) async {
    await FavoritesManager.toggleFavorite(product);
    _loadFavorites();
  }

  Future<void> _handleAddToCart(int productId) async {
    try {
      await Api.addToCart(productId, 1);
      if (mounted) {
        showAddToCartAnimation(context);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${'Failed to add to cart:'.tr()} $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;

    if (_isLoading) {
      return Scaffold(
        backgroundColor: theme.scaffoldBackgroundColor,
        appBar: AppBar(
          backgroundColor: colorScheme.surface,
          elevation: 0,
          title: Text(
            'My Favorites'.tr(),
            style: GoogleFonts.inter(
                fontWeight: FontWeight.bold, color: colorScheme.onSurface),
          ),
        ),
        body: Center(child: CircularProgressIndicator(color: colorScheme.primary)),
      );
    }

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      appBar: AppBar(
        backgroundColor: colorScheme.surface,
        elevation: 0,
        scrolledUnderElevation: 1,
        title: Text(
          'My Favorites'.tr(),
          style: GoogleFonts.inter(
              fontWeight: FontWeight.bold, color: colorScheme.onSurface),
        ),
        leading: IconButton(
          icon: Icon(Icons.arrow_back_rounded, color: colorScheme.onSurface),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: _favorites.isEmpty
          ? Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Container(
                    width: 90,
                    height: 90,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: colorScheme.primary.withValues(alpha: 0.08),
                    ),
                    child: Icon(Icons.favorite_border_rounded,
                        size: 44, color: colorScheme.primary.withValues(alpha: 0.5)),
                  ),
                  const SizedBox(height: 20),
                  Text(
                    'No favorites yet'.tr(),
                    style: GoogleFonts.inter(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                      color: colorScheme.onSurface,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Items you love will appear here'.tr(),
                    style: GoogleFonts.inter(
                      fontSize: 14,
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 24),
                  OutlinedButton.icon(
                    style: OutlinedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 20, vertical: 12),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                    onPressed: () => Navigator.pop(context),
                    icon: const Icon(Icons.shopping_bag_outlined),
                    label: Text('Start Shopping'.tr()),
                  ),
                ],
              ),
            )
          : GridView.builder(
              padding: const EdgeInsets.all(16),
              physics: const BouncingScrollPhysics(),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                crossAxisSpacing: 16,
                mainAxisSpacing: 16,
                childAspectRatio: 0.75,
              ),
              itemCount: _favorites.length,
              itemBuilder: (context, index) {
                final product = _favorites[index];
                final String productIdStr = product['id'].toString();
                final int pId = int.tryParse(productIdStr) ?? 0;

                return StitchProductCard(
                  product: product,
                  isFavorite: true,
                  onTap: () {
                    if (pId > 0) {
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (_) => ProductInfoPage(productId: pId),
                        ),
                      ).then((_) => _loadFavorites());
                    }
                  },
                  onFavoriteToggle: () => _toggleFavorite(product),
                  onAddToCart: () {
                    if (pId > 0) {
                      _handleAddToCart(pId);
                    }
                  },
                );
              },
            ),
      bottomNavigationBar: StitchBottomNavigationBar(
        currentIndex: 1,
        onTap: (index) {
          if (index == 0) {
            Navigator.popUntil(context, ModalRoute.withName('/categories'));
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
