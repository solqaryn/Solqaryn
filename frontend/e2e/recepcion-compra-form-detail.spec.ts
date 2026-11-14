import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConPermisos(page: Page, permisos: string[]): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, message: '', data: { permisos, esAdministrador: false, operacionMultiEmpresaRestringida: false, capacidadesHabilitadas: [], rutasHabilitadas: [] } })
  }));
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

function recepcion(estado: 1 | 2 | 3) {
  return {
    id: 41,
    numeroRecepcion: 'RC-2026-000041',
    ordenCompraId: 10,
    numeroOrdenCompra: 'OC-2026-000010',
    estado,
    observaciones: 'Recepción E2E',
    fechaRecepcionUtc: estado >= 2 ? '2026-08-19T16:00:00Z' : null,
    recibidaPorUsuarioId: estado >= 2 ? 1 : null,
    recibidaPorNombreSnapshot: estado >= 2 ? 'e2e_admin' : null,
    fechaAnulacionUtc: estado === 3 ? '2026-08-19T17:00:00Z' : null,
    anuladaPorUsuarioId: estado === 3 ? 1 : null,
    motivoAnulacion: estado === 3 ? 'Prueba E2E' : null,
    cantidadRecibidaTotal: 5,
    cantidadAceptadaTotal: 4,
    cantidadDanadaTotal: 1,
    cantidadFaltanteTotal: 0,
    cantidadSobranteTotal: 0,
    detalles: [{
      id: 401,
      ordenCompraDetalleId: 100,
      productoId: 7,
      productoVarianteId: null,
      almacenId: 2,
      ubicacionAlmacenId: null,
      cantidadRecibida: 5,
      cantidadAceptada: 4,
      cantidadDanada: 1,
      cantidadFaltante: 0,
      cantidadSobrante: 0,
      costoUnitarioSnapshot: 100,
      productoSkuSnapshot: 'SKU-007',
      productoNombreSnapshot: 'Producto E2E',
      productoMarcaSnapshot: 'Marca',
      productoModeloSnapshot: 'Modelo',
      productoColorSnapshot: 'Negro',
      productoTallaSnapshot: 'M'
    }]
  };
}

async function interceptarDetalle(page: Page, estadoInicial: 1 | 2 | 3): Promise<void> {
  await page.route('**/recepciones-compra/41**', async route => {
    const request = route.request();
    const url = new URL(request.url());
    if (request.method() === 'GET' && request.resourceType() !== 'document' && url.pathname === '/recepciones-compra/41') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: recepcion(estadoInicial) }) });
      return;
    }
    await route.continue();
  });
}

test.describe('Recepción de mercancía - formulario y detalle', () => {
  test('crea un borrador desde una orden aprobada y almacén activo sin IDs manuales', async ({ page }) => {
    await page.route('**/ordenes-compra**', async route => {
      const request = route.request();
      const url = new URL(request.url());
      if (request.method() === 'GET' && request.resourceType() !== 'document' && url.pathname === '/ordenes-compra') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({
          success: true, message: '', data: { items: [{ id: 10, numeroOrden: 'OC-2026-000010', estado: 3, solicitudCompraId: null, proveedorId: 5, proveedorNombre: 'Proveedor E2E', moneda: 'HNL', condicionesCompra: null, fechaEsperadaUtc: null, observaciones: null, subtotal: 1000, descuento: 0, impuesto: 0, total: 1000, fechaEnvioAprobacionUtc: null, fechaAprobacionUtc: '2026-08-19T10:00:00Z', fechaCancelacionUtc: null, detalles: [] }], page: 1, pageSize: 100, totalCount: 1, totalPages: 1 }
        }) });
        return;
      }
      await route.continue();
    });
    await page.route('**/almacenes/activos**', async route => {
      const request = route.request();
      if (request.method() === 'GET' && request.resourceType() !== 'document') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: [{ id: 2, sucursalId: 1, sucursalCodigo: 'TGU', sucursalNombre: 'Tegucigalpa', codigo: 'ALM-01', nombre: 'Principal', tipo: 'Venta', activo: true, fechaCreacion: '2026-01-01T00:00:00Z', fechaActualizacion: '2026-01-01T00:00:00Z' }] }) });
        return;
      }
      await route.continue();
    });
    await page.route('**/recepciones-compra/ordenes/10/saldo', async route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: '', data: { ordenCompraId: 10, numeroOrden: 'OC-2026-000010', estadoOrden: 3, completa: false, lineas: [{ ordenCompraDetalleId: 100, productoId: 7, productoVarianteId: null, productoSkuSnapshot: 'SKU-007', productoNombreSnapshot: 'Producto E2E', cantidadOrdenada: 10, cantidadAceptadaAcumulada: 0, cantidadPendiente: 10 }] } })
    }));

    let payload: any;
    await page.route('**/recepciones-compra', async route => {
      const request = route.request();
      if (request.method() === 'POST') {
        payload = request.postDataJSON();
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: recepcion(1) }) });
        return;
      }
      await route.continue();
    });

    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver', 'Compras:Crear']);
    await page.goto('/recepciones-compra/nueva');
    await expect(page.getByRole('heading', { name: 'Nueva recepción de mercancía' })).toBeVisible();

    await page.getByTestId('orden-compra-select').click();
    await page.getByRole('option', { name: /OC-2026-000010/ }).click();
    await expect(page.locator('.cdk-overlay-backdrop.cdk-overlay-backdrop-showing')).toHaveCount(0);
    await expect(page.getByText('SKU-007')).toBeVisible();

    await page.getByTestId('almacen-0').focus();
    await page.keyboard.press('Enter');
    await page.getByRole('option', { name: /ALM-01/ }).click();
    await page.getByTestId('recibida-0').fill('5');
    await page.getByTestId('danada-0').fill('1');
    await page.getByTestId('guardar-recepcion').click();

    await expect(page).toHaveURL(/\/recepciones-compra\/41$/);
    expect(payload.ordenCompraId).toBe(10);
    expect(payload.detalles).toHaveLength(1);
    expect(payload.detalles[0]).toMatchObject({ ordenCompraDetalleId: 100, almacenId: 2, cantidadRecibida: 5, cantidadDanada: 1, cantidadFaltante: 0, cantidadSobrante: 0 });
  });

  test('oculta Confirmar y Anular sin grants aunque exista Compras:Ver', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver']);
    await interceptarDetalle(page, 1);

    await page.goto('/recepciones-compra/41');

    await expect(page.locator('strong.status')).toHaveText('Borrador');
    await expect(page.getByTestId('confirmar-recepcion')).toHaveCount(0);
    await expect(page.getByTestId('anular-recepcion')).toHaveCount(0);
  });

  test('respeta estado y permisos al confirmar y anular con MatDialog', async ({ page }) => {
    let estado: 1 | 2 | 3 = 1;
    await page.route('**/recepciones-compra/41**', async route => {
      const request = route.request();
      const url = new URL(request.url());
      if (request.method() === 'GET' && request.resourceType() !== 'document' && url.pathname === '/recepciones-compra/41') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: recepcion(estado) }) });
        return;
      }
      if (request.method() === 'POST' && url.pathname.endsWith('/confirmar')) {
        estado = 2;
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: recepcion(estado) }) });
        return;
      }
      if (request.method() === 'POST' && url.pathname.endsWith('/anular')) {
        estado = 3;
        const body = request.postDataJSON();
        expect(body.motivo).toBe('Prueba E2E');
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: recepcion(estado) }) });
        return;
      }
      await route.continue();
    });

    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver', 'Compras:Confirmar', 'Compras:Anular']);
    await page.goto('/recepciones-compra/41');
    await expect(page.locator('strong.status')).toHaveText('Borrador');
    await expect(page.getByTestId('confirmar-recepcion')).toBeVisible();
    await expect(page.getByTestId('anular-recepcion')).toHaveCount(0);

    await page.getByTestId('confirmar-recepcion').click();
    const confirmDialog = page.getByRole('dialog');
    await expect(confirmDialog).toContainText('Confirmar recepción');
    await confirmDialog.getByRole('button', { name: 'Confirmar', exact: true }).click();
    await expect(page.locator('strong.status')).toHaveText('Recibida');
    await expect(page.getByTestId('confirmar-recepcion')).toHaveCount(0);
    await expect(page.getByTestId('anular-recepcion')).toBeVisible();

    await page.getByTestId('anular-recepcion').click();
    const anularDialog = page.getByRole('dialog');
    await anularDialog.getByLabel('Motivo de anulación').fill('Prueba E2E');
    await anularDialog.getByRole('button', { name: 'Anular', exact: true }).click();
    await expect(page.locator('strong.status')).toHaveText('Anulada');
    await expect(page.getByTestId('anular-recepcion')).toHaveCount(0);
  });
});
