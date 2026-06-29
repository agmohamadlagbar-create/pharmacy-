import 'package:flutter/material.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';

class NotificationsPage extends StatefulWidget {
  const NotificationsPage({super.key});

  @override
  State<NotificationsPage> createState() => _NotificationsPageState();
}

class _NotificationsPageState extends State<NotificationsPage> {
  List<Map<String, dynamic>> _notifications = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final res = await Api.get('/api/notifications');
      if (mounted) {
        final data =
            res is List ? res : (res['data'] is List ? res['data'] : []);
        setState(() {
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
              backgroundColor: Colors.redAccent),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: Text('Notifications'.tr())),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _notifications.isEmpty
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.notifications_none_rounded,
                          size: 64,
                          color: theme.colorScheme.onSurface
                              .withValues(alpha: 0.3)),
                      const SizedBox(height: 12),
                      Text('No notifications yet.'.tr(),
                          style: const TextStyle(color: Colors.grey)),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _notifications.length,
                    itemBuilder: (ctx, i) {
                      final n = _notifications[i];
                      final isRead = n['isRead'] == true;
                      return Card(
                        margin: const EdgeInsets.only(bottom: 8),
                        color: isRead
                            ? null
                            : theme.colorScheme.primary.withValues(alpha: 0.06),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12)),
                        child: ListTile(
                          contentPadding: const EdgeInsets.symmetric(
                              horizontal: 16, vertical: 4),
                          leading: CircleAvatar(
                            backgroundColor: isRead
                                ? theme.colorScheme.surfaceContainerHighest
                                : theme.colorScheme.primary
                                    .withValues(alpha: 0.15),
                            child: Icon(
                              isRead
                                  ? Icons.notifications_rounded
                                  : Icons.notifications_active_rounded,
                              color: isRead
                                  ? Colors.grey
                                  : theme.colorScheme.primary,
                              size: 20,
                            ),
                          ),
                          title: Text(n['message'] ?? '',
                              style: TextStyle(
                                  fontWeight: isRead
                                      ? FontWeight.normal
                                      : FontWeight.bold,
                                  fontSize: 14)),
                          subtitle: Text(n['createdAt'] ?? '',
                              style: const TextStyle(fontSize: 11)),
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}
