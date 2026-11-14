import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function login(page: Page): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: 'OK', data: { permisos: ['Dashboard:Ver', 'MovimientosInventario:Ver', 'MovimientosInventario:Crear'], esAdministrador: false } })
    });
  });
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

function existencia(id: number, nombre: string) {
  return {
    id,
    productoVarianteId: 100 + id,
    productoNombre: nombre,
    varianteSku: `SKU-${id}`,
    almacenId: 11,
    almacenCodigo: 'ALM-01',
    almacenNombre: 'Principal',
    ubicacionAlmacenId: null,
    ubicacionCodigo: null,
    ubicacionNombre: null,
    stockFisico: 10,
    stockReservado: 0,
    stockDisponible: 10,
    stockTransito: 0,
    stockMinimo: 1,
    stockMaximo: null,
    tieneStockBajo: false,
    estaAgotada: false,
    fechaCreacion: '2026-08-17T00:00:00Z',
    fechaActualizacion: '2026-08-17T00:00:00Z'
  };
}

test('carga todas las páginas de existencias antes de habilitar el selector físico', async ({ page }) => {
  await login(page);

  const paginas: number[] = [];
  await page.route('**/existencias-variante**', async route => {
    const url = new URL(route.request().url());
    const pageNumber = Number(url.searchParams.get('page') ?? '1');
    paginas.push(pageNumber);
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'OK',
        data: {
          items: pageNumber === 1 ? [existencia(1, 'Primera página')] : [existencia(2, 'Segunda página')],
          page: pageNumber,
          pageSize: 200,
          totalItems: 2,
          totalPages: 2
        }
      })
    });
  });

  await page.goto('/inventario/reservas/nueva');
  const selector = page.locator('mat-select[formcontrolname="existenciaVarianteId"]').first();
  await expect(selector).toBeVisible();
  await selector.focus();
  await selector.press('Enter');
  await expect(page.getByRole('option', { name: /Segunda página.*SKU-2/ })).toBeVisible();
  expect(paginas).toEqual([1, 2]);
});
