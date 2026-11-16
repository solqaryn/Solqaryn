import { expect, test, type Page } from '@playwright/test';

// C02-04.M04.04: true Chromium UI interaction, synthetic session only.
// No JWT/MFA/RBAC/tenant backend, MySQL or grants are certified.
const endpoint = /\/plataforma\/sesion(?:\?|$)/;
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const allowed = { success: true, data: {
  nombreUsuario: 'Sintetico', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
}};
async function grant(page: Page, authorized: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0204-m0404-not-a-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(endpoint, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await route.fulfill({ status: authorized ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(authorized ? allowed : { success: false, message: 'PRIVATE_GLOBAL_DIAGNOSTIC' }) });
  });
}

test('C02-04.M04.04 anónimo: guard impide navegación y estado privado', async ({ page }) => {
  await page.goto('/plataforma/consola-global-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.control-cards li')).toHaveCount(0);
});

test('C02-04.M04.04 403 sintético: nunca muestra datos ni diagnóstico', async ({ page }) => {
  await grant(page, false);
  await page.goto('/plataforma/consola-global-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.control-cards li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_GLOBAL_DIAGNOSTIC')).toHaveCount(0);
});

test('C02-04.M04.04 lector global: navegación por teclado y sin formularios ni mutaciones', async ({ page }) => {
  await grant(page, true);
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.goto('/plataforma/consola-global-v5');
  await expect(page.getByRole('heading', { name: 'Consola global auditada' })).toBeVisible();
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  await expect(page.locator('.control-plane form, .control-plane input, .control-plane textarea')).toHaveCount(0);
  const back = page.getByRole('link', { name: 'Volver a administración de plataforma' });
  let reached = false;
  for (let i = 0; i < 75; i++) {
    await page.keyboard.press('Tab');
    if (await back.evaluate(el => document.activeElement === el)) { reached = true; break; }
  }
  expect(reached, 'teclado Tab debe alcanzar navegación real').toBe(true);
  await expect(back).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma(?:\?|$)/);
  expect(writes).toEqual([]);
});

test('C02-04.M04.04 error 503 no filtra mensaje y nuevo GET restaura solo lectura', async ({ page }) => {
  await grant(page, true);
  await page.goto('/plataforma/consola-global-v5');
  let fail = true;
  let calls = 0;
  await page.route(endpoint, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    calls++;
    await route.fulfill({ status: fail ? 503 : 200, headers, contentType: 'application/json',
      body: JSON.stringify(fail ? { success: false, message: 'SECRET_DSN_SHOULD_NOT_RENDER' } : allowed) });
  });
  await page.getByRole('button', { name: 'Revalidar sesión global' }).click();
  await expect(page.getByRole('alert')).toContainText('No se pudo verificar');
  await expect(page.locator('.control-cards li')).toHaveCount(0);
  await expect(page.getByText('SECRET_DSN_SHOULD_NOT_RENDER')).toHaveCount(0);
  fail = false;
  await page.getByRole('button', { name: 'Reintentar verificación' }).click();
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  expect(calls).toBe(2);
});

test('C02-04.M04.04 verificación single-flight: segundo click deshabilitado y sin petición doble', async ({ page }) => {
  await grant(page, true);
  await page.goto('/plataforma/consola-global-v5');
  let release!: () => void;
  const pending = new Promise<void>(resolve => { release = resolve; });
  let calls = 0;
  await page.route(endpoint, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    calls++;
    await pending;
    await route.fulfill({ status: 200, headers, contentType: 'application/json', body: JSON.stringify(allowed) });
  });
  const button = page.getByRole('button', { name: 'Revalidar sesión global' });
  await button.click();
  await expect(page.locator('#control-plane-estado')).toHaveAttribute('aria-busy', 'true');
  await expect(page.locator('.control-plane nav button')).toBeDisabled();
  // HTML disabled button no emite segundo click; no invocamos mutaciones.
  await page.locator('.control-plane nav button').evaluate(el => (el as HTMLButtonElement).click());
  expect(calls).toBe(1);
  release();
  await expect(page.locator('#control-plane-estado')).toHaveAttribute('aria-busy', 'false');
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  expect(calls).toBe(1);
});
