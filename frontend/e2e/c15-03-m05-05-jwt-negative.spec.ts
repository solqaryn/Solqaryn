import { expect, test, type Page, type APIResponse } from '@playwright/test';

/** C15-03.M05.05 R2: pruebas NEGATIVAS con JWT firmado, API real y MySQL 8.4 efimero.
 * Sin page.route ni tokens inventados. No acredita pentest completo, archivos, jobs ni PROD. */
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('C15-03.M05.05: solo CI efimera con JWT y MySQL reales');
}
async function ingresar(page: Page, usuario: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(usuario);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const response = page.waitForResponse(r =>
    r.url() === 'http://localhost:5005/auth/login' && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const actual = await response;
  expect(actual.status()).toBe(200);
  const jwt: unknown = (await actual.json())?.data?.token;
  expect(typeof jwt).toBe('string');
  expect((jwt as string).split('.')).toHaveLength(3);
  return jwt as string;
}
function observarMutaciones(page: Page) {
  const mutations: string[] = [];
  page.on('request', request => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method()))
      mutations.push(request.method() + ' ' + request.url());
  });
  return mutations;
}

async function exigirCuarentena(response: APIResponse): Promise<void> {
  expect(response.status(), 'cuarentena P0 multiempresa, no autorización 403').toBe(503);
  const body: unknown = await response.json();
  expect(body).toMatchObject({ success: false, code: 'TENANT_DATA_ISOLATION_REQUIRED' });
}
test.describe('C15-03.M05.05 — negativa JWT y tenancy HTTP real', () => {
  test.describe.configure({ retries: 0 });

  test('anonimo no consulta tenant ni puede entrar en guia privada', async ({ page, request }) => {
    const resp = await request.get(api + '/tenant-context/1', { headers: { 'X-Empresa-Id': '1' } });
    expect([401, 403]).toContain(resp.status());
    const platform = await request.get(api + '/plataforma/sesion');
    expect([401, 403]).toContain(platform.status());
    const mutations = observarMutaciones(page);
    await page.goto('/plataforma/auditoria-tenancy-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('section.tenancy-audit')).toHaveCount(0);
    expect(mutations).toEqual([]);
  });

  test('firma JWT adulterada se rechaza en API y navegador privado', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_platform_only');
    const initial = await request.get(api + '/plataforma/sesion',
      { headers: { Authorization: 'Bearer ' + jwt } });
    expect(initial.status()).toBe(200);
    const parts = jwt.split('.');
    const sig = parts[2];
    expect(sig.length).toBeGreaterThan(10);
    const bad = [parts[0], parts[1], (sig[0] === 'A' ? 'B' : 'A') + sig.slice(1)].join('.');
    const response = await request.get(api + '/plataforma/sesion',
      { headers: { Authorization: 'Bearer ' + bad } });
    expect([401, 403]).toContain(response.status());
    await page.evaluate(token => localStorage.setItem('solqaryn_token', token), bad);
    const mutations = observarMutaciones(page);
    await page.goto('/plataforma/auditoria-tenancy-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('section.tenancy-audit')).toHaveCount(0);
    expect(mutations).toEqual([]);
  });

  test('A: suplantar X-Empresa-Id B no habilita lectura B o consola global', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_a');
    const auth = { Authorization: 'Bearer ' + jwt };
    const own = await request.get(api + '/tenant-context/1',
      { headers: { ...auth, 'X-Empresa-Id': '1' } });
    expect(own.status()).toBe(200);
    expect((await own.json())?.data?.empresaId).toBe(1);
    const spoof = await request.get(api + '/tenant-context/2',
      { headers: { ...auth, 'X-Empresa-Id': '2' } });
    expect([403, 404]).toContain(spoof.status());
    const denied = await request.get(api + '/plataforma/sesion',
      { headers: { ...auth, 'X-Empresa-Id': '2' } });
    expect(denied.status()).toBe(403);
    const mutations = observarMutaciones(page);
    await page.goto('/plataforma/auditoria-tenancy-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('section.tenancy-audit')).toHaveCount(0);
    expect(mutations).toEqual([]);
  });

  test('B: suplantar X-Empresa-Id A no habilita lectura A o consola global', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_b');
    const auth = { Authorization: 'Bearer ' + jwt };
    const own = await request.get(api + '/tenant-context/2',
      { headers: { ...auth, 'X-Empresa-Id': '2' } });
    expect(own.status()).toBe(200);
    expect((await own.json())?.data?.empresaId).toBe(2);
    const spoof = await request.get(api + '/tenant-context/1',
      { headers: { ...auth, 'X-Empresa-Id': '1' } });
    expect([403, 404]).toContain(spoof.status());
    const denied = await request.get(api + '/plataforma/sesion',
      { headers: { ...auth, 'X-Empresa-Id': '1' } });
    expect(denied.status()).toBe(403);
    const mutations = observarMutaciones(page);
    await page.goto('/plataforma/auditoria-tenancy-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('section.tenancy-audit')).toHaveCount(0);
    expect(mutations).toEqual([]);
  });

  test('rol empresa no escala a plataforma por encabezado de tenant arbitrario', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_a');
    const suspiciousHeaders = {
      Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '999999'
    };
    const platform = await request.get(api + '/plataforma/sesion',
      { headers: suspiciousHeaders });
    expect(platform.status()).toBe(403);
    const other = await request.get(api + '/tenant-context/999999',
      { headers: suspiciousHeaders });
    expect([403, 404]).toContain(other.status());
    const mutations = observarMutaciones(page);
    await page.goto('/plataforma/auditoria-tenancy-v5');
    await expect(page.locator('section.tenancy-audit')).toHaveCount(0);
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    expect(mutations).toEqual([]);
  });
  // Estas rutas siguen en cuarentena P0 multiempresa: 503 TIPADO no es 403 ni PASS de ownership.
  // El positivo real permitido se prueba con tenant-context. Nunca relajar la cuarentena.
  test('sucursales: JWT A/B positivo en tenant-context y cuarentena comercial exacta', async ({ browser, request }) => {
    for (const [usuario, propia, ajena] of [
      ['c1504_admin_a', 1, 2], ['c1504_admin_b', 2, 1]
    ] as const) {
      const context = await browser.newContext();
      try {
        const page = await context.newPage();
        const jwt = await ingresar(page, usuario);
        const headers = { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': String(propia) };
        const ownTenant = await request.get(api + '/tenant-context/' + propia, { headers });
        expect(ownTenant.status()).toBe(200);
        expect((await ownTenant.json())?.data?.empresaId).toBe(propia);
        const crossTenant = await request.get(api + '/tenant-context/' + ajena, { headers });
        expect([403, 404]).toContain(crossTenant.status());
        await exigirCuarentena(await request.get(api + '/sucursales/activas?empresaId=' + propia, { headers }));
        await exigirCuarentena(await request.get(api + '/sucursales/activas?empresaId=' + ajena, { headers }));
      } finally {
        await context.close();
      }
    }
  });

  test('configuracion tenant-first: lectura propia autorizada, B denegada y logo ajeno en cuarentena', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_a');
    const headers = { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '1' };
    expect((await request.get(api + '/tenant-context/1', { headers })).status()).toBe(200);
    expect([403, 404]).toContain((await request.get(api + '/tenant-context/2', { headers })).status());
    // V22 permite exclusivamente GET privado del tenant verificado con JWT, membresia y Configuracion.Ver.
    // Un 200 propio es esperado; no afirmar que ese acceso es un bypass P0.
    const ownBrand = await request.get(api + '/empresa-configuracion/tenant/1', { headers });
    expect(ownBrand.status(), 'branding propio autenticado por servidor y MySQL').toBe(200);
    expect((await ownBrand.json())?.data?.empresaId, 'respuesta atada a Empresa 1').toBe(1);
    expect(ownBrand.headers()['cache-control'] ?? '', 'no guardar marca privada en caches').toContain('no-store');
    const foreignBrand = await request.get(api + '/empresa-configuracion/tenant/2', { headers });
    expect([403, 404], 'nunca entregar marca de Empresa 2 a Empresa 1').toContain(foreignBrand.status());
    // Mutador multipart extranjero permanece bloqueado por P0, no simular grant con 503.
    const denied = await request.post(api + '/empresa-configuracion/tenant/2/logo', {
      headers, multipart: { logo: { name: 'test.png', mimeType: 'image/png',
        buffer: Buffer.from('CI-only-invalid-png') } }
    });
    await exigirCuarentena(denied);
  });

  test('control plane QA: admin empresa B no eleva rol y middleware conserva cuarentena', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_b');
    const headers = { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '2' };
    const global = await request.get(api + '/plataforma/sesion', { headers });
    expect(global.status(), 'empresa B no es plataforma').toBe(403);
    const denied = await request.get(api +
      '/plataforma/gestion/qa/evidencias/ab70b055b3c8bb10b11a5088b5e82d69e74bdbc4', { headers });
    await exigirCuarentena(denied);
  });
  test('JWT alg none y payload reescrito no autorizan recursos ni consola', async ({ page, request }) => {
    const signed = await ingresar(page, 'c1504_admin_a');
    const parts = signed.split('.');
    expect(parts).toHaveLength(3);
    const ownHeaders = { Authorization: 'Bearer ' + signed, 'X-Empresa-Id': '1' };
    const positive = await request.get(api + '/tenant-context/1', { headers: ownHeaders });
    expect(positive.status(), 'JWT original debe ser aceptado antes de las mutaciones').toBe(200);
    const unsignedHeader = Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url');
    const withoutSignature = [unsignedHeader, parts[1], ''].join('.');
    const claims = JSON.parse(Buffer.from(parts[1]!, 'base64url').toString('utf8')) as Record<string, unknown>;
    const changedClaims = { ...claims, exp: Number(claims['exp'] ?? 0) + 86401 };
    const forgedPayload = Buffer.from(JSON.stringify(changedClaims)).toString('base64url');
    const editedPayload = [parts[0], forgedPayload, parts[2]].join('.');
    for (const forged of [withoutSignature, editedPayload]) {
      for (const tenant of [1, 2]) {
        const response = await request.get(api + '/tenant-context/' + tenant, {
          headers: { Authorization: 'Bearer ' + forged, 'X-Empresa-Id': String(tenant) }
        });
        expect([401, 403], 'JWT sin firma valida nunca abre tenant ' + tenant).toContain(response.status());
      }
      const platform = await request.get(api + '/plataforma/sesion', {
        headers: { Authorization: 'Bearer ' + forged, 'X-Empresa-Id': '2' }
      });
      expect([401, 403], 'JWT manipulado nunca concede rol plataforma').toContain(platform.status());
    }
    await page.evaluate(token => localStorage.setItem('solqaryn_token', token), withoutSignature);
    const mutations = observarMutaciones(page);
    await page.goto('/plataforma/auditoria-tenancy-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('section.tenancy-audit')).toHaveCount(0);
    expect(mutations).toEqual([]);
  });


  test('revocacion de pertenencia en MySQL sin renovar JWT deniega tenant B', async ({ page, request }) => {
    // Frontera R2 en GitHub-hosted: JWT previamente emitido y MySQL 8.4 efimero; sin mocks.
    const jwt = await ingresar(page, 'c1504_admin_b');
    const headers = { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '2' };
    const before = await request.get(api + '/tenant-context/2', { headers });
    expect(before.status(), 'control positivo de membresia B').toBe(200);
    expect((await before.json())?.data?.empresaId).toBe(2);
    const { execFileSync } = await import('node:child_process');
    const container = execFileSync('docker',
      ['ps', '--filter', 'ancestor=mysql:8.4', '--format', '{{.ID}}'], { encoding: 'utf8' })
      .trim().split(/\r?\n/)[0];
    expect(container, 'contenedor MySQL 8.4 aislado').toMatch(/^[0-9a-f]{12,64}$/);
    const setMembership = (active: 0 | 1): void => {
      const sql = 'UPDATE UsuarioEmpresas ue JOIN Usuarios u ON u.Id=ue.UsuarioId ' +
        'SET ue.Activa=' + active + ', ue.FechaActualizacion=UTC_TIMESTAMP(6) ' +
        "WHERE u.NombreUsuario='c1504_admin_b' AND ue.EmpresaId=2;" +
        'SELECT ue.Activa FROM UsuarioEmpresas ue JOIN Usuarios u ON u.Id=ue.UsuarioId ' +
        "WHERE u.NombreUsuario='c1504_admin_b' AND ue.EmpresaId=2;";
      const state = execFileSync('docker',
        ['exec', '-i', container!, 'mysql', '-uroot', '-proot',
          'solqaryn_c1504_real_ci', '-Nse', sql], { encoding: 'utf8' }).trim();
      expect(state, 'readback SQL Activa=' + active).toBe(String(active));
    };
    try {
      setMembership(0);
      const revoked = await request.get(api + '/tenant-context/2', { headers });
      expect([401, 403, 404], 'JWT anterior a revocacion nunca concede B').toContain(revoked.status());
      const cross = await request.get(api + '/tenant-context/1', {
        headers: { ...headers, 'X-Empresa-Id': '1' }
      });
      expect([403, 404], 'revocacion tampoco concede A').toContain(cross.status());
    } finally {
      setMembership(1); // Rollback de datos CI aun cuando la asercion falle.
    }
    const restored = await request.get(api + '/tenant-context/2', { headers });
    expect(restored.status(), 'control positivo de restauracion CI').toBe(200);
  });

});
