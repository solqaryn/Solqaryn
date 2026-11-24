#!/usr/bin/env node
// Certificación HTTP/JWT real de sesión de plataforma contra MySQL 8.4 efímero.
// Se ejecuta después de la suite comercial A/B (sin tocar Aiven/Render).
import assert from 'node:assert/strict';
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
const api = process.env.P4_API_URL ?? 'http://127.0.0.1:5005';
assert(/^http:\/\/127\.0\.0\.1:\d+$/.test(api), 'Solo API local descartable');
const phase = process.env.P4_PLATFORM_TEST_PHASE;
assert(['active', 'revoked', 'inactive', 'role-revoked'].includes(phase),
  'Fase de prueba no soportada');
const password = process.env.P4_TEST_PASSWORD;
assert(typeof password === 'string' && password.length >= 12);
const companyA = Number(process.env.P4_COMPANY_A_ID || '1');
const companyB = Number(process.env.P4_COMPANY_B_ID || '2');
assert(process.env.RUNNER_TEMP, 'La evidencia JWT efimera exige runner aislado');
const priorFile = join(process.env.RUNNER_TEMP, 'p4-prior-enterprise-jwt.json');

async function request(method, path, token = '', company = null, payload) {
  const headers = {};
  if (token) headers.authorization = 'Bearer ' + token;
  if (company !== null) headers['X-Empresa-Id'] = String(company);
  if (payload !== undefined) headers['Content-Type'] = 'application/json';
  const response = await fetch(api + path, {
    method,
    headers,
    body: payload === undefined ? undefined : JSON.stringify(payload),
    signal: AbortSignal.timeout(15000)
  });
  const body = await response.text();
  let value;
  try { value = JSON.parse(body); } catch { value = body; }
  return { status: response.status, value, body };
}
function status(r, expected, msg) {
  assert.equal(r.status, expected,
    msg + ' HTTP=' + r.status + ', body=' + r.body.slice(0, 300));
}
async function login(user) {
  const r = await request('POST', '/auth/login', '', null,
    { nombreUsuario: user, password });
  status(r, 200, 'login real JWT ' + user);
  const jwt = (r.value?.data ?? r.value)?.token;
  assert(typeof jwt === 'string' && jwt.length > 30, 'No se generó JWT real');
  return jwt;
}

const platformJwt = await login('p4_platform_only_ci');
if (phase === 'active' || phase === 'inactive') {
  const r = await request('GET', '/plataforma/sesion', platformJwt);
  status(r, 200, 'autoridad plataforma MySQL ' + phase);
  assert.equal(r.value?.data?.nombreUsuario, 'p4_platform_only_ci');
  assert.equal(r.value?.data?.ambito, 'Plataforma');
  assert.equal(r.value?.data?.esAdministrador, true);
  assert.equal(r.value?.data?.empresaId, undefined,
    'La autoridad global no debe incluir EmpresaId');
  // Cabeceras del cliente no conceden autoridad ni cambian contexto de plataforma.
  status(await request('GET', '/plataforma/sesion', platformJwt, 999999), 200,
    'cabecera X-Empresa-Id arbitraria no controla plano de plataforma');
  let enterpriseA, enterpriseB;
  if (phase === 'active') {
    const invalid = await request('POST', '/auth/login', '', null,
      { nombreUsuario: 'p4_invalid_base_ci', password });
    status(invalid, 401, 'Grant global no sustituye membresia empresarial del rol base');
    assert(!invalid.value?.data?.token, 'Login incompatible no puede emitir JWT');
    enterpriseA = await login('p4_admin_a');
    enterpriseB = await login('p4_admin_b');
    writeFileSync(priorFile, JSON.stringify({ enterpriseA, enterpriseB }), { mode: 0o600, flag: 'wx' });
  } else {
    for (const user of ['p4_admin_a', 'p4_admin_b']) {
      const denied = await request('POST', '/auth/login', '', null, { nombreUsuario: user, password });
      status(denied, 401, 'Empresa suspendida no permite nuevo JWT ' + user);
      assert(!denied.value?.data?.token, 'Empresa suspendida no puede emitir JWT');
    }
    ({ enterpriseA, enterpriseB } = JSON.parse(readFileSync(priorFile, 'utf8')));
    assert(typeof enterpriseA === 'string' && typeof enterpriseB === 'string', 'Faltan JWT previos reales A/B');
  }
  status(await request('GET', '/plataforma/sesion', enterpriseA), 403,
    'admin de A no adquiere permisos globales por claim esAdministrador');
  status(await request('GET', '/plataforma/sesion', enterpriseB, companyB), 403,
    'admin de B no adquiere permisos globales');
  for (const token of ['', 'invalid.jwt.signature']) {
    status(await request('GET', '/plataforma/sesion', token), 401,
      'JWT anónimo o inválido denegado');
  }
  // Exact-route guard; no se amplía la whitelist comercial.
  status(await request('GET', '/plataforma/sesion/extra', platformJwt), 503,
    'sufijo de plataforma bloqueado');
  status(await request('POST', '/plataforma/sesion', platformJwt), 503,
    'POST de plataforma bloqueado');
  status(await request('GET', '/plataforma/usuarios', platformJwt), 503,
    'rutas globales no implementadas permanecen en cuarentena');
  const forbidden = await request('GET', '/clientes', platformJwt, companyA);
  assert([401, 403, 503].includes(forbidden.status),
    'rol global recibió permisos comerciales de A: HTTP=' + forbidden.status);
  if (phase === 'inactive') {
    for (const [company, token] of [[companyA, enterpriseA], [companyB, enterpriseB]]) {
      const noTenant = await request('GET', '/tenant-context/' + company, token);
      assert([401, 403].includes(noTenant.status),
        'Empresa desactivada sigue concediendo membresía: HTTP=' + noTenant.status);
    }
  }
} else {
  status(await request('GET', '/plataforma/sesion', platformJwt), 403,
    'autoridad revocada en MySQL debe ser denegada inmediatamente');
}
console.log('P4 PLATFORM JWT MYSQL PASS phase=' + phase +
  ' (authority fresh, no EmpresaId, audit/tenant isolation preserved)');
