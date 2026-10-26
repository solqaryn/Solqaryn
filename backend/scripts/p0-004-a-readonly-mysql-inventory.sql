-- P0-004.A / DEV ONLY / MANUAL READ-ONLY INVENTORY
-- Requires a separately authorized read-only MySQL account.
-- Do NOT execute via DatabaseBootstrap or EF migrations.
-- Do NOT copy output containing server names, user identities, or secrets into git.
-- Stop with NO_GO if DATABASE() is NULL, expected schema differs, or a query fails.

-- Q01: selected server version and effective database, without credentials.
SELECT VERSION() AS mysql_version, DATABASE() AS effective_schema;

-- Q02: physical tables / engine (no row data or approximate row counts).
SELECT TABLE_NAME, TABLE_TYPE, ENGINE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
ORDER BY TABLE_NAME;

-- Q03: physical columns (no defaults or sample business values).
SELECT TABLE_NAME, ORDINAL_POSITION, COLUMN_NAME, COLUMN_TYPE,
       IS_NULLABLE, EXTRA
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
ORDER BY TABLE_NAME, ORDINAL_POSITION;

-- Q04: effective indexes and columns.
SELECT TABLE_NAME, INDEX_NAME, NON_UNIQUE, SEQ_IN_INDEX,
       COLUMN_NAME, INDEX_TYPE
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;

-- Q05: primary, foreign, unique, and CHECK constraints.
SELECT TABLE_NAME, CONSTRAINT_NAME, CONSTRAINT_TYPE
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE TABLE_SCHEMA = DATABASE()
ORDER BY TABLE_NAME, CONSTRAINT_NAME;

-- Q06: constraint columns / referenced tables.
SELECT TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION, COLUMN_NAME,
       REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION;

-- Q07: triggers (metadata only; no trigger bodies or data rows).
SELECT TRIGGER_NAME, EVENT_OBJECT_TABLE, ACTION_TIMING, EVENT_MANIPULATION
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE TRIGGER_SCHEMA = DATABASE()
ORDER BY EVENT_OBJECT_TABLE, TRIGGER_NAME;

-- Q08: discover history table before running Q09.
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = '__EFMigrationsHistory'
ORDER BY TABLE_NAME;

-- Q09: execute ONLY if Q08 confirms the table exists in the expected schema.
-- If Q08 returns no row, record NO_GO; do not create/fix this table.
SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId;
