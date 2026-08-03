import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:flutter_stripe/flutter_stripe.dart' hide Card;
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'cart_page.dart';
import 'categories_page.dart';
import 'profile_page.dart';
import 'edit_profile_page.dart';
import 'category_products_page.dart';
import 'admin_page.dart';
import 'store_products_page.dart';
import 'store_orders_page.dart';
import 'store_analytics_page.dart';
import 'delivery_jobs_page.dart';
import 'notifications_page.dart';
import 'settings_page.dart';
import 'checkout_page.dart';
import 'payment_page.dart';
import 'add_card_page.dart';
import 'search_products_page.dart';
import 'favorites_page.dart';
import 'delivered_orders_page.dart';
import 'active_orders_page.dart';
import 'push_notification_service.dart';
import 'utils/app_theme.dart';
import 'utils/animation_utils.dart';

enum AccountType { user, shop, delivery }

class TypewriterText extends StatefulWidget {
  final String text;
  final Duration typingSpeed;
  final Duration deletingSpeed;
  final Duration delay;

  const TypewriterText({
    super.key,
    required this.text,
    this.typingSpeed = const Duration(milliseconds: 80),
    this.deletingSpeed = const Duration(milliseconds: 40),
    this.delay = const Duration(seconds: 2),
  });

  @override
  State<TypewriterText> createState() => _TypewriterTextState();
}

class _TypewriterTextState extends State<TypewriterText> {
  String _displayedText = '';
  int _index = 0;
  bool _isDeleting = false;
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _startTimer();
  }

  @override
  void didUpdateWidget(covariant TypewriterText oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.text != widget.text) {
      _timer?.cancel();
      _displayedText = '';
      _index = 0;
      _isDeleting = false;
      _startTimer();
    }
  }

  void _startTimer() {
    _timer?.cancel();
    final delay = _isDeleting ? widget.deletingSpeed : widget.typingSpeed;
    _timer = Timer(delay, _onTick);
  }

  void _onTick() {
    if (!mounted) return;

    setState(() {
      if (!_isDeleting && _index < widget.text.length) {
        _displayedText += widget.text[_index];
        _index++;
        _startTimer();
      } else if (!_isDeleting && _index == widget.text.length) {
        // Pausing before delete
        _timer = Timer(widget.delay, () {
          if (mounted) {
            setState(() => _isDeleting = true);
            _startTimer();
          }
        });
      } else if (_isDeleting && _index > 0) {
        _displayedText = widget.text.substring(0, _index - 1);
        _index--;
        _startTimer();
      } else if (_isDeleting && _index == 0) {
        _isDeleting = false;
        _startTimer();
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      alignment: Alignment.center,
      height: 24,
      margin: const EdgeInsets.only(bottom: 16),
      child: Text(
        '$_displayedText|',
        style: TextStyle(
          fontWeight: FontWeight.bold,
          color: Theme.of(context).colorScheme.primary,
          fontSize: 14,
        ),
      ),
    );
  }
}

enum AuthMode { login, signup }

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await EasyLocalization.ensureInitialized();
  PushNotificationService().initializeAndRegisterToken();

  runApp(
    EasyLocalization(
      supportedLocales: const [Locale('en'), Locale('ar'), Locale('tr')],
      path: 'assets/translations',
      fallbackLocale: const Locale('en'),
      useOnlyLangCode: true,
      child: const OnlineStoreApp(),
    ),
  );
}

class OnlineStoreApp extends StatefulWidget {
  const OnlineStoreApp({super.key});

  static OnlineStoreAppState? of(BuildContext context) =>
      context.findAncestorStateOfType<OnlineStoreAppState>();

  @override
  State<OnlineStoreApp> createState() => OnlineStoreAppState();
}

class OnlineStoreAppState extends State<OnlineStoreApp> {
  ThemeMode _themeMode = ThemeMode.system;

  Map<String, dynamic> _cart = {'items': [], 'totalAmount': 0.0};

  @override
  void initState() {
    super.initState();
    _loadThemeMode();
    _checkToken();
    _initStripe();
  }

  Future<void> _loadThemeMode() async {
    final prefs = await SharedPreferences.getInstance();
    final savedMode = prefs.getString('themeMode');
    if (savedMode != null && mounted) {
      setState(() {
        if (savedMode == 'light') {
          _themeMode = ThemeMode.light;
        } else if (savedMode == 'dark') {
          _themeMode = ThemeMode.dark;
        } else {
          _themeMode = ThemeMode.system;
        }
      });
    } else {
      // Legacy fallback check
      final isLight = prefs.getBool('isLightTheme');
      if (isLight != null && mounted) {
        setState(() {
          _themeMode = isLight ? ThemeMode.light : ThemeMode.dark;
        });
      }
    }
  }

  Future<void> setThemeMode(ThemeMode mode) async {
    setState(() {
      _themeMode = mode;
    });
    final prefs = await SharedPreferences.getInstance();
    String modeStr = 'system';
    if (mode == ThemeMode.light) modeStr = 'light';
    if (mode == ThemeMode.dark) modeStr = 'dark';
    await prefs.setString('themeMode', modeStr);
    await prefs.setBool('isLightTheme', mode == ThemeMode.light);
  }

  void toggleTheme() {
    if (_themeMode == ThemeMode.system) {
      setThemeMode(ThemeMode.light);
    } else if (_themeMode == ThemeMode.light) {
      setThemeMode(ThemeMode.dark);
    } else {
      setThemeMode(ThemeMode.system);
    }
  }

  ThemeMode get themeMode => _themeMode;

  bool get isDark {
    if (_themeMode == ThemeMode.system) {
      return WidgetsBinding.instance.platformDispatcher.platformBrightness == Brightness.dark;
    }
    return _themeMode == ThemeMode.dark;
  }

  Future<void> _initStripe() async {
    try {
      final config = await Api.get('/api/payment/config');
      Stripe.publishableKey = config['publishableKey'];
      if (!kIsWeb) {
        await Stripe.instance.applySettings();
      }
    } catch (e) {
      debugPrint('Failed to load Stripe config: $e');
    }
  }

  Future<void> _checkToken() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('token');
    if (token != null) {
      fetchCart();
    }
  }

  void clearCart() {
    if (mounted) {
      setState(() {
        _cart = {'items': [], 'totalAmount': 0.0};
      });
    }
  }

  Future<void> fetchCart() async {
    final prefs = await SharedPreferences.getInstance();
    if (prefs.getString('accountType') != 'User') {
      clearCart();
      return;
    }

    try {
      final data = await Api.getCart();
      if (mounted) setState(() => _cart = data);
    } catch (e) {
      debugPrint('Failed to fetch cart: $e');
    }
  }

  Future<void> _addToCart(int productId, {int quantity = 1}) async {
    final prefs = await SharedPreferences.getInstance();
    if (prefs.getBool('isGuest') == true) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Please sign in to add items to cart'.tr())),
        );
      }
      return;
    }

    try {
      final data = await Api.addToCart(productId, quantity);
      if (mounted) setState(() => _cart = data);
      if (mounted) {
        showAddToCartAnimation(context);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${tr('Failed to add to cart:')} $e')),
        );
      }
    }
  }

  Future<void> _updateCartQuantity(int productId, int quantity) async {
    try {
      final data = await Api.updateCartQuantity(productId, quantity);
      if (mounted) setState(() => _cart = data);
    } catch (e) {
      debugPrint('Failed to update quantity: $e');
    }
  }

  Future<void> _removeFromCart(int productId) async {
    try {
      final data = await Api.removeFromCart(productId);
      if (mounted) setState(() => _cart = data);
    } catch (e) {
      debugPrint('Failed to remove item: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'LAGBAR SHOP',
      localizationsDelegates: context.localizationDelegates,
      supportedLocales: context.supportedLocales,
      locale: context.locale,
      debugShowCheckedModeBanner: false,
      themeMode: _themeMode,
      theme: AppTheme.lightTheme,
      darkTheme: AppTheme.darkTheme,
      routes: {
        '/': (context) => const AuthScreen(),
        '/categories': (context) => CategoriesPage(
              cartCount: (_cart['items'] as List).length,
              onNavigateCart: () => Navigator.of(context).pushNamed('/cart'),
              onAddToCart: _addToCart,
              onLogout: () async {
                final prefs = await SharedPreferences.getInstance();
                final rt = prefs.getString('refreshToken');
                if (rt != null) {
                  try {
                    await Api.post('/api/auth/logout', {'refreshToken': rt});
                  } catch (_) {}
                }
                await prefs.remove('token');
                await prefs.remove('refreshToken');
                await prefs.remove('accountType');
                await prefs.remove('isGuest');
                if (context.mounted) {
                  OnlineStoreApp.of(context)?.clearCart();
                  Navigator.of(context).pushAndRemoveUntil(
                    MaterialPageRoute(builder: (_) => const AuthScreen()),
                    (route) => false,
                  );
                }
              },
            ),
        '/profile': (context) => const ProfilePage(),
        '/edit-profile': (context) => const EditProfilePage(),
        '/category-products': (context) {
          final args = (ModalRoute.of(context)!.settings.arguments as Map)
              .cast<String, dynamic>();
          return CategoryProductsPage(
            categoryId: args['id']!,
            categoryName: args['name']!,
            cartCount: (_cart['items'] as List).length,
            onNavigateCart: () => Navigator.of(context).pushNamed('/cart'),
            onAddToCart: _addToCart,
          );
        },
        '/store-products': (context) => const StoreProductsPage(),
        '/store-orders': (context) => const StoreOrdersPage(),
        '/store-analytics': (context) => const StoreAnalyticsPage(),
        '/delivery-jobs': (context) => const DeliveryJobsPage(),
        '/admin': (context) => const AdminPage(),
        '/notifications': (context) => const NotificationsPage(),
        '/settings': (context) => const SettingsPage(),
        '/cart': (context) => CartPage(
              cart: _cart,
              onUpdateQuantity: _updateCartQuantity,
              onRemoveItem: _removeFromCart,
              onCheckout: () {
                if ((_cart['items'] as List).isNotEmpty) {
                  Navigator.of(context)
                      .pushNamed('/checkout', arguments: _cart);
                }
              },
            ),
        '/checkout': (context) {
          final cartArgs = ModalRoute.of(context)!.settings.arguments
              as Map<String, dynamic>;
          return CheckoutPage(cart: cartArgs);
        },
        '/payment': (context) {
          final args = ModalRoute.of(context)!.settings.arguments
              as Map<String, dynamic>;
          return PaymentPage(
            cart: args['cart'],
            checkoutData: args['checkoutData'],
          );
        },
        '/add-card': (context) => const AddCardPage(),
        '/search': (context) => const SearchProductsPage(),
        '/favorites': (context) => const FavoritesPage(),
        '/active-orders': (context) => const ActiveOrdersPage(),
        '/delivered-orders': (context) => const DeliveredOrdersPage(),
      },
    );
  }
}

class AuthScreen extends StatefulWidget {
  const AuthScreen({super.key});

  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen> {
  AccountType accountType = AccountType.user;
  AuthMode mode = AuthMode.login;

  final emailCtrl = TextEditingController();
  final passCtrl = TextEditingController();
  final fullNameCtrl = TextEditingController();
  final shopNameCtrl = TextEditingController();
  final locationCtrl = TextEditingController();
  final countryCtrl = TextEditingController();
  final codeCtrl = TextEditingController();
  final phoneCtrl = TextEditingController();
  final vehicleCtrl = TextEditingController();

  String storeType = 'Restaurant';

  bool loading = false;
  bool codeSending = false;
  bool codeSent = false;
  String? message;
  bool isSuccess = false;

  // Forgot password state
  bool showForgot = false;
  final resetEmailCtrl = TextEditingController();
  final resetCodeCtrl = TextEditingController();
  final newPassCtrl = TextEditingController();
  bool resetCodeSent = false;
  bool resetCodeSending = false;

  bool get isShop => accountType == AccountType.shop;
  bool get isDelivery => accountType == AccountType.delivery;
  bool get isSignup => mode == AuthMode.signup;

  String? validate() {
    if (emailCtrl.text.trim().isEmpty) return 'Email is required'.tr();
    if (!RegExp(
      r'^[^\s@]+@[^\s@]+\.[^\s@]+$',
    ).hasMatch(emailCtrl.text.trim())) {
      return 'Please enter a valid email address'.tr();
    }
    if (passCtrl.text.trim().isEmpty) return 'Password is required'.tr();
    if (passCtrl.text.length < 8) {
      return 'Password must be at least 8 characters'.tr();
    }
    if (isSignup) {
      if (codeCtrl.text.trim().isEmpty) {
        return 'Verification code is required. Click "Get Code" first.'.tr();
      }
      if (countryCtrl.text.trim().isEmpty) return 'Country is required'.tr();
      if (isShop) {
        if (shopNameCtrl.text.trim().isEmpty) {
          return 'Shop name is required'.tr();
        }
      }
    }
    return null;
  }

  Future<void> sendCode() async {
    if (emailCtrl.text.trim().isEmpty) {
      _showMsg('Enter your email first'.tr(), false);
      return;
    }
    if (!RegExp(
      r'^[^\s@]+@[^\s@]+\.[^\s@]+$',
    ).hasMatch(emailCtrl.text.trim())) {
      _showMsg('Please enter a valid email address'.tr(), false);
      return;
    }

    setState(() {
      codeSending = true;
      message = null;
    });

    try {
      await Api.post('/api/auth/send-code', {'email': emailCtrl.text.trim()});
      setState(() => codeSent = true);
      _showMsg('✅ Code sent! Check your email.'.tr(), true);
    } catch (e) {
      _showMsg(e.toString().replaceFirst('Exception: ', ''), false);
    } finally {
      setState(() => codeSending = false);
    }
  }

  Future<void> submit() async {
    setState(() => message = null);

    final err = validate();
    if (err != null) {
      _showMsg(err, false);
      return;
    }

    setState(() => loading = true);

    try {
      Map<String, dynamic> res;

      if (mode == AuthMode.login) {
        res = await Api.post('/api/auth/login', {
          'email': emailCtrl.text.trim(),
          'password': passCtrl.text,
        });
      } else {
        if (isShop) {
          res = await Api.post('/api/auth/register-shop', {
            'email': emailCtrl.text.trim(),
            'password': passCtrl.text,
            'shopName': shopNameCtrl.text.trim(),
            'country': countryCtrl.text.trim(),
            'storeType': storeType,
            'code': codeCtrl.text.trim(),
          });
        } else if (isDelivery) {
          res = await Api.post('/api/auth/register-delivery', {
            'email': emailCtrl.text.trim(),
            'password': passCtrl.text,
            'fullName': fullNameCtrl.text.trim().isEmpty
                ? null
                : fullNameCtrl.text.trim(),
            'country': countryCtrl.text.trim(),
            'phone':
                phoneCtrl.text.trim().isEmpty ? null : phoneCtrl.text.trim(),
            'vehicleInfo': vehicleCtrl.text.trim().isEmpty
                ? null
                : vehicleCtrl.text.trim(),
            'code': codeCtrl.text.trim(),
          });
        } else {
          res = await Api.post('/api/auth/register-user', {
            'email': emailCtrl.text.trim(),
            'password': passCtrl.text,
            'fullName': fullNameCtrl.text.trim().isEmpty
                ? null
                : fullNameCtrl.text.trim(),
            'country': countryCtrl.text.trim(),
            'code': codeCtrl.text.trim(),
          });
        }
      }

      final token = res['token']?.toString() ?? '';
      final refreshToken = res['refreshToken']?.toString() ?? '';
      final account = res['accountType']?.toString() ?? '';

      final prefs = await SharedPreferences.getInstance();
      await prefs.setString('token', token);
      await prefs.setString('refreshToken', refreshToken);
      await prefs.setString('accountType', account);

      if (mounted) {
        OnlineStoreApp.of(context)?.fetchCart();
      }

      _showMsg(
        mode == AuthMode.login
            ? 'Login successful ✅'.tr()
            : 'Account created ✅ You are now logged in!'.tr(),
        true,
      );

      // Navigate to categories page after short delay
      await Future.delayed(const Duration(milliseconds: 600));
      if (mounted) {
        context.setLocale(context.deviceLocale);
        Navigator.of(context).pushReplacementNamed('/categories');
      }
    } catch (e) {
      _showMsg(e.toString().replaceFirst('Exception: ', ''), false);
    } finally {
      setState(() => loading = false);
    }
  }

  // ── Forgot Password ──

  Future<void> sendResetCode() async {
    if (resetEmailCtrl.text.trim().isEmpty) {
      _showMsg('Enter your email first'.tr(), false);
      return;
    }

    setState(() {
      resetCodeSending = true;
      message = null;
    });

    try {
      await Api.post('/api/auth/forgot-password', {
        'email': resetEmailCtrl.text.trim(),
      });
      setState(() => resetCodeSent = true);
      _showMsg('✅ If the account exists, a reset code was sent.'.tr(), true);
    } catch (e) {
      _showMsg(e.toString().replaceFirst('Exception: ', ''), false);
    } finally {
      setState(() => resetCodeSending = false);
    }
  }

  Future<void> resetPassword() async {
    if (resetCodeCtrl.text.trim().isEmpty) {
      _showMsg('Enter the reset code'.tr(), false);
      return;
    }
    if (newPassCtrl.text.length < 8) {
      _showMsg('Password must be at least 8 characters'.tr(), false);
      return;
    }

    setState(() {
      loading = true;
      message = null;
    });

    try {
      await Api.post('/api/auth/reset-password', {
        'email': resetEmailCtrl.text.trim(),
        'code': resetCodeCtrl.text.trim(),
        'newPassword': newPassCtrl.text,
      });
      _showMsg('✅ Password reset! You can now log in.'.tr(), true);
      await Future.delayed(const Duration(seconds: 2));
      setState(() {
        showForgot = false;
        resetCodeSent = false;
        message = null;
      });
    } catch (e) {
      _showMsg(e.toString().replaceFirst('Exception: ', ''), false);
    } finally {
      setState(() => loading = false);
    }
  }

  void _showMsg(String text, bool success) {
    setState(() {
      message = text;
      isSuccess = success;
    });
  }

  @override
  void dispose() {
    emailCtrl.dispose();
    passCtrl.dispose();
    fullNameCtrl.dispose();
    shopNameCtrl.dispose();
    locationCtrl.dispose();
    countryCtrl.dispose();
    codeCtrl.dispose();
    phoneCtrl.dispose();
    vehicleCtrl.dispose();
    resetEmailCtrl.dispose();
    resetCodeCtrl.dispose();
    newPassCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final appState = OnlineStoreApp.of(context);
    final isDark = appState?.isDark ?? false;
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      body: Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: isDark
                ? [
                    colorScheme.surface,
                    colorScheme.surfaceContainerLow,
                    colorScheme.surfaceContainerLowest,
                  ]
                : [
                    const Color(0xFFF9F9FF),
                    const Color(0xFFF0F3FF),
                    const Color(0xFFE7EEFE),
                  ],
          ),
        ),
        child: SafeArea(
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 480),
              child: SingleChildScrollView(
                padding:
                    const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
                child: Column(
                  children: [
                    const SizedBox(height: 16),
                    // Logo Container with web icon
                    Container(
                      width: 86,
                      height: 86,
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: isDark
                            ? colorScheme.surfaceContainerHigh
                            : Colors.white,
                        borderRadius: BorderRadius.circular(24),
                        border: Border.all(
                          color: isDark
                              ? colorScheme.outlineVariant.withValues(alpha: 0.4)
                              : colorScheme.primary.withValues(alpha: 0.15),
                          width: 1.5,
                        ),
                        boxShadow: [
                          BoxShadow(
                            color: isDark
                                ? Colors.black.withValues(alpha: 0.4)
                                : colorScheme.primary.withValues(alpha: 0.15),
                            blurRadius: 24,
                            offset: const Offset(0, 8),
                          ),
                        ],
                      ),
                      child: Image.asset(
                        'assets/images/logo.png',
                        width: 62,
                        height: 62,
                        fit: BoxFit.contain,
                        errorBuilder: (_, __, ___) => Icon(
                          Icons.store_rounded,
                          size: 40,
                          color: colorScheme.primary,
                        ),
                      ),
                    ),
                    const SizedBox(height: 16),
                    Text(
                      'LAGBAR SHOP',
                      style: GoogleFonts.playfairDisplay(
                        fontSize: 26,
                        fontWeight: FontWeight.w700,
                        color: colorScheme.onSurface,
                        letterSpacing: 1.5,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      'Your one-stop marketplace'.tr(),
                      style: GoogleFonts.dmSans(
                        fontSize: 13,
                        color: colorScheme.onSurface.withValues(alpha: 0.6),
                        letterSpacing: 0.5,
                      ),
                    ),
                    const SizedBox(height: 28),

                    // Main card
                    Container(
                      decoration: BoxDecoration(
                        color: isDark
                            ? colorScheme.surfaceContainer.withValues(alpha: 0.95)
                            : Colors.white.withValues(alpha: 0.95),
                        borderRadius: BorderRadius.circular(24),
                        border: Border.all(
                          color: isDark
                              ? colorScheme.outlineVariant.withValues(alpha: 0.35)
                              : colorScheme.onSurface.withValues(alpha: 0.06),
                        ),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black
                                .withValues(alpha: isDark ? 0.4 : 0.06),
                            blurRadius: 30,
                            offset: const Offset(0, 12),
                          ),
                        ],
                      ),
                      child: Padding(
                        padding: const EdgeInsets.all(24),
                        child:
                            showForgot ? _buildForgotForm() : _buildAuthForm(),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildAuthForm() {
    final title = mode == AuthMode.login
        ? "Login".tr()
        : '${accountType == AccountType.user ? "User".tr() : accountType == AccountType.shop ? "Shop".tr() : "Delivery".tr()} • ${"Sign Up".tr()}';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(title, style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 12),

        if (mode == AuthMode.signup) ...[
          _AccountTypeSelector(
            selected: accountType,
            onChanged: (at) => setState(() => accountType = at),
          ),
          const SizedBox(height: 10),
        ],
        _Segment(
          left: 'Login'.tr(),
          right: 'Sign Up'.tr(),
          isRightSelected: isSignup,
          onChanged: (right) => setState(() {
            mode = right ? AuthMode.signup : AuthMode.login;
            message = null;
          }),
        ),
        const SizedBox(height: 16),

        TypewriterText(
          text: mode == AuthMode.login
              ? "WELCOME IN LAGBAR SHOP".tr()
              : "WE WILL GET YOU EVERY THING YOU NEED".tr(),
        ),

        if (isSignup && !isShop && !isDelivery)
          TextField(
            controller: fullNameCtrl,
            decoration: InputDecoration(
              labelText: 'Full name (optional)'.tr(),
              border: const OutlineInputBorder(),
            ),
          ),
        if (isSignup && !isShop && !isDelivery) const SizedBox(height: 12),
        if (isSignup && !isShop && !isDelivery)
          TextField(
            controller: countryCtrl,
            decoration: InputDecoration(
              labelText: 'Country *'.tr(),
              hintText: 'e.g. Iraq, Turkey...'.tr(),
              border: const OutlineInputBorder(),
            ),
          ),
        if (isSignup && !isShop && !isDelivery) const SizedBox(height: 12),

        if (isSignup && isShop) ...[
          TextField(
            controller: shopNameCtrl,
            decoration: InputDecoration(
              labelText: 'Shop name'.tr(),
              border: const OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: countryCtrl,
            decoration: InputDecoration(
              labelText: 'Country *'.tr(),
              hintText: 'e.g. Iraq, Turkey...'.tr(),
              border: const OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          DropdownButtonFormField<String>(
            // ignore: deprecated_member_use
            value: storeType,
            decoration: InputDecoration(
              labelText: 'Store Type'.tr(),
              border: const OutlineInputBorder(),
            ),
            items: ['Restaurant', 'Market', 'Pharmacy']
                .map((type) => DropdownMenuItem(
                      value: type,
                      child: Text(type.tr()),
                    ))
                .toList(),
            onChanged: (val) {
              if (val != null) {
                setState(() => storeType = val);
              }
            },
          ),
          const SizedBox(height: 12),
        ],

        if (isSignup && isDelivery) ...[
          TextField(
            controller: fullNameCtrl,
            decoration: InputDecoration(
              labelText: 'Full name'.tr(),
              border: const OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: countryCtrl,
            decoration: InputDecoration(
              labelText: 'Country *'.tr(),
              hintText: 'e.g. Iraq, Turkey...'.tr(),
              border: const OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: phoneCtrl,
            keyboardType: TextInputType.phone,
            decoration: InputDecoration(
              labelText: 'Phone'.tr(),
              border: const OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: vehicleCtrl,
            decoration: InputDecoration(
              labelText: 'Vehicle info (e.g. Motorcycle, Car)'.tr(),
              border: const OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
        ],

        TextField(
          controller: emailCtrl,
          keyboardType: TextInputType.emailAddress,
          decoration: InputDecoration(
            labelText: 'Email'.tr(),
            border: const OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 12),

        TextField(
          controller: passCtrl,
          obscureText: true,
          decoration: InputDecoration(
            labelText: 'Password (min 8 chars)'.tr(),
            border: const OutlineInputBorder(),
          ),
        ),

        // Verification code field with Get Code button inside
        if (isSignup) ...[
          const SizedBox(height: 12),
          TextField(
            controller: codeCtrl,
            keyboardType: TextInputType.number,
            maxLength: 6,
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.bold,
              letterSpacing: 6,
            ),
            decoration: InputDecoration(
              labelText: 'Verification Code'.tr(),
              border: const OutlineInputBorder(),
              counterText: '',
              suffixIcon: Padding(
                padding: const EdgeInsets.only(right: 4),
                child: TextButton(
                  onPressed: codeSending ? null : sendCode,
                  style: TextButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 12),
                  ),
                  child: Text(
                    codeSending
                        ? '...'
                        : codeSent
                            ? 'Resend'.tr()
                            : 'Get Code'.tr(),
                    style: const TextStyle(fontSize: 13),
                  ),
                ),
              ),
            ),
          ),
        ],

        if (message != null) ...[
          const SizedBox(height: 12),
          Text(
            message!,
            style: TextStyle(color: isSuccess ? Colors.green : Colors.red),
          ),
        ],

        const SizedBox(height: 16),
        FilledButton(
          onPressed: loading ? null : submit,
          child: Text(
            loading
                ? 'Please wait...'.tr()
                : (mode == AuthMode.login ? 'Login'.tr() : 'Sign Up'.tr()),
          ),
        ),

        // Forgot Password — only on Login
        if (!isSignup) ...[
          const SizedBox(height: 12),
          Center(
            child: TextButton(
              onPressed: () => setState(() {
                showForgot = true;
                message = null;
              }),
              child: Text('Forgot Password?'.tr()),
            ),
          ),
        ],

        // ── Guest login (only on Login) ──
        if (!isSignup) ...[
          const SizedBox(height: 20),
          Row(
            children: [
              Expanded(child: Divider(color: Theme.of(context).dividerColor)),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                child: Text('or'.tr(),
                    style: TextStyle(
                        color: Theme.of(context)
                            .colorScheme
                            .onSurface
                            .withValues(alpha: 0.5),
                        fontSize: 12)),
              ),
              Expanded(child: Divider(color: Theme.of(context).dividerColor)),
            ],
          ),
          const SizedBox(height: 12),
          OutlinedButton.icon(
            onPressed: () async {
              final prefs = await SharedPreferences.getInstance();
              await prefs.setBool('isGuest', true);
              if (mounted) {
                Navigator.of(context).pushReplacementNamed('/categories');
              }
            },
            icon: const Icon(Icons.visibility_rounded),
            label: Text('Continue as Guest'.tr()),
            style: OutlinedButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 14),
            ),
          ),
        ],
      ],
    );
  }

  Widget _buildForgotForm() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text('Reset Password'.tr(),
            style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 16),
        TextField(
          controller: resetEmailCtrl,
          keyboardType: TextInputType.emailAddress,
          decoration: InputDecoration(
            labelText: 'Email'.tr(),
            border: const OutlineInputBorder(),
            suffixIcon: Padding(
              padding: const EdgeInsets.only(right: 4),
              child: TextButton(
                onPressed: resetCodeSending ? null : sendResetCode,
                style: TextButton.styleFrom(
                  padding: const EdgeInsets.symmetric(horizontal: 12),
                ),
                child: Text(
                  resetCodeSending ? '...' : 'Send Code'.tr(),
                  style: const TextStyle(fontSize: 13),
                ),
              ),
            ),
          ),
        ),
        if (resetCodeSent) ...[
          const SizedBox(height: 12),
          TextField(
            controller: resetCodeCtrl,
            keyboardType: TextInputType.number,
            maxLength: 6,
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.bold,
              letterSpacing: 6,
            ),
            decoration: InputDecoration(
              labelText: 'Reset Code'.tr(),
              border: const OutlineInputBorder(),
              counterText: '',
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: newPassCtrl,
            obscureText: true,
            decoration: InputDecoration(
              labelText: 'New Password (min 8 chars)'.tr(),
              border: const OutlineInputBorder(),
            ),
          ),
        ],
        if (message != null) ...[
          const SizedBox(height: 12),
          Text(
            message!,
            style: TextStyle(color: isSuccess ? Colors.green : Colors.red),
          ),
        ],
        if (resetCodeSent) ...[
          const SizedBox(height: 16),
          FilledButton(
            onPressed: loading ? null : resetPassword,
            child: Text(loading ? 'Resetting...'.tr() : 'Reset Password'.tr()),
          ),
        ],
        const SizedBox(height: 12),
        Center(
          child: TextButton(
            onPressed: () => setState(() {
              showForgot = false;
              resetCodeSent = false;
              message = null;
            }),
            child: Text('← Back to Login'.tr()),
          ),
        ),
      ],
    );
  }
}

class _Segment extends StatelessWidget {
  final String left;
  final String right;
  final bool isRightSelected;
  final void Function(bool rightSelected) onChanged;

  const _Segment({
    required this.left,
    required this.right,
    required this.isRightSelected,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return SegmentedButton<bool>(
      segments: [
        ButtonSegment(value: false, label: Text(left)),
        ButtonSegment(value: true, label: Text(right)),
      ],
      selected: {isRightSelected},
      onSelectionChanged: (s) => onChanged(s.first),
    );
  }
}

class _AccountTypeSelector extends StatelessWidget {
  final AccountType selected;
  final void Function(AccountType) onChanged;

  const _AccountTypeSelector({
    required this.selected,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return SegmentedButton<AccountType>(
      segments: [
        ButtonSegment(value: AccountType.user, label: Text('User'.tr())),
        ButtonSegment(value: AccountType.shop, label: Text('Shop'.tr())),
        ButtonSegment(
            value: AccountType.delivery, label: Text('Delivery'.tr())),
      ],
      selected: {selected},
      onSelectionChanged: (s) => onChanged(s.first),
    );
  }
}
