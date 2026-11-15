import { expect, test, type Page } from '@playwright/test';

/** C15-05.M04.03 — Chromium UI real; respuesta de sesión sintética, nunca JWT/backend real. */
async function prepararSesion(page: Page, permitida = true): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c15-05-branding-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') { await route.fulfill({ status: 204, headers }); return; }
    await route.fulfill({
      status: permitida ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(permitida
        ? { success: true, data: { nombreUsuario: 'Prueba', rolNombre: 'Plataforma',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'DENEGADO', data: null })
    });
  });
}
test('C15-05.M04.03 muestra branding institucional solo bajo sesión sintética autorizada', async ({ page }) => {
  await prepararSesion(page);
  await page.goto('/plataforma/auditoria-operativa-v5');
  const brand = page.getByRole('region', { name: 'Identidad visual de auditoría' });
  await expect(brand).toBeVisible();
  await expect(brand).toContainText('SOLQARYN · Auditoría operativa');
  await expect(brand.locator('form, input, button, img')).toHaveCount(0);
});
test('C15-05.M04.03 anónimo no accede al branding de la ruta protegida', async ({ page }) => {
  await page.goto('/plataforma/auditoria-operativa-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('region', { name: 'Identidad visual de auditoría' })).toHaveCount(0);
});
test('C15-05.M04.03 denegación 403 de sesión sintética oculta la identidad', async ({ page }) => {
  await prepararSesion(page, false);
  await page.goto('/plataforma/auditoria-operativa-v5');
  await expect(page.getByRole('region', { name: 'Identidad visual de auditoría' })).toHaveCount(0);
});
test('C15-05.M04.03 identidad legible en 390 y 1280 px sin overflow', async ({ page }) => {
  await prepararSesion(page);
  for (const width of [390, 1280]) {
    await page.setViewportSize({ width, height: 844 });
    await page.goto('/plataforma/auditoria-operativa-v5');
    await expect(page.getByRole('region', { name: 'Identidad visual de auditoría' })).toBeVisible();
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow).toBeLessThanOrEqual(2);
  }
});
