import { expect, test, type Page } from '@playwright/test';

/** API y token sintéticos; NO evidencia de autenticación productiva ni SLO. */
async function mockSession(page: Page, allow: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c11-04-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await route.fulfill({ status: allow ? 200 : 403, contentType: 'application/json', headers,
      body: JSON.stringify(allow ?
        { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } } :
        { success: false, data: null, message: 'PRIVATE_TELEMETRY_SECRET' }) });
  });
}
test('ruta de observabilidad es privada para anónimo', async ({ page }) => {
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.obs-points li')).toHaveCount(0);
});
test('403 sintético no expone contenido ni diagnóstico', async ({ page }) => {
  await mockSession(page, false);
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.obs-points li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_TELEMETRY_SECRET')).toHaveCount(0);
});
test('vista sintética read-only, móvil y sin exporter ni consultas operativas', async ({ page }) => {
  await mockSession(page, true);
  await page.setViewportSize({ width: 375, height: 812 });
  const blocked: string[] = [];
  await page.route(/\/(?:telemetry|metrics\/export|traces|alerts\/dispatch)(?:\/|\?|$)/i, route => {
    blocked.push(route.request().url());
    return route.abort();
  });
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page.getByRole('heading', { name: 'Diagnóstico y señales seguras' })).toBeVisible();
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  await expect(page.getByText(/proveedor de telemetría: NO_MEDIDO/)).toBeVisible();
  await expect(page.locator('.observability-guide button, .observability-guide form, .observability-guide input')).toHaveCount(0);
  expect(blocked).toEqual([]);
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
});
