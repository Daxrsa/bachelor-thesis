# Store Banner plugin

Third-party sample used to exercise marketplace publish (not listed in `catalog.json`).

## Contracts

This project references:

- `ECommerce.IntegrationContracts` — RabbitMQ event envelopes / names
- `ECommerce.PluginContracts` — `PluginManifest` shape

Pack them from the platform repo (`backend/`), then copy the packages next to this project so Docker restore can see them:

```bash
dotnet pack src/ECommerce.IntegrationContracts/ECommerce.IntegrationContracts.csproj -c Release -o ./nupkgs
dotnet pack src/ECommerce.PluginContracts/ECommerce.PluginContracts.csproj -c Release -o ./nupkgs
mkdir -p community/banner-plugin/nupkgs
cp nupkgs/*.nupkg community/banner-plugin/nupkgs/
```

`nuget.config` points at `./nupkgs` (works both on the host and inside the image).

## Runtime

- Listen on `0.0.0.0:8080`
- `GET /health` → `200` when ready
- `GET /banner` → promo payload; uses `X-User-Email` when the core forwards identity

## Build and push

```bash
docker build -t localhost:5000/ecommerce/banner-plugin:1.0.0 .
docker push localhost:5000/ecommerce/banner-plugin:1.0.0
```

## Publish

Requires the `publisher` role. Use the Publisher Portal (`/publisher/plugins/new`) or:

```bash
curl -X POST http://localhost:8080/api/marketplace/listings \
  -H "Authorization: Bearer <publisher-jwt>" \
  -H "Content-Type: application/json" \
  -d @plugin.json
```

Install, then call through the core: `http://localhost:8080/api/p/banner-plugin/banner`.
