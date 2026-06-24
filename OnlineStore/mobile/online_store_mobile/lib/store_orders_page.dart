import 'package:flutter/material.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';

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
          SnackBar(
              content: Text('${'Error:'.tr()} $e'), backgroundColor: Colors.redAccent),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: Text('Store Orders'.tr())),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _orders.isEmpty
              ? Center(
                  child: Text('No orders yet.'.tr(),
                      style: const TextStyle(color: Colors.grey)))
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _orders.length,
                    itemBuilder: (ctx, i) {
                      final o = _orders[i];
                      final total = (o['totalAmount'] as num?)?.toDouble() ?? 0;
                      return Card(
                        margin: const EdgeInsets.only(bottom: 12),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(14)),
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisAlignment:
                                    MainAxisAlignment.spaceBetween,
                                children: [
                                  Text('${'Order'.tr()} #${o['id']}',
                                      style: const TextStyle(
                                          fontWeight: FontWeight.bold,
                                          fontSize: 16)),
                                  Container(
                                    padding: const EdgeInsets.symmetric(
                                        horizontal: 10, vertical: 4),
                                    decoration: BoxDecoration(
                                      color: theme.colorScheme.primary
                                          .withValues(alpha: 0.12),
                                      borderRadius: BorderRadius.circular(20),
                                    ),
                                    child: Text((o['status'] ?? '').toString().tr(),
                                        style: TextStyle(
                                            color: theme.colorScheme.primary,
                                            fontWeight: FontWeight.bold,
                                            fontSize: 11)),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 8),
                              Text('${'Total:'.tr()} \$${total.toStringAsFixed(2)}',
                                  style: TextStyle(
                                      color: theme.colorScheme.onSurface
                                          .withValues(alpha: 0.6))),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}

