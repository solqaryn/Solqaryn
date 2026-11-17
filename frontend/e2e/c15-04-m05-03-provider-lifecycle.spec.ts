import { expect, test, type Page } from '@playwright/test';

// C15-04.M05.03: integración real de ruta/servicio Angular con proveedor HTTP SIMULADO.
// No equivale a persistencia MySQL, JWT firmado, RBAC o tenant backend real.
const session = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const valid = {
  success: true, data: {
    nombreUsuario: 'PRIVATE_PROFILE_SENTINEL_DO_NOT_PERSIST',
    rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true
  }
};
async function prepararToken(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1504m0503-sintetico-no-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}
function vigilarEscrituras(page: Page) {
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  return writes;
}
async function proveedorSecuencial(page: Page, afterStatus: number) {
  let count = 0;
  await page.route(session, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    count++;
    const status = count === 1 ? 200 : afterStatus;
    await route.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200 ? valid : {
        success: false, data: null, message: 'PRIVATE_DSN_NEVER_RENDER'
      })
    });
  });
  return () => count;
}
async function noPrivado(page: Page, writes: string[]) {
  await expect(page).toHaveURL(/\/login/);
  await expect(page.locator('[data-testid="ux-audit-v19"]')).toHaveCount(0);
  await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_DSN_NEVER_RENDER')).toHaveCount(0);
  expect(writes).toEqual([]);
}
test.describe('C15-04.M05.03 integración UI con proveedor de sesión sintético', () => {
  test.describe.configure({ retries: 0 });

  test('una autorización previa se revoca en siguiente GET 403 sin retener datos privados', async ({ page }) => {
    await prepararToken(page);
    const writes = vigilarEscrituras(page);
    const requests = await proveedorSecuencial(page, 403);
    await page.goto('/plataforma/auditoria-ux-v5');
    await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(5);
    const persisted = await page.evaluate(() =>
      [...Object.values(localStorage), ...Object.values(sessionStorage)].join('|')
    );
    expect(persisted).not.toContain('PRIVATE_PROFILE_SENTINEL_DO_NOT_PERSIST');
    await page.reload();
    await noPrivado(page, writes);
    expect(requests()).toBe(2);
  });

  test('503 posterior a autorización destruye la vista y no muestra detalles del proveedor', async ({ page }) => {
    await prepararToken(page);
    const writes = vigilarEscrituras(page);
    const requests = await proveedorSecuencial(page, 503);
    await page.goto('/plataforma/auditoria-ux-v5');
    await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(5);
    await page.reload();
    await noPrivado(page, writes);
    expect(requests()).toBe(2);
  });

  test('HTTP 200 success=false con datos de autoridad no crea una sesión válida', async ({ page }) => {
    await prepararToken(page);
    const writes = vigilarEscrituras(page);
    let requests = 0;
    await page.route(session, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors }); return;
      }
      requests++;
      await route.fulfill({
        status: 200, headers: cors, contentType: 'application/json',
        body: JSON.stringify({ ...valid, success: false })
      });
    });
    await page.goto('/plataforma/auditoria-ux-v5');
    await noPrivado(page, writes);
    expect(requests).toBe(1);
  });
});
