import { expect, test, type Page } from '@playwright/test';

const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function sesionSintetica(page: Page, permitir: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0804-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    if (!permitir) return route.fulfill({ status: 403, headers: cors, contentType: 'application/json', body: JSON.stringify({ success: false }) });
    return route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: { usuarioId: 7, empresaId: 71, rolId: 3, rolNombre: 'Lectura', esAdministrador: false } }) });
  });
}

test('C08-04.M04.01 anónimo rechazado por guard', async ({ page }) => {
  await page.goto('/extensiones-sectoriales');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.sector-card')).toHaveCount(0);
});

test('C08-04.M04.01 tenant sintético 403 no expone superficie', async ({ page }) => {
  await sesionSintetica(page, false);
  await page.goto('/extensiones-sectoriales');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.sector-card')).toHaveCount(0);
});

test('C08-04.M04.01 vista informativa a 320, 768 y 1280, sin consultar módulos', async ({ page }) => {
  await sesionSintetica(page, true);
  const peticionesModulos: string[] = [];
  page.on('request', request => {
    if (/\/(?:capabilities|sector-modules|modulos-sectoriales)(?:\/|\?|$)/i.test(new URL(request.url()).pathname)) {
      peticionesModulos.push(request.url());
    }
  });
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/extensiones-sectoriales');
    await expect(page.getByRole('heading', { name: 'Capacidades por rubro' })).toBeVisible();
    await expect(page.locator('.sector-card')).toHaveCount(4);
    await expect(page.getByRole('status', { name: 'Estado de las capacidades' })).toContainText('Sin módulos conectados');
    await expect(page.locator('.sector-readonly form, .sector-readonly button, .sector-readonly input')).toHaveCount(0);
    const bounds = await page.locator('.sector-readonly').evaluate(el => {
      const rect = el.getBoundingClientRect();
      return { left: rect.left, right: rect.right, viewport: document.documentElement.clientWidth };
    });
    expect(bounds.left).toBeGreaterThanOrEqual(-2);
    expect(bounds.right).toBeLessThanOrEqual(bounds.viewport + 2);
  }
  expect(peticionesModulos).toHaveLength(0);
});
