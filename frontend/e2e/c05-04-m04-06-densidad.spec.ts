import { expect, test, type Page, type Route } from '@playwright/test';

// Synthetic session and directory fixtures verify UI behavior only, never live JWT/MySQL.
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const payload = (items: unknown[]) => ({
  success: true, data: { items, pagina: 1, tamano: 25, total: items.length, puedeCrear: false }
});

async function session(page: Page, authorized = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0504-m0406-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    await route.fulfill({
      status: authorized ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(authorized
        ? { success: true, data: { nombreUsuario: 'Demo', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'DO_NOT_EXPOSE_PRIVATE_SESSION' })
    });
  });
}

async function directory(page: Page, respond: (route: Route) => Promise<void>) {
  await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    await respond(route);
  });
}

test('C05-04.M04.06 anonymous access never renders private density surface', async ({ page }) => {
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
});

test('C05-04.M04.06 denied session suppresses private diagnostics', async ({ page }) => {
  await session(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
  await expect(page.getByText('DO_NOT_EXPOSE_PRIVATE_SESSION')).toHaveCount(0);
});

test('C05-04.M04.06 progressive loading remains readable and compact toggles never duplicate GET', async ({ page }) => {
  await session(page);
  let release!: () => void;
  const pending = new Promise<void>(resolve => { release = resolve; });
  let directoryGets = 0;
  await directory(page, async route => {
    directoryGets++;
    await pending;
    await route.fulfill({ status: 200, headers, contentType: 'application/json', body: JSON.stringify(payload([])) });
  });
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.goto('/plataforma');
  const summary = page.locator('.ui-lightness-v5 > summary');
  await expect(summary).toBeVisible();
  await summary.click();
  const surface = page.locator('.ui-lightness-v5');
  await expect(page.getByTestId('c0504-loading')).toBeVisible();
  await expect(surface.locator('.ui-lightness-v5__checks li')).toHaveCount(3);
  await surface.getByRole('button', { name: 'Reducir detalles' }).click();
  await expect(surface.locator('.ui-lightness-v5__checks li')).toHaveCount(0);
  await expect(surface.locator('.ui-lightness-v5__status')).toContainText('tres criterios');
  await surface.getByRole('button', { name: 'Ver criterios' }).click();
  await expect(surface.locator('.ui-lightness-v5__checks li')).toHaveCount(3);
  await expect(surface).toContainText('no medido aquí');
  release();
  await expect(page.getByTestId('c0504-empty')).toBeVisible();
  expect(directoryGets, 'local presentation toggles must not repeat directory GET').toBe(1);
  expect(writes, 'read-only UI must not mutate server data').toEqual([]);
});

test('C05-04.M04.06 320/375/768/1280 preserves density and 44px controls', async ({ page }) => {
  await session(page);
  await directory(page, async route => {
    const rows = Array.from({ length: 25 }, (_, i) => ({
      id: i + 1, nombre: 'Empresa ' + (i + 1), activa: true, usuariosActivos: i + 1
    }));
    await route.fulfill({ status: 200, headers, contentType: 'application/json', body: JSON.stringify(payload(rows)) });
  });
  await page.goto('/plataforma');
  await page.locator('.ui-lightness-v5 > summary').click();
  const surface = page.locator('.ui-lightness-v5');
  await expect(surface.locator('.ui-lightness-v5__checks li')).toHaveCount(3);
  await expect(page.locator('table[aria-label="Empresas registradas"] tbody tr')).toHaveCount(25);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'page overflow at ' + width).toBeLessThanOrEqual(2);
    const toggle = surface.getByRole('button', { name: 'Reducir detalles' });
    const box = await toggle.boundingBox();
    expect(box, 'toggle should be visible').not.toBeNull();
    expect(box!.height, 'touch target >=44px').toBeGreaterThanOrEqual(44);
  }
  await expect(surface).toContainText('sin resultados de campo certificados');
});

test('C05-04.M04.06 503 preserves compact controls, redacts provider error, retries once only', async ({ page }) => {
  await session(page);
  let calls = 0;
  await directory(page, async route => {
    calls++;
    await route.fulfill({
      status: calls === 1 ? 503 : 200, headers, contentType: 'application/json',
      body: calls === 1
        ? JSON.stringify({ success: false, message: 'DO_NOT_EXPOSE_PROVIDER_DETAILS' })
        : JSON.stringify(payload([]))
    });
  });
  await page.goto('/plataforma');
  await page.locator('.ui-lightness-v5 > summary').click();
  const surface = page.locator('.ui-lightness-v5');
  await expect(page.getByTestId('c0504-error')).toBeVisible();
  await expect(page.getByText('DO_NOT_EXPOSE_PROVIDER_DETAILS')).toHaveCount(0);
  await surface.getByRole('button', { name: 'Reducir detalles' }).click();
  await expect(surface.locator('.ui-lightness-v5__status')).toBeVisible();
  expect(calls, 'changing visual density must not automatically retry a failed provider').toBe(1);
  await page.getByTestId('c0504-feedback').getByRole('button', { name: 'Reintentar directorio' }).click();
  await expect(page.getByTestId('c0504-empty')).toBeVisible();
  expect(calls, 'one explicit retry').toBe(2);
});
