import { expect, test, type Page } from '@playwright/test';
// C02-02.M04.01: verificaciones UI con sesión sintética, no routing MySQL real.
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
async function grant(page: Page, allow: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'browser-synthetic-no-jwt');
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
      status: allow ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allow ? {
        success: true, data: { nombreUsuario: 'Simulado', rolNombre: 'Admin',
          ambito: 'Plataforma', esAdministrador: true }
      } : { success: false, data: null, message: 'NO_AUTHORITY' })
    });
  });
}
test('anónimo sin token no alcanza contrato privado', async ({ page }) => {
  await page.goto('/plataforma/placement-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Placement compartido y dedicado' })).toHaveCount(0);
});
test('403 sintético no muestra modalidades ni credenciales', async ({ page }) => {
  await grant(page, false);
  await page.goto('/plataforma/placement-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.modalidades article')).toHaveCount(0);
});
test('grant simulado solo lectura mobile sin endpoints de migración/cutover', async ({ page }) => {
  await grant(page, true);
  await page.setViewportSize({ width: 390, height: 844 });
  const unexpected: string[] = [];
  page.on('request', r => {
    if (/\/(?:placement|migrations?|cutover|tenant-dsn)(?:\/|\?|$)/i.test(r.url())
        && !['GET', 'HEAD', 'OPTIONS'].includes(r.method())) {
      unexpected.push(r.method() + ':' + r.url());
    }
  });
  await page.goto('/plataforma/placement-v5');
  await expect(page.getByRole('heading', { name: 'Placement compartido y dedicado' })).toBeVisible();
  await expect(page.locator('.modalidades article')).toHaveCount(2);
  await expect(page.getByText('Placement, router del servidor y migraciones reales: NO_MEDIDO / NO_EJECUTADO.')).toBeVisible();
  await expect(page.locator('.placement-review form, .placement-review input, .placement-review textarea')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Revalidar sesión de placement' })).toHaveCount(1);
  expect(unexpected).toEqual([]);
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
});


test('C02-02.M04.02: 503 durante revalidación oculta información y no filtra payload', async ({ page }) => {
  await grant(page, true);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/plataforma/placement-v5');
  await expect(page.locator('.modalidades article')).toHaveCount(2);
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: {
        'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
        'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
        'Access-Control-Allow-Methods': 'GET, OPTIONS'
      }});
      return;
    }
    await route.fulfill({ status: 503, contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': 'http://127.0.0.1:4200' },
      body: JSON.stringify({ success: false, data: null, message: 'DSN_SECRETO_BROWSER' }) });
  });
  await page.getByRole('button', { name: 'Revalidar sesión de placement' }).click();
  await expect(page.getByText('No fue posible revalidar la sesión. La información permanece restringida.')).toBeVisible();
  await expect(page.locator('.modalidades article')).toHaveCount(0);
  await expect(page.getByText('DSN_SECRETO_BROWSER')).toHaveCount(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
});

test('C02-02.M04.04: navegación segura, 403 posterior y mensaje sin datos privados', async ({ page }) => {
  await grant(page, true);
  await page.goto('/plataforma/placement-v5');
  await expect(page.locator('.modalidades article')).toHaveCount(2);
  await expect(page.getByRole('link', { name: 'Regresar a administración de plataforma' }))
    .toHaveAttribute('href', '/plataforma');
  await page.route(sessionUrl, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    await route.fulfill({ status: 403, headers, contentType: 'application/json',
      body: JSON.stringify({ success: false, data: null, message: 'DSN_NOT_FOR_BROWSER' }) });
  });
  await page.getByRole('button', { name: 'Revalidar sesión de placement' }).click();
  await expect(page.getByText('Sesión administrativa no confirmada. Placement privado oculto.').first()).toBeVisible();
  await expect(page.locator('.modalidades article')).toHaveCount(0);
  await expect(page.getByText('DSN_NOT_FOR_BROWSER')).toHaveCount(0);
  await expect(page.locator('.placement-review form, .placement-review input')).toHaveCount(0);
});
