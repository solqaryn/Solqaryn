import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';

const mysql = (...sql) => execFileSync('mysql',
  ['--protocol=TCP', '-h', '127.0.0.1', '-P', '3306', '-uroot', '-N', '-B', '-D', 'solqaryn_c0601_m0503', '-e', sql.join('\n')],
  { encoding: 'utf8', timeout: 25_000, env: process.env }
).trim();

const version = mysql('SELECT VERSION();');
assert.match(version, /^8\.4\./, 'Actual Oracle MySQL 8.4 service required');
console.log('C0601_PROVIDER_MYSQL_VERSION=' + version);

const schemaTables = Number(mysql(
  "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE();"
));
assert.ok(Number.isInteger(schemaTables) && schemaTables >= 139,
  'Actual EF10 schema bootstrap (>=139 tables) must exist');
console.log('C0601_PROVIDER_EF_BOOTSTRAPPED_TABLES=' + schemaTables);

const probeId = 906010503;
const before = Number(mysql('SELECT COUNT(*) FROM Empresas WHERE Id = ' + probeId + ';'));
assert.equal(before, 0, 'Disposable provider fixture must begin empty');
const roundtrip = mysql(
  'START TRANSACTION;',
  'INSERT INTO Empresas (Id, Nombre, Activa, FechaCreacion, FechaActualizacion) ' +
    "VALUES (" + probeId + ", 'C0601 Test Provider Transaction', TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));",
  'SELECT COUNT(*) FROM Empresas WHERE Id = ' + probeId + ';',
  'ROLLBACK;',
  'SELECT COUNT(*) FROM Empresas WHERE Id = ' + probeId + ';'
).split(/\r?\n/);
assert.deepEqual(roundtrip, ['1', '0'],
  'Actual Empresa insert must be visible inside transaction and absent after ROLLBACK');
assert.equal(Number(mysql('SELECT COUNT(*) FROM Empresas WHERE Id = ' + probeId + ';')), 0,
  'Database probe must not persist across connections');
console.log('C0601_PROVIDER_MYSQL_REAL_TRANSACTION_ROLLBACK=PASS');

for (const path of ['/health', '/health/ready']) {
  const response = await fetch('http://127.0.0.1:5005' + path,
    { method: 'GET', redirect: 'error', credentials: 'omit', cache: 'no-store',
      headers: { Accept: 'application/json' }, signal: AbortSignal.timeout(12_000) });
  const body = await response.text();
  assert.equal(response.status, 200, 'Actual ASP.NET /health readiness must succeed: ' + path);
  assert.ok(!/Password=|Jwt__Secret|MYSQL_ROOT_PASSWORD|ConnectionStrings__/i.test(body),
    'Public health responses must never expose connection strings/secrets');
  console.log('C0601_PROVIDER_ASPNET_PUBLIC_' + path.replaceAll('/', '_') + '=200_NO_SENSITIVE_BODY');
}
console.log('C0601_M0503_REAL_ORACLE_EF_MYSQL84_API_PROVIDER_INTEGRATION=PASS');
