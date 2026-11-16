#!/usr/bin/env node
// C02-04.M05.03 - real HTTP/JWT/RBAC + ephemeral MySQL 8.4 fixture, never remote.
import assert from 'node:assert/strict';

const base = process.env.P4_API_URL || '';
assert.match(base, /^http:\/\/127\.0\.0\.1:\d+$/, 'solo API local en runner efimero');
const password = process.env.P4_TEST_PASSWORD;
assert.ok(password && password.length >= 12, 'solo credencial efimera desde entorno CI');

async function call(method, path, jwt = '', company = null, body = undefined) {
  const headers = {};
  if (jwt) headers.authorization = 'Bearer ' + jwt;
  if (company !== null) headers['X-Empresa-Id'] = String(company);
  if (body !== undefined) headers['content-type'] = 'application/json';
  const res = await fetch(base + path, {
    method, headers, body: body === undefined ? undefined : JSON.stringify(body),
    signal: AbortSignal.timeout(15000)
  });
  const raw = await res.text();
  let parsed;
  try { parsed = JSON.parse(raw); } catch { parsed = null; }
  return { status: res.status, cache: res.headers.get('cache-control') || '', raw, parsed };
}
const dataOf = reply => reply.parsed?.data ?? reply.parsed?.Data;
async function login(username) {
  const response = await call('POST', '/auth/login', '', null, { nombreUsuario: username, password });
  assert.equal(response.status, 200, 'login efimero debe autenticar ' + username);
  const token = dataOf(response)?.token ?? dataOf(response)?.Token;
  assert.ok(typeof token === 'string' && token.length > 30, 'JWT real obligatorio');
  return token;
}
function denied(reply, name) {
  assert.ok([401, 403].includes(reply.status), name + ' NO puede devolver autoridad: ' + reply.status);
  assert.ok(!reply.raw.includes('p4_platform_only_ci'), 'identidad global nunca se expone en error');
}
const [globalJwt, adminA, adminB] = await Promise.all([
  login('p4_platform_only_ci'), login('p4_admin_a'), login('p4_admin_b')
]);
const [global, noToken, fromA, fromB, spoofA, badPage, badSize, boundary] = await Promise.all([
  call('GET', '/plataforma/empresas?pagina=1&tamano=25', globalJwt),
  call('GET', '/plataforma/empresas?pagina=1&tamano=25'),
  call('GET', '/plataforma/empresas?pagina=1&tamano=25', adminA),
  call('GET', '/plataforma/empresas?pagina=1&tamano=25', adminB),
  call('GET', '/plataforma/empresas?pagina=1&tamano=25', adminA, 2),
  call('GET', '/plataforma/empresas?pagina=0&tamano=25', globalJwt),
  call('GET', '/plataforma/empresas?pagina=1&tamano=51', globalJwt),
  call('GET', '/plataforma/empresas?pagina=1&tamano=1', globalJwt)
]);
assert.equal(global.status, 200, 'grant global vivo debe leer directorio');
assert.match(global.cache, /no-store/, 'el directorio global no debe poder cachearse');
const directory = dataOf(global);
assert.ok(Array.isArray(directory?.items), 'DTO directorio real');
assert.ok(directory.items.some(x => x.id === 1), 'empresa A persistida visible para rol global');
assert.ok(directory.items.some(x => x.id === 2), 'empresa B persistida visible para rol global');
assert.equal(directory.pagina, 1);
assert.equal(directory.tamano, 25);
assert.ok(Number.isInteger(directory.total) && directory.total >= 2);
assert.equal(typeof directory.puedeCrear, 'boolean');
assert.ok(directory.items.every(x => Number.isInteger(x.id) && typeof x.nombre === 'string'));
assert.ok(!/passwordhash|apikey|secret|connectionstring/i.test(global.raw), 'DTO no revela secreto');
denied(noToken, 'anonimo');
denied(fromA, 'administrador empresarial A');
denied(fromB, 'administrador empresarial B');
denied(spoofA, 'A con cabecera de B');
assert.equal(badPage.status, 400, 'pagina=0 debe fallar antes de leer datos');
assert.equal(badSize.status, 400, 'tamano=51 debe fallar sin listar');
assert.equal(boundary.status, 200);
assert.equal(dataOf(boundary)?.items?.length, 1);
assert.equal(dataOf(boundary)?.tamano, 1);
console.info('C0204_M0503_REAL_HTTP_JWT_AB_PASS;MYSQL_VERIFIED_BY_WORKFLOW_SQL;P4_LOCAL_ONLY=TRUE');
