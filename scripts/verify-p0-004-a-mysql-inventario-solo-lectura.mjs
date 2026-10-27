import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(fileURLToPath(new URL('..', import.meta.url)));
const path = resolve(root, 'backend/scripts/p0-004-a-mysql-inventario-solo-lectura.sql');
const requiredSources = [
  'INFORMATION_SCHEMA.TABLES',
  'INFORMATION_SCHEMA.COLUMNS',
  'INFORMATION_SCHEMA.STATISTICS',
  'INFORMATION_SCHEMA.TABLE_CONSTRAINTS',
  'INFORMATION_SCHEMA.KEY_COLUMN_USAGE',
  'INFORMATION_SCHEMA.TRIGGERS',
  '__EFMigrationsHistory',
];

function statements(sql) {
  // This contract deliberately does not support quoted semicolons, comments
  // inside statements, client directives, or dynamic SQL.
  const uncommented = sql.replace(/^\s*--[^\n]*(?:\n|$)/gm, '\n').trim();
  return uncommented.split(';').map((x) => x.trim()).filter(Boolean);
}

function validateStatement(query) {
  // Inline MySQL -- comments can conceal the required schema predicate while
  // leaving its text visible to a regex. Only full-line header comments are supported.
  if (/--/.test(query)) throw new Error('Inline SQL comments are not supported');
  // The fixed metadata inventory allows only Q08's literal table name.
  // A quoted SELECT value can otherwise spoof WHERE ... = DATABASE() while
  // leaving the actual SELECT unrestricted across schemas.
  const withoutHistoryLiteral = query.replaceAll("'__EFMigrationsHistory'", '');
  if (/['"`]/.test(withoutHistoryLiteral)) {
    throw new Error('Unapproved SQL literal or quoted identifier in metadata query');
  }
  if (!/^SELECT\s/i.test(query)) {
    throw new Error('Only SELECT statements are authorized');
  }
  if (/\b(?:INTO\s+(?:OUTFILE|DUMPFILE)|FOR\s+UPDATE|LOCK\s+IN\s+SHARE\s+MODE|INSERT|UPDATE|DELETE|REPLACE|MERGE|CREATE|ALTER|DROP|TRUNCATE|GRANT|REVOKE|CALL|EXECUTE|LOAD|HANDLER|PREPARE|DEALLOCATE|SET)\b/i.test(query)) {
    throw new Error('Mutating/locking/export SQL construct rejected');
  }
  // A SELECT-only prefix does not by itself prevent filesystem reads,
  // session locks, delays, variable writes, or nested statements.
  if (/\bINTO\b|@/.test(query)) {
    throw new Error('Variable/INTO operations are not metadata-only reads');
  }
  if ((query.match(/\bSELECT\b/gi) || []).length !== 1) {
    throw new Error('Nested or multiple SELECT expressions are not supported');
  }
  const allowedFunctions = new Set(['VERSION', 'DATABASE']);
  for (const call of query.matchAll(/\b([A-Za-z_][A-Za-z0-9_]*)\s*\(/g)) {
    if (!allowedFunctions.has(call[1].toUpperCase())) {
      throw new Error('Unapproved SQL function in metadata query');
    }
  }
  // Reject uninspected extra tables introduced with comma or STRAIGHT_JOIN.
  // The source allowlist below otherwise sees only the first FROM table.
  const fromClause = query.match(/\bFROM\s+([\s\S]*?)(?=\bWHERE\b|\bGROUP\s+BY\b|\bORDER\s+BY\b|\bLIMIT\b|$)/i)?.[1] ?? '';
  if (/,|\bSTRAIGHT_JOIN\b/i.test(fromClause)) {
    throw new Error('Comma/STRAIGHT_JOIN table sources are not metadata-only');
  }
  const sources = [...query.matchAll(/\b(?:FROM|JOIN)\s+([`A-Za-z0-9_.]+)/gi)].map((m) => m[1].replaceAll('`', '').toUpperCase());
  // Permit only metadata tables required by the read-only metadata contract.
  // A broad INFORMATION_SCHEMA prefix would also accept ACL/plugin metadata.
  const allowedSources = new Set(requiredSources.map((source) => source.toUpperCase()));
  if (sources.some((source) => !allowedSources.has(source))) {
    throw new Error('Reading an unapproved SQL metadata source is prohibited');
  }
}

function verify(sql) {
  if (/\/\*|\*\/|#[^\n]*|\\g\b/i.test(sql)) throw new Error('Unsupported SQL syntax or client command');
  const queries = statements(sql);
  if (queries.length !== 9) throw new Error(`Expected nine read-only queries, got ${queries.length}`);
  // Bind each of the nine fixed queries to its intended source. Merely
  // mentioning every source somewhere in the file is insufficient.
  const expectedSources = [
    [],
    ['INFORMATION_SCHEMA.TABLES'],
    ['INFORMATION_SCHEMA.COLUMNS'],
    ['INFORMATION_SCHEMA.STATISTICS'],
    ['INFORMATION_SCHEMA.TABLE_CONSTRAINTS'],
    ['INFORMATION_SCHEMA.KEY_COLUMN_USAGE'],
    ['INFORMATION_SCHEMA.TRIGGERS'],
    ['INFORMATION_SCHEMA.TABLES'],
    ['__EFMIGRATIONSHISTORY'],
  ];
  for (const [index, query] of queries.entries()) {
    validateStatement(query);
    const actual = [...query.matchAll(/\b(?:FROM|JOIN)\s+([`A-Za-z0-9_.]+)/gi)]
      .map((match) => match[1].replaceAll('`', '').toUpperCase());
    assert.deepEqual(actual, expectedSources[index], `Unexpected source(s) in Q${index + 1}`);
    if (index >= 1 && index <= 7) {
      const schemaColumn = index === 6 ? 'TRIGGER_SCHEMA' : 'TABLE_SCHEMA';
      const guard = new RegExp(
        `\\bWHERE\\s+${schemaColumn}\\s*=\\s*DATABASE\\(\\)(?=\\s*(?:AND\\b|ORDER\\s+BY\\b|$))`, 'i'
      );
      if (!guard.test(query) || /\b(?:OR|XOR|UNION|EXCEPT|INTERSECT)\b|\|/i.test(query)) {
        throw new Error('Cross-schema metadata reads are not authorized');
      }
    }
    if (index === 7 && !/\bAND\s+TABLE_NAME\s*=\s*'__EFMigrationsHistory'/i.test(query)) {
      throw new Error('Q08 must verify the EF migrations history table');
    }
  }
  const normalized = queries.join('\n').toUpperCase();
  for (const source of requiredSources) {
    if (!normalized.includes(source.toUpperCase())) throw new Error(`Missing required source: ${source}`);
  }
  if (!normalized.includes('VERSION()') || !normalized.includes('DATABASE()')) {
    throw new Error('Server version and effective schema must be recorded');
  }
  if (!/FROM\s+__EFMIGRATIONSHISTORY\s+ORDER\s+BY\s+MIGRATIONID/i.test(normalized)) {
    throw new Error('Explicit ordered EF migrations history read required');
  }
  return queries.length;
}

const sql = readFileSync(path, 'utf8');
assert.equal(verify(sql), 9);

if (process.argv.includes('--self-test')) {
  const negativeStatements = [
    'DELETE FROM INFORMATION_SCHEMA.TABLES',
    'UPDATE INFORMATION_SCHEMA.TABLES SET TABLE_NAME = 1',
    'CREATE TABLE INFORMATION_SCHEMA.FAKE (id INT)',
    'SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES FOR UPDATE',
    "SELECT 'x' INTO OUTFILE '/tmp/leak'",
    'SELECT id FROM empresas',
    'SELECT a.TABLE_NAME FROM INFORMATION_SCHEMA.TABLES a JOIN clientes c ON 1=1',
    'SELECT a.TABLE_NAME FROM INFORMATION_SCHEMA.TABLES a, clientes c',
    'SELECT a.TABLE_NAME FROM INFORMATION_SCHEMA.TABLES a STRAIGHT_JOIN clientes c',
    'SELECT GRANTEE FROM INFORMATION_SCHEMA.USER_PRIVILEGES',
    'SELECT PLUGIN_NAME FROM INFORMATION_SCHEMA.PLUGINS',
    'SET @x = 1',
    'CALL sp_mutation()',
    'SELECT 1; DROP TABLE `__EFMigrationsHistory`',
    "SELECT LOAD_FILE('/etc/passwd')",
    "SELECT GET_LOCK('metadata-lock', 5)",
    'SELECT SLEEP(60)',
    'SELECT @@hostname',
    'SELECT VERSION() INTO @probe',
    'SELECT (SELECT id FROM empresas)',
    'SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES -- WHERE TABLE_SCHEMA = DATABASE()',
    "SELECT 'WHERE TABLE_SCHEMA = DATABASE() ORDER BY' FROM INFORMATION_SCHEMA.TABLES",
    'SELECT "WHERE TRIGGER_SCHEMA = DATABASE() ORDER BY" FROM INFORMATION_SCHEMA.TRIGGERS',
  ];
  for (const bad of negativeStatements) {
    assert.throws(() => validateStatement(bad), Error, `Expected rejected query: ${bad}`);
  }
  const negativeDocuments = [
    // The apparent schema predicate is inside a selected string, not in SQL WHERE.
    sql.replace('SELECT TABLE_NAME, TABLE_TYPE, ENGINE\\nFROM INFORMATION_SCHEMA.TABLES\\nWHERE TABLE_SCHEMA = DATABASE()\\nORDER BY TABLE_NAME;',
      "SELECT 'WHERE TABLE_SCHEMA = DATABASE() ORDER BY' AS fake_guard, TABLE_NAME, TABLE_TYPE, ENGINE\\nFROM INFORMATION_SCHEMA.TABLES\\nORDER BY TABLE_NAME;"),
    sql.replace('SELECT TRIGGER_NAME, EVENT_OBJECT_TABLE, ACTION_TIMING, EVENT_MANIPULATION\\nFROM INFORMATION_SCHEMA.TRIGGERS\\nWHERE TRIGGER_SCHEMA = DATABASE()\\nORDER BY EVENT_OBJECT_TABLE, TRIGGER_NAME;',
      "SELECT 'WHERE TRIGGER_SCHEMA = DATABASE() ORDER BY' AS fake_guard, TRIGGER_NAME, EVENT_OBJECT_TABLE, ACTION_TIMING, EVENT_MANIPULATION\\nFROM INFORMATION_SCHEMA.TRIGGERS\\nORDER BY EVENT_OBJECT_TABLE, TRIGGER_NAME;"),
    sql.replace('SELECT VERSION()', 'DELETE VERSION()'),
    sql.replace('FROM __EFMigrationsHistory', 'FROM usuarios'),
    sql.replace('FROM INFORMATION_SCHEMA.TABLES\nWHERE', 'FROM INFORMATION_SCHEMA.TABLES, clientes\nWHERE'),
    sql.replace('INFORMATION_SCHEMA.TRIGGERS', 'information_schema.FAKE'),
    sql.replace('FROM INFORMATION_SCHEMA.TRIGGERS\nWHERE',
      'FROM INFORMATION_SCHEMA.TRIGGERS JOIN INFORMATION_SCHEMA.USER_PRIVILEGES p ON 1=1\nWHERE'),
    sql.replace('ORDER BY MigrationId;', 'ORDER BY MigrationId; INSERT INTO t VALUES(1);'),
    // An otherwise SELECT-only script must not list other databases metadata.
    // Guard text inside a MySQL inline comment must not count as a schema filter.
    sql.replace('FROM INFORMATION_SCHEMA.TABLES\nWHERE TABLE_SCHEMA = DATABASE()\nORDER BY TABLE_NAME;',
      'FROM INFORMATION_SCHEMA.TABLES -- WHERE TABLE_SCHEMA = DATABASE()\nORDER BY TABLE_NAME;'),
    sql.replace('FROM INFORMATION_SCHEMA.TRIGGERS\nWHERE TRIGGER_SCHEMA = DATABASE()',
      'FROM INFORMATION_SCHEMA.TRIGGERS -- WHERE TRIGGER_SCHEMA = DATABASE()'),
    sql.replace('WHERE TABLE_SCHEMA = DATABASE()', 'WHERE 1=1'),
    sql.replace('WHERE TABLE_SCHEMA = DATABASE()', 'WHERE TABLE_SCHEMA = DATABASE() OR 1=1'),
    sql.replace('WHERE TABLE_SCHEMA = DATABASE()', 'WHERE TABLE_SCHEMA = DATABASE() AND 1=1 or 1=1'),
    sql.replace('WHERE TRIGGER_SCHEMA = DATABASE()', 'WHERE 1=1'),
    sql.replace("AND TABLE_NAME = '__EFMigrationsHistory'", 'AND TABLE_NAME IS NOT NULL'),
    sql.replace('FROM INFORMATION_SCHEMA.COLUMNS', 'FROM INFORMATION_SCHEMA.TABLES'),
  ];
  for (const altered of negativeDocuments) assert.throws(() => verify(altered), Error);
  console.log(`P0-004.A static-only PASS: 9 SELECT queries; ${negativeStatements.length + negativeDocuments.length} negative regressions`);
} else {
  console.log('P0-004.A static-only PASS: nine SELECT queries; no live MySQL access attempted');
}
