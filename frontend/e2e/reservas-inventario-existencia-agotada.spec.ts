import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function login(page: Page): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'OK', data: { permisos: ['Dashboard:Ver', 'MovimientosInventario:Ver', 'MovimientosInventario:Crear'], esAdministrador: false } }) }));
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

test('deshabilita en el selector una existencia sin stock disponible', async ({ page }) => {
  await login(page);
  await page.route('**/existencias-variante**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      success: true,
      message: 'OK',
      data: {
        items: [{ id: 999, productoVarianteId: 199, productoNombre: 'Agotado', varianteSku: 'AG-0', almacenId: 11, almacenCodigo: 'ALM-01', almacenNombre: 'Principal', ubicacionAlmacenId: null, ubicacionCodigo: null, ubicacionNombre: null, stockFisico: 5, stockReservado: 5, stockDisponible: 0, stockTransito: 0, stockMinimo: 1, stockMaximo: null, tieneStockBajo: true, estaAgotada: true, fechaCreacion: '2026-08-17T00:00:00Z', fechaActualizacion: '2026-08-17T00:00:00Z' }],
        page: 1,
        pageSize: 200,
        totalItems: 1,
        totalPages: 1
      }
    })
  }));

  await page.goto('/inventario/reservas/nueva');
  const selector = page.locator('mat-select[formcontrolname="existenciaVarianteId"]').first();
  await selector.focus();
  await selector.press('Enter');
  const opcion = page.getByRole('option', { name: /Agotado.*AG-0.*sin stock disponible/ });
  await expect(opcion).toHaveAttribute('aria-disabled', 'true');
  await page.keyboard.press('Escape');
  await expect(page.getByRole('button', { name: 'Guardar reserva', exact: true })).toBeDisabled();
});
