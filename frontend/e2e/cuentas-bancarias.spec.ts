import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

test.describe('Cuentas Bancarias E2E y Accesibilidad N4.2.E / N4.3.E', () => {
  test.beforeEach(async ({ page }) => {
    await loginUi(page);
  });

  test('valida renderizado y estado de accesibilidad para interacciones clave', async ({ page }) => {
    await page.goto('/cuentas-bancarias');

    const heading = page.getByRole('heading', { name: 'Cuentas Bancarias' });
    await expect(heading).toBeVisible({ timeout: 5000 });

    const nuevaCuentaBtn = page.getByRole('button', { name: 'Nueva Cuenta' });
    await expect(nuevaCuentaBtn).toHaveAttribute('aria-expanded', 'false');

    await nuevaCuentaBtn.click();

    const cancelarBtn = page.getByRole('button', { name: 'Cancelar' });
    await expect(cancelarBtn).toHaveAttribute('aria-expanded', 'true');
    await expect(page.getByRole('heading', { name: 'Registrar Nueva Cuenta' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Nombre de la cuenta' })).toBeVisible();
    await expect(page.getByRole('search', { name: 'Filtros de búsqueda' })).toBeVisible();
    await expect(page.getByRole('table', { name: 'Tabla de cuentas bancarias' })).toBeVisible();
  });

  test('abre la conciliación N4.3.E y expone importación y matches sin credenciales ni bypasses', async ({ page }) => {
    await page.route('**/cuentas-bancarias**', async (route) => {
      const request = route.request();
      if (request.method() !== 'GET' || request.resourceType() === 'document') {
        await route.continue();
        return;
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [{
            id: 7001,
            bancoId: 1,
            nombre: 'Cuenta E2E Conciliación',
            numeroCuenta: 'HN-E2E-7001',
            moneda: 'HNL',
            saldoInicial: 1000,
            estado: 1
          }],
          page: 1,
          pageSize: 50,
          totalCount: 1,
          totalPages: 1
        })
      });
    });

    await page.goto('/cuentas-bancarias');

    const conciliar = page.getByRole('button', { name: 'Conciliar cuenta Cuenta E2E Conciliación' });
    await expect(conciliar).toBeVisible({ timeout: 5000 });
    await conciliar.click();

    await expect(page.getByRole('heading', { name: 'Conciliación bancaria' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'HN-E2E-7001', exact: true })).toBeVisible();
    await expect(page.getByRole('form', { name: 'Importar estado de cuenta' })).toBeVisible();
    await expect(page.getByRole('form', { name: 'Registrar coincidencia bancaria' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Importar estado de cuenta' })).toBeEnabled();
    await expect(page.getByRole('button', { name: 'Registrar coincidencia' })).toBeEnabled();
  });
});