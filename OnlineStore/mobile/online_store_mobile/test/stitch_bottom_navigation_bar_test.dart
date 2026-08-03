import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:online_store_mobile/widgets/stitch_bottom_navigation_bar.dart';

void main() {
  testWidgets('StitchBottomNavigationBar retains isGuest flag state',
      (WidgetTester tester) async {
    const navBarGuest = StitchBottomNavigationBar(
      currentIndex: 0,
      isGuest: true,
      onTap: _dummyOnTap,
    );

    const navBarUser = StitchBottomNavigationBar(
      currentIndex: 0,
      isGuest: false,
      onTap: _dummyOnTap,
    );

    expect(navBarGuest.isGuest, isTrue);
    expect(navBarUser.isGuest, isFalse);
  });
}

void _dummyOnTap(int index) {}
