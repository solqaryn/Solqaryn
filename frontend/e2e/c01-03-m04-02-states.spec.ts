import { expect, test, type Page } from '@playwright/test';

// UI fault presentation is stubbed, NOT a real JWT/RBAC rejection test.
// The existing M04.01 spec in the same job validates real JWT and MySQL A/B.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('M04.02 requires GitHub-hosted JWT/MySQL/Chromium fixture.');
}
const target = '/configuracion/dinamica-v5';
const endpoint = '**/empresa-configuracion/tenant/1';

async function loginA(page: Page): Promise<void> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill('c1504_admin_a');
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const pending = page.waitForResponse(r => r.url().endsWith('/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const res = await pending;
  expect(res.status()).toBe(200);
  const token: unknown = (await res.json())?.data?.token;
  expect(typeof token).toBe('string');
  expect((token as string).split('.')).toHaveLength(3);
  const membership = await page.request.get(api + '/tenant-context/1', {
    headers: { Authorization: 'Bearer ' + token }
  });
  expect(membership.status()).toBe(200);
  await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
}

test.describe('V24 M04.02 UI states after real JWT login with isolated response simulation', () => {
  test.describe.configure({ retries: 0 });
  for (const item of [
    { status: 403, state: 'forbidden', message: /No tienes permiso/ },
    { status: 404, state: 'not-found', message: /No se encontró la configuración/ },
    { status: 429, state: 'throttled', message: /demasiadas solicitudes/ },
    { status: 503, state: 'unavailable', message: /servicio no está disponible/ }
  ] as const) {
    test('differentiates HTTP ' + item.status + ' without exposing old form', async ({ page }) => {
      test.setTimeout(180_000);
      await loginA(page);
      await page.route(endpoint, route => route.fulfill({
        status: item.status, contentType: 'application/json',
        body: JSON.stringify({ message: 'SENSITIVE_BACKEND_DETAIL_NOT_FOR_USERS' })
      }));
      await page.goto(target);
      const state = page.locator('[data-config-state="' + item.state + '"]');
      await expect(state).toBeVisible();
      await expect(state).toContainText(item.message);
      await expect(page.getByText('SENSITIVE_BACKEND_DETAIL_NOT_FOR_USERS')).toHaveCount(0);
      await expect(page.getByRole('form', { name: 'Configuración del tenant activo' })).toHaveCount(0);
      for (const width of [320, 390, 768, 1280, 1440]) {
        await page.setViewportSize({ width, height: 900 });
        const bodyOverflow = await page.evaluate(() =>
          document.documentElement.scrollWidth - document.documentElement.clientWidth);
        expect(bodyOverflow, 'body overflow at ' + width).toBeLessThanOrEqual(2);
        await page.screenshot({ path: 'test-results/c0103-m0402-' + item.status + '-' + width + '.png', fullPage: true });
      }
    });
  }
  test('200 without data is empty, not a permitted editable form', async ({ page }) => {
    await loginA(page);
    await page.route(endpoint, route => route.fulfill({
      status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: null })
    }));
    await page.goto(target);
    await expect(page.locator('[data-config-state="empty"]')).toBeVisible();
    await expect(page.getByRole('form', { name: 'Configuración del tenant activo' })).toHaveCount(0);
  });
  test('transient failure recovers on retry against the REAL API', async ({ page }) => {
    await loginA(page);
    // The page and its tenant card both GET the same authenticated endpoint.
    // Fail ALL initial GETs, not merely the first (which could be the brand GET).
    // Re-enable the REAL API only after the user explicitly requests a retry.
    let simulateOutage = true;
    let injectedFailures = 0;
    let genuineRecoveryGets = 0;
    await page.route(endpoint, async route => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }
      if (simulateOutage) {
        injectedFailures++;
        await route.fulfill({ status: 503, contentType: 'application/json', body: '{}' });
      } else {
        genuineRecoveryGets++;
        await route.continue();
      }
    });
    await page.goto(target);
    await expect(page.locator('[data-config-state="unavailable"]')).toBeVisible();
    expect(injectedFailures, 'must exercise at least one true simulated outage on initial GET').toBeGreaterThan(0);
    simulateOutage = false;
    await page.getByRole('button', { name: 'Reintentar consulta' }).click();
    await expect(page.getByRole('form', { name: 'Configuración del tenant activo' })).toBeVisible();
    await expect(page.getByText('Empresa #1', { exact: false })).toBeVisible();
    expect(genuineRecoveryGets, 'retry must reach the real authenticated MySQL-backed API').toBeGreaterThan(0);
  });
});
