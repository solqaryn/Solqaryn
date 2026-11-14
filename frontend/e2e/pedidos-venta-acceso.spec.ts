import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

type PermisoVentas =
  | 'Ventas:Ver'
  | 'Ventas:Crear'
  | 'Ventas:Editar'
  | 'Ventas:Confirmar'
  | 'Ventas:Anular';

async function loginConPermisos(page: Page, permisos: PermisoVentas[]): Promise<void> {
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

async function mockPedidosVenta(page: Page): Promise<void> {
  await page.route(/\/pedidos-venta(?:\/1)?(?:\?.*)?$/, async route => {
    const request = route.request();
    const url = new URL(request.url());
    const esApiPedido = request.resourceType() !== 'document'
      && (url.pathname === '/pedidos-venta' || url.pathname === '/pedidos-venta/1');

    if (!esApiPedido) {
      await route.continue();
      return;
    }

    if (url.pathname === '/pedidos-venta/1') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: 'Pedido cargado',
          data: {
            id: 1,
            cotizacionId: 10,
            clienteId: 20,
            clienteNombreSnapshot: 'Cliente Test',
            clienteDocumentoSnapshot: '0801199912345',
            estado: 1,
            observaciones: null,
            fechaConfirmacionUtc: null,
            fechaAnulacionUtc: null,
            motivoAnulacion: null,
            subtotal: 100,
            descuento: 0,
            impuesto: 0,
            total: 100,
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
        message: 'Pedidos cargados',
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

test.describe('PedidoVenta - seguridad frontend', () => {
  test('redirige a login cuando no existe sesión autenticada', async ({ page }) => {
    await page.goto('/pedidos-venta');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega el listado a un usuario sin Ventas:Ver', async ({ page }) => {
    await loginConPermisos(page, []);
    await page.goto('/pedidos-venta');

    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
    await expect(page).not.toHaveURL(/\/pedidos-venta(?:\?|$)/);
  });

  test('respeta Ventas:Crear en la acción Nuevo pedido', async ({ page }) => {
    await mockPedidosVenta(page);
    await loginConPermisos(page, ['Ventas:Ver']);
    await page.goto('/pedidos-venta');

    await expect(page.getByRole('heading', { name: 'Pedidos de venta' })).toBeVisible();
    await expect(page.getByRole('link', { name: /Nuevo pedido/ })).toHaveCount(0);

    await page.unroute('**/permisos/mis-permisos/empresa/*');
  });

  test('muestra Nuevo pedido cuando existe Ventas:Crear', async ({ page }) => {
    await mockPedidosVenta(page);
    await loginConPermisos(page, ['Ventas:Ver', 'Ventas:Crear']);
    await page.goto('/pedidos-venta');

    await expect(page.getByRole('link', { name: /Nuevo pedido/ })).toBeVisible();
  });

  test('oculta acciones mutativas del detalle sin permisos', async ({ page }) => {
    await mockPedidosVenta(page);
    await loginConPermisos(page, ['Ventas:Ver']);
    await page.goto('/pedidos-venta/1');

    await expect(page.getByRole('heading', { name: 'Pedido PED-1' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Editar' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Confirmar' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Anular' })).toHaveCount(0);
  });

  test('muestra Editar y Confirmar solo con permisos del lifecycle en Borrador', async ({ page }) => {
    await mockPedidosVenta(page);
    await loginConPermisos(page, ['Ventas:Ver', 'Ventas:Editar', 'Ventas:Confirmar']);
    await page.goto('/pedidos-venta/1');

    await expect(page.getByRole('link', { name: 'Editar' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Confirmar' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Anular' })).toHaveCount(0);
  });
});
