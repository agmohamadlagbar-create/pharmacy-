import 'package:flutter/material.dart';
import 'package:flutter_stripe/flutter_stripe.dart';
import 'package:lottie/lottie.dart' as lottie;
import 'package:easy_localization/easy_localization.dart';

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
      
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('${'Failed:'.tr()} $e')),
      );
    } finally {
      if (mounted) setState(() => _isProcessing = false);
    }
  }

  Future<void> _deleteCard(String id) async {
    try {
      await Api.delete('/api/payment/cards/$id');
      setState(() {
        _savedCards.removeWhere((c) => c['id'] == id);
        if (_selectedCardId == id) {
          _selectedCardId = _savedCards.isNotEmpty ? _savedCards[0]['id'] : null;
        }
        if (_savedCards.isEmpty) {
          _isAddingCard = true;
        }
      });
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('${'Failed to delete card:'.tr()} $e')));
    }
  }

  Future<void> _processPayment() async {
    if (widget.checkoutData['paymentMethod'] == 'Credit Card' && _selectedCardId == null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Please select a payment card.'.tr())));
      return;
    }

    setState(() => _isProcessing = true);

    try {
      final itemsList = (widget.cart['items'] as List<dynamic>? ?? []);
      if (itemsList.isEmpty) throw Exception('Cart is empty');

      await Api.post('/api/orders/checkout-cart', {
        'deliveryAddress': widget.checkoutData['addressString'],
        'deliveryCity': widget.checkoutData['deliveryCity'],
        'paymentMethod': widget.checkoutData['paymentMethod'],
        'offerCode': widget.checkoutData['offerCode'],
        'paymentToken': _selectedCardId,
      });

      await Api.clearCart();
      if (!mounted) return;
      OnlineStoreApp.of(context)?.fetchCart();

      if (mounted) {
        showDialog(
          context: context,
          barrierColor: Colors.black45,
          barrierDismissible: false,
          builder: (ctx) {
            return Center(
              child: Container(
                width: 200,
                height: 200,
                decoration: BoxDecoration(
                  color: Theme.of(context).cardTheme.color,
                  borderRadius: BorderRadius.circular(20),
                ),
                child: lottie.Lottie.asset(
                  'assets/lottie/payment-success.json',
                  repeat: false,
                ),
              ),
            );
          },
        );
      }

      await Future.delayed(const Duration(milliseconds: 2300));
      
      if (!mounted) return;
      if (context.mounted) {
        Navigator.of(context).pop(); // pop dialog
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Order placed successfully!'.tr())));
        Navigator.of(context).pushNamedAndRemoveUntil('/categories', (route) => false);
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('${'Failed:'.tr()} $e')));
      setState(() => _isProcessing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final tAmount = (widget.cart['totalAmount'] as num).toStringAsFixed(2);
    final pMethod = widget.checkoutData['paymentMethod'];
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        leading: Padding(
          padding: const EdgeInsets.all(8.0),
          child: Container(
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              border: Border.all(color: colorScheme.onSurface.withValues(alpha: 0.1)),
              color: Colors.transparent,
            ),
            child: IconButton(
              icon: const Icon(Icons.chevron_left, size: 20),
              onPressed: () => Navigator.pop(context),
              padding: EdgeInsets.zero,
            ),
          ),
        ),
        title: Text(
          'Payment details'.tr(),
          style: const TextStyle(fontWeight: FontWeight.w600, fontFamily: 'Playfair Display'),
        ),
        centerTitle: false,
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 450),
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                color: isDark ? const Color(0xFF261D1A) : colorScheme.surface,
                borderRadius: BorderRadius.circular(24),
                border: Border.all(
                  color: isDark ? Colors.white.withValues(alpha: 0.05) : colorScheme.onSurface.withValues(alpha: 0.1)
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'ORDER TOTAL'.tr(),
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 12, letterSpacing: 1.5, color: colorScheme.onSurface.withValues(alpha: 0.6), fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '\$$tAmount',
                    textAlign: TextAlign.center,
                    style: TextStyle(fontFamily: 'Playfair Display', fontSize: 44, fontWeight: FontWeight.bold, color: colorScheme.onSurface),
                  ),
                  const SizedBox(height: 32),
                  
                  // Method Row
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('Method'.tr(), style: TextStyle(color: colorScheme.onSurface.withValues(alpha: 0.6))),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                        decoration: BoxDecoration(
                           border: Border.all(color: colorScheme.onSurface.withValues(alpha: 0.1)),
                           borderRadius: BorderRadius.circular(16),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(pMethod == 'Credit Card' ? Icons.credit_card : Icons.local_shipping_outlined, size: 16, color: colorScheme.primary),
                            const SizedBox(width: 8),
                            Text(pMethod, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600)),
                          ]
                        )
                      )
                    ]
                  ),
                  const SizedBox(height: 16),
                  
                  // Deliver to row
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('Deliver to'.tr(), style: TextStyle(color: colorScheme.onSurface.withValues(alpha: 0.6))),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.end,
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Icon(Icons.location_on, size: 16, color: colorScheme.onSurface.withValues(alpha: 0.6)),
                            const SizedBox(width: 4),
                            Expanded(child: Text(widget.checkoutData['addressString'], textAlign: TextAlign.right, style: TextStyle(color: colorScheme.onSurface.withValues(alpha: 0.8)))),
                          ]
                        )
                      )
                    ]
                  ),
                  
                  const SizedBox(height: 24),
                  Divider(height: 1, color: colorScheme.onSurface.withValues(alpha: 0.1)),
                  const SizedBox(height: 24),

                  if (pMethod == 'Credit Card') ...[
                    if (_isLoadingCards)
                      const Center(child: Padding(
                        padding: EdgeInsets.all(24.0),
                        child: CircularProgressIndicator(),
                      ))
                    else if (_isAddingCard) ...[
                      // Add new card section
                      Text(
                        'Add a new card'.tr(),
                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16, fontFamily: 'Playfair Display'),
                      ),
                      const SizedBox(height: 16),
                      
                      // Card Mockup
                      Container(
                        padding: const EdgeInsets.all(20),
                        decoration: BoxDecoration(
                          gradient: const LinearGradient(
                            colors: [Color(0xFF6A3B24), Color(0xFF4A2511)],
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                          ),
                          borderRadius: BorderRadius.circular(16),
                          boxShadow: const [
                            BoxShadow(color: Colors.black26, offset: Offset(0, 4), blurRadius: 10)
                          ]
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Container(
                                  width: 40, height: 28,
                                  decoration: BoxDecoration(
                                    color: Colors.amber.shade300,
                                    borderRadius: BorderRadius.circular(6)
                                  ),
                                ),
                                SizedBox(
                                  width: 40,
                                  child: Stack(
                                    children: [
                                      Align(
                                        alignment: Alignment.centerLeft,
                                        child: Container(width: 24, height: 24, decoration: BoxDecoration(shape: BoxShape.circle, color: Colors.red.withValues(alpha: 0.8))),
                                      ),
                                      Align(
                                        alignment: Alignment.centerRight,
                                        child: Container(width: 24, height: 24, decoration: BoxDecoration(shape: BoxShape.circle, color: Colors.amber.withValues(alpha: 0.8))),
                                      ),
                                    ],
                                  ),
                                )
                              ]
                            ),
                            const SizedBox(height: 24),
                            const Text('•••• •••• •••• ••••', style: TextStyle(fontSize: 24, letterSpacing: 3, color: Colors.white, fontFamily: 'Courier')),
                            const SizedBox(height: 16),
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text('CARDHOLDER'.tr(), style: const TextStyle(fontSize: 10, letterSpacing: 1, color: Colors.white54)),
                                    Text(_nameController.text.isEmpty ? 'Your name'.tr() : _nameController.text, style: const TextStyle(color: Colors.white, fontSize: 14)),
                                  ]
                                ),
                                Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text('EXPIRES'.tr(), style: const TextStyle(fontSize: 10, letterSpacing: 1, color: Colors.white54)),
                                    Text('MM / YY'.tr(), style: const TextStyle(color: Colors.white, fontSize: 14)),
                                  ]
                                ),
                              ]
                            )
                          ]
                        )
                      ),
                      const SizedBox(height: 24),

                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                        decoration: BoxDecoration(
                          color: isDark ? const Color(0xFF332B28) : colorScheme.surfaceContainerHighest,
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: colorScheme.onSurface.withValues(alpha: 0.1)),
                        ),
                        child: CardFormField(
                          enablePostalCode: false,
                          onCardChanged: (details) {
                            setState(() => _cardDetails = details);
                          },
                        ),
                      ),
                      const SizedBox(height: 16),
                      Text('CARDHOLDER NAME'.tr(), style: TextStyle(fontSize: 12, letterSpacing: 1, color: colorScheme.onSurface.withValues(alpha: 0.5))),
                      const SizedBox(height: 8),
                      TextField(
                        controller: _nameController,
                        onChanged: (v) => setState((){}),
                        style: TextStyle(color: colorScheme.onSurface),
                        decoration: InputDecoration(
                          hintText: 'Name on card'.tr(),
                          hintStyle: TextStyle(color: colorScheme.onSurface.withValues(alpha: 0.4)),
                          filled: true,
                          fillColor: isDark ? const Color(0xFF332B28) : colorScheme.surfaceContainerHighest,
                          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(8), borderSide: BorderSide.none),
                          focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(8), borderSide: BorderSide(color: colorScheme.primary.withValues(alpha: 0.5))),
                          enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(8), borderSide: BorderSide(color: colorScheme.onSurface.withValues(alpha: 0.1))),
                        ),
                      ),
                      const SizedBox(height: 24),

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
                              style: OutlinedButton.styleFrom(
                                side: BorderSide(color: colorScheme.onSurface.withValues(alpha: 0.1)),
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                padding: const EdgeInsets.symmetric(vertical: 16),
                              ),
                              child: Text('Cancel'.tr(), style: TextStyle(color: colorScheme.onSurface)),
                            )
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: OutlinedButton(
                              onPressed: _isProcessing ? null : _saveInlineCard,
                              style: OutlinedButton.styleFrom(
                                side: BorderSide(color: colorScheme.onSurface.withValues(alpha: 0.1)),
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                padding: const EdgeInsets.symmetric(vertical: 16),
                              ),
                              child: _isProcessing 
                                ? SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: colorScheme.onSurface)) 
                                : Text('Save card'.tr(), style: TextStyle(color: colorScheme.onSurface)),
                            )
                          ),
                        ]
                      ),
                    ] else ...[
                      // Show Saved Cards
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text('Saved Cards'.tr(), style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16, fontFamily: 'Playfair Display')),
                          TextButton(
                            onPressed: () => setState(() {
                              _nameController.clear();
                              _cardDetails = null;
                              _isAddingCard = true;
                            }),
                            child: Text('+ Add new'.tr()),
                          )
                        ],
                      ),
                      const SizedBox(height: 12),
                      ..._savedCards.map((c) => Padding(
                        padding: const EdgeInsets.only(bottom: 12.0),
                        child: InkWell(
                          onTap: () => setState(() => _selectedCardId = c['id']),
                          borderRadius: BorderRadius.circular(12),
                          child: Container(
                            decoration: BoxDecoration(
                              color: _selectedCardId == c['id'] ? colorScheme.primary.withValues(alpha: 0.1) : Colors.transparent,
                              borderRadius: BorderRadius.circular(12),
                              border: Border.all(
                                color: _selectedCardId == c['id'] ? colorScheme.primary : colorScheme.onSurface.withValues(alpha: 0.1),
                                width: _selectedCardId == c['id'] ? 2 : 1,
                              ),
                            ),
                            child: ListTile(
                              leading: Icon(Icons.credit_card, color: colorScheme.primary),
                              title: Text('{{brand}} ending in {{last4}}'.tr(namedArgs: {'brand': (c['brand'] as String).toUpperCase(), 'last4': c['last4']}), style: const TextStyle(fontWeight: FontWeight.bold)),
                              subtitle: Text('Expires {{month}}/{{year}}'.tr(namedArgs: {'month': c['expMonth'].toString(), 'year': c['expYear'].toString()})),
                              trailing: IconButton(
                                icon: Icon(Icons.delete_outline, color: colorScheme.error.withValues(alpha: 0.8), size: 20),
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
                      padding: const EdgeInsets.all(24),
                      decoration: BoxDecoration(
                        color: colorScheme.primary.withValues(alpha: 0.05),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Column(
                        children: [
                          const Icon(Icons.local_shipping_outlined, size: 48),
                          const SizedBox(height: 12),
                          Text('You have selected Cash on Delivery. You will pay when the order arrives.'.tr(), textAlign: TextAlign.center),
                        ],
                      ),
                    )
                  ],

                  if (!_isAddingCard) ...[
                    const SizedBox(height: 32),
                    Divider(height: 1, color: colorScheme.onSurface.withValues(alpha: 0.1)),
                    const SizedBox(height: 24),
                    
                    OutlinedButton(
                      onPressed: _isProcessing || (pMethod == 'Credit Card' && _selectedCardId == null) ? null : _processPayment,
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        side: BorderSide(color: colorScheme.onSurface.withValues(alpha: 0.1)),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                      ),
                      child: _isProcessing 
                          ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2)) 
                          : Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                const Icon(Icons.arrow_forward, size: 18),
                                const SizedBox(width: 8),
                                Text(pMethod == 'Credit Card' ? 'Pay now - \${{amount}}'.tr(namedArgs: {'amount': tAmount}) : 'Confirm Order'.tr(), style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                              ]
                            ),
                    ),
                    const SizedBox(height: 16),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.shield_outlined, size: 14, color: colorScheme.onSurface.withValues(alpha: 0.5)),
                        const SizedBox(width: 6),
                        Text('SSL encrypted - Secured by Lagbar'.tr(), style: TextStyle(fontSize: 11, color: colorScheme.onSurface.withValues(alpha: 0.5))),
                      ],
                    )
                  ] else ...[
                    // Show disabled pay now when adding card to match visual weighting if they expect it?
                    // Actually the design only shows Cancel and Save Card. "Pay now" is probably at the bottom or hidden until saved.
                    // The design has "Pay now" at the bottom! So it's ALWAYS visible in the design.
                    const SizedBox(height: 32),
                    Divider(height: 1, color: colorScheme.onSurface.withValues(alpha: 0.1)),
                    const SizedBox(height: 24),
                    
                    OutlinedButton(
                      onPressed: null, // Disabled while adding card
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        side: BorderSide(color: colorScheme.onSurface.withValues(alpha: 0.1)),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          const Icon(Icons.arrow_forward, size: 18),
                          const SizedBox(width: 8),
                          Text(pMethod == 'Credit Card' ? 'Pay now - \${{amount}}'.tr(namedArgs: {'amount': tAmount}) : 'Confirm Order'.tr(), style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                        ]
                      ),
                    ),
                    const SizedBox(height: 16),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.shield_outlined, size: 14, color: colorScheme.onSurface.withValues(alpha: 0.5)),
                        const SizedBox(width: 6),
                        Text('SSL encrypted - Secured by Lagbar'.tr(), style: TextStyle(fontSize: 11, color: colorScheme.onSurface.withValues(alpha: 0.5))),
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



