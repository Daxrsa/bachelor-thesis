# Plugin author guide

How to build and publish a third-party plugin without cloning the full e-commerce monorepo.

## 1. Pack the host contracts (platform maintainers)

From `backend/`:

```bash
dotnet pack src/ECommerce.IntegrationContracts/ECommerce.IntegrationContracts.csproj -c Release -o ./nupkgs
dotnet pack src/ECommerce.PluginContracts/ECommerce.PluginContracts.csproj -c Release -o ./nupkgs
```

Authors consume these via `PackageReference` (or the local `nupkgs` feed in the template `nuget.config`).

## 2. Scaffold from the official template

```bash
dotnet new install ./templates/ecommerce-plugin
dotnet new ecommerce-plugin -n Acme.ReviewsPlugin --PluginId reviews-plugin --PluginName "Reviews Plugin"
```

## 3. Implement, build, push

Follow the generated README: implement `/health`, optional RabbitMQ consumers using `ECommerce.IntegrationContracts`, then:

```bash
docker build -t localhost:5000/ecommerce/reviews-plugin:1.0.0 .
docker push localhost:5000/ecommerce/reviews-plugin:1.0.0
```

## 4. Become a publisher

Register a normal account, then open **Publisher Portal** (`/publisher`) and submit a request.
An admin approves it under **Publisher Requests** (`/publisher-requests`). Sign in again so the JWT
includes `role=publisher`.

Alternatively an admin can grant the role directly:

```bash
curl -X POST http://localhost:8080/api/auth/users/role \
  -H "Authorization: Bearer <admin-jwt>" \
  -H 'Content-Type: application/json' \
  -d '{"email":"author@example.com","role":"publisher"}'
```

## 5. Publish the manifest (no catalog.json edit)

Use the Publisher Portal (**Publish plugin**), or:

```bash
curl -X POST http://localhost:8080/api/marketplace/listings \
  -H "Authorization: Bearer <publisher-jwt>" \
  -H 'Content-Type: application/json' \
  -d @plugin.json
```

The listing appears in `GET /api/plugins/marketplace`. Admins/users install as usual via `POST /api/plugins/{id}/install`.

Official plugins in `marketplace/catalog.json` always win on ID collision; you cannot publish over reserved official IDs.
