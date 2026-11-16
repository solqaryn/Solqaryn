import { expect, test, type Page } from '@playwright/test';

const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function tenantFixture(page: Page, empresa: 71 | 72, status: 200 | 401 | 403 | 503) {
  await page.addInitScript((requested: number) => {
    localStorage.setItem('solqaryn_token', 'C0804_TEST_FIXTURE_NOT_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', String(requested));
    localStorage.setItem('sector_capabilities_enabled', 'true');
  }, empresa);
  await page.route(/\/tenant-context\/(?:71|72)(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    const requested = Number(new URL(route.request().url()).pathname.split('/').at(-1));
    const finalStatus = requested === empresa ? status : 403;
    await route.fulfill({
      status: finalStatus, headers: cors, contentType: 'application/json',
      body: JSON.stringify(finalStatus === 200
        ? { success: true, data: { usuarioId: 7, empresaId: empresa, rolId: 3, rolNombre: 'Lectura', esAdministrador: false } }
        : { success: false, message: 'INTERNAL_PROVIDER_DIAGNOSTIC_NEVER_SHOW' })
    });
  });
}

test.describe('C08-04.M05.05 abuso y aislamiento UX (HTTP simulado, Chromium real)', () => {
  test.describe.configure({ retries: 0 });

  test('anonimo no abre ruta privada ni ve capacidades', async ({ page }) => {
    await page.goto('/extensiones-sectoriales');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('.sector-readonly, .sector-card')).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Capacidades por rubro' })).toHaveCount(0);
  });

  test('401 de tenant-context deniega ruta y no expone el diagnostico', async ({ page }) => {
    await tenantFixture(page, 71, 401);
    await page.goto('/extensiones-sectoriales');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('.sector-card')).toHaveCount(0);
    await expect(page.getByText('INTERNAL_PROVIDER_DIAGNOSTIC_NEVER_SHOW')).toHaveCount(0);
  });

  test('empresa B denegada 403 no hereda vista de empresa A', async ({ page }) => {
    await tenantFixture(page, 72, 403);
    await page.goto('/extensiones-sectoriales');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('.sector-readonly, .sector-card')).toHaveCount(0);
    await expect(page.getByText('INTERNAL_PROVIDER_DIAGNOSTIC_NEVER_SHOW')).toHaveCount(0);
  });

  test('503 de contexto no concede autorizacion ni expone detalles internos', async ({ page }) => {
    await tenantFixture(page, 71, 503);
    await page.goto('/extensiones-sectoriales');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('.sector-card')).toHaveCount(0);
    await expect(page.getByText('INTERNAL_PROVIDER_DIAGNOSTIC_NEVER_SHOW')).toHaveCount(0);
  });

  test('bandera local maliciosa no activa modulos ni solicita escrituras', async ({ page }) => {
    await tenantFixture(page, 71, 200);
    const mutations: string[] = [];
    const moduleRequests: string[] = [];
    page.on('request', request => {
      const path = new URL(request.url()).pathname;
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) mutations.push(path);
      if (/\/(?:sector-modules|capabilities|modulos-sectoriales)(?:\/|$)/i.test(path)) moduleRequests.push(path);
    });
    await page.goto('/extensiones-sectoriales');
    await expect(page.getByRole('heading', { name: 'Capacidades por rubro' })).toBeVisible();
    const cards = page.locator('.sector-readonly .sector-card');
    await expect(cards).toHaveCount(4);
    expect(await cards.locator('h3').allTextContents()).toEqual([
      'Reservas', 'Membresías', 'Catálogo', 'Sitios por rubro'
    ]);
    await expect(cards.locator('.sector-tag')).toHaveText([
      'No conectado', 'No conectado', 'No conectado', 'No conectado'
    ]);
    await expect(page.locator('.sector-readonly form, .sector-readonly input, .sector-readonly button')).toHaveCount(0);
    await expect(page.locator('.sector-readonly')).toContainText('No muestra activaciones');
    expect(mutations).toEqual([]);
    expect(moduleRequests).toEqual([]);
  });
});
