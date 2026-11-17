import { expect, test, type Page } from '@playwright/test';

// C15-04.M05.05: escenarios adversariales en Angular real con HTTP sintético.
// NO certifican JWT firmado, RBAC/tenant server-side ni API real.
const session = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function tokenSintetico(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1504m0505-token-sintetico-no-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}
function registrarEscrituras(page: Page) {
  const writes: string[] = [];
  page.on('request', req => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(req.method())) writes.push(req.method());
  });
  return writes;
}
async function respuestaSesion(page: Page, status: number, data?: object) {
  let gets = 0;
  await page.route(session, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    gets++;
    await route.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify({
        success: status === 200,
        data: data ?? null,
        message: status === 200 ? '' : 'PRIVATE_PROVIDER_DSN_NEVER_RENDER'
      })
    });
  });
  return () => gets;
}
async function esperarDenegacion(page: Page, writes: string[]) {
  await expect(page).toHaveURL(/\/login/);
  await expect(page.locator('[data-testid="ux-audit-v19"]')).toHaveCount(0);
  await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_PROVIDER_DSN_NEVER_RENDER')).toHaveCount(0);
  expect(writes).toEqual([]);
}

test.describe('C15-04.M05.05 acceso negativo y aislamiento frontend', () => {
  test.describe.configure({ retries: 0 });

  test('sin autenticación rechaza ruta antes de mostrar tarjetas privadas', async ({ page }) => {
    const writes = registrarEscrituras(page);
    await page.goto('/plataforma/auditoria-ux-v5');
    await esperarDenegacion(page, writes);
  });

  test('401 tras token local sintético no permite reusar una sesión privada', async ({ page }) => {
    await tokenSintetico(page);
    const writes = registrarEscrituras(page);
    const gets = await respuestaSesion(page, 401);
    await page.goto('/plataforma/auditoria-ux-v5');
    await esperarDenegacion(page, writes);
    expect(gets()).toBe(1);
  });

  test('403 explícito permanece fail-closed y jamás renderiza mensaje del proveedor', async ({ page }) => {
    await tokenSintetico(page);
    const writes = registrarEscrituras(page);
    const gets = await respuestaSesion(page, 403);
    await page.goto('/plataforma/auditoria-ux-v5');
    await esperarDenegacion(page, writes);
    expect(gets()).toBe(1);
  });

  test('200 con sesión de empresa no equivale a administrador de plataforma', async ({ page }) => {
    await tokenSintetico(page);
    const writes = registrarEscrituras(page);
    const gets = await respuestaSesion(page, 200, {
      nombreUsuario: 'Empresa sintética',
      rolNombre: 'Administrador',
      ambito: 'Empresa',
      esAdministrador: true
    });
    await page.goto('/plataforma/auditoria-ux-v5');
    await esperarDenegacion(page, writes);
    expect(gets()).toBe(1);
  });
});
