import { expect, test, type Page } from '@playwright/test';

// Fixture sintética: no valida JWT HTTP real, MFA, permisos en backend ni MySQL.
const sesionUrl = /\/plataforma\/sesion(?:\?|$)/;
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function preparar(page: Page, acceso = true, contador?: { llamadas: number }) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0204-m0403-not-a-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sesionUrl, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    if (contador) contador.llamadas++;
    return route.fulfill({
      status: acceso ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(acceso
        ? { success: true, data: { nombreUsuario: 'Test', rolNombre: 'Global',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'SENSITIVE_SECRET_NOT_FOR_BROWSER' })
    });
  });
}

test('C02-04.M04.03 visitante anónimo no ve branding privado', async ({ page }) => {
  await page.goto('/plataforma/consola-global-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByTestId('c0204-branding')).toHaveCount(0);
});

test('C02-04.M04.03 acceso 403 rechaza branding y detalle sensible', async ({ page }) => {
  await preparar(page, false);
  await page.goto('/plataforma/consola-global-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.getByTestId('c0204-branding')).toHaveCount(0);
  await expect(page.getByText('SENSITIVE_SECRET_NOT_FOR_BROWSER')).toHaveCount(0);
});

test('C02-04.M04.03 sólo dos marcas; sin HTTP adicional o doble efecto', async ({ page }) => {
  const contador = { llamadas: 0 };
  await preparar(page, true, contador);
  await page.goto('/plataforma/consola-global-v5');
  const grupo = page.getByTestId('c0204-branding');
  await expect(grupo).toBeVisible();
  await expect(grupo.locator('button')).toHaveCount(2);
  const normal = page.getByTestId('c0204-brand-institucional');
  const alto = page.getByTestId('c0204-brand-contraste');
  await expect(normal).toHaveAttribute('aria-pressed', 'true');
  const anterior = contador.llamadas;
  await alto.click();
  await expect(grupo).toHaveClass(/control-brand--contrast/);
  await expect(alto).toHaveAttribute('aria-pressed', 'true');
  await alto.click();
  await expect(alto).toHaveAttribute('aria-pressed', 'true');
  await normal.click();
  await expect(grupo).not.toHaveClass(/control-brand--contrast/);
  await expect(normal).toHaveAttribute('aria-pressed', 'true');
  expect(contador.llamadas).toBe(anterior);
});

test('C02-04.M04.03 responsive 320/375/768/1280 y activación por teclado', async ({ page }) => {
  await preparar(page);
  await page.goto('/plataforma/consola-global-v5');
  const grupo = page.getByTestId('c0204-branding');
  await expect(grupo).toBeVisible();
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(grupo).toBeVisible();
    expect(await grupo.evaluate(el => el.scrollWidth - el.clientWidth)).toBeLessThanOrEqual(2);
  }
  const alto = page.getByTestId('c0204-brand-contraste');
  await alto.focus();
  await expect(alto).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(alto).toHaveAttribute('aria-pressed', 'true');
});

test('C02-04.M04.03 revocación 403 oculta controles tras revalidar', async ({ page }) => {
  await preparar(page);
  await page.goto('/plataforma/consola-global-v5');
  await expect(page.getByTestId('c0204-branding')).toBeVisible();
  await page.route(sesionUrl, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    return route.fulfill({ status: 403, headers, contentType: 'application/json',
      body: JSON.stringify({ success: false, data: null, message: 'REVOKED_PRIVATE_TOKEN' }) });
  });
  await page.getByRole('button', { name: 'Revalidar sesión global' }).click();
  await expect(page.getByTestId('c0204-branding')).toHaveCount(0);
  await expect(page.getByText('REVOKED_PRIVATE_TOKEN')).toHaveCount(0);
});
