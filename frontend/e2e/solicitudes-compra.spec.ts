import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

const borrador = {
  id: 701,
  numeroSolicitud: 'SC-2026-000701',
  estado: 'Borrador',
  proveedorId: 31,
  proveedorNombre: 'Proveedor E2E',
  notas: 'Reposición documental',
  fechaSolicitudUtc: null,
  solicitadaPorUsuarioId: null,
  solicitadaPorNombreSnapshot: null,
  fechaDecisionUtc: null,
  decididaPorUsuarioId: null,
  decididaPorNombreSnapshot: null,
  motivoRechazo: null,
  detalles: [{
    id: 1,
    productoId: 11,
    productoVarianteId: 111,
    cantidadSolicitada: 3,
    costoEstimadoUnitario: 125,
    observacion: 'Línea E2E',
    productoSkuSnapshot: 'SKU-E2E-AZUL',
    productoNombreSnapshot: 'Producto E2E',
    productoMarcaSnapshot: 'Marca E2E',
    productoModeloSnapshot: 'Modelo E2E',
    productoColorSnapshot: 'Azul',
    productoTallaSnapshot: 'M'
  }]
};

async function login(page: Page, permisos: string[] = ['Dashboard:Ver', 'Compras:Ver', 'Compras:Crear', 'Compras:Editar', 'Compras:Confirmar', 'Compras:Aprobar', 'Compras:Rechazar']): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, message: '', errors: [], data: { permisos, esAdministrador: false } })
  }));

  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

async function mockCatalogos(page: Page): Promise<void> {
  await page.route('**/proveedores/activos', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, message: '', errors: [], data: [{ id: 31, nombre: 'Proveedor E2E', activo: true }] })
  }));
  await page.route('**/productos?**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, message: '', errors: [], data: { items: [{ id: 11, nombre: 'Producto E2E', marcaNombre: 'Marca E2E', activo: true, variantes: [], imagenes: [] }], page: 1, pageSize: 100, totalCount: 1, totalPages: 1 } })
  }));
  await page.route('**/productos/11/variantes**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, message: '', errors: [], data: [{ id: 111, productoId: 11, productoNombre: 'Producto E2E', sku: 'SKU-E2E-AZUL', etiqueta: 'SKU-E2E-AZUL · Azul · M', activo: true, eliminado: false }] })
  }));
}

async function mockListado(page: Page): Promise<void> {
  await page.route(/\/solicitudes-compra(?:\?.*)?$/, route => {
    if (route.request().method() !== 'GET' || route.request().resourceType() === 'document') return route.fallback();
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: '', errors: [], data: { items: [borrador], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } })
    });
  });
  await page.route('**/solicitudes-compra/701', route => {
    if (route.request().method() !== 'GET') return route.fallback();
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: borrador }) });
  });
}

test.describe('Solicitud de compra - frontend documental', () => {
  test('crea un borrador con producto, variante, proveedor y costo estimado', async ({ page }) => {
    await login(page);
    await mockCatalogos(page);
    await mockListado(page);

    let payload: Record<string, unknown> | undefined;
    await page.route('**/solicitudes-compra', async route => {
      if (route.request().method() !== 'POST') return route.fallback();
      payload = route.request().postDataJSON() as Record<string, unknown>;
      await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: borrador }) });
    });

    await page.goto('/solicitudes-compra');
    await page.getByRole('button', { name: 'Nueva solicitud' }).click();
    await expect(page.getByRole('heading', { name: 'Nueva solicitud de compra' })).toBeVisible();

    const selects = page.locator('form mat-select');
    await selects.nth(0).click();
    await page.getByRole('option', { name: 'Proveedor E2E', exact: true }).click();
    await selects.nth(1).click();
    await page.getByRole('option', { name: /Producto E2E/ }).click();
    await selects.nth(2).click();
    await page.getByRole('option', { name: /SKU-E2E-AZUL/ }).click();

    await page.locator('input[name="cantidad-0"]').fill('3');
    await page.locator('input[name="costo-0"]').fill('125');
    await page.getByRole('button', { name: 'Guardar borrador' }).click();

    await expect.poll(() => payload).toMatchObject({
      proveedorId: 31,
      detalles: [{ productoId: 11, productoVarianteId: 111, cantidadSolicitada: 3, costoEstimadoUnitario: 125 }]
    });
    await expect(page.getByRole('heading', { name: 'Detalle de solicitud de compra' })).toBeVisible();
  });

  test('expone editar y enviar solo para Borrador y ejecuta Enviar con permiso Confirmar', async ({ page }) => {
    await login(page);
    await mockCatalogos(page);
    await mockListado(page);

    let enviados = 0;
    await page.route('**/solicitudes-compra/701/enviar', async route => {
      enviados += 1;
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: { ...borrador, estado: 'Solicitada', fechaSolicitudUtc: '2026-08-18T17:30:00Z' } }) });
    });

    await page.goto('/solicitudes-compra?detalle=701');
    await expect(page.getByRole('heading', { name: 'Detalle de solicitud de compra' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Editar borrador' })).toBeVisible();
    await page.getByRole('button', { name: 'Enviar', exact: true }).click();
    await expect.poll(() => enviados).toBe(1);
    await expect(page.getByText('Solicitada', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Editar borrador' })).toHaveCount(0);
  });

  test('un usuario con solo Compras:Ver no recibe controles de mutación', async ({ page }) => {
    await login(page, ['Dashboard:Ver', 'Compras:Ver']);
    await mockCatalogos(page);
    await mockListado(page);

    await page.goto('/solicitudes-compra');
    await expect(page.getByRole('button', { name: 'Nueva solicitud' })).toHaveCount(0);
    await page.goto('/solicitudes-compra?detalle=701');
    await expect(page.getByRole('heading', { name: 'Detalle de solicitud de compra' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Editar borrador' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Enviar', exact: true })).toHaveCount(0);
  });
});
