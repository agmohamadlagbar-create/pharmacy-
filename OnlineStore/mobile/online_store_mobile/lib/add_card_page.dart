import 'package:flutter/material.dart';
import 'package:flutter_stripe/flutter_stripe.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';

class AddCardPage extends StatefulWidget {
  const AddCardPage({super.key});

  @override
  State<AddCardPage> createState() => _AddCardPageState();
}

class _AddCardPageState extends State<AddCardPage> {
  bool _isProcessing = false;
  CardFieldInputDetails? _cardDetails;
  final TextEditingController _cardholderNameController = TextEditingController();

  @override
  void dispose() {
    _cardholderNameController.dispose();
    super.dispose();
  }

  Future<void> _saveCard() async {
    if (_cardDetails == null || !_cardDetails!.complete) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Please complete card details'.tr())),
      );
      return;
    }
    if (_cardholderNameController.text.trim().isEmpty) {
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
            billingDetails: BillingDetails(
              name: _cardholderNameController.text.trim(),
            ),
          ),
        ),
      );

      final response = await Api.post('/api/payment/cards', {
        'paymentMethodId': paymentMethod.id,
      });

      if (!mounted) return;
      Navigator.pop(context, response);

    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('${'Failed:'.tr()} $e')),
      );
    } finally {
      if (mounted) setState(() => _isProcessing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Add a New Card'.tr())),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            Text(
              'Enter your card details safely. We use Stripe to process and protect your data.'.tr(),
              style: Theme.of(context).textTheme.bodyLarge,
            ),
            const SizedBox(height: 24),
            Text(
              'Card details'.tr(),
              style: Theme.of(context).textTheme.labelLarge,
            ),
            const SizedBox(height: 8),
            CardFormField(
              enablePostalCode: false,
              onCardChanged: (details) {
                setState(() => _cardDetails = details);
              },
            ),
            const SizedBox(height: 16),
            Text(
              'Cardholder name'.tr(),
              style: Theme.of(context).textTheme.labelLarge,
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _cardholderNameController,
              textCapitalization: TextCapitalization.words,
              decoration: InputDecoration(
                hintText: 'Name on card'.tr(),
                border: const OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 40),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _isProcessing ? null : _saveCard,
                style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 16)),
                child: _isProcessing
                    ? const CircularProgressIndicator(color: Colors.white)
                    : Text('Save Card'.tr(), style: const TextStyle(fontSize: 16)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
