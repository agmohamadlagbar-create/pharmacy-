import 'package:flutter/material.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:google_fonts/google_fonts.dart';
import 'api.dart';
import 'favorites_manager.dart';
import 'product_info_page.dart';

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

  Widget _placeholder() {
    return Container(
      color: const Color(0xFFF0E9E6),
      child: const Center(
        child: Icon(Icons.image, size: 40, color: Color(0xFFA8948D)),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(
          title: Text('My Favorites'.tr(), style: GoogleFonts.playfairDisplay(fontWeight: FontWeight.w600)),
        ),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: Text('My Favorites'.tr(), style: GoogleFonts.playfairDisplay(fontWeight: FontWeight.w600)),
      ),
      body: _favorites.isEmpty
        ? Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Container(
                  width: 100,
                  height: 100,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: colorScheme.primary.withValues(alpha: 0.08),
                  ),
                  child: Icon(Icons.favorite_border_rounded, size: 48, color: colorScheme.primary.withValues(alpha: 0.4)),
                ),
                const SizedBox(height: 20),
                Text(
                  'No favorites yet'.tr(),
                  style: GoogleFonts.playfairDisplay(
                    fontSize: 20,
                    fontWeight: FontWeight.w600,
                    color: colorScheme.onSurface,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  'Items you love will appear here'.tr(),
                  style: GoogleFonts.dmSans(
                    fontSize: 14,
                    color: colorScheme.onSurface.withValues(alpha: 0.5),
                  ),
                ),
                const SizedBox(height: 24),
                OutlinedButton.icon(
                  onPressed: () => Navigator.pop(context),
                  icon: const Icon(Icons.shopping_bag_outlined),
                  label: Text('Start Shopping'.tr()),
                  style: OutlinedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  ),
                ),
              ],
            ),
          )
        : GridView.builder(
            padding: const EdgeInsets.all(16),
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 2,
              crossAxisSpacing: 14,
              mainAxisSpacing: 14,
              childAspectRatio: 0.65,
            ),
            itemCount: _favorites.length,
            itemBuilder: (context, index) {
              final product = _favorites[index];
              final imgUrl = product['imageUrl'];
              return Card(
                clipBehavior: Clip.antiAlias,
                elevation: 0,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                  side: BorderSide(color: colorScheme.onSurface.withValues(alpha: 0.06)),
                ),
                child: Stack(
                  children: [
                    InkWell(
                      onTap: () async {
                        await Navigator.push(context, MaterialPageRoute(
                          builder: (_) => ProductInfoPage(productId: product['id'])
                        ));
                        _loadFavorites();
                      },
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Expanded(
                            child: imgUrl != null
                              ? Image.network(
                                  imgUrl.toString().startsWith('http') ? imgUrl : '${Api.baseUrl}$imgUrl',
                                  fit: BoxFit.cover,
                                  cacheWidth: 300,
                                  errorBuilder: (_,__,___) => _placeholder(),
                                )
                              : _placeholder(),
                          ),
                          Padding(
                            padding: const EdgeInsets.all(12),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  product['name'] ?? '',
                                  style: GoogleFonts.dmSans(fontWeight: FontWeight.w600, fontSize: 13),
                                  maxLines: 2,
                                  overflow: TextOverflow.ellipsis,
                                ),
                                const SizedBox(height: 4),
                                if (product['price'] != null)
                                  Text(
                                    '\$${product['price']}',
                                    style: GoogleFonts.dmSans(
                                      color: colorScheme.primary,
                                      fontWeight: FontWeight.w700,
                                      fontSize: 15,
                                    ),
                                  ),
                              ],
                            ),
                          )
                        ],
                      ),
                    ),
                    // Favorite button
                    Positioned(
                      top: 8,
                      right: 8,
                      child: Material(
                        color: Colors.white.withValues(alpha: 0.9),
                        shape: const CircleBorder(),
                        clipBehavior: Clip.antiAlias,
                        child: InkWell(
                          onTap: () => _toggleFavorite(product),
                          child: const Padding(
                            padding: EdgeInsets.all(8),
                            child: Icon(Icons.favorite, color: Colors.red, size: 18),
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
    );
  }
}
