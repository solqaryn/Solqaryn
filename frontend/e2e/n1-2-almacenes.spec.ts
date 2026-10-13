import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5006';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

let adminToken = '';
let empresaId = 0;
let sucursalId = 0;
let almacenId = 0;
const suffix = `${Date.now()}`;
const sucursalCodigo = `N12-${suffix.slice(-7)}`;
const almacenCodigo = `ALM-${suffix.slice(-7)}`;
const almacenNombre = `Almacén E2E ${suffix}`;
const almacenNombreActualizado = `Almacén E2E Actualizado ${suffix}`;

function authHeaders(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

async function dataOf(response: APIResponse): Promise<any> {
  const payload = await response.json();
  return payload.data ?? payload.Data;
}

async function loginApi(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, {
    data: { nombreUsuario: ADMIN_USERNAME, password: ADMIN_PASSWORD }
  });
  expect(response.status(), await response.text()).toBe(200);
  const data = await dataOf(response);
  expect(data?.token).toBeTruthy();
  return data.token;
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(url => url.pathname !== '/login', { timeout: 20_000 });
}

async function auditoriaDe(
  request: APIRequestContext,
  accion: string,
  referenciaId: number
): Promise<any> {
  const response = await request.get(
    `${API_URL}/auditoria?entidad=Almacen&referenciaId=${referenciaId}&accion=${encodeURIComponent(accion)}&page=1&pageSize=50`,
    { headers: authHeaders(adminToken) }
  );
  expect(response.status(), await response.text()).toBe(200);
  return await dataOf(response);
}

async function crearEmpresa(request: APIRequestContext): Promise<number> {
  // El tenant inicial existe exclusivamente en MySQL efímero del workflow CI.
  // Crear una segunda empresa antes del aislamiento comercial es una vulnerabilidad,
  // no un requisito de los E2E de sucursales/almacenes.
  const tenantId = 1;
  const response = await request.get(`${API_URL}/tenant-context/${tenantId}`, {
    headers: authHeaders(adminToken)
  });
  expect(response.status(), await response.text()).toBe(200);
  const contexto = await dataOf(response);
  expect(contexto?.empresaId).toBe(tenantId);
  return tenantId;
}

async function crearSucursal(request: APIRequestContext): Promise<number> {
  const response = await request.post(`${API_URL}/sucursales`, {
    headers: authHeaders(adminToken),
    data: {
      empresaId,
      codigo: sucursalCodigo,
      nombre: `Sucursal N1.2 ${suffix}`,
      zonaHoraria: 'America/Tegucigalpa'
    }
  });
  expect(response.status(), await response.text()).toBe(201);
  const sucursal = await dataOf(response);
  expect(sucursal?.empresaId).toBe(empresaId);
  return sucursal.id;
}

test.describe('ERP-N1.2 — Almacenes', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    adminToken = await loginApi(request);
    empresaId = await crearEmpresa(request);
    sucursalId = await crearSucursal(request);
  });

  test('rechaza acceso anónimo y emite correlation ID', async ({ request }) => {
    const anonima = await request.get(`${API_URL}/almacenes`);
    expect(anonima.status()).toBe(401);

    const autenticada = await request.get(`${API_URL}/almacenes?pagina=1&tamanoPagina=10`, {
      headers: authHeaders(adminToken)
    });
    expect(autenticada.status(), await autenticada.text()).toBe(200);
    expect(autenticada.headers()['x-correlation-id']).toBeTruthy();
  });

  test('crea, normaliza, audita y bloquea código duplicado', async ({ request }) => {
    const crear = await request.post(`${API_URL}/almacenes`, {
      headers: authHeaders(adminToken),
      data: {
        sucursalId,
        codigo: almacenCodigo.toLowerCase(),
        nombre: almacenNombre,
        tipo: 'bodega'
      }
    });
    expect(crear.status(), await crear.text()).toBe(201);
    const creado = await dataOf(crear);
    almacenId = creado.id;
    expect(almacenId).toBeGreaterThan(0);
    expect(creado.codigo).toBe(almacenCodigo.toUpperCase());
    expect(creado.sucursalId).toBe(sucursalId);
    expect(creado.tipo).toBe('Bodega');
    expect(creado.activo).toBe(true);

    const audit = await auditoriaDe(request, 'Crear', almacenId);
    expect(audit.totalCount).toBe(1);
    expect(audit.items[0].entidad).toBe('Almacen');
    expect(audit.items[0].referenciaId).toBe(almacenId);
    expect(audit.items[0].correlationId).toBeTruthy();

    const duplicado = await request.post(`${API_URL}/almacenes`, {
      headers: authHeaders(adminToken),
      data: {
        sucursalId,
        codigo: ` ${almacenCodigo.toLowerCase()} `,
        nombre: `${almacenNombre} duplicado`,
        tipo: 'Tienda'
      }
    });
    expect(duplicado.status(), await duplicado.text()).toBe(400);
  });

  test('filtros, tipos, activos y paginación usan contrato server-side', async ({ request }) => {
    const tipos = await request.get(`${API_URL}/almacenes/tipos`, { headers: authHeaders(adminToken) });
    expect(tipos.status(), await tipos.text()).toBe(200);
    expect((await dataOf(tipos)).map((t: any) => t.codigo)).toEqual([
      'Tienda', 'Bodega', 'Transito', 'Devolucion', 'Cuarentena'
    ]);

    const buscar = await request.get(
      `${API_URL}/almacenes?buscar=${encodeURIComponent(suffix)}&sucursalId=${sucursalId}&tipo=Bodega&activo=true&pagina=1&tamanoPagina=10`,
      { headers: authHeaders(adminToken) }
    );
    expect(buscar.status(), await buscar.text()).toBe(200);
    const pagina = await dataOf(buscar);
    expect(pagina.pagina).toBe(1);
    expect(pagina.tamanoPagina).toBe(10);
    expect(pagina.items.some((item: any) => item.id === almacenId)).toBe(true);

    const activos = await request.get(`${API_URL}/almacenes/activos?sucursalId=${sucursalId}`, {
      headers: authHeaders(adminToken)
    });
    expect(activos.status(), await activos.text()).toBe(200);
    expect((await dataOf(activos)).some((item: any) => item.id === almacenId)).toBe(true);
  });

  test('desactivar es idempotente y no duplica auditoría', async ({ request }) => {
    const primera = await request.patch(`${API_URL}/almacenes/${almacenId}/desactivar`, {
      headers: authHeaders(adminToken)
    });
    expect(primera.status(), await primera.text()).toBe(200);
    expect((await dataOf(primera)).activo).toBe(false);

    const auditAntes = await auditoriaDe(request, 'Desactivar', almacenId);
    expect(auditAntes.totalCount).toBe(1);

    const segunda = await request.patch(`${API_URL}/almacenes/${almacenId}/desactivar`, {
      headers: authHeaders(adminToken)
    });
    expect(segunda.status(), await segunda.text()).toBe(200);
    expect((await dataOf(segunda)).activo).toBe(false);

    const auditDespues = await auditoriaDe(request, 'Desactivar', almacenId);
    expect(auditDespues.totalCount).toBe(1);
  });

  test('Sucursal inactiva bloquea reactivación del almacén', async ({ request }) => {
    const desactivarSucursal = await request.patch(`${API_URL}/sucursales/${sucursalId}/desactivar`, {
      headers: authHeaders(adminToken)
    });
    expect(desactivarSucursal.status(), await desactivarSucursal.text()).toBe(200);

    const bloqueada = await request.patch(`${API_URL}/almacenes/${almacenId}/activar`, {
      headers: authHeaders(adminToken)
    });
    expect(bloqueada.status(), await bloqueada.text()).toBe(400);

    const activarSucursal = await request.patch(`${API_URL}/sucursales/${sucursalId}/activar`, {
      headers: authHeaders(adminToken)
    });
    expect(activarSucursal.status(), await activarSucursal.text()).toBe(200);

    const activarAlmacen = await request.patch(`${API_URL}/almacenes/${almacenId}/activar`, {
      headers: authHeaders(adminToken)
    });
    expect(activarAlmacen.status(), await activarAlmacen.text()).toBe(200);
    expect((await dataOf(activarAlmacen)).activo).toBe(true);
  });

  test('editar no cambia estado y actualiza tipo maestro', async ({ request }) => {
    const actualizar = await request.put(`${API_URL}/almacenes/${almacenId}`, {
      headers: authHeaders(adminToken),
      data: {
        sucursalId,
        codigo: almacenCodigo,
        nombre: almacenNombreActualizado,
        tipo: 'Cuarentena'
      }
    });
    expect(actualizar.status(), await actualizar.text()).toBe(200);
    const actualizado = await dataOf(actualizar);
    expect(actualizado.nombre).toBe(almacenNombreActualizado);
    expect(actualizado.tipo).toBe('Cuarentena');
    expect(actualizado.activo).toBe(true);
  });

  test('UI expone mantenimiento protegido y no desborda en móvil', async ({ page }) => {
    await loginUi(page);
    await page.goto('/almacenes');
    await expect(page.getByRole('heading', { name: 'Almacenes', exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: /Nuevo almacén/i })).toBeVisible();

    const busqueda = page.getByLabel('Buscar almacén');
    await busqueda.fill(suffix);
    const fila = page.locator('table.table-desktop tbody tr', { hasText: almacenNombreActualizado });
    await expect(fila).toBeVisible();
    await expect(fila).toContainText(sucursalCodigo);
    await expect(fila).toContainText('Cuarentena');

    await page.goto(`/almacenes/${almacenId}/editar`);
    await expect(page.getByRole('heading', { name: 'Editar almacén', exact: true })).toBeVisible();
    await expect(page.locator('input[formcontrolname="codigo"]')).toHaveValue(almacenCodigo.toUpperCase());
    await expect(page.locator('input[formcontrolname="nombre"]')).toHaveValue(almacenNombreActualizado);

    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/almacenes');
    await page.waitForLoadState('networkidle');
    const layout = await page.evaluate(() => ({
      viewport: document.documentElement.clientWidth,
      documentWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
      mobileCardsVisible: getComputedStyle(document.querySelector('.cards-mobile') as Element).display !== 'none'
    }));
    expect(layout.documentWidth - layout.viewport).toBeLessThanOrEqual(1);
    expect(layout.mobileCardsVisible).toBe(true);
  });

  test('soft-delete oculta el almacén y conserva auditoría', async ({ request }) => {
    const eliminar = await request.delete(`${API_URL}/almacenes/${almacenId}`, {
      headers: authHeaders(adminToken)
    });
    expect(eliminar.status(), await eliminar.text()).toBe(200);

    const buscar = await request.get(`${API_URL}/almacenes?buscar=${encodeURIComponent(suffix)}&pagina=1&tamanoPagina=10`, {
      headers: authHeaders(adminToken)
    });
    expect(buscar.status(), await buscar.text()).toBe(200);
    expect((await dataOf(buscar)).items.some((item: any) => item.id === almacenId)).toBe(false);

    const audit = await auditoriaDe(request, 'EliminarLogico', almacenId);
    expect(audit.totalCount).toBe(1);
    expect(audit.items[0].entidad).toBe('Almacen');
  });
});
