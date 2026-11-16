import { expect, test, type Page } from '@playwright/test';

/** Prueba browser sintética: no certifica JWT HTTP, MFA ni grants reales. */
const endpoint = /\/plataforma\/sesion(?:\?|$)/;
async function preparar(page: Page, acceso: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c02-04-browser-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(endpoint, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await route.fulfill({ status: acceso ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(acceso
        ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Global', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'PRIVATE_BACKEND_DIAGNOSTIC' }) });
  });
}

test('sin token, guard niega ruta de control plane', async ({ page }) => {
  await page.goto('/plataforma/consola-global-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.control-cards li')).toHaveCount(0);
});

test('403 sintético rechaza acceso y no revela diagnóstico', async ({ page }) => {
  await preparar(page, false);
  await page.goto('/plataforma/consola-global-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.control-cards li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_BACKEND_DIAGNOSTIC')).toHaveCount(0);
});

test('acceso simulado muestra cuatro controles sin llamadas operativas ni overflow', async ({ page }) => {
  await preparar(page, true);
  await page.setViewportSize({ width: 390, height: 844 });
  const operaciones: string[] = [];
  await page.route(/\/(?:grants|restore|mfa\/enable|impersonate|jobs\/dispatch)(?:\/|\?|$)/i, route => {
    operaciones.push(route.request().url());
    return route.abort();
  });
  await page.goto('/plataforma/consola-global-v5');
  await expect(page.getByRole('heading', { name: 'Consola global auditada' })).toBeVisible();
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  await expect(page.getByText(/JWT HTTP y auditoría durable: NO_MEDIDO/)).toBeVisible();
  await expect(page.locator('.control-plane form, .control-plane input')).toHaveCount(0);
  expect(operaciones).toEqual([]);
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
});

test('fallo 503 tras grant oculta los controles y sanitiza error', async ({ page }) => {
  await preparar(page, true);
  await page.goto('/plataforma/consola-global-v5');
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  await page.route(endpoint, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await route.fulfill({ status: 503, headers, contentType: 'application/json',
      body: JSON.stringify({ success: false, data: null, message: 'SECRET_DSN' }) });
  });
  await page.getByRole('button', { name: 'Revalidar sesión global' }).click();
  await expect(page.getByRole('alert')).toContainText('No se pudo verificar la sesión');
  await expect(page.locator('.control-cards li')).toHaveCount(0);
  await expect(page.getByText('SECRET_DSN')).toHaveCount(0);
});

test('C02-04.M04.02 error 503 permite recuperar solo tras nuevo GET autorizado', async ({ page }) => {
  await preparar(page, true);
  await page.goto('/plataforma/consola-global-v5');
  await expect(page.locator('.control-cards li')).toHaveCount(4);

  let falla = true;
  let consultas = 0;
  await page.route(endpoint, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    consultas++;
    await route.fulfill({
      status: falla ? 503 : 200, headers, contentType: 'application/json',
      body: JSON.stringify(falla
        ? { success: false, data: null, message: 'SENSITIVE_PROVIDER_DIAGNOSTIC' }
        : { success: true, data: { nombreUsuario: 'Global', rolNombre: 'Admin',
            ambito: 'Plataforma', esAdministrador: true } })
    });
  });
  await page.getByRole('button', { name: 'Revalidar sesión global' }).click();
  await expect(page.getByRole('alert')).toContainText('No se pudo verificar');
  await expect(page.locator('.control-cards li')).toHaveCount(0);
  await expect(page.getByText('SENSITIVE_PROVIDER_DIAGNOSTIC')).toHaveCount(0);
  falla = false;
  await page.getByRole('button', { name: 'Reintentar verificación' }).click();
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  expect(consultas).toBe(2);
});
