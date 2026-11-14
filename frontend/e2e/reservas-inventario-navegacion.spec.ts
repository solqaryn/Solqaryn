import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConInventario(page: Page): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'Permisos cargados',
        data: {
          permisos: ['Dashboard:Ver', 'MovimientosInventario:Ver'],
          esAdministrador: false
        }
      })
    });
  });

  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

test.describe('Reservas de inventario - navegación', () => {
  test('muestra la entrada lateral y navega al listado cuando el usuario tiene permiso', async ({ page }) => {
    await loginConInventario(page);

    const reservas = page.getByRole('link', { name: 'Reservas', exact: true });
    await expect(reservas).toBeVisible();
    await expect(reservas).toHaveAttribute('href', '/inventario/reservas');

    await reservas.click();
    await expect(page).toHaveURL(/\/inventario\/reservas(?:\?|$)/);
  });
});
