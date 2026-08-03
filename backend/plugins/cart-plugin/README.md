# Cart Plugin

This plugin owns shopping carts in an isolated plugin database.

- One cart belongs to one user (`UserId` is unique in `Carts`).
- A cart can contain many products via `CartItems`.
- Service methods are event-oriented for `ProductAddedToCartEvent` and `ProductRemovedFromCartEvent`.

At this stage, the plugin defines event handling methods and HTTP endpoints, but does not yet include RabbitMQ consumer wiring.
