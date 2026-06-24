import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'public_store_page.dart';
import 'favorites_manager.dart';

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
          title: Text('Add Review'.tr(), style: GoogleFonts.playfairDisplay(fontWeight: FontWeight.w600)),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: List.generate(5, (index) => IconButton(
                  icon: Icon(
                    index < localRating ? Icons.star : Icons.star_border,
                    color: const Color(0xFFD85A30),
                  ),
                  onPressed: () => setDialogState(() => localRating = index + 1),
                )),
              ),
              TextField(
                controller: commentController,
                maxLines: 3,
                decoration: InputDecoration(
                  hintText: 'Your comment...'.tr(),
                  hintStyle: GoogleFonts.dmSans(color: const Color(0xFFA8948D)),
                  filled: true,
                  fillColor: const Color(0xFFF9F5F3),
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: BorderSide.none),
                ),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('Cancel'.tr(), style: GoogleFonts.dmSans(color: const Color(0xFF6B5C55))),
            ),
            ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFFD85A30),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
              onPressed: () {
                if (commentController.text.trim().isNotEmpty) {
                  _submitReview(localRating, commentController.text.trim());
                  Navigator.pop(context);
                }
              },
              child: Text('Submit'.tr(), style: GoogleFonts.dmSans(color: Colors.white, fontWeight: FontWeight.w600)),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Color(0xFF1A0A00)),
          onPressed: () => Navigator.pop(context),
        ),
        actions: [
          IconButton(
            icon: AnimatedSwitcher(
              duration: const Duration(milliseconds: 300),
              transitionBuilder: (child, anim) => ScaleTransition(scale: anim, child: child),
              child: Icon(
                _isFavorite ? Icons.favorite : Icons.favorite_border,
                key: ValueKey(_isFavorite),
                color: _isFavorite ? Colors.red : const Color(0xFF1A0A00),
              ),
            ),
            onPressed: _toggleFavorite,
          ),
          IconButton(
            icon: Badge(
              label: Text('${widget.cartCount ?? 0}'),
              child: const Icon(Icons.shopping_cart_outlined, color: Color(0xFF1A0A00)),
            ),
            onPressed: widget.onNavigateCart,
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: Color(0xFFD85A30)))
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.error_outline, size: 48, color: Colors.redAccent),
                      const SizedBox(height: 12),
                      Text(_error!),
                      const SizedBox(height: 16),
                      ElevatedButton(onPressed: _loadDetails, child: Text('Retry'.tr())),
                    ],
                  ),
                )
              : _buildContent(context),
      bottomNavigationBar: _details == null ? null : _buildBottomBar(),
    );
  }

  Widget _buildBottomBar() {
    final inStock = _details!['inStock'] ?? 0;
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.05), blurRadius: 10, offset: const Offset(0, -5))],
      ),
      child: SafeArea(
        child: ElevatedButton(
          onPressed: inStock > 0 ? () => widget.onAddToCart?.call(widget.productId) : null,
          style: ElevatedButton.styleFrom(
            backgroundColor: const Color(0xFFD85A30),
            disabledBackgroundColor: const Color(0xFFA8948D).withValues(alpha: 0.3),
            padding: const EdgeInsets.symmetric(vertical: 16),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            elevation: 0,
          ),
          child: Text(
            inStock > 0 ? 'ADD TO CART'.tr() : 'OUT OF STOCK'.tr(), 
            style: GoogleFonts.dmSans(fontSize: 15, fontWeight: FontWeight.bold, letterSpacing: 1.2, color: Colors.white),
          ),
        ),
      ),
    );
  }

  Widget _buildContent(BuildContext context) {
    final p = _details!['product'] ?? {};
    final inStock = _details!['inStock'] ?? 0;
    final avgRating = (_details!['averageRating'] ?? 0).toDouble();
    final totalReviews = _details!['totalReviews'] ?? 0;

    List<String> allImages = [];
    if (p['imageUrl'] != null) allImages.add(p['imageUrl']);
    if (p['additionalImageUrls'] != null) {
      allImages.addAll(List<String>.from(p['additionalImageUrls']));
    }

    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Image Gallery
          Container(
            height: 350,
            color: const Color(0xFFF9F5F3),
            child: Stack(
              children: [
                PageView.builder(
                  itemCount: allImages.length,
                  onPageChanged: (idx) => setState(() => _activeImageIndex = idx),
                  itemBuilder: (ctx, i) => Image.network(
                    allImages[i].startsWith('http') ? allImages[i] : '${Api.baseUrl}${allImages[i]}',
                    fit: BoxFit.cover,
                    errorBuilder: (_, __, ___) => const Icon(Icons.image_not_supported, size: 80, color: Color(0xFFA8948D)),
                  ),
                ),
                if (allImages.length > 1)
                  Positioned(
                    bottom: 20,
                    left: 0,
                    right: 0,
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: List.generate(allImages.length, (idx) => Container(
                        width: 8, height: 8,
                        margin: const EdgeInsets.symmetric(horizontal: 4),
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: _activeImageIndex == idx ? const Color(0xFFD85A30) : const Color(0x33000000),
                        ),
                      )),
                    ),
                  ),
              ],
            ),
          ),

          Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        p['name'] ?? '',
                        style: GoogleFonts.playfairDisplay(fontSize: 28, fontWeight: FontWeight.w600, color: const Color(0xFF1A0A00)),
                      ),
                    ),
                    const SizedBox(width: 16),
                    Text(
                      '\$${(p['price'] ?? 0).toStringAsFixed(2)}',
                      style: GoogleFonts.dmSans(fontSize: 24, fontWeight: FontWeight.w700, color: const Color(0xFF993C1D)),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                if (p['storeName'] != null)
                  InkWell(
                    onTap: () {
                      if (p['storeUserId'] != null) {
                        Navigator.push(context, MaterialPageRoute(builder: (_) => PublicStorePage(
                          storeUserId: p['storeUserId'].toString(),
                          cartCount: widget.cartCount,
                          onNavigateCart: widget.onNavigateCart,
                          onAddToCart: widget.onAddToCart,
                        )));
                      }
                    },
                    child: Row(
                      children: [
                        const Icon(Icons.storefront_outlined, size: 18, color: Color(0xFFD85A30)),
                        const SizedBox(width: 8),
                        Text(
                          p['storeName'],
                          style: GoogleFonts.dmSans(color: const Color(0xFFD85A30), fontWeight: FontWeight.w600, decoration: TextDecoration.underline),
                        ),
                      ],
                    ),
                  ),
                const SizedBox(height: 24),
                Text(
                  'DESCRIPTION'.tr(),
                  style: GoogleFonts.dmSans(fontSize: 11, fontWeight: FontWeight.w600, letterSpacing: 1.2, color: const Color(0xFFA8948D)),
                ),
                const SizedBox(height: 8),
                Text(
                  p['description'] ?? 'No description available.'.tr(),
                  style: GoogleFonts.dmSans(fontSize: 15, height: 1.6, color: const Color(0xFF6B5C55)),
                ),
                const SizedBox(height: 32),
                
                // Tabs
                Row(
                  children: [
                    _buildTab('Details'.tr(), 0),
                    _buildTab('${'Reviews'.tr()} ($totalReviews)', 1),
                  ],
                ),
                const SizedBox(height: 20),
                _activeTabIndex == 0 ? _buildDetailsTab(p, inStock) : _buildReviewsTab(avgRating, totalReviews),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTab(String title, int idx) {
    final active = _activeTabIndex == idx;
    return GestureDetector(
      onTap: () => setState(() => _activeTabIndex = idx),
      child: Container(
        margin: const EdgeInsets.only(right: 24),
        padding: const EdgeInsets.only(bottom: 8),
        decoration: BoxDecoration(
          border: Border(bottom: BorderSide(color: active ? const Color(0xFFD85A30) : Colors.transparent, width: 2)),
        ),
        child: Text(
          title,
          style: GoogleFonts.dmSans(fontSize: 14, fontWeight: active ? FontWeight.bold : FontWeight.w500, color: active ? const Color(0xFF1A0A00) : const Color(0xFFA8948D)),
        ),
      ),
    );
  }

  Widget _buildDetailsTab(Map p, int inStock) {
    return Column(
      children: [
        _buildInfoRow('Category'.tr(), p['categoryName'] ?? 'N/A'),
        _buildInfoRow('Availability'.tr(), inStock > 0 ? '$inStock ${'pieces'.tr()}' : 'Out of Stock'.tr()),
        _buildInfoRow('Condition'.tr(), 'New Original'.tr()),
      ],
    );
  }

  Widget _buildInfoRow(String label, String val) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: GoogleFonts.dmSans(color: const Color(0xFF6B5C55))),
          Text(val, style: GoogleFonts.dmSans(fontWeight: FontWeight.w600, color: const Color(0xFF1A0A00))),
        ],
      ),
    );
  }

  Widget _buildReviewsTab(double avgRating, int totalReviews) {
    final latestReviews = _details!['latestReviews'] as List? ?? [];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Text(avgRating.toStringAsFixed(1), style: GoogleFonts.playfairDisplay(fontSize: 40, fontWeight: FontWeight.bold)),
            const SizedBox(width: 16),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: List.generate(5, (idx) => Icon(Icons.star, size: 16, color: idx < avgRating ? const Color(0xFFD85A30) : const Color(0xFFE5E0DE))),
                ),
                Text('Average Rating'.tr(), style: GoogleFonts.dmSans(fontSize: 12, color: const Color(0xFF6B5C55))),
              ],
            ),
            const Spacer(),
            TextButton(onPressed: _showReviewDialog, child: Text('Add Review'.tr())),
          ],
        ),
        const SizedBox(height: 24),
        ...latestReviews.map((r) => Container(
          margin: const EdgeInsets.only(bottom: 16),
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(color: const Color(0xFFF9F5F3), borderRadius: BorderRadius.circular(12)),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(r['userName'] ?? 'User'.tr(), style: GoogleFonts.dmSans(fontWeight: FontWeight.bold)),
                  Row(children: List.generate(5, (idx) => Icon(Icons.star, size: 12, color: idx < (r['rating'] ?? 0) ? const Color(0xFFD85A30) : const Color(0xFFE5E0DE)))),
                ],
              ),
              const SizedBox(height: 8),
              Text(r['comment'] ?? '', style: GoogleFonts.dmSans(color: const Color(0xFF6B5C55), height: 1.4)),
            ],
          ),
        )),
      ],
    );
  }
}

