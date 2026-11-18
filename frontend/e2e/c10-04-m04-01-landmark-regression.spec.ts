import { expect, test, type Page } from '@playwright/test';

/** C10-04.M04.01 A5_FAIL P2: Chromium real, backend/JWT sinteticos.
 * No certifica HTTP firmado, aislamiento A/B, MySQL, backups o restore.
 */
const path = '/plataforma/continuidad-operativa-v5';
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function fixtureSession(page: Page, currentStatus: () => number): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c10-m0401-a5-not-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    const status = currentStatus();
    await route.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Prueba sintetica', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'TENANT_PRIVATE_SECRET_NEVER_RENDER' })
    });
  });
}
test.describe('C10-04.M04.01 - regresion A5 P2 de landmark main', () => {
  test.describe.configure({ retries: 0 });
  test('anonimo y 403 no montan pantalla privada ni main interno', async ({ page }) => {
    await page.goto(path);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('section.continuidad')).toHaveCount(0);
    await fixtureSession(page, () => 403);
    await page.goto(path);
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('section.continuidad')).toHaveCount(0);
    await expect(page.getByText('TENANT_PRIVATE_SECRET_NEVER_RENDER')).toHaveCount(0);
  });
  test('sesion sintetica: exactamente un main del shell y seccion interna', async ({ page }) => {
    await fixtureSession(page, () => 200);
    await page.goto(path);
    await expect(page.getByRole('heading', { name: 'Controles de recuperación y aislamiento' })).toBeVisible();
    await expect(page.locator('main')).toHaveCount(1);
    await expect(page.locator('main#main-content')).toHaveCount(1);
    await expect(page.locator('main main, section.continuidad main')).toHaveCount(0);
    await expect(page.locator('main#main-content section.continuidad')).toHaveAttribute('aria-labelledby', 'continuidad-title');
    await expect(page.locator('section.continuidad ol.controles li')).toHaveCount(4);
    await expect(page.locator('section.continuidad form, section.continuidad input')).toHaveCount(0);
  });
  test('320px: Tab real y Enter revalidan sin desbordamiento ni grant nuevo', async ({ page }) => {
    await fixtureSession(page, () => 200);
    await page.setViewportSize({ width: 320, height: 740 });
    await page.goto(path);
    const button = page.getByRole('button', { name: 'Revalidar sesión de continuidad' });
    await expect(button).toBeVisible();
    let reached = false;
    for (let i = 0; i < 120; i++) {
      await page.keyboard.press('Tab');
      if (await button.evaluate(el => document.activeElement === el)) {
        reached = true;
        break;
      }
    }
    expect(reached, 'Revalidacion debe alcanzarse con Tab real').toBe(true);
    const outline = await button.evaluate(el => {
      const s = getComputedStyle(el);
      return { width: parseFloat(s.outlineWidth), style: s.outlineStyle };
    });
    expect(outline.style).toBe('solid');
    expect(outline.width).toBeGreaterThanOrEqual(3);
    await page.keyboard.press('Enter');
    await expect(page.locator('section.continuidad ol.controles li')).toHaveCount(4);
    await expect(page.locator('main')).toHaveCount(1);
    const horizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth
    );
    expect(horizontalOverflow).toBeLessThanOrEqual(2);
  });
  test('revocacion 403 posterior oculta controles y diagnostico', async ({ page }) => {
    let status = 200;
    await fixtureSession(page, () => status);
    await page.goto(path);
    await expect(page.locator('section.continuidad ol.controles li')).toHaveCount(4);
    status = 403;
    await page.getByRole('button', { name: 'Revalidar sesión de continuidad' }).click();
    await expect(page.locator('section.continuidad ol.controles li')).toHaveCount(0);
    await expect(page.getByText('TENANT_PRIVATE_SECRET_NEVER_RENDER')).toHaveCount(0);
  });
});
