-- ============================================================
-- 003-audit-updated-at — add the audit "last change" column the SDK audit interceptor stamps.
-- Tables: products, servers, deployments (the three IAuditable entities).
-- The existing created_at_utc already backs IAuditable.CreatedAt (mapped, no rename); this adds the
-- companion updated_at_utc that IAuditable.UpdatedAt requires so the dormant interceptor actually persists.
-- Backfills existing rows to created_at_utc (a row never modified shares its creation instant), then sets NOT NULL
-- to match the non-nullable EF model. timestamptz (mirrors the other audit columns). Idempotent via IF NOT EXISTS.
-- EF is a pure mapper over this hand-authored schema (schema-first) — see persistence/database.md.
-- ============================================================

ALTER TABLE products    ADD COLUMN IF NOT EXISTS updated_at_utc timestamptz NULL;
ALTER TABLE servers     ADD COLUMN IF NOT EXISTS updated_at_utc timestamptz NULL;
ALTER TABLE deployments ADD COLUMN IF NOT EXISTS updated_at_utc timestamptz NULL;

UPDATE products    SET updated_at_utc = created_at_utc WHERE updated_at_utc IS NULL;
UPDATE servers     SET updated_at_utc = created_at_utc WHERE updated_at_utc IS NULL;
UPDATE deployments SET updated_at_utc = created_at_utc WHERE updated_at_utc IS NULL;

ALTER TABLE products    ALTER COLUMN updated_at_utc SET NOT NULL;
ALTER TABLE servers     ALTER COLUMN updated_at_utc SET NOT NULL;
ALTER TABLE deployments ALTER COLUMN updated_at_utc SET NOT NULL;
