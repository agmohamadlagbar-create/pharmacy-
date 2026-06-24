import 'package:flutter/material.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'public_store_page.dart';

class CartPage extends StatefulWidget {
  final Map<String, dynamic> cart;
  final Function(int productId, int quantity) onUpdateQuantity;
  final Function(int productId) onRemoveItem;
  final VoidCallback onCheckout;

  const CartPage({
    super.key,
    required this.cart,
    required this.onUpdateQuantity,
    required this.onRemoveItem,
    required this.onCheckout,
  });

  @override
  State<CartPage> createState() => _CartPageState();
}

class _CartPageState extends State<CartPage> {
  @override
  Widget build(BuildContext context) {
    final items = (widget.cart['items'] as List?) ?? [];
    final total = widget.cart['totalAmount'] ?? 0.0;

    return Scaffold(
      appBar: AppBar(
        title: Text('My Cart'.tr()),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: items.isEmpty
          ? _buildEmptyCart()
          : Column(
              children: [
                Expanded(
                  child: ListView.separated(
                    padding: const EdgeInsets.all(16),
                    itemCount: items.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 12),
                    itemBuilder: (context, index) {
                      final item = items[index];
                      return _buildCartItem(item);
                    },
                  ),
                ),
                _buildSummary(total),
              ],
            ),
    );
  }

  Widget _buildEmptyCart() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.shopping_cart_outlined,
              size: 80, color: Colors.grey.withValues(alpha: 0.5)),
          const SizedBox(height: 16),
          Text(
            'Your cart is empty'.tr(),
            style: TextStyle(
                fontSize: 18, color: Colors.grey.shade600),
          ),
          const SizedBox(height: 24),
          ElevatedButton(
            onPressed: () => Navigator.pop(context),
            child: Text('Start Shopping'.tr()),
          ),
        ],
      ),
    );
  }

  Widget _buildCartItem(Map<String, dynamic> item) {
    final productId = item['productId'];
    final productName = item['productName'] ?? 'Unknown';
    final price = item['price'] ?? 0.0;
    final quantity = item['quantity'] ?? 1;
    final imageUrl = item['imageUrl'];
    final storeName = item['storeName'];

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            Container(
              width: 70,
              height: 70,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(12),
                color: Colors.grey.withValues(alpha: 0.1),
              ),
              clipBehavior: Clip.antiAlias,
              child: imageUrl != null
                  ? Image.network(
                      imageUrl.startsWith('http')
                          ? imageUrl
                          : '${Api.baseUrl}$imageUrl',
                      fit: BoxFit.cover,
                    )
                  : const Icon(Icons.image, color: Colors.black26),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    productName,
                    style: const TextStyle(
                        fontWeight: FontWeight.bold, fontSize: 16),
                  ),
                  if (storeName != null && storeName.toString().isNotEmpty)
                    InkWell(
                      onTap: () {
                        if (item['storeUserId'] != null) {
                          Navigator.push(context, MaterialPageRoute(builder: (_) => PublicStorePage(
                            storeUserId: item['storeUserId'],
                          )));
                        }
                      },
                      child: Padding(
                        padding: const EdgeInsets.symmetric(vertical: 2),
                        child: Row(
                          children: [
                            Icon(Icons.store,
                                size: 11,
                                color: Theme.of(context).colorScheme.primary),
                            const SizedBox(width: 3),
                            Flexible(
                              child: Text(
                                '${'from'.tr()} $storeName',
                                style: TextStyle(
                                  color: Theme.of(context).colorScheme.primary,
                                  fontSize: 11,
                                  decoration: TextDecoration.underline,
                                  decorationColor: Theme.of(context).colorScheme.primary,
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  const SizedBox(height: 4),
                  Text(
                    '\$${price.toStringAsFixed(2)}',
                    style: TextStyle(
                        color: Theme.of(context).colorScheme.primary,
                        fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 10),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Container(
                        decoration: BoxDecoration(
                          color: Colors.black12,
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: Row(
                          children: [
                            IconButton(
                              icon: const Icon(Icons.remove, size: 18),
                              onPressed: quantity > 1
                                  ? () => widget.onUpdateQuantity(
                                      productId, quantity - 1)
                                  : null,
                            ),
                            Text('$quantity',
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                            IconButton(
                              icon: const Icon(Icons.add, size: 18),
                              onPressed: () => widget.onUpdateQuantity(
                                  productId, quantity + 1),
                            ),
                          ],
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.delete_outline,
                            color: Colors.redAccent),
                        onPressed: () => widget.onRemoveItem(productId),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSummary(double total) {
    return Container(
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        color: Theme.of(context).cardTheme.color,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(30)),
        border: Border(top: BorderSide(color: Colors.grey.withValues(alpha: 0.2))),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Total'.tr(),
                  style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              Text(
                '\$${total.toStringAsFixed(2)}',
                style: TextStyle(
                  fontSize: 22,
                  fontWeight: FontWeight.bold,
                  color: Theme.of(context).colorScheme.primary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 24),
          SizedBox(
            width: double.infinity,
            child: FilledButton(
              style: FilledButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(16)),
              ),
              onPressed: widget.onCheckout,
              child: Text('Proceed to Checkout'.tr(),
                  style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            ),
          ),
        ],
      ),
    );
  }
}

