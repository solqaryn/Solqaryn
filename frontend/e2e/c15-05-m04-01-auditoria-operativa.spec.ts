import { expect, test, type Page } from '@playwright/test';

/** C15-05.M04.01: Chromium UI aislado. Mocks NO prueban JWT/RBAC del backend. */
const sessionPath = /\/plataforma\/sesion(?:\?|$)/;
async function tokenSintetico(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'v17-ui-test-no-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}
async function sesionSimulada(page: Page, permitir: boolean) {
  await page.route(sessionPath, async route => {
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
      status: permitir ? 200 : 403, contentType: 'application/json',
      headers,
      body: JSON.stringify(permitir ? {
      success: true, data: { nombreUsuario: 'Sintético', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true }
    } : { success: false, message: 'Sin permiso', data: null })
    });
  });
}
test('anónimo no abre auditoría ni muestra controles privados', async ({ page }) => {
  await page.goto('/plataforma/auditoria-operativa-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Controles de ejecución y evidencia' })).toHaveCount(0);
});
test('403 simulado deniega y nunca expone datos institucionales', async ({ page }) => {
  await tokenSintetico(page);
  await sesionSimulada(page, false);
  await page.goto('/plataforma/auditoria-operativa-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.controls li')).toHaveCount(0);
});
test('viewport móvil y teclado muestran solo guía estática para sesión simulada', async ({ page }) => {
  await tokenSintetico(page);
  await sesionSimulada(page, true);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/plataforma/auditoria-operativa-v5');
  await expect(page.getByRole('heading', { name: 'Controles de ejecución y evidencia' })).toBeVisible();
  await expect(page.locator('.controls li')).toHaveCount(4);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(2);
  const back = page.getByRole('link', { name: 'Regresar a administración de plataforma' });
  await back.focus();
  await expect(back).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma(?:$|[?#])/);
});
