-- Reverse 003-audit-updated-at — drop the audit "last change" column from the three IAuditable tables.
-- created_at_utc is untouched (it predates this migration). Dev/test recovery only.

ALTER TABLE products    DROP COLUMN IF EXISTS updated_at_utc;
ALTER TABLE servers     DROP COLUMN IF EXISTS updated_at_utc;
ALTER TABLE deployments DROP COLUMN IF EXISTS updated_at_utc;
