import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';

import 'api.dart';
import 'product_info_page.dart';
import 'widgets/ai_assistant_fab.dart';

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

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() => _loading = true);
    try {
      final storeRes = await Api.get('/api/shops/${widget.storeUserId}');
      final prodRes = await Api.get('/api/products/approved?storeUserId=${widget.storeUserId}');

      if (!mounted) return;
      setState(() {
        _store = storeRes;
        final items = prodRes['data'] is List ? prodRes['data'] : [];
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
    if (_loading && _store == null) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    if (_store == null) {
      return Scaffold(
        appBar: AppBar(title: Text('Store Profile'.tr())),
        body: Center(child: Text('Store not found'.tr())),
      );
    }

    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final onSurface = colorScheme.onSurface;
    final secondaryText = onSurface.withValues(alpha: 0.65);
    final isOpen = _isStoreOpen(
      _store!['openingTime']?.toString(),
      _store!['closingTime']?.toString(),
    );
    final avatarUrl = _store!['profilePictureUrl']?.toString();
    final storeInitial = (_store!['shopName']?.toString().isNotEmpty ?? false)
        ? _store!['shopName']![0].toUpperCase()
        : 'S';

    final location = _store!['location']?.toString().isNotEmpty == true
        ? _store!['location']
        : 'Türkiye';

    return Scaffold(
      backgroundColor: theme.brightness == Brightness.dark ? const Color(0xFF1A0A00) : Colors.white,
      floatingActionButton: const AiAssistantFab(),
      body: RefreshIndicator(
        onRefresh: _loadData,
        child: CustomScrollView(
          slivers: [
            SliverAppBar(
              expandedHeight: 180,
              pinned: true,
              backgroundColor: const Color(0xFFD85A30),
              iconTheme: const IconThemeData(color: Colors.white),
              flexibleSpace: FlexibleSpaceBar(
                background: Stack(
                  fit: StackFit.expand,
                  children: [
                    Container(
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                          colors: theme.brightness == Brightness.dark 
                              ? const [Color(0xFFFAF0EB), Color(0xFFF0997B), Color(0xFFD85A30), Color(0xFF993C1D)]
                              : const [Color(0xFF1A0A00), Color(0xFF4A1B0C), Color(0xFF993C1D), Color(0xFFD85A30)],
                        ),
                      ),
                    ),
                    Opacity(
                      opacity: 0.08,
                      child: GridView.builder(
                        physics: const NeverScrollableScrollPhysics(),
                        gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: 15,
                          mainAxisSpacing: 24,
                          crossAxisSpacing: 24,
                        ),
                        itemCount: 150,
                        itemBuilder: (_, __) => const Icon(
                          Icons.circle,
                          size: 3,
                          color: Colors.white,
                        ),
                      ),
                    ),
                    Positioned(
                      top: MediaQuery.of(context).padding.top + 10,
                      right: 16,
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
                        decoration: BoxDecoration(
                          color: isOpen 
                              ? (theme.brightness == Brightness.dark ? const Color(0xFF223A07) : const Color(0xFFEAF3DE))
                              : (theme.brightness == Brightness.dark ? const Color(0xFF4A1414) : const Color(0xFFFCEBEB)),
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: Text(
                          '• ${isOpen ? 'OPEN'.tr() : 'CLOSED'.tr()}',
                          style: GoogleFonts.dmSans(
                            fontSize: 11,
                            fontWeight: FontWeight.w500,
                            letterSpacing: 0.8,
                            color: isOpen
                                ? (theme.brightness == Brightness.dark ? const Color(0xFFEAF3DE) : const Color(0xFF3B6D11))
                                : (theme.brightness == Brightness.dark ? const Color(0xFFFCEBEB) : const Color(0xFFA32D2D)),
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              actions: [
                if (widget.cartCount != null)
                  Padding(
                    padding: const EdgeInsets.only(right: 16),
                    child: IconButton(
                      icon: Badge(
                        label: Text(widget.cartCount.toString()),
                        child: const Icon(Icons.shopping_cart_outlined),
                      ),
                      onPressed: widget.onNavigateCart,
                    ),
                  ),
              ],
            ),
            SliverToBoxAdapter(
              child: Stack(
                clipBehavior: Clip.none,
                children: [
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.fromLTRB(24, 60, 24, 24),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          _store!['shopName'] ?? '',
                          style: GoogleFonts.playfairDisplay(
                            fontSize: 26,
                            fontWeight: FontWeight.w600,
                            color: onSurface,
                          ),
                        ),
                        if (_store!['openingTime'] != null) ...[
                          const SizedBox(height: 6),
                          Text(
                            '${'Opens'.tr()} ${_store!['openingTime']} · ${'Closes'.tr()} ${_store!['closingTime']}',
                            style: GoogleFonts.dmSans(fontSize: 13, color: secondaryText),
                          ),
                        ],
                        const SizedBox(height: 16),
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Row(
                                  children: [
                                    const Icon(Icons.location_on, size: 14, color: Color(0xFFF0997B)),
                                    const SizedBox(width: 6),
                                    Text(
                                      'LOCATION'.tr(),
                                      style: GoogleFonts.dmSans(
                                        fontSize: 11,
                                        fontWeight: FontWeight.w500,
                                        letterSpacing: 1.0,
                                        color: onSurface.withValues(alpha: 0.55),
                                      ),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  location,
                                  style: GoogleFonts.dmSans(
                                    fontSize: 14,
                                    fontWeight: FontWeight.w500,
                                    color: onSurface,
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                        if ((_store!['description']?.toString() ?? '').isNotEmpty) ...[
                          const SizedBox(height: 16),
                          Text(
                            _store!['description'],
                            style: GoogleFonts.dmSans(
                              fontSize: 13,
                              height: 1.6,
                              color: secondaryText,
                            ),
                          ),
                        ],
                        const SizedBox(height: 24),
                        Divider(height: 1, thickness: 0.5, color: onSurface.withValues(alpha: 0.08)),
                      ],
                    ),
                  ),
                  Positioned(
                    top: -44,
                    left: 24,
                    child: Container(
                      width: 88,
                      height: 88,
                      padding: const EdgeInsets.all(3),
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        gradient: const LinearGradient(
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                          colors: [
                            Color(0xFFF0997B),
                            Color(0xFFD85A30),
                            Color(0xFF4A1B0C),
                          ],
                        ),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withValues(alpha: 0.1),
                            blurRadius: 12,
                            offset: const Offset(0, 4),
                          ),
                        ],
                      ),
                      child: Container(
                        decoration: const BoxDecoration(
                          shape: BoxShape.circle,
                          color: Colors.white,
                        ),
                        clipBehavior: Clip.antiAlias,
                        child: avatarUrl != null
                            ? Image.network(
                                avatarUrl.startsWith('http')
                                    ? avatarUrl
                                    : '${Api.baseUrl}$avatarUrl',
                                fit: BoxFit.cover,
                              )
                            : Center(
                                child: Text(
                                  storeInitial,
                                  style: GoogleFonts.playfairDisplay(
                                    fontSize: 32,
                                    fontWeight: FontWeight.bold,
                                    color: const Color(0xFFD85A30),
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
                  style: GoogleFonts.dmSans(
                    fontSize: 11,
                    fontWeight: FontWeight.w500,
                    letterSpacing: 1.0,
                    color: onSurface.withValues(alpha: 0.55),
                  ),
                ),
              ),
            ),
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(24, 12, 24, 40),
              sliver: _products.isEmpty
                  ? SliverToBoxAdapter(
                      child: Center(
                        child: Padding(
                          padding: const EdgeInsets.all(40),
                          child: Text('This store has no products yet.'.tr(), style: const TextStyle(color: Colors.grey)),
                        ),
                      ),
                    )
                  : SliverGrid(
                      gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
                        maxCrossAxisExtent: 220,
                        mainAxisExtent: 100,
                        mainAxisSpacing: 10,
                        crossAxisSpacing: 10,
                      ),
                      delegate: SliverChildBuilderDelegate(
                        (ctx, i) {
                          final product = _products[i];
                          return _ProductCardMinimal(
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
      ),
    );
  }
}

class _ProductCardMinimal extends StatelessWidget {
  final Map<String, dynamic> product;
  final Function(int)? onAddToCart;
  final VoidCallback? onNavigateCart;
  final int? cartCount;

  const _ProductCardMinimal({
    required this.product,
    this.onAddToCart,
    this.onNavigateCart,
    this.cartCount,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final price = (product['price'] as num?)?.toDouble() ?? 0.0;

    return InkWell(
      onTap: () {
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => ProductInfoPage(
              productId: product['id'],
              onAddToCart: onAddToCart ?? (id) {},
              onNavigateCart: onNavigateCart ?? () {},
              cartCount: cartCount ?? 0,
            ),
          ),
        );
      },
      borderRadius: BorderRadius.circular(12),
      child: Container(
        decoration: BoxDecoration(
          color: isDark ? const Color(0xFF2C1A10) : const Color(0xFFF9F5F3),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: theme.colorScheme.onSurface.withValues(alpha: 0.08),
            width: 0.5,
          ),
        ),
        padding: const EdgeInsets.all(14),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    product['name'] ?? '',
                    style: GoogleFonts.dmSans(
                      fontWeight: FontWeight.w500,
                      fontSize: 13,
                      color: theme.colorScheme.onSurface,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  if ((product['description']?.toString() ?? '').isNotEmpty) ...[
                    const SizedBox(height: 4),
                    Text(
                      product['description'],
                      style: GoogleFonts.dmSans(
                        fontSize: 11,
                        color: theme.colorScheme.onSurface.withValues(alpha: 0.55),
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                  const Spacer(),
                  Text(
                    '\$${price.toStringAsFixed(2)}',
                    style: GoogleFonts.dmSans(
                      color: isDark ? const Color(0xFFD85A30) : const Color(0xFF993C1D),
                      fontWeight: FontWeight.w500,
                      fontSize: 14,
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
