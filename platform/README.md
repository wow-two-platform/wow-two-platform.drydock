# platform/

The deployable Drydock platform — `.NET 10` API + `React` dashboard, shipped as one host
(the API serves the built SPA from `wwwroot`).

## Dev

```bash
# Terminal 1 — API (https 8210 / http 8211)
cd src/drydock.backend && dotnet run --project Drydock.Api --launch-profile https

# Terminal 2 — dashboard (5174, proxies /api → 8211)
cd src/drydock.frontend && npm install && npm run dev
```

## Single-host (API serves the SPA)

```bash
cd src/drydock.frontend && npm run deploy        # vite build → copy into Drydock.Api/wwwroot
cd ../drydock.backend && dotnet run --project Drydock.Api
```

## Container

```bash
docker compose up -d --build       # API + SPA, loopback :8210, SQLite on a volume
```

Ports follow the wow-two rule: HTTPS even (8210), HTTP odd (8211).
