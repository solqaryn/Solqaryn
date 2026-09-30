import { test, expect } from '@playwright/test';

test.describe('Transferencias de inventario - formulario', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('solqaryn_token', 'e2e-token-transferencias-form');
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
          permisos: ['MovimientosInventario:Ver', 'MovimientosInventario:Crear'],
          esAdministrador: false
        }
      })
    }));

    await page.route('**/almacenes/activos', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: [
          { id: 1, sucursalId: 1, sucursalCodigo: 'CEN', sucursalNombre: 'Centro', codigo: 'ALM-01', nombre: 'Tienda Centro', tipo: 'Tienda', activo: true, fechaCreacion: '', fechaActualizacion: '' },
          { id: 2, sucursalId: 1, sucursalCodigo: 'CEN', sucursalNombre: 'Centro', codigo: 'ALM-02', nombre: 'Bodega Norte', tipo: 'Bodega', activo: true, fechaCreacion: '', fechaActualizacion: '' }
        ]
      })
    }));

    await page.route('**/ubicaciones-almacen/activas**', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: [
          { id: 10, almacenId: 1, almacenCodigo: 'ALM-01', almacenNombre: 'Tienda Centro', codigo: 'PISO', nombre: 'Piso venta', tipo: 'Piso', activa: true, fechaCreacion: '', fechaActualizacion: '' },
          { id: 20, almacenId: 2, almacenCodigo: 'ALM-02', almacenNombre: 'Bodega Norte', codigo: 'RACK-A', nombre: 'Rack A', tipo: 'Rack', activa: true, fechaCreacion: '', fechaActualizacion: '' }
        ]
      })
    }));

    await page.route('**/productos?**', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: {
          items: [{
            id: 7,
            nombre: 'Funda Premium',
            marca: 'Storefront',
            modelo: 'S24',
            cantidad: 10,
            costo: 100,
            precio: 200,
            precioMinimo: 200,
            precioMaximo: 200,
            umbralStockBajo: 1,
            tieneStockBajo: false,
            estaAgotado: false,
            estadoInventario: 'Disponible',
            activo: true,
            imagenes: [],
            totalImagenes: 0,
            totalVariantes: 1,
            usaVariantes: true,
            fechaCreacion: '',
            fechaActualizacion: '',
            variantes: [{
              id: 77,
              productoId: 7,
              productoNombre: 'Funda Premium',
              marcaNombre: 'Storefront',
              modeloNombre: 'S24',
              colorNombre: 'Negro',
              tallaNombre: 'Única',
              sku: 'SKU-77',
              cantidad: 10,
              umbralStockBajo: 1,
              costo: 100,
              precio: 200,
              activo: true,
              eliminado: false,
              tieneStockBajo: false,
              estaAgotada: false,
              estadoInventario: 'Disponible',
              fechaCreacion: '',
              fechaActualizacion: ''
            }]
          }],
          totalCount: 1,
          page: 1,
          pageSize: 500,
          totalPages: 1
        }
      })
    }));
  });

  test('crea una transferencia seleccionando almacenes, variante y ubicaciones', async ({ page }) => {
    let payload: unknown;
    const creada = {
      id: 41,
      numero: 'TR-000041',
      estado: 'Borrador',
      almacenOrigenId: 1,
      almacenOrigenNombre: 'Tienda Centro',
      almacenDestinoId: 2,
      almacenDestinoNombre: 'Bodega Norte',
      observaciones: 'Reposición de tienda',
      fechaSolicitud: null,
      fechaAprobacion: null,
      fechaDespacho: null,
      fechaRecepcion: null,
      fechaCancelacion: null,
      motivoCancelacion: null,
      detalles: []
    };

    await page.route('**/transferencias-inventario/41', async route => {
      if (route.request().method() !== 'GET') return route.continue();
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, data: creada })
      });
    });

    await page.route('**/transferencias-inventario', async route => {
      if (route.request().method() !== 'POST') return route.continue();
      payload = route.request().postDataJSON();
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, data: creada })
      });
    });

    await page.goto('/inventario/transferencias/nueva');
    await expect(page.getByRole('heading', { name: 'Nueva transferencia' })).toBeVisible();

    await page.getByLabel('Almacén origen').click();
    await page.getByRole('option', { name: /ALM-01.*Tienda Centro/ }).click();
    await page.locator('.cdk-overlay-backdrop').waitFor({ state: 'detached' });

    await page.getByLabel('Almacén destino').click();
    await page.getByRole('option', { name: /ALM-02.*Bodega Norte/ }).click();
    await page.locator('.cdk-overlay-backdrop').waitFor({ state: 'detached' });

    await page.getByLabel('Variante').click();
    await page.getByRole('option', { name: /Funda Premium.*SKU SKU-77/ }).click();
    await page.locator('.cdk-overlay-backdrop').waitFor({ state: 'detached' });

    const ubicacionOrigen = page.getByLabel('Ubicación origen');
    await ubicacionOrigen.focus();
    await ubicacionOrigen.press('Enter');
    await page.getByRole('option', { name: /PISO.*Piso venta/ }).click();
    await page.locator('.cdk-overlay-backdrop').waitFor({ state: 'detached' });

    const ubicacionDestino = page.getByLabel('Ubicación destino');
    await ubicacionDestino.focus();
    await ubicacionDestino.press('Enter');
    await page.getByRole('option', { name: /RACK-A.*Rack A/ }).click();
    await page.locator('.cdk-overlay-backdrop').waitFor({ state: 'detached' });

    await page.getByLabel('Cantidad').fill('3');
    await page.getByLabel('Observaciones').fill('Reposición de tienda');
    await page.getByRole('button', { name: 'Crear transferencia' }).click();

    await expect.poll(() => payload).toEqual({
      almacenOrigenId: 1,
      almacenDestinoId: 2,
      observaciones: 'Reposición de tienda',
      detalles: [{
        productoVarianteId: 77,
        ubicacionOrigenId: 10,
        ubicacionDestinoId: 20,
        cantidadSolicitada: 3
      }]
    });
    await expect(page).toHaveURL(/\/inventario\/transferencias\/41$/);
    await expect(page.getByRole('heading', { name: 'TR-000041' })).toBeVisible();
  });
});
