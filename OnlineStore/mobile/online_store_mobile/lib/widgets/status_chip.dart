import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

enum StatusChipType { success, warning, error, info }

class StatusChip extends StatelessWidget {
  final String label;
  final StatusChipType type;
  final bool showDot;
  final EdgeInsetsGeometry padding;

  const StatusChip({
    super.key,
    required this.label,
    this.type = StatusChipType.info,
    this.showDot = false,
    this.padding = const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
  });

  Color _getColor(BuildContext context) {
    final theme = Theme.of(context);
    switch (type) {
      case StatusChipType.success:
        return const Color(0xFF10B981);
      case StatusChipType.warning:
        return const Color(0xFFF59E0B);
      case StatusChipType.error:
        return theme.colorScheme.error;
      case StatusChipType.info:
        return theme.colorScheme.primary;
    }
  }

  @override
  Widget build(BuildContext context) {
    final color = _getColor(context);

    return Container(
      padding: padding,
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(9999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          if (showDot) ...[
            Container(
              width: 6,
              height: 6,
              decoration: BoxDecoration(
                color: color,
                shape: BoxShape.circle,
              ),
            ),
            const SizedBox(width: 6),
          ],
          Text(
            label,
            style: GoogleFonts.inter(
              color: color,
              fontSize: 12,
              fontWeight: FontWeight.w600,
              letterSpacing: 0.2,
            ),
          ),
        ],
      ),
    );
  }
}
