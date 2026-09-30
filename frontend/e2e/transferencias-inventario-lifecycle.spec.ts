import { test, expect } from '@playwright/test';

const transferencia = (estado = 'Borrador') => ({
  id: 41,
  numero: 'TR-000041',
  almacenOrigenId: 1,
  almacenOrigenNombre: 'Tienda Centro',
  almacenDestinoId: 2,
  almacenDestinoNombre: 'Bodega Norte',
  estado,
  observaciones: 'Reposición interna',
  fechaSolicitud: estado === 'Borrador' ? null : '2026-08-16T10:00:00Z',
  fechaAprobacion: null,
  fechaDespacho: null,
  fechaRecepcion: null,
  fechaCancelacion: null,
  motivoCancelacion: null,
  detalles: [{
    id: 501,
    productoVarianteId: 77,
    ubicacionOrigenId: 10,
    ubicacionDestinoId: 20,
    cantidadSolicitada: 3,
    cantidadAprobada: 0,
    cantidadDespachada: 0,
    cantidadRecibida: 0,
    cantidadFaltante: 0,
    cantidadSobrante: 0,
    cantidadDanada: 0,
    productoSkuSnapshot: 'SKU-77',
    productoMarcaSnapshot: 'Marca',
    productoModeloSnapshot: 'Modelo',
    productoColorSnapshot: 'Negro',
    productoTallaSnapshot: 'M'
  }]
});

const transferenciaEnTransito = () => ({
  ...transferencia('EnTransito'),
  fechaAprobacion: '2026-08-16T10:05:00Z',
  fechaDespacho: '2026-08-16T10:10:00Z',
  detalles: transferencia().detalles.map(detalle => ({
    ...detalle,
    cantidadAprobada: 3,
    cantidadDespachada: 3
  }))
});

async function confirmarAlerta(page: import('@playwright/test').Page, titulo: string, confirmarTexto: string): Promise<void> {
  await expect(page.getByRole('heading', { name: titulo })).toBeVisible();
  await page.locator('.app-alert__confirm').filter({ hasText: confirmarTexto }).click();
}

test.describe('Transferencias de inventario - lifecycle UI', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('solqaryn_token', 'e2e-token-transferencias-lifecycle');
      localStorage.setItem('solqaryn_user', 'admin-e2e');
      localStorage.setItem('solqaryn_nombre_completo', 'Admin E2E');
      localStorage.setItem('solqaryn_rol', 'Administrador');
      localStorage.setItem('solqaryn_expira_en', '2099-12-31T23:59:59Z');
    });

    await page.route('**/permisos/mis-permisos/empresa/*', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'Permisos cargados',
        data: {
          permisos: [
            'MovimientosInventario:Ver',
            'MovimientosInventario:Crear',
            'MovimientosInventario:Editar',
            'MovimientosInventario:CambiarEstado',
            'MovimientosInventario:Aprobar',
            'MovimientosInventario:Confirmar',
            'MovimientosInventario:Anular'
          ],
          esAdministrador: false
        }
      })
    }));
  });

  test('lista, abre detalle y solicita un borrador', async ({ page }) => {
    await page.route('**/transferencias-inventario?**', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: { items: [transferencia()], totalCount: 1, page: 1, pageSize: 20, totalPages: 1 }
      })
    }));

    await page.route('**/transferencias-inventario/41', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, data: transferencia() })
    }));

    let solicitudRegistrada = false;
    await page.route('**/transferencias-inventario/41/solicitar', async route => {
      solicitudRegistrada = true;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, data: transferencia('Solicitada') })
      });
    });

    await page.goto('/inventario/transferencias');
    await expect(page.getByText('TR-000041')).toBeVisible();
    await expect(page.getByText('Tienda Centro')).toBeVisible();

    await page.getByRole('button', { name: 'Ver' }).click();
    await expect(page).toHaveURL(/\/inventario\/transferencias\/41$/);
    await expect(page.getByRole('heading', { name: 'TR-000041' })).toBeVisible();

    await page.getByRole('button', { name: 'Solicitar' }).click();
    await confirmarAlerta(page, 'Solicitar transferencia', 'Solicitar');

    await expect.poll(() => solicitudRegistrada).toBe(true);
    await expect(page.getByRole('strong').filter({ hasText: /^Solicitada$/ })).toBeVisible();
  });

  test('registra recepción parcial con faltante, daño y sobrante', async ({ page }) => {
    const enTransito = transferenciaEnTransito();
    await page.route('**/transferencias-inventario/41', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, data: enTransito })
    }));

    let payload: unknown;
    await page.route('**/transferencias-inventario/41/recibir', async route => {
      payload = route.request().postDataJSON();
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          data: {
            ...enTransito,
            estado: 'Recibida',
            fechaRecepcion: '2026-08-16T10:30:00Z',
            detalles: enTransito.detalles.map(detalle => ({
              ...detalle,
              cantidadRecibida: 1,
              cantidadFaltante: 1,
              cantidadDanada: 1,
              cantidadSobrante: 2
            }))
          }
        })
      });
    });

    await page.goto('/inventario/transferencias/41');
    const recepcion = page.locator('section.recepcion');
    await expect(recepcion.getByRole('heading', { name: 'Recepción y discrepancias' })).toBeVisible();

    const cantidades = recepcion.getByRole('spinbutton');
    await expect(cantidades).toHaveCount(4);
    await cantidades.nth(0).fill('1');
    await cantidades.nth(1).fill('1');
    await cantidades.nth(2).fill('1');
    await cantidades.nth(3).fill('2');
    await expect(recepcion.getByText('Cuadra', { exact: true })).toBeVisible();

    await page.getByRole('button', { name: 'Registrar recepción' }).click();
    await confirmarAlerta(page, 'Registrar recepción', 'Registrar recepción');

    await expect.poll(() => payload).toEqual({
      detalles: [{
        detalleId: 501,
        cantidadRecibida: 1,
        cantidadFaltante: 1,
        cantidadDanada: 1,
        cantidadSobrante: 2
      }]
    });
    await expect(page.getByRole('strong').filter({ hasText: /^Recibida$/ })).toBeVisible();
  });
});
