import assert from 'node:assert/strict';
import { copyFileSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

// No live MySQL connections, application configuration, or migrations.
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const sourceScript = join(root, 'scripts/verify-p0-004-a-mysql-inventario-solo-lectura.mjs');
const sourceSql = join(root, 'backend/scripts/p0-004-a-mysql-inventario-solo-lectura.sql');
const original = readFileSync(sourceSql, 'utf8');
const scratch = mkdtempSync(join(tmpdir(), 'solqaryn-p0-004-b-'));
const testScript = join(scratch, 'scripts/verify-p0-004-a-mysql-inventario-solo-lectura.mjs');
const testSql = join(scratch, 'backend/scripts/p0-004-a-mysql-inventario-solo-lectura.sql');

function runValidator(selfTest = false) {
  return spawnSync(process.execPath, [testScript, ...(selfTest ? ['--self-test'] : [])], {
    cwd: scratch, encoding: 'utf8', timeout: 30000,
    env: { PATH: process.env.PATH ?? '' },
  });
}
function replaceRequired(find, replacement) {
  assert.ok(original.includes(find), 'Missing SQL contract fragment: ' + find);
  return original.replace(find, replacement);
}

try {
  mkdirSync(dirname(testScript), { recursive: true });
  mkdirSync(dirname(testSql), { recursive: true });
  copyFileSync(sourceScript, testScript);
  writeFileSync(testSql, original);

  const baseline = runValidator(true);
  assert.equal(baseline.status, 0, 'Original contract failed:\n' + baseline.stdout + '\n' + baseline.stderr);
  assert.match(baseline.stdout, /9 SELECT queries; 50 negative regressions/);

  const negatives = [
    ['DML update', original + '\nUPDATE Usuarios SET Activo = 0;'],
    ['DML delete', original + '\nDELETE FROM Usuarios;'],
    ['DDL migration', original + '\nALTER TABLE Usuarios ADD COLUMN unsafe INT;'],
    ['procedure invocation', original + '\nCALL DatabaseBootstrap();'],
    ['export', original + "\nSELECT TABLE_NAME INTO OUTFILE '/tmp/leak' FROM INFORMATION_SCHEMA.TABLES;"],
    ['cross-schema read', replaceRequired('WHERE TABLE_SCHEMA = DATABASE()', 'WHERE TABLE_SCHEMA IS NOT NULL')],
    ['business-row read', replaceRequired('FROM __EFMigrationsHistory', 'FROM Usuarios')],
    ['unrestricted projection', replaceRequired('SELECT TABLE_NAME, TABLE_TYPE, ENGINE', 'SELECT *')],
  ];
  for (const [name, changedSql] of negatives) {
    writeFileSync(testSql, changedSql);
    const result = runValidator();
    assert.notEqual(result.status, 0, 'Unsafe SQL fixture accepted: ' + name);
    assert.match(result.stderr, /AssertionError|Error|ERR_ASSERTION/, 'Missing rejection evidence: ' + name);
  }
  console.log('P0-004.B PASS: 9 SELECT and 50 original negative cases; ' +
    negatives.length + ' additional unsafe SQL fixtures rejected; no live database access');
} finally {
  rmSync(scratch, { recursive: true, force: true });
}
