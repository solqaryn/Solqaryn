import { expect, Page, test } from '@playwright/test';

const API_URL = 'http://localhost:5005';

const metodos = [
  { id: 1, codigo: 'EFECTIVO', nombre: 'Efectivo', tipo: 'Efectivo', activo: true, eliminado: false, requiereReferencia: false, requiereBanco: false, permiteCambio: true, orden: 0, metadata: null },
  { id: 2, codigo: 'TRANSFERENCIA', nombre: 'Transferencia bancaria', tipo: 'Transferencia', activo: true, eliminado: false, requiereReferencia: true, requiereBanco: true, permiteCambio: false, orden: 1, metadata: null },
  { id: 3, codigo: 'TARJETA', nombre: 'Tarjeta', tipo: 'Tarjeta', activo: true, eliminado: false, requiereReferencia: true, requiereBanco: false, permiteCambio: false, orden: 2, metadata: null },
  { id: 4, codigo: 'LEGACY_OFF', nombre: 'Método fuera de uso', tipo: 'Otro', activo: false, eliminado: false, requiereReferencia: false, requiereBanco: false, permiteCambio: false, orden: 3, metadata: null }
];

function api<T>(data: T) {
  return { success: true, message: 'OK', errors: [], data };
}

async function prepararSesion(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'e2e-token-n05');
    localStorage.setItem('solqaryn_user', 'e2e_admin');
    localStorage.setItem('solqaryn_nombre_completo', 'Administrador E2E');
    localStorage.setItem('solqaryn_rol', 'Administrador');
    localStorage.setItem('solqaryn_expira_en', '2099-12-31T23:59:59Z');
  });

  await page.route(`${API_URL}/permisos/mis-permisos/empresa/*`, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(api({ esAdministrador: true, permisos: [] }))
  }));
}

async function mockCatalogos(page: Page): Promise<void> {
  await page.route(`${API_URL}/metodos-pago/activos`, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(api(metodos))
  }));
  await page.route(`${API_URL}/metodos-pago/bancos-activos`, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(api([{ id: 10, codigo: 'BAC', nombre: 'BAC Credomatic' }]))
  }));
}

test.describe('ERP N0.5 — regresión relacional de métodos de pago', () => {
  test.beforeEach(async ({ page }) => {
    await prepararSesion(page);
    await mockCatalogos(page);
  });

  test('venta editada conserva método histórico inactivo sin ofrecerlo para nuevas operaciones', async ({ page }) => {
    await page.route(`${API_URL}/costos-envio`, route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(api([]))
    }));
    await page.route(`${API_URL}/ventas/77`, route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(api({
        id: 77,
        numeroVenta: 'V-000077',
        clienteNombre: 'Cliente histórico',
        clienteTelefono: '',
        clienteIdentidadORTN: '',
        clienteCorreo: '',
        clienteDireccion: '',
        metodoPago: 'LEGACY_OFF',
        estadoPago: 'Pendiente',
        estado: 'Borrador',
        costoEnvioId: null,
        costoEnvioNombre: null,
        costoEnvio: 0,
        envioExonerado: false,
        motivoExoneracionEnvio: null,
        notas: '',
        detalles: [],
        importeBruto: 0,
        importeProductos: 0,
        subtotal: 0,
        descuentosAplicados: [],
        descuento: 0,
        impuestosAplicados: [],
        impuesto: 0,
        total: 0
      }))
    }));

    await page.goto('/ventas/77/editar');
    await expect(page.getByRole('heading', { name: 'Editar venta (Borrador)' })).toBeVisible();
    await expect(page.getByText('El método histórico se conserva solo para lectura; selecciona uno activo para una nueva operación.')).toBeVisible();

    const selector = page.locator('app-metodo-pago-select mat-select');
    await selector.click();
    await expect(page.getByRole('option', { name: 'Efectivo' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'Transferencia bancaria' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'Tarjeta' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'LEGACY_OFF (histórico/inactivo)' })).toBeDisabled();
    await expect(page.getByRole('option', { name: 'Método fuera de uso' })).toHaveCount(0);
  });

  test('pagos aplica reglas de efectivo, transferencia, tarjeta, referencia, banco e histórico', async ({ page }) => {
    const facturaBase = {
      id: 88,
      numeroFactura: 'F-000088',
      clienteNombre: 'Cliente N0.5',
      estado: 'Emitida',
      total: 150,
      totalPagado: 50,
      saldoPendiente: 100,
      pagos: [
        {
          id: 501,
          fechaPago: '2026-08-13T12:00:00Z',
          metodoPago: 'Tarjeta histórica renombrada',
          bancoId: null,
          bancoCodigo: null,
          bancoNombre: null,
          referencia: 'HIST-001',
          montoRecibido: 50,
          monto: 50,
          cambio: 0,
          anulado: false,
          motivoAnulacion: null
        }
      ]
    };

    await page.route(`${API_URL}/facturas/88`, route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(api(facturaBase))
    }));

    let payloadRegistrado: Record<string, unknown> | null = null;
    await page.route(`${API_URL}/facturas/88/pagos`, async route => {
      payloadRegistrado = route.request().postDataJSON() as Record<string, unknown>;
      const actualizado = {
        ...facturaBase,
        totalPagado: 150,
        saldoPendiente: 0,
        pagos: [
          ...facturaBase.pagos,
          {
            id: 502,
            fechaPago: '2026-08-13T14:10:00Z',
            metodoPago: 'Transferencia bancaria',
            bancoId: 10,
            bancoCodigo: 'BAC',
            bancoNombre: 'BAC Credomatic',
            referencia: 'TRX-N05-001',
            montoRecibido: 100,
            monto: 100,
            cambio: 0,
            anulado: false,
            motivoAnulacion: null
          }
        ]
      };
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(api(actualizado)) });
    });

    await page.goto('/facturas/88/pagos');
    const historial = page.locator('.historial tbody');
    await expect(page.getByRole('heading', { name: 'Pagos de F-000088' })).toBeVisible();
    await expect(historial.getByText('Tarjeta histórica renombrada')).toBeVisible();
    await expect(historial.getByText('HIST-001')).toBeVisible();

    const metodoField = page.locator('mat-form-field').filter({ hasText: 'Método de pago' });
    const metodoSelect = metodoField.locator('mat-select');
    const referenciaField = page.locator('mat-form-field').filter({ hasText: /^Referencia/ });

    await metodoSelect.click();
    await expect(page.getByRole('option', { name: 'Efectivo' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'Transferencia bancaria' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'Tarjeta' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'Método fuera de uso' })).toHaveCount(0);
    await page.getByRole('option', { name: 'Efectivo' }).click();
    await expect(page.locator('mat-form-field').filter({ hasText: 'Banco *' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Registrar pago' })).toBeEnabled();

    await metodoSelect.click();
    await page.getByRole('option', { name: 'Tarjeta' }).click();
    await expect(referenciaField.getByText('Obligatoria para este método.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Registrar pago' })).toBeDisabled();

    await metodoSelect.click();
    await page.getByRole('option', { name: 'Transferencia bancaria' }).click();
    const bancoField = page.locator('mat-form-field').filter({ hasText: 'Banco *' });
    await expect(referenciaField.getByText('Obligatoria para este método.')).toBeVisible();
    await expect(bancoField).toBeVisible();
    await expect(page.getByRole('button', { name: 'Registrar pago' })).toBeDisabled();

    await referenciaField.locator('input').fill('TRX-N05-001');
    await bancoField.locator('mat-select').click();
    await page.getByRole('option', { name: 'BAC Credomatic (BAC)' }).click();
    await expect(page.getByRole('button', { name: 'Registrar pago' })).toBeEnabled();

    await page.getByRole('button', { name: 'Registrar pago' }).click();
    await expect(historial.getByText('TRX-N05-001')).toBeVisible();
    await expect(historial.getByText('Transferencia bancaria')).toBeVisible();
    expect(payloadRegistrado).toMatchObject({
      monto: 100,
      metodoPago: 'TRANSFERENCIA',
      bancoId: 10,
      referencia: 'TRX-N05-001'
    });
  });
});
