import 'dart:async';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'widgets/ripple_avatar.dart';
import 'widgets/custom_store_app_bar.dart';
import 'widgets/skeleton_loader.dart';
import 'widgets/ai_assistant_fab.dart';
import 'widgets/stitch_category_card.dart';
import 'widgets/stitch_bottom_navigation_bar.dart';
import 'widgets/section_header.dart';
import 'widgets/status_chip.dart';

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
  // Sidebar & Profile state
  String _userName = 'My User Profile';
  File? _avatarFile;
  String? _avatarUrl;
  String? _accountType;
  bool _isGuest = false;

  // Categories state from Backend API
  List<Map<String, dynamic>> _categories = [];
  bool _loadingCategories = true;

  // Dynamic Ad Banners from Backend API
  List<Map<String, dynamic>> _adBanners = [];
  bool _loadingAds = true;
  final PageController _pageCtrl = PageController();
  int _adIndex = 0;
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
          _accountType = res['accountType'] ?? _accountType;
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
    } catch (_) {}
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
                    'imageUrl': Api.formatImageUrl(c['imageUrl']?.toString()),
                  })
              .toList();
          _loadingCategories = false;
        });
      } else {
        if (mounted) setState(() => _loadingCategories = false);
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
          _adBanners = data
              .map((a) => {
                    'id': a['id']?.toString(),
                    'imageUrl': Api.formatImageUrl(a['imageUrl']?.toString()),
                    'title': a['title']?.toString(),
                    'linkUrl': a['linkUrl']?.toString(),
                  })
              .toList();
          _loadingAds = false;
        });
        if (_adBanners.isNotEmpty) _startAutoScroll();
      } else {
        if (mounted) setState(() => _loadingAds = false);
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

  @override
  void dispose() {
    _timer?.cancel();
    _pageCtrl.dispose();
    super.dispose();
  }

  Widget _buildDrawerItem({
    required IconData icon,
    required String title,
    required VoidCallback onTap,
    Color? color,
  }) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final iconColor = color ?? colorScheme.onSurfaceVariant;
    final textColor = color ?? colorScheme.onSurface;

    return ListTile(
      leading: Icon(icon, color: iconColor, size: 22),
      title: Text(
        title,
        style: GoogleFonts.inter(
          color: textColor,
          fontSize: 15,
          fontWeight: FontWeight.w500,
        ),
      ),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      horizontalTitleGap: 12,
      contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 2),
      onTap: onTap,
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
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
        backgroundColor: theme.scaffoldBackgroundColor,
        child: Column(
          children: [
            // Drawer Header
            Container(
              width: double.infinity,
              padding: const EdgeInsets.fromLTRB(24, 60, 24, 24),
              decoration: BoxDecoration(
                color: colorScheme.surfaceContainer,
                border: Border(
                  bottom: BorderSide(
                    color: colorScheme.outlineVariant.withValues(alpha: 0.5),
                    width: 1,
                  ),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  Container(
                    width: 80,
                    height: 80,
                    padding: const EdgeInsets.all(3),
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      gradient: LinearGradient(
                        colors: [
                          colorScheme.primary,
                          colorScheme.primaryContainer,
                        ],
                      ),
                    ),
                    child: RippleAvatar(
                      size: 74,
                      image: _avatarFile != null
                          ? FileImage(_avatarFile!)
                          : (_avatarUrl != null && _avatarUrl!.trim().isNotEmpty
                              ? NetworkImage(Api.formatImageUrl(_avatarUrl))
                              : null) as ImageProvider?,
                      onTap: _handleAvatarTapped,
                    ),
                  ),
                  const SizedBox(height: 14),
                  Text(
                    _userName,
                    textAlign: TextAlign.center,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: GoogleFonts.inter(
                      fontWeight: FontWeight.bold,
                      fontSize: 18,
                      color: colorScheme.onSurface,
                    ),
                  ),
                  if (_accountType != null) ...[
                    const SizedBox(height: 8),
                    StatusChip(
                      label: _accountType!.toUpperCase(),
                      type: StatusChipType.info,
                      showDot: false,
                    ),
                  ],
                ],
              ),
            ),

            // Navigation links
            Expanded(
              child: ListView(
                padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 8),
                children: [
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
                          _fetchAds();
                          _fetchCategories();
                        },
                      ),
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
                ],
              ),
            ),

            Divider(color: colorScheme.outlineVariant.withValues(alpha: 0.5), height: 1),
            SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.all(8.0),
                child: _buildDrawerItem(
                  icon: _isGuest ? Icons.login_rounded : Icons.logout_rounded,
                  title: _isGuest ? 'Sign In'.tr() : 'Log out'.tr(),
                  onTap: widget.onLogout,
                  color: _isGuest ? colorScheme.primary : colorScheme.error,
                ),
              ),
            ),
          ],
        ),
      ),
      body: SingleChildScrollView(
        physics: const BouncingScrollPhysics(),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 12),

            // ── Ads Banner Carousel ──
            if (_loadingAds)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16.0),
                child: Shimmer(
                  child: Skeleton(
                    height: 190,
                    width: double.infinity,
                    borderRadius: BorderRadius.circular(20),
                  ),
                ),
              )
            else if (_adBanners.isNotEmpty) ...[
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16.0),
                child: Container(
                  height: 190,
                  width: double.infinity,
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(20),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withValues(alpha: isDark ? 0.3 : 0.06),
                        blurRadius: 14,
                        offset: const Offset(0, 4),
                      ),
                    ],
                  ),
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(20),
                    child: PageView.builder(
                      controller: _pageCtrl,
                      onPageChanged: (i) => setState(() => _adIndex = i),
                      itemCount: _adBanners.length,
                      itemBuilder: (ctx, i) {
                        final ad = _adBanners[i];
                        final String rawUrl = ad['imageUrl'] ?? ad['image'] ?? '';
                        final String formattedUrl = Api.formatImageUrl(rawUrl);
                        final String title = ad['title'] ?? '';

                        return Stack(
                          fit: StackFit.expand,
                          children: [
                            Container(
                              color: colorScheme.surfaceContainerHighest,
                              child: formattedUrl.isEmpty
                                  ? Center(
                                      child: Icon(
                                        Icons.campaign_outlined,
                                        size: 48,
                                        color: colorScheme.onSurfaceVariant,
                                      ),
                                    )
                                  : (formattedUrl.startsWith('assets/')
                                      ? Image.asset(
                                          formattedUrl,
                                          fit: BoxFit.cover,
                                          width: double.infinity,
                                          errorBuilder: (_, __, ___) => Center(
                                            child: Icon(
                                              Icons.campaign_outlined,
                                              size: 48,
                                              color: colorScheme.onSurfaceVariant,
                                            ),
                                          ),
                                        )
                                      : Image.network(
                                          formattedUrl,
                                          fit: BoxFit.cover,
                                          width: double.infinity,
                                          errorBuilder: (_, __, ___) => Center(
                                            child: Icon(
                                              Icons.campaign_outlined,
                                              size: 48,
                                              color: colorScheme.onSurfaceVariant,
                                            ),
                                          ),
                                        )),
                            ),
                            if (title.isNotEmpty) ...[
                              Positioned(
                                bottom: 0,
                                left: 0,
                                right: 0,
                                child: Container(
                                  padding: const EdgeInsets.all(16),
                                  decoration: BoxDecoration(
                                    gradient: LinearGradient(
                                      begin: Alignment.topCenter,
                                      end: Alignment.bottomCenter,
                                      colors: [
                                        Colors.transparent,
                                        Colors.black.withValues(alpha: 0.75),
                                      ],
                                    ),
                                  ),
                                  child: Text(
                                    title,
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                    style: GoogleFonts.inter(
                                      color: Colors.white,
                                      fontSize: 16,
                                      fontWeight: FontWeight.bold,
                                    ),
                                  ),
                                ),
                              ),
                            ],
                          ],
                        );
                      },
                    ),
                  ),
                ),
              ),

              // Carousel Indicator Dots
              if (_adBanners.length > 1) ...[
                const SizedBox(height: 12),
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: List.generate(
                    _adBanners.length,
                    (i) => AnimatedContainer(
                      duration: const Duration(milliseconds: 300),
                      width: _adIndex == i ? 20 : 8,
                      height: 8,
                      margin: const EdgeInsets.symmetric(horizontal: 3),
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(4),
                        color: _adIndex == i
                            ? colorScheme.primary
                            : colorScheme.onSurfaceVariant.withValues(alpha: 0.3),
                      ),
                    ),
                  ),
                ),
              ],
            ],

            const SizedBox(height: 8),

            // ── Featured Categories Header ──
            SectionHeader(
              title: 'Featured Categories'.tr(),
            ),

            // ── Horizontal Categories Bento Scroll ──
            if (_loadingCategories)
              SizedBox(
                height: 260,
                child: ListView.builder(
                  scrollDirection: Axis.horizontal,
                  physics: const NeverScrollableScrollPhysics(),
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  itemCount: 4,
                  itemBuilder: (context, index) => Container(
                    width: 200,
                    margin: const EdgeInsets.only(right: 14),
                    child: Shimmer(
                      child: Skeleton(
                        height: 260,
                        width: 200,
                        borderRadius: BorderRadius.circular(16),
                      ),
                    ),
                  ),
                ),
              )
            else if (_categories.isEmpty)
              Padding(
                padding: const EdgeInsets.all(40),
                child: Center(
                  child: Text(
                    'No categories found.'.tr(),
                    style: GoogleFonts.inter(
                      fontSize: 15,
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                ),
              )
            else
              SizedBox(
                height: 260,
                child: ListView.builder(
                  scrollDirection: Axis.horizontal,
                  physics: const BouncingScrollPhysics(),
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  itemCount: _categories.length,
                  itemBuilder: (context, index) {
                    final cat = _categories[index];
                    final String catId = cat['id']!;
                    final String name = cat['name']!;
                    final String imgUrl = cat['imageUrl'] ?? '';

                    return StitchCategoryCard(
                      title: name,
                      imageUrl: imgUrl,
                      onTap: () {
                        Navigator.pushNamed(
                          context,
                          '/category-products',
                          arguments: {'id': catId, 'name': name},
                        );
                      },
                    );
                  },
                ),
              ),

            const SizedBox(height: 32),
          ],
        ),
      ),
      bottomNavigationBar: StitchBottomNavigationBar(
        currentIndex: 0,
        cartCount: widget.cartCount,
        isShop: _accountType == 'Shop',
        isDelivery: _accountType == 'Delivery',
        isAdmin: _accountType == 'Admin',
        onTap: (index) {
          if (index == 0) return;
          if (_accountType == 'Admin') {
            if (index == 1) {
              Navigator.pushNamed(context, '/admin', arguments: {'initialTab': 0});
            } else if (index == 2) {
              Navigator.pushNamed(context, '/admin', arguments: {'initialTab': 4});
            } else if (index == 3) {
              Navigator.pushNamed(context, '/admin', arguments: {'initialTab': 3});
            } else if (index == 4) {
              Navigator.pushNamed(context, '/profile');
            }
          } else if (_accountType == 'Delivery') {
            if (index == 1) {
              Navigator.pushNamed(context, '/delivery-jobs');
            } else if (index == 2) {
              Navigator.pushNamed(context, '/notifications');
            } else if (index == 3) {
              Navigator.pushNamed(context, '/settings');
            } else if (index == 4) {
              Navigator.pushNamed(context, '/profile');
            }
          } else if (_accountType == 'Shop') {
            if (index == 1) {
              Navigator.pushNamed(context, '/store-products');
            } else if (index == 2) {
              Navigator.pushNamed(context, '/store-orders');
            } else if (index == 3) {
              Navigator.pushNamed(context, '/store-analytics');
            } else if (index == 4) {
              Navigator.pushNamed(context, '/profile');
            }
          } else {
            if (index == 1) {
              Navigator.pushNamed(context, '/favorites');
            } else if (index == 2) {
              widget.onNavigateCart();
            } else if (index == 3) {
              Navigator.pushNamed(context, '/active-orders');
            } else if (index == 4) {
              Navigator.pushNamed(context, '/profile');
            }
          }
        },
      ),
    );
  }
}
