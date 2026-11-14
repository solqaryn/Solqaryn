import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

type PermisoCompras =
  | 'Compras:Ver'
  | 'Compras:Crear'
  | 'Compras:Editar'
  | 'Compras:Confirmar'
  | 'Compras:Anular';

async function loginConPermisos(page: Page, permisos: PermisoCompras[]): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'Permisos cargados',
        data: {
          esAdministrador: false,
          permisos: ['Dashboard:Ver', ...permisos]
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

async function mockFacturasProveedor(page: Page): Promise<void> {
  await page.route(/\/facturas-proveedor(?:\/1)?(?:\?.*)?$/, async route => {
    const request = route.request();
    const url = new URL(request.url());
    const esApiFacturaProveedor = request.resourceType() !== 'document'
      && (url.pathname === '/facturas-proveedor' || url.pathname === '/facturas-proveedor/1');

    if (!esApiFacturaProveedor) {
      await route.continue();
      return;
    }

    if (/\/facturas-proveedor\/1$/.test(url.pathname)) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: 'Factura cargada',
          data: {
            id: 1,
            numeroFactura: 'FAC-123',
            proveedorId: 1,
            ordenCompraId: 1,
            proveedorNombreSnapshot: 'Proveedor Test',
            proveedorDocumentoSnapshot: null,
            moneda: 'HNL',
            fechaEmisionUtc: '2026-08-20T00:00:00Z',
            fechaVencimientoUtc: null,
            referenciaFiscal: null,
            observaciones: null,
            estado: 1,
            fechaRegistroUtc: null,
            registradaPorUsuarioId: null,
            registradaPorNombreSnapshot: null,
            fechaAnulacionUtc: null,
            anuladaPorUsuarioId: null,
            motivoAnulacion: null,
            subtotal: 100,
            descuento: 0,
            impuesto: 0,
            total: 100,
            esEditable: true,
            detalles: []
          }
        })
      });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'Facturas cargadas',
        data: {
          items: [],
          page: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 0
        }
      })
    });
  });
}

test.describe('FacturaProveedor - seguridad frontend', () => {
  test('redirige a login cuando no existe sesión autenticada', async ({ page }) => {
    await page.goto('/facturas-proveedor');

    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega el listado a un usuario sin Compras:Ver', async ({ page }) => {
    await loginConPermisos(page, []);

    await page.goto('/facturas-proveedor');

    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
    await expect(page).not.toHaveURL(/\/facturas-proveedor(?:\?|$)/);
  });

  test('oculta Nueva factura cuando falta Compras:Crear', async ({ page }) => {
    await mockFacturasProveedor(page);
    await loginConPermisos(page, ['Compras:Ver']);

    await page.goto('/facturas-proveedor');

    await expect(page.getByRole('heading', { name: 'Facturas de proveedor' })).toBeVisible();
    await expect(page.locator('a[href="/facturas-proveedor/nueva"]')).toHaveCount(0);
  });

  test('muestra Nueva factura cuando existe Compras:Crear', async ({ page }) => {
    await mockFacturasProveedor(page);
    await loginConPermisos(page, ['Compras:Ver', 'Compras:Crear']);

    await page.goto('/facturas-proveedor');

    await expect(page.locator('a[href="/facturas-proveedor/nueva"]')).toBeVisible();
  });

  test('oculta acciones mutativas del detalle sin sus permisos', async ({ page }) => {
    await mockFacturasProveedor(page);
    await loginConPermisos(page, ['Compras:Ver']);

    await page.goto('/facturas-proveedor/1');

    await expect(page.getByRole('heading', { name: 'Factura FAC-123' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Editar' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Registrar' })).toHaveCount(0);
  });

  test('muestra Editar y Registrar únicamente con los permisos correspondientes', async ({ page }) => {
    await mockFacturasProveedor(page);
    await loginConPermisos(page, ['Compras:Ver', 'Compras:Editar', 'Compras:Confirmar']);

    await page.goto('/facturas-proveedor/1');

    await expect(page.getByRole('link', { name: 'Editar' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Registrar' })).toBeVisible();
  });
});
