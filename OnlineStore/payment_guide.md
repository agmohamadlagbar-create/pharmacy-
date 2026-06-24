# Payment Integration Guide

This document explains how the payment system is implemented in the Lagbar Online Store application.

## 1. Overview
The application supports two primary payment methods:
- **Credit/Debit Card**: Handled through **Stripe**.
- **Cash on Delivery (COD)**: A manual payment method where the user pays upon receipt.

## 2. Stripe Integration Architecture
The integration follows Stripe's recommended patterns for security and PCI compliance. Card data is tokenized on the client-side (Web/Mobile) and never stored on our servers.

### Technology Stack
- **Backend**: ASP.NET Core with `Stripe.net`.
- **Web Frontend**: React with `@stripe/stripe-js` and `@stripe/react-stripe-js`.
- **Mobile Frontend**: Flutter with `flutter_stripe`.

---

## 3. Backend Implementation

### Configuration
Stripe keys are configured in `backend/OnlineStore.Api/appsettings.json`:
```json
"Stripe": {
  "PublishableKey": "pk_test_...",
  "SecretKey": "sk_test_..."
}
```

### Key Components
1.  **`PaymentController.cs`**:
    - `GET /api/payment/config`: Provides the publishable key to frontends.
    - `POST /api/payment/cards`: Attaches a `PaymentMethodId` to the user's Stripe Customer record.
    - `GET /api/payment/cards`: Lists the user's saved cards.
    - `DELETE /api/payment/cards/{id}`: Removes a saved card.
2.  **`OrdersController.cs`**:
    - Processes order creation.
    - For card payments, it creates and confirms a **`PaymentIntent`** using the provided `PaymentToken` (Payment Method ID).
    - If payment fails, the order is automatically cancelled.

### Automatic Customer Creation
When a user adds their first card, the backend automatically creates a Stripe Customer and saves the `StripeCustomerId` in the `AppUser` entity:
```csharp
var options = new CustomerCreateOptions { Email = user.Email, Name = user.FullName };
var service = new CustomerService();
var customer = await service.CreateAsync(options);
user.StripeCustomerId = customer.Id;
await _userManager.UpdateAsync(user);
```

---

## 4. Web Implementation (`web/online-store-web`)

### Flow
1.  **Card Entry**: Uses Stripe's `CardElement` (via `StripeAddCard.jsx`) to safely collect card details.
2.  **Tokenization**: The browser communicates directly with Stripe to create a `PaymentMethod`.
3.  **Saving**: The `PaymentMethodId` is sent to our backend to be associated with the user.
4.  **Checkout**: During checkout, the user selects a saved card. The `id` of that card (e.g., `pm_...`) is sent as the `paymentToken` in the order request.

---

## 5. Mobile Implementation (`mobile/online_store_mobile`)

### Flow
1.  **Setup**: Initialized in `main.dart` with the publishable key.
2.  **Card Field**: Uses the `CardField` widget from `flutter_stripe` for secure input.
3.  **Payment Processing**: 
    - `Stripe.instance.createPaymentMethod(...)` is called to generate a token.
    - The token is saved via `POST /api/payment/cards`.
    - The order is placed via `POST /api/orders` using the selected payment method ID.

---

## 6. Order & Transaction Metadata
To improve debugging and tracking in the **Stripe Dashboard**, every `PaymentIntent` is created with metadata:

| Key | Description |
| :--- | :--- |
| `OrderId` | The internal ID of the order in our database. |
| `UserId` | The ID of the buyer. |
| `UserEmail` | The email of the buyer. |

---

## 7. Security Best Practices
- **No Card Storage**: We only store Stripe's `PaymentMethodId` (e.g., `pm_123...`) and `CustomerId`. We **never** store card numbers, CVV, or expirations.
- **SSL/HTTPS**: All communication between the frontends and backend is encrypted (via ngrok tunnels or production SSL).
- **Server-Side Validation**: Final amounts and product prices are always recalculated on the backend to prevent client-side price manipulation.
