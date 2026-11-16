import { expect, test, type Page } from '@playwright/test';

/**
 * C05-04.M04.04: real Chromium verification of private UI interactions.
 * Synthetic authorization fixtures only; no real JWT, MySQL or tenant grant.
 */
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const payload = (items: unknown[] = []) => ({
  success: true,
  data: { items, pagina: 1, tamano: 25, total: items.length, puedeCrear: false }
});

async function prepareSession(page: Page, allowed = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0504-m0404-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({
      status: allowed ? 200 : 403, headers,
      contentType: 'application/json',
      body: JSON.stringify(allowed
        ? { success: true, data: { nombreUsuario: 'Dev', rolNombre: 'Admin',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'SECRET_INTERNAL_NOT_FOR_BROWSER' })
    });
  });
}

async function mockDirectory(page: Page, handler?: (
  route: import('@playwright/test').Route) => Promise<void>) {
  await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
    } else if (handler) {
      await handler(route);
    } else {
      await route.fulfill({
        status: 200, headers, contentType: 'application/json',
        body: JSON.stringify(payload())
      });
    }
  });
}

test('C05-04.M04.04 anónimo: la ruta y los controles privados no aparecen', async ({ page }) => {
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Actualizar directorio' })).toHaveCount(0);
});

test('C05-04.M04.04 sesión 403: denegación sin mensaje de servidor', async ({ page }) => {
  await prepareSession(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
  await expect(page.getByText('SECRET_INTERNAL_NOT_FOR_BROWSER')).toHaveCount(0);
});

test('C05-04.M04.04 teclado Tab/Enter: estado ARIA reversible sin escritura ni formulario', async ({ page }) => {
  await prepareSession(page);
  await mockDirectory(page);
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
      writes.push(request.method() + ':' + request.url());
    }
  });
  await page.goto('/plataforma');
  const panel = page.locator('.ui-lightness-v5');
  await panel.locator('summary').click();
  const toggle = panel.getByRole('button', { name: 'Reducir detalles' });
  await expect(toggle).toBeVisible();
  for (let attempt = 0; attempt < 40; attempt++) {
    if (await toggle.evaluate(element => element === document.activeElement)) break;
    await page.keyboard.press('Tab');
  }
  await expect(toggle).toBeFocused();
  await expect(toggle).toHaveAttribute('aria-pressed', 'false');
  await page.keyboard.press('Enter');
  await expect(panel.getByRole('button', { name: 'Ver criterios' }))
    .toHaveAttribute('aria-pressed', 'true');
  await expect(panel.locator('.ui-lightness-v5__checks li')).toHaveCount(0);
  await page.keyboard.press('Enter');
  await expect(panel.getByRole('button', { name: 'Reducir detalles' }))
    .toHaveAttribute('aria-pressed', 'false');
  await expect(panel.locator('.ui-lightness-v5__checks li')).toHaveCount(3);
  await expect(panel.locator('form, input, textarea')).toHaveCount(0);
  expect(writes, 'la navegación nunca modifica datos institucionales').toEqual([]);
});

test('C05-04.M04.04 doble acción: actualizar deshabilitado durante GET pendiente', async ({ page }) => {
  await prepareSession(page);
  let requests = 0;
  let release!: () => void;
  const pending = new Promise<void>(resolve => { release = resolve; });
  await mockDirectory(page, async route => {
    requests++;
    if (requests === 2) await pending;
    await route.fulfill({
      status: 200, headers, contentType: 'application/json',
      body: JSON.stringify(payload())
    });
  });
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.goto('/plataforma');
  // The feedback is inside a closed <details> until the user opens it.
  await page.locator('.ui-lightness-v5 summary').click();
  await expect(page.getByTestId('c0504-empty')).toBeVisible();
  const update = page.getByRole('button', { name: 'Actualizar directorio' });
  await update.click();
  await expect(page.getByTestId('c0504-loading')).toBeVisible();
  await expect(update).toBeDisabled();
  await page.keyboard.press('Enter');
  expect(requests).toBe(2);
  release();
  await expect(page.getByTestId('c0504-empty')).toBeVisible();
  expect(requests).toBe(2);
  expect(writes).toEqual([]);
});

test('C05-04.M04.04 error 503: mensaje opaco y un reintento explícito sin POST', async ({ page }) => {
  await prepareSession(page);
  let attempts = 0;
  await mockDirectory(page, async route => {
    attempts++;
    await route.fulfill({
      status: attempts === 1 ? 503 : 200, headers,
      contentType: 'application/json',
      body: attempts === 1
        ? JSON.stringify({ success: false, message: 'PRIVATE_HTTP_STACKTRACE_TOKEN' })
        : JSON.stringify(payload())
    });
  });
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.goto('/plataforma');
  await page.locator('.ui-lightness-v5 summary').click();
  await expect(page.getByTestId('c0504-error')).toBeVisible();
  await expect(page.getByText('PRIVATE_HTTP_STACKTRACE_TOKEN')).toHaveCount(0);
  await page.getByTestId('c0504-feedback')
    .getByRole('button', { name: 'Reintentar directorio' }).click();
  await expect(page.getByTestId('c0504-empty')).toBeVisible();
  expect(attempts).toBe(2);
  expect(writes).toEqual([]);
});

test('C05-04.M04.04 permiso 403 de directorio no admite reintento ni filtra datos', async ({ page }) => {
  await prepareSession(page);
  await mockDirectory(page, async route => {
    await route.fulfill({ status: 403, headers, contentType: 'application/json',
      body: JSON.stringify({ success: false, message: 'PRIVATE_TENANT_DATA' }) });
  });
  await page.goto('/plataforma');
  await page.locator('.ui-lightness-v5 summary').click();
  await expect(page.getByTestId('c0504-permission')).toBeVisible();
  await expect(page.getByTestId('c0504-feedback')
    .getByRole('button', { name: 'Reintentar directorio' })).toHaveCount(0);
  await expect(page.getByText('PRIVATE_TENANT_DATA')).toHaveCount(0);
});
