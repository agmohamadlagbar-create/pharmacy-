import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'widgets/stitch_bottom_navigation_bar.dart';

class StoreOrdersPage extends StatefulWidget {
  const StoreOrdersPage({super.key});

  @override
  State<StoreOrdersPage> createState() => _StoreOrdersPageState();
}

class _StoreOrdersPageState extends State<StoreOrdersPage> {
  List<Map<String, dynamic>> _orders = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final res = await Api.get('/api/orders');
      if (mounted) {
        final data =
            res is List ? res : (res['data'] is List ? res['data'] : []);
        setState(() {
          _orders =
              (data as List).map((e) => Map<String, dynamic>.from(e)).toList();
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${'Error:'.tr()} $e')),
        );
      }
    }
  }

  Color _statusColor(String? status) {
    switch (status) {
      case 'Pending':
        return const Color(0xFFF59E0B);
      case 'Preparing':
        return const Color(0xFF3B82F6);
      case 'ReadyForPickup':
        return const Color(0xFF22C55E);
      case 'PickedUp':
        return const Color(0xFF8B5CF6);
      case 'OnTheWay':
        return const Color(0xFF06B6D4);
      case 'Delivered':
        return const Color(0xFF10B981);
      case 'Cancelled':
        return const Color(0xFFEF4444);
      default:
        return const Color(0xFF888888);
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
          'Store Orders'.tr(),
          style: GoogleFonts.inter(
              fontWeight: FontWeight.bold, color: colorScheme.onSurface),
        ),
        leading: IconButton(
          icon: Icon(Icons.arrow_back_rounded, color: colorScheme.onSurface),
          onPressed: () => Navigator.pop(context),
        ),
        actions: [
          IconButton(
            icon: Icon(Icons.refresh_rounded, color: colorScheme.primary),
            onPressed: _load,
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: _loading
          ? Center(child: CircularProgressIndicator(color: colorScheme.primary))
          : _orders.isEmpty
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
                        child: Icon(Icons.receipt_long_outlined,
                            size: 40, color: colorScheme.primary),
                      ),
                      const SizedBox(height: 16),
                      Text(
                        'No store orders yet'.tr(),
                        style: GoogleFonts.inter(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: colorScheme.onSurface),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        'Incoming customer orders will appear here'.tr(),
                        style: GoogleFonts.inter(
                            color: colorScheme.onSurfaceVariant, fontSize: 13),
                      ),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.separated(
                    physics: const BouncingScrollPhysics(),
                    padding: const EdgeInsets.all(16),
                    itemCount: _orders.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 12),
                    itemBuilder: (ctx, i) {
                      final o = _orders[i];
                      final total = (o['totalAmount'] as num?)?.toDouble() ?? 0;
                      final status = o['status']?.toString() ?? 'Pending';
                      final statusCol = _statusColor(status);

                      return Container(
                        padding: const EdgeInsets.all(16),
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
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text(
                                  '${'Order'.tr()} #${o['id']}',
                                  style: GoogleFonts.inter(
                                      fontWeight: FontWeight.bold,
                                      fontSize: 16,
                                      color: colorScheme.onSurface),
                                ),
                                Container(
                                  padding: const EdgeInsets.symmetric(
                                      horizontal: 10, vertical: 4),
                                  decoration: BoxDecoration(
                                    color: statusCol.withValues(alpha: 0.12),
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  child: Text(
                                    status.tr(),
                                    style: GoogleFonts.inter(
                                        color: statusCol,
                                        fontWeight: FontWeight.bold,
                                        fontSize: 12),
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text(
                                  'Total Amount'.tr(),
                                  style: GoogleFonts.inter(
                                      fontSize: 13,
                                      color: colorScheme.onSurfaceVariant),
                                ),
                                Text(
                                  '\$${total.toStringAsFixed(2)}',
                                  style: GoogleFonts.inter(
                                      fontSize: 16,
                                      fontWeight: FontWeight.bold,
                                      color: colorScheme.primary),
                                ),
                              ],
                            ),
                            if (o['createdAt'] != null) ...[
                              const SizedBox(height: 4),
                              Text(
                                o['createdAt'].toString(),
                                style: GoogleFonts.inter(
                                    fontSize: 11,
                                    color: colorScheme.onSurfaceVariant),
                              ),
                            ]
                          ],
                        ),
                      );
                    },
                  ),
                ),
      bottomNavigationBar: StitchBottomNavigationBar(
        currentIndex: 2,
        isShop: true,
        onTap: (index) {
          if (index == 2) return;
          if (index == 0) {
            Navigator.pushReplacementNamed(context, '/categories');
          } else if (index == 1) {
            Navigator.pushReplacementNamed(context, '/store-products');
          } else if (index == 3) {
            Navigator.pushReplacementNamed(context, '/store-analytics');
          } else if (index == 4) {
            Navigator.pushReplacementNamed(context, '/profile');
          }
        },
      ),
    );
  }
}
