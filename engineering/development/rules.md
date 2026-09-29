# Wheelhouse — Rules

*Last updated: 2026-09-29*

Operational rules specific to Wheelhouse. Shared code style lives in `wow-two-ws/conventions/`.

- **Secrets:** Wheelhouse holds SSH identities, catalog tokens and vault credentials — the highest-value set in the portfolio.
  Mount them as protected files; never commit, bake into an image, log, or return a secret value.
- **Secret values:** the vault console is write-only — values go to the vault and never come back to the browser.
  A minted product token is shown once, then only its metadata remains.
- **Failure reasons:** surface only static rule text, setting key names, step names and exit codes; never raw command output.
- **Data:** PostgreSQL; schema owned by hand-authored SQL migrations applied on startup, forward-only.
- **Deployment:** one private instance; bind to loopback and reach it over a private mesh or SSH tunnel.
- **Outbound execution:** remote operations go through the Python runner over pinned OpenSSH, and HTTP calls only
  to code-owned endpoints — never ad-hoc shell commands or browser-supplied URLs.
- **Versioning:** the newest `engineering/planning/version-track/v{X.Y}/` folder sets the code version; CI assigns the patch.
- **Ports:** HTTPS 8210 / HTTP 8211 (dev) — even HTTPS, odd HTTP.
