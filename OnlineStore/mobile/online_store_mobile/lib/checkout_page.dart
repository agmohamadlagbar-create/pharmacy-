import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';

class CheckoutPage extends StatefulWidget {
  final Map<String, dynamic> cart;

  const CheckoutPage({super.key, required this.cart});

  @override
  State<CheckoutPage> createState() => _CheckoutPageState();
}

class _CheckoutPageState extends State<CheckoutPage> {
  List<dynamic> _addresses = [];
  bool _isLoading = true;
  String? _selectedAddressId;
  String _paymentMethod = 'Cash on Delivery';
  final _offerCodeCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _fetchAddresses();
  }

  @override
  void dispose() {
    _offerCodeCtrl.dispose();
    super.dispose();
  }

  Future<void> _fetchAddresses() async {
    setState(() => _isLoading = true);
    try {
      final res = await Api.get('/api/me/addresses');
      setState(() {
        // Api.get wraps array responses as {'data': [...]}
        final list = res['data'];
        _addresses = list is List ? list : <dynamic>[];
        if (_addresses.isNotEmpty) {
          final def = _addresses.firstWhere((a) => a['isDefault'] == true, orElse: () => _addresses.first);
          _selectedAddressId = def['id'].toString();
        }
      });
    } catch (e) {
      debugPrint("Failed to fetch addresses: $e");
    } finally {
      setState(() => _isLoading = false);
    }
  }

  void _showAddAddressSheet() {
    final titleCtrl = TextEditingController(text: 'Home');
    final streetCtrl = TextEditingController();
    final cityCtrl = TextEditingController();
    final buildingCtrl = TextEditingController();

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Theme.of(context).cardTheme.color,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return Padding(
          padding: EdgeInsets.only(
            bottom: MediaQuery.of(ctx).viewInsets.bottom,
            left: 20, right: 20, top: 20,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text('Add New Address'.tr(), style: GoogleFonts.inter(
                fontSize: 20,
                fontWeight: FontWeight.bold,
              )),
              const SizedBox(height: 16),
              TextField(
                controller: titleCtrl,
                decoration: InputDecoration(labelText: 'Label (e.g., Home)'.tr()),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: streetCtrl,
                decoration: InputDecoration(labelText: 'Street *'.tr()),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: cityCtrl,
                decoration: InputDecoration(labelText: 'City *'.tr()),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: buildingCtrl,
                decoration: InputDecoration(labelText: 'Building/Apt'.tr()),
              ),
              const SizedBox(height: 20),
              ElevatedButton(
                onPressed: () async {
                  if (streetCtrl.text.isEmpty || cityCtrl.text.isEmpty) {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Street and City are required'.tr())));
                    return;
                  }
                  
                  try {
                    final newAddr = await Api.post('/api/me/addresses', {
                      'title': titleCtrl.text,
                      'street': streetCtrl.text,
                      'city': cityCtrl.text,
                      'buildingNumber': buildingCtrl.text,
                    });
                    
                    setState(() {
                      _addresses.add(newAddr);
                      _selectedAddressId = newAddr['id'].toString();
                    });
                    if (ctx.mounted) Navigator.pop(ctx);
                  } catch (e) {
                    if (ctx.mounted) {
                      ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(content: Text('${'Error:'.tr()} $e')));
                    }
                  }
                },
                child: Text('Save Address'.tr()),
              ),
              const SizedBox(height: 30),
            ],
          ),
        );
      },
    );
  }

  Future<void> _proceedToPayment() async {
    if (_selectedAddressId == null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Please select or add a delivery address'.tr())));
      return;
    }

    final selectedAddr = _addresses.firstWhere((a) => a['id'].toString() == _selectedAddressId, orElse: () => null);
    if (selectedAddr == null) return;

    final addressCity = selectedAddr['city']?.toString().toLowerCase().trim();

    // Check location mismatch
    try {
      final profile = await Api.get('/api/me');
      final profileCity = profile['city']?.toString().toLowerCase().trim();
      
      if (profileCity != null && profileCity.isNotEmpty && addressCity != null && addressCity.isNotEmpty && profileCity != addressCity) {
        if (!mounted) return;
        final shouldUpdate = await showDialog<bool>(
          context: context,
          builder: (ctx) => AlertDialog(
            title: Text('Location Mismatch'.tr()),
            content: Text('Your selected delivery address is in {{addressCity}}, but your profile is currently set to {{profileCity}}.\n\nWould you like to update your profile location to match your delivery location?'.tr(namedArgs: {'addressCity': selectedAddr['city'], 'profileCity': profile['city']}), style: GoogleFonts.inter()),
            actions: [
              TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text('No, keep profile city'.tr())),
              FilledButton(onPressed: () => Navigator.pop(ctx, true), child: Text('Yes, update profile'.tr())),
            ],
          )
        );
        
        if (shouldUpdate == true) {
          await Api.put('/api/me/profile', {
            'fullName': profile['fullName'],
            'city': selectedAddr['city'],
            'country': profile['country'] ?? 'Unknown'
          });
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Profile location updated automatically.'.tr())));
          }
        }
      }
    } catch (e) {
      debugPrint('Failed to verify profile location: $e');
    }

    if (!mounted) return;

    final addressString = "${selectedAddr['title']}: ${selectedAddr['street']}, ${selectedAddr['buildingNumber'] ?? ''}, ${selectedAddr['city']}".trim();

    final data = {
      'addressString': addressString,
      'deliveryCity': selectedAddr['city'],
      'paymentMethod': _paymentMethod,
      'offerCode': _offerCodeCtrl.text,
    };

    Navigator.of(context).pushNamed('/payment', arguments: {
      'cart': widget.cart,
      'checkoutData': data,
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    return Scaffold(
      appBar: AppBar(
        title: Text('Checkout'.tr(), style: GoogleFonts.inter(fontWeight: FontWeight.w600)),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // ADDRESS
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('1. Delivery Address'.tr(), style: GoogleFonts.inter(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    )),
                    const SizedBox(height: 12),
                    if (_addresses.isEmpty)
                      Text('You have no saved addresses.'.tr(), style: GoogleFonts.inter(color: Colors.grey))
                    else
                      ..._addresses.map((a) {
                        final isSelected = _selectedAddressId == a['id'].toString();
                        return InkWell(
                          onTap: () => setState(() => _selectedAddressId = a['id'].toString()),
                          child: Container(
                            margin: const EdgeInsets.only(bottom: 8),
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              border: Border.all(color: isSelected ? Theme.of(context).colorScheme.primary : Colors.grey.withValues(alpha: 0.3)),
                              borderRadius: BorderRadius.circular(8),
                              color: isSelected ? Theme.of(context).colorScheme.primary.withValues(alpha: 0.05) : null,
                            ),
                            child: Row(
                              children: [
                                Icon(
                                  isSelected ? Icons.check_circle : Icons.radio_button_unchecked,
                                  color: isSelected ? Theme.of(context).colorScheme.primary : Colors.grey,
                                ),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(a['title'] ?? 'Other'.tr(), style: GoogleFonts.dmSans(fontWeight: FontWeight.bold)),
                                      Text('${a['street']}, ${a['buildingNumber'] ?? ''}', style: GoogleFonts.dmSans(fontSize: 14)),
                                      Text('${a['city']}', style: GoogleFonts.dmSans(fontSize: 14)),
                                    ],
                                  ),
                                ),
                              ],
                            ),
                          ),
                        );
                      }),
                    const SizedBox(height: 12),
                    TextButton.icon(
                      onPressed: _showAddAddressSheet,
                      icon: const Icon(Icons.add),
                      label: Text('Add new address'.tr(), style: GoogleFonts.inter(fontWeight: FontWeight.w500)),
                    )
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),

            // PAYMENT
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('2. Payment Method'.tr(), style: GoogleFonts.inter(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    )),
                    const SizedBox(height: 12),
                    SegmentedButton<String>(
                      segments: [
                        ButtonSegment(value: 'Cash on Delivery', label: Text('Cash on Delivery'.tr()), icon: const Icon(Icons.local_shipping_outlined)),
                        ButtonSegment(value: 'Credit Card', label: Text('Credit Card'.tr()), icon: const Icon(Icons.credit_card)),
                      ],
                      selected: {_paymentMethod},
                      onSelectionChanged: (s) => setState(() => _paymentMethod = s.first),
                      showSelectedIcon: false,
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),

            // OFFERS
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('3. Offers & Promos'.tr(), style: GoogleFonts.inter(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    )),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _offerCodeCtrl,
                      decoration: InputDecoration(
                        labelText: 'Enter Offer Code'.tr(),
                        border: const OutlineInputBorder(),
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 32),

            // SUMMARY
            Card(
              color: Theme.of(context).colorScheme.primary.withValues(alpha: 0.05),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text('Total Amount'.tr(), style: GoogleFonts.inter(fontSize: 16)),
                        Text('\$${(widget.cart['totalAmount'] as num).toStringAsFixed(2)}', 
                             style: GoogleFonts.inter(fontSize: 22, fontWeight: FontWeight.bold, color: Theme.of(context).colorScheme.primary)),
                      ],
                    ),
                    const SizedBox(height: 16),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton(
                        onPressed: _proceedToPayment,
                        child: Text('Proceed to Payment'.tr(), style: GoogleFonts.inter(fontWeight: FontWeight.bold)),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

