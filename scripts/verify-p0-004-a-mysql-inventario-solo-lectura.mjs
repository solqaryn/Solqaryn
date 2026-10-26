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
  if (!/^SELECT\s/i.test(query)) {
    throw new Error('Only SELECT statements are authorized');
  }
  if (/\b(?:INTO\s+(?:OUTFILE|DUMPFILE)|FOR\s+UPDATE|LOCK\s+IN\s+SHARE\s+MODE|INSERT|UPDATE|DELETE|REPLACE|MERGE|CREATE|ALTER|DROP|TRUNCATE|GRANT|REVOKE|CALL|EXECUTE|LOAD|HANDLER|PREPARE|DEALLOCATE|SET)\b/i.test(query)) {
    throw new Error('Mutating/locking/export SQL construct rejected');
  }
  const sources = [...query.matchAll(/\b(?:FROM|JOIN)\s+([`A-Za-z0-9_.]+)/gi)].map((m) => m[1].replaceAll('`', '').toUpperCase());
  if (sources.some((source) => !source.startsWith('INFORMATION_SCHEMA.') && source !== '__EFMIGRATIONSHISTORY')) {
    throw new Error('Reading business tables or another schema is prohibited');
  }
}

function verify(sql) {
  if (/\/\*|\*\/|#[^\n]*|\\g\b/i.test(sql)) throw new Error('Unsupported SQL syntax or client command');
  const queries = statements(sql);
  if (queries.length !== 9) throw new Error(`Expected nine read-only queries, got ${queries.length}`);
  for (const query of queries) validateStatement(query);
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
    'SET @x = 1',
    'CALL sp_mutation()',
    'SELECT 1; DROP TABLE `__EFMigrationsHistory`',
  ];
  for (const bad of negativeStatements) {
    assert.throws(() => validateStatement(bad), Error, `Expected rejected query: ${bad}`);
  }
  const negativeDocuments = [
    sql.replace('SELECT VERSION()', 'DELETE VERSION()'),
    sql.replace('FROM __EFMigrationsHistory', 'FROM usuarios'),
    sql.replace('INFORMATION_SCHEMA.TRIGGERS', 'information_schema.FAKE'),
    sql.replace('ORDER BY MigrationId;', 'ORDER BY MigrationId; INSERT INTO t VALUES(1);'),
  ];
  for (const altered of negativeDocuments) assert.throws(() => verify(altered), Error);
  console.log(`P0-004.A static-only PASS: 9 SELECT queries; ${negativeStatements.length + negativeDocuments.length} negative regressions`);
} else {
  console.log('P0-004.A static-only PASS: nine SELECT queries; no live MySQL access attempted');
}
