#!/usr/bin/env node
// Certificación DEV A/B/C con JWT de usuarios reales: exclusivamente HTTP GET
// sobre el backend actual. No imprime credenciales, tokens, cuerpos ni PII.
// La única escritura esperada es la auditoría normal del POST /auth/login.
import assert from 'node:assert/strict';

const API = 'https://solqaryn-api-dev-fxx8.onrender.com';
const labels = ['A', 'B', 'C'];
const denied = new Set([401, 403, 404, 503]);
const commercial = [
  { path: '/productos?pagina=1&tamano=1', modulo: 'Productos' },
  { path: '/clientes?pagina=1&tamano=1', modulo: 'Clientes' },
  { path: '/proveedores?pagina=1&tamano=1', modulo: 'Proveedores' },
  { path: '/ventas?pagina=1&tamano=1', modulo: 'Ventas' },
  { path: '/compras?pagina=1&tamano=1', modulo: 'Compras' },
  { path: '/facturas?pagina=1&tamano=1', modulo: 'Facturacion' }
];
const dataOf = v => v?.data ?? v?.Data ?? null;
const firstOf = (v, camel, pascal) => v?.[camel] ?? v?.[pascal];

function requireThat(ok, key) {
  if (!ok) throw new Error('MF3_BLOCKED=' + key);
}
function readSecret(key) {
  const s = process.env[key];
  requireThat(typeof s === 'string' && s.length > 0,
    'missing_secret_' + key.replace('SOLQARYN_DEV_', ''));
  return s;
}
function headerOf(token, company) {
  const h = { accept: 'application/json', 'cache-control': 'no-store' };
  if (token) h.authorization = 'Bearer ' + token;
  if (company !== undefined) h['X-Empresa-Id'] = String(company);
  return h;
}
async function http(path, token, company, body, parse = false) {
  const url = new URL(path, API);
  requireThat(url.origin === API && url.protocol === 'https:', 'wrong_origin');
  const isLogin = body !== undefined;
  const headers = headerOf(token, company);
  if (isLogin) headers['content-type'] = 'application/json';
  const response = await fetch(url, {
    method: isLogin ? 'POST' : 'GET', headers,
    body: isLogin ? JSON.stringify(body) : undefined,
    redirect: 'manual',
    signal: AbortSignal.timeout(20000)
  });
  requireThat(response.status < 300 || response.status >= 400,
    'redirect_rejected');
  let data;
  if (parse) {
    // Los cuerpos completos nunca salen del proceso ni de Actions.
    const text = await response.text();
    requireThat(text.length <= 262144, 'unexpected_payload_size');
    try { data = JSON.parse(text); } catch { data = null; }
  } else {
    await response.body?.cancel();
  }
  return { status: response.status, data, headers: response.headers };
}
async function readiness() {
  for (let attempt = 0; attempt < 35; attempt++) {
    try {
      const response = await http('/health/ready');
      if (response.status === 200) return;
    } catch { /* Render puede arrancar en frío; no registrar datos. */ }
    await new Promise(resolve => setTimeout(resolve, 4000));
  }
  throw new Error('MF3_BLOCKED=dev_readiness');
}
function getIdentities() {
  const users = labels.map(label => {
    const name = readSecret('SOLQARYN_DEV_' + label + '_USERNAME');
    const password = readSecret('SOLQARYN_DEV_' + label + '_PASSWORD');
    const id = Number(process.env['MF3_COMPANY_' + label + '_ID']);
    requireThat(Number.isSafeInteger(id) && id > 0,
      'missing_company_mapping_' + label);
    return { label, name, password, id, token: '', permissions: null };
  });
  requireThat(new Set(users.map(x => x.name.toLowerCase())).size === 3,
    'duplicate_username');
  requireThat(new Set(users.map(x => x.id)).size === 3, 'duplicate_company');
  return users;
}
async function authenticate(user) {
  const res = await http('/auth/login', '', undefined, {
    nombreUsuario: user.name, password: user.password
  }, true);
  requireThat(res.status === 200, 'login_' + user.label + '_http_' + res.status);
  const dto = dataOf(res.data);
  const token = firstOf(dto, 'token', 'Token');
  requireThat(typeof token === 'string' && token.length > 40,
    'jwt_missing_' + user.label);
  const loginName = firstOf(dto, 'nombreUsuario', 'NombreUsuario');
  requireThat(typeof loginName === 'string' &&
    loginName.toLowerCase() === user.name.trim().toLowerCase(),
    'jwt_identity_' + user.label);
  user.token = token;
  console.log('MF3_LOGIN_' + user.label + '=PASS');
}
async function validateOwnedIdentity(user) {
  const ctx = await http('/tenant-context/' + user.id, user.token, undefined,
    undefined, true);
  requireThat(ctx.status === 200,
    'own_context_' + user.label + '_http_' + ctx.status);
  const scopedCompanyId = Number(firstOf(dataOf(ctx.data), 'empresaId', 'EmpresaId'));
  requireThat(scopedCompanyId === user.id, 'scope_id_mismatch_' + user.label);
  const perms = await http('/permisos/mis-permisos/empresa/' + user.id,
    user.token, undefined, undefined, true);
  requireThat(perms.status === 200,
    'own_permissions_' + user.label + '_http_' + perms.status);
  const d = dataOf(perms.data);
  requireThat(typeof d === 'object' && d !== null &&
    Array.isArray(firstOf(d, 'permisos', 'Permisos')) &&
    Array.isArray(firstOf(d, 'capacidadesHabilitadas', 'CapacidadesHabilitadas')) &&
    Array.isArray(firstOf(d, 'rutasHabilitadas', 'RutasHabilitadas')),
    'permissions_contract_' + user.label);
  requireThat(firstOf(d, 'operacionMultiEmpresaRestringida',
    'OperacionMultiEmpresaRestringida') === true,
    'expected_multiempresa_restriction_' + user.label);
  requireThat((perms.headers.get('cache-control') ?? '').includes('no-store'),
    'permissions_cache_' + user.label);
  user.permissions = d;
  console.log('MF3_OWN_TENANT_' + user.label + '=PASS');
}
async function verifyCrossedContexts(users) {
  let checks = 0;
  for (const current of users) {
    for (const foreign of users) {
      if (current === foreign) continue;
      // Dos rutas de seguridad conocidas y autorizadas para la empresa propia;
      // el 403 debe surgir de membresía MySQL, no de cuarentena global 503.
      for (const route of ['/tenant-context/', '/permisos/mis-permisos/empresa/']) {
        const res = await http(route + foreign.id, current.token, current.id);
        requireThat(res.status === 403,
          'cross_scope_' + current.label + '_to_' + foreign.label +
          '_http_' + res.status);
        checks++;
      }
    }
  }
  // Incluso una ruta sin JWT nunca debe devolver información de permisos.
  const anonymous = await http('/permisos/mis-permisos/empresa/' + users[0].id);
  requireThat(denied.has(anonymous.status), 'anonymous_permission_leak');
  console.log('MF3_CROSS_TENANT_DENIALS=PASS count=' + checks);
}
async function verifyCommercial(users) {
  let pilotReads = 0, quarantineReads = 0, deniedCrossed = 0;
  for (const current of users) {
    let positiveForUser = 0;
    const grants = new Set(firstOf(current.permissions, 'permisos', 'Permisos'));
    const caps = new Set(firstOf(current.permissions,
      'capacidadesHabilitadas', 'CapacidadesHabilitadas'));
    for (const item of commercial) {
      const key = item.modulo + ':Ver';
      const expectedOwnRead = grants.has(key) && caps.has(key);
      const own = await http(item.path, current.token, current.id);
      if (expectedOwnRead) {
        requireThat(own.status === 200,
          'certified_read_' + current.label + '_' + item.modulo +
          '_http_' + own.status);
        pilotReads++;
        positiveForUser++;
      } else {
        // Un piloto no otorgado por rol y capacidad no puede quedar abierto.
        requireThat(denied.has(own.status),
          'uncertified_read_visible_' + current.label + '_' + item.modulo +
          '_http_' + own.status);
        quarantineReads++;
      }
      for (const foreign of users) {
        if (current === foreign) continue;
        const crossed = await http(item.path, current.token, foreign.id);
        requireThat(denied.has(crossed.status),
          'crossed_commercial_' + current.label + '_to_' + foreign.label +
          '_' + item.modulo + '_http_' + crossed.status);
        deniedCrossed++;
      }
    }
    requireThat(positiveForUser > 0,
      'no_positive_commercial_pilot_' + current.label);
  }
  console.log('MF3_CROSS_COMMERCIAL_DENIALS=PASS count=' + deniedCrossed);
  console.log('MF3_OWN_CERTIFIED_READS=PASS count=' + pilotReads);
  console.log('MF3_UNCERTIFIED_READS_DENIED=PASS count=' + quarantineReads);
  // Cada identidad probó al menos una lectura comercial legítima, no solo el grupo.
}
async function main() {
  if (process.argv.includes('--self-test')) {
    assert.deepEqual(labels, ['A', 'B', 'C']);
    assert(denied.has(503) && !denied.has(200) && !denied.has(500));
    assert.equal(dataOf({ data: { id: 1 } }).id, 1);
    assert.equal(firstOf({ empresaId: 3 }, 'empresaId', 'EmpresaId'), 3);
    assert(commercial.length >= 6 && commercial.every(x => x.path.startsWith('/')));
    console.log('MF3_CONTRACT_SELF_TEST=PASS');
    return;
  }
  requireThat(process.env.GITHUB_REPOSITORY === 'solqaryn/Solqaryn' &&
    process.env.GITHUB_REF === 'refs/heads/dev',
  'invalid_workflow_context');
  await readiness();
  const users = getIdentities();
  for (const user of users) await authenticate(user);
  requireThat(new Set(users.map(u => u.token)).size === 3,
    'duplicated_jwt');
  for (const user of users) await validateOwnedIdentity(user);
  await verifyCrossedContexts(users);
  await verifyCommercial(users);
  console.log('MF3_DEV_ABC_REAL_JWT=PASS');
}
main().catch(error => {
  // Mensajes genéricos y códigos técnicos; jamás salida de response/stack/tokens.
  const key = typeof error?.message === 'string' &&
    /^MF3_BLOCKED=[A-Za-z0-9_]+$/.test(error.message)
      ? error.message : 'MF3_BLOCKED=unexpected_failure';
  console.error(key);
  process.exitCode = 1;
});
