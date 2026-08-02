# Bachelor Thesis — Plugin-Based E-Commerce Platform

A **self-hostable e-commerce core** where every feature beyond the essentials
(auth + plugin management + marketplace) is delivered as an **independent
containerized plugin**. The core discovers plugins through a marketplace
catalog, pulls them from a Docker registry, runs them as sibling containers on
a shared network, and proxies HTTP traffic to them under `/api/p/{pluginId}/…`.

```
┌───────────────┐          HTTPS         ┌────────────────────────┐
│  Angular SPA  │ ─────────────────────▶ │  ECommerce.Api (core)  │
│  (webclient)  │                        │  auth · plugins · proxy│
└───────────────┘                        └───────────┬────────────┘
                                                     │ Docker API
                                          ┌──────────┴──────────┐
                                          ▼                     ▼
                                   ┌────────────┐        ┌────────────┐
                                   │ hello-plug │  ...   │ other-plug │
                                   └────────────┘        └────────────┘
                                        (containers on ecommerce_plugins network)
```

## Repository layout

| Path | Purpose |
|------|---------|
| `backend/` | .NET 10 solution (core API, domain, infrastructure, plugin runtime, contracts) |
| `backend/plugins/hello-plugin/` | Sample containerized plugin |
| `backend/marketplace/catalog.json` | Static marketplace catalog (replace with a real registry later) |
| `webclient/` | Angular 21 (PrimeNG Sakai) SPA with auth + marketplace pages |
| `backend/docker-compose.yml` | All Docker services and plugin image builds |

## Quick start

### 1. Build and publish plugin images

```bash
cd backend
docker compose up -d registry
docker compose --profile plugin-images build hello-plugin-image products-plugin-image
docker compose --profile plugin-images push hello-plugin-image products-plugin-image
```

Quick check:

```bash
curl http://localhost:5000/v2/_catalog
```

### 2. Start Postgres + the core API

```bash
docker compose up --build
```

The API is now on <http://localhost:8080>, Swagger at <http://localhost:8080/swagger>.

The local registry is on <http://localhost:5000>.

### 3. Run the Angular client

```bash
cd ../webclient
npm install
npm start
```

Open <http://localhost:4200>, register an account, then visit **Marketplace**
to install `hello-plugin`. Once running, call it through the core:

```bash
curl -H "Authorization: Bearer <jwt>" http://localhost:8080/api/p/hello-plugin/greeting
```

## The plugin contract

Every plugin ships a `plugin.json` manifest and a Docker image. The manifest
is what the marketplace catalog exposes:

```jsonc
{
  "id": "hello-plugin",
  "name": "Hello Plugin",
  "version": "0.1.0",
  "description": "…",
  "publisher": "example",
  "image": "localhost:5000/ecommerce/hello-plugin:0.1.0",
  "containerPort": 8080,
  "healthEndpoint": "/health",
  "hostApi": "^1.0.0",
  "permissions": [],
  "uiExtensions": {}
}
```

Runtime rules:

- Plugin must expose `GET /health` → `200 OK`.
- Plugin binds `0.0.0.0:<containerPort>` inside its container.
- Plugin is reachable **only through the core** (`/api/p/{id}/…`).
- The core forwards the caller identity via `X-User-Id` / `X-User-Email` headers.

## Security model

- **JWT bearer** authentication on all `/api/plugins` endpoints.
- **Permission grants** — the client must submit every permission listed in
  the manifest when installing; the server rejects incomplete grants.
- **Network isolation** — plugin containers live on `ecommerce_plugins`, not
  exposed to the host. Only the core can reach them.
- **Central choke-point** — all plugin traffic funnels through
  `PluginProxyController`, where audit logging, rate limiting and additional
  policy hooks can be added.
- **Signed images (todo)** — production deployments should verify image
  signatures (e.g. cosign) before pulling.

## Development notes

- The scaffold uses `EnsureCreatedAsync()` for the DB schema so `docker compose
  up` works out of the box. Switch to `dotnet ef migrations add …` +
  `MigrateAsync()` before production.
- Change `Jwt:Key` in `appsettings.json` — the default is a placeholder.
- To run the API on the host (outside Docker) point `PluginRuntime:DockerEndpoint`
  at your local Docker socket and set `ConnectionStrings:Default` at a reachable
  Postgres.
