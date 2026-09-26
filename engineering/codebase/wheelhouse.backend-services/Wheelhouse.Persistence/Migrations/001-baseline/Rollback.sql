-- 001-baseline rollback — drop all tables (no FK constraints, so order is free). Dev/test recovery only.

DROP TABLE IF EXISTS deployments;
DROP TABLE IF EXISTS domains;
DROP TABLE IF EXISTS secrets;
DROP TABLE IF EXISTS products;
DROP TABLE IF EXISTS servers;
