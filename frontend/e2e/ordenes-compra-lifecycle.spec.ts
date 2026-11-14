import { expect, Page, Route, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

type Estado = 'Borrador' | 'PendienteAprobacion' | 'Aprobada' | 'Cancelada';

async function loginConPermisos(page: Page, permisos: string[]): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: 'Permisos cargados', data: { permisos, esAdministrador: false } })
    });
  });
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

function orden(estado: Estado) {
  return {
    id: 77,
    numeroOrden: 'OC-000077',
    estado,
    solicitudCompraId: 31,
    proveedorId: 11,
    proveedorNombre: 'Proveedor Uno',
    moneda: 'HNL',
    condicionesCompra: 'Crédito 30 días',
    fechaEsperadaUtc: '2026-08-31T00:00:00Z',
    observaciones: null,
    subtotal: 100,
    descuento: 0,
    impuesto: 0,
    total: 100,
    detalles: [{
      id: 1,
      productoId: 21,
      productoVarianteId: null,
      cantidadOrdenada: 1,
      precioUnitario: 100,
      descuento: 0,
      impuesto: 0,
      subtotal: 100,
      total: 100,
      productoNombreSnapshot: 'Producto Uno',
      productoSkuSnapshot: 'SKU-21'
    }]
  };
}

async function mockOrdenes(page: Page, estadoInicial: Estado, onCancel?: (motivo: string) => void): Promise<void> {
  let estado = estadoInicial;

  await page.route('**/proveedores/activos', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, message: '', errors: [], data: [{ id: 11, nombre: 'Proveedor Uno' }] })
  }));

  await page.route('**/ordenes-compra**', async (route: Route) => {
    const request = route.request();
    if (request.resourceType() === 'document') return route.fallback();

    const url = new URL(request.url());
    const path = url.pathname;
    const method = request.method();

    if (method === 'GET' && path.endsWith('/ordenes-compra')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: '',
          errors: [],
          data: { items: [orden(estado)], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 }
        })
      });
    }

    if (method === 'GET' && path.endsWith('/ordenes-compra/77')) {
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: orden(estado) }) });
    }

    if (method === 'POST' && path.endsWith('/ordenes-compra/77/enviar-aprobacion')) {
      estado = 'PendienteAprobacion';
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'Orden enviada a aprobación correctamente.', errors: [], data: orden(estado) }) });
    }

    if (method === 'POST' && path.endsWith('/ordenes-compra/77/aprobar')) {
      estado = 'Aprobada';
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'Orden de compra aprobada correctamente.', errors: [], data: orden(estado) }) });
    }

    if (method === 'POST' && path.endsWith('/ordenes-compra/77/cancelar')) {
      const body = request.postDataJSON() as { motivo?: string };
      onCancel?.(body.motivo ?? '');
      estado = 'Cancelada';
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'Orden de compra cancelada correctamente.', errors: [], data: orden(estado) }) });
    }

    return route.fallback();
  });
}

async function abrirDetalle(page: Page): Promise<void> {
  await page.goto('/ordenes-compra');
  await expect(page.getByRole('heading', { name: 'Órdenes de compra' })).toBeVisible();
  await page.getByRole('button', { name: 'Ver' }).click();
  await expect(page.getByRole('heading', { name: 'OC-000077' })).toBeVisible();
}

test.describe('OrdenCompra lifecycle - aprobación, cancelación y RBAC', () => {
  test('comprador envía a aprobación y aprobador completa la decisión sin recepción de inventario', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver', 'Compras:Confirmar', 'Compras:Aprobar']);
    await mockOrdenes(page, 'Borrador');

    page.on('dialog', dialog => void dialog.accept());
    await abrirDetalle(page);
    const detalle = page.locator('article.detail-panel');

    await detalle.getByRole('button', { name: 'Enviar a aprobación' }).click();
    await expect(detalle.getByRole('status')).toContainText('Orden enviada a aprobación correctamente.');
    await expect(detalle.getByText('Pendiente de aprobación', { exact: true })).toBeVisible();
    await expect(detalle.getByRole('button', { name: 'Aprobar' })).toBeVisible();

    await detalle.getByRole('button', { name: 'Aprobar' }).click();
    await expect(detalle.getByRole('status')).toContainText('Orden de compra aprobada correctamente.');
    await expect(detalle.getByText('Aprobada', { exact: true })).toBeVisible();
    await expect(detalle.getByRole('button', { name: 'Aprobar' })).toHaveCount(0);
    await expect(detalle.getByRole('button', { name: 'Cancelar' })).toHaveCount(0);
  });

  test('cancelación exige motivo y envía la causa al backend', async ({ page }) => {
    let motivoCapturado = '';
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver', 'Compras:Anular']);
    await mockOrdenes(page, 'Borrador', motivo => { motivoCapturado = motivo; });

    page.on('dialog', dialog => {
      if (dialog.type() === 'prompt') return void dialog.accept('Proveedor no puede cumplir la fecha');
      void dialog.accept();
    });
    await abrirDetalle(page);
    const detalle = page.locator('article.detail-panel');

    await detalle.getByRole('button', { name: 'Cancelar' }).click();
    await expect(detalle.getByRole('status')).toContainText('Orden de compra cancelada correctamente.');
    await expect(detalle.getByText('Cancelada', { exact: true })).toBeVisible();
    expect(motivoCapturado).toBe('Proveedor no puede cumplir la fecha');
  });

  test('usuario de consulta no ve acciones de lifecycle aunque pueda abrir el detalle', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver']);
    await mockOrdenes(page, 'Borrador');
    await abrirDetalle(page);
    const detalle = page.locator('article.detail-panel');

    await expect(detalle.getByRole('button', { name: 'Enviar a aprobación' })).toHaveCount(0);
    await expect(detalle.getByRole('button', { name: 'Aprobar' })).toHaveCount(0);
    await expect(detalle.getByRole('button', { name: 'Cancelar' })).toHaveCount(0);
  });
});
