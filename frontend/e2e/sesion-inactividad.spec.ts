import { test, expect, Page } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const LAST_ACTIVITY_KEY = 'solqaryn_last_activity';
const LAST_RENEW_KEY = 'solqaryn_last_renew';
const TOKEN_KEY = 'solqaryn_token';
const THIRTY_ONE_MINUTES_MS = 31 * 60 * 1000;
const SIX_MINUTES_MS = 6 * 60 * 1000;

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function ageActivity(page: Page): Promise<void> {
  await page.evaluate(({ key, elapsed }) => {
    localStorage.setItem(key, String(Date.now() - elapsed));
  }, { key: LAST_ACTIVITY_KEY, elapsed: THIRTY_ONE_MINUTES_MS });
}

async function forceRenewalWindow(page: Page): Promise<void> {
  await page.evaluate(({ activityKey, renewKey, elapsed }) => {
    localStorage.setItem(activityKey, String(Date.now()));
    localStorage.setItem(renewKey, String(Date.now() - elapsed));
  }, { activityKey: LAST_ACTIVITY_KEY, renewKey: LAST_RENEW_KEY, elapsed: SIX_MINUTES_MS });
}

test.describe('Sesión por 30 minutos continuos de inactividad', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test('una actividad real reinicia el contador y evita el cierre', async ({ page }) => {
    await login(page);
    await ageActivity(page);

    await page.mouse.move(120, 160);
    await page.waitForTimeout(16_000);

    await expect(page).not.toHaveURL(/\/login$/);
    const lastActivity = await page.evaluate((key) => Number(localStorage.getItem(key)), LAST_ACTIVITY_KEY);
    expect(Date.now() - lastActivity).toBeLessThan(60_000);
  });

  test('cierra únicamente después de inactividad continua', async ({ page }) => {
    await login(page);
    await ageActivity(page);

    await page.waitForURL(/\/login$/, { timeout: 20_000 });
    await expect(page.getByText('Tu sesión expiró por 30 minutos de inactividad.')).toBeVisible();
    await expect.poll(async () => page.evaluate((key) => localStorage.getItem(key), TOKEN_KEY)).toBeNull();
  });

  test('la actividad de otra pestaña mantiene la sesión compartida', async ({ page, context }) => {
    await login(page);
    const secondPage = await context.newPage();
    await secondPage.goto('/dashboard');
    await expect(secondPage).not.toHaveURL(/\/login$/);

    await ageActivity(page);
    await secondPage.mouse.move(160, 180);
    await secondPage.locator('body').click({ position: { x: 30, y: 30 } });
    await page.waitForTimeout(16_000);

    await expect(page).not.toHaveURL(/\/login$/);
    await expect(secondPage).not.toHaveURL(/\/login$/);
    await secondPage.close();
  });

  test('renueva el token mientras existe actividad sin perder un formulario de venta', async ({ page }) => {
    let renewals = 0;
    page.on('response', (response) => {
      if (response.url().endsWith('/auth/renovar') && response.status() === 200) renewals += 1;
    });

    await login(page);
    await page.goto('/ventas/nueva');
    const customerName = page.locator('input[formcontrolname="clienteNombre"]');
    await customerName.fill('Cliente con venta en curso');
    await forceRenewalWindow(page);

    await expect.poll(() => renewals, { timeout: 20_000 }).toBeGreaterThan(0);
    await expect(page).toHaveURL(/\/ventas\/nueva$/);
    await expect(customerName).toHaveValue('Cliente con venta en curso');
    await expect.poll(async () => page.evaluate((key) => localStorage.getItem(key), TOKEN_KEY)).not.toBeNull();
  });

  test('una falla de red al renovar no expulsa al usuario activo', async ({ page }) => {
    await page.route('**/auth/renovar', (route) => route.abort('failed'));
    await login(page);
    await forceRenewalWindow(page);

    await page.waitForTimeout(16_000);

    await expect(page).not.toHaveURL(/\/login$/);
    await expect.poll(async () => page.evaluate((key) => localStorage.getItem(key), TOKEN_KEY)).not.toBeNull();
  });
});
