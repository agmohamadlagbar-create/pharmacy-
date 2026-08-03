import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'product_info_page.dart';
import 'favorites_manager.dart';
import 'widgets/stitch_product_card.dart';
import 'utils/animation_utils.dart';

class PublicStorePage extends StatefulWidget {
  final String storeUserId;
  final int? cartCount;
  final VoidCallback? onNavigateCart;
  final Function(int)? onAddToCart;

  const PublicStorePage({
    super.key,
    required this.storeUserId,
    this.cartCount,
    this.onNavigateCart,
    this.onAddToCart,
  });

  @override
  State<PublicStorePage> createState() => _PublicStorePageState();
}

class _PublicStorePageState extends State<PublicStorePage> {
  Map<String, dynamic>? _store;
  List<Map<String, dynamic>> _products = [];
  bool _loading = true;
  Set<String> _favoriteProductIds = {};

  @override
  void initState() {
    super.initState();
    _loadFavorites();
    _loadData();
  }

  Future<void> _loadFavorites() async {
    final favs = await FavoritesManager.getFavorites();
    if (mounted) {
      setState(() {
        _favoriteProductIds = favs.map((e) => e['id'].toString()).toSet();
      });
    }
  }

  Future<void> _loadData() async {
    setState(() => _loading = true);
    try {
      final storeRes = await Api.get('/api/shops/${widget.storeUserId}');
      final prodRes = await Api.get('/api/products/approved?storeUserId=${widget.storeUserId}');

      if (!mounted) return;
      setState(() {
        _store = storeRes['data'] is Map ? Map<String, dynamic>.from(storeRes['data']) : Map<String, dynamic>.from(storeRes);
        final items = prodRes['data'] is List ? prodRes['data'] : (prodRes is List ? prodRes : []);
        _products = (items as List)
            .map((e) => Map<String, dynamic>.from(e as Map))
            .toList();
        _loading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() => _loading = false);
    }
  }

  Future<void> _toggleFavorite(Map<String, dynamic> product) async {
    await FavoritesManager.toggleFavorite(product);
    await _loadFavorites();
  }

  bool _isStoreOpen(String? openTime, String? closeTime) {
    if (openTime == null || closeTime == null || openTime.isEmpty || closeTime.isEmpty) {
      return true;
    }

    try {
      final now = DateTime.now();

      int toMinutes(String value) {
        final parts = value.trim().split(':');
        if (parts.length < 2) return 0;
        final hour = int.tryParse(parts[0]) ?? 0;
        final minutePart = parts[1].length > 2 ? parts[1].substring(0, 2) : parts[1];
        final minute = int.tryParse(minutePart) ?? 0;
        return hour * 60 + minute;
      }

      final openMins = toMinutes(openTime);
      final closeMins = toMinutes(closeTime);
      final currentMins = now.hour * 60 + now.minute;

      if (openMins <= closeMins) {
        return currentMins >= openMins && currentMins <= closeMins;
      }

      return currentMins >= openMins || currentMins <= closeMins;
    } catch (_) {
      return true;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;

    if (_loading && _store == null) {
      return Scaffold(
        backgroundColor: theme.scaffoldBackgroundColor,
        body: Center(child: CircularProgressIndicator(color: colorScheme.primary)),
      );
    }

    if (_store == null) {
      return Scaffold(
        backgroundColor: theme.scaffoldBackgroundColor,
        appBar: AppBar(
          title: Text('Store Profile'.tr()),
          leading: IconButton(
            icon: Icon(Icons.arrow_back_rounded, color: colorScheme.onSurface),
            onPressed: () => Navigator.pop(context),
          ),
        ),
        body: Center(
          child: Text('Store not found'.tr(),
              style: GoogleFonts.inter(color: colorScheme.onSurfaceVariant)),
        ),
      );
    }

    final onSurface = colorScheme.onSurface;
    final secondaryText = colorScheme.onSurfaceVariant;
    final isOpen = _isStoreOpen(
      _store!['openingTime']?.toString(),
      _store!['closingTime']?.toString(),
    );
    final avatarUrl = _store!['profilePictureUrl']?.toString();
    final formattedAvatar = Api.formatImageUrl(avatarUrl);
    final storeInitial = (_store!['shopName']?.toString().isNotEmpty ?? false)
        ? _store!['shopName']![0].toUpperCase()
        : 'S';

    final location = _store!['location']?.toString().isNotEmpty == true
        ? _store!['location']
        : 'Türkiye';

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      body: RefreshIndicator(
        onRefresh: _loadData,
        child: CustomScrollView(
          physics: const BouncingScrollPhysics(),
          slivers: [
            // Store Hero Header Banner & Avatar Stack
            SliverToBoxAdapter(
              child: Stack(
                clipBehavior: Clip.none,
                children: [
                  // 1. Structural Column (defines total stack dimensions)
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Colorful Banner Gradient
                      Container(
                        height: 180,
                        width: double.infinity,
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                            colors: isDark
                                ? const [Color(0xFF1E1B4B), Color(0xFF312E81), Color(0xFF3730A3)]
                                : const [Color(0xFF3525CD), Color(0xFF4F46E5), Color(0xFF6366F1)],
                          ),
                        ),
                        child: Stack(
                          children: [
                            // Back Button
                            Positioned(
                              top: MediaQuery.of(context).padding.top + 8,
                              left: 12,
                              child: CircleAvatar(
                                backgroundColor: Colors.black.withValues(alpha: 0.25),
                                child: IconButton(
                                  icon: const Icon(Icons.arrow_back_rounded, color: Colors.white),
                                  onPressed: () => Navigator.pop(context),
                                ),
                              ),
                            ),
                            // Cart Badge
                            if (widget.cartCount != null)
                              Positioned(
                                top: MediaQuery.of(context).padding.top + 8,
                                right: 90,
                                child: CircleAvatar(
                                  backgroundColor: Colors.black.withValues(alpha: 0.25),
                                  child: IconButton(
                                    icon: Badge(
                                      label: Text(widget.cartCount.toString()),
                                      child: const Icon(Icons.shopping_bag_outlined, color: Colors.white),
                                    ),
                                    onPressed: widget.onNavigateCart,
                                  ),
                                ),
                              ),
                            // Open / Closed Status Badge
                            Positioned(
                              top: MediaQuery.of(context).padding.top + 14,
                              right: 16,
                              child: Container(
                                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
                                decoration: BoxDecoration(
                                  color: isOpen
                                      ? const Color(0xFF10B981).withValues(alpha: 0.3)
                                      : const Color(0xFFEF4444).withValues(alpha: 0.3),
                                  borderRadius: BorderRadius.circular(20),
                                  border: Border.all(
                                    color: isOpen ? const Color(0xFF10B981) : const Color(0xFFEF4444),
                                    width: 1.5,
                                  ),
                                ),
                                child: Text(
                                  '• ${isOpen ? 'OPEN'.tr() : 'CLOSED'.tr()}',
                                  style: GoogleFonts.inter(
                                    fontSize: 11,
                                    fontWeight: FontWeight.bold,
                                    letterSpacing: 0.8,
                                    color: Colors.white,
                                  ),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),

                      // Store Metadata Details Container (below 180px banner, top padding 55 leaves room for avatar)
                      Padding(
                        padding: const EdgeInsets.fromLTRB(24, 55, 24, 16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              _store!['shopName'] ?? '',
                              style: GoogleFonts.inter(
                                fontSize: 24,
                                fontWeight: FontWeight.bold,
                                color: onSurface,
                              ),
                            ),
                            if (_store!['openingTime'] != null) ...[
                              const SizedBox(height: 6),
                              Row(
                                children: [
                                  Icon(Icons.access_time_rounded,
                                      size: 15, color: theme.colorScheme.primary),
                                  const SizedBox(width: 6),
                                  Text(
                                    '${'Opens'.tr()} ${_store!['openingTime']} · ${'Closes'.tr()} ${_store!['closingTime']}',
                                    style: GoogleFonts.inter(
                                        fontSize: 13, color: secondaryText),
                                  ),
                                ],
                              ),
                            ],
                            const SizedBox(height: 10),
                            Row(
                              children: [
                                Icon(Icons.location_on_rounded,
                                    size: 15, color: theme.colorScheme.primary),
                                const SizedBox(width: 6),
                                Text(
                                  location,
                                  style: GoogleFonts.inter(
                                    fontSize: 13,
                                    fontWeight: FontWeight.w500,
                                    color: onSurface,
                                  ),
                                ),
                              ],
                            ),
                            if ((_store!['description']?.toString() ?? '').isNotEmpty) ...[
                              const SizedBox(height: 12),
                              Text(
                                _store!['description'],
                                style: GoogleFonts.inter(
                                  fontSize: 13,
                                  height: 1.5,
                                  color: secondaryText,
                                ),
                              ),
                            ],
                            const SizedBox(height: 20),
                            Divider(
                                height: 1,
                                thickness: 1,
                                color: theme.colorScheme.outline),
                          ],
                        ),
                      ),
                    ],
                  ),

                  // 2. Overlapping Shop Avatar Photo (Positioned ON TOP at y=135)
                  Positioned(
                    top: 135,
                    left: 24,
                    child: Container(
                      width: 90,
                      height: 90,
                      padding: const EdgeInsets.all(4),
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: theme.scaffoldBackgroundColor,
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withValues(alpha: isDark ? 0.4 : 0.15),
                            blurRadius: 14,
                            offset: const Offset(0, 6),
                          ),
                        ],
                      ),
                      child: Container(
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: theme.colorScheme.surfaceContainerHighest,
                        ),
                        clipBehavior: Clip.antiAlias,
                        child: formattedAvatar.isNotEmpty
                            ? Image.network(
                                formattedAvatar,
                                fit: BoxFit.cover,
                                errorBuilder: (_, __, ___) => Center(
                                  child: Text(
                                    storeInitial,
                                    style: GoogleFonts.inter(
                                      fontSize: 32,
                                      fontWeight: FontWeight.bold,
                                      color: theme.colorScheme.primary,
                                    ),
                                  ),
                                ),
                              )
                            : Center(
                                child: Text(
                                  storeInitial,
                                  style: GoogleFonts.inter(
                                    fontSize: 32,
                                    fontWeight: FontWeight.bold,
                                    color: theme.colorScheme.primary,
                                  ),
                                ),
                              ),
                      ),
                    ),
                  ),
                ],
              ),
            ),

            SliverPadding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              sliver: SliverToBoxAdapter(
                child: Text(
                  'PRODUCTS'.tr(),
                  style: GoogleFonts.inter(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 1.2,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              ),
            ),

            // Store Products Grid
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 40),
              sliver: _products.isEmpty
                  ? SliverToBoxAdapter(
                      child: Center(
                        child: Padding(
                          padding: const EdgeInsets.all(40),
                          child: Text('This store has no products yet.'.tr(),
                              style: TextStyle(
                                  color: theme.colorScheme.onSurfaceVariant)),
                        ),
                      ),
                    )
                  : SliverGrid(
                      gridDelegate:
                          const SliverGridDelegateWithFixedCrossAxisCount(
                        crossAxisCount: 2,
                        crossAxisSpacing: 16,
                        mainAxisSpacing: 16,
                        childAspectRatio: 0.75,
                      ),
                      delegate: SliverChildBuilderDelegate(
                        (ctx, i) {
                          final product = _products[i];
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
                                    builder: (_) => ProductInfoPage(
                                      productId: pId,
                                      onAddToCart: widget.onAddToCart,
                                      onNavigateCart: widget.onNavigateCart,
                                      cartCount: widget.cartCount,
                                    ),
                                  ),
                                );
                              }
                            },
                            onFavoriteToggle: () => _toggleFavorite(product),
                             onAddToCart: () async {
                               final pId = int.tryParse(productId) ?? 0;
                               if (pId > 0) {
                                 if (widget.onAddToCart != null) {
                                   widget.onAddToCart!(pId);
                                 } else {
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
      ),
    );
  }
}
