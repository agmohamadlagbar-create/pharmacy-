import 'dart:io';
import 'dart:math';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:path_provider/path_provider.dart';
import 'api.dart';
import 'widgets/stitch_bottom_navigation_bar.dart';

class StoreAnalyticsPage extends StatefulWidget {
  const StoreAnalyticsPage({super.key});

  @override
  State<StoreAnalyticsPage> createState() => _StoreAnalyticsPageState();
}

class _StoreAnalyticsPageState extends State<StoreAnalyticsPage> {
  bool _loading = true;
  String? _error;
  Map<String, dynamic>? _data;

  // Date range
  String _activePreset = 'this_year';
  DateTime? _customStart;
  DateTime? _customEnd;

  // Reports
  String _reportTab = 'monthly';
  int _reportYear = DateTime.now().year;
  int _reportMonth = DateTime.now().month;
  String? _downloading;

  // Low stock threshold
  int _threshold = 10;
  bool _savingThreshold = false;
  final _thresholdCtrl = TextEditingController(text: '10');

  // Telegram
  bool _tgConnected = false;
  String? _tgMaskedId;
  String? _tgConnectedAt;
  final _tgChatIdCtrl = TextEditingController();
  bool _tgConnecting = false;
  bool _tgDisconnecting = false;
  bool _tgShowConnect = false;
  String? _tgMessageText;
  String? _tgMessageType; // 'success' or 'error'
  List<Map<String, dynamic>> _tgNotifications = [];
  bool _tgSaving = false;
  bool _tgLoaded = false;
  bool _tgTesting = false;
  bool _tgOpen = false;

  static const _notificationTypes = [
    {'key': 'NewCustomer', 'emoji': '🎉', 'label': 'New Customer Registration', 'desc': 'When a new customer signs up', 'category': 'events'},
    {'key': 'NewOrder', 'emoji': '🛒', 'label': 'New Order Received', 'desc': 'When a new order is placed', 'category': 'events'},
    {'key': 'OrderDelivered', 'emoji': '✅', 'label': 'Order Delivered', 'desc': 'When an order is marked delivered', 'category': 'events'},
    {'key': 'LowStock', 'emoji': '⚠️', 'label': 'Low Stock Warning', 'desc': 'When product stock falls below threshold', 'category': 'events'},
    {'key': 'NegativeReview', 'emoji': '👎', 'label': 'Negative Review Alert', 'desc': 'When a product receives a low rating', 'category': 'events'},
    {'key': 'DailyIncome', 'emoji': '📊', 'label': 'Daily Income Report', 'desc': 'Daily revenue, orders & top seller', 'category': 'scheduled'},
    {'key': 'WeeklyReport', 'emoji': '📈', 'label': 'Weekly Income Report', 'desc': 'Weekly summary with growth %', 'category': 'scheduled'},
    {'key': 'MonthlyReport', 'emoji': '📅', 'label': 'Monthly Income Report', 'desc': 'Full monthly business report', 'category': 'scheduled'},
    {'key': 'RevenueGoal', 'emoji': '🎯', 'label': 'Revenue Goal Reached', 'desc': 'When revenue milestone is hit', 'category': 'events'},
    {'key': 'ReturningCustomer', 'emoji': '🔄', 'label': 'Returning Customer Activity', 'desc': 'When returning customers trend changes', 'category': 'events'},
    {'key': 'TopProducts', 'emoji': '🏆', 'label': 'Top Selling Products Report', 'desc': 'Weekly top 5 products ranking', 'category': 'scheduled'},
    {'key': 'AiInsights', 'emoji': '🤖', 'label': 'AI Business Insights', 'desc': 'AI-powered business recommendations', 'category': 'scheduled'},
  ];

  static const _scheduledTypes = {'DailyIncome', 'WeeklyReport', 'MonthlyReport', 'TopProducts', 'AiInsights'};

  static const _frequencies = [
    {'value': 'Daily', 'label': 'Every Day'},
    {'value': 'Every2Days', 'label': 'Every 2 Days'},
    {'value': 'Every3Days', 'label': 'Every 3 Days'},
    {'value': 'Weekly', 'label': 'Every Week'},
    {'value': 'Monthly', 'label': 'Every Month'},
    {'value': 'Custom', 'label': 'Custom Interval'},
  ];

  static const _daysOfWeek = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

  static const _timezones = [
    'UTC', 'Europe/Istanbul', 'Europe/London', 'Europe/Berlin', 'Europe/Paris',
    'America/New_York', 'America/Chicago', 'America/Los_Angeles',
    'Asia/Baghdad', 'Asia/Dubai', 'Asia/Riyadh', 'Asia/Tokyo', 'Asia/Shanghai',
    'Australia/Sydney', 'Africa/Cairo',
  ];

  @override
  void initState() {
    super.initState();
    _fetchDashboard();
    _loadTelegram();
  }

  @override
  void dispose() {
    _tgChatIdCtrl.dispose();
    _thresholdCtrl.dispose();
    super.dispose();
  }

  // ═══════════════════════════════════════
  //  Date Helpers
  // ═══════════════════════════════════════

  Map<String, DateTime>? _getDatePreset(String key) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    switch (key) {
      case 'today':
        return {'start': today, 'end': now};
      case '7days':
        return {'start': today.subtract(const Duration(days: 6)), 'end': now};
      case '30days':
        return {'start': today.subtract(const Duration(days: 29)), 'end': now};
      case 'this_month':
        return {'start': DateTime(now.year, now.month, 1), 'end': now};
      case 'prev_month':
        final pm = DateTime(now.year, now.month - 1, 1);
        return {'start': pm, 'end': DateTime(now.year, now.month, 0, 23, 59, 59)};
      case 'this_year':
        return {'start': DateTime(now.year, 1, 1), 'end': now};
      default:
        return null;
    }
  }

  String _formatDateParam(DateTime d) => '${d.year}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  // ═══════════════════════════════════════
  //  API Calls
  // ═══════════════════════════════════════

  Future<void> _fetchDashboard() async {
    setState(() { _loading = true; _error = null; });
    try {
      String url = '/api/analytics/dashboard';
      final params = <String>[];

      if (_activePreset == 'custom' && _customStart != null && _customEnd != null) {
        params.add('startDate=${_formatDateParam(_customStart!)}');
        params.add('endDate=${_formatDateParam(_customEnd!)}');
      } else if (_activePreset != 'custom') {
        final range = _getDatePreset(_activePreset);
        if (range != null) {
          params.add('startDate=${_formatDateParam(range['start']!)}');
          params.add('endDate=${_formatDateParam(range['end']!)}');
        }
      }

      if (params.isNotEmpty) url += '?${params.join('&')}';
      final result = await Api.get(url);
      if (mounted) {
        setState(() {
          _data = result;
          _threshold = (result['lowStockThreshold'] as num?)?.toInt() ?? 10;
          _thresholdCtrl.text = '$_threshold';
          _loading = false;
        });
      }
    } catch (err) {
      if (mounted) {
        setState(() {
          _error = err.toString();
          _loading = false;
        });
      }
    }
  }

  Future<void> _handleSaveThreshold() async {
    setState(() => _savingThreshold = true);
    try {
      await Api.put('/api/analytics/settings/low-stock-threshold', {'threshold': _threshold});
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Threshold updated successfully!'.tr()),
            backgroundColor: const Color(0xFF10b981),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
      await _fetchDashboard();
    } catch (err) {
      debugPrint('Failed to save threshold: $err');
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('${'Failed to save threshold:'.tr()} $err'),
            backgroundColor: Colors.red,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _savingThreshold = false);
    }
  }

  Future<void> _handleDownload(String type, String format) async {
    final key = '${type}_$format';
    setState(() => _downloading = key);
    try {
      final lang = context.locale.languageCode;
      String url;
      if (type == 'monthly') {
        url = '/api/analytics/reports/monthly/$format?year=$_reportYear&month=$_reportMonth&lang=$lang';
      } else {
        url = '/api/analytics/reports/yearly/$format?year=$_reportYear&lang=$lang';
      }

      final bytes = await Api.getBytes(url);
      final ext = format == 'pdf' ? 'pdf' : 'xlsx';
      final fileName = type == 'monthly'
          ? 'MonthlySalesReport_${_reportYear}_${_reportMonth.toString().padLeft(2, '0')}.$ext'
          : 'YearlySalesReport_$_reportYear.$ext';

      final dir = await getTemporaryDirectory();
      final file = File('${dir.path}/$fileName');
      await file.writeAsBytes(bytes);

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('${'Report saved:'.tr()} $fileName'),
            duration: const Duration(seconds: 3),
          ),
        );
      }
    } catch (err) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${'Download failed:'.tr()} $err')),
        );
      }
    } finally {
      if (mounted) setState(() => _downloading = null);
    }
  }

  // ═══════════════════════════════════════
  //  Telegram Methods
  // ═══════════════════════════════════════

  Future<void> _loadTelegram() async {
    try {
      final res = await Api.get('/api/telegram/settings');
      if (mounted) {
        setState(() {
          final conn = res['connection'] as Map<String, dynamic>?;
          _tgConnected = conn?['connected'] == true;
          _tgMaskedId = conn?['maskedChatId']?.toString();
          _tgConnectedAt = conn?['connectedAt']?.toString();
          final notifs = res['notifications'];
          if (notifs is List) {
            _tgNotifications = notifs.map((n) => Map<String, dynamic>.from(n as Map)).toList();
          }
        });
      }
    } catch (err) {
      debugPrint('Telegram settings not loaded: $err');
    } finally {
      if (mounted) setState(() => _tgLoaded = true);
    }
  }

  Future<void> _handleTgConnect() async {
    final chatIdText = _tgChatIdCtrl.text.trim();
    if (chatIdText.isEmpty) return;
    setState(() { _tgConnecting = true; _tgMessageText = null; });
    try {
      final res = await Api.post('/api/telegram/connect', {'chatId': int.parse(chatIdText)});
      setState(() {
        _tgConnected = true;
        _tgMaskedId = res['maskedChatId']?.toString();
        _tgConnectedAt = res['connectedAt']?.toString();
        _tgShowConnect = false;
        _tgChatIdCtrl.clear();
        _tgMessageType = 'success';
        _tgMessageText = 'Telegram bot connected successfully!'.tr();
      });
      _loadTelegram();
    } catch (err) {
      setState(() {
        _tgMessageType = 'error';
        _tgMessageText = err.toString();
      });
    } finally {
      if (mounted) setState(() => _tgConnecting = false);
    }
  }

  Future<void> _handleTgDisconnect() async {
    setState(() { _tgDisconnecting = true; _tgMessageText = null; });
    try {
      await Api.post('/api/telegram/disconnect', {});
      setState(() {
        _tgConnected = false;
        _tgMaskedId = null;
        _tgMessageType = 'success';
        _tgMessageText = 'Telegram bot disconnected.'.tr();
      });
    } catch (err) {
      setState(() { _tgMessageType = 'error'; _tgMessageText = err.toString(); });
    } finally {
      if (mounted) setState(() => _tgDisconnecting = false);
    }
  }

  Future<void> _handleTgTest() async {
    setState(() { _tgTesting = true; _tgMessageText = null; });
    try {
      await Api.post('/api/telegram/test', {});
      setState(() { _tgMessageType = 'success'; _tgMessageText = 'Test message sent successfully!'.tr(); });
    } catch (err) {
      setState(() { _tgMessageType = 'error'; _tgMessageText = err.toString(); });
    } finally {
      if (mounted) setState(() => _tgTesting = false);
    }
  }

  void _handleTgToggle(String notifType) {
    setState(() {
      _tgNotifications = _tgNotifications.map((n) {
        if (n['notificationType'] == notifType) {
          return {...n, 'enabled': !(n['enabled'] == true)};
        }
        return n;
      }).toList();
    });
  }

  void _handleTgScheduleChange(String notifType, String field, dynamic value) {
    setState(() {
      _tgNotifications = _tgNotifications.map((n) {
        if (n['notificationType'] == notifType) {
          return {...n, field: value};
        }
        return n;
      }).toList();
    });
  }

  Map<String, dynamic> _getTgNotif(String type) {
    return _tgNotifications.firstWhere(
      (n) => n['notificationType'] == type,
      orElse: () => {
        'notificationType': type, 'enabled': false, 'frequency': 'Daily',
        'intervalDays': null, 'weeklyDay': null, 'hour': 9, 'minute': 0, 'timeZone': 'UTC',
      },
    );
  }

  Future<void> _handleTgSave() async {
    setState(() { _tgSaving = true; _tgMessageText = null; });
    try {
      await Api.put('/api/telegram/settings', {'settings': _tgNotifications});
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Telegram settings saved!'.tr()), backgroundColor: Colors.green),
        );
      }
    } catch (err) {
      setState(() { _tgMessageType = 'error'; _tgMessageText = err.toString(); });
    } finally {
      if (mounted) setState(() => _tgSaving = false);
    }
  }

  // ═══════════════════════════════════════
  //  Image URL Helper
  // ═══════════════════════════════════════

  String? _resolveImg(String? url) {
    if (url == null || url.isEmpty) return null;
    return url.startsWith('http') ? url : '${Api.baseUrl}$url';
  }

  // ═══════════════════════════════════════
  //  BUILD
  // ═══════════════════════════════════════

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Icon(Icons.bar_chart_rounded, color: colorScheme.primary),
            const SizedBox(width: 8),
            Text('Store Analytics'.tr(), style: GoogleFonts.playfairDisplay(fontWeight: FontWeight.w600)),
          ],
        ),
      ),
      body: _loading
          ? Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  CircularProgressIndicator(color: colorScheme.primary),
                  const SizedBox(height: 16),
                  Text('Loading analytics...'.tr(), style: GoogleFonts.dmSans(color: colorScheme.onSurface.withValues(alpha: 0.5))),
                ],
              ),
            )
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(32),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.error_outline, size: 48, color: Colors.red.shade300),
                        const SizedBox(height: 16),
                        Text(_error!, textAlign: TextAlign.center),
                        const SizedBox(height: 16),
                        ElevatedButton(onPressed: _fetchDashboard, child: Text('Retry'.tr())),
                      ],
                    ),
                  ),
                )
              : _data == null
                  ? const SizedBox.shrink()
                  : RefreshIndicator(
                      onRefresh: _fetchDashboard,
                      child: SingleChildScrollView(
                        physics: const AlwaysScrollableScrollPhysics(),
                        padding: const EdgeInsets.fromLTRB(16, 8, 16, 40),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            _buildDateRangeBar(colorScheme, isDark),
                            const SizedBox(height: 20),
                            _buildSummaryCards(colorScheme, isDark),
                            const SizedBox(height: 24),
                            _buildGrowthSection(colorScheme, isDark),
                            const SizedBox(height: 24),
                            _buildSalesCharts(colorScheme, isDark),
                            const SizedBox(height: 24),
                            _buildTopSellingProducts(colorScheme, isDark),
                            const SizedBox(height: 24),
                            _buildProductOverview(colorScheme, isDark),
                            const SizedBox(height: 24),
                            _buildTrendingProducts(colorScheme, isDark),
                            const SizedBox(height: 24),
                            _buildReturningCustomers(colorScheme, isDark),
                            const SizedBox(height: 24),
                            _buildProductRatings(colorScheme, isDark),
                            const SizedBox(height: 24),
                            _buildLowStockWarnings(colorScheme, isDark),
                            const SizedBox(height: 24),
                            _buildAiRecommendations(colorScheme, isDark),
                            const SizedBox(height: 24),
                            _buildReportsSection(colorScheme, isDark),
                            const SizedBox(height: 24),
                            if (_tgLoaded) _buildTelegramSection(colorScheme, isDark),
                          ],
                        ),
                      ),
                    ),
      bottomNavigationBar: StitchBottomNavigationBar(
        currentIndex: 3,
        isShop: true,
        onTap: (index) {
          if (index == 3) return;
          if (index == 0) {
            Navigator.pushReplacementNamed(context, '/categories');
          } else if (index == 1) {
            Navigator.pushReplacementNamed(context, '/store-products');
          } else if (index == 2) {
            Navigator.pushReplacementNamed(context, '/store-orders');
          } else if (index == 4) {
            Navigator.pushReplacementNamed(context, '/profile');
          }
        },
      ),
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: Date Range Bar
  // ═══════════════════════════════════════

  Widget _buildDateRangeBar(ColorScheme cs, bool isDark) {
    final presets = [
      {'key': 'today', 'label': 'Today'.tr()},
      {'key': '7days', 'label': '7 Days'.tr()},
      {'key': '30days', 'label': '30 Days'.tr()},
      {'key': 'this_month', 'label': 'This Month'.tr()},
      {'key': 'prev_month', 'label': 'Last Month'.tr()},
      {'key': 'this_year', 'label': 'This Year'.tr()},
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Period'.tr(), style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w600, color: cs.onSurface.withValues(alpha: 0.5))),
        const SizedBox(height: 8),
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: presets.map((p) {
              final isActive = _activePreset == p['key'];
              return Padding(
                padding: const EdgeInsets.only(right: 8),
                child: ChoiceChip(
                  label: Text(p['label']!, style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w600)),
                  selected: isActive,
                  selectedColor: cs.primary.withValues(alpha: 0.15),
                  backgroundColor: isDark ? cs.surface : Colors.grey.shade100,
                  side: BorderSide(color: isActive ? cs.primary : Colors.transparent),
                  onSelected: (_) {
                    setState(() => _activePreset = p['key']!);
                    _fetchDashboard();
                  },
                ),
              );
            }).toList(),
          ),
        ),
        const SizedBox(height: 10),
        Row(
          children: [
            Expanded(
              child: _DateButton(
                label: _customStart != null ? _formatDateParam(_customStart!) : 'Start Date'.tr(),
                onTap: () async {
                  final d = await showDatePicker(
                    context: context,
                    initialDate: _customStart ?? DateTime.now().subtract(const Duration(days: 30)),
                    firstDate: DateTime(2020),
                    lastDate: DateTime.now(),
                  );
                  if (d != null) setState(() => _customStart = d);
                },
              ),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8),
              child: Icon(Icons.arrow_forward, size: 16, color: cs.onSurface.withValues(alpha: 0.3)),
            ),
            Expanded(
              child: _DateButton(
                label: _customEnd != null ? _formatDateParam(_customEnd!) : 'End Date'.tr(),
                onTap: () async {
                  final d = await showDatePicker(
                    context: context,
                    initialDate: _customEnd ?? DateTime.now(),
                    firstDate: DateTime(2020),
                    lastDate: DateTime.now(),
                  );
                  if (d != null) setState(() => _customEnd = d);
                },
              ),
            ),
            const SizedBox(width: 8),
            SizedBox(
              height: 40,
              child: ElevatedButton(
                onPressed: _customStart != null && _customEnd != null
                    ? () { setState(() => _activePreset = 'custom'); _fetchDashboard(); }
                    : null,
                style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(horizontal: 16)),
                child: Text('Apply'.tr(), style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w600)),
              ),
            ),
          ],
        ),
      ],
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: Summary Cards
  // ═══════════════════════════════════════

  Widget _buildSummaryCards(ColorScheme cs, bool isDark) {
    final summary = _data!['summary'] as Map<String, dynamic>? ?? {};
    final currency = summary['currencySymbol'] ?? '\$';

    String fmtMoney(dynamic val) {
      final num n = (val is num) ? val : 0;
      return '$currency${n.toStringAsFixed(2)}';
    }

    final cards = [
      {'icon': Icons.attach_money, 'label': 'Total Revenue'.tr(), 'value': fmtMoney(summary['totalRevenue']), 'color': const Color(0xFF6366f1)},
      {'icon': Icons.calendar_today, 'label': 'Avg Daily Revenue'.tr(), 'value': fmtMoney(summary['averageDailyRevenue']), 'color': const Color(0xFF10b981)},
      {'icon': Icons.date_range, 'label': 'Avg Monthly Revenue'.tr(), 'value': fmtMoney(summary['averageMonthlyRevenue']), 'color': const Color(0xFFf59e0b)},
      {'icon': Icons.trending_up, 'label': 'Peak Day Revenue'.tr(), 'value': fmtMoney(summary['peakDayRevenue']), 'color': const Color(0xFFec4899)},
      {'icon': Icons.shopping_bag, 'label': 'Total Orders'.tr(), 'value': '${(summary['totalOrders'] as num?)?.toInt() ?? 0}', 'color': const Color(0xFF3b82f6)},
      {'icon': Icons.center_focus_strong, 'label': 'Avg Order Value'.tr(), 'value': fmtMoney(summary['averageOrderValue']), 'color': const Color(0xFF8b5cf6)},
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionTitle(Icons.attach_money, 'Overview'.tr(), cs),
        const SizedBox(height: 12),
        GridView.count(
          crossAxisCount: 2,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 10,
          crossAxisSpacing: 10,
          childAspectRatio: 1.5,
          children: cards.map((c) {
            return _AnalyticsCard(
              icon: c['icon'] as IconData,
              label: c['label'] as String,
              value: c['value'] as String,
              color: c['color'] as Color,
              isDark: isDark,
            );
          }).toList(),
        ),
      ],
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: Growth
  // ═══════════════════════════════════════

  Widget _buildGrowthSection(ColorScheme cs, bool isDark) {
    final growth = _data!['growth'] as Map<String, dynamic>? ?? {};
    final currency = (_data!['summary'] as Map?)?['currencySymbol'] ?? '\$';
    final revGrowth = (growth['revenueGrowthPercent'] as num?)?.toDouble() ?? 0;
    final orderGrowth = (growth['orderGrowthPercent'] as num?)?.toDouble() ?? 0;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionTitle(Icons.trending_up, 'Growth'.tr(), cs),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(child: _GrowthCard(
              label: 'Revenue Growth'.tr(),
              percent: revGrowth,
              prevLabel: 'Previous Month'.tr(),
              prevValue: '$currency${(growth['previousMonthRevenue'] as num?)?.toInt() ?? 0}',
              curLabel: 'Current Month'.tr(),
              curValue: '$currency${(growth['currentMonthRevenue'] as num?)?.toInt() ?? 0}',
              isDark: isDark,
            )),
            const SizedBox(width: 10),
            Expanded(child: _GrowthCard(
              label: 'Order Growth'.tr(),
              percent: orderGrowth,
              prevLabel: 'Previous Month'.tr(),
              prevValue: '${(growth['previousMonthOrders'] as num?)?.toInt() ?? 0}',
              curLabel: 'Current Month'.tr(),
              curValue: '${(growth['currentMonthOrders'] as num?)?.toInt() ?? 0}',
              isDark: isDark,
            )),
          ],
        ),
      ],
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: Sales Charts
  // ═══════════════════════════════════════

  Widget _buildSalesCharts(ColorScheme cs, bool isDark) {
    final dailySales = (_data!['dailySales'] as List?)?.cast<Map<String, dynamic>>() ?? [];
    final monthlySales = (_data!['monthlySales'] as List?)?.cast<Map<String, dynamic>>() ?? [];
    final currency = (_data!['summary'] as Map?)?['currencySymbol'] ?? '\$';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionTitle(Icons.bar_chart_rounded, 'Sales Charts'.tr(), cs),
        const SizedBox(height: 12),
        if (dailySales.isNotEmpty) ...[
          _chartCard(
            title: 'Daily Sales (Last 7 Days)'.tr(),
            chart: _buildLineChart(dailySales, 'date', const Color(0xFF6366f1), currency, isDark, cs),
            isDark: isDark,
            cs: cs,
          ),
          const SizedBox(height: 12),
        ],
        if (monthlySales.isNotEmpty)
          _chartCard(
            title: 'Monthly Sales (Last 12 Months)'.tr(),
            chart: _buildLineChart(monthlySales, 'month', const Color(0xFF10b981), currency, isDark, cs),
            isDark: isDark,
            cs: cs,
          ),
      ],
    );
  }

  Widget _chartCard({required String title, required Widget chart, required bool isDark, required ColorScheme cs}) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: GoogleFonts.dmSans(fontSize: 14, fontWeight: FontWeight.w600)),
          const SizedBox(height: 16),
          SizedBox(height: 220, child: chart),
        ],
      ),
    );
  }

  Widget _buildLineChart(List<Map<String, dynamic>> data, String labelKey, Color color, String currency, bool isDark, ColorScheme cs) {
    if (data.isEmpty) return const SizedBox.shrink();

    final revenueSpots = <FlSpot>[];
    final labels = <String>[];
    double maxRevenue = 0;

    for (int i = 0; i < data.length; i++) {
      final rev = (data[i]['revenue'] as num?)?.toDouble() ?? 0;
      revenueSpots.add(FlSpot(i.toDouble(), rev));
      labels.add(data[i][labelKey]?.toString() ?? '');
      if (rev > maxRevenue) maxRevenue = rev;
    }

    return LineChart(
      LineChartData(
        minX: 0,
        maxX: (data.length - 1).toDouble(),
        minY: 0,
        maxY: maxRevenue * 1.2,
        gridData: FlGridData(
          show: true,
          drawVerticalLine: false,
          horizontalInterval: maxRevenue > 0 ? maxRevenue / 4 : 1,
          getDrawingHorizontalLine: (value) => FlLine(
            color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : Colors.black.withValues(alpha: 0.06),
            strokeWidth: 1,
          ),
        ),
        borderData: FlBorderData(show: false),
        titlesData: FlTitlesData(
          topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
          rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
          bottomTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              reservedSize: 30,
              interval: 1,
              getTitlesWidget: (value, meta) {
                final idx = value.toInt();
                if (idx < 0 || idx >= labels.length) return const SizedBox.shrink();
                final label = labels[idx];
                final shortLabel = label.length > 6 ? label.substring(0, 6) : label;
                return SideTitleWidget(
                  meta: meta,
                  child: Text(shortLabel, style: GoogleFonts.dmSans(fontSize: 9, color: isDark ? cs.onSurfaceVariant : Colors.black38)),
                );
              },
            ),
          ),
          leftTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              reservedSize: 50,
              interval: maxRevenue > 0 ? maxRevenue / 4 : 1,
              getTitlesWidget: (value, meta) {
                return Text(
                  _formatCompact(value),
                  style: GoogleFonts.dmSans(fontSize: 9, color: isDark ? cs.onSurfaceVariant : Colors.black38),
                );
              },
            ),
          ),
        ),
        lineTouchData: LineTouchData(
          touchTooltipData: LineTouchTooltipData(
            getTooltipColor: (_) => isDark ? cs.surfaceContainerHigh : cs.surfaceContainerLowest,
            tooltipBorder: BorderSide(color: isDark ? cs.outlineVariant.withValues(alpha: 0.4) : cs.outlineVariant.withValues(alpha: 0.5)),
            tooltipRoundedRadius: 8,
            getTooltipItems: (spots) {
              return spots.map((s) {
                final idx = s.x.toInt();
                final label = idx >= 0 && idx < labels.length ? labels[idx] : '';
                return LineTooltipItem(
                  '$label\n$currency${s.y.toStringAsFixed(0)}',
                  GoogleFonts.dmSans(color: color, fontWeight: FontWeight.w600, fontSize: 12),
                );
              }).toList();
            },
          ),
        ),
        lineBarsData: [
          LineChartBarData(
            spots: revenueSpots,
            isCurved: true,
            color: color,
            barWidth: 2.5,
            dotData: FlDotData(
              show: true,
              getDotPainter: (_, __, ___, ____) => FlDotCirclePainter(radius: 3, color: color, strokeWidth: 0),
            ),
            belowBarData: BarAreaData(
              show: true,
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [color.withValues(alpha: 0.3), color.withValues(alpha: 0.0)],
              ),
            ),
          ),
        ],
      ),
    );
  }

  String _formatCompact(double val) {
    if (val >= 1000000) return '${(val / 1000000).toStringAsFixed(1)}M';
    if (val >= 1000) return '${(val / 1000).toStringAsFixed(1)}K';
    return val.toStringAsFixed(0);
  }

  // ═══════════════════════════════════════
  //  SECTION: Top Selling Products
  // ═══════════════════════════════════════

  Widget _buildTopSellingProducts(ColorScheme cs, bool isDark) {
    final products = (_data!['topSellingProducts'] as List?)?.cast<Map<String, dynamic>>() ?? [];
    if (products.isEmpty) return const SizedBox.shrink();
    final currency = (_data!['summary'] as Map?)?['currencySymbol'] ?? '\$';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionTitle(Icons.inventory_2_rounded, 'Top Selling Products'.tr(), cs),
        const SizedBox(height: 12),
        ...products.asMap().entries.map((entry) {
          final i = entry.key;
          final p = entry.value;
          final imgUrl = _resolveImg(p['imageUrl']?.toString());
          final rankColors = [const Color(0xFFf59e0b), const Color(0xFFa0a0a0), const Color(0xFFcd7f32)];

          return Container(
            margin: const EdgeInsets.only(bottom: 8),
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
            ),
            child: Row(
              children: [
                Container(
                  width: 28, height: 28,
                  decoration: BoxDecoration(
                    color: i < 3 ? rankColors[i].withValues(alpha: 0.15) : cs.primary.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Center(
                    child: Text('${i + 1}', style: GoogleFonts.dmSans(fontSize: 13, fontWeight: FontWeight.w700,
                      color: i < 3 ? rankColors[i] : cs.primary)),
                  ),
                ),
                const SizedBox(width: 10),
                ClipRRect(
                  borderRadius: BorderRadius.circular(8),
                  child: imgUrl != null
                      ? Image.network(imgUrl, width: 42, height: 42, fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) => _imgPlaceholder(42))
                      : _imgPlaceholder(42),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(p['productName'] ?? '', style: GoogleFonts.dmSans(fontSize: 13, fontWeight: FontWeight.w600), maxLines: 1, overflow: TextOverflow.ellipsis),
                      Text('${p['quantitySold']} ${'sold'.tr()}', style: GoogleFonts.dmSans(fontSize: 11, color: cs.onSurface.withValues(alpha: 0.5))),
                    ],
                  ),
                ),
                Text('$currency${(p['revenue'] as num?)?.toInt() ?? 0}',
                    style: GoogleFonts.dmSans(fontSize: 14, fontWeight: FontWeight.w700, color: const Color(0xFF10b981))),
              ],
            ),
          );
        }),
      ],
    );
  }

  Widget _imgPlaceholder(double size) {
    return Container(
      width: size, height: size,
      decoration: BoxDecoration(color: Colors.grey.shade200, borderRadius: BorderRadius.circular(8)),
      child: Icon(Icons.inventory_2_outlined, size: size * 0.5, color: Colors.grey.shade400),
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: Product Overview
  // ═══════════════════════════════════════

  Widget _buildProductOverview(ColorScheme cs, bool isDark) {
    final products = (_data!['productPreviews'] as List?)?.cast<Map<String, dynamic>>() ?? [];
    if (products.isEmpty) return const SizedBox.shrink();
    final currency = (_data!['summary'] as Map?)?['currencySymbol'] ?? '\$';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionTitle(Icons.grid_view_rounded, 'Product Overview'.tr(), cs),
        const SizedBox(height: 12),
        SizedBox(
          height: 180,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            itemCount: products.length,
            separatorBuilder: (_, __) => const SizedBox(width: 10),
            itemBuilder: (context, i) {
              final p = products[i];
              final imgUrl = _resolveImg(p['imageUrl']?.toString());
              final stock = (p['currentStock'] as num?)?.toInt() ?? 0;

              return Container(
                width: 140,
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: imgUrl != null
                          ? Image.network(imgUrl, width: double.infinity, height: 70, fit: BoxFit.cover,
                              errorBuilder: (_, __, ___) => _imgPlaceholder(70))
                          : Container(height: 70, color: Colors.grey.shade200, child: const Center(child: Icon(Icons.inventory_2_outlined))),
                    ),
                    const SizedBox(height: 6),
                    Text(p['productName'] ?? '', style: GoogleFonts.dmSans(fontSize: 11, fontWeight: FontWeight.w600), maxLines: 1, overflow: TextOverflow.ellipsis),
                    const Spacer(),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text('$currency${p['price']}', style: GoogleFonts.dmSans(fontSize: 11, fontWeight: FontWeight.w700, color: cs.primary)),
                        Text('${p['totalSoldCount']} ${'sold'.tr()}', style: GoogleFonts.dmSans(fontSize: 9, color: cs.onSurface.withValues(alpha: 0.4))),
                      ],
                    ),
                    const SizedBox(height: 2),
                    Text('${'Stock'.tr()}: $stock',
                        style: GoogleFonts.dmSans(fontSize: 10, fontWeight: FontWeight.w600,
                            color: stock < _threshold ? Colors.red : cs.onSurface.withValues(alpha: 0.5))),
                  ],
                ),
              );
            },
          ),
        ),
      ],
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: Trending Products
  // ═══════════════════════════════════════

  Widget _buildTrendingProducts(ColorScheme cs, bool isDark) {
    final products = (_data!['trendingProducts'] as List?)?.cast<Map<String, dynamic>>() ?? [];
    if (products.isEmpty) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionTitle(Icons.trending_up, 'Trending Products'.tr(), cs),
        const SizedBox(height: 12),
        ...products.map((p) {
          final imgUrl = _resolveImg(p['imageUrl']?.toString());
          return Container(
            margin: const EdgeInsets.only(bottom: 8),
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
            ),
            child: Row(
              children: [
                ClipRRect(
                  borderRadius: BorderRadius.circular(8),
                  child: imgUrl != null
                      ? Image.network(imgUrl, width: 42, height: 42, fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) => _imgPlaceholder(42))
                      : _imgPlaceholder(42),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(p['productName'] ?? '', style: GoogleFonts.dmSans(fontSize: 13, fontWeight: FontWeight.w600), maxLines: 1, overflow: TextOverflow.ellipsis),
                      Text('${p['recentQuantity']} ${'sold this week'.tr()} (${'was'.tr()} ${p['previousQuantity']})',
                          style: GoogleFonts.dmSans(fontSize: 10, color: cs.onSurface.withValues(alpha: 0.5))),
                    ],
                  ),
                ),
                Column(
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(color: const Color(0xFF10b981).withValues(alpha: 0.12), borderRadius: BorderRadius.circular(20)),
                      child: Text('↑ ${(p['growthPercent'] as num?)?.toStringAsFixed(0) ?? 0}%',
                          style: GoogleFonts.dmSans(fontSize: 11, fontWeight: FontWeight.w700, color: const Color(0xFF10b981))),
                    ),
                    const SizedBox(height: 4),
                    Text('+${p['quantityIncrease']}', style: GoogleFonts.dmSans(fontSize: 11, fontWeight: FontWeight.w600, color: const Color(0xFF10b981))),
                  ],
                ),
              ],
            ),
          );
        }),
      ],
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: Returning Customers
  // ═══════════════════════════════════════

  Widget _buildReturningCustomers(ColorScheme cs, bool isDark) {
    final rc = _data!['returningCustomers'] as Map<String, dynamic>? ?? {};
    final pct = (rc['returningPercentage'] as num?)?.toDouble() ?? 0;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionTitle(Icons.people_rounded, 'Returning Customers'.tr(), cs),
        const SizedBox(height: 12),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
          ),
          child: Row(
            children: [
              _CircularProgress(percentage: pct, size: 100),
              const SizedBox(width: 24),
              Expanded(
                child: Column(
                  children: [
                    _statItem('Total Customers'.tr(), '${rc['totalCustomers'] ?? 0}', cs),
                    const SizedBox(height: 12),
                    _statItem('Returning'.tr(), '${rc['returningCustomers'] ?? 0}', cs),
                  ],
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _statItem(String label, String value, ColorScheme cs) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: GoogleFonts.dmSans(fontSize: 12, color: cs.onSurface.withValues(alpha: 0.5))),
        Text(value, style: GoogleFonts.dmSans(fontSize: 18, fontWeight: FontWeight.w700)),
      ],
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: Product Ratings
  // ═══════════════════════════════════════

  Widget _buildProductRatings(ColorScheme cs, bool isDark) {
    final ratingStats = _data!['ratingStats'] as Map<String, dynamic>?;
    if (ratingStats == null) return const SizedBox.shrink();

    final overallRating = (ratingStats['overallAverageRating'] as num?)?.toDouble() ?? 0;
    final totalReviews = (ratingStats['totalReviews'] as num?)?.toInt() ?? 0;
    final highest = (ratingStats['highestRated'] as List?)?.cast<Map<String, dynamic>>() ?? [];
    final lowest = (ratingStats['lowestRated'] as List?)?.cast<Map<String, dynamic>>() ?? [];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionTitle(Icons.star_rounded, 'Product Ratings'.tr(), cs),
        const SizedBox(height: 12),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
          ),
          child: Column(
            children: [
              Row(
                children: [
                  Text(overallRating.toStringAsFixed(1),
                      style: GoogleFonts.playfairDisplay(fontSize: 36, fontWeight: FontWeight.w700)),
                  const SizedBox(width: 12),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(children: List.generate(5, (i) {
                        return Icon(
                          i < overallRating.round() ? Icons.star_rounded : Icons.star_outline_rounded,
                          size: 20, color: const Color(0xFFf59e0b),
                        );
                      })),
                      Text('$totalReviews ${'reviews'.tr()}',
                          style: GoogleFonts.dmSans(fontSize: 12, color: cs.onSurface.withValues(alpha: 0.5))),
                    ],
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('⬆ ${'Highest Rated'.tr()}', style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w700, color: const Color(0xFF10b981))),
                        const SizedBox(height: 6),
                        ...highest.map((p) => _ratedProductItem(p, const Color(0xFFf59e0b))),
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('⬇ ${'Lowest Rated'.tr()}', style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w700, color: const Color(0xFFef4444))),
                        const SizedBox(height: 6),
                        ...lowest.map((p) => _ratedProductItem(p, const Color(0xFFef4444))),
                      ],
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _ratedProductItem(Map<String, dynamic> p, Color color) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        children: [
          Expanded(child: Text(p['productName'] ?? '', style: GoogleFonts.dmSans(fontSize: 11), maxLines: 1, overflow: TextOverflow.ellipsis)),
          Text('★ ${(p['averageRating'] as num?)?.toStringAsFixed(1) ?? '0'}',
              style: GoogleFonts.dmSans(fontSize: 11, fontWeight: FontWeight.w700, color: color)),
          Text(' (${p['reviewCount']})', style: GoogleFonts.dmSans(fontSize: 9, color: Colors.grey)),
        ],
      ),
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: Low Stock Warnings
  // ═══════════════════════════════════════

  Widget _buildLowStockWarnings(ColorScheme cs, bool isDark) {
    final productPreviews = (_data!['productPreviews'] as List?)?.cast<Map<String, dynamic>>() ?? [];
    final List<Map<String, dynamic>> products;
    if (productPreviews.isNotEmpty) {
      products = productPreviews
          .where((p) => ((p['currentStock'] as num?)?.toInt() ?? 0) <= _threshold)
          .map((p) => {
                'productId': p['productId'],
                'productName': p['productName'],
                'imageUrl': p['imageUrl'],
                'remainingQuantity': p['currentStock'],
                'threshold': _threshold
              })
          .toList();
    } else {
      products = (_data!['lowStockProducts'] as List?)?.cast<Map<String, dynamic>>() ?? [];
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionTitle(Icons.warning_rounded, 'Low Stock Warnings'.tr(), cs),
        const SizedBox(height: 12),
        // Threshold setting
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
          ),
          child: Row(
            children: [
              Text('${'Alert Threshold'.tr()}:', style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w600)),
              const SizedBox(width: 8),
              SizedBox(
                width: 60,
                height: 36,
                child: TextField(
                  keyboardType: TextInputType.number,
                  style: GoogleFonts.dmSans(fontSize: 13),
                  decoration: InputDecoration(
                    contentPadding: const EdgeInsets.symmetric(horizontal: 8),
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                    isDense: true,
                  ),
                  controller: _thresholdCtrl,
                  onChanged: (val) => setState(() {
                    _threshold = int.tryParse(val) ?? _threshold;
                  }),
                ),
              ),
              const SizedBox(width: 8),
              SizedBox(
                height: 36,
                child: ElevatedButton(
                  onPressed: _savingThreshold ? null : _handleSaveThreshold,
                  style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(horizontal: 12)),
                  child: Text(_savingThreshold ? '...' : 'Save'.tr(), style: GoogleFonts.dmSans(fontSize: 12)),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 10),
        if (products.isEmpty)
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(24),
            decoration: BoxDecoration(
              color: const Color(0xFF10b981).withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Center(child: Text('All products are well-stocked! 🎉'.tr(),
                style: GoogleFonts.dmSans(fontSize: 13, color: const Color(0xFF10b981), fontWeight: FontWeight.w600))),
          )
        else
          ...products.map((p) {
            final imgUrl = _resolveImg(p['imageUrl']?.toString());
            return Container(
              margin: const EdgeInsets.only(bottom: 8),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: const Color(0xFFef4444).withValues(alpha: 0.05),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFef4444).withValues(alpha: 0.2)),
              ),
              child: Row(
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(8),
                    child: imgUrl != null
                        ? Image.network(imgUrl, width: 40, height: 40, fit: BoxFit.cover,
                            errorBuilder: (_, __, ___) => _imgPlaceholder(40))
                        : _imgPlaceholder(40),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(p['productName'] ?? '', style: GoogleFonts.dmSans(fontSize: 13, fontWeight: FontWeight.w600), maxLines: 1, overflow: TextOverflow.ellipsis),
                        Text('${p['remainingQuantity']} ${'remaining'.tr()}',
                            style: GoogleFonts.dmSans(fontSize: 11, color: const Color(0xFFef4444), fontWeight: FontWeight.w600)),
                      ],
                    ),
                  ),
                  Icon(Icons.warning_rounded, color: const Color(0xFFef4444).withValues(alpha: 0.6), size: 20),
                ],
              ),
            );
          }),
      ],
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: AI Recommendations
  // ═══════════════════════════════════════

  Widget _buildAiRecommendations(ColorScheme cs, bool isDark) {
    final recs = (_data!['aiRecommendations'] as List?)?.cast<Map<String, dynamic>>() ?? [];
    if (recs.isEmpty) return const SizedBox.shrink();

    IconData iconForType(String? type) {
      switch (type) {
        case 'sales_up': return Icons.trending_up;
        case 'sales_down': return Icons.trending_down;
        case 'low_stock': return Icons.warning_rounded;
        case 'rating_high': return Icons.star_rounded;
        case 'rating_low': return Icons.star_outline_rounded;
        case 'stock': return Icons.inventory_2_rounded;
        default: return Icons.lightbulb_rounded;
      }
    }

    Color colorForType(String? type) {
      switch (type) {
        case 'sales_up': case 'rating_high': return const Color(0xFF10b981);
        case 'sales_down': case 'rating_low': case 'low_stock': return const Color(0xFFef4444);
        case 'stock': return const Color(0xFFf59e0b);
        default: return const Color(0xFF6366f1);
      }
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionTitle(Icons.lightbulb_rounded, 'AI Recommendations'.tr(), cs),
        const SizedBox(height: 12),
        ...recs.map((rec) {
          final type = rec['type']?.toString();
          final color = colorForType(type);
          return Container(
            margin: const EdgeInsets.only(bottom: 8),
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.06),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: color.withValues(alpha: 0.15)),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: 32, height: 32,
                  decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(8)),
                  child: Icon(iconForType(type), size: 18, color: color),
                ),
                const SizedBox(width: 12),
                Expanded(child: Text(rec['message'] ?? '', style: GoogleFonts.dmSans(fontSize: 12, height: 1.5))),
              ],
            ),
          );
        }),
      ],
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: Reports & Export
  // ═══════════════════════════════════════

  Widget _buildReportsSection(ColorScheme cs, bool isDark) {
    const months = ['January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _sectionTitle(Icons.download_rounded, 'Reports & Export'.tr(), cs),
        const SizedBox(height: 12),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Tab selector
              Row(
                children: [
                  _tabBtn('Monthly Report'.tr(), _reportTab == 'monthly', () => setState(() => _reportTab = 'monthly'), cs),
                  const SizedBox(width: 8),
                  _tabBtn('Yearly Report'.tr(), _reportTab == 'yearly', () => setState(() => _reportTab = 'yearly'), cs),
                ],
              ),
              const SizedBox(height: 12),
              // Year + Month selectors
              Row(
                children: [
                  Expanded(
                    child: DropdownButtonFormField<int>(
                      initialValue: _reportYear,
                      decoration: InputDecoration(
                        contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                        isDense: true,
                      ),
                      items: List.generate(5, (i) => DateTime.now().year - i)
                          .map((y) => DropdownMenuItem(value: y, child: Text('$y', style: GoogleFonts.dmSans(fontSize: 13))))
                          .toList(),
                      onChanged: (v) { if (v != null) setState(() => _reportYear = v); },
                    ),
                  ),
                  if (_reportTab == 'monthly') ...[
                    const SizedBox(width: 8),
                    Expanded(
                      child: DropdownButtonFormField<int>(
                        initialValue: _reportMonth,
                        decoration: InputDecoration(
                          contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                          isDense: true,
                        ),
                        items: months.asMap().entries
                            .map((e) => DropdownMenuItem(value: e.key + 1, child: Text(e.value.tr(), style: GoogleFonts.dmSans(fontSize: 13))))
                            .toList(),
                        onChanged: (v) { if (v != null) setState(() => _reportMonth = v); },
                      ),
                    ),
                  ],
                ],
              ),
              const SizedBox(height: 12),
              // Download buttons
              Row(
                children: [
                  Expanded(
                    child: ElevatedButton.icon(
                      onPressed: _downloading != null ? null : () => _handleDownload(_reportTab, 'pdf'),
                      icon: const Icon(Icons.picture_as_pdf, size: 16),
                      label: Text(
                        _downloading == '${_reportTab}_pdf' ? 'Downloading...'.tr() : 'PDF',
                        style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w600),
                      ),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFFef4444),
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 12),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: ElevatedButton.icon(
                      onPressed: _downloading != null ? null : () => _handleDownload(_reportTab, 'excel'),
                      icon: const Icon(Icons.table_chart, size: 16),
                      label: Text(
                        _downloading == '${_reportTab}_excel' ? 'Downloading...'.tr() : 'Excel',
                        style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w600),
                      ),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFF10b981),
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 12),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _tabBtn(String label, bool active, VoidCallback onTap, ColorScheme cs) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
        decoration: BoxDecoration(
          color: active ? cs.primary.withValues(alpha: 0.1) : Colors.transparent,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: active ? cs.primary : Colors.transparent),
        ),
        child: Text(label, style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w600,
            color: active ? cs.primary : cs.onSurface.withValues(alpha: 0.5))),
      ),
    );
  }

  // ═══════════════════════════════════════
  //  SECTION: Telegram
  // ═══════════════════════════════════════

  Widget _buildTelegramSection(ColorScheme cs, bool isDark) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Collapsible header
        GestureDetector(
          onTap: () => setState(() => _tgOpen = !_tgOpen),
          child: Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
            ),
            child: Row(
              children: [
                Icon(Icons.send_rounded, size: 20, color: cs.primary),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Telegram Reports & Notifications'.tr(), style: GoogleFonts.dmSans(fontSize: 14, fontWeight: FontWeight.w600)),
                      Text('Receive automated reports and alerts on Telegram'.tr(),
                          style: GoogleFonts.dmSans(fontSize: 11, color: cs.onSurface.withValues(alpha: 0.5))),
                    ],
                  ),
                ),
                Icon(_tgOpen ? Icons.expand_less : Icons.expand_more, color: cs.onSurface.withValues(alpha: 0.4)),
              ],
            ),
          ),
        ),
        if (_tgOpen) ...[
          const SizedBox(height: 10),
          // Connection card
          _buildTgConnectionCard(cs, isDark),
          // Notification toggles
          if (_tgConnected) ...[
            const SizedBox(height: 10),
            _buildTgNotificationSection('events', '⚡ ${'Real-Time Event Notifications'.tr()}', cs, isDark),
            const SizedBox(height: 10),
            _buildTgNotificationSection('scheduled', '📅 ${'Scheduled Reports'.tr()}', cs, isDark),
            // Schedule config for enabled scheduled ones
            if (_tgNotifications.any((n) => _scheduledTypes.contains(n['notificationType']) && n['enabled'] == true)) ...[
              const SizedBox(height: 10),
              _buildTgScheduleConfig(cs, isDark),
            ],
            const SizedBox(height: 10),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _tgSaving ? null : _handleTgSave,
                child: Text(_tgSaving ? 'Saving...'.tr() : '💾 ${'Save Settings'.tr()}',
                    style: GoogleFonts.dmSans(fontWeight: FontWeight.w600)),
              ),
            ),
          ],
          if (_tgMessageText != null && _tgMessageType == 'error') ...[
            const SizedBox(height: 8),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.red.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text('❌ $_tgMessageText', style: GoogleFonts.dmSans(fontSize: 12, color: Colors.red)),
            ),
          ],
        ],
      ],
    );
  }

  Widget _buildTgConnectionCard(ColorScheme cs, bool isDark) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.send_rounded, size: 20, color: cs.primary),
              const SizedBox(width: 8),
              Text('Telegram Bot'.tr(), style: GoogleFonts.dmSans(fontSize: 14, fontWeight: FontWeight.w700)),
            ],
          ),
          const SizedBox(height: 10),
          // Status badge
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
            decoration: BoxDecoration(
              color: _tgConnected ? const Color(0xFF10b981).withValues(alpha: 0.1) : Colors.red.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(20),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  width: 8, height: 8,
                  decoration: BoxDecoration(shape: BoxShape.circle,
                      color: _tgConnected ? const Color(0xFF10b981) : Colors.red),
                ),
                const SizedBox(width: 6),
                Text(_tgConnected ? 'Connected'.tr() : 'Disconnected'.tr(),
                    style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w600,
                        color: _tgConnected ? const Color(0xFF10b981) : Colors.red)),
              ],
            ),
          ),
          if (_tgConnected && _tgMaskedId != null) ...[
            const SizedBox(height: 8),
            Text('${'Chat ID'.tr()}: $_tgMaskedId', style: GoogleFonts.dmSans(fontSize: 12, color: cs.onSurface.withValues(alpha: 0.5))),
            if (_tgConnectedAt != null) ...[
              const SizedBox(height: 4),
              Text('${'Connected At'.tr()}: $_tgConnectedAt', style: GoogleFonts.dmSans(fontSize: 11, color: cs.onSurface.withValues(alpha: 0.4))),
            ],
          ],
          const SizedBox(height: 12),
          // Action buttons
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              if (_tgConnected) ...[
                _tgActionBtn('🔄 ${'Reconnect'.tr()}', () { setState(() { _tgShowConnect = true; _tgMessageText = null; }); }, cs),
                _tgActionBtn('🔔 ${_tgTesting ? 'Sending...'.tr() : 'Test Message'.tr()}', _tgTesting ? null : _handleTgTest, cs),
                _tgActionBtn('✕ ${_tgDisconnecting ? 'Disconnecting...'.tr() : 'Disconnect'.tr()}',
                    _tgDisconnecting ? null : _handleTgDisconnect, cs, danger: true),
              ] else
                ElevatedButton(
                  onPressed: () { setState(() { _tgShowConnect = true; _tgMessageText = null; }); },
                  child: Text('✈️ ${'Connect Telegram Bot'.tr()}', style: GoogleFonts.dmSans(fontWeight: FontWeight.w600)),
                ),
            ],
          ),
          // Connect form
          if (_tgShowConnect) ...[
            const SizedBox(height: 12),
            TextField(
              controller: _tgChatIdCtrl,
              keyboardType: TextInputType.number,
              decoration: InputDecoration(
                labelText: 'Telegram Chat ID'.tr(),
                hintText: 'Enter your Chat ID'.tr(),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                isDense: true,
              ),
              style: GoogleFonts.dmSans(fontSize: 13),
              onSubmitted: (_) => _handleTgConnect(),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                ElevatedButton(
                  onPressed: _tgConnecting || _tgChatIdCtrl.text.trim().isEmpty ? null : _handleTgConnect,
                  child: Text(_tgConnecting ? 'Connecting...'.tr() : 'Connect'.tr(), style: GoogleFonts.dmSans(fontWeight: FontWeight.w600)),
                ),
                const SizedBox(width: 8),
                TextButton(
                  onPressed: () => setState(() => _tgShowConnect = false),
                  child: Text('Cancel'.tr()),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: cs.primary.withValues(alpha: 0.05),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('How to get your Chat ID'.tr(), style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w700)),
                  const SizedBox(height: 6),
                  Text('1. ${'Open Telegram and search for'.tr()} @userinfobot', style: GoogleFonts.dmSans(fontSize: 11)),
                  Text('2. ${'Start a chat and it will show your Chat ID'.tr()}', style: GoogleFonts.dmSans(fontSize: 11)),
                  Text('3. ${'Copy the ID number and paste it above'.tr()}', style: GoogleFonts.dmSans(fontSize: 11)),
                  Text('4. ${'Then search for your store bot and press'.tr()} /start', style: GoogleFonts.dmSans(fontSize: 11)),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _tgActionBtn(String label, VoidCallback? onTap, ColorScheme cs, {bool danger = false}) {
    return OutlinedButton(
      onPressed: onTap,
      style: OutlinedButton.styleFrom(
        foregroundColor: danger ? Colors.red : cs.onSurface,
        side: BorderSide(color: danger ? Colors.red.withValues(alpha: 0.3) : cs.onSurface.withValues(alpha: 0.15)),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      ),
      child: Text(label, style: GoogleFonts.dmSans(fontSize: 11, fontWeight: FontWeight.w600)),
    );
  }

  Widget _buildTgNotificationSection(String category, String title, ColorScheme cs, bool isDark) {
    final types = _notificationTypes.where((nt) => nt['category'] == category).toList();

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: GoogleFonts.dmSans(fontSize: 13, fontWeight: FontWeight.w700)),
          const SizedBox(height: 10),
          ...types.map((nt) {
            final key = nt['key'] as String;
            final notif = _getTgNotif(key);
            final enabled = notif['enabled'] == true;

            return Padding(
              padding: const EdgeInsets.only(bottom: 6),
              child: Row(
                children: [
                  Text(nt['emoji'] as String, style: const TextStyle(fontSize: 18)),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text((nt['label'] as String).tr(), style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w600)),
                        Text((nt['desc'] as String).tr(), style: GoogleFonts.dmSans(fontSize: 10, color: cs.onSurface.withValues(alpha: 0.4))),
                      ],
                    ),
                  ),
                  Switch.adaptive(
                    value: enabled,
                    onChanged: (_) => _handleTgToggle(key),
                    activeThumbColor: cs.primary,
                  ),
                ],
              ),
            );
          }),
        ],
      ),
    );
  }

  Widget _buildTgScheduleConfig(ColorScheme cs, bool isDark) {
    final enabledScheduled = _notificationTypes.where((nt) {
      return _scheduledTypes.contains(nt['key']) && _getTgNotif(nt['key'] as String)['enabled'] == true;
    }).toList();

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.schedule, size: 14, color: cs.onSurface.withValues(alpha: 0.5)),
              const SizedBox(width: 6),
              Text('Schedule configuration'.tr(), style: GoogleFonts.dmSans(fontSize: 13, fontWeight: FontWeight.w700)),
            ],
          ),
          const SizedBox(height: 12),
          ...enabledScheduled.map((nt) {
            final key = nt['key'] as String;
            final notif = _getTgNotif(key);
            final freq = notif['frequency']?.toString() ?? 'Daily';
            final hour = (notif['hour'] as num?)?.toInt() ?? 9;
            final minute = (notif['minute'] as num?)?.toInt() ?? 0;
            final tz = notif['timeZone']?.toString() ?? 'UTC';

            return Container(
              margin: const EdgeInsets.only(bottom: 12),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: cs.primary.withValues(alpha: 0.03),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: cs.primary.withValues(alpha: 0.08)),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('${nt['emoji']} ${(nt['label'] as String).tr()}',
                      style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w700)),
                  const SizedBox(height: 10),
                  // Frequency
                  DropdownButtonFormField<String>(
                    initialValue: freq,
                    decoration: InputDecoration(
                      labelText: 'Frequency'.tr(),
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                      isDense: true,
                      contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                    ),
                    items: _frequencies.map((f) => DropdownMenuItem(
                      value: f['value'] as String,
                      child: Text((f['label'] as String).tr(), style: GoogleFonts.dmSans(fontSize: 12)),
                    )).toList(),
                    onChanged: (v) { if (v != null) _handleTgScheduleChange(key, 'frequency', v); },
                  ),
                  if (freq == 'Custom') ...[
                    const SizedBox(height: 8),
                    TextField(
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                        labelText: 'Send every X days'.tr(),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                        isDense: true,
                      ),
                      style: GoogleFonts.dmSans(fontSize: 12),
                      controller: TextEditingController(text: '${notif['intervalDays'] ?? 1}'),
                      onChanged: (v) => _handleTgScheduleChange(key, 'intervalDays', int.tryParse(v) ?? 1),
                    ),
                  ],
                  if (freq == 'Weekly') ...[
                    const SizedBox(height: 8),
                    DropdownButtonFormField<int>(
                      initialValue: (notif['weeklyDay'] as num?)?.toInt() ?? 0,
                      decoration: InputDecoration(
                        labelText: 'Day'.tr(),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                        isDense: true,
                        contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      ),
                      items: _daysOfWeek.asMap().entries
                          .map((e) => DropdownMenuItem(value: e.key, child: Text(e.value.tr(), style: GoogleFonts.dmSans(fontSize: 12))))
                          .toList(),
                      onChanged: (v) { if (v != null) _handleTgScheduleChange(key, 'weeklyDay', v); },
                    ),
                  ],
                  const SizedBox(height: 8),
                  // Time picker
                  Row(
                    children: [
                      Expanded(
                        child: InkWell(
                          onTap: () async {
                            final time = await showTimePicker(
                              context: context,
                              initialTime: TimeOfDay(hour: hour, minute: minute),
                            );
                            if (time != null) {
                              _handleTgScheduleChange(key, 'hour', time.hour);
                              _handleTgScheduleChange(key, 'minute', time.minute);
                            }
                          },
                          child: InputDecorator(
                            decoration: InputDecoration(
                              labelText: 'Time'.tr(),
                              border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                              isDense: true,
                              contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                            ),
                            child: Text(
                              '${hour.toString().padLeft(2, '0')}:${minute.toString().padLeft(2, '0')}',
                              style: GoogleFonts.dmSans(fontSize: 13),
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: DropdownButtonFormField<String>(
                          initialValue: tz,
                          decoration: InputDecoration(
                            labelText: 'Timezone'.tr(),
                            border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                            isDense: true,
                            contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                          ),
                          items: _timezones.map((t) => DropdownMenuItem(
                            value: t,
                            child: Text(t, style: GoogleFonts.dmSans(fontSize: 11)),
                          )).toList(),
                          onChanged: (v) { if (v != null) _handleTgScheduleChange(key, 'timeZone', v); },
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            );
          }),
        ],
      ),
    );
  }

  // ═══════════════════════════════════════
  //  Common Helpers
  // ═══════════════════════════════════════

  Widget _sectionTitle(IconData icon, String title, ColorScheme cs) {
    return Row(
      children: [
        Icon(icon, size: 20, color: cs.primary),
        const SizedBox(width: 8),
        Text(title, style: GoogleFonts.playfairDisplay(fontSize: 18, fontWeight: FontWeight.w600)),
      ],
    );
  }
}

// ═══════════════════════════════════════
//  Reusable Widgets
// ═══════════════════════════════════════

class _DateButton extends StatelessWidget {
  final String label;
  final VoidCallback onTap;
  const _DateButton({required this.label, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(8),
      child: Container(
        height: 40,
        padding: const EdgeInsets.symmetric(horizontal: 12),
        decoration: BoxDecoration(
          border: Border.all(color: cs.onSurface.withValues(alpha: 0.12)),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Row(
          children: [
            Icon(Icons.calendar_today, size: 14, color: cs.onSurface.withValues(alpha: 0.4)),
            const SizedBox(width: 8),
            Text(label, style: GoogleFonts.dmSans(fontSize: 12, color: cs.onSurface.withValues(alpha: 0.6))),
          ],
        ),
      ),
    );
  }
}

class _AnalyticsCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color color;
  final bool isDark;

  const _AnalyticsCard({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 32, height: 32,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(icon, size: 18, color: color),
          ),
          const Spacer(),
          Text(label, style: GoogleFonts.dmSans(fontSize: 10, color: cs.onSurface.withValues(alpha: 0.5)),
              maxLines: 1, overflow: TextOverflow.ellipsis),
          const SizedBox(height: 2),
          Text(value, style: GoogleFonts.dmSans(fontSize: 16, fontWeight: FontWeight.w700),
              maxLines: 1, overflow: TextOverflow.ellipsis),
        ],
      ),
    );
  }
}

class _GrowthCard extends StatelessWidget {
  final String label;
  final double percent;
  final String prevLabel;
  final String prevValue;
  final String curLabel;
  final String curValue;
  final bool isDark;

  const _GrowthCard({
    required this.label,
    required this.percent,
    required this.prevLabel,
    required this.prevValue,
    required this.curLabel,
    required this.curValue,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final isPositive = percent > 0;
    final isNegative = percent < 0;
    final badgeColor = isPositive ? const Color(0xFF10b981) : isNegative ? const Color(0xFFef4444) : Colors.grey;

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: isDark ? cs.surfaceContainer : cs.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: isDark ? cs.outlineVariant.withValues(alpha: 0.3) : cs.outlineVariant.withValues(alpha: 0.5)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: GoogleFonts.dmSans(fontSize: 11, fontWeight: FontWeight.w600)),
          const SizedBox(height: 6),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
            decoration: BoxDecoration(
              color: badgeColor.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(20),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(isPositive ? Icons.trending_up : isNegative ? Icons.trending_down : Icons.remove,
                    size: 14, color: badgeColor),
                const SizedBox(width: 4),
                Text('${isPositive ? '+' : ''}${percent.toStringAsFixed(1)}%',
                    style: GoogleFonts.dmSans(fontSize: 12, fontWeight: FontWeight.w700, color: badgeColor)),
              ],
            ),
          ),
          const SizedBox(height: 10),
          _growthValueRow(prevLabel, prevValue, Theme.of(context).colorScheme),
          const SizedBox(height: 4),
          _growthValueRow(curLabel, curValue, Theme.of(context).colorScheme),
        ],
      ),
    );
  }

  Widget _growthValueRow(String label, String value, ColorScheme cs) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Flexible(child: Text(label, style: GoogleFonts.dmSans(fontSize: 9, color: cs.onSurface.withValues(alpha: 0.4)), overflow: TextOverflow.ellipsis)),
        Text(value, style: GoogleFonts.dmSans(fontSize: 11, fontWeight: FontWeight.w700)),
      ],
    );
  }
}

class _CircularProgress extends StatelessWidget {
  final double percentage;
  final double size;
  const _CircularProgress({required this.percentage, this.size = 120});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: size,
      height: size,
      child: Stack(
        alignment: Alignment.center,
        children: [
          SizedBox(
            width: size,
            height: size,
            child: CircularProgressIndicator(
              value: min(percentage, 100) / 100,
              strokeWidth: 8,
              backgroundColor: Theme.of(context).colorScheme.onSurface.withValues(alpha: 0.08),
              valueColor: const AlwaysStoppedAnimation<Color>(Color(0xFF6366f1)),
              strokeCap: StrokeCap.round,
            ),
          ),
          Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text('${percentage.toStringAsFixed(1)}%',
                  style: GoogleFonts.dmSans(fontSize: 20, fontWeight: FontWeight.w700)),
              Text('Returning'.tr(), style: GoogleFonts.dmSans(fontSize: 10,
                  color: Theme.of(context).colorScheme.onSurface.withValues(alpha: 0.4))),
            ],
          ),
        ],
      ),
    );
  }
}
