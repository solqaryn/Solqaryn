import { expect, test, type Page } from '@playwright/test';

/** Browser sintético C15-03.M04.01, no verifica JWT/DB/tenancy del backend. */
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
async function fakeToken(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c15-03-browser-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}
async function sessionMock(page: Page, allow: boolean) {
  await page.route(sessionUrl, async route => {
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
      status: allow ? 200 : 403, contentType: 'application/json', headers,
      body: JSON.stringify(allow ? {
        success: true,
        data: { nombreUsuario: 'UI sintética', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true }
      } : { success: false, data: null, message: 'PRIVADO_NO_RENDERIZAR' })
    });
  });
}
test('sin token no navega a superficie privada', async ({ page }) => {
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Fronteras de aislamiento y evidencia' })).toHaveCount(0);
});
test('403 sintético mantiene ruta denegada sin guía', async ({ page }) => {
  await fakeToken(page);
  await sessionMock(page, false);
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.fronteras li')).toHaveCount(0);
});
test('grant simulado muestra guía estática, responsive y sin endpoints operativos', async ({ page }) => {
  await fakeToken(page);
  await sessionMock(page, true);
  await page.setViewportSize({ width: 390, height: 844 });
  const forbidden: string[] = [];
  await page.route(/\/(?:backup|restore|security-audit|jobs\/dispatch)(?:\/|\?|$)/i, route => {
    forbidden.push(route.request().url());
    return route.abort();
  });
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page.getByRole('heading', { name: 'Fronteras de aislamiento y evidencia' })).toBeVisible();
  await expect(page.locator('.fronteras li')).toHaveCount(4);
  await expect(page.getByText('Estado de pentest, resultados A/B y métricas: NO_MEDIDO. QA/PROD no autorizados.')).toBeVisible();
  expect(forbidden).toEqual([]);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(2);
});
