-- ============================================================
-- 005-vitals-samples — thirty days of host and container readings per target, for trends.
-- One row per target per sampling pass; figures only, never container labels, environment or logs.
-- The sampler deletes rows older than 30 days. Idempotent.
-- EF is a pure mapper over this hand-authored schema (schema-first) — see persistence/database.md.
-- ============================================================

CREATE TABLE IF NOT EXISTS vitals_samples (
    id                 uuid             PRIMARY KEY,
    target_id          text             NOT NULL,
    server_id          text             NOT NULL,
    sampled_at_utc     timestamptz      NOT NULL,
    readable           boolean          NOT NULL,
    load_percent       double precision NULL,
    memory_percent     double precision NULL,
    disk_percent       double precision NULL,
    containers         integer          NOT NULL,
    healthy_containers integer          NOT NULL,
    restarts           integer          NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_vitals_samples_target_id_sampled_at_utc ON vitals_samples (target_id, sampled_at_utc);
CREATE INDEX IF NOT EXISTS ix_vitals_samples_sampled_at_utc ON vitals_samples (sampled_at_utc);
