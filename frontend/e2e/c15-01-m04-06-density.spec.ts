import { expect, test, type Page } from '@playwright/test';

// C15-01.M04.06: navegador Angular real, sesion sintetica; no acredita JWT HTTP.
const target = '/plataforma/trazabilidad-cobertura-v5';
const sessionPattern = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function token(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1501-m0406-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}
const ok = () => JSON.stringify({ success: true, data: {
  nombreUsuario: 'Admin Synthetic', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
} });
test.describe('C15-01.M04.06 densidad y carga segura', () => {
  test.describe.configure({ retries: 0 });
  test('320/375/768/1280: cuatro dimensiones, reflow sin escrituras', async ({ page }) => {
    await token(page);
    await page.route(sessionPattern, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors }); return;
      }
      await route.fulfill({ status: 200, headers: cors, contentType: 'application/json', body: ok() });
    });
    const writes: string[] = [];
    page.on('request', req => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(req.method())) writes.push(req.method());
    });
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 812 });
      await page.goto(target);
      await expect(page.getByTestId('c1501-loaded')).toBeVisible();
      await expect(page.locator('.coverage-list li')).toHaveCount(4);
      await expect(page.locator('.coverage-shell [role="status"]')).toContainText('NO_MEDIDO');
      await expect(page.locator('.coverage-shell img, .coverage-shell iframe, .coverage-shell video')).toHaveCount(0);
      const columns = await page.locator('.coverage-list').evaluate(el =>
        getComputedStyle(el).gridTemplateColumns.split(' ').filter(Boolean).length);
      expect(columns, 'columnas en ' + width + 'px').toBe(width <= 720 ? 1 : 2);
      const overflow = await page.evaluate(() =>
        document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(overflow, 'overflow ' + width + 'px').toBeLessThanOrEqual(2);
    }
    expect(writes).toEqual([]);
  });
  test('sesion pendiente oculta dimensiones hasta autorizacion', async ({ page }) => {
    await token(page);
    let release!: () => void;
    const hold = new Promise<void>(resolve => { release = resolve; });
    let calls = 0;
    await page.route(sessionPattern, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors }); return;
      }
      calls++;
      await hold;
      await route.fulfill({ status: 200, headers: cors, contentType: 'application/json', body: ok() });
    });
    try {
      await page.goto(target, { waitUntil: 'domcontentloaded' });
      await expect.poll(() => calls).toBeGreaterThan(0);
      await expect(page.locator('.coverage-list li')).toHaveCount(0);
      await expect(page.getByTestId('c1501-loaded')).toHaveCount(0);
      release();
      await expect(page.getByTestId('c1501-loaded')).toBeVisible();
      await expect(page.locator('.coverage-list li')).toHaveCount(4);
    } finally { release(); }
  });
  test('403 y 503 rechazan acceso y nunca filtran detalle', async ({ browser }) => {
    for (const status of [403, 503]) {
      const page = await browser.newPage();
      try {
        await token(page);
        await page.route(sessionPattern, async route => {
          if (route.request().method() === 'OPTIONS') {
            await route.fulfill({ status: 204, headers: cors }); return;
          }
          await route.fulfill({ status, headers: cors, contentType: 'application/json',
            body: JSON.stringify({ success: false, message: 'TENANT_B_PRIVATE_NO_RENDER' }) });
        });
        await page.goto(target);
        await expect(page).toHaveURL(/\/login/);
        await expect(page.locator('.coverage-list li')).toHaveCount(0);
        await expect(page.getByText('TENANT_B_PRIVATE_NO_RENDER')).toHaveCount(0);
      } finally { await page.close(); }
    }
  });
});
