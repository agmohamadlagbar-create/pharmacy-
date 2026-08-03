import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'widgets/profile_ripple_avatar.dart';
import 'widgets/stitch_bottom_navigation_bar.dart';

class ProfilePage extends StatefulWidget {
  const ProfilePage({super.key});

  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  Map<String, dynamic>? _userData;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchProfile();
  }

  Future<void> _fetchProfile() async {
    try {
      final res = await Api.get('/api/me');
      if (mounted) {
        setState(() {
          _userData = res;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${'Failed to load profile:'.tr()} $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;

    if (_isLoading) {
      return Scaffold(
        backgroundColor: theme.scaffoldBackgroundColor,
        appBar: AppBar(
          backgroundColor: colorScheme.surface,
          elevation: 0,
          title: Text('My Profile'.tr(),
              style: GoogleFonts.inter(
                  fontWeight: FontWeight.bold, color: colorScheme.onSurface)),
        ),
        body: Center(child: CircularProgressIndicator(color: colorScheme.primary)),
      );
    }

    final String name = _userData?['fullName'] ?? 'Not set'.tr();
    final String email = _userData?['email'] ?? 'Not set'.tr();
    final String? avatarUrl = _userData?['profilePictureUrl'];
    final String formattedAvatar = Api.formatImageUrl(avatarUrl);

    final String city = _userData?['city'] ?? 'Not set'.tr();
    final String district = _userData?['district'] ?? 'Not set'.tr();
    final String neighborhood = _userData?['neighborhood'] ?? 'Not set'.tr();
    final String street = _userData?['street'] ?? 'Not set'.tr();
    final String building = _userData?['buildingNumber'] ?? 'Not set'.tr();
    final String floor = _userData?['floor'] ?? 'Not set'.tr();
    final String apartmentNo = _userData?['apartmentNo'] ?? 'Not set'.tr();
    final String country = _userData?['country'] ?? 'Not set'.tr();
    final String postalCode = _userData?['postalCode'] ?? 'Not set'.tr();

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      appBar: AppBar(
        backgroundColor: colorScheme.surface,
        elevation: 0,
        scrolledUnderElevation: 1,
        title: Text(
          'My Profile'.tr(),
          style: GoogleFonts.inter(
              fontWeight: FontWeight.bold, color: colorScheme.onSurface),
        ),
        leading: IconButton(
          icon: Icon(Icons.arrow_back_rounded, color: colorScheme.onSurface),
          onPressed: () => Navigator.pop(context),
        ),
        actions: [
          IconButton(
            icon: Icon(Icons.edit_outlined, color: colorScheme.primary),
            onPressed: () {
              Navigator.pushNamed(context, '/edit-profile').then((_) => _fetchProfile());
            },
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          physics: const BouncingScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(20, 20, 20, 40),
          child: Column(
            children: [
              // Avatar
              Center(
                child: ProfileRippleAvatar(
                  size: 140,
                  image: formattedAvatar.isNotEmpty
                      ? NetworkImage(formattedAvatar)
                      : null,
                ),
              ),
              const SizedBox(height: 16),

              Text(
                name,
                textAlign: TextAlign.center,
                style: GoogleFonts.inter(
                  fontSize: 22,
                  fontWeight: FontWeight.bold,
                  color: colorScheme.onSurface,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                email,
                textAlign: TextAlign.center,
                style: GoogleFonts.inter(
                  fontSize: 14,
                  color: colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: 24),

              // Personal Info Card
              Container(
                padding: const EdgeInsets.all(18),
                decoration: BoxDecoration(
                  color: colorScheme.surface,
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(
                    color: isDark
                        ? Colors.white.withValues(alpha: 0.1)
                        : colorScheme.outline.withValues(alpha: 0.5),
                  ),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'PERSONAL INFO'.tr(),
                      style: GoogleFonts.inter(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 1.2,
                        color: colorScheme.primary,
                      ),
                    ),
                    const SizedBox(height: 14),
                    _buildInfoTile(
                        Icons.person_outline,
                        _userData?['accountType'] == 'Shop'
                            ? 'Shop Name'.tr()
                            : 'Full Name'.tr(),
                        name),
                    const Divider(height: 24),
                    _buildInfoTile(Icons.email_outlined, 'Email'.tr(), email),
                    if (_userData?['accountType'] != null) ...[
                      const Divider(height: 24),
                      _buildInfoTile(Icons.badge_outlined, 'Account Type'.tr(),
                          _userData!['accountType'].toString().toUpperCase()),
                    ]
                  ],
                ),
              ),
              const SizedBox(height: 16),

              // Detailed Location & Address Card
              Container(
                padding: const EdgeInsets.all(18),
                decoration: BoxDecoration(
                  color: colorScheme.surface,
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(
                    color: isDark
                        ? Colors.white.withValues(alpha: 0.1)
                        : colorScheme.outline.withValues(alpha: 0.5),
                  ),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'LOCATION & ADDRESS'.tr(),
                      style: GoogleFonts.inter(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 1.2,
                        color: colorScheme.primary,
                      ),
                    ),
                    const SizedBox(height: 14),
                    _buildInfoTile(Icons.location_city_outlined, 'City'.tr(), city),
                    const Divider(height: 24),
                    _buildInfoTile(Icons.map_outlined, 'District'.tr(), district),
                    const Divider(height: 24),
                    _buildInfoTile(Icons.holiday_village_outlined, 'Neighborhood'.tr(), neighborhood),
                    const Divider(height: 24),
                    _buildInfoTile(Icons.home_outlined, 'Street'.tr(), street),
                    const Divider(height: 24),
                    _buildInfoTile(
                        Icons.apartment_outlined,
                        'Building / Floor / Apt'.tr(),
                        'Bldg $building · Kat $floor · Daire $apartmentNo'),
                    const Divider(height: 24),
                    _buildInfoTile(Icons.flag_outlined, 'Country'.tr(), country),
                    const Divider(height: 24),
                    _buildInfoTile(
                        Icons.markunread_mailbox_outlined, 'Postal Code'.tr(), postalCode),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
      bottomNavigationBar: StitchBottomNavigationBar(
        currentIndex: 4,
        isShop: _userData?['accountType'] == 'Shop',
        isDelivery: _userData?['accountType'] == 'Delivery',
        isAdmin: _userData?['accountType'] == 'Admin',
        onTap: (index) {
          final accountType = _userData?['accountType'];
          if (accountType == 'Admin') {
            if (index == 4) return;
            if (index == 0) {
              Navigator.pushReplacementNamed(context, '/categories');
            } else if (index == 1) {
              Navigator.pushReplacementNamed(context, '/admin');
            } else if (index == 2) {
              Navigator.pushReplacementNamed(context, '/admin');
            } else if (index == 3) {
              Navigator.pushReplacementNamed(context, '/categories');
            }
          } else if (accountType == 'Delivery') {
            if (index == 4) return;
            if (index == 0) {
              Navigator.pushReplacementNamed(context, '/categories');
            } else if (index == 1) {
              Navigator.pushReplacementNamed(context, '/delivery-jobs');
            } else if (index == 2) {
              Navigator.pushReplacementNamed(context, '/notifications');
            } else if (index == 3) {
              Navigator.pushReplacementNamed(context, '/settings');
            }
          } else if (accountType == 'Shop') {
            if (index == 4) return;
            if (index == 0) {
              Navigator.pushReplacementNamed(context, '/categories');
            } else if (index == 1) {
              Navigator.pushReplacementNamed(context, '/store-products');
            } else if (index == 2) {
              Navigator.pushReplacementNamed(context, '/store-orders');
            } else if (index == 3) {
              Navigator.pushReplacementNamed(context, '/store-analytics');
            }
          } else {
            if (index == 4) return;
            if (index == 0) {
              Navigator.popUntil(context, ModalRoute.withName('/categories'));
            } else if (index == 1) {
              Navigator.pushNamed(context, '/favorites');
            } else if (index == 2) {
              Navigator.pushNamed(context, '/cart');
            } else if (index == 3) {
              Navigator.pushNamed(context, '/active-orders');
            }
          }
        },
      ),
    );
  }

  Widget _buildInfoTile(IconData icon, String label, String value) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;

    return Row(
      children: [
        Icon(icon, size: 20, color: colorScheme.onSurfaceVariant),
        const SizedBox(width: 14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: GoogleFonts.inter(
                  fontSize: 12,
                  color: colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: GoogleFonts.inter(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                  color: colorScheme.onSurface,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
