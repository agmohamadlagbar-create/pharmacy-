import 'package:flutter/material.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:google_fonts/google_fonts.dart';
import 'api.dart';
import 'main.dart';
import 'public_store_page.dart';
import 'widgets/stitch_bottom_navigation_bar.dart';

class DeliveredOrdersPage extends StatefulWidget {
  const DeliveredOrdersPage({super.key});

  @override
  State<DeliveredOrdersPage> createState() => _DeliveredOrdersPageState();
}

class _DeliveredOrdersPageState extends State<DeliveredOrdersPage> {
  List<Map<String, dynamic>> _orderGroups = [];
  bool _isLoading = true;
  String? _error;
  String? _expandedGroupId;
  String? _isReordering;

  @override
  void initState() {
    super.initState();
    _fetchOrders();
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
        return const Color(0xFF22C55E);
      case 'Cancelled':
        return const Color(0xFFEF4444);
      default:
        return const Color(0xFF888888);
    }
  }

  Future<void> _fetchOrders() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final res = await Api.get('/api/orders?status=Delivered&limit=50');
      final orders = res['data'] ?? res;

      // Group orders by orderGroupId
      final Map<String, Map<String, dynamic>> groups = {};
      for (final o in orders) {
        final order = Map<String, dynamic>.from(o);
        final gid = order['orderGroupId']?.toString() ?? 'S-${order['id']}';

        if (!groups.containsKey(gid)) {
          groups[gid] = {
            'id': gid,
            'date': order['createdAtUtc'] ?? order['createdAt'],
            'subOrders': <Map<String, dynamic>>[],
            'total': 0.0,
            'status': order['status'],
          };
        }
        (groups[gid]!['subOrders'] as List<Map<String, dynamic>>).add(order);
        groups[gid]!['total'] = (groups[gid]!['total'] as double) +
            ((order['totalAmount'] as num?)?.toDouble() ?? 0.0);
      }

      if (mounted) {
        setState(() {
          _orderGroups = groups.values.toList();
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = '${'Failed to load orders:'.tr()} $e';
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _reorderAll(Map<String, dynamic> group) async {
    final gid = group['id'];
    setState(() => _isReordering = gid);

    try {
      final subOrders = group['subOrders'] as List<Map<String, dynamic>>;
      for (final so in subOrders) {
        final items = so['items'] as List<dynamic>? ?? [];
        for (final item in items) {
          final productId = item['productId'];
          final quantity = item['quantity'] ?? 1;
          if (productId != null) {
            await Api.addToCart(productId, quantity);
          }
        }
      }
      if (mounted) {
        OnlineStoreApp.of(context)?.fetchCart();
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('All items added to cart!'.tr())),
        );
        Navigator.of(context).pushNamed('/cart');
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${'Failed to reorder:'.tr()} $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isReordering = null);
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
          'Delivered Orders'.tr(),
          style: GoogleFonts.inter(
              fontWeight: FontWeight.bold, color: colorScheme.onSurface),
        ),
        leading: IconButton(
          icon: Icon(Icons.arrow_back_rounded, color: colorScheme.onSurface),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: _isLoading
          ? Center(child: CircularProgressIndicator(color: colorScheme.primary))
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(_error!, style: const TextStyle(color: Colors.red)),
                      const SizedBox(height: 12),
                      FilledButton(
                          onPressed: _fetchOrders, child: Text('Retry'.tr())),
                    ],
                  ),
                )
              : _orderGroups.isEmpty
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
                            child: Icon(Icons.inventory_2_outlined,
                                size: 40,
                                color: colorScheme.primary.withValues(alpha: 0.5)),
                          ),
                          const SizedBox(height: 16),
                          Text(
                            'No Delivered Orders'.tr(),
                            style: GoogleFonts.inter(
                                fontSize: 18,
                                fontWeight: FontWeight.bold,
                                color: colorScheme.onSurface),
                          ),
                          const SizedBox(height: 6),
                          Text(
                            "You haven't received any orders yet.".tr(),
                            style: GoogleFonts.inter(
                                color: colorScheme.onSurfaceVariant,
                                fontSize: 13),
                          ),
                        ],
                      ),
                    )
                  : RefreshIndicator(
                      onRefresh: _fetchOrders,
                      child: ListView.separated(
                        physics: const BouncingScrollPhysics(),
                        padding: const EdgeInsets.all(16),
                        itemCount: _orderGroups.length,
                        separatorBuilder: (_, __) => const SizedBox(height: 12),
                        itemBuilder: (context, index) {
                          final group = _orderGroups[index];
                          return _buildOrderGroupCard(group, colorScheme, isDark);
                        },
                      ),
                    ),
      bottomNavigationBar: StitchBottomNavigationBar(
        currentIndex: 3,
        onTap: (index) {
          if (index == 0) {
            Navigator.popUntil(context, ModalRoute.withName('/categories'));
          } else if (index == 1) {
            Navigator.pushNamed(context, '/favorites');
          } else if (index == 2) {
            Navigator.pushNamed(context, '/cart');
          } else if (index == 4) {
            Navigator.pushNamed(context, '/profile');
          }
        },
      ),
    );
  }

  Widget _buildOrderGroupCard(
      Map<String, dynamic> group, ColorScheme colorScheme, bool isDark) {
    final gid = group['id'];
    final isExpanded = _expandedGroupId == gid;
    final subOrders = group['subOrders'] as List<Map<String, dynamic>>;
    final total = group['total'] as double;
    final status = group['status']?.toString() ?? 'Delivered';
    final sColor = _statusColor(status);
    final dateStr = group['date'] != null
        ? _formatDate(group['date'].toString())
        : 'N/A'.tr();

    return Container(
      decoration: BoxDecoration(
        color: colorScheme.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isDark
              ? Colors.white.withValues(alpha: 0.1)
              : colorScheme.outline.withValues(alpha: 0.5),
        ),
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: () {
          setState(() {
            _expandedGroupId = isExpanded ? null : gid;
          });
        },
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Order #{{id}}'.tr(namedArgs: {'id': gid.toString()}),
                            style: GoogleFonts.inter(
                                fontWeight: FontWeight.bold,
                                fontSize: 15,
                                color: colorScheme.onSurface)),
                        const SizedBox(height: 4),
                        Text(
                          '{{count}} stores involved'.tr(namedArgs: {
                            'count': subOrders.length.toString()
                          }),
                          style: GoogleFonts.inter(
                              fontSize: 12,
                              color: colorScheme.onSurfaceVariant),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          dateStr,
                          style: GoogleFonts.inter(
                              fontSize: 11,
                              color: colorScheme.onSurfaceVariant),
                        ),
                      ],
                    ),
                  ),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: sColor.withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          status.tr(),
                          style: GoogleFonts.inter(
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              color: sColor),
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        '\$${total.toStringAsFixed(2)}',
                        style: GoogleFonts.inter(
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                            color: colorScheme.primary),
                      ),
                    ],
                  ),
                ],
              ),

              // Expand arrow
              Center(
                child: AnimatedRotation(
                  turns: isExpanded ? 0.5 : 0,
                  duration: const Duration(milliseconds: 200),
                  child: Icon(Icons.keyboard_arrow_down,
                      color: colorScheme.onSurfaceVariant),
                ),
              ),

              // Expanded content
              if (isExpanded) ...[
                Divider(
                    height: 16,
                    color: colorScheme.outline.withValues(alpha: 0.3)),
                ...subOrders.map((so) => _buildSubOrder(so, colorScheme)),
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('Total Paid'.tr(),
                        style: GoogleFonts.inter(
                            fontWeight: FontWeight.bold,
                            fontSize: 14,
                            color: colorScheme.onSurface)),
                    Text('\$${total.toStringAsFixed(2)}',
                        style: GoogleFonts.inter(
                            fontWeight: FontWeight.bold,
                            fontSize: 15,
                            color: colorScheme.primary)),
                  ],
                ),
                const SizedBox(height: 16),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton.icon(
                    onPressed:
                        _isReordering == gid ? null : () => _reorderAll(group),
                    icon: _isReordering == gid
                        ? const SizedBox(
                            width: 16,
                            height: 16,
                            child: CircularProgressIndicator(
                                strokeWidth: 2, color: Colors.white))
                        : const Icon(Icons.replay_rounded),
                    label: Text(_isReordering == gid
                        ? 'Adding to Cart...'.tr()
                        : 'Order All Again'.tr()),
                    style: FilledButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSubOrder(Map<String, dynamic> so, ColorScheme colorScheme) {
    final items = so['items'] as List<dynamic>? ?? [];
    final storeName = so['storeName'] ?? 'Unknown Store'.tr();

    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              InkWell(
                onTap: () {
                  if (so['storeUserId'] != null) {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => PublicStorePage(
                            storeUserId: so['storeUserId'].toString()),
                      ),
                    );
                  }
                },
                child: Text(
                  storeName,
                  style: GoogleFonts.inter(
                      color: colorScheme.primary,
                      fontWeight: FontWeight.bold,
                      fontSize: 13),
                ),
              ),
              Text(
                'Part #{{id}}'.tr(namedArgs: {'id': so['id'].toString()}),
                style: GoogleFonts.inter(
                    fontSize: 11, color: colorScheme.onSurfaceVariant),
              ),
            ],
          ),
          const SizedBox(height: 6),
          ...items.map((item) {
            final qty = item['quantity'] ?? 1;
            final unitPrice = (item['unitPrice'] as num?)?.toDouble() ?? 0;
            return Padding(
              padding: const EdgeInsets.only(bottom: 4),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Text(
                      '${item['productName']} × $qty',
                      style: GoogleFonts.inter(
                          fontSize: 13, color: colorScheme.onSurfaceVariant),
                    ),
                  ),
                  Text(
                    '\$${(unitPrice * qty).toStringAsFixed(2)}',
                    style: GoogleFonts.inter(
                        fontSize: 13, color: colorScheme.onSurfaceVariant),
                  ),
                ],
              ),
            );
          }),
        ],
      ),
    );
  }

  String _formatDate(String dateStr) {
    try {
      final dt = DateTime.parse(dateStr);
      final months = [
        'Jan',
        'Feb',
        'Mar',
        'Apr',
        'May',
        'Jun',
        'Jul',
        'Aug',
        'Sep',
        'Oct',
        'Nov',
        'Dec'
      ];
      return '${months[dt.month - 1]} ${dt.day}, ${dt.year}';
    } catch (_) {
      return dateStr;
    }
  }
}
