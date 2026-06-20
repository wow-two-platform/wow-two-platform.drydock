-- ============================================================
-- 001-baseline — Drydock control-plane schema (PostgreSQL).
-- Tables: servers, products, deployments, domains, secrets.
-- Mirrors the EF Core model: snake_case columns, enums-as-text, timestamptz, uuid, bytea.
-- EF is a pure mapper over this hand-authored schema (schema-first). No inter-table FKs.
-- ============================================================

CREATE TABLE servers (
    id                  uuid        NOT NULL,
    name                text        NOT NULL,
    host                text        NOT NULL,
    ssh_port            integer     NOT NULL,
    ssh_user            text        NOT NULL,
    ssh_key_secret_id   uuid        NULL,
    hetzner_server_id   text        NULL,
    region              text        NULL,
    status              text        NOT NULL,
    created_at_utc      timestamptz NOT NULL,
    last_checked_at_utc timestamptz NULL,
    CONSTRAINT pk_servers PRIMARY KEY (id)
);

CREATE UNIQUE INDEX ix_servers_host ON servers (host);

CREATE TABLE products (
    id             uuid        NOT NULL,
    slug           text        NOT NULL,
    name           text        NOT NULL,
    repo           text        NOT NULL,
    status         text        NOT NULL,
    created_at_utc timestamptz NOT NULL,
    CONSTRAINT pk_products PRIMARY KEY (id)
);

CREATE UNIQUE INDEX ix_products_slug ON products (slug);

CREATE TABLE deployments (
    id               uuid        NOT NULL,
    product_id       uuid        NOT NULL,
    server_id        uuid        NOT NULL,
    environment      text        NOT NULL,
    image_web_tag    text        NULL,
    image_api_tag    text        NULL,
    status           text        NOT NULL,
    log              text        NULL,
    triggered_by     text        NULL,
    created_at_utc   timestamptz NOT NULL,
    completed_at_utc timestamptz NULL,
    CONSTRAINT pk_deployments PRIMARY KEY (id)
);

CREATE INDEX ix_deployments_product_id_created_at_utc ON deployments (product_id, created_at_utc);

CREATE TABLE domains (
    id                  uuid        NOT NULL,
    name                text        NOT NULL,
    registrar           text        NULL,
    dns_provider        text        NULL,
    assigned_product_id uuid        NULL,
    status              text        NOT NULL,
    purchased_at_utc    timestamptz NULL,
    expires_at_utc      timestamptz NULL,
    auto_renew          boolean     NOT NULL,
    CONSTRAINT pk_domains PRIMARY KEY (id)
);

CREATE UNIQUE INDEX ix_domains_name ON domains (name);

CREATE TABLE secrets (
    id             uuid        NOT NULL,
    scope          text        NOT NULL,
    ref_id         uuid        NULL,
    key            text        NOT NULL,
    cipher_text    bytea       NOT NULL,
    nonce          bytea       NOT NULL,
    tag            bytea       NOT NULL,
    updated_at_utc timestamptz NOT NULL,
    CONSTRAINT pk_secrets PRIMARY KEY (id)
);

CREATE UNIQUE INDEX ix_secrets_scope_ref_id_key ON secrets (scope, ref_id, key);
