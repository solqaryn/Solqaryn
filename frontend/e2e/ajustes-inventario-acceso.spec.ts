import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConPermisoSoloDashboard(page: Page): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'Permisos cargados',
        data: {
          esAdministrador: false,
          permisos: ['Dashboard:Ver']
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

test.describe('Ajustes de inventario - control de acceso', () => {
  test('redirige a login cuando se intenta abrir el módulo sin sesión autenticada', async ({ page }) => {
    await page.goto('/inventario/ajustes');

    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega el módulo a un usuario autenticado sin permiso Inventario:Ver', async ({ page }) => {
    await loginConPermisoSoloDashboard(page);

    await page.goto('/inventario/ajustes');

    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
    await expect(page).not.toHaveURL(/\/inventario\/ajustes(?:\?|$)/);
  });
});
