import { expect, test, type Page } from '@playwright/test';

/** C15-03.M04.04: UI de auditoría sin operaciones; API de sesión deliberadamente simulada. */
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
async function mockPlatform(page: Page, allowed: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-only-not-a-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
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
      status: allowed ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allowed
        ? { success: true, data: { nombreUsuario: 'Sintetico',
            rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'NO_AUTHORITY' })
    });
  });
}
test('sin sesión: ruta y mensajes privados denegados sin botones de acción', async ({ page }) => {
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Fronteras de aislamiento y evidencia' })).toHaveCount(0);
  await expect(page.locator('.tenancy-audit')).toHaveCount(0);
});
test('403 de sesión: no filtra contenido ni link de navegación de plataforma', async ({ page }) => {
  await mockPlatform(page, false);
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.tenancy-audit')).toHaveCount(0);
});
test('grant simulado: mensajes de límites, link único y superficie no operativa', async ({ page }) => {
  await mockPlatform(page, true);
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page.getByRole('heading', { name: 'Fronteras de aislamiento y evidencia' })).toBeVisible();
  await expect(page.locator('.tenancy-audit [aria-label="Alcance de solo lectura"]')).toBeVisible();
  await expect(page.getByText('Estado de pentest, resultados A/B y métricas: NO_MEDIDO. QA/PROD no autorizados.')).toBeVisible();
  await expect(page.locator('.tenancy-audit button, .tenancy-audit form, .tenancy-audit input, .tenancy-audit textarea')).toHaveCount(0);
  await expect(page.locator('.tenancy-audit a[routerlink="/plataforma"]')).toHaveCount(0);
  const back = page.getByRole('link', { name: 'Regresar a administración de plataforma' });
  await expect(back).toHaveCount(1);
  await expect(back).toHaveAttribute('href', '/plataforma');
});
test('doble navegación es inofensiva: cero mutaciones de auditoría, backup, restore o jobs', async ({ page }) => {
  await mockPlatform(page, true);
  const dangerous: string[] = [];
  page.on('request', request => {
    if (/\/(?:backup|restore|security-audit|jobs\/dispatch|tenant-grants)(?:\/|\?|$)/i.test(request.url())
        && !['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
      dangerous.push(request.method() + ':' + request.url());
    }
  });
  for (let attempt = 0; attempt < 2; attempt++) {
    await page.goto('/plataforma/auditoria-tenancy-v5');
    const back = page.getByRole('link', { name: 'Regresar a administración de plataforma' });
    await expect(back).toBeVisible();
    await back.click();
    await expect(page).toHaveURL(/\/plataforma(?:\?|$)/);
  }
  expect(dangerous).toEqual([]);
});
