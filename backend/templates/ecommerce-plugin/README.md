# ECommerce plugin template

Scaffolded with `dotnet new ecommerce-plugin`.

## Contracts

This project references:

- `ECommerce.IntegrationContracts` — RabbitMQ event envelopes / names
- `ECommerce.PluginContracts` — `PluginManifest` shape

Pack them from the platform repo (`backend/`):

```bash
dotnet pack src/ECommerce.IntegrationContracts/ECommerce.IntegrationContracts.csproj -c Release -o ./nupkgs
dotnet pack src/ECommerce.PluginContracts/ECommerce.PluginContracts.csproj -c Release -o ./nupkgs
```

The included `nuget.config` points at `../../nupkgs` relative to this template folder when used inside the monorepo. Outside the monorepo, point `ecommerce-local` at your packed folder or publish to a private NuGet feed.

## Runtime rules

- Listen on `0.0.0.0:8080`
- `GET /health` → `200` when ready
- Read identity from `X-User-Id` / `X-User-Email` (core terminates JWT)
- Configure via env: `ConnectionStrings__Default`, `RabbitMq__*`, `ECOMMERCE_PLUGIN_ID`
- Cross-plugin effects: publish/consume integration events only — never call other plugins over HTTP

## Build and push

1. Edit `plugin.json` (`id`, `image`, `version`, …). Id must match `^[a-z0-9]+(-[a-z0-9]+)*-plugin$`.
2. Build and push the image referenced by `plugin.json`:

```bash
docker build -t localhost:5000/ecommerce/sample-plugin:1.0.0 .
docker push localhost:5000/ecommerce/sample-plugin:1.0.0
```

## Publish to the marketplace

Requires the `publisher` role. Request it from the **Publisher Portal** (`/publisher`), or have an
admin grant it via `POST /api/auth/users/role`. Then publish the manifest in the portal
(**Publish plugin**) or:

```bash
curl -X POST http://localhost:8080/api/marketplace/listings \
  -H "Authorization: Bearer <publisher-jwt>" \
  -H "Content-Type: application/json" \
  -d @plugin.json
```

Then install from the UI Marketplace or:

```bash
curl -X POST http://localhost:8080/api/plugins/sample-plugin/install \
  -H "Authorization: Bearer <user-jwt>" \
  -H "Content-Type: application/json" \
  -d '{"grantedPermissions":[]}'
```

Call your endpoints through the core proxy: `http://localhost:8080/api/p/sample-plugin/greeting`.
