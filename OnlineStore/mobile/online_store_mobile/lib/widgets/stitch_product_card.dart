import 'package:flutter/material.dart';
import '../api.dart';

class StitchProductCard extends StatelessWidget {
  final Map<String, dynamic> product;
  final bool isFavorite;
  final VoidCallback onTap;
  final VoidCallback onFavoriteToggle;
  final VoidCallback onAddToCart;

  const StitchProductCard({
    super.key,
    required this.product,
    required this.isFavorite,
    required this.onTap,
    required this.onFavoriteToggle,
    required this.onAddToCart,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    final String name = product['name'] ?? product['title'] ?? 'Product';
    final double price = (product['price'] is num)
        ? (product['price'] as num).toDouble()
        : double.tryParse(product['price'].toString()) ?? 0.0;
    final String rawImageUrl = product['imageUrl'] ?? product['image'] ?? '';
    final String formattedImageUrl = Api.formatImageUrl(rawImageUrl);

    final dynamic rawStock = product['stockQuantity'] ??
        product['inStock'] ??
        product['stock'] ??
        product['Stock'] ??
        product['quantity'] ??
        product['currentStock'];
    final bool hasStockField = rawStock != null;
    final int stock = (rawStock is num)
        ? rawStock.toInt()
        : (rawStock != null ? (int.tryParse(rawStock.toString()) ?? 0) : 1);
    final bool isOutOfStock =
        product['isOutOfStock'] == true || (hasStockField && stock <= 0);

    return GestureDetector(
      onTap: onTap,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Aspect Square Image Container
          AspectRatio(
            aspectRatio: 1.0,
            child: Container(
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(16),
                color: isDark
                    ? theme.colorScheme.surfaceContainerHighest
                    : const Color(0xFFF1F5F9),
                border: Border.all(
                  color: isDark
                      ? Colors.white.withValues(alpha: 0.1)
                      : theme.colorScheme.outline.withValues(alpha: 0.4),
                ),
              ),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(16),
                child: Stack(
                  fit: StackFit.expand,
                  children: [
                    // Image
                    formattedImageUrl.isEmpty
                        ? Center(
                            child: Icon(
                              Icons.shopping_bag_outlined,
                              size: 40,
                              color: theme.colorScheme.onSurfaceVariant,
                            ),
                          )
                        : (formattedImageUrl.startsWith('assets/')
                            ? Image.asset(
                                formattedImageUrl,
                                fit: BoxFit.cover,
                                errorBuilder: (context, error, stackTrace) =>
                                    Center(
                                  child: Icon(
                                    Icons.shopping_bag_outlined,
                                    size: 40,
                                    color: theme.colorScheme.onSurfaceVariant,
                                  ),
                                ),
                              )
                            : Image.network(
                                formattedImageUrl,
                                fit: BoxFit.cover,
                                errorBuilder: (context, error, stackTrace) =>
                                    Center(
                                  child: Icon(
                                    Icons.shopping_bag_outlined,
                                    size: 40,
                                    color: theme.colorScheme.onSurfaceVariant,
                                  ),
                                ),
                              )),

                    // Out of stock overlay badge
                    if (isOutOfStock)
                      Positioned(
                        top: 8,
                        left: 8,
                        child: Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 8, vertical: 4),
                          decoration: BoxDecoration(
                            color: Colors.black.withValues(alpha: 0.7),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: const Text(
                            'OUT OF STOCK',
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 9,
                              fontWeight: FontWeight.bold,
                              letterSpacing: 0.5,
                            ),
                          ),
                        ),
                      ),

                    // Favorite Button (Top Right)
                    Positioned(
                      top: 8,
                      right: 8,
                      child: Material(
                        color: isDark
                            ? Colors.black.withValues(alpha: 0.5)
                            : Colors.white.withValues(alpha: 0.9),
                        shape: const CircleBorder(),
                        child: InkWell(
                          customBorder: const CircleBorder(),
                          onTap: onFavoriteToggle,
                          child: Padding(
                            padding: const EdgeInsets.all(8.0),
                            child: Icon(
                              isFavorite ? Icons.favorite : Icons.favorite_border,
                              size: 18,
                              color: isFavorite
                                  ? const Color(0xFFEF4444)
                                  : (isDark ? Colors.white : theme.colorScheme.onSurface),
                            ),
                          ),
                        ),
                      ),
                    ),

                    // Quick Add to Cart Button (Bottom Right)
                    Positioned(
                      bottom: 8,
                      right: 8,
                      child: Material(
                        color: isOutOfStock
                            ? (isDark
                                ? Colors.white.withValues(alpha: 0.12)
                                : const Color(0xFFCBD5E1))
                            : theme.colorScheme.primary,
                        borderRadius: BorderRadius.circular(10),
                        elevation: isOutOfStock ? 0 : 2,
                        child: InkWell(
                          borderRadius: BorderRadius.circular(10),
                          onTap: isOutOfStock ? null : onAddToCart,
                          child: Padding(
                            padding: const EdgeInsets.all(8.0),
                            child: Icon(
                              isOutOfStock
                                  ? Icons.remove_shopping_cart_rounded
                                  : Icons.add_shopping_cart_rounded,
                              size: 16,
                              color: isOutOfStock
                                  ? (isDark
                                      ? Colors.white38
                                      : const Color(0xFF64748B))
                                  : Colors.white,
                            ),
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
          const SizedBox(height: 8),

          // Title
          Text(
            name,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: theme.textTheme.titleMedium?.copyWith(
              fontSize: 14,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 2),

          // Price
          Text(
            '\$${price.toStringAsFixed(2)}',
            style: theme.textTheme.bodyMedium?.copyWith(
              fontSize: 14,
              fontWeight: FontWeight.bold,
              color: theme.colorScheme.primary,
            ),
          ),
        ],
      ),
    );
  }
}
