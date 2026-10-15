#!/usr/bin/env node
// P4: aceptación HTTP/JWT REAL A/B contra MySQL local descartable.
// No usa Render, Aiven ni credenciales de un entorno persistente.
import assert from 'node:assert/strict';
import { request as nodeHttpRequest } from 'node:http';

const api = process.env.P4_API_URL ?? 'http://127.0.0.1:5005';
if (!/^http:\/\/127\.0\.0\.1:\d+$/.test(api))
  throw new Error('Esta suite sólo permite API local descartable.');
const password = process.env.P4_TEST_PASSWORD;
if (!password) throw new Error('P4_TEST_PASSWORD obligatorio para CI efímero.');
const aId = Number(process.env.P4_COMPANY_A_ID ?? '1');
const bId = Number(process.env.P4_COMPANY_B_ID ?? '2');
assert(aId > 0 && bId > 0 && aId !== bId);
const unwrap = x => x?.data ?? x?.Data ?? x;

// Node fetch/undici sobrescribe el Host explícito con 127.0.0.1. Para
// certificar los bindings públicos A/B enviar el Host exacto con http.request;
// el socket sigue conectado ÚNICAMENTE a la API local efímera.
function localRequestWithHost(method, path, headers, payload) {
  return new Promise((resolve, reject) => {
    const request = nodeHttpRequest(api + path,
      { method, headers, timeout: 15000 }, response => {
        const chunks = [];
        response.on('data', chunk => chunks.push(chunk));
        response.on('error', reject);
        response.on('end', () => {
          const body = Buffer.concat(chunks).toString('utf8');
          let parsed;
          try { parsed = JSON.parse(body); } catch { parsed = body; }
          resolve({ status: response.statusCode, payload: parsed, body });
        });
      });
    request.on('timeout', () => request.destroy(new Error('HTTP local timeout')));
    request.on('error', reject);
    if (payload !== undefined) request.write(JSON.stringify(payload));
    request.end();
  });
}

async function call(method, path, token = '', company = null, payload, host = '') {
  const headers = {};
  if (token) headers.authorization = 'Bearer ' + token;
  if (company !== null) headers['X-Empresa-Id'] = String(company);
  if (host) headers.host = host;
  if (payload !== undefined) headers['content-type'] = 'application/json';
  if (host) return localRequestWithHost(method, path, headers, payload);
  const response = await fetch(api + path, {
    method, headers,
    body: payload === undefined ? undefined : JSON.stringify(payload),
    signal: AbortSignal.timeout(15000)
  });
  const body = await response.text();
  let parsed;
  try { parsed = JSON.parse(body); } catch { parsed = body; }
  return { status: response.status, payload: parsed, body };
}
function expectStatus(response, code, name) {
  assert.equal(response.status, code,
    name + ': HTTP=' + response.status + ', body=' + response.body.slice(0, 500));
}
async function deny(method, path, token, id, name) {
  const r = await call(method, path, token, id);
  assert([401, 403, 404, 503].includes(r.status),
    name + ': acceso indebido HTTP=' + r.status);
}
async function login(username) {
  const r = await call('POST', '/auth/login', '', null,
    { nombreUsuario: username, password });
  expectStatus(r, 200, 'login ' + username);
  const data = unwrap(r.payload);
  const token = data?.token ?? data?.Token;
  assert(typeof token === 'string' && token.length > 30, 'JWT ausente');
  return token;
}
function idOf(response, name) {
  const data = unwrap(response.payload);
  const id = Number(data?.id ?? data?.Id);
  assert(Number.isInteger(id) && id > 0,
    name + ': id ausente en ' + response.body.slice(0, 350));
  return id;
}
function ownNotForeign(response, own, foreign, name) {
  expectStatus(response, 200, name);
  const items = unwrap(response.payload);
  assert(Array.isArray(items), name + ': respuesta no lista');
  assert(items.some(v => Number(v.id ?? v.Id) === own), name + ': registro propio ausente');
  assert(!items.some(v => Number(v.id ?? v.Id) === foreign), name + ': exposición A/B');
}

const jwtA = await login('p4_admin_a');
const jwtB = await login('p4_admin_b');

expectStatus(await call('GET', '/tenant-context/' + aId, jwtA), 200, 'membresía A');
expectStatus(await call('GET', '/tenant-context/' + bId, jwtB), 200, 'membresía B');
await deny('GET', '/tenant-context/' + bId, jwtA, null, 'membresía A→B');
await deny('GET', '/tenant-context/' + aId, jwtB, null, 'membresía B→A');
await deny('GET', '/productos', jwtA, bId, 'spoof cabecera A→B');

const typeBody = { nombre: 'General P4 A/B', colorHex: '#2A4C68',
  activo: true, esPredeterminado: true };
const typeA = await call('POST', '/tipo-clientes', jwtA, aId, typeBody);
const typeB = await call('POST', '/tipo-clientes', jwtB, bId, typeBody);
expectStatus(typeA, 201, 'tipo A');
expectStatus(typeB, 201, 'tipo B');
const typeAId = idOf(typeA, 'tipo A'), typeBId = idOf(typeB, 'tipo B');
ownNotForeign(await call('GET', '/tipo-clientes', jwtA, aId),
  typeAId, typeBId, 'tipos A');
ownNotForeign(await call('GET', '/tipo-clientes', jwtB, bId),
  typeBId, typeAId, 'tipos B');
expectStatus(await call('GET', '/tipo-clientes/' + typeBId, jwtA, aId),
  404, 'tipo ajeno A→B');

const clientA = await call('POST', '/clientes', jwtA, aId,
  { nombre: 'Cliente privado A', tipoClienteId: typeAId });
const clientB = await call('POST', '/clientes', jwtB, bId,
  { nombre: 'Cliente privado B', tipoClienteId: typeBId });
expectStatus(clientA, 201, 'cliente A');
expectStatus(clientB, 201, 'cliente B');
const clientAId = idOf(clientA, 'cliente A'), clientBId = idOf(clientB, 'cliente B');
ownNotForeign(await call('GET', '/clientes', jwtA, aId),
  clientAId, clientBId, 'clientes A');
ownNotForeign(await call('GET', '/clientes', jwtB, bId),
  clientBId, clientAId, 'clientes B');
expectStatus(await call('GET', '/clientes/' + clientBId, jwtA, aId),
  404, 'cliente ajeno A→B');

const doc = 'P4AB-TEST-2026';
const providerA = await call('POST', '/proveedores', jwtA, aId,
  { nombre: 'Proveedor privado A', documento: doc });
const providerB = await call('POST', '/proveedores', jwtB, bId,
  { nombre: 'Proveedor privado B', documento: doc });
expectStatus(providerA, 201, 'proveedor A');
expectStatus(providerB, 201, 'proveedor B');
const providerAId = idOf(providerA, 'proveedor A');
const providerBId = idOf(providerB, 'proveedor B');
ownNotForeign(await call('GET', '/proveedores', jwtA, aId),
  providerAId, providerBId, 'proveedores A');
ownNotForeign(await call('GET', '/proveedores', jwtB, bId),
  providerBId, providerAId, 'proveedores B');
expectStatus(await call('GET', '/proveedores/' + providerBId, jwtA, aId),
  404, 'proveedor ajeno A→B');

for (const path of ['/ventas', '/compras', '/facturas']) {
  expectStatus(await call('GET', path, jwtA, aId), 200, path + ' A');
  expectStatus(await call('GET', path, jwtB, bId), 200, path + ' B');
  await deny('GET', path, jwtA, bId, 'spoof lectura ' + path);
}
for (const [method, path] of [
  ['POST', '/ventas'], ['POST', '/compras'], ['POST', '/facturas/1/pagos'],
  ['GET', '/facturas/1/pdf'], ['GET', '/compras/1/documentos'],
  ['GET', '/productos/1/imagenes/descargar-todas']
]) {
  expectStatus(await call(method, path, jwtA, aId), 503,
    'cuarentena ' + method + ' ' + path);
}

expectStatus(await call('GET', '/tienda/categorias', '', null, undefined,
  'store-a.p4.test'), 200, 'tienda Host A');
expectStatus(await call('GET', '/tienda/categorias', '', null, undefined,
  'store-b.p4.test'), 200, 'tienda Host B');
expectStatus(await call('GET', '/tienda/categorias', '', null, undefined,
  'unknown.p4.test'), 503, 'Host público no aprobado');

console.log('P4 HTTP JWT A/B PASS: memberships, RBAC, types, clients, suppliers, documentary GETs and stores.');
console.log('No certifica mutaciones de ventas/compras, inventarios, archivos, pagos ni jobs.');
