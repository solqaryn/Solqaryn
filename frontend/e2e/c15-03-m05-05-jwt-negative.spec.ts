import { expect, test, type Page } from '@playwright/test';

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
  test('sucursales: consulta positiva A/B y rechazo de filtro extranjero', async ({ page, request }) => {
    for (const [usuario, propia, ajena] of [
      ['c1504_admin_a', 1, 2], ['c1504_admin_b', 2, 1]
    ] as const) {
      const jwt = await ingresar(page, usuario);
      const headers = { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': String(propia) };
      const own = await request.get(api + '/sucursales/activas?empresaId=' + propia, { headers });
      expect(own.status(), 'consulta sucursal propia debe autorizarse').toBe(200);
      const foreign = await request.get(api + '/sucursales/activas?empresaId=' + ajena, { headers });
      expect([403, 404], 'no enumerar sucursal tenant ajeno').toContain(foreign.status());
    }
  });

  test('recurso tenant y archivo: A no lee ni sube logo para B', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_a');
    const headers = { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '1' };
    const own = await request.get(api + '/empresa-configuracion/tenant/1', { headers });
    expect(own.status(), 'configuracion propia es accesible').toBe(200);
    const foreign = await request.get(api + '/empresa-configuracion/tenant/2', { headers });
    expect([403, 404], 'configuracion de B prohibida').toContain(foreign.status());
    // Solo API y MySQL efimeros; no contactar storage ni modificar archivo externo.
    const denied = await request.post(api + '/empresa-configuracion/tenant/2/logo', {
      headers, multipart: { logo: { name: 'test.png', mimeType: 'image/png',
        buffer: Buffer.from('CI-only-invalid-png') } }
    });
    expect([403, 404], 'archivo de otro tenant denegado antes de storage').toContain(denied.status());
  });

  test('job/control plane: admin empresa B no lee evidencias QA globales', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_b');
    const headers = { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '2' };
    const denied = await request.get(api +
      '/plataforma/gestion/qa/evidencias/ab70b055b3c8bb10b11a5088b5e82d69e74bdbc4', { headers });
    expect(denied.status(), 'sin autoridad global no hay acceso al job SHA').toBe(403);
    const global = await request.get(api + '/plataforma/sesion', { headers });
    expect(global.status(), 'empresa B no es plataforma').toBe(403);
  });
});
