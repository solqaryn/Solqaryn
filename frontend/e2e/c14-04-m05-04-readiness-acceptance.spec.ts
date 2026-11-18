import { expect, test, type Page } from '@playwright/test';

/** C14-04.M05.04: Chromium y sesion sintetica; NO prueba JWT/PROD. */
async function accesoSimulado(page: Page): Promise<() => number> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'm0504-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let gets = 0;
  const headers = {
    'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
    'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
    'Access-Control-Allow-Methods': 'GET, OPTIONS'
  };
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    gets++;
    await route.fulfill({
      status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        nombreUsuario: 'Synthetic', rolNombre: 'Admin',
        ambito: 'Plataforma', esAdministrador: true
      } })
    });
  });
  return () => gets;
}

test('C14-04.M05.04 positivo: cuatro puertas no certificadas, sin accion', async ({ page }) => {
  const count = await accesoSimulado(page);
  await page.goto('/plataforma/flujo-v5');
  const panel = page.getByTestId('release-readiness-contract');
  await expect(panel.getByRole('heading', { name: 'Preparación de salida · evidencia pendiente' })).toBeVisible();
  await expect(panel.getByRole('list', { name: 'Puertas de preparación no certificadas' })).toBeVisible();
  await expect(panel.locator('ol li')).toHaveCount(4);
  await expect(panel.getByText('SIN_VERIFICAR')).toHaveCount(3);
  await expect(panel.getByText('NO_MEDIDO')).toHaveCount(1);
  await expect(page.getByTestId('release-readiness-feedback')).toHaveAttribute('data-state', 'exito');
  await expect(page.getByTestId('release-empty')).toHaveAttribute('data-evidence-state', 'vacio');
  await expect(panel.locator('form, input, select, textarea, button')).toHaveCount(0);
  await expect(panel).not.toContainText('PASS');
  expect(count()).toBe(1);
});

test('C14-04.M05.04 positivo: revalidacion no inventa PASS ni cambia evidencia', async ({ page }) => {
  const count = await accesoSimulado(page);
  await page.goto('/plataforma/flujo-v5');
  await expect(page.getByTestId('release-readiness-contract')).toBeVisible();
  await page.getByRole('button', { name: /Revalidar sesión/ }).click();
  await expect(page.getByTestId('release-readiness-feedback')).toHaveAttribute('data-state', 'exito');
  const panel = page.getByTestId('release-readiness-contract');
  await expect(panel.locator('ol li')).toHaveCount(4);
  await expect(panel.getByText('NO_MEDIDO')).toHaveCount(1);
  await expect(panel).not.toContainText('PASS');
  await expect(panel.locator('form, button, input')).toHaveCount(0);
  expect(count()).toBe(2);
});

test('C14-04.M05.04 positivo: sin overflow a 320/768/1280', async ({ page }) => {
  await accesoSimulado(page);
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/plataforma/flujo-v5');
    const panel = page.getByTestId('release-readiness-contract');
    await expect(panel.getByRole('heading', { name: 'Preparación de salida · evidencia pendiente' })).toBeVisible();
    await expect(panel.locator('ol li')).toHaveCount(4);
    await expect(panel).not.toContainText('PASS');
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'overflow at ' + width).toBeLessThanOrEqual(2);
  }
});
