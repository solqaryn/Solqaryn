import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConPermisos(page: Page, permisos: string[]): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'OK', data: { permisos, esAdministrador: false } }) });
  });
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

test.describe('Reservas de inventario - permisos de rutas', () => {
  test('Ver no autoriza crear una reserva', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'MovimientosInventario:Ver']);
    await page.goto('/inventario/reservas/nueva');
    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
  });

  test('Crear no sustituye Editar', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'MovimientosInventario:Ver', 'MovimientosInventario:Crear']);
    await page.goto('/inventario/reservas/701/editar');
    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
  });
});
