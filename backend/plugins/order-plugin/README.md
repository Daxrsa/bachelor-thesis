# Order Plugin

The order plugin owns immutable paid-order records.

It consumes `payment.succeeded.v1`, validates the payment, user, cart, and priced line snapshot, then persists exactly one order per payment ID. It publishes `order.created.v1` after the order is saved; the cart plugin consumes that event and clears only the matching cart.

## Endpoints

- `GET /health`
- `GET /orders/me`
- `GET /orders/me/{orderId}`

The core forwards the authenticated user as `X-User-Id`. Orders can only be read by their owner.

## Idempotency

`PaymentId` and `CorrelationId` have unique database constraints. Duplicate payment-success delivery returns the existing order and does not publish a second order-created event.
