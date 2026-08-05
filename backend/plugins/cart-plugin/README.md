# Cart Plugin

This plugin owns shopping carts in an isolated plugin database.

- One cart belongs to one user (`UserId` is unique in `Carts`).
- A cart can contain many products via `CartItems`.
- Service methods are event-oriented for `ProductAddedToCartEvent` and `ProductRemovedFromCartEvent`.

`POST /carts/{userId}/items` and `DELETE /carts/{userId}/items/{productId}` no longer update the cart
in-process. They publish the corresponding integration event to the `ecommerce.integration-events`
topic exchange on RabbitMQ (routing key = event name) and return `202 Accepted`. A background
`CartEventsConsumer` hosted service consumes those events from the `cart-plugin.cart-events` queue and
applies them via `ICartService`, so cart state updates asynchronously. Callers should poll
`GET /carts/{userId}` to observe the applied change.

