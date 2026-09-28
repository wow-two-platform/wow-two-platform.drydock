-- ============================================================
-- 004-audit-trail — the append-only, hash-chained record of operator actions.
-- Every row chains to the one before it (sequence, previous_hash, hash), so an edited, reordered or removed middle
-- row fails verification. Rows hold operator-safe text only: never a secret value, token or setting.
-- The trigger refuses UPDATE and DELETE; TRUNCATE stays possible for disposable databases. Idempotent.
-- EF is a pure mapper over this hand-authored schema (schema-first) — see persistence/database.md.
-- ============================================================

CREATE TABLE IF NOT EXISTS audit_entries (
    id              uuid        PRIMARY KEY,
    sequence        bigint      NOT NULL,
    previous_hash   bytea       NOT NULL,
    hash            bytea       NOT NULL,
    occurred_at_utc timestamptz NOT NULL,
    actor           text        NOT NULL,
    action          text        NOT NULL,
    subject         text        NOT NULL,
    outcome         text        NOT NULL,
    detail          text        NULL,
    reason          text        NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_audit_entries_sequence ON audit_entries (sequence);
CREATE INDEX IF NOT EXISTS ix_audit_entries_occurred_at_utc ON audit_entries (occurred_at_utc);

CREATE OR REPLACE FUNCTION audit_entries_append_only() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'audit_entries is append-only';
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER audit_entries_append_only
    BEFORE UPDATE OR DELETE ON audit_entries
    FOR EACH ROW EXECUTE FUNCTION audit_entries_append_only();
