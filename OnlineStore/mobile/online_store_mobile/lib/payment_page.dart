import 'package:flutter/material.dart';
import 'package:flutter_stripe/flutter_stripe.dart';
import 'package:lottie/lottie.dart' as lottie;
import 'package:easy_localization/easy_localization.dart';
import 'package:google_fonts/google_fonts.dart';

import 'api.dart';
import 'main.dart';

class PaymentPage extends StatefulWidget {
  final Map<String, dynamic> cart;
  final Map<String, dynamic> checkoutData;

  const PaymentPage({super.key, required this.cart, required this.checkoutData});

  @override
  State<PaymentPage> createState() => _PaymentPageState();
}

class _PaymentPageState extends State<PaymentPage> {
  bool _isProcessing = false;
  bool _isLoadingCards = false;
  List<dynamic> _savedCards = [];
  String? _selectedCardId;

  bool _isAddingCard = false;
  CardFieldInputDetails? _cardDetails;
  final TextEditingController _nameController = TextEditingController();

  @override
  void initState() {
    super.initState();
    if (widget.checkoutData['paymentMethod'] == 'Credit Card') {
      _loadCards();
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    super.dispose();
  }

  Future<void> _loadCards() async {
    setState(() => _isLoadingCards = true);
    try {
      final Map<String, dynamic> response = await Api.get('/api/payment/cards');
      final List<dynamic> data = response['data'] ?? [];
      if (mounted) {
        setState(() {
          _savedCards = data;
          if (_savedCards.isNotEmpty) {
            _selectedCardId = _savedCards[0]['id'];
            _isAddingCard = false;
          } else {
            _isAddingCard = true;
          }
        });
      }
    } catch (e) {
      debugPrint('Failed to load cards: $e');
    } finally {
      if (mounted) setState(() => _isLoadingCards = false);
    }
  }

  Future<void> _saveInlineCard() async {
    if (_cardDetails == null || !_cardDetails!.complete) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Please complete card details'.tr())),
      );
      return;
    }
    if (_nameController.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Please enter cardholder name'.tr())),
      );
      return;
    }

    setState(() => _isProcessing = true);

    try {
      final paymentMethod = await Stripe.instance.createPaymentMethod(
        params: PaymentMethodParams.card(
          paymentMethodData: PaymentMethodData(
            billingDetails: BillingDetails(name: _nameController.text.trim()),
          ),
        ),
      );

      final response = await Api.post('/api/payment/cards', {
        'paymentMethodId': paymentMethod.id,
      });

      if (!mounted) return;
      setState(() {
        _savedCards.add(response);
        _selectedCardId = response['id'];
        _isAddingCard = false;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Card saved successfully!'.tr())),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to save card: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isProcessing = false);
    }
  }

  Future<void> _deleteCard(String id) async {
    try {
      await Api.delete('/api/payment/cards/$id');
      if (mounted) {
        setState(() {
          _savedCards.removeWhere((c) => c['id'] == id);
          if (_selectedCardId == id) {
            _selectedCardId = _savedCards.isNotEmpty ? _savedCards[0]['id'] : null;
            if (_savedCards.isEmpty) _isAddingCard = true;
          }
        });
      }
    } catch (e) {
      debugPrint('Failed to delete card: $e');
    }
  }

  Future<void> _processPayment() async {
    setState(() => _isProcessing = true);

    try {
      final pMethod = widget.checkoutData['paymentMethod'];

      if (pMethod == 'Credit Card') {
        if (_selectedCardId == null) {
          throw Exception('Please select a payment card'.tr());
        }

        final double amount = (widget.cart['totalAmount'] is num)
            ? (widget.cart['totalAmount'] as num).toDouble()
            : double.tryParse(widget.cart['totalAmount'].toString()) ?? 0.0;

        final intentRes = await Api.post('/api/payment/create-intent', {
          'amount': (amount * 100).toInt(),
          'currency': 'usd',
          'paymentMethodId': _selectedCardId,
        });

        final clientSecret = intentRes['clientSecret'];
        if (clientSecret != null && clientSecret.toString().isNotEmpty) {
          await Stripe.instance.confirmPayment(
            paymentIntentClientSecret: clientSecret,
          );
        }
      }

      final payload = {
        'deliveryAddress': widget.checkoutData['deliveryAddress'] ?? widget.checkoutData['addressString'],
        'deliveryCity': widget.checkoutData['deliveryCity'],
        'paymentMethod': pMethod,
        'offerCode': widget.checkoutData['offerCode'],
        if (pMethod == 'Credit Card' && _selectedCardId != null)
          'paymentToken': _selectedCardId,
      };

      await Api.post('/api/orders/checkout-cart', payload);

      if (!mounted) return;

      OnlineStoreApp.of(context)?.fetchCart();

      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (context) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              SizedBox(
                height: 120,
                child: lottie.Lottie.asset(
                  'assets/lottie/successful.json',
                  repeat: false,
                  errorBuilder: (_, __, ___) => Icon(
                    Icons.check_circle_rounded,
                    size: 80,
                    color: Theme.of(context).colorScheme.primary,
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Text(
                'Order Placed!'.tr(),
                style: GoogleFonts.inter(
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Your order has been submitted successfully.'.tr(),
                textAlign: TextAlign.center,
                style: GoogleFonts.inter(
                  fontSize: 14,
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: 24),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 14),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  onPressed: () {
                    Navigator.of(context).pushNamedAndRemoveUntil(
                      '/categories',
                      (route) => false,
                    );
                  },
                  child: Text(
                    'Back to Store'.tr(),
                    style: GoogleFonts.inter(fontWeight: FontWeight.bold),
                  ),
                ),
              ),
            ],
          ),
        ),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('${'Payment failed:'.tr()} $e'),
            backgroundColor: Colors.redAccent,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isProcessing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;

    final double totalAmount = (widget.cart['totalAmount'] is num)
        ? (widget.cart['totalAmount'] as num).toDouble()
        : double.tryParse(widget.cart['totalAmount'].toString()) ?? 0.0;

    final String tAmount = totalAmount.toStringAsFixed(2);
    final String pMethod = widget.checkoutData['paymentMethod'] ?? 'Cash on Delivery';

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      appBar: AppBar(
        backgroundColor: colorScheme.surface,
        elevation: 0,
        scrolledUnderElevation: 1,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_rounded, color: colorScheme.onSurface),
          onPressed: () => Navigator.pop(context),
        ),
        title: Text(
          'Payment Details'.tr(),
          style: GoogleFonts.inter(
              fontWeight: FontWeight.bold, color: colorScheme.onSurface),
        ),
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 480),
          child: SingleChildScrollView(
            physics: const BouncingScrollPhysics(),
            padding: const EdgeInsets.all(20),
            child: Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: colorScheme.surface,
                borderRadius: BorderRadius.circular(20),
                border: Border.all(
                  color: isDark
                      ? Colors.white.withValues(alpha: 0.1)
                      : colorScheme.outline.withValues(alpha: 0.5),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'ORDER TOTAL'.tr(),
                    textAlign: TextAlign.center,
                    style: GoogleFonts.inter(
                        fontSize: 11,
                        letterSpacing: 1.5,
                        color: colorScheme.primary,
                        fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '\$$tAmount',
                    textAlign: TextAlign.center,
                    style: GoogleFonts.inter(
                        fontSize: 34,
                        fontWeight: FontWeight.bold,
                        color: colorScheme.onSurface),
                  ),
                  const SizedBox(height: 24),

                  // Method Row
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('Method'.tr(),
                          style: GoogleFonts.inter(
                              color: colorScheme.onSurfaceVariant)),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 12, vertical: 6),
                        decoration: BoxDecoration(
                          color: colorScheme.primary.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(
                              color: colorScheme.primary.withValues(alpha: 0.2)),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(
                              pMethod == 'Credit Card'
                                  ? Icons.credit_card_rounded
                                  : Icons.local_shipping_outlined,
                              size: 16,
                              color: colorScheme.primary,
                            ),
                            const SizedBox(width: 8),
                            Text(pMethod.tr(),
                                style: GoogleFonts.inter(
                                    fontSize: 12,
                                    fontWeight: FontWeight.bold,
                                    color: colorScheme.primary)),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),

                  // Deliver to row
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('Deliver to'.tr(),
                          style: GoogleFonts.inter(
                              color: colorScheme.onSurfaceVariant)),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.end,
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Icon(Icons.location_on_rounded,
                                size: 16, color: colorScheme.primary),
                            const SizedBox(width: 4),
                            Expanded(
                              child: Text(
                                widget.checkoutData['addressString'] ?? '',
                                textAlign: TextAlign.right,
                                style: GoogleFonts.inter(
                                    color: colorScheme.onSurface,
                                    fontSize: 13),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 24),
                  Divider(height: 1, color: colorScheme.outline.withValues(alpha: 0.5)),
                  const SizedBox(height: 24),

                  if (pMethod == 'Credit Card') ...[
                    if (_isLoadingCards)
                      Center(
                        child: Padding(
                          padding: const EdgeInsets.all(24.0),
                          child: CircularProgressIndicator(
                              color: colorScheme.primary),
                        ),
                      )
                    else if (_isAddingCard) ...[
                      // Add new card section
                      Text(
                        'Add a new card'.tr(),
                        style: GoogleFonts.inter(
                            fontWeight: FontWeight.bold,
                            fontSize: 16,
                            color: colorScheme.onSurface),
                      ),
                      const SizedBox(height: 16),

                      // Card Mockup
                      Container(
                        padding: const EdgeInsets.all(20),
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            colors: [
                              colorScheme.primary,
                              colorScheme.primary.withValues(alpha: 0.8)
                            ],
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                          ),
                          borderRadius: BorderRadius.circular(16),
                          boxShadow: [
                            BoxShadow(
                                color: Colors.black.withValues(alpha: 0.15),
                                offset: const Offset(0, 4),
                                blurRadius: 10)
                          ],
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Container(
                                  width: 40,
                                  height: 28,
                                  decoration: BoxDecoration(
                                      color: Colors.amber.shade300,
                                      borderRadius: BorderRadius.circular(6)),
                                ),
                                SizedBox(
                                  width: 40,
                                  child: Stack(
                                    children: [
                                      Align(
                                        alignment: Alignment.centerLeft,
                                        child: Container(
                                            width: 24,
                                            height: 24,
                                            decoration: BoxDecoration(
                                                shape: BoxShape.circle,
                                                color: Colors.red
                                                    .withValues(alpha: 0.8))),
                                      ),
                                      Align(
                                        alignment: Alignment.centerRight,
                                        child: Container(
                                            width: 24,
                                            height: 24,
                                            decoration: BoxDecoration(
                                                shape: BoxShape.circle,
                                                color: Colors.amber
                                                    .withValues(alpha: 0.8))),
                                      ),
                                    ],
                                  ),
                                )
                              ],
                            ),
                            const SizedBox(height: 24),
                            const Text('•••• •••• •••• ••••',
                                style: TextStyle(
                                    fontSize: 22,
                                    letterSpacing: 3,
                                    color: Colors.white,
                                    fontFamily: 'Courier')),
                            const SizedBox(height: 16),
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text('CARDHOLDER'.tr(),
                                        style: GoogleFonts.inter(
                                            fontSize: 10,
                                            letterSpacing: 1,
                                            color: Colors.white70)),
                                    Text(
                                        _nameController.text.isEmpty
                                            ? 'Your name'.tr()
                                            : _nameController.text,
                                        style: GoogleFonts.inter(
                                            color: Colors.white,
                                            fontSize: 13,
                                            fontWeight: FontWeight.w600)),
                                  ],
                                ),
                                Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text('EXPIRES'.tr(),
                                        style: GoogleFonts.inter(
                                            fontSize: 10,
                                            letterSpacing: 1,
                                            color: Colors.white70)),
                                    Text('MM / YY'.tr(),
                                        style: GoogleFonts.inter(
                                            color: Colors.white,
                                            fontSize: 13,
                                            fontWeight: FontWeight.w600)),
                                  ],
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 20),

                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 12, vertical: 8),
                        decoration: BoxDecoration(
                          color: colorScheme.surface,
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(
                              color: colorScheme.outline.withValues(alpha: 0.5)),
                        ),
                        child: CardFormField(
                          enablePostalCode: false,
                          onCardChanged: (details) {
                            setState(() => _cardDetails = details);
                          },
                        ),
                      ),
                      const SizedBox(height: 16),
                      Text('CARDHOLDER NAME'.tr(),
                          style: GoogleFonts.inter(
                              fontSize: 11,
                              fontWeight: FontWeight.bold,
                              letterSpacing: 1,
                              color: colorScheme.primary)),
                      const SizedBox(height: 8),
                      TextField(
                        controller: _nameController,
                        onChanged: (v) => setState(() {}),
                        style: GoogleFonts.inter(color: colorScheme.onSurface),
                        decoration: InputDecoration(
                          hintText: 'Name on card'.tr(),
                          border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12)),
                        ),
                      ),
                      const SizedBox(height: 20),

                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton(
                              onPressed: () {
                                if (_savedCards.isNotEmpty) {
                                  setState(() => _isAddingCard = false);
                                } else {
                                  Navigator.pop(context);
                                }
                              },
                              child: Text('Cancel'.tr()),
                            ),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: ElevatedButton(
                              onPressed:
                                  _isProcessing ? null : _saveInlineCard,
                              child: _isProcessing
                                  ? const SizedBox(
                                      width: 16,
                                      height: 16,
                                      child: CircularProgressIndicator(
                                          strokeWidth: 2, color: Colors.white))
                                  : Text('Save card'.tr()),
                            ),
                          ),
                        ],
                      ),
                    ] else ...[
                      // Show Saved Cards
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text('Saved Cards'.tr(),
                              style: GoogleFonts.inter(
                                  fontWeight: FontWeight.bold,
                                  fontSize: 16,
                                  color: colorScheme.onSurface)),
                          TextButton.icon(
                            onPressed: () => setState(() {
                              _nameController.clear();
                              _cardDetails = null;
                              _isAddingCard = true;
                            }),
                            icon: const Icon(Icons.add_rounded, size: 18),
                            label: Text('Add new'.tr()),
                          )
                        ],
                      ),
                      const SizedBox(height: 12),
                      ..._savedCards.map((c) => Padding(
                            padding: const EdgeInsets.only(bottom: 12.0),
                            child: InkWell(
                              onTap: () =>
                                  setState(() => _selectedCardId = c['id']),
                              borderRadius: BorderRadius.circular(12),
                              child: Container(
                                decoration: BoxDecoration(
                                  color: _selectedCardId == c['id']
                                      ? colorScheme.primary
                                          .withValues(alpha: 0.08)
                                      : colorScheme.surface,
                                  borderRadius: BorderRadius.circular(12),
                                  border: Border.all(
                                    color: _selectedCardId == c['id']
                                        ? colorScheme.primary
                                        : colorScheme.outline
                                            .withValues(alpha: 0.5),
                                    width: _selectedCardId == c['id'] ? 2 : 1,
                                  ),
                                ),
                                child: ListTile(
                                  leading: Icon(Icons.credit_card_rounded,
                                      color: colorScheme.primary),
                                  title: Text(
                                    '{{brand}} ending in {{last4}}'.tr(
                                        namedArgs: {
                                          'brand': (c['brand'] as String)
                                              .toUpperCase(),
                                          'last4': c['last4']
                                        }),
                                    style: GoogleFonts.inter(
                                        fontWeight: FontWeight.bold,
                                        color: colorScheme.onSurface),
                                  ),
                                  subtitle: Text(
                                    'Expires {{month}}/{{year}}'.tr(namedArgs: {
                                      'month': c['expMonth'].toString(),
                                      'year': c['expYear'].toString()
                                    }),
                                    style: GoogleFonts.inter(
                                        color: colorScheme.onSurfaceVariant),
                                  ),
                                  trailing: IconButton(
                                    icon: Icon(Icons.delete_outline_rounded,
                                        color: colorScheme.error, size: 20),
                                    onPressed: () => _deleteCard(c['id']),
                                  ),
                                ),
                              ),
                            ),
                          )),
                    ],
                  ] else ...[
                    // Cash on Delivery
                    Container(
                      padding: const EdgeInsets.all(20),
                      decoration: BoxDecoration(
                        color: colorScheme.primary.withValues(alpha: 0.08),
                        borderRadius: BorderRadius.circular(14),
                        border: Border.all(
                            color: colorScheme.primary.withValues(alpha: 0.2)),
                      ),
                      child: Column(
                        children: [
                          Icon(Icons.local_shipping_outlined,
                              size: 48, color: colorScheme.primary),
                          const SizedBox(height: 12),
                          Text(
                            'You have selected Cash on Delivery. You will pay when the order arrives.'
                                .tr(),
                            textAlign: TextAlign.center,
                            style: GoogleFonts.inter(
                                color: colorScheme.onSurface, height: 1.4),
                          ),
                        ],
                      ),
                    )
                  ],

                  if (!_isAddingCard) ...[
                    const SizedBox(height: 28),
                    Divider(
                        height: 1,
                        color: colorScheme.outline.withValues(alpha: 0.5)),
                    const SizedBox(height: 20),

                    ElevatedButton(
                      onPressed: _isProcessing ||
                              (pMethod == 'Credit Card' &&
                                  _selectedCardId == null)
                          ? null
                          : _processPayment,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: colorScheme.primary,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(14)),
                        elevation: 2,
                      ),
                      child: _isProcessing
                          ? const SizedBox(
                              width: 20,
                              height: 20,
                              child: CircularProgressIndicator(
                                  strokeWidth: 2, color: Colors.white))
                          : Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                const Icon(Icons.check_circle_rounded, size: 20),
                                const SizedBox(width: 8),
                                Text(
                                    pMethod == 'Credit Card'
                                        ? 'Pay now - \${{amount}}'
                                            .tr(namedArgs: {'amount': tAmount})
                                        : 'Confirm Order'.tr(),
                                    style: GoogleFonts.inter(
                                        fontSize: 16,
                                        fontWeight: FontWeight.bold)),
                              ],
                            ),
                    ),
                    const SizedBox(height: 16),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.shield_outlined,
                            size: 14, color: colorScheme.onSurfaceVariant),
                        const SizedBox(width: 6),
                        Text(
                          'SSL encrypted - Secured by Lagbar'.tr(),
                          style: GoogleFonts.inter(
                              fontSize: 11,
                              color: colorScheme.onSurfaceVariant),
                        ),
                      ],
                    )
                  ]
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
