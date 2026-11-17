import { expect, test, type Page } from '@playwright/test';

const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function fixture(page: Page, status: 200 | 403 | 503) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C0804_M0506_SYNTHETIC_NOT_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
    localStorage.setItem('sector_capabilities_enabled', 'true');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    await route.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { usuarioId: 7, empresaId: 71, rolId: 3,
            rolNombre: 'Lectura', esAdministrador: false } }
        : { success: false, message: 'C0804_PRIVATE_PROVIDER_DIAGNOSTIC_NEVER_EXPOSE' })
    });
  });
}
test.describe('C08-04.M05.06 regresion sectorial Chromium', () => {
  test.describe.configure({ retries: 0 });
  test('responsive positivo sin escrituras ni llamadas a modulos', async ({ page }) => {
    await fixture(page, 200);
    const writes: string[] = [], modules: string[] = [];
    page.on('request', req => {
      const path = new URL(req.url()).pathname;
      if (['POST','PUT','PATCH','DELETE'].includes(req.method())) writes.push(path);
      if (/\/(?:sector-modules|modulos-sectoriales|capabilities)(?:\/|$)/i.test(path)) modules.push(path);
    });
    for (const width of [320,375,768,1280]) {
      await page.setViewportSize({ width, height: 820 });
      await page.goto('/extensiones-sectoriales');
      const main = page.locator('main.sector-readonly');
      await expect(main).toBeVisible();
      await expect(main.getByRole('heading', { name: 'Capacidades por rubro' })).toBeVisible();
      await expect(main.locator('.sector-card h3')).toHaveText(['Reservas','Membresías','Catálogo','Sitios por rubro']);
      await expect(main.locator('.sector-tag')).toHaveText(['No conectado','No conectado','No conectado','No conectado']);
      await expect(main.getByRole('status', { name: 'Estado de las capacidades' })).toContainText('Sin módulos conectados');
      const bounds = await main.evaluate(root => {
        const cards = [...root.querySelectorAll('.sector-card')];
        return {
          overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
          right: Math.max(...cards.map(el => el.getBoundingClientRect().right)),
          viewport: document.documentElement.clientWidth
        };
      });
      expect(bounds.overflow,'horizontal overflow at ' + width).toBeLessThanOrEqual(2);
      expect(bounds.right,'cards outside viewport at ' + width).toBeLessThanOrEqual(bounds.viewport + 2);
      await expect(main.locator('form,input,button,[aria-pressed]')).toHaveCount(0);
    }
    expect(writes).toEqual([]);
    expect(modules).toEqual([]);
  });
  test('teclado Tab alcanza enlace y foco visible', async ({ page }) => {
    await fixture(page, 200);
    await page.goto('/extensiones-sectoriales');
    const link = page.getByRole('navigation', { name: 'Navegación de extensiones sectoriales' })
      .getByRole('link', { name: 'Volver al panel' });
    await expect(link).toBeVisible();
    let reached = false;
    for (let i = 0; i < 60; i++) {
      await page.keyboard.press('Tab');
      if (await link.evaluate(el => document.activeElement === el)) { reached = true; break; }
    }
    expect(reached,'Tab from initial focus without .focus()').toBe(true);
    const focus = await link.evaluate(el => {
      const css = getComputedStyle(el), rect = el.getBoundingClientRect();
      return { style: css.outlineStyle, width: parseFloat(css.outlineWidth), height: rect.height };
    });
    expect(focus.style).not.toBe('none');
    expect(focus.width).toBeGreaterThanOrEqual(3);
    expect(focus.height).toBeGreaterThanOrEqual(44);
  });
  for (const status of [403,503] as const) {
    test('denegacion por estado ' + status + ' sin diagnostico privado', async ({ page }) => {
      await fixture(page, status);
      const writes: string[] = [];
      page.on('request', req => {
        if (['POST','PUT','PATCH','DELETE'].includes(req.method())) writes.push(req.url());
      });
      await page.goto('/extensiones-sectoriales');
      await expect(page).toHaveURL(/\/login(?:\?|$)/);
      await expect(page.locator('.sector-readonly,.sector-card')).toHaveCount(0);
      await expect(page.getByText('C0804_PRIVATE_PROVIDER_DIAGNOSTIC_NEVER_EXPOSE')).toHaveCount(0);
      expect(writes).toEqual([]);
    });
  }
});
