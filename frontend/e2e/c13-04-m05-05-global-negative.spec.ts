import { expect, test, type Page } from '@playwright/test';

// R2 TEST ONLY. Angular/Chromium real, session HTTP mock. No real JWT/MySQL/tenant authorization.
const routePath = '/plataforma/inventario-rutas-v19';
const sessionEndpoint = /\/plataforma\/sesion(?:\?|$)/;
const secret = 'SYNTHETIC_PRIVATE_TENANT_B_DSN';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function setupToken(page: Page, tenantId = 101, expired = false) {
  await page.addInitScript(({ tenantId, expired }) => {
    localStorage.setItem('solqaryn_token', 'c1304-m0505-not-jwt');
    localStorage.setItem('solqaryn_expira_en', expired ? '2000-01-01T00:00:00Z' : '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_rol', 'Administrador');
    localStorage.setItem('solqaryn_empresa_solicitada_id', String(tenantId));
  }, { tenantId, expired });
}
async function fakeSession(page: Page, status: number, body: object) {
  let count = 0;
  const tenantHeaders: string[] = [];
  await page.route(sessionEndpoint, async route => {
    const r = route.request();
    if (r.method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    count++;
    tenantHeaders.push(r.headers()['x-empresa-id'] ?? '');
    await route.fulfill({ status, headers: cors, contentType: 'application/json', body: JSON.stringify(body) });
  });
  return { count: () => count, tenantHeaders };
}
async function denied(page: Page) {
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
  await expect(page.getByText(secret)).toHaveCount(0);
}
test.describe('C13-04.M05.05 seguridad UI negativa', () => {
  test.describe.configure({ retries: 0 });
  test.use({ extraHTTPHeaders: {} });
  test('anonimo no accede ni solicita sesion', async ({ page }) => {
    const s = await fakeSession(page, 200, { success: true, data: { ambito: 'Plataforma', esAdministrador: true } });
    await page.goto(routePath);
    await denied(page);
    expect(s.count()).toBe(0);
  });
  test('token vencido no accede ni solicita sesion', async ({ page }) => {
    await setupToken(page, 101, true);
    const s = await fakeSession(page, 200, { success: true, data: { ambito: 'Plataforma', esAdministrador: true } });
    await page.goto(routePath);
    await denied(page);
    expect(s.count()).toBe(0);
  });
  for (const tenantId of [101, 202]) {
    test('admin Empresa sintetica ' + tenantId + ' no suplanta Plataforma', async ({ page }) => {
      await setupToken(page, tenantId);
      const writes: string[] = [];
      page.on('request', r => { if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method()); });
      const s = await fakeSession(page, 200, {
        success: true, data: { nombreUsuario: secret, rolNombre: 'Administrador',
          ambito: 'Empresa', empresaId: tenantId, esAdministrador: true }, message: secret
      });
      await page.goto(routePath);
      await expect(page).toHaveURL(/\/login\?plataforma=no-disponible(?:&|$)/);
      await denied(page);
      expect(s.count()).toBe(1);
      expect(s.tenantHeaders).toEqual(['']);
      expect(writes).toEqual([]);
    });
  }
  for (const status of [401, 403, 503] as const) {
    test('status HTTP simulado ' + status + ' no divulga diagnostico', async ({ page }) => {
      await setupToken(page);
      const s = await fakeSession(page, status, { success: false, data: null, message: secret });
      await page.goto(routePath);
      await expect(page).toHaveURL(status === 401 ? /\/login$/
        : status === 403 ? /\/login\?plataforma=denegado(?:&|$)/
          : /\/login\?plataforma=no-disponible(?:&|$)/);
      await denied(page);
      expect(s.count()).toBe(1);
    });
  }
  test('success=false con data privilegiada no autoriza', async ({ page }) => {
    await setupToken(page);
    await fakeSession(page, 200, { success: false,
      data: { nombreUsuario: secret, ambito: 'Plataforma', esAdministrador: true }, message: secret });
    await page.goto(routePath);
    await denied(page);
  });
  test('control positivo de Plataforma no enumera enlaces dinamicos ni escribe', async ({ page }) => {
    await setupToken(page);
    const writes: string[] = [];
    page.on('request', r => { if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method()); });
    const s = await fakeSession(page, 200, { success: true,
      data: { nombreUsuario: 'Synthetic Platform', rolNombre: 'Plataforma',
        ambito: 'Plataforma', esAdministrador: true } });
    await page.goto(routePath);
    await expect(page.getByTestId('c1304-route-catalogo')).toBeVisible();
    const params = page.locator('.rutas li').filter({ hasText: 'clientes/:id/editar' });
    expect(await params.count()).toBeGreaterThanOrEqual(1);
    await expect(params.locator('a')).toHaveCount(0);
    const hrefs = await page.locator('.rutas a').evaluateAll(nodes =>
      nodes.map(n => (n as HTMLAnchorElement).getAttribute('href') ?? ''));
    expect(hrefs.length).toBeGreaterThan(0);
    expect(hrefs.every(h => h.startsWith('/') && !h.startsWith('//') &&
      !h.includes(':') && !h.includes('*'))).toBe(true);
    expect(writes).toEqual([]);
    expect(s.count()).toBe(1);
  });
});
