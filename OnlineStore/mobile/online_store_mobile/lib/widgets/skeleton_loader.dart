import 'package:flutter/material.dart';

/// A widget that applies a shimmer effect to its child.
class Shimmer extends StatefulWidget {
  final Widget child;
  final Duration period;
  final Color baseColor;
  final Color highlightColor;

  const Shimmer({
    super.key,
    required this.child,
    this.period = const Duration(milliseconds: 1500),
    this.baseColor = const Color(0xFFE0E0E0),
    this.highlightColor = const Color(0xFFF5F5F5),
  });

  /// Factory constructor for dark theme variants usually
  factory Shimmer.dark({
    Key? key,
    required Widget child,
  }) {
    return Shimmer(
      key: key,
      baseColor: Colors.grey.shade800,
      highlightColor: Colors.grey.shade700,
      child: child,
    );
  }

  @override
  State<Shimmer> createState() => _ShimmerState();
}

class _ShimmerState extends State<Shimmer> with SingleTickerProviderStateMixin {
  late AnimationController _controller;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(vsync: this, duration: widget.period)
      ..repeat();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _controller,
      builder: (context, child) {
        final double gradientPosition = _controller.value * 2 - 1; // -1 to 1

        return ShaderMask(
          blendMode: BlendMode.srcATop,
          shaderCallback: (bounds) {
            return LinearGradient(
              begin: const Alignment(-1.0, -0.3),
              end: const Alignment(1.0, 0.3),
              stops: const [0.0, 0.5, 1.0],
              colors: [
                widget.baseColor,
                widget.highlightColor,
                widget.baseColor,
              ],
              transform:
                  _SlidingGradientTransform(slidePercent: gradientPosition),
            ).createShader(bounds);
          },
          child: child,
        );
      },
      child: widget.child,
    );
  }
}

class _SlidingGradientTransform extends GradientTransform {
  final double slidePercent;

  const _SlidingGradientTransform({required this.slidePercent});

  @override
  Matrix4? transform(Rect bounds, {TextDirection? textDirection}) {
    return Matrix4.translationValues(bounds.width * slidePercent, 0.0, 0.0);
  }
}

/// A standard block container used inside Shimmer widgets.
class Skeleton extends StatelessWidget {
  final double? width;
  final double? height;
  final BorderRadiusGeometry? borderRadius;
  final BoxShape shape;

  const Skeleton({
    super.key,
    this.width,
    this.height,
    this.borderRadius,
    this.shape = BoxShape.rectangle,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: width,
      height: height,
      decoration: BoxDecoration(
        color:
            Colors.white, // Colors don't matter much as Shimmer overrides them
        borderRadius: shape == BoxShape.circle
            ? null
            : (borderRadius ?? BorderRadius.circular(8)),
        shape: shape,
      ),
    );
  }
}

/// A pre-built skeleton loader for product cards.
class SkeletonLoader extends StatelessWidget {
  const SkeletonLoader({super.key});

  @override
  Widget build(BuildContext context) {
    return Shimmer(
      child: Card(
        clipBehavior: Clip.antiAlias,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: const BorderSide(color: Color(0x14000000), width: 0.5),
        ),
        color: Colors.white,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Expanded(
              child: Skeleton(
                borderRadius: BorderRadius.zero,
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Skeleton(height: 13, width: 100),
                  const SizedBox(height: 6),
                  const Skeleton(height: 11, width: 60),
                  const SizedBox(height: 12),
                  Skeleton(
                    height: 36,
                    width: double.infinity,
                    borderRadius: BorderRadius.circular(8),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
