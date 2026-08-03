import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'public_store_page.dart';
import 'favorites_manager.dart';
import 'widgets/status_chip.dart';
import 'utils/animation_utils.dart';

class ProductInfoPage extends StatefulWidget {
  final int productId;
  final Function(int)? onAddToCart;
  final VoidCallback? onNavigateCart;
  final int? cartCount;

  const ProductInfoPage({
    super.key,
    required this.productId,
    this.onAddToCart,
    this.onNavigateCart,
    this.cartCount,
  });

  @override
  State<ProductInfoPage> createState() => _ProductInfoPageState();
}

class _ProductInfoPageState extends State<ProductInfoPage> {
  Map<String, dynamic>? _details;
  bool _loading = true;
  String? _error;
  int _activeTabIndex = 0;
  int _activeImageIndex = 0;
  int _quantity = 1;
  bool _isFavorite = false;

  @override
  void initState() {
    super.initState();
    _loadDetails();
    _checkFavorite();
  }

  Future<void> _checkFavorite() async {
    final fav = await FavoritesManager.isFavorite(widget.productId);
    if (mounted) setState(() => _isFavorite = fav);
  }

  Future<void> _toggleFavorite() async {
    if (_details == null) return;
    final p = _details!['product'] ?? {};
    await FavoritesManager.toggleFavorite({
      'id': widget.productId,
      'name': p['name'],
      'price': p['price'],
      'imageUrl': p['imageUrl'],
    });
    _checkFavorite();
  }

  Future<void> _loadDetails() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await Api.get('/api/products/${widget.productId}/details');
      final data = res['data'] ?? res;
      if (mounted) {
        setState(() {
          _details = Map<String, dynamic>.from(data);
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString().replaceFirst('Exception: ', '');
          _loading = false;
        });
      }
    }
  }

  Future<void> _submitReview(int rating, String comment) async {
    try {
      await Api.post('/api/products/${widget.productId}/reviews', {
        'rating': rating,
        'comment': comment,
      });
      _loadDetails();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Review submitted successfully!'.tr())),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${'Failed:'.tr()} $e')),
        );
      }
    }
  }

  void _showReviewDialog() {
    int localRating = 5;
    final commentController = TextEditingController();

    showDialog(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          title: Text('Add Review'.tr(),
              style: GoogleFonts.inter(fontWeight: FontWeight.bold)),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: List.generate(
                  5,
                  (index) => IconButton(
                    icon: Icon(
                      index < localRating ? Icons.star_rounded : Icons.star_border_rounded,
                      color: const Color(0xFFF59E0B),
                    ),
                    onPressed: () => setDialogState(() => localRating = index + 1),
                  ),
                ),
              ),
              TextField(
                controller: commentController,
                maxLines: 3,
                decoration: InputDecoration(
                  hintText: 'Your comment...'.tr(),
                  filled: true,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                    borderSide: BorderSide.none,
                  ),
                ),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('Cancel'.tr()),
            ),
            ElevatedButton(
              onPressed: () {
                if (commentController.text.trim().isNotEmpty) {
                  _submitReview(localRating, commentController.text.trim());
                  Navigator.pop(context);
                }
              },
              child: Text('Submit'.tr()),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      appBar: AppBar(
        backgroundColor: theme.scaffoldBackgroundColor,
        elevation: 0,
        scrolledUnderElevation: 1,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_rounded, color: colorScheme.onSurface),
          onPressed: () => Navigator.pop(context),
        ),
        title: Text(
          _details != null ? (_details!['product']?['name'] ?? '') : 'Product Details'.tr(),
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: GoogleFonts.inter(
            fontSize: 16,
            fontWeight: FontWeight.bold,
            color: colorScheme.onSurface,
          ),
        ),
        actions: [
          IconButton(
            icon: Icon(
              _isFavorite ? Icons.favorite_rounded : Icons.favorite_border_rounded,
              color: _isFavorite ? const Color(0xFFEF4444) : colorScheme.onSurface,
            ),
            onPressed: _toggleFavorite,
          ),
          if (widget.onNavigateCart != null)
            IconButton(
              icon: Badge(
                label: Text('${widget.cartCount ?? 0}'),
                child: Icon(Icons.shopping_bag_outlined, color: colorScheme.onSurface),
              ),
              onPressed: widget.onNavigateCart,
            ),
          const SizedBox(width: 8),
        ],
      ),
      body: _loading
          ? Center(child: CircularProgressIndicator(color: colorScheme.primary))
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.error_outline_rounded,
                          size: 48, color: colorScheme.error),
                      const SizedBox(height: 12),
                      Text(_error!, style: GoogleFonts.inter(color: colorScheme.onSurface)),
                      const SizedBox(height: 16),
                      ElevatedButton(
                        onPressed: _loadDetails,
                        child: Text('Retry'.tr()),
                      ),
                    ],
                  ),
                )
              : _buildContent(context),
      bottomNavigationBar: _details == null ? null : _buildBottomBar(context),
    );
  }

  Widget _buildBottomBar(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;
    final p = _details!['product'] ?? {};
    final inStock = _details!['inStock'] ?? 0;
    final double price = (p['price'] is num)
        ? (p['price'] as num).toDouble()
        : double.tryParse(p['price'].toString()) ?? 0.0;
    final double totalPrice = price * _quantity;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
      decoration: BoxDecoration(
        color: colorScheme.surface,
        border: Border(
          top: BorderSide(
            color: colorScheme.outlineVariant.withValues(alpha: 0.5),
            width: 1,
          ),
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: isDark ? 0.3 : 0.05),
            blurRadius: 12,
            offset: const Offset(0, -4),
          ),
        ],
      ),
      child: SafeArea(
        child: Row(
          children: [
            // Quantity Selector
            Container(
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(12),
                border: Border.all(
                  color: colorScheme.outlineVariant.withValues(alpha: 0.6),
                ),
                color: colorScheme.surfaceContainerHighest.withValues(alpha: 0.4),
              ),
              child: Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.remove, size: 18),
                    onPressed: _quantity > 1
                        ? () => setState(() => _quantity--)
                        : null,
                  ),
                  Text(
                    '$_quantity',
                    style: GoogleFonts.inter(
                      fontWeight: FontWeight.bold,
                      fontSize: 16,
                      color: colorScheme.onSurface,
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.add, size: 18),
                    onPressed: inStock > _quantity
                        ? () => setState(() => _quantity++)
                        : null,
                  ),
                ],
              ),
            ),
            const SizedBox(width: 16),

            // Add to Cart Button
            Expanded(
              child: ElevatedButton(
                onPressed: inStock > 0
                    ? () async {
                        if (widget.onAddToCart != null) {
                          widget.onAddToCart?.call(widget.productId);
                        } else {
                          try {
                            await Api.addToCart(widget.productId, _quantity);
                            if (context.mounted) {
                              showAddToCartAnimation(context);
                            }
                          } catch (e) {
                            if (context.mounted) {
                              ScaffoldMessenger.of(context).showSnackBar(
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
                    : null,
                style: ElevatedButton.styleFrom(
                  backgroundColor: colorScheme.primary,
                  foregroundColor: Colors.white,
                  disabledBackgroundColor: isDark
                      ? Colors.white.withValues(alpha: 0.12)
                      : const Color(0xFFCBD5E1),
                  disabledForegroundColor: isDark
                      ? Colors.white38
                      : const Color(0xFF64748B),
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  elevation: 0,
                ),
                child: Text(
                  inStock > 0
                      ? '${'Add to Cart'.tr()} • \$${totalPrice.toStringAsFixed(2)}'
                      : 'OUT OF STOCK'.tr(),
                  style: GoogleFonts.inter(
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildContent(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;

    final p = _details!['product'] ?? {};
    final inStock = _details!['inStock'] ?? 0;
    final avgRating = (_details!['averageRating'] ?? 0).toDouble();
    final totalReviews = _details!['totalReviews'] ?? 0;

    List<String> allImages = [];
    if (p['imageUrl'] != null && p['imageUrl'].toString().isNotEmpty) {
      allImages.add(Api.formatImageUrl(p['imageUrl'].toString()));
    }
    if (p['additionalImageUrls'] != null && p['additionalImageUrls'] is List) {
      for (var img in p['additionalImageUrls']) {
        if (img != null && img.toString().isNotEmpty) {
          allImages.add(Api.formatImageUrl(img.toString()));
        }
      }
    }

    return SingleChildScrollView(
      physics: const BouncingScrollPhysics(),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Large Image Carousel Container
          Container(
            height: 340,
            decoration: BoxDecoration(
              color: colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
            ),
            child: Stack(
              children: [
                PageView.builder(
                  itemCount: allImages.isEmpty ? 1 : allImages.length,
                  onPageChanged: (idx) => setState(() => _activeImageIndex = idx),
                  itemBuilder: (ctx, i) {
                    if (allImages.isEmpty) {
                      return Center(
                        child: Icon(
                          Icons.image_not_supported_rounded,
                          size: 80,
                          color: colorScheme.onSurfaceVariant,
                        ),
                      );
                    }
                    return Image.network(
                      allImages[i],
                      fit: BoxFit.cover,
                      errorBuilder: (_, __, ___) => Center(
                        child: Icon(
                          Icons.image_not_supported_rounded,
                          size: 80,
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                    );
                  },
                ),

                // Indicator Dots
                if (allImages.length > 1)
                  Positioned(
                    bottom: 16,
                    left: 0,
                    right: 0,
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: List.generate(
                        allImages.length,
                        (idx) => AnimatedContainer(
                          duration: const Duration(milliseconds: 300),
                          width: _activeImageIndex == idx ? 18 : 8,
                          height: 8,
                          margin: const EdgeInsets.symmetric(horizontal: 3),
                          decoration: BoxDecoration(
                            borderRadius: BorderRadius.circular(4),
                            color: _activeImageIndex == idx
                                ? colorScheme.primary
                                : Colors.white.withValues(alpha: 0.6),
                          ),
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),

          // Thumbnail List Row
          if (allImages.length > 1)
            Container(
              height: 70,
              padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 16),
              color: colorScheme.surface,
              child: ListView.builder(
                scrollDirection: Axis.horizontal,
                itemCount: allImages.length,
                itemBuilder: (ctx, idx) => GestureDetector(
                  onTap: () => setState(() => _activeImageIndex = idx),
                  child: Container(
                    width: 54,
                    height: 54,
                    margin: const EdgeInsets.only(right: 8),
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                        color: _activeImageIndex == idx
                            ? colorScheme.primary
                            : colorScheme.outlineVariant.withValues(alpha: 0.5),
                        width: _activeImageIndex == idx ? 2 : 1,
                      ),
                    ),
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: Image.network(
                        allImages[idx],
                        fit: BoxFit.cover,
                        errorBuilder: (_, __, ___) => Container(
                          color: colorScheme.surfaceContainerHighest,
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),

          // Product Details Card Section
          Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Stock Badge + Category
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    StatusChip(
                      label: inStock > 0 ? '${'In Stock'.tr()} ($inStock)' : 'Out of Stock'.tr(),
                      type: inStock > 0 ? StatusChipType.success : StatusChipType.error,
                      showDot: true,
                    ),
                    if (p['categoryName'] != null)
                      Text(
                        p['categoryName'],
                        style: GoogleFonts.inter(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                  ],
                ),
                const SizedBox(height: 12),

                // Title and Price
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        p['name'] ?? '',
                        style: GoogleFonts.inter(
                          fontSize: 22,
                          fontWeight: FontWeight.bold,
                          letterSpacing: -0.3,
                          color: colorScheme.onSurface,
                        ),
                      ),
                    ),
                    const SizedBox(width: 16),
                    Text(
                      '\$${((p['price'] ?? 0) as num).toDouble().toStringAsFixed(2)}',
                      style: GoogleFonts.inter(
                        fontSize: 22,
                        fontWeight: FontWeight.bold,
                        color: colorScheme.primary,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),

                // Rating & Reviews row
                Row(
                  children: [
                    Row(
                      children: List.generate(
                        5,
                        (idx) => Icon(
                          Icons.star_rounded,
                          size: 18,
                          color: idx < avgRating
                              ? const Color(0xFFF59E0B)
                              : colorScheme.outlineVariant,
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Text(
                      '${avgRating.toStringAsFixed(1)} ($totalReviews ${'reviews'.tr()})',
                      style: GoogleFonts.inter(
                        fontSize: 13,
                        color: colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),

                // Store info tag
                if (p['storeName'] != null)
                  InkWell(
                    borderRadius: BorderRadius.circular(10),
                    onTap: () {
                      if (p['storeUserId'] != null) {
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (_) => PublicStorePage(
                              storeUserId: p['storeUserId'].toString(),
                              cartCount: widget.cartCount,
                              onNavigateCart: widget.onNavigateCart,
                              onAddToCart: widget.onAddToCart,
                            ),
                          ),
                        );
                      }
                    },
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 14, vertical: 10),
                      decoration: BoxDecoration(
                        color: colorScheme.primary.withValues(alpha: 0.08),
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(
                          color: colorScheme.primary.withValues(alpha: 0.2),
                        ),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.storefront_rounded,
                              size: 18, color: colorScheme.primary),
                          const SizedBox(width: 8),
                          Text(
                            p['storeName'],
                            style: GoogleFonts.inter(
                              color: colorScheme.primary,
                              fontWeight: FontWeight.bold,
                              fontSize: 13,
                            ),
                          ),
                          const SizedBox(width: 4),
                          Icon(Icons.chevron_right,
                              size: 16, color: colorScheme.primary),
                        ],
                      ),
                    ),
                  ),
                const SizedBox(height: 24),

                // Description Header
                Text(
                  'DESCRIPTION'.tr(),
                  style: GoogleFonts.inter(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 1.2,
                    color: colorScheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  p['description'] ?? 'No description available.'.tr(),
                  style: GoogleFonts.inter(
                    fontSize: 15,
                    height: 1.6,
                    color: colorScheme.onSurface,
                  ),
                ),
                const SizedBox(height: 32),

                // Tabs: Details / Reviews
                Row(
                  children: [
                    _buildTab(context, 'Details'.tr(), 0),
                    _buildTab(context, '${'Reviews'.tr()} ($totalReviews)', 1),
                  ],
                ),
                const SizedBox(height: 20),
                _activeTabIndex == 0
                    ? _buildDetailsTab(context, p, inStock)
                    : _buildReviewsTab(context, avgRating, totalReviews),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTab(BuildContext context, String title, int idx) {
    final theme = Theme.of(context);
    final active = _activeTabIndex == idx;
    return GestureDetector(
      onTap: () => setState(() => _activeTabIndex = idx),
      child: Container(
        margin: const EdgeInsets.only(right: 24),
        padding: const EdgeInsets.only(bottom: 8),
        decoration: BoxDecoration(
          border: Border(
            bottom: BorderSide(
              color: active ? theme.colorScheme.primary : Colors.transparent,
              width: 2,
            ),
          ),
        ),
        child: Text(
          title,
          style: GoogleFonts.inter(
            fontSize: 15,
            fontWeight: active ? FontWeight.bold : FontWeight.w500,
            color: active
                ? theme.colorScheme.onSurface
                : theme.colorScheme.onSurfaceVariant,
          ),
        ),
      ),
    );
  }

  Widget _buildDetailsTab(BuildContext context, Map p, int inStock) {
    return Column(
      children: [
        _buildInfoRow(context, 'Category'.tr(), p['categoryName'] ?? 'N/A'),
        _buildInfoRow(
            context,
            'Availability'.tr(),
            inStock > 0
                ? '$inStock ${'pieces'.tr()}'
                : 'Out of Stock'.tr()),
        _buildInfoRow(context, 'Condition'.tr(), 'New Original'.tr()),
      ],
    );
  }

  Widget _buildInfoRow(BuildContext context, String label, String val) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label,
              style: GoogleFonts.inter(color: theme.colorScheme.onSurfaceVariant, fontSize: 14)),
          Text(val,
              style: GoogleFonts.inter(
                  fontWeight: FontWeight.w600, color: theme.colorScheme.onSurface, fontSize: 14)),
        ],
      ),
    );
  }

  Widget _buildReviewsTab(
      BuildContext context, double avgRating, int totalReviews) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final latestReviews = _details!['latestReviews'] as List? ?? [];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Text(
              avgRating.toStringAsFixed(1),
              style: GoogleFonts.inter(
                  fontSize: 36,
                  fontWeight: FontWeight.bold,
                  color: colorScheme.onSurface),
            ),
            const SizedBox(width: 16),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: List.generate(
                    5,
                    (idx) => Icon(
                      Icons.star_rounded,
                      size: 16,
                      color: idx < avgRating
                          ? const Color(0xFFF59E0B)
                          : colorScheme.outlineVariant,
                    ),
                  ),
                ),
                Text('Average Rating'.tr(),
                    style: GoogleFonts.inter(
                        fontSize: 12, color: colorScheme.onSurfaceVariant)),
              ],
            ),
            const Spacer(),
            TextButton(
              onPressed: _showReviewDialog,
              child: Text('Add Review'.tr()),
            ),
          ],
        ),
        const SizedBox(height: 24),
        ...latestReviews.map((r) => Container(
              margin: const EdgeInsets.only(bottom: 16),
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: colorScheme.surfaceContainerHighest.withValues(alpha: 0.4),
                borderRadius: BorderRadius.circular(14),
                border: Border.all(
                  color: colorScheme.outlineVariant.withValues(alpha: 0.5),
                  width: 1,
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        r['userName'] ?? 'User'.tr(),
                        style: GoogleFonts.inter(
                            fontWeight: FontWeight.bold,
                            color: colorScheme.onSurface),
                      ),
                      Row(
                        children: List.generate(
                          5,
                          (idx) => Icon(
                            Icons.star_rounded,
                            size: 14,
                            color: idx < (r['rating'] ?? 0)
                                ? const Color(0xFFF59E0B)
                                : colorScheme.outlineVariant,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text(
                    r['comment'] ?? '',
                    style: GoogleFonts.inter(
                        color: colorScheme.onSurfaceVariant, height: 1.4),
                  ),
                ],
              ),
            )),
      ],
    );
  }
}
