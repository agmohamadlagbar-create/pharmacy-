import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'api.dart';
import 'widgets/stitch_bottom_navigation_bar.dart';

class NotificationsPage extends StatefulWidget {
  const NotificationsPage({super.key});

  @override
  State<NotificationsPage> createState() => _NotificationsPageState();
}

class _NotificationsPageState extends State<NotificationsPage> {
  List<Map<String, dynamic>> _notifications = [];
  bool _loading = true;
  String _accountType = '';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final prefs = await SharedPreferences.getInstance();
      final accType = prefs.getString('accountType') ?? '';
      final res = await Api.get('/api/notifications');
      if (mounted) {
        final data =
            res is List ? res : (res['data'] is List ? res['data'] : []);
        setState(() {
          _accountType = accType;
          _notifications =
              (data as List).map((e) => Map<String, dynamic>.from(e)).toList();
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('${'Error:'.tr()} $e'),
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      appBar: AppBar(
        backgroundColor: colorScheme.surface,
        elevation: 0,
        scrolledUnderElevation: 1,
        title: Text(
          'Notifications'.tr(),
          style: GoogleFonts.inter(
              fontWeight: FontWeight.bold, color: colorScheme.onSurface),
        ),
        leading: IconButton(
          icon: Icon(Icons.arrow_back_rounded, color: colorScheme.onSurface),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: _loading
          ? Center(child: CircularProgressIndicator(color: colorScheme.primary))
          : _notifications.isEmpty
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Container(
                        width: 80,
                        height: 80,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: colorScheme.primary.withValues(alpha: 0.08),
                        ),
                        child: Icon(Icons.notifications_none_rounded,
                            size: 40,
                            color: colorScheme.primary.withValues(alpha: 0.5)),
                      ),
                      const SizedBox(height: 16),
                      Text(
                        'No notifications yet'.tr(),
                        style: GoogleFonts.inter(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: colorScheme.onSurface),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        'You will receive updates about your orders here'.tr(),
                        style: GoogleFonts.inter(
                            color: colorScheme.onSurfaceVariant, fontSize: 13),
                      ),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.separated(
                    padding: const EdgeInsets.all(16),
                    physics: const BouncingScrollPhysics(),
                    itemCount: _notifications.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 10),
                    itemBuilder: (ctx, i) {
                      final n = _notifications[i];
                      final isRead = n['isRead'] == true;
                      return Container(
                        padding: const EdgeInsets.all(14),
                        decoration: BoxDecoration(
                          color: isRead
                              ? colorScheme.surface
                              : colorScheme.primary.withValues(alpha: 0.06),
                          borderRadius: BorderRadius.circular(14),
                          border: Border.all(
                            color: isRead
                                ? (isDark
                                    ? Colors.white.withValues(alpha: 0.1)
                                    : colorScheme.outline.withValues(alpha: 0.5))
                                : colorScheme.primary.withValues(alpha: 0.3),
                            width: isRead ? 1 : 1.5,
                          ),
                        ),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Container(
                              width: 40,
                              height: 40,
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                color: isRead
                                    ? colorScheme.surfaceContainerHighest
                                    : colorScheme.primary
                                        .withValues(alpha: 0.15),
                              ),
                              child: Icon(
                                isRead
                                    ? Icons.notifications_outlined
                                    : Icons.notifications_active_rounded,
                                color: isRead
                                    ? colorScheme.onSurfaceVariant
                                    : colorScheme.primary,
                                size: 20,
                              ),
                            ),
                            const SizedBox(width: 14),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    n['message'] ?? '',
                                    style: GoogleFonts.inter(
                                      fontWeight: isRead
                                          ? FontWeight.w500
                                          : FontWeight.bold,
                                      fontSize: 14,
                                      color: colorScheme.onSurface,
                                    ),
                                  ),
                                  if (n['createdAt'] != null) ...[
                                    const SizedBox(height: 4),
                                    Text(
                                      n['createdAt'].toString(),
                                      style: GoogleFonts.inter(
                                        fontSize: 11,
                                        color: colorScheme.onSurfaceVariant,
                                      ),
                                    ),
                                  ]
                                ],
                              ),
                            ),
                          ],
                        ),
                      );
                    },
                  ),
                ),
      bottomNavigationBar: _accountType == 'Delivery'
          ? StitchBottomNavigationBar(
              currentIndex: 2,
              isDelivery: true,
              onTap: (index) {
                if (index == 2) return;
                if (index == 0) {
                  Navigator.pushReplacementNamed(context, '/categories');
                } else if (index == 1) {
                  Navigator.pushReplacementNamed(context, '/delivery-jobs');
                } else if (index == 3) {
                  Navigator.pushReplacementNamed(context, '/settings');
                } else if (index == 4) {
                  Navigator.pushReplacementNamed(context, '/profile');
                }
              },
            )
          : null,
    );
  }
}
