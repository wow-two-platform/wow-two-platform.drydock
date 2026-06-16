# deployment/

Ops + deploy for Drydock.

- `Dockerfile` — multi-stage: build the SPA → publish the .NET API with the SPA in `wwwroot` → slim runtime image. Build context is `engineering/codebase/`.
- `docker-compose.yml` — one `drydock` service (context `../codebase`, dockerfile `../deployment/Dockerfile`).

```bash
# from engineering/deployment/
docker compose up -d --build
```

> **Single-host pattern:** the SPA is built and copied into the API's `wwwroot`; the .NET host serves both the API and the dashboard on one origin.
>
> **Never expose publicly.** Bind to loopback (`127.0.0.1:8210`) and reach over Tailscale / SSH tunnel. SQLite (`drydock.db`) lives on a named volume so the control-plane state survives container replacement.

> **Note — this is Drydock's *own* container** (the control plane running locally). The *product* deploy substrate Drydock manages on the fleet (Docker + Traefik per Hetzner VPS, GHCR images) is a runtime concern of the deploy executor, not this folder. See `../architecture/architecture.md` → Deploy flow.
