import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConPermisos(page: Page, permisos: string[]): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'Permisos cargados',
        data: { esAdministrador: false, permisos }
      })
    });
  });

  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

async function mockListadoVacio(page: Page): Promise<void> {
  await page.route('**/recepciones-compra?**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'OK',
        data: { items: [], totalCount: 0, page: 1, pageSize: 20 }
      })
    });
  });
}

async function mockDetalleBorrador(page: Page): Promise<void> {
  await page.route('**/recepciones-compra/1', async route => {
    if (route.request().resourceType() === 'document') {
      await route.fallback();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'OK',
        data: {
          id: 1,
          numeroRecepcion: 'REC-E2E-001',
          ordenCompraId: 10,
          numeroOrdenCompra: 'OC-E2E-001',
          estado: 'Borrador',
          fechaRecepcionUtc: null,
          observaciones: null,
          cantidadRecibidaTotal: 1,
          cantidadAceptadaTotal: 1,
          cantidadDanadaTotal: 0,
          cantidadFaltanteTotal: 0,
          cantidadSobranteTotal: 0,
          detalles: []
        }
      })
    });
  });
}

test.describe('Recepción de compra - seguridad RBAC UI', () => {
  test('redirige a login cuando se intenta abrir el módulo sin sesión autenticada', async ({ page }) => {
    await page.goto('/recepciones-compra');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega el módulo a un usuario autenticado sin Compras:Ver', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver']);
    await page.goto('/recepciones-compra');
    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
  });

  test('con Compras:Ver pero sin Crear no expone la acción Nueva recepción', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver']);
    await mockListadoVacio(page);

    await page.goto('/recepciones-compra');

    await expect(page.getByRole('heading', { name: 'Recepción de mercancía' })).toBeVisible();
    await expect(page.getByRole('button', { name: /Nueva recepción/i })).toHaveCount(0);
  });

  test('con Compras:Ver pero sin Confirmar ni Anular oculta acciones críticas', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver']);
    await mockDetalleBorrador(page);

    await page.goto('/recepciones-compra/1');

    await expect(page.getByRole('heading', { name: 'Detalle de recepción' })).toBeVisible();
    await expect(page.getByTestId('confirmar-recepcion')).toHaveCount(0);
    await expect(page.getByTestId('anular-recepcion')).toHaveCount(0);
  });
});
