import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConPermisoSoloDashboard(page: Page): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: 'Permisos cargados', data: { permisos: ['Dashboard:Ver'], esAdministrador: false } })
    });
  });

  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

test.describe('Reservas de inventario - acceso', () => {
  test('redirige al login cuando no existe sesión', async ({ page }) => {
    await page.goto('/inventario/reservas');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega la ruta a un usuario autenticado sin MovimientosInventario:Ver', async ({ page }) => {
    await loginConPermisoSoloDashboard(page);
    await page.goto('/inventario/reservas');
    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
    await expect(page).not.toHaveURL(/\/inventario\/reservas(?:\?|$)/);
  });
});
