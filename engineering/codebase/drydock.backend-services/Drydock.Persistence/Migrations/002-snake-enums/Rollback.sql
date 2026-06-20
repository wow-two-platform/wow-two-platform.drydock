-- Reverse 002-snake-enums: snake_case → PascalCase (initcap each underscore-separated word, strip underscores).
--   draft → Draft · rolled_back → RolledBack
UPDATE servers     SET status = replace(initcap(replace(status, '_', ' ')), ' ', '');
UPDATE products    SET status = replace(initcap(replace(status, '_', ' ')), ' ', '');
UPDATE deployments SET status = replace(initcap(replace(status, '_', ' ')), ' ', '');
UPDATE domains     SET status = replace(initcap(replace(status, '_', ' ')), ' ', '');
UPDATE secrets     SET scope  = replace(initcap(replace(scope,  '_', ' ')), ' ', '');
