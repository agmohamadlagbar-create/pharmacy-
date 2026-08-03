import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:lottie/lottie.dart' as lottie;

/// Displays the animated add-to-cart Lottie overlay (matching web design).
void showAddToCartAnimation(BuildContext context) {
  showDialog(
    context: context,
    useRootNavigator: true,
    barrierColor: Colors.black.withValues(alpha: 0.45),
    barrierDismissible: false,
    builder: (ctx) {
      Future.delayed(const Duration(milliseconds: 1800), () {
        if (ctx.mounted && Navigator.canPop(ctx)) {
          Navigator.of(ctx, rootNavigator: true).pop();
        }
      });
      final theme = Theme.of(context);
      final isDark = theme.brightness == Brightness.dark;

      return Center(
        child: Material(
          color: Colors.transparent,
          child: Container(
            width: 220,
            height: 220,
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
            decoration: BoxDecoration(
              color: isDark ? const Color(0xFF1E293B) : Colors.white,
              borderRadius: BorderRadius.circular(24),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.35),
                  blurRadius: 24,
                  offset: const Offset(0, 8),
                ),
              ],
              border: Border.all(
                color: isDark
                    ? Colors.white.withValues(alpha: 0.15)
                    : Colors.black.withValues(alpha: 0.08),
              ),
            ),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Expanded(
                  child: Container(
                    decoration: BoxDecoration(
                      color: isDark ? Colors.white : Colors.transparent,
                      borderRadius: BorderRadius.circular(16),
                    ),
                    padding: const EdgeInsets.all(8),
                    child: lottie.Lottie.asset(
                      'assets/lottie/add-to-cart.json',
                      repeat: false,
                      fit: BoxFit.contain,
                    ),
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  'Added to Cart!'.tr(),
                  style: GoogleFonts.inter(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: isDark ? Colors.white : const Color(0xFF1E293B),
                  ),
                ),
              ],
            ),
          ),
        ),
      );
    },
  );
}
