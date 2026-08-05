# Payment Plugin

Consumes integration events and processes payments through Stripe.

## Event-driven flow

1. `products-plugin` publishes `product.upserted.v1` and `product.deleted.v1`.
2. `cart-plugin` publishes `cart.checkout-requested.v1` for authenticated users.
3. `payment-plugin` consumes those events, computes amount as sum of `quantity * productPrice`, then calls Stripe.
4. `payment-plugin` publishes either `payment.succeeded.v1` or `payment.failed.v1`.

## Required development configuration

The plugin needs Stripe settings passed via environment variables:

- `Stripe__SecretKey`
- `Stripe__SuccessPaymentMethodToken` (optional, default `pm_card_visa`)
- `Stripe__FailurePaymentMethodToken` (optional, default `pm_card_chargeDeclined`)

For testing, you can trigger a failure by setting checkout `paymentMethod` to `declined`.
