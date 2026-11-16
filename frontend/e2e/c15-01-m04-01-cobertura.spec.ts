import { expect, test, type Page } from '@playwright/test';

// Sesión sintética de plataforma. No acredita JWT/tenant backend real.
async function session(page: Page, allow: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c15-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = { 'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS' };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await route.fulfill({ status: allow ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allow ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'PRIVATE_DO_NOT_EXPOSE' }) });
  });
}

test('C15-01.M04.01 acceso anónimo o 403 no muestra tabla interna', async ({ page }) => {
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.coverage-list li')).toHaveCount(0);
  await session(page, false);
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.getByText('PRIVATE_DO_NOT_EXPOSE')).toHaveCount(0);
});

test('C15-01.M04.01 vista autorizada 320/1280 sin scroll y sin escrituras', async ({ page }) => {
  await session(page, true);
  for (const width of [320, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/plataforma/trazabilidad-cobertura-v5');
    await expect(page.getByRole('heading', { name: 'Revisión de cobertura del plan' })).toBeVisible();
    await expect(page.locator('.coverage-list li')).toHaveCount(4);
    await expect(page.getByText('NO_MEDIDO')).toBeVisible();
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow).toBeLessThanOrEqual(2);
    await expect(page.locator('.coverage-shell form, .coverage-shell button, .coverage-shell input, .coverage-shell textarea')).toHaveCount(0);
  }
});

test('C15-01.M04.01 teclado permite volver con foco visible', async ({ page }) => {
  await session(page, true);
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  const back = page.getByRole('link', { name: 'Regresar a administración de plataforma' });
  await back.focus();
  await expect(back).toBeFocused();
  const outline = await back.evaluate(el => parseFloat(getComputedStyle(el).outlineWidth));
  expect(outline).toBeGreaterThanOrEqual(3);
  await back.click();
  await expect(page).toHaveURL(/\/plataforma(?:\/|$)/);
});
