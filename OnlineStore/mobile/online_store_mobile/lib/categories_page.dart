import 'dart:async';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'widgets/ripple_avatar.dart';
import 'widgets/custom_store_app_bar.dart';
import 'widgets/skeleton_loader.dart';
import 'widgets/ai_assistant_fab.dart';

class CategoriesPage extends StatefulWidget {
  final VoidCallback onLogout;
  final int cartCount;
  final VoidCallback onNavigateCart;
  final Function(int) onAddToCart;

  const CategoriesPage({
    super.key,
    required this.onLogout,
    required this.cartCount,
    required this.onNavigateCart,
    required this.onAddToCart,
  });

  @override
  State<CategoriesPage> createState() => _CategoriesPageState();
}

class _CategoriesPageState extends State<CategoriesPage> {
  final PageController _pageCtrl = PageController();
  int _adIndex = 0;

  // Sidebar state
  String _userName = 'My User Profile';
  File? _avatarFile;
  String? _avatarUrl;
  String? _accountType;
  bool _isGuest = false;

  // Categories from API
  List<Map<String, dynamic>> _categories = [];
  bool _loadingCategories = true;

  // Dynamic ad banners from API
  List<Map<String, dynamic>> _adBanners = [];
  bool _loadingAds = true;

  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _loadAccountType().then((_) {
      if (!_isGuest) _fetchProfile();
    });
    _fetchCategories();
    _fetchAds();
  }

  Future<void> _loadAccountType() async {
    final prefs = await SharedPreferences.getInstance();
    if (mounted) {
      setState(() {
        _accountType = prefs.getString('accountType');
        _isGuest = prefs.getBool('isGuest') ?? false;
      });
    }
  }

  Future<void> _fetchProfile() async {
    try {
      final res = await Api.get('/api/me');
      if (mounted) {
        setState(() {
          _userName =
              (res['fullName'] != null && res['fullName'].toString().isNotEmpty)
                  ? res['fullName']
                  : ((res['shopName'] != null &&
                          res['shopName'].toString().isNotEmpty)
                      ? res['shopName']
                      : 'User');
          _avatarUrl = res['profilePictureUrl'];
        });
      }
    } catch (e) {
      // ignore
    }
  }

  Future<void> _fetchCategories() async {
    try {
      final res = await Api.get(
          '/api/categories?_=${DateTime.now().millisecondsSinceEpoch}');
      final data = res['data'];
      if (mounted && data is List) {
        setState(() {
          _categories = data
              .map((c) => {
                    'id': c['id'].toString(),
                    'name': c['name'].toString(),
                    'imageUrl': c['imageUrl'],
                  })
              .toList();
          _loadingCategories = false;
        });
      }
    } catch (e) {
      if (mounted) setState(() => _loadingCategories = false);
    }
  }

  Future<void> _fetchAds() async {
    try {
      final res =
          await Api.get('/api/ads?_=${DateTime.now().millisecondsSinceEpoch}');
      final data = res is List ? res : (res['data'] is List ? res['data'] : []);
      if (mounted && data is List) {
        setState(() {
          _adBanners = data.map((a) => Map<String, dynamic>.from(a)).toList();
          _loadingAds = false;
        });
        if (_adBanners.isNotEmpty) _startAutoScroll();
      }
    } catch (_) {
      if (mounted) setState(() => _loadingAds = false);
    }
  }

  void _startAutoScroll() {
    _timer?.cancel();
    if (_adBanners.isEmpty) return;
    _timer = Timer.periodic(const Duration(seconds: 5), (timer) {
      if (!mounted || _adBanners.isEmpty) return;
      final next = (_adIndex + 1) % _adBanners.length;
      _pageCtrl.animateToPage(
        next,
        duration: const Duration(milliseconds: 600),
        curve: Curves.easeInOut,
      );
    });
  }

  void _goToAd(int index) {
    if (!mounted) return;
    _pageCtrl.animateToPage(
      index,
      duration: const Duration(milliseconds: 400),
      curve: Curves.easeInOut,
    );
    _startAutoScroll();
  }

  void _prevAd() {
    if (_adBanners.isEmpty) return;
    _goToAd((_adIndex - 1 + _adBanners.length) % _adBanners.length);
  }

  void _nextAd() {
    if (_adBanners.isEmpty) return;
    _goToAd((_adIndex + 1) % _adBanners.length);
  }

  Future<void> _openAdLink(String? url) async {
    if (url == null || url.isEmpty) return;
    final uri = Uri.tryParse(url);
    if (uri != null && await canLaunchUrl(uri)) {
      await launchUrl(uri, mode: LaunchMode.externalApplication);
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    _pageCtrl.dispose();
    super.dispose();
  }

  Future<void> _handleAvatarTapped() async {
    try {
      final picker = ImagePicker();
      final pickedFile = await picker.pickImage(source: ImageSource.gallery);
      if (pickedFile != null) {
        final file = File(pickedFile.path);
        setState(() {
          _avatarFile = file;
          _avatarUrl = null;
        });
        try {
          final res = await Api.upload('/api/me/profile-picture', file);
          if (mounted) {
            setState(() {
              _avatarUrl = res['profilePictureUrl'];
            });
          }
        } catch (e) {
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text('${'Upload failed:'.tr()} $e')),
            );
          }
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${'Failed to pick image:'.tr()} $e')),
        );
      }
    }
  }

  Widget _buildDrawerItem({
    required IconData icon,
    required String title,
    required VoidCallback onTap,
    Color? color,
  }) {
    return ListTile(
      leading: Icon(icon, color: color),
      title: Text(title, style: color != null ? TextStyle(color: color) : null),
      onTap: onTap,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: CustomStoreAppBar(
        isRoot: true,
        cartCount: widget.cartCount,
        onCartTap: widget.onNavigateCart,
        onSearchTap: () {
          Navigator.pushNamed(context, '/search');
        },
      ),
      floatingActionButton: const AiAssistantFab(),
      drawer: Drawer(
        child: Column(
          children: [
            Container(
              width: double.infinity,
              padding: const EdgeInsets.fromLTRB(24, 60, 24, 20),
              decoration: const BoxDecoration(
                color: Color(0xFFF9F5F3), // Surface color
                border: Border(
                    bottom: BorderSide(color: Color(0x14000000), width: 0.5)),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  Container(
                    width: 80,
                    height: 80,
                    padding: const EdgeInsets.all(3),
                    decoration: const BoxDecoration(
                      shape: BoxShape.circle,
                      gradient: LinearGradient(
                        colors: [
                          Color(0xFFF0997B),
                          Color(0xFFD85A30),
                          Color(0xFF4a1b0c)
                        ],
                      ),
                    ),
                    child: RippleAvatar(
                      size: 74,
                      image: _avatarFile != null
                          ? FileImage(_avatarFile!)
                          : (_avatarUrl != null
                              ? NetworkImage('${Api.baseUrl}$_avatarUrl')
                              : null) as ImageProvider?,
                      onTap: _handleAvatarTapped,
                    ),
                  ),
                  const SizedBox(height: 16),
                  Text(
                    _userName,
                    textAlign: TextAlign.center,
                    style: GoogleFonts.playfairDisplay(
                      fontWeight: FontWeight.w600,
                      fontSize: 20,
                      color: const Color(0xFF1A0A00),
                    ),
                  ),
                  if (_accountType != null) ...[
                    const SizedBox(height: 6),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: const Color(0xFFFAECE7),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Text(
                        _accountType!.toUpperCase(),
                        textAlign: TextAlign.center,
                        style: GoogleFonts.dmSans(
                          color: const Color(0xFF993C1D),
                          fontSize: 10,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.2,
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),

            // ── Common links ──
            _buildDrawerItem(
              icon: Icons.home_rounded,
              title: 'Home Page'.tr(),
              onTap: () => Navigator.of(context).pop(),
            ),

            if (!_isGuest) ...[
              _buildDrawerItem(
                icon: Icons.person_rounded,
                title: 'Profile Page'.tr(),
                onTap: () async {
                  Navigator.of(context).pop();
                  await Navigator.of(context).pushNamed('/profile');
                  _fetchProfile();
                },
              ),

              // ── Role-specific links ──
              if (_accountType == 'Shop') ...[
                _buildDrawerItem(
                  icon: Icons.inventory_2_rounded,
                  title: 'My Products'.tr(),
                  onTap: () {
                    Navigator.of(context).pop();
                    Navigator.of(context).pushNamed('/store-products');
                  },
                ),
                _buildDrawerItem(
                  icon: Icons.receipt_long_rounded,
                  title: 'Store Orders'.tr(),
                  onTap: () {
                    Navigator.of(context).pop();
                    Navigator.of(context).pushNamed('/store-orders');
                  },
                ),
                _buildDrawerItem(
                  icon: Icons.analytics_rounded,
                  title: 'Store Analytics'.tr(),
                  onTap: () {
                    Navigator.of(context).pop();
                    Navigator.of(context).pushNamed('/store-analytics');
                  },
                ),
              ],

              if (_accountType == 'Delivery')
                _buildDrawerItem(
                  icon: Icons.local_shipping_rounded,
                  title: 'Delivery Jobs'.tr(),
                  onTap: () {
                    Navigator.of(context).pop();
                    Navigator.of(context).pushNamed('/delivery-jobs');
                  },
                ),

              if (_accountType == 'Admin')
                _buildDrawerItem(
                  icon: Icons.admin_panel_settings_rounded,
                  title: 'Admin Panel'.tr(),
                  onTap: () async {
                    Navigator.of(context).pop();
                    await Navigator.of(context).pushNamed('/admin');
                    // Re-fetch ads and categories when returning from admin
                    _fetchAds();
                    _fetchCategories();
                  },
                ),

              // ── Customer-only links ──
              if (_accountType == 'User') ...[
                _buildDrawerItem(
                  icon: Icons.favorite_rounded,
                  title: 'Favorites'.tr(),
                  onTap: () {
                    Navigator.of(context).pop();
                    Navigator.of(context).pushNamed('/favorites');
                  },
                ),
                _buildDrawerItem(
                  icon: Icons.track_changes_rounded,
                  title: 'Active Orders'.tr(),
                  onTap: () {
                    Navigator.of(context).pop();
                    Navigator.of(context).pushNamed('/active-orders');
                  },
                ),
                _buildDrawerItem(
                  icon: Icons.local_shipping_rounded,
                  title: 'Delivered Orders'.tr(),
                  onTap: () {
                    Navigator.of(context).pop();
                    Navigator.of(context).pushNamed('/delivered-orders');
                  },
                ),
              ],

              _buildDrawerItem(
                icon: Icons.notifications_rounded,
                title: 'Notifications'.tr(),
                onTap: () {
                  Navigator.of(context).pop();
                  Navigator.of(context).pushNamed('/notifications');
                },
              ),

              _buildDrawerItem(
                icon: Icons.settings_rounded,
                title: 'Settings'.tr(),
                onTap: () {
                  Navigator.of(context).pop();
                  Navigator.of(context).pushNamed('/settings');
                },
              ),
            ],

            const Spacer(),
            const Divider(),
            SafeArea(
              top: false,
              child: _buildDrawerItem(
                icon: _isGuest ? Icons.login_rounded : Icons.logout_rounded,
                title: _isGuest ? 'Sign In'.tr() : 'Log out'.tr(),
                onTap: widget.onLogout,
                color: _isGuest
                    ? Theme.of(context).colorScheme.primary
                    : Colors.redAccent,
              ),
            ),
          ],
        ),
      ),
      body: SingleChildScrollView(
        child: Column(
          children: [
            // Ads carousel
            if (_loadingAds)
              Shimmer(
                child: Padding(
                  padding: const EdgeInsets.all(0),
                  child: Skeleton(
                      height: 200,
                      width: MediaQuery.of(context).size.width,
                      borderRadius: BorderRadius.zero),
                ),
              )
            else if (_adBanners.isNotEmpty)
              SizedBox(
                height: 200,
                child: Stack(
                  children: [
                    PageView.builder(
                      controller: _pageCtrl,
                      onPageChanged: (i) => setState(() => _adIndex = i),
                      itemCount: _adBanners.length,
                      itemBuilder: (ctx, i) {
                        final ad = _adBanners[i];
                        final imgUrl = ad['imageUrl']?.toString() ?? '';
                        final fullUrl = imgUrl.startsWith('http')
                            ? imgUrl
                            : '${Api.baseUrl}$imgUrl';
                        return GestureDetector(
                          onTap: () => _openAdLink(ad['linkUrl']?.toString()),
                          child: Image.network(
                            fullUrl,
                            fit: BoxFit.cover,
                            width: double.infinity,
                            cacheWidth:
                                800, // Optimize memory for horizontal ads
                            errorBuilder: (_, __, ___) => Container(
                              color: Colors.grey.shade300,
                              child: const Center(
                                  child: Icon(Icons.image,
                                      size: 48, color: Colors.grey)),
                            ),
                          ),
                        );
                      },
                    ),
                    Positioned(
                      left: 8,
                      top: 0,
                      bottom: 0,
                      child: Center(
                        child: Container(
                          decoration: BoxDecoration(
                              color: Colors.black.withAlpha(100),
                              shape: BoxShape.circle),
                          child: IconButton(
                              icon: const Icon(Icons.chevron_left,
                                  color: Colors.white),
                              onPressed: _prevAd),
                        ),
                      ),
                    ),
                    Positioned(
                      right: 8,
                      top: 0,
                      bottom: 0,
                      child: Center(
                        child: Container(
                          decoration: BoxDecoration(
                              color: Colors.black.withAlpha(100),
                              shape: BoxShape.circle),
                          child: IconButton(
                              icon: const Icon(Icons.chevron_right,
                                  color: Colors.white),
                              onPressed: _nextAd),
                        ),
                      ),
                    ),
                  ],
                ),
              ),

            // Dots
            if (_adBanners.isNotEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 10),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: List.generate(
                    _adBanners.length,
                    (i) => GestureDetector(
                      onTap: () => _goToAd(i),
                      child: Container(
                        width: 10,
                        height: 10,
                        margin: const EdgeInsets.symmetric(horizontal: 4),
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: _adIndex == i
                              ? Theme.of(context).colorScheme.primary
                              : Theme.of(context)
                                  .colorScheme
                                  .onSurface
                                  .withAlpha(60),
                        ),
                      ),
                    ),
                  ),
                ),
              ),

            // Section title
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: Text(
                'Shop by Category'.tr(),
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
              ),
            ),

            // Category grid
            if (_loadingCategories)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: Shimmer(
                  child: GridView.count(
                    crossAxisCount: 2,
                    mainAxisSpacing: 14,
                    crossAxisSpacing: 14,
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    childAspectRatio: 0.85,
                    children: List.generate(
                      4,
                      (i) => Card(
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(28)),
                        elevation: 4,
                        shadowColor: Colors.black26,
                        child: Column(
                          children: [
                            const Expanded(
                                child: Skeleton(
                                    borderRadius: BorderRadius.only(
                                        topLeft: Radius.circular(28),
                                        topRight: Radius.circular(28)))),
                            Padding(
                              padding: const EdgeInsets.all(12),
                              child: Center(
                                  child: Skeleton(
                                      height: 15,
                                      width: 80,
                                      borderRadius: BorderRadius.circular(4))),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              )
            else if (_categories.isEmpty)
              Padding(
                padding: const EdgeInsets.all(40),
                child: Text('No categories yet.'.tr(),
                    style: const TextStyle(color: Colors.grey)),
              )
            else
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: GridView.count(
                  crossAxisCount: 2,
                  mainAxisSpacing: 14,
                  crossAxisSpacing: 14,
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  childAspectRatio: 0.85,
                  children: _categories
                      .map((cat) => _CategoryCard(
                            id: cat['id']!,
                            name: cat['name']!,
                            imageUrl: cat['imageUrl'],
                          ))
                      .toList(),
                ),
              ),

            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }
}

class _CategoryCard extends StatelessWidget {
  final String id;
  final String name;
  final String? imageUrl;

  const _CategoryCard({
    required this.id,
    required this.name,
    this.imageUrl,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: const BorderSide(color: Color(0x14000000), width: 0.5),
      ),
      color: const Color(0xFFF9F5F3),
      child: InkWell(
        onTap: () {
          if (kDebugMode) print('Category tapped: $id ($name)');
          Navigator.pushNamed(
            context,
            '/category-products',
            arguments: <String, dynamic>{'id': id, 'name': name},
          );
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Expanded(
              child: Container(
                decoration: const BoxDecoration(
                  color: Color(0xFFF0E9E6),
                ),
                child: imageUrl != null
                    ? Image.network(
                        imageUrl!.startsWith('http')
                            ? imageUrl!
                            : '${Api.baseUrl}$imageUrl',
                        fit: BoxFit.cover,
                        cacheWidth: 300, // Optimize memory
                        errorBuilder: (_, __, ___) => _placeholder(context),
                      )
                    : _placeholder(context),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Text(
                name,
                textAlign: TextAlign.center,
                style: GoogleFonts.dmSans(
                  fontWeight: FontWeight.w500,
                  fontSize: 14,
                  color: const Color(0xFF1A0A00),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _placeholder(BuildContext ctx) {
    return Container(
      color: const Color(0xFFF0E9E6),
      child: const Icon(Icons.category_rounded,
          size: 40, color: Color(0xFFA8948D)),
    );
  }
}
