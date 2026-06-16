# Drydock — Product

*Last updated: 2026-06-12*

> Durable truths. Update when the model changes, not on every task.

## What it is

The internal product ops & deploy control plane for the micro-SaaS portfolio — *a personal Vercel/Heroku for the whole fleet.* One dashboard + .NET API that holds every product, every VPS, every domain, and every secret, and turns "ship this product to that server on this domain" into a few clicks. It deploys product front+back to Hetzner VPSs over SSH (Docker + Traefik), buys + wires domains (Porkbun + Cloudflare), holds secrets, and watches the fleet stay alive.

## Who it's for

The single operator of a growing micro-SaaS portfolio (target 50–100 launches by EOY 2026) — replacing the pile of manual SSH + registrar tabs + scattered `.env` files those products would otherwise need.

## Model

Internal infrastructure, **not a sold product**. A `wow-two-platform` service that manages the *other* products, so it sits above them operationally. Value = collapsing deploy + domains + secrets + monitoring for the whole fleet into one control plane, and serving as a flagship internal .NET build.

## Name

**Drydock** (chosen 2026-06-09) — a ship is built, serviced, launched, and repaired from drydock; the portfolio is a fleet of small vessels. (Considered: Hangar, Marina, Mission Control, Shipyard, Launchpad.)

## Positioning

- **vs manual ops (SSH + registrar tabs + scattered `.env`):** one dashboard, one audited path; deploy/rollback/domain/secret in a few clicks instead of a checklist.
- **vs Vercel/Heroku/Coolify (buy):** bespoke on the wow-two stack (Docker + Traefik + SSH.NET + GHCR) for full control and learning. The registry + domain-buying + unified dashboard + cost/kill-gate layer is the value-add; deployment is the commodity underneath. Coolify is the fast-lane fallback substrate if the bespoke deploy thread stalls.

> Full design spec: `wow-two-ws/ideas/drydock-spec.md`.
