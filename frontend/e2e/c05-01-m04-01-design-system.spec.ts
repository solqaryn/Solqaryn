import { expect, test, type Page } from '@playwright/test';

/** API y token sintéticos; no certifican JWT real ni datos productivos. */
async function mockSession(page: Page, allow: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c05-01-m04-01-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    return route.fulfill({ status: allow ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allow ?
        { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } } :
        { success: false, data: null, message: 'PRIVATE_DESIGN_TOKEN_SECRET' }) });
  });
}

test('sin token no se expone la biblioteca de plataforma', async ({ page }) => {
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.design-catalog .token-item')).toHaveCount(0);
});

test('403 sintético deniega tokens privados sin filtrar el mensaje de proveedor', async ({ page }) => {
  await mockSession(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.design-catalog .token-item')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_DESIGN_TOKEN_SECRET')).toHaveCount(0);
});

test('biblioteca read-only abre por teclado y es responsive en 320/768/1280', async ({ page }) => {
  await mockSession(page, true);
  await page.goto('/plataforma');
  const summary = page.locator('details.design-system-preview summary');
  await expect(summary).toBeVisible();
  await summary.focus();
  await page.keyboard.press('Enter');
  const catalog = page.locator('.design-catalog');
  await expect(catalog.getByRole('heading', { name: 'Tokens y componentes de referencia' })).toBeVisible();
  await expect(catalog.locator('.token-item')).toHaveCount(4);
  await expect(catalog.locator('form, input, button')).toHaveCount(0);
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(catalog.locator('.token-item')).toHaveCount(4);
    const result = await page.evaluate(() => ({
      overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
      visibleColors: document.querySelectorAll('.design-catalog .swatch').length
    }));
    expect(result.visibleColors).toBe(4);
    expect(result.overflow, 'sin overflow a ' + width).toBeLessThanOrEqual(2);
  }
});
