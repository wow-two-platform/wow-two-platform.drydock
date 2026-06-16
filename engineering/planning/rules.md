# Drydock — Rules

*Last updated: 2026-06-12*

Operational conventions specific to Drydock. Shared code style lives in `wow-two-ws/conventions/`.

- **Secrets handling:** Drydock holds the highest-value secret set in the portfolio (VPS SSH keys, registrar billing API, Cloudflare token, GHCR PAT). Encrypt at rest; never commit, bake into an image, or log a secret value. Use scoped tokens (Cloudflare per-zone, GitHub GHCR-read). Keep an audit trail on every deploy / secret change / domain purchase.
- **Data handling:** SQLite store; EF migrations applied on startup, forward-only. Never ship a real `*.db` into an image layer (see `codebase/.dockerignore`).
- **Deployment:** one instance; bind to loopback and reach over a private mesh (Tailscale / SSH tunnel) — never expose publicly.
- **Outbound exec:** remote operations go through SSH.NET (transparent + loggable) and scoped REST clients in `Infrastructure` — never ad-hoc shell-outs from the host.
- **Versioning:** product progress is tracked as version docs under `versions/v{X.Y}/`.
- **Ports:** HTTPS 8210 / HTTP 8211 (dev) — even HTTPS + odd HTTP adjacent.
