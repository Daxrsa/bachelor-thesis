# ECommerce.IntegrationContracts

Shared integration-event contracts for plugin-to-plugin event-driven communication.

## Versioning

Contracts are grouped by version namespace (`V1`, `V2`, etc.).

- `V1/IntegrationEventEnvelope.cs` defines the shared message envelope.
- `V1/EventNames.cs` defines canonical event names.
- `V1/CartAndProductEvents.cs` defines cart integration payloads for add/remove product events.
- `V1/OrderAndPaymentEvents.cs` defines order/payment integration payloads.

## Rules

- Keep only transport-safe integration contracts here.
- Do not add EF entities, aggregate roots, or persistence-specific models.
- Prefer additive, backward-compatible changes inside a version.
- Breaks require a new version namespace.
