import { expect, test, type Page } from '@playwright/test';

/** C10-04.M04.02: Chromium con backend sintetico; NO acredita JWT/MySQL real. */
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function tokenSintetico(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'v17-continuidad-test-not-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}
async function mockSession(page: Page, status: () => number) {
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    const code = status();
    await route.fulfill({
      status: code, headers: cors, contentType: 'application/json',
      body: JSON.stringify(code === 200 ? {
        success: true, data: { nombreUsuario: 'UI sintética', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true }
      } : { success: false, data: null, message: 'PRIVADO_NO_RENDERIZABLE' })
    });
  });
}
test.describe('C10-04.M04.02 - estados privados, Chromium real, API sintetica', () => {
  test('sin JWT no dibuja contenido ni controles', async ({ page }) => {
    await page.goto('/plataforma/continuidad-operativa-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Controles de recuperación y aislamiento' })).toHaveCount(0);
  });
  test('403 con token sintetico bloquea navegacion protegida', async ({ page }) => {
    await tokenSintetico(page);
    await mockSession(page, () => 403);
    await page.goto('/plataforma/continuidad-operativa-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('.controles li')).toHaveCount(0);
  });
  test('sesion sintetica permite solo guia, 503 posterior oculta y no revela detalles', async ({ page }) => {
    await tokenSintetico(page);
    let status = 200;
    await mockSession(page, () => status);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/plataforma/continuidad-operativa-v5');
    await expect(page.getByRole('heading', { name: 'Controles de recuperación y aislamiento' })).toBeVisible();
    await expect(page.locator('.controles li')).toHaveCount(4);
    status = 503;
    await page.getByRole('button', { name: 'Revalidar sesión de continuidad' }).click();
    await expect(page.getByText('No fue posible validar la sesión. El acceso sigue restringido.')).toBeVisible();
    await expect(page.locator('.controles li')).toHaveCount(0);
    await expect(page.getByText('PRIVADO_NO_RENDERIZABLE')).toHaveCount(0);
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow).toBeLessThanOrEqual(2);
  });
});
