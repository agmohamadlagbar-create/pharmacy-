import 'package:flutter/material.dart';
import 'package:easy_localization/easy_localization.dart';

class CustomStoreAppBar extends StatelessWidget implements PreferredSizeWidget {
  final String? initialSearchQuery;
  final Function(String)? onSearchChanged;
  final VoidCallback? onSearchTap;
  final VoidCallback? onLeadingTap;
  final bool isRoot;

  final int cartCount;
  final VoidCallback? onCartTap;

  const CustomStoreAppBar({
    super.key,
    this.initialSearchQuery,
    this.onSearchChanged,
    this.onSearchTap,
    this.onLeadingTap,
    this.isRoot = false,
    this.cartCount = 0,
    this.onCartTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final topPadding = MediaQuery.of(context).padding.top;

    return Container(
      color: theme.colorScheme.surface,
      padding: EdgeInsets.fromLTRB(12, topPadding + 8, 12, 12),
      child: Row(
        children: [
          // Sidebar / Back button next to search bar
          IconButton(
            icon: Icon(isRoot ? Icons.menu : Icons.arrow_back),
            tooltip: isRoot ? 'Open Menu'.tr() : 'Back'.tr(),
            onPressed: onLeadingTap ??
                () {
                  if (isRoot) {
                    Scaffold.of(context).openDrawer();
                  } else {
                    Navigator.of(context).pop();
                  }
                },
          ),
          const SizedBox(width: 4),
          // Search bar
          Expanded(
            child: Container(
              decoration: BoxDecoration(
                color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
                borderRadius: BorderRadius.circular(25),
              ),
              child: TextField(
                onChanged: onSearchChanged,
                onTap: onSearchTap,
                readOnly: onSearchTap != null && onSearchChanged == null,
                decoration: InputDecoration(
                  hintText: 'Search products...'.tr(),
                  prefixIcon: const Icon(Icons.search),
                  border: InputBorder.none,
                  contentPadding: const EdgeInsets.symmetric(vertical: 12),
                ),
              ),
            ),
          ),
          if (onCartTap != null) ...[
            const SizedBox(width: 8),
            Stack(
              alignment: Alignment.center,
              children: [
                IconButton(
                  icon: const Icon(Icons.shopping_bag_outlined),
                  onPressed: onCartTap,
                ),
                if (cartCount > 0)
                  Positioned(
                    right: 4,
                    top: 4,
                    child: Container(
                      padding: const EdgeInsets.all(4),
                      decoration: const BoxDecoration(
                        color: Colors.redAccent,
                        shape: BoxShape.circle,
                      ),
                      constraints: const BoxConstraints(
                        minWidth: 16,
                        minHeight: 16,
                      ),
                      child: Text(
                        '$cartCount',
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                        ),
                        textAlign: TextAlign.center,
                      ),
                    ),
                  ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  @override
  Size get preferredSize => const Size.fromHeight(70);
}

