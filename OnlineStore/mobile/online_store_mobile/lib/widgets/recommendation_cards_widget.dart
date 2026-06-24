import 'package:flutter/material.dart';

/// Section configuration — maps backend keys to customer-friendly UI.
class _SectionConfig {
  final String icon;
  final String title;
  final String explanation;
  final bool showTrendingBadge;

  const _SectionConfig({
    required this.icon,
    required this.title,
    required this.explanation,
    this.showTrendingBadge = false,
  });
}

const _sectionConfigs = {
  'BasedOnHistory': _SectionConfig(
    icon: '✨',
    title: 'Recommended For You',
    explanation: 'Based on your shopping history, you might love these.',
  ),
  'FrequentlyBoughtTogether': _SectionConfig(
    icon: '🛒',
    title: 'Customers Also Bought',
    explanation: 'Popular items often purchased together.',
    showTrendingBadge: true,
  ),
  'RelatedByCategory': _SectionConfig(
    icon: '💡',
    title: 'You May Also Like',
    explanation: 'Similar products in this category.',
  ),
  'SmartUpsells': _SectionConfig(
    icon: '🔥',
    title: 'Complete Your Setup',
    explanation: 'Premium alternatives to level up your experience.',
  ),
};

/// Validates a product for display. Filters out bad data.
bool _isValidProduct(Map<String, dynamic> product) {
  final name = (product['ProductName'] ?? product['productName'] ?? '') as String;
  if (name.trim().isEmpty) return false;
  if (RegExp(r'^\d+$').hasMatch(name.trim())) return false;
  if (name.trim().length < 2) return false;
  return true;
}

/// Returns stock status info.
({String label, Color bgColor, Color textColor, bool isUrgent}) _getStockStatus(
    int stock, ThemeData theme) {
  if (stock <= 0) {
    return (
      label: 'Out of Stock',
      bgColor: const Color(0xFFFCEBEB),
      textColor: const Color(0xFFA32D2D),
      isUrgent: true,
    );
  }
  if (stock <= 3) {
    return (
      label: 'Only $stock left!',
      bgColor: const Color(0xFFFEE2E2),
      textColor: const Color(0xFFDC2626),
      isUrgent: true,
    );
  }
  if (stock <= 10) {
    return (
      label: 'Low Stock',
      bgColor: const Color(0xFFFEF3C7),
      textColor: const Color(0xFF92400E),
      isUrgent: false,
    );
  }
  return (
    label: 'In Stock',
    bgColor: const Color(0xFFEAF3DE),
    textColor: const Color(0xFF3B6D11),
    isUrgent: false,
  );
}

/// A premium recommendation card carousel widget for the AI chat.
class RecommendationCardsWidget extends StatefulWidget {
  final Map<String, dynamic> data;
  final Function(int productId, int quantity)? onAddToCart;
  final Function(int productId)? onNavigateProduct;

  const RecommendationCardsWidget({
    super.key,
    required this.data,
    this.onAddToCart,
    this.onNavigateProduct,
  });

  @override
  State<RecommendationCardsWidget> createState() =>
      _RecommendationCardsWidgetState();
}

class _RecommendationCardsWidgetState extends State<RecommendationCardsWidget>
    with SingleTickerProviderStateMixin {
  late AnimationController _animController;

  @override
  void initState() {
    super.initState();
    _animController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 600),
    )..forward();
  }

  @override
  void dispose() {
    _animController.dispose();
    super.dispose();
  }

  List<_SectionData> _buildSections() {
    final seenIds = <int>{};
    final sections = <_SectionData>[];

    for (final entry in _sectionConfigs.entries) {
      final key = entry.key;
      final config = entry.value;

      // Normalize: backend may use PascalCase or camelCase
      final rawList = (widget.data[key] ??
              widget.data[key[0].toLowerCase() + key.substring(1)] ??
              []) as List<dynamic>;

      final products = rawList
          .where((p) => p is Map<String, dynamic> && _isValidProduct(p))
          .cast<Map<String, dynamic>>()
          .where((p) {
        final id = (p['ProductId'] ?? p['productId'] ?? 0) as int;
        if (seenIds.contains(id)) return false;
        seenIds.add(id);
        return true;
      }).toList();

      if (products.isNotEmpty) {
        sections.add(_SectionData(config: config, products: products));
      }
    }

    return sections;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final sections = _buildSections();

    if (sections.isEmpty) return const SizedBox.shrink();

    return FadeTransition(
      opacity: _animController,
      child: SlideTransition(
        position: Tween<Offset>(
          begin: const Offset(0, 0.1),
          end: Offset.zero,
        ).animate(CurvedAnimation(
          parent: _animController,
          curve: Curves.easeOut,
        )),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            for (int i = 0; i < sections.length; i++) ...[
              if (i > 0)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 6),
                  child: Container(
                    height: 1,
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        colors: [
                          Colors.transparent,
                          theme.dividerColor.withValues(alpha: 0.3),
                          Colors.transparent,
                        ],
                      ),
                    ),
                  ),
                ),
              _buildSection(sections[i], theme),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildSection(_SectionData section, ThemeData theme) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        // Header
        Padding(
          padding: const EdgeInsets.only(left: 2, bottom: 2),
          child: Row(
            children: [
              Text(
                section.config.icon,
                style: const TextStyle(fontSize: 14),
              ),
              const SizedBox(width: 6),
              Expanded(
                child: Text(
                  section.config.title,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    fontWeight: FontWeight.w700,
                    fontSize: 13,
                  ),
                ),
              ),
            ],
          ),
        ),
        Padding(
          padding: const EdgeInsets.only(left: 2, bottom: 8),
          child: Text(
            section.config.explanation,
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.hintColor,
              fontStyle: FontStyle.italic,
              fontSize: 11,
            ),
          ),
        ),
        // Carousel
        SizedBox(
          height: 250,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 2),
            physics: const BouncingScrollPhysics(),
            itemCount: section.products.length,
            separatorBuilder: (_, __) => const SizedBox(width: 10),
            itemBuilder: (context, index) {
              return _ProductCard(
                product: section.products[index],
                showTrendingBadge: section.config.showTrendingBadge,
                onAddToCart: widget.onAddToCart,
                onNavigateProduct: widget.onNavigateProduct,
              );
            },
          ),
        ),
      ],
    );
  }
}

class _SectionData {
  final _SectionConfig config;
  final List<Map<String, dynamic>> products;

  _SectionData({required this.config, required this.products});
}

/// Individual product card widget.
class _ProductCard extends StatefulWidget {
  final Map<String, dynamic> product;
  final bool showTrendingBadge;
  final Function(int productId, int quantity)? onAddToCart;
  final Function(int productId)? onNavigateProduct;

  const _ProductCard({
    required this.product,
    this.showTrendingBadge = false,
    this.onAddToCart,
    this.onNavigateProduct,
  });

  @override
  State<_ProductCard> createState() => _ProductCardState();
}

class _ProductCardState extends State<_ProductCard> {
  bool _addedToCart = false;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final p = widget.product;

    final name = (p['ProductName'] ?? p['productName'] ?? 'Product') as String;
    final price = (p['Price'] ?? p['price'] ?? 0).toDouble();
    final stock = (p['Stock'] ?? p['stock'] ?? 0) as int;
    final imageUrl = (p['ImageUrl'] ?? p['imageUrl']) as String?;
    final productId = (p['ProductId'] ?? p['productId'] ?? 0) as int;
    final categoryName =
        (p['CategoryName'] ?? p['categoryName'] ?? '') as String;
    final storeName = (p['StoreName'] ?? p['storeName'] ?? '') as String;
    final stockStatus = _getStockStatus(stock, theme);

    return GestureDetector(
      onTap: () => widget.onNavigateProduct?.call(productId),
      child: Container(
        width: 155,
        decoration: BoxDecoration(
          color: theme.cardColor,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: theme.dividerColor.withValues(alpha: 0.2),
          ),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.04),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Image ──
            ClipRRect(
              borderRadius:
                  const BorderRadius.vertical(top: Radius.circular(16)),
              child: Stack(
                children: [
                  SizedBox(
                    width: double.infinity,
                    height: 110,
                    child: imageUrl != null && imageUrl.isNotEmpty
                        ? Image.network(
                            imageUrl,
                            fit: BoxFit.cover,
                            errorBuilder: (_, __, ___) =>
                                _buildPlaceholderImage(theme),
                          )
                        : _buildPlaceholderImage(theme),
                  ),
                  // Badges
                  if (widget.showTrendingBadge)
                    Positioned(
                      top: 6,
                      left: 6,
                      child: _buildBadge(
                        '🔥 Popular',
                        theme.primaryColor,
                        Colors.white,
                      ),
                    ),
                  if (stockStatus.isUrgent && stock > 0)
                    Positioned(
                      top: widget.showTrendingBadge ? 28 : 6,
                      left: 6,
                      child: _buildBadge(
                        stockStatus.label,
                        const Color(0xFFDC2626),
                        Colors.white,
                      ),
                    ),
                ],
              ),
            ),
            // ── Body ──
            Expanded(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(10, 8, 10, 8),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Store
                    if (storeName.isNotEmpty)
                      Text(
                        storeName.toUpperCase(),
                        style: TextStyle(
                          fontSize: 9,
                          fontWeight: FontWeight.w700,
                          color: theme.primaryColor,
                          letterSpacing: 0.4,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    const SizedBox(height: 2),
                    // Name
                    Text(
                      name,
                      style: theme.textTheme.bodySmall?.copyWith(
                        fontWeight: FontWeight.w600,
                        fontSize: 12,
                        height: 1.2,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    if (categoryName.isNotEmpty) ...[
                      const SizedBox(height: 1),
                      Text(
                        categoryName,
                        style: TextStyle(
                          fontSize: 9.5,
                          color: theme.hintColor,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ],
                    const SizedBox(height: 3),
                    // Price + stock
                    Row(
                      children: [
                        Text(
                          '\$${price.toStringAsFixed(2)}',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w800,
                            color: theme.primaryColor,
                          ),
                        ),
                        const SizedBox(width: 4),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 5,
                            vertical: 1,
                          ),
                          decoration: BoxDecoration(
                            color: stockStatus.bgColor,
                            borderRadius: BorderRadius.circular(4),
                          ),
                          child: Text(
                            stockStatus.label,
                            style: TextStyle(
                              fontSize: 8,
                              fontWeight: FontWeight.w700,
                              color: stockStatus.textColor,
                            ),
                          ),
                        ),
                      ],
                    ),
                    const Spacer(),
                    // ── Actions ──
                    Row(
                      children: [
                        Expanded(
                          child: SizedBox(
                            height: 30,
                            child: ElevatedButton(
                              onPressed: stock <= 0 || _addedToCart
                                  ? null
                                  : () {
                                      widget.onAddToCart
                                          ?.call(productId, 1);
                                      setState(() => _addedToCart = true);
                                      Future.delayed(
                                        const Duration(seconds: 2),
                                        () {
                                          if (mounted) {
                                            setState(
                                                () => _addedToCart = false);
                                          }
                                        },
                                      );
                                    },
                              style: ElevatedButton.styleFrom(
                                backgroundColor: _addedToCart
                                    ? const Color(0xFF3B6D11)
                                    : theme.primaryColor,
                                foregroundColor: Colors.white,
                                padding:
                                    const EdgeInsets.symmetric(horizontal: 6),
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                textStyle: const TextStyle(
                                  fontSize: 10,
                                  fontWeight: FontWeight.w700,
                                ),
                                elevation: 0,
                              ),
                              child: Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  if (!_addedToCart) ...[
                                    const Icon(Icons.shopping_cart_outlined,
                                        size: 12),
                                    const SizedBox(width: 3),
                                  ],
                                  Text(_addedToCart ? 'Added ✓' : 'Add'),
                                ],
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(width: 5),
                        SizedBox(
                          width: 30,
                          height: 30,
                          child: OutlinedButton(
                            onPressed: () =>
                                widget.onNavigateProduct?.call(productId),
                            style: OutlinedButton.styleFrom(
                              padding: EdgeInsets.zero,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(8),
                              ),
                              side: BorderSide(
                                color:
                                    theme.dividerColor.withValues(alpha: 0.3),
                              ),
                            ),
                            child: Icon(
                              Icons.visibility_outlined,
                              size: 14,
                              color: theme.hintColor,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPlaceholderImage(ThemeData theme) {
    return Container(
      color: theme.scaffoldBackgroundColor,
      child: Center(
        child: Icon(
          Icons.image_outlined,
          size: 28,
          color: theme.hintColor.withValues(alpha: 0.5),
        ),
      ),
    );
  }

  Widget _buildBadge(String text, Color bg, Color textColor) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: bg.withValues(alpha: 0.9),
        borderRadius: BorderRadius.circular(5),
      ),
      child: Text(
        text,
        style: TextStyle(
          fontSize: 8,
          fontWeight: FontWeight.w800,
          color: textColor,
          letterSpacing: 0.3,
        ),
      ),
    );
  }
}
