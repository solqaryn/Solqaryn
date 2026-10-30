#!/usr/bin/env node
// Certificación DEV A/B/C con JWT de usuarios reales: exclusivamente HTTP GET
// sobre el backend actual. No imprime credenciales, tokens, cuerpos ni PII.
// La única escritura esperada es la auditoría normal del POST /auth/login.
import assert from 'node:assert/strict';
import { request as httpsRequest } from 'node:https';

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
const roleIdsOf = roles => new Set(roles.map(role =>
  Number(firstOf(role, 'id', 'Id'))));
const isRoleVisibleToTenant = (role, companyId) => {
  const owner = firstOf(role, 'empresaId', 'EmpresaId');
  return owner === companyId || (owner == null &&
    firstOf(role, 'esSistema', 'EsSistema') === true);
};
const canReadRoles = user => {
  const grants = new Set(firstOf(user.permissions, 'permisos', 'Permisos'));
  const caps = new Set(firstOf(user.permissions,
    'capacidadesHabilitadas', 'CapacidadesHabilitadas'));
  return grants.has('Roles:Ver') && caps.has('Roles:Ver');
};
const roleDenialKind = response => {
  if (response.status === 403) return 'RBAC_403';
  if (response.status === 503 &&
      firstOf(response.data, 'code', 'Code') === 'TENANT_DATA_ISOLATION_REQUIRED')
    return 'ISOLATION_GUARD_503';
  return null;
};

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
let phase = 'START';
const pause = ms => new Promise(resolve => setTimeout(resolve, ms));

function exchange(url, method, headers, payload, parse) {
  return new Promise((resolve, reject) => {
    // Sin pooling ni reutilización de sockets entre tres JWT diferentes.
    const req = httpsRequest(url, {
      method, headers, agent: false, signal: AbortSignal.timeout(45000)
    }, response => {
      const status = response.statusCode ?? 0;
      const responseHeaders = {
        get: name => response.headers[String(name).toLowerCase()] ?? null
      };
      if (status >= 300 && status < 400) {
        response.destroy();
        reject(new Error('MF3_BLOCKED=redirect_rejected'));
        return;
      }
      if (!parse) {
        // Solo verificar el código HTTP; nunca conservar registros comerciales.
        response.destroy();
        resolve({ status, headers: responseHeaders });
        return;
      }
      const chunks = [];
      let size = 0;
      response.on('data', chunk => {
        size += chunk.length;
        if (size > 262144) {
          req.destroy(new Error('MF3_BLOCKED=unexpected_payload_size'));
          return;
        }
        chunks.push(chunk);
      });
      response.once('error', reject);
      response.once('end', () => {
        let data;
        try { data = JSON.parse(Buffer.concat(chunks).toString('utf8')); }
        catch { data = null; }
        resolve({ status, data, headers: responseHeaders });
      });
    });
    req.once('error', reject);
    if (payload !== undefined) req.write(payload);
    req.end();
  });
}
async function http(path, token, company, body, parse = false) {
  const url = new URL(path, API);
  requireThat(url.origin === API && url.protocol === 'https:', 'wrong_origin');
  const method = body === undefined ? 'GET' : 'POST';
  const headers = {
    ...headerOf(token, company),
    connection: 'close',
    'accept-encoding': 'identity'
  };
  const payload = body === undefined ? undefined : JSON.stringify(body);
  if (payload !== undefined) {
    headers['content-type'] = 'application/json';
    headers['content-length'] = String(Buffer.byteLength(payload));
  }
  // Repetir SOLO fallos de transporte (no reintentar respuestas 401/403/503).
  // Un login reenviado puede registrar auditoría adicional, nunca datos comerciales.
  for (let attempt = 1; attempt <= 3; attempt++) {
    try {
      return await exchange(url, method, headers, payload, parse);
    } catch (error) {
      if (typeof error?.message === 'string' &&
          /^MF3_BLOCKED=[A-Za-z0-9_]+$/.test(error.message)) throw error;
      if (attempt === 3) throw new Error('MF3_BLOCKED=transport_' + phase);
      console.log('MF3_NETWORK_RETRY_' + phase + '=' + attempt);
      await pause(500 * attempt);
    }
  }
  throw new Error('MF3_BLOCKED=transport_' + phase);
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
  phase = 'LOGIN_' + user.label;
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
  phase = 'OWN_CONTEXT_' + user.label;
  const ctx = await http('/tenant-context/' + user.id, user.token, undefined,
    undefined, true);
  requireThat(ctx.status === 200,
    'own_context_' + user.label + '_http_' + ctx.status);
  const scopedCompanyId = Number(firstOf(dataOf(ctx.data), 'empresaId', 'EmpresaId'));
  requireThat(scopedCompanyId === user.id, 'scope_id_mismatch_' + user.label);
  phase = 'OWN_PERMISSIONS_' + user.label;
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
        phase = 'CROSS_SCOPE_' + current.label + '_TO_' + foreign.label;
        const res = await http(route + foreign.id, current.token, current.id);
        requireThat(res.status === 403,
          'cross_scope_' + current.label + '_to_' + foreign.label +
          '_http_' + res.status);
        checks++;
      }
    }
  }
  // Incluso una ruta sin JWT nunca debe devolver información de permisos.
  phase = 'ANONYMOUS_PERMISSIONS';
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
      phase = 'OWN_COMMERCIAL_' + current.label + '_' + item.modulo;
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
        phase = 'CROSS_COMMERCIAL_' + current.label + '_TO_' + foreign.label;
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
async function verifyLiveRoles(users) {
  const a = users.find(user => user.label === 'A');
  const b = users.find(user => user.label === 'B');
  requireThat(a && b, 'roles_test_identities_missing');
  const privateRoleIds = new Map();
  const assignedRoleIds = new Map();
  for (const label of ['A', 'B']) {
    const privateRaw = process.env['MF3_ROLE_' + label + '_ID'] ?? '';
    if (privateRaw === '') {
      privateRoleIds.set(label, null);
    } else {
      const privateId = Number(privateRaw);
      requireThat(Number.isSafeInteger(privateId) && privateId > 0,
        'tenant_role_mapping_' + label + '_invalid');
      privateRoleIds.set(label, privateId);
    }
    const assignedId = Number(process.env['MF3_ASSIGNED_ROLE_' + label + '_ID']);
    requireThat(Number.isSafeInteger(assignedId) && assignedId > 0,
      'assigned_role_mapping_' + label + '_missing');
    assignedRoleIds.set(label, assignedId);
  }
  let crossIdChecks = 0;
  for (const user of [a, b]) {
    if (!canReadRoles(user)) {
      phase = 'DENIED_ROLES_LIST_' + user.label;
      const listDenied = await http('/roles', user.token, user.id,
        undefined, true);
      const listDenial = roleDenialKind(listDenied);
      requireThat(listDenial !== null,
        'roles_list_without_grant_' + user.label + '_http_' + listDenied.status);
      phase = 'DENIED_ROLE_DETAIL_' + user.label;
      const detailDenied = await http('/roles/' + assignedRoleIds.get(user.label),
        user.token, user.id, undefined, true);
      const detailDenial = roleDenialKind(detailDenied);
      requireThat(detailDenial !== null,
        'role_detail_without_grant_' + user.label + '_http_' + detailDenied.status);
      console.log('MF3_ROLES_LIST_' + user.label + '=' + listDenial + '_PASS');
      console.log('MF3_ROLE_DETAIL_' + user.label + '=' + detailDenial + '_PASS');
      continue;
    }
    phase = 'OWN_ROLES_LIST_' + user.label;
    const list = await http('/roles', user.token, user.id, undefined, true);
    requireThat(list.status === 200,
      'own_roles_list_' + user.label + '_http_' + list.status);
    const roles = dataOf(list.data);
    requireThat(Array.isArray(roles), 'roles_list_contract_' + user.label);
    const ids = roleIdsOf(roles);
    const ownRoleId = assignedRoleIds.get(user.label);
    requireThat(ids.has(ownRoleId), 'assigned_role_absent_' + user.label);
    for (const role of roles) {
      requireThat(isRoleVisibleToTenant(role, user.id),
        'foreign_or_unowned_role_in_list_' + user.label);
    }

    phase = 'OWN_ROLE_DETAIL_' + user.label;
    const own = await http('/roles/' + ownRoleId, user.token, user.id,
      undefined, true);
    requireThat(own.status === 200,
      'own_role_detail_' + user.label + '_http_' + own.status);
    const ownDto = dataOf(own.data);
    requireThat(Number(firstOf(ownDto, 'id', 'Id')) === ownRoleId &&
      (Number(firstOf(ownDto, 'empresaId', 'EmpresaId')) === user.id ||
        (firstOf(ownDto, 'empresaId', 'EmpresaId') == null &&
          firstOf(ownDto, 'esSistema', 'EsSistema') === true)),
      'own_role_detail_scope_' + user.label);

    const otherLabel = user.label === 'A' ? 'B' : 'A';
    const foreignRoleId = privateRoleIds.get(otherLabel);
    if (foreignRoleId !== null) {
      requireThat(!ids.has(foreignRoleId), 'foreign_role_leaked_' + user.label);
      phase = 'CROSS_ROLE_DETAIL_' + user.label;
      const crossed = await http('/roles/' + foreignRoleId, user.token, user.id,
        undefined, true);
      requireThat(crossed.status === 404,
        'cross_role_detail_' + user.label + '_http_' + crossed.status);
      crossIdChecks++;
    }
  }
  phase = 'ANONYMOUS_ROLES';
  const anonymous = await http('/roles');
  requireThat(denied.has(anonymous.status),
    'anonymous_roles_list_http_' + anonymous.status);
  if (crossIdChecks > 0)
    console.log('MF3_CROSS_ROLE_ID_DENIALS=PASS count=' + crossIdChecks);
  else
    console.log('MF3_CROSS_ROLE_ID_DENIALS=NOT_RUN reason=no_private_role_fixture');
  console.log('MF3_LIVE_ROLES_AB_AUTHORIZATION=PASS');
}
async function main() {
  if (process.argv.includes('--self-test')) {
    assert.deepEqual(labels, ['A', 'B', 'C']);
    assert(denied.has(503) && !denied.has(200) && !denied.has(500));
    assert.equal(dataOf({ data: { id: 1 } }).id, 1);
    assert.equal(firstOf({ empresaId: 3 }, 'empresaId', 'EmpresaId'), 3);
    assert(commercial.length >= 6 && commercial.every(x => x.path.startsWith('/')));
    assert.deepEqual([...roleIdsOf([{ id: 7 }, { Id: '8' }])], [7, 8]);
    assert(isRoleVisibleToTenant({ empresaId: 4, esSistema: false }, 4));
    assert(isRoleVisibleToTenant({ EmpresaId: null, EsSistema: true }, 4));
    assert(!isRoleVisibleToTenant({ empresaId: 5, esSistema: false }, 4));
    assert(!isRoleVisibleToTenant({ EmpresaId: null, EsSistema: false }, 4));
    assert(canReadRoles({ permissions: {
      permisos: ['Roles:Ver'], capacidadesHabilitadas: ['Roles:Ver']
    } }));
    assert(!canReadRoles({ permissions: {
      permisos: [], capacidadesHabilitadas: []
    } }));
    assert.equal(roleDenialKind({ status: 403, data: null }), 'RBAC_403');
    assert.equal(roleDenialKind({ status: 503, data: {
      code: 'TENANT_DATA_ISOLATION_REQUIRED'
    } }), 'ISOLATION_GUARD_503');
    assert.equal(roleDenialKind({ status: 503, data: { code: 'OTHER' } }), null);
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
  await verifyLiveRoles(users);
  await verifyCommercial(users);
  console.log('MF3_DEV_ABC_REAL_JWT=PASS');
}
main().catch(error => {
  // Mensajes genéricos y códigos técnicos; jamás salida de response/stack/tokens.
  const key = typeof error?.message === 'string' &&
    /^MF3_BLOCKED=[A-Za-z0-9_]+$/.test(error.message)
      ? error.message : 'MF3_BLOCKED=unexpected_' + phase;
  console.error(key);
  process.exitCode = 1;
});
