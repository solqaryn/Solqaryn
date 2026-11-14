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
        data: { permisos, esAdministrador: false }
      })
    });
  });

  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

function crearOrden(id: number) {
  return {
    id,
    numeroOrden: `OC-${id.toString().padStart(6, '0')}`,
    estado: 1,
    solicitudCompraId: null,
    proveedorId: 9,
    proveedorNombre: 'Proveedor QA',
    moneda: 'HNL',
    condicionesCompra: 'Crédito 30 días',
    fechaEsperadaUtc: '2026-08-30T00:00:00Z',
    observaciones: null,
    subtotal: 1000,
    descuento: 0,
    impuesto: 150,
    total: 1150,
    fechaEnvioAprobacionUtc: null,
    fechaAprobacionUtc: null,
    fechaCancelacionUtc: null,
    detalles: []
  };
}

test.describe('Órdenes de compra - paginación y performance N2.2.G.2', () => {
  test('consulta una sola página acotada y sólo solicita la siguiente tras navegación explícita', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver']);

    await page.route('**/proveedores/activos', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: '', errors: [], data: [] })
    }));

    const solicitudes: Array<{ page: number; pageSize: number }> = [];
    await page.route('**/ordenes-compra?**', async route => {
      const url = new URL(route.request().url());
      const pagina = Number(url.searchParams.get('page'));
      const pageSize = Number(url.searchParams.get('pageSize'));
      solicitudes.push({ page: pagina, pageSize });

      const inicio = (pagina - 1) * pageSize + 1;
      const items = Array.from({ length: pageSize }, (_, index) => crearOrden(inicio + index));

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: '',
          errors: [],
          data: {
            items,
            page: pagina,
            pageSize,
            totalCount: 250,
            totalPages: Math.ceil(250 / pageSize)
          }
        })
      });
    });

    await page.goto('/ordenes-compra');
    await expect(page.getByRole('heading', { name: 'Órdenes de compra' })).toBeVisible();
    await expect(page.getByText('OC-000001', { exact: true })).toBeVisible();

    expect(solicitudes).toEqual([{ page: 1, pageSize: 10 }]);
    expect(solicitudes[0].pageSize).toBeLessThanOrEqual(100);

    const paginator = page.locator('mat-paginator[aria-label="Paginación de órdenes de compra"]');
    await expect(paginator).toBeVisible();
    const siguiente = paginator.getByRole('button', { name: /next page|página siguiente/i });
    await expect(siguiente).toBeEnabled();
    await siguiente.click();

    await expect(page.getByText('OC-000011', { exact: true })).toBeVisible();
    expect(solicitudes).toEqual([
      { page: 1, pageSize: 10 },
      { page: 2, pageSize: 10 }
    ]);
    expect(solicitudes.every(item => item.pageSize <= 100)).toBe(true);
  });
});
