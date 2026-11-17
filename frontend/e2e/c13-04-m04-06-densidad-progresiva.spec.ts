import { expect, test, type Page } from '@playwright/test';
const url = '/plataforma/inventario-rutas-v19';
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = { 'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS' };
const allowed = { success: true, data: { nombreUsuario: 'Catalogo Test',
  rolNombre: 'Administrador', ambito: 'Plataforma', esAdministrador: true } };
async function token(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1304-m0406-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}
async function session(page: Page, code: number): Promise<void> {
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    await route.fulfill({ status: code, headers: cors, contentType: 'application/json',
      body: JSON.stringify(code === 200 ? allowed :
        { success: false, message: 'TENANT_B_PRIVATE_KEY_NOT_FOR_UI' }) });
  });
}
test.describe('C13-04.M04.06 densidad, carga progresiva y ligereza', () => {
  test.describe.configure({ retries: 0 });
  test('16 programas, 108 rutas, reflow 320/375/768/1280 sin overflow', async ({ page }) => {
    await token(page);
    await session(page, 200);
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 812 });
      await page.goto(url);
      await expect(page.getByTestId('c1304-route-catalogo')).toBeVisible();
      await expect(page.locator('.programas li')).toHaveCount(16);
      expect(await page.locator('.rutas li').count()).toBeGreaterThanOrEqual(108);
      const dims = await page.evaluate(() => ({
        overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
        columns: getComputedStyle(document.querySelector('.rutas')!).gridTemplateColumns.split(' ').length
      }));
      expect(dims.overflow).toBeLessThanOrEqual(2);
      expect(dims.columns).toBe(width <= 768 ? 1 : 2);
    }
  });
  test('espera GET de sesion sin revelar inventario y lo presenta al responder', async ({ page }) => {
    await token(page);
    let release!: () => void;
    const hold = new Promise<void>(resolve => { release = resolve; });
    await page.route(sessionUrl, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors }); return;
      }
      await hold;
      await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
        body: JSON.stringify(allowed) });
    });
    try {
      await page.goto(url, { waitUntil: 'domcontentloaded' });
      await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
      release();
      await expect(page.getByTestId('c1304-route-catalogo')).toBeVisible();
      await expect(page.locator('.programas li')).toHaveCount(16);
    } finally { release(); }
  });
  test('401,403,503 sin contenido privado, sin secretos ni escrituras', async ({ page }) => {
    await token(page);
    let status = 401;
    const writes: string[] = [];
    page.on('request', request => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
    });
    await page.route(sessionUrl, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors }); return;
      }
      await route.fulfill({ status, headers: cors, contentType: 'application/json',
        body: JSON.stringify({ success: false, message: 'TENANT_B_PRIVATE_KEY_NOT_FOR_UI' }) });
    });
    for (const code of [401, 403, 503]) {
      status = code;
      await page.goto(url);
      await expect(page).toHaveURL(/\/login/);
      await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
      await expect(page.getByText('TENANT_B_PRIVATE_KEY_NOT_FOR_UI')).toHaveCount(0);
    }
    expect(writes).toEqual([]);
  });
  test('Tab real, foco visible y objetivo minimo 44 px', async ({ page }) => {
    await token(page);
    await session(page, 200);
    await page.goto(url);
    const link = page.getByRole('link', { name: 'Volver a consola global' });
    await expect(link).toBeVisible();
    let reached = false;
    for (let n = 0; n < 110; n++) {
      await page.keyboard.press('Tab');
      if (await link.evaluate(el => document.activeElement === el)) {
        reached = true; break;
      }
    }
    expect(reached).toBe(true);
    const metrics = await link.evaluate(el => {
      const r = el.getBoundingClientRect(), style = getComputedStyle(el);
      return { width: r.width, height: r.height, outline: parseFloat(style.outlineWidth),
        outlineStyle: style.outlineStyle };
    });
    expect(metrics.width).toBeGreaterThanOrEqual(44);
    expect(metrics.height).toBeGreaterThanOrEqual(44);
    expect(metrics.outlineStyle).not.toBe('none');
    expect(metrics.outline).toBeGreaterThanOrEqual(2);
  });
});
