# Drydock — Features

*Last updated: 2026-06-12*

Per-feature specs. One doc per non-trivial feature; this is the index. Tier: **must** = v1 core ask · **should** / **later** = portfolio depth.

| Feature | Tier | What it does |
|---|---|---|
| Deploy front+back to VPS + rollback | must | SSH → render compose → `docker compose pull && up -d`; rollback = re-pin prior image SHA |
| Domain search / buy / assign | must | Porkbun buy + Cloudflare A-record → assign to product/env → Traefik route + SSL |
| Secrets vault + env injection | must | Encrypted-at-rest secrets, scoped, injected at deploy — unblocks deploy + domains |
| Live deploy/build logs (SignalR) | must | Stream stdout to the console widget — trust the box did what you clicked |
| Servers registry | done | Register + list VPSs (the wired v1 vertical) |
| Health/uptime + Telegram alerts | should | Calm ops — a dead box pings you |
| Domain + SSL expiry alerts | should | A dead domain = a dead product |
| Per-product managed datastores + backups | should | Every SaaS needs a DB + backups |
| Cost per product (domain + VPS share) | should | Feeds kill-gates + the financial domain |
| 0→live scaffold | should | Template repo → GitHub API → CI → domain → first deploy — the 50–100-launch accelerator |
| Teardown / kill | should | Stop stack, final backup, archive repo, release domain — executes kill-gates cleanly |
| Capacity / placement view | later | Bin-pack products across servers |
| VPS provisioning (Hetzner Cloud + cloud-init) | later | One-click new server, Docker+Traefik on first boot |

> Build detail + flows: `../../engineering/architecture/architecture.md`. Flows: `../flows/flows.md`.
