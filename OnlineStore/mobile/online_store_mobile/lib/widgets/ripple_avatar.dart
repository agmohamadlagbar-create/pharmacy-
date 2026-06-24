import 'package:flutter/material.dart';

class RippleAvatar extends StatefulWidget {
  final ImageProvider? image;
  final double size;
  final Color? glowColor;
  final double glowThickness;
  final Duration duration;
  final VoidCallback? onTap;

  const RippleAvatar({
    super.key,
    this.image,
    this.size = 80,
    this.glowColor,
    this.glowThickness = 2.0,
    this.duration = const Duration(seconds: 2),
    this.onTap,
  });

  @override
  State<RippleAvatar> createState() => _RippleAvatarState();
}

class _RippleAvatarState extends State<RippleAvatar>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _pulseAnimation;
  late Animation<double> _opacityAnimation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: widget.duration,
    )..repeat();

    _pulseAnimation = Tween<double>(begin: 0.9, end: 1.6).animate(
      CurvedAnimation(
        parent: _controller,
        curve: Curves.easeOut,
      ),
    );

    _opacityAnimation = Tween<double>(begin: 0.6, end: 0.0).animate(
      CurvedAnimation(
        parent: _controller,
        curve: Curves.easeOut,
      ),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final color = widget.glowColor ?? Theme.of(context).colorScheme.primary;

    return GestureDetector(
      onTap: widget.onTap,
      child: Stack(
        alignment: Alignment.center,
        children: [
          // Animated Pulse Ring
          AnimatedBuilder(
            animation: _controller,
            builder: (context, child) {
              return Transform.scale(
                scale: _pulseAnimation.value,
                child: Opacity(
                  opacity: _opacityAnimation.value,
                  child: Container(
                    width: widget.size,
                    height: widget.size,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      boxShadow: [
                        BoxShadow(
                          color: color.withValues(alpha: 0.8),
                          blurRadius: 15,
                          spreadRadius: 8,
                        ),
                      ],
                    ),
                  ),
                ),
              );
            },
          ),
          // Static Avatar Circle
          Container(
            width: widget.size,
            height: widget.size,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: Theme.of(context).colorScheme.surfaceContainerHighest,
              border: Border.all(
                color: color,
                width: widget.glowThickness,
              ),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.2),
                  blurRadius: 10,
                  offset: const Offset(0, 4),
                ),
              ],
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
                      size: widget.size * 0.5,
                      color: color,
                    ),
            ),
          ),
        ],
      ),
    );
  }
}

