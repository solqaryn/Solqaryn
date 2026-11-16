import { expect, test, type Page } from '@playwright/test';

const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function session(page: Page, permitted: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0804-m0504-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({
      status: permitted ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(permitted
        ? { success: true, data: { usuarioId: 7, empresaId: 71, rolId: 3,
            rolNombre: 'Lectura', esAdministrador: false } }
        : { success: false, message: 'DO_NOT_EXPOSE_PRIVATE_SECTOR_DEBUG' })
    });
  });
}

test('C08-04.M05.04: cuatro capacidades ilustrativas y no activadas en 320/1280', async ({ page }) => {
  await session(page, true);
  const mutations: string[] = [];
  const moduleRequests: string[] = [];
  page.on('request', request => {
    const path = new URL(request.url()).pathname;
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
      mutations.push(path);
    }
    if (/\/(?:sector-modules|capabilities|modulos-sectoriales)(?:\/|$)/i.test(path)) {
      moduleRequests.push(path);
    }
  });
  for (const width of [320, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/extensiones-sectoriales');
    const view = page.locator('main.sector-readonly');
    await expect(page.getByRole('heading', { name: 'Capacidades por rubro' })).toBeVisible();
    await expect(view.locator('.sector-card h3')).toHaveText([
      'Reservas', 'Membresías', 'Catálogo', 'Sitios por rubro'
    ]);
    await expect(view.locator('.sector-card .sector-tag')).toHaveText([
      'No conectado', 'No conectado', 'No conectado', 'No conectado'
    ]);
    await expect(page.getByRole('heading', { name: 'Ámbitos previstos, no autorizaciones' })).toBeVisible();
    await expect(view.locator('[aria-label="Estado de las capacidades"]')).toContainText('Sin módulos conectados');
    await expect(view.locator('form,input,button,[aria-pressed]')).toHaveCount(0);
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth
    );
    expect(overflow).toBeLessThanOrEqual(2);
  }
  expect(mutations, 'vista informativa no debe emitir escrituras HTTP').toEqual([]);
  expect(moduleRequests, 'vista informativa no debe invocar API de módulos').toEqual([]);
});

test('C08-04.M05.04: 403 sintético impide aceptación y oculta diagnóstico', async ({ page }) => {
  await session(page, false);
  await page.goto('/extensiones-sectoriales');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.sector-card,.sector-grid')).toHaveCount(0);
  await expect(page.getByText('DO_NOT_EXPOSE_PRIVATE_SECTOR_DEBUG')).toHaveCount(0);
});
