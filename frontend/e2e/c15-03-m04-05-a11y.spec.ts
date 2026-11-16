import { expect, test, type Page } from '@playwright/test';
// C15-03.M04.05: browser real; sesion y token sinteticos, no JWT/tenant backend.
const url = /\/plataforma\/sesion(?:\?|$)/;
async function grant(page: Page, allowed: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-browser-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(url, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({
      status: allowed ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allowed ? {
        success: true, data: {
          nombreUsuario: 'Sintetico', rolNombre: 'Admin',
          ambito: 'Plataforma', esAdministrador: true
        }
      } : { success: false, data: null, message: 'DENEGADO' })
    });
  });
}
test('anonimo sin token no ve atajo ni matriz', async ({ page }) => {
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('link', { name: 'Saltar a los cuatro frentes de revisión' })).toHaveCount(0);
});
test('403 sintetico deniega matriz y atajo', async ({ page }) => {
  await grant(page, false);
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.fronteras li')).toHaveCount(0);
});
test('grant simulado: foco teclado, etiquetas, enlace y ausencia de acciones', async ({ page }) => {
  await grant(page, true);
  await page.goto('/plataforma/auditoria-tenancy-v5');
  const skip = page.getByRole('link', { name: 'Saltar a los cuatro frentes de revisión' });
  await expect(skip).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Cuatro frentes de comprobación futura' })).toHaveAttribute('tabindex', '-1');
  await expect(page.locator('ol.fronteras')).toHaveAttribute('aria-labelledby', 'fronteras-title');
  await expect(page.locator('.fronteras li')).toHaveCount(4);
  await expect(page.locator('button, form, input, textarea')).toHaveCount(0);
  await skip.focus();
  await page.keyboard.press('Tab');
  await page.keyboard.press('Shift+Tab');
  await expect(skip).toBeFocused();
  const outline = await skip.evaluate(el => {
    const css = getComputedStyle(el);
    return { width: parseFloat(css.outlineWidth), style: css.outlineStyle };
  });
  expect(outline.style).toBe('solid');
  expect(outline.width).toBeGreaterThanOrEqual(3);
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/#fronteras-title$/);
});
test('320/768 px: sin overflow y con etiquetas visibles', async ({ page }) => {
  await grant(page, true);
  for (const width of [320, 768]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/plataforma/auditoria-tenancy-v5');
    await expect(page.locator('.fronteras li')).toHaveCount(4);
    await expect(page.getByRole('link', { name: 'Regresar a administración de plataforma' })).toBeVisible();
    expect(await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  }
});
