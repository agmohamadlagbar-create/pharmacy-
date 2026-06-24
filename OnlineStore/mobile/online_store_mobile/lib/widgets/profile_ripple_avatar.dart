import 'package:flutter/material.dart';
import 'dart:math' as math;

class ProfileRippleAvatar extends StatefulWidget {
  final ImageProvider? image;
  final double size;
  final Color? glowColor;

  const ProfileRippleAvatar({
    super.key,
    this.image,
    this.size = 150,
    this.glowColor,
  });

  @override
  State<ProfileRippleAvatar> createState() => _ProfileRippleAvatarState();
}

class _ProfileRippleAvatarState extends State<ProfileRippleAvatar>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 4),
    )..repeat();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final color = widget.glowColor ?? Theme.of(context).colorScheme.primary;

    return Center(
      child: Stack(
        alignment: Alignment.center,
        children: [
          // Orbital Animations
          AnimatedBuilder(
            animation: _controller,
            builder: (context, child) {
              return CustomPaint(
                painter: _OrbitalPainter(
                  progress: _controller.value,
                  color: color,
                ),
                size: Size(widget.size * 2, widget.size * 2),
              );
            },
          ),

          // Main Avatar
          Container(
            width: widget.size,
            height: widget.size,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              boxShadow: [
                BoxShadow(
                  color: color.withValues(alpha: 0.4),
                  blurRadius: 20,
                  spreadRadius: 5,
                ),
              ],
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [
                  color.withValues(alpha: 0.8),
                  Colors.white.withValues(alpha: 0.9),
                ],
              ),
            ),
            padding: const EdgeInsets.all(4),
            child: Container(
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: Theme.of(context).colorScheme.surface,
              ),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(widget.size / 2),
                child: widget.image != null
                    ? Image(
                        image: widget.image!,
                        fit: BoxFit.cover,
                      )
                    : Icon(
                        Icons.person,
                        size: widget.size * 0.6,
                        color: color,
                      ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _OrbitalPainter extends CustomPainter {
  final double progress;
  final Color color;

  _OrbitalPainter({required this.progress, required this.color});

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final baseRadius = size.width / 4;

    // Ring 1: Standard Pulse
    _drawRing(canvas, center, baseRadius * (1.0 + progress * 0.4),
        color.withValues(alpha: (1.0 - progress) * 0.5), 2.0);

    // Ring 2: Delayed Pulse
    double delayedProgress = (progress + 0.33) % 1.0;
    _drawRing(canvas, center, baseRadius * (1.0 + delayedProgress * 0.5),
        color.withValues(alpha: (1.0 - delayedProgress) * 0.3), 1.5,
        isDashed: true);

    // Ring 3: Slow Orbital
    double slowProgress = (progress + 0.66) % 1.0;
    _drawRing(canvas, center, baseRadius * (1.1 + slowProgress * 0.6),
        color.withValues(alpha: (1.0 - slowProgress) * 0.2), 3.0,
        blur: 4.0);
  }

  void _drawRing(Canvas canvas, Offset center, double radius, Color color,
      double strokeWidth,
      {bool isDashed = false, double blur = 0}) {
    final paint = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = strokeWidth;

    if (blur > 0) {
      paint.maskFilter = MaskFilter.blur(BlurStyle.normal, blur);
    }

    if (isDashed) {
      // Simple dashed circle implementation
      const double dashWidth = 10.0;
      const double dashSpace = 8.0;
      double currentAngle = 0.0;
      while (currentAngle < 2 * math.pi) {
        double sweepAngle = dashWidth / radius;
        canvas.drawArc(
          Rect.fromCircle(center: center, radius: radius),
          currentAngle,
          sweepAngle,
          false,
          paint,
        );
        currentAngle += sweepAngle + (dashSpace / radius);
      }
    } else {
      canvas.drawCircle(center, radius, paint);
    }
  }

  @override
  bool shouldRepaint(_OrbitalPainter oldDelegate) => true;
}

