import { expect, test, type Page } from '@playwright/test';

/** C14-04.M04.01: sesión HTTP simulada; no certifica JWT, DNS, R2 ni PROD. */
async function mockPlatform(page: Page, status: 200 | 403 | 503): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'readiness-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
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
      status, headers, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'READINESS_PRIVATE_DIAGNOSTIC' })
    });
  });
}

test('C14-04.M04.01: anónimo redirigido sin datos readiness', async ({ page }) => {
  await page.goto('/plataforma/flujo-v5');
  await expect(page).toHaveURL(/\/login(?:\?.*)?$/);
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
});

test('C14-04.M04.01: 403 sintético denegado sin filtrar diagnóstico', async ({ page }) => {
  await mockPlatform(page, 403);
  await page.goto('/plataforma/flujo-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
  await expect(page.getByText('READINESS_PRIVATE_DIAGNOSTIC')).toHaveCount(0);
});

test('C14-04.M04.01: 503 fail-closed, no certifica ninguna puerta', async ({ page }) => {
  await mockPlatform(page, 503);
  await page.goto('/plataforma/flujo-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=no-disponible/);
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
  await expect(page.getByText('READINESS_PRIVATE_DIAGNOSTIC')).toHaveCount(0);
});

test('C14-04.M04.01: autoridad sintética visualiza solo contrato y ningún PASS', async ({ page }) => {
  await mockPlatform(page, 200);
  await page.setViewportSize({ width: 320, height: 720 });
  await page.goto('/plataforma/flujo-v5');
  const panel = page.getByTestId('release-readiness-contract');
  await expect(panel.getByRole('heading', { name: 'Preparación de salida · evidencia pendiente' })).toBeVisible();
  await expect(panel.locator('ol li')).toHaveCount(4);
  await expect(panel.getByText('SIN_VERIFICAR')).toHaveCount(3);
  await expect(panel.getByText('NO_MEDIDO')).toHaveCount(1);
  await expect(panel.locator('button, form, input')).toHaveCount(0);
  await expect(panel).not.toContainText('PASS');
  await expect(panel).not.toContainText('READINESS_PRIVATE_DIAGNOSTIC');
  expect(await page.evaluate(() => document.documentElement.scrollWidth -
    document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  await page.setViewportSize({ width: 1280, height: 800 });
  await expect(panel.getByRole('heading', { name: 'Preparación de salida · evidencia pendiente' })).toBeVisible();
});
