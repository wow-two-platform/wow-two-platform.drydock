-- Reverse 004-audit-trail — drop the audit trail with its append-only trigger. Dev/test recovery only:
-- this deletes the record of every operator action.

DROP TRIGGER IF EXISTS audit_entries_append_only ON audit_entries;
DROP FUNCTION IF EXISTS audit_entries_append_only();
DROP TABLE IF EXISTS audit_entries;
