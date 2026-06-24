import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'widgets/profile_ripple_avatar.dart';

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
      setState(() {
        _userData = res;
        _isLoading = false;
      });
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
    final colorScheme = Theme.of(context).colorScheme;

    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(
          title: Text('My Profile'.tr(), style: GoogleFonts.playfairDisplay(fontWeight: FontWeight.w600)),
        ),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    final String name = _userData?['fullName'] ?? 'Not set'.tr();
    final String email = _userData?['email'] ?? 'Not set'.tr();
    final String? avatarUrl = _userData?['profilePictureUrl'];

    // Address fields
    final String street = _userData?['street'] ?? 'Not set'.tr();
    final String building = _userData?['buildingNumber'] ?? 'Not set'.tr();
    final String city = _userData?['city'] ?? 'Not set'.tr();
    final String postalCode = _userData?['postalCode'] ?? 'Not set'.tr();

    return Scaffold(
      appBar: AppBar(
        title: Text('My Profile'.tr(), style: GoogleFonts.playfairDisplay(fontWeight: FontWeight.w600)),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(20, 20, 20, 40),
          child: Column(
            children: [
              Center(
                child: ProfileRippleAvatar(
                  size: 160,
                  image: avatarUrl != null
                      ? NetworkImage('${Api.baseUrl}$avatarUrl')
                      : null,
                ),
              ),
              const SizedBox(height: 24),

              // Personal info card
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'PERSONAL INFO'.tr(),
                        style: GoogleFonts.dmSans(
                          fontSize: 11,
                          fontWeight: FontWeight.w600,
                          letterSpacing: 1.2,
                          color: colorScheme.onSurface.withValues(alpha: 0.4),
                        ),
                      ),
                      const SizedBox(height: 12),
                      _buildInfoTile(Icons.person_outline, _userData?['accountType'] == 'Shop' ? 'Shop Name'.tr() : 'Full Name'.tr(), name),
                      _buildInfoTile(Icons.email_outlined, 'Email'.tr(), email),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: 16),

              // Address info card
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'ADDRESS'.tr(),
                        style: GoogleFonts.dmSans(
                          fontSize: 11,
                          fontWeight: FontWeight.w600,
                          letterSpacing: 1.2,
                          color: colorScheme.onSurface.withValues(alpha: 0.4),
                        ),
                      ),
                      const SizedBox(height: 12),
                      _buildInfoTile(Icons.location_on_outlined, 'Street'.tr(), street),
                      _buildInfoTile(Icons.home_outlined, 'Building No'.tr(), building),
                      _buildInfoTile(Icons.location_city_outlined, 'City'.tr(), city),
                      _buildInfoTile(Icons.markunread_mailbox_outlined, 'Postal Code'.tr(), postalCode),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: 24),
              SizedBox(
                width: double.infinity,
                height: 50,
                child: ElevatedButton(
                  onPressed: () async {
                    final result =
                        await Navigator.pushNamed(context, '/edit-profile');
                    if (result == true) {
                      _fetchProfile(); // Refresh
                    }
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: colorScheme.primary,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  ),
                  child: Text('Edit Profile'.tr(),
                      style: GoogleFonts.dmSans(fontSize: 16, fontWeight: FontWeight.bold)),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildInfoTile(IconData icon, String label, String value) {
    final colorScheme = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: colorScheme.primary.withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(icon, color: colorScheme.primary, size: 18),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: GoogleFonts.dmSans(
                    color: colorScheme.onSurface.withValues(alpha: 0.5),
                    fontSize: 11,
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  value,
                  style: GoogleFonts.dmSans(
                    fontSize: 15,
                    fontWeight: FontWeight.w600,
                    color: colorScheme.onSurface,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
