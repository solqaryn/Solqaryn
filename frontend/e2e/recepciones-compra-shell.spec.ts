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
        data: { permisos, esAdministrador: false, operacionMultiEmpresaRestringida: false, capacidadesHabilitadas: [], rutasHabilitadas: [] }
      })
    });
  });

  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

function respuestaListado() {
  return {
    success: true,
    message: '',
    errors: [],
    data: {
      items: [{
        id: 41,
        numeroRecepcion: 'RC-2026-000041',
        ordenCompraId: 17,
        numeroOrdenCompra: 'OC-2026-000017',
        estado: 1,
        observaciones: null,
        fechaRecepcionUtc: null,
        recibidaPorUsuarioId: null,
        recibidaPorNombreSnapshot: null,
        fechaAnulacionUtc: null,
        anuladaPorUsuarioId: null,
        motivoAnulacion: null,
        cantidadRecibidaTotal: 8,
        cantidadAceptadaTotal: 7,
        cantidadDanadaTotal: 1,
        cantidadFaltanteTotal: 2,
        cantidadSobranteTotal: 0,
        detalles: []
      }],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1
    }
  };
}

async function interceptarListado(page: Page, urls?: string[]): Promise<void> {
  await page.route('**/recepciones-compra**', async route => {
    const request = route.request();
    const requestUrl = new URL(request.url());
    const esListadoApi = request.method() === 'GET'
      && request.resourceType() !== 'document'
      && requestUrl.pathname === '/recepciones-compra';
    if (!esListadoApi) {
      await route.continue();
      return;
    }
    urls?.push(request.url());
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(respuestaListado())
    });
  });
}

test.describe('Recepción de mercancía - shell y acceso', () => {
  test('redirige a login sin sesión autenticada', async ({ page }) => {
    await page.goto('/recepciones-compra');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega la ruta a un usuario autenticado sin Compras:Ver', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver']);

    await page.goto('/recepciones-compra');
    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
  });

  test('oculta Nueva recepción cuando existe Compras:Ver pero falta Compras:Crear', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver']);
    await interceptarListado(page);

    await page.goto('/recepciones-compra');

    await expect(page.getByRole('heading', { name: 'Recepción de mercancía' })).toBeVisible();
    await expect(page.getByRole('button', { name: /Nueva recepción/i })).toHaveCount(0);
  });

  test('lista recepciones y aplica filtros contra el contrato paginado', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver']);

    const urls: string[] = [];
    await interceptarListado(page, urls);

    await page.goto('/recepciones-compra');
    await expect(page.getByRole('heading', { name: 'Recepción de mercancía' })).toBeVisible();
    await expect(page.getByText('RC-2026-000041')).toBeVisible();
    await expect(page.getByText('OC-2026-000017')).toBeVisible();
    await expect(page.locator('span.status[data-status="1"]')).toHaveText('Borrador');

    await page.locator('input[name="ordenCompraId"]').fill('17');
    await page.locator('mat-select[name="estado"]').click();
    await page.getByRole('option', { name: 'Borrador', exact: true }).click();
    await page.locator('input[name="desde"]').fill('2026-08-01');
    await page.locator('input[name="hasta"]').fill('2026-08-19');
    await page.getByRole('button', { name: 'Filtrar' }).click();

    await expect.poll(() => urls.length).toBeGreaterThan(1);
    const filtrada = new URL(urls.at(-1)!);
    expect(filtrada.searchParams.get('ordenCompraId')).toBe('17');
    expect(filtrada.searchParams.get('estado')).toBe('Borrador');
    expect(filtrada.searchParams.get('page')).toBe('1');
    expect(filtrada.searchParams.get('pageSize')).toBe('20');

    const fechasEsperadas = await page.evaluate(() => {
      const desdeUtc = new Date('2026-08-01T00:00:00').toISOString();
      const siguienteMedianocheLocal = new Date('2026-08-19T00:00:00');
      siguienteMedianocheLocal.setDate(siguienteMedianocheLocal.getDate() + 1);
      const ultimoSegundo = new Date(siguienteMedianocheLocal.getTime() - 1000).toISOString();
      return {
        desdeUtc,
        hastaUtc: ultimoSegundo.replace(/\.\d{3}Z$/, '.9999999Z')
      };
    });
    expect(filtrada.searchParams.get('desdeUtc')).toBe(fechasEsperadas.desdeUtc);
    expect(filtrada.searchParams.get('hastaUtc')).toBe(fechasEsperadas.hastaUtc);
    expect(filtrada.searchParams.get('hastaUtc')).toMatch(/\.9999999Z$/);
  });
});
