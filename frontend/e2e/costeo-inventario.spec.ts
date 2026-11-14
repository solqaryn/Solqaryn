import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConPermisosCosteo(page: Page): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'Permisos cargados',
        data: { permisos: ['Dashboard:Ver', 'MovimientosInventario:Ver', 'MovimientosInventario:Editar'], esAdministrador: false }
      })
    });
  });

  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

function politica(metodo = 1, nombre = 'Promedio Ponderado') {
  return {
    id: metodo,
    empresaConfiguracionId: 1,
    metodo,
    metodoNombre: nombre,
    vigenteDesdeUtc: '2026-08-18T09:00:00Z',
    vigenteHastaUtc: null,
    estaVigente: true,
    motivo: 'Política inicial',
    fechaCreacion: '2026-08-18T09:00:00Z',
    fechaActualizacion: '2026-08-18T09:00:00Z'
  };
}

async function mockLecturas(page: Page): Promise<void> {
  await page.route('**/costeo-inventario/politica-vigente', async route => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: politica() }) });
      return;
    }
    await route.continue();
  });
  await page.route('**/costeo-inventario/metodos', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, message: '', errors: [], data: [
      { id: 1, nombre: 'Promedio Ponderado' }, { id: 2, nombre: 'FIFO' }, { id: 3, nombre: 'Estándar' }
    ] })
  }));
  await page.route('**/costeo-inventario/politicas**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, message: '', errors: [], data: { items: [politica()], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 } })
  }));
}

test.describe('Costeo de inventario - política empresarial', () => {
  test('muestra política vigente, historial y publica un cambio con motivo obligatorio', async ({ page }) => {
    await loginConPermisosCosteo(page);
    await mockLecturas(page);

    let payload: unknown;
    await page.route('**/costeo-inventario/politica-vigente', async route => {
      if (route.request().method() !== 'PUT') {
        await route.fallback();
        return;
      }
      payload = route.request().postDataJSON();
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, message: 'Política de costeo actualizada correctamente.', errors: [], data: politica(2, 'FIFO') })
      });
    });

    await page.goto('/inventario/costeo');
    await expect(page.getByRole('heading', { name: 'Política de costeo' })).toBeVisible();
    await expect(page.getByText('Promedio Ponderado', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('Política inicial').first()).toBeVisible();

    const cambio = page.locator('.change-form');
    await cambio.locator('mat-select').click();
    await page.getByRole('option', { name: 'FIFO', exact: true }).click();
    await cambio.locator('textarea[name="motivo"]').fill('Cambio aprobado por cierre contable');
    await cambio.getByRole('button', { name: 'Aplicar política' }).click();

    await expect.poll(() => payload).toEqual({ metodo: 2, motivo: 'Cambio aprobado por cierre contable' });
    await expect(page.getByText('Política de costeo actualizada correctamente.')).toBeVisible();
    await expect(page.getByText('FIFO', { exact: true }).first()).toBeVisible();
  });

  test('bloquea el mismo método y motivos menores al contrato mínimo antes del API', async ({ page }) => {
    await loginConPermisosCosteo(page);
    await mockLecturas(page);
    await page.goto('/inventario/costeo');

    const cambio = page.locator('.change-form');
    const boton = cambio.getByRole('button', { name: 'Aplicar política' });
    const motivo = cambio.locator('textarea[name="motivo"]');

    await motivo.fill('Motivo válido para probar el mismo método');
    await expect(boton).toBeDisabled();

    await cambio.locator('mat-select').click();
    await page.getByRole('option', { name: 'FIFO', exact: true }).click();
    await motivo.fill('ab');
    await expect(boton).toBeDisabled();

    await motivo.fill('abc');
    await expect(boton).toBeEnabled();
  });

  test('convierte filtros temporales a UTC y bloquea rangos invertidos antes del API', async ({ page }) => {
    await loginConPermisosCosteo(page);
    await mockLecturas(page);
    await page.goto('/inventario/costeo');
    await expect(page.getByRole('heading', { name: 'Política de costeo' })).toBeVisible();

    const desde = page.locator('input[name="desdeUtc"]');
    const hasta = page.locator('input[name="hastaUtc"]');
    await desde.fill('2026-08-19T10:00');
    await hasta.fill('2026-08-18T10:00');
    await page.getByRole('button', { name: 'Filtrar' }).click();
    await expect(page.getByText('La fecha “Desde” no puede ser posterior a “Hasta”.')).toBeVisible();

    await desde.fill('2026-08-18T08:00');
    await hasta.fill('2026-08-19T10:00');
    const requestPromise = page.waitForRequest(request => request.url().includes('/costeo-inventario/politicas') && request.url().includes('desdeUtc='));
    await page.getByRole('button', { name: 'Filtrar' }).click();
    const request = await requestPromise;
    const url = new URL(request.url());
    expect(url.searchParams.get('desdeUtc')).toMatch(/Z$/);
    expect(url.searchParams.get('hastaUtc')).toMatch(/Z$/);
  });
});
