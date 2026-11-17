import { expect, test, type Page } from '@playwright/test';

// C13-04.M04.01: sesión de plataforma SINTÉTICA sólo para visualización, no JWT real.
const url = '/plataforma/inventario-rutas-v19';
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function sesion(page: Page, status = 200) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1304-m0401-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    await route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Synthetic', rolNombre: 'Plataforma',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'SECRET_TENANT_B_PRIVATE_DSN' }) });
  });
}

test.describe('C13-04.M04.01 catálogo Router real y 16 programas', () => {
  test.describe.configure({ retries: 0 });

  test('anonimo y 403 no acceden al inventario privado', async ({ page }) => {
    await page.goto(url);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
    await sesion(page, 403);
    await page.goto(url);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
    await expect(page.getByText('SECRET_TENANT_B_PRIVATE_DSN')).toHaveCount(0);
  });

  test('16 programas del PLAN y todas las rutas de config sin enlaces con :id', async ({ page }) => {
    await sesion(page);
    const writes: string[] = [];
    page.on('request', r => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
    });
    await page.goto(url);
    await expect(page.getByRole('heading', { name: 'Inventario de programas y rutas declaradas' })).toBeVisible();
    await expect(page.locator('.programas li')).toHaveCount(16);
    expect(await page.locator('.rutas li').count()).toBeGreaterThanOrEqual(108);
    const param = page.locator('.rutas li').filter({ hasText: 'clientes/:id/editar' });
    await expect(param).toHaveCount(1);
    await expect(param.locator('a')).toHaveCount(0);
    await expect(page.getByText('SIN_VERIFICAR').first()).toBeVisible();
    expect(writes).toEqual([]);
  });

  test('320,375,768,1280 sin overflow ni enlaces remotos o inventados', async ({ page }) => {
    await sesion(page);
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 812 });
      await page.goto(url);
      await expect(page.getByTestId('c1304-route-catalogo')).toBeVisible();
      const widthOverflow = await page.evaluate(() =>
        document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(widthOverflow, 'overflow at ' + width).toBeLessThanOrEqual(2);
      const links = await page.locator('.rutas a').evaluateAll(nodes =>
        nodes.map(el => (el as HTMLAnchorElement).getAttribute('href') || ''));
      expect(links.length).toBeGreaterThan(0);
      expect(links.every(link => link.startsWith('/') && !link.includes(':') && !link.includes('*'))).toBe(true);
    }
  });

  test('Tab verdadero alcanza regreso, foco visible y objetivo minimo de 44 px', async ({ page }) => {
    await sesion(page);
    await page.goto(url);
    const link = page.getByRole('link', { name: 'Volver a consola global' });
    let reached = false;
    for (let n = 0; n < 110; n++) {
      await page.keyboard.press('Tab');
      if (await link.evaluate(el => document.activeElement === el)) {
        reached = true; break;
      }
    }
    expect(reached).toBe(true);
    const focus = await link.evaluate(el => {
      const rect = el.getBoundingClientRect(), style = getComputedStyle(el);
      return { width: rect.width, height: rect.height,
        outline: Number.parseFloat(style.outlineWidth), outlineStyle: style.outlineStyle };
    });
    expect(focus.width).toBeGreaterThanOrEqual(44);
    expect(focus.height).toBeGreaterThanOrEqual(44);
    expect(focus.outlineStyle).not.toBe('none');
    expect(focus.outline).toBeGreaterThanOrEqual(2);
  });
});
