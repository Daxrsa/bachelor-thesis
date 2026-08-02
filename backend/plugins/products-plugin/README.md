# Products Plugin

This plugin owns the product catalog database. The core runtime starts two containers for it:

- `ecom-plugin-products-plugin`: the ASP.NET Core plugin API.
- `ecom-plugin-products-plugin-db`: a private Postgres sidecar using the named volume `ecom-plugin-products-plugin-data`.

The core API stores only the plugin installation record. Product rows live in the plugin database and are accessed through the plugin proxy at `/api/p/products-plugin/products`.

Cross-plugin relationships should use stable IDs and API composition instead of database foreign keys across containers. For example, an Orders plugin should store `ProductId` and a purchase-time product snapshot, then call the Products plugin through the core proxy when it needs fresh product details.