-- ============================================================
-- 006-product-catalog — product identity moves to the code-owned catalog (runner catalog.py).
-- product_metadata keeps what the operator records per catalog product, keyed by slug: its lifecycle.
-- integration_keys keeps the keys other programs present: the secret's SHA-256 and a display prefix, never the secret.
-- The legacy products registry stays in place, unread; each registered product's status seeds its lifecycle.
-- Idempotent. EF is a pure mapper over this hand-authored schema (schema-first) — see persistence/database.md.
-- ============================================================

CREATE TABLE IF NOT EXISTS product_metadata (
    slug           text        PRIMARY KEY,
    lifecycle      text        NOT NULL,
    created_at_utc timestamptz NOT NULL,
    updated_at_utc timestamptz NOT NULL
);

INSERT INTO product_metadata (slug, lifecycle, created_at_utc, updated_at_utc)
SELECT slug,
       CASE status WHEN 'active' THEN 'live' WHEN 'paused' THEN 'paused' WHEN 'killed' THEN 'killed' ELSE 'building' END,
       created_at_utc,
       updated_at_utc
FROM products
ON CONFLICT (slug) DO NOTHING;

CREATE TABLE IF NOT EXISTS integration_keys (
    id               uuid        PRIMARY KEY,
    name             text        NOT NULL,
    prefix           text        NOT NULL,
    hash             text        NOT NULL,
    scopes           text        NOT NULL,
    created_by       text        NOT NULL,
    last_used_at_utc timestamptz NULL,
    revoked_at_utc   timestamptz NULL,
    created_at_utc   timestamptz NOT NULL,
    updated_at_utc   timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_integration_keys_hash ON integration_keys (hash);
