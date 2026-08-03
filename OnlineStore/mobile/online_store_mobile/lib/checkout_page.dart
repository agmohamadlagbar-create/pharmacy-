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
        final list = res['data'];
        _addresses = list is List ? list : <dynamic>[];
        if (_addresses.isNotEmpty) {
          final def = _addresses.firstWhere((a) => a['isDefault'] == true,
              orElse: () => _addresses.first);
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
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final titleCtrl = TextEditingController(text: 'Home');
    final cityCtrl = TextEditingController();
    final districtCtrl = TextEditingController();
    final neighborhoodCtrl = TextEditingController();
    final streetCtrl = TextEditingController();
    final buildingCtrl = TextEditingController();
    final floorCtrl = TextEditingController();
    final apartmentNoCtrl = TextEditingController();

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: colorScheme.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) {
        return Padding(
          padding: EdgeInsets.only(
            bottom: MediaQuery.of(ctx).viewInsets.bottom,
            left: 24,
            right: 24,
            top: 24,
          ),
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Add New Address'.tr(),
                  style: GoogleFonts.inter(
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                    color: colorScheme.onSurface,
                  ),
                ),
                const SizedBox(height: 16),
                TextField(
                  controller: titleCtrl,
                  style: GoogleFonts.inter(color: colorScheme.onSurface),
                  decoration: InputDecoration(
                    labelText: 'Label (e.g., Home)'.tr(),
                    border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12)),
                  ),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: cityCtrl,
                        style: GoogleFonts.inter(color: colorScheme.onSurface),
                        decoration: InputDecoration(
                          labelText: 'City *'.tr(),
                          border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12)),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: TextField(
                        controller: districtCtrl,
                        style: GoogleFonts.inter(color: colorScheme.onSurface),
                        decoration: InputDecoration(
                          labelText: 'District *'.tr(),
                          border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12)),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: neighborhoodCtrl,
                  style: GoogleFonts.inter(color: colorScheme.onSurface),
                  decoration: InputDecoration(
                    labelText: 'Neighborhood *'.tr(),
                    border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12)),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: streetCtrl,
                  style: GoogleFonts.inter(color: colorScheme.onSurface),
                  decoration: InputDecoration(
                    labelText: 'Street *'.tr(),
                    border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12)),
                  ),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: buildingCtrl,
                        style: GoogleFonts.inter(color: colorScheme.onSurface),
                        decoration: InputDecoration(
                          labelText: 'Bldg No *'.tr(),
                          border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12)),
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: TextField(
                        controller: floorCtrl,
                        style: GoogleFonts.inter(color: colorScheme.onSurface),
                        decoration: InputDecoration(
                          labelText: 'Floor *'.tr(),
                          border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12)),
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: TextField(
                        controller: apartmentNoCtrl,
                        style: GoogleFonts.inter(color: colorScheme.onSurface),
                        decoration: InputDecoration(
                          labelText: 'Apt No *'.tr(),
                          border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12)),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                ElevatedButton(
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  onPressed: () async {
                    if (cityCtrl.text.trim().isEmpty ||
                        districtCtrl.text.trim().isEmpty ||
                        neighborhoodCtrl.text.trim().isEmpty ||
                        streetCtrl.text.trim().isEmpty ||
                        buildingCtrl.text.trim().isEmpty ||
                        floorCtrl.text.trim().isEmpty ||
                        apartmentNoCtrl.text.trim().isEmpty) {
                      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                          content: Text(
                              'City, District, Neighborhood, Street, Bldg, Floor, and Apt are required'.tr())));
                      return;
                    }

                    try {
                      final newAddr = await Api.post('/api/me/addresses', {
                        'title': titleCtrl.text,
                        'city': cityCtrl.text,
                        'district': districtCtrl.text,
                        'neighborhood': neighborhoodCtrl.text,
                        'street': streetCtrl.text,
                        'buildingNumber': buildingCtrl.text,
                        'floor': floorCtrl.text,
                        'apartmentNo': apartmentNoCtrl.text,
                      });

                      setState(() {
                        _addresses.add(newAddr);
                        _selectedAddressId = newAddr['id'].toString();
                      });
                      if (ctx.mounted) Navigator.pop(ctx);
                    } catch (e) {
                      if (ctx.mounted) {
                        ScaffoldMessenger.of(ctx).showSnackBar(
                            SnackBar(content: Text('${'Error:'.tr()} $e')));
                      }
                    }
                  },
                  child: Text(
                    'Save Address'.tr(),
                    style: GoogleFonts.inter(fontWeight: FontWeight.bold),
                  ),
                ),
                const SizedBox(height: 30),
              ],
            ),
          ),
        );
      },
    );
  }

  Future<void> _proceedToPayment() async {
    if (_selectedAddressId == null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text('Please select or add a delivery address'.tr())));
      return;
    }

    final selectedAddr = _addresses.firstWhere(
        (a) => a['id'].toString() == _selectedAddressId,
        orElse: () => null);
    if (selectedAddr == null) return;

    final addressCity = selectedAddr['city']?.toString().toLowerCase().trim();

    try {
      final profile = await Api.get('/api/me');
      final profileCity = profile['city']?.toString().toLowerCase().trim();

      if (profileCity != null &&
          profileCity.isNotEmpty &&
          addressCity != null &&
          addressCity.isNotEmpty &&
          profileCity != addressCity) {
        if (!mounted) return;
        final shouldUpdate = await showDialog<bool>(
            context: context,
            builder: (ctx) => AlertDialog(
                  title: Text('Location Mismatch'.tr()),
                  content: Text(
                      'Your selected delivery address is in {{addressCity}}, but your profile is currently set to {{profileCity}}.\n\nWould you like to update your profile location to match your delivery location?'
                          .tr(namedArgs: {
                        'addressCity': selectedAddr['city'],
                        'profileCity': profile['city']
                      }),
                      style: GoogleFonts.inter()),
                  actions: [
                    TextButton(
                        onPressed: () => Navigator.pop(ctx, false),
                        child: Text('No, keep profile city'.tr())),
                    FilledButton(
                        onPressed: () => Navigator.pop(ctx, true),
                        child: Text('Yes, update profile'.tr())),
                  ],
                ));

        if (shouldUpdate == true) {
          await Api.put('/api/me/profile', {
            'fullName': profile['fullName'],
            'city': selectedAddr['city'],
            'district': selectedAddr['district'] ?? profile['district'],
            'neighborhood': selectedAddr['neighborhood'] ?? profile['neighborhood'],
            'street': selectedAddr['street'] ?? profile['street'],
            'buildingNumber': selectedAddr['buildingNumber'] ?? profile['buildingNumber'],
            'floor': selectedAddr['floor'] ?? profile['floor'],
            'apartmentNo': selectedAddr['apartmentNo'] ?? profile['apartmentNo'],
            'country': profile['country'] ?? 'Unknown'
          });
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                content: Text('Profile location updated automatically.'.tr())));
          }
        }
      }
    } catch (e) {
      debugPrint('Failed to verify profile location: $e');
    }

    if (!mounted) return;

    final String bldg = selectedAddr['buildingNumber'] ?? '';
    final String floor = selectedAddr['floor'] ?? '';
    final String apt = selectedAddr['apartmentNo'] ?? '';
    final String dist = selectedAddr['district'] ?? '';
    final String neigh = selectedAddr['neighborhood'] ?? '';

    final addressString =
        "${selectedAddr['title']}: ${selectedAddr['street']}, $neigh, $dist, Bldg $bldg/Fl $floor/Apt $apt, ${selectedAddr['city']}"
            .trim();

    final data = {
      'deliveryAddress': addressString,
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
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;
    final isDark = theme.brightness == Brightness.dark;

    final double totalAmount = (widget.cart['totalAmount'] is num)
        ? (widget.cart['totalAmount'] as num).toDouble()
        : double.tryParse(widget.cart['totalAmount'].toString()) ?? 0.0;

    if (_isLoading) {
      return Scaffold(
        backgroundColor: theme.scaffoldBackgroundColor,
        body: Center(child: CircularProgressIndicator(color: colorScheme.primary)),
      );
    }

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      appBar: AppBar(
        backgroundColor: colorScheme.surface,
        elevation: 0,
        scrolledUnderElevation: 1,
        title: Text(
          'Checkout'.tr(),
          style: GoogleFonts.inter(
              fontWeight: FontWeight.bold, color: colorScheme.onSurface),
        ),
        leading: IconButton(
          icon: Icon(Icons.arrow_back_rounded, color: colorScheme.onSurface),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: SingleChildScrollView(
        physics: const BouncingScrollPhysics(),
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // 1. Delivery Address Card
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: colorScheme.surface,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: isDark
                      ? Colors.white.withValues(alpha: 0.1)
                      : colorScheme.outline.withValues(alpha: 0.5),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.location_on_outlined,
                          size: 20, color: colorScheme.primary),
                      const SizedBox(width: 8),
                      Text(
                        '1. Delivery Address'.tr(),
                        style: GoogleFonts.inter(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: colorScheme.onSurface,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 14),
                  if (_addresses.isEmpty)
                    Text(
                      'You have no saved addresses.'.tr(),
                      style:
                          GoogleFonts.inter(color: colorScheme.onSurfaceVariant),
                    )
                  else
                    ..._addresses.map((a) {
                      final isSelected =
                          _selectedAddressId == a['id'].toString();
                      final street = a['street'] ?? '';
                      final district = a['district'] ?? '';
                      final neighborhood = a['neighborhood'] ?? '';
                      final bldg = a['buildingNumber'] ?? '';
                      final floor = a['floor'] ?? '';
                      final apt = a['apartmentNo'] ?? '';

                      return InkWell(
                        onTap: () => setState(
                            () => _selectedAddressId = a['id'].toString()),
                        borderRadius: BorderRadius.circular(12),
                        child: Container(
                          margin: const EdgeInsets.only(bottom: 10),
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            border: Border.all(
                              color: isSelected
                                  ? colorScheme.primary
                                  : colorScheme.outline,
                              width: isSelected ? 2 : 1,
                            ),
                            borderRadius: BorderRadius.circular(12),
                            color: isSelected
                                ? colorScheme.primary.withValues(alpha: 0.06)
                                : colorScheme.surfaceContainerHighest
                                    .withValues(alpha: 0.3),
                          ),
                          child: Row(
                            children: [
                              Icon(
                                isSelected
                                    ? Icons.check_circle_rounded
                                    : Icons.radio_button_unchecked_rounded,
                                color: isSelected
                                    ? colorScheme.primary
                                    : colorScheme.onSurfaceVariant,
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      a['title'] ?? 'Other'.tr(),
                                      style: GoogleFonts.inter(
                                          fontWeight: FontWeight.bold,
                                          color: colorScheme.onSurface),
                                    ),
                                    Text(
                                      '$street, $neighborhood, $district',
                                      style: GoogleFonts.inter(
                                          fontSize: 13,
                                          color: colorScheme.onSurfaceVariant),
                                    ),
                                    Text(
                                      'Bldg $bldg · Kat $floor · Daire $apt, ${a['city']}',
                                      style: GoogleFonts.inter(
                                          fontSize: 12,
                                          color: colorScheme.onSurfaceVariant),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      );
                    }),
                  TextButton.icon(
                    onPressed: _showAddAddressSheet,
                    icon: Icon(Icons.add_rounded, color: colorScheme.primary),
                    label: Text(
                      'Add new address'.tr(),
                      style: GoogleFonts.inter(
                          fontWeight: FontWeight.bold,
                          color: colorScheme.primary),
                    ),
                  )
                ],
              ),
            ),
            const SizedBox(height: 16),

            // 2. Payment Method Card
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: colorScheme.surface,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: isDark
                      ? Colors.white.withValues(alpha: 0.1)
                      : colorScheme.outline.withValues(alpha: 0.5),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.payment_outlined,
                          size: 20, color: colorScheme.primary),
                      const SizedBox(width: 8),
                      Text(
                        '2. Payment Method'.tr(),
                        style: GoogleFonts.inter(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: colorScheme.onSurface,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 14),
                  SegmentedButton<String>(
                    segments: [
                      ButtonSegment(
                        value: 'Cash on Delivery',
                        label: Text('Cash on Delivery'.tr()),
                        icon: const Icon(Icons.local_shipping_outlined),
                      ),
                      ButtonSegment(
                        value: 'Credit Card',
                        label: Text('Credit Card'.tr()),
                        icon: const Icon(Icons.credit_card_rounded),
                      ),
                    ],
                    selected: {_paymentMethod},
                    onSelectionChanged: (s) =>
                        setState(() => _paymentMethod = s.first),
                    showSelectedIcon: false,
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),

            // 3. Offers & Promo Card
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: colorScheme.surface,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: isDark
                      ? Colors.white.withValues(alpha: 0.1)
                      : colorScheme.outline.withValues(alpha: 0.5),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.local_offer_outlined,
                          size: 20, color: colorScheme.primary),
                      const SizedBox(width: 8),
                      Text(
                        '3. Offers & Promos'.tr(),
                        style: GoogleFonts.inter(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: colorScheme.onSurface,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 14),
                  TextField(
                    controller: _offerCodeCtrl,
                    style: GoogleFonts.inter(color: colorScheme.onSurface),
                    decoration: InputDecoration(
                      labelText: 'Enter Offer Code'.tr(),
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            // Order Summary & Action
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: colorScheme.primary.withValues(alpha: 0.05),
                borderRadius: BorderRadius.circular(20),
                border: Border.all(
                    color: colorScheme.primary.withValues(alpha: 0.5),
                    width: 1.5),
              ),
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'Total Amount'.tr(),
                        style: GoogleFonts.inter(
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                            color: colorScheme.onSurface),
                      ),
                      Text(
                        '\$${totalAmount.toStringAsFixed(2)}',
                        style: GoogleFonts.inter(
                            fontSize: 22,
                            fontWeight: FontWeight.bold,
                            color: colorScheme.primary),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: colorScheme.primary,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                        elevation: 2,
                      ),
                      onPressed: _proceedToPayment,
                      child: Text(
                        'Proceed to Payment'.tr(),
                        style: GoogleFonts.inter(
                            fontWeight: FontWeight.bold, fontSize: 16),
                      ),
                    ),
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
