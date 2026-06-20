-- ============================================================
-- 002-snake-enums — migrate persisted enum values PascalCase → snake_case.
-- Aligns stored values with the SDK EnumCaseConverter (Postgres-native casing); columns stay text.
-- Generic PascalCase→snake: insert '_' at lower/digit→Upper boundaries, then lowercase.
--   Draft → draft · RolledBack → rolled_back · Unreachable → unreachable
-- Idempotent on already-snake values (no boundary to match → just lowercased).
-- ============================================================

UPDATE servers     SET status = lower(regexp_replace(status, '([a-z0-9])([A-Z])', '\1_\2', 'g'));
UPDATE products    SET status = lower(regexp_replace(status, '([a-z0-9])([A-Z])', '\1_\2', 'g'));
UPDATE deployments SET status = lower(regexp_replace(status, '([a-z0-9])([A-Z])', '\1_\2', 'g'));
UPDATE domains     SET status = lower(regexp_replace(status, '([a-z0-9])([A-Z])', '\1_\2', 'g'));
UPDATE secrets     SET scope  = lower(regexp_replace(scope,  '([a-z0-9])([A-Z])', '\1_\2', 'g'));
