import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';

class CustomStoreAppBar extends StatefulWidget implements PreferredSizeWidget {
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
  State<CustomStoreAppBar> createState() => _CustomStoreAppBarState();

  @override
  Size get preferredSize => const Size.fromHeight(70);
}

class _CustomStoreAppBarState extends State<CustomStoreAppBar> {
  late TextEditingController _searchController;

  @override
  void initState() {
    super.initState();
    _searchController = TextEditingController(text: widget.initialSearchQuery ?? '');
  }

  @override
  void didUpdateWidget(covariant CustomStoreAppBar oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.initialSearchQuery != oldWidget.initialSearchQuery &&
        widget.initialSearchQuery != _searchController.text) {
      _searchController.text = widget.initialSearchQuery ?? '';
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final topPadding = MediaQuery.of(context).padding.top;

    final isClickableSearch = widget.onSearchTap != null && widget.onSearchChanged == null;

    return Container(
      decoration: BoxDecoration(
        color: colorScheme.surface,
        border: Border(
          bottom: BorderSide(
            color: colorScheme.outlineVariant.withValues(alpha: 0.5),
            width: 1,
          ),
        ),
      ),
      padding: EdgeInsets.fromLTRB(12, topPadding + 6, 12, 10),
      child: Row(
        children: [
          // Menu / Back Button
          IconButton(
            icon: Icon(widget.isRoot ? Icons.menu_rounded : Icons.arrow_back_rounded),
            color: colorScheme.onSurface,
            tooltip: widget.isRoot ? 'Open Menu'.tr() : 'Back'.tr(),
            onPressed: widget.onLeadingTap ??
                () {
                  if (widget.isRoot) {
                    Scaffold.of(context).openDrawer();
                  } else {
                    Navigator.of(context).pop();
                  }
                },
          ),
          const SizedBox(width: 4),

          // Search Container
          Expanded(
            child: Container(
              height: 44,
              decoration: BoxDecoration(
                color: colorScheme.surfaceContainerHighest.withValues(alpha: 0.6),
                borderRadius: BorderRadius.circular(22),
                border: Border.all(
                  color: colorScheme.outlineVariant.withValues(alpha: 0.4),
                ),
              ),
              child: isClickableSearch
                  ? InkWell(
                      onTap: widget.onSearchTap,
                      borderRadius: BorderRadius.circular(22),
                      child: Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 14),
                        child: Row(
                          children: [
                            Icon(
                              Icons.search_rounded,
                              size: 20,
                              color: colorScheme.onSurfaceVariant,
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Text(
                                _searchController.text.isNotEmpty
                                    ? _searchController.text
                                    : 'Search products...'.tr(),
                                style: GoogleFonts.inter(
                                  fontSize: 14,
                                  color: _searchController.text.isNotEmpty
                                      ? colorScheme.onSurface
                                      : colorScheme.onSurfaceVariant,
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          ],
                        ),
                      ),
                    )
                  : TextField(
                      controller: _searchController,
                      onChanged: widget.onSearchChanged,
                      onTap: widget.onSearchTap,
                      textAlignVertical: TextAlignVertical.center,
                      style: GoogleFonts.inter(
                        fontSize: 14,
                        color: colorScheme.onSurface,
                      ),
                      decoration: InputDecoration(
                        isDense: true,
                        hintText: 'Search products...'.tr(),
                        hintStyle: GoogleFonts.inter(
                          fontSize: 14,
                          color: colorScheme.onSurfaceVariant,
                        ),
                        prefixIcon: Icon(
                          Icons.search_rounded,
                          size: 20,
                          color: colorScheme.onSurfaceVariant,
                        ),
                        suffixIcon: _searchController.text.isNotEmpty && widget.onSearchChanged != null
                            ? IconButton(
                                icon: Icon(
                                  Icons.clear_rounded,
                                  size: 18,
                                  color: colorScheme.onSurfaceVariant,
                                ),
                                onPressed: () {
                                  _searchController.clear();
                                  widget.onSearchChanged!('');
                                  setState(() {});
                                },
                              )
                            : null,
                        border: InputBorder.none,
                        enabledBorder: InputBorder.none,
                        focusedBorder: InputBorder.none,
                        contentPadding: const EdgeInsets.symmetric(
                          horizontal: 12,
                          vertical: 10,
                        ),
                      ),
                    ),
            ),
          ),

          const SizedBox(width: 4),

          // Cart Button
          if (widget.onCartTap != null) ...[
            Stack(
              alignment: Alignment.center,
              children: [
                IconButton(
                  icon: Icon(
                    Icons.shopping_bag_outlined,
                    size: 22,
                    color: colorScheme.onSurface,
                  ),
                  onPressed: widget.onCartTap,
                ),
                if (widget.cartCount > 0)
                  Positioned(
                    right: 4,
                    top: 4,
                    child: Container(
                      padding: const EdgeInsets.all(4),
                      decoration: BoxDecoration(
                        color: colorScheme.primary,
                        shape: BoxShape.circle,
                        border: Border.all(
                          color: colorScheme.surface,
                          width: 1.5,
                        ),
                      ),
                      constraints: const BoxConstraints(
                        minWidth: 16,
                        minHeight: 16,
                      ),
                      child: Text(
                        widget.cartCount > 99 ? '99+' : '${widget.cartCount}',
                        style: GoogleFonts.inter(
                          color: Colors.white,
                          fontSize: 9,
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
}
