# Drydock — Iteration Guide

*Last updated: 2026-06-12*

How work moves through Drydock. Keep the phases explicit.

1. **Plan** — capture the slice in `engineering/planning/planning.md`; open or extend the active version doc in `versions/`.
2. **Implement** — code under `codebase/`; follow `development/`.
3. **Verify** — build + run; smoke-test end-to-end (e.g. register → 201, duplicate host → 409, invalid → 400).
4. **Consolidate** — fold durable design into `architecture/architecture.md`; update `.claude/rules/file-references.md` if docs moved.
5. **Close** — tick the version doc's iteration tasks; update planning status. Deferred work / known issues → the `planning.md` backlog, **not** the version doc.

## Conventions

- First-ship principle: **one vertical slice, not horizontal layers** — get one product deployed to a real box on a real domain, then widen.
- Version docs follow `wow-two-ws/conventions/planning/version-planning/version-docs.md` — abstract capability tasks, not implementation detail.
- Migrations: one EF migration per schema change, forward-only.
