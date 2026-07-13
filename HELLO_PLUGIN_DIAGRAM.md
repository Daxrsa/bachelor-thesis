# Hello Plugin Architecture Diagram

This document explains how the hello-plugin works across frontend, backend, and Docker.

## 1) System Architecture

```mermaid
flowchart LR
    subgraph FE[Frontend]
        W[Angular HelloPluginWidget]
        LS[localStorage ecommerce.token]
    end

    subgraph API[Core Backend API :8080]
        AUTH[AuthController /api/auth/*]
        PLUG[PluginsController /api/plugins/*]
        PROXY[PluginProxyController /api/p/{pluginId}/*]
        SVC[PluginService]
        RT[DockerPluginRuntime]
    end

    subgraph DATA[Persistence]
        PG[(Postgres)]
        INST[(PluginInstallations table)]
        USERS[(Users table)]
        CAT[marketplace/catalog.json]
    end

    subgraph DOCKER[Docker]
        NET[(ecommerce_plugins network)]
        HP[ecom-plugin-hello-plugin container]
        REG[(Local Registry :5000)]
    end

    W -- login/register --> AUTH
    AUTH --> USERS
    AUTH -- JWT token --> W
    W --> LS

    W -- install/uninstall/list --> PLUG
    PLUG --> SVC
    SVC --> CAT
    SVC --> INST
    SVC --> RT
    RT --> REG
    RT --> NET
    RT --> HP

    W -- greeting call with Bearer token --> PROXY
    PROXY --> SVC
    SVC --> INST
    PROXY -- forwards HTTP + X-User-* headers --> HP
    HP -- greeting response --> PROXY
    PROXY --> W

    SVC --> PG
```

## 2) Runtime Sequence (Install + Greeting)

```mermaid
sequenceDiagram
    participant U as User
    participant FE as Angular HelloPluginWidget
    participant AUTH as AuthController
    participant PLUG as PluginsController
    participant SVC as PluginService
    participant RT as DockerPluginRuntime
    participant DB as Postgres
    participant PROXY as PluginProxyController
    participant HP as hello-plugin container

    U->>FE: Click Login/Register
    FE->>AUTH: POST /api/auth/login or /register
    AUTH->>DB: Validate/create user
    AUTH-->>FE: JWT token
    FE->>FE: Store token in localStorage

    U->>FE: Click Install Plugin
    FE->>PLUG: POST /api/plugins/hello-plugin/install
    PLUG->>SVC: InstallAsync(pluginId)
    SVC->>DB: Insert installation(state=Installing)
    SVC->>RT: StartAsync(manifest)
    RT->>RT: Ensure network/image, create+start container
    RT->>HP: GET /health until ready
    RT-->>SVC: RunningPlugin(host,port)
    SVC->>DB: Update installation(state=Running, endpoint)
    SVC-->>PLUG: PluginInstallation record
    PLUG-->>FE: Install success

    U->>FE: Click Call Greeting
    FE->>PROXY: GET /api/p/hello-plugin/greeting (Bearer JWT)
    PROXY->>SVC: ResolveAsync(pluginId)
    SVC->>DB: Read installed plugin endpoint/state
    PROXY->>HP: Forward GET /greeting + X-User-Id + X-User-Email
    HP-->>PROXY: { message: "Hello, <email>!" }
    PROXY-->>FE: Proxied response
    FE-->>U: Greeting displayed
```

## 3) Key Notes

- There is one backend base API (`http://localhost:8080`), not one per plugin.
- Plugin-specific routing happens via path segments (`/api/plugins/{pluginId}` and `/api/p/{pluginId}`).
- The proxy keeps plugins private behind the core API and forwards user identity headers to the plugin.
- If the plugin container is not running or endpoint is stale, greeting calls fail during proxy forwarding.