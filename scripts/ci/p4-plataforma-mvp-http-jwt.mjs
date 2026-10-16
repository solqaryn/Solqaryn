#!/usr/bin/env node
// Microfase 4 - aceptación JWT/HTTP real y MySQL efímero; NUNCA Aiven ni PROD.
import assert from 'node:assert/strict';

const api = process.env.P4_API_URL ?? 'http://127.0.0.1:5005';
assert(/^http:\/\/127\.0\.0\.1:\d+$/.test(api), 'La suite exige servidor localhost descartable');
const phase = process.env.P4_MVP_TEST_PHASE;
assert(['active', 'inactive', 'revoked'].includes(phase), 'Fase inválida');
const password = process.env.P4_TEST_PASSWORD;
assert(typeof password === 'string' && password.length >= 12);
const companyA = Number(process.env.P4_COMPANY_A_ID || '1');
const companyB = Number(process.env.P4_COMPANY_B_ID || '2');
assert(companyA > 0 && companyB > 0 && companyA !== companyB);

async function request(method, path, jwt = '', body = undefined, empresaSpoof = null) {
  const headers = {};
  if (jwt) headers.authorization = 'Bearer ' + jwt;
  if (empresaSpoof !== null) headers['X-Empresa-Id'] = String(empresaSpoof);
  if (body !== undefined) headers['content-type'] = 'application/json';
  const res = await fetch(api + path, {
    method, headers, body: body === undefined ? undefined : JSON.stringify(body),
    signal: AbortSignal.timeout(18000)
  });
  const raw = await res.text();
  let value;
  try { value = JSON.parse(raw); } catch { value = raw; }
  return { status: res.status, value, raw };
}
function expectStatus(r, code, name) {
  assert.equal(r.status, code, name + ' HTTP=' + r.status + ' ' + r.raw.slice(0, 370));
}
function mustDeny(r, name) {
  assert([401, 403, 404, 503].includes(r.status),
    name + ': autoridad indebida HTTP=' + r.status + ' ' + r.raw.slice(0, 160));
}
async function login(user, pass = password) {
  const r = await request('POST', '/auth/login', '', { nombreUsuario: user, password: pass });
  expectStatus(r, 200, 'Login JWT ' + user);
  const token = r.value?.data?.token ?? r.value?.token;
  assert(typeof token === 'string' && token.length > 30, 'JWT no emitido');
  return token;
}

const platform = await login('p4_platform_only_ci');
const aToken = await login('p4_admin_a');
const bToken = await login('p4_admin_b');
const url = '/plataforma/gestion';

if (phase === 'active') {
  const dir = await request('GET', '/plataforma/empresas?pagina=1&tamano=25', platform, undefined, companyB);
  expectStatus(dir, 200, 'Directorio global A/B');
  const rows = dir.value?.data?.items;
  assert(Array.isArray(rows) && rows.some(x => x.id === companyA) &&
    rows.some(x => x.id === companyB), 'El directorio no contiene A y B');
  mustDeny(await request('GET', '/plataforma/empresas', aToken),
    'Admin empresarial A no puede listar global');
  mustDeny(await request('GET', '/plataforma/empresas', bToken),
    'Admin empresarial B no puede listar global');

  const roles = await request('GET', url + '/roles?empresaId=' + companyA, platform);
  expectStatus(roles, 200, 'Roles empresariales A');
  const r = roles.value?.data;
  assert(Array.isArray(r), 'Roles no son lista');
  const admin = r.find(x => x.esAdministrador && x.empresaId === null);
  const base = r.find(x => !x.esAdministrador && x.empresaId === null);
  assert(admin?.id > 0 && base?.id > 0, 'Faltan roles de plantilla empresarial');
  assert(r.every(x => x.empresaId === null || x.empresaId === companyA),
    'Rol B o global filtrado al catálogo A');
  mustDeny(await request('GET', url + '/roles?empresaId=' + companyA, aToken),
    'Admin empresarial no consulta control plane');

  const userName = 'p4_mvp_user_ci';
  const create = await request('POST', url + '/usuarios', platform, {
    nombreUsuario: userName, nombreCompleto: 'Operador empresa A CI',
    password
  });
  expectStatus(create, 201, 'Crear identidad sin membresía');
  const newId = create.value?.data?.id;
  assert(Number.isInteger(newId) && newId > 0, 'Falta ID nueva identidad');
  const search = await request('GET', url + '/usuarios/buscar?nombreUsuario=' + userName,
    platform);
  expectStatus(search, 200, 'Buscar cuenta global');
  assert.equal(search.value?.data?.id, newId);
  mustDeny(await request('GET', url + '/usuarios/buscar?nombreUsuario=' + userName,
    bToken), 'Admin empresarial no busca identidad global');
  mustDeny(await request('POST', url + '/usuarios', aToken, {
    nombreUsuario: 'forbiddenuser', nombreCompleto: 'Identidad prohibida',
    password
  }), 'Admin empresarial no crea identidad global');

  const attach = await request('POST', url + '/empresas/' + companyA + '/membresias',
    platform, { usuarioId: newId, rolId: base.id }, companyB);
  expectStatus(attach, 200, 'Asignar exclusivamente a A');
  mustDeny(await request('POST', url + '/empresas/' + companyB + '/membresias',
    aToken, { usuarioId: newId, rolId: admin.id }), 'Admin A no concede B');
  const newToken = await login(userName);
  expectStatus(await request('GET', '/tenant-context/' + companyA, newToken), 200,
    'Nueva cuenta accede únicamente a A');
  mustDeny(await request('GET', '/tenant-context/' + companyB, newToken),
    'Nueva cuenta no accede B');

  expectStatus(await request('PUT', url + '/empresas/' + companyA +
    '/membresias/' + newId + '/rol', platform, { rolId: admin.id }),
    200, 'Promover admin de A');
  expectStatus(await request('PUT', url + '/empresas/' + companyA +
    '/membresias/' + newId + '/rol', platform, { rolId: base.id }),
    200, 'Cambiar permisos de A sin afectar B');
  expectStatus(await request('PUT', url + '/empresas/' + companyA +
    '/membresias/' + newId + '/estado', platform, { activa: false }),
    200, 'Revocación de membresía A');
  mustDeny(await request('GET', '/tenant-context/' + companyA, newToken),
    'Membresía A revocada debe perder acceso inmediatamente');
  expectStatus(await request('PUT', url + '/empresas/' + companyA +
    '/membresias/' + newId + '/estado', platform, { activa: true }),
    200, 'Restaurar membresía A');
  expectStatus(await request('GET', '/tenant-context/' + companyA, newToken),
    200, 'Restauración de membresía A');
  mustDeny(await request('GET', '/tenant-context/' + companyB, newToken),
    'A jamás obtiene B por restaurar acceso');

  const rename = await request('PUT', url + '/empresas/' + companyA + '/nombre',
    platform, { nombre: 'Empresa A JWT temporal' });
  expectStatus(rename, 200, 'Renombrar A bajo auditoría');
  expectStatus(await request('PUT', url + '/empresas/' + companyA + '/nombre',
    platform, { nombre: 'Empresa A JWT' }), 200, 'Restaurar nombre A');
  mustDeny(await request('POST', url + '/empresas/' + companyA + '/nombre',
    platform, { nombre: 'nombre incorrecto' }), 'Método no permitido');
  mustDeny(await request('GET', url + '/empresas/' + companyA + '/membresias/extra',
    platform), 'Ruta adyacente bloqueada');

  const list = await request('GET', url + '/empresas/' + companyA + '/membresias', platform);
  expectStatus(list, 200, 'Membresías A verificadas');
  assert(Array.isArray(list.value?.data) &&
    list.value.data.some(x => x.usuarioId === newId && x.activa),
    'Membresía A ausente después de restaurar');
  assert(list.value.data.every(x => x.nombreUsuario !== 'p4_admin_b'),
    'La lista de A filtró administrador exclusivo de B');
} else if (phase === 'inactive') {
  const companies = await request('GET', '/plataforma/empresas', platform);
  expectStatus(companies, 200, 'El global sigue leyendo directorio suspendido');
  assert(companies.value?.data?.items?.some(e => e.id === companyA && e.activa === false));
  assert(companies.value?.data?.items?.some(e => e.id === companyB && e.activa === false));
  const lookup = await request('GET', url + '/empresas/' + companyA + '/membresias', platform);
  expectStatus(lookup, 200, 'Membresía legible por plataforma aun con empresa A suspendida');
  mustDeny(await request('GET', '/tenant-context/' + companyA, aToken),
    'Empresa A inactiva no confiere acceso empresarial');
  mustDeny(await request('GET', url + '/empresas/' + companyA + '/membresias', aToken),
    'Admin empresa inactiva no obtiene control plane');
} else {
  mustDeny(await request('GET', '/plataforma/empresas', platform),
    'Grant global revocado: directorio denegado');
  mustDeny(await request('GET', url + '/roles?empresaId=' + companyA, platform),
    'Grant global revocado: roles denegados');
  mustDeny(await request('POST', url + '/usuarios', platform, {
    nombreUsuario: 'none', nombreCompleto: 'No crear nada', password
  }), 'Grant global revocado: escrituras denegadas');
}
console.log('P4 MVP GLOBAL HTTP MYSQL PASS phase=' + phase +
  ' (JWT, empresas inactivas, identidades, RBAC por empresa, revocación e aislamiento A/B)');
