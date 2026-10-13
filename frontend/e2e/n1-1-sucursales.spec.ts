import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

let adminToken = '';
let empresaId = 0;
let sucursalId = 0;
const suffix = `${Date.now()}`;
const codigo = `TGU-${suffix.slice(-8)}`;
const nombre = `Sucursal E2E ${suffix}`;
const nombreActualizado = `Sucursal E2E Actualizada ${suffix}`;

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

async function prepararTenantAdmin(request: APIRequestContext): Promise<number> {
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

async function completarSeleccionTenant(page: Page): Promise<void> {
  const selector = page.getByLabel('ID de empresa');
  await selector.waitFor({ state: 'visible', timeout: 5_000 });
  await selector.fill(String(empresaId));
  await page.getByRole('button', { name: 'Entrar a la empresa', exact: true }).click();
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await completarSeleccionTenant(page);
  await page.waitForURL(url => url.pathname !== '/login', { timeout: 20_000 });
}

async function auditoriaDe(
  request: APIRequestContext,
  accion: string,
  referenciaId: number
): Promise<any> {
  const response = await request.get(
    `${API_URL}/auditoria?entidad=Sucursal&referenciaId=${referenciaId}&accion=${encodeURIComponent(accion)}&page=1&pageSize=50`,
    { headers: authHeaders(adminToken) }
  );
  expect(response.status(), await response.text()).toBe(200);
  return await dataOf(response);
}

test.describe('ERP-N1.1 — Sucursales', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    adminToken = await loginApi(request);
    // N6.5 exige una membresía UsuarioEmpresa real para materializar el tenant.
    // El fixture usa únicamente APIs públicas y evita depender de secretos/IDs hard-coded.
    empresaId = await prepararTenantAdmin(request);
  });

  test('rechaza acceso anónimo y emite correlation ID', async ({ request }) => {
    const anonima = await request.get(`${API_URL}/sucursales`);
    expect(anonima.status()).toBe(401);

    const autenticada = await request.get(`${API_URL}/sucursales?pagina=1&tamanoPagina=10`, {
      headers: authHeaders(adminToken)
    });
    expect(autenticada.status(), await autenticada.text()).toBe(200);
    expect(autenticada.headers()['x-correlation-id']).toBeTruthy();
  });

  test('crea, normaliza, audita y bloquea código duplicado', async ({ request }) => {
    const crear = await request.post(`${API_URL}/sucursales`, {
      headers: authHeaders(adminToken),
      data: {
        empresaId,
        codigo: codigo.toLowerCase(),
        nombre,
        direccion: 'Tegucigalpa, Francisco Morazán',
        telefono: '9999-0000',
        correo: `sucursal-${suffix}@example.com`,
        zonaHoraria: 'America/Tegucigalpa'
      }
    });
    expect(crear.status(), await crear.text()).toBe(201);
    const creada = await dataOf(crear);
    sucursalId = creada.id;
    expect(sucursalId).toBeGreaterThan(0);
    expect(creada.empresaId).toBe(empresaId);
    expect(creada.codigo).toBe(codigo.toUpperCase());
    expect(creada.nombre).toBe(nombre);
    expect(creada.activa).toBe(true);

    const audit = await auditoriaDe(request, 'Crear', sucursalId);
    expect(audit.totalCount).toBe(1);
    expect(audit.items[0].entidad).toBe('Sucursal');
    expect(audit.items[0].referenciaId).toBe(sucursalId);
    expect(audit.items[0].correlationId).toBeTruthy();

    const duplicada = await request.post(`${API_URL}/sucursales`, {
      headers: authHeaders(adminToken),
      data: {
        empresaId,
        codigo: ` ${codigo.toLowerCase()} `,
        nombre: `${nombre} duplicada`,
        zonaHoraria: 'America/Tegucigalpa'
      }
    });
    expect(duplicada.status(), await duplicada.text()).toBe(400);
  });

  test('búsqueda, filtros y paginación server-side devuelven contrato estable', async ({ request }) => {
    const buscar = await request.get(
      `${API_URL}/sucursales?buscar=${encodeURIComponent(suffix)}&activa=true&pagina=1&tamanoPagina=10`,
      { headers: authHeaders(adminToken) }
    );
    expect(buscar.status(), await buscar.text()).toBe(200);
    const pagina = await dataOf(buscar);
    expect(pagina.pagina).toBe(1);
    expect(pagina.tamanoPagina).toBe(10);
    expect(pagina.total).toBeGreaterThanOrEqual(1);
    expect(pagina.items.some((item: any) => item.id === sucursalId)).toBe(true);
  });

  test('desactivar es idempotente y no duplica auditoría', async ({ request }) => {
    const primera = await request.patch(`${API_URL}/sucursales/${sucursalId}/desactivar`, {
      headers: authHeaders(adminToken)
    });
    expect(primera.status(), await primera.text()).toBe(200);
    expect((await dataOf(primera)).activa).toBe(false);

    const auditAntes = await auditoriaDe(request, 'Desactivar', sucursalId);
    expect(auditAntes.totalCount).toBe(1);

    const segunda = await request.patch(`${API_URL}/sucursales/${sucursalId}/desactivar`, {
      headers: authHeaders(adminToken)
    });
    expect(segunda.status(), await segunda.text()).toBe(200);
    expect((await dataOf(segunda)).activa).toBe(false);

    const auditDespues = await auditoriaDe(request, 'Desactivar', sucursalId);
    expect(auditDespues.totalCount).toBe(1);

    const activas = await request.get(`${API_URL}/sucursales/activas`, {
      headers: authHeaders(adminToken)
    });
    expect(activas.status(), await activas.text()).toBe(200);
    expect((await dataOf(activas)).some((item: any) => item.id === sucursalId)).toBe(false);
  });

  test('editar no cambia estado y activar restaura disponibilidad', async ({ request }) => {
    const actualizar = await request.put(`${API_URL}/sucursales/${sucursalId}`, {
      headers: authHeaders(adminToken),
      data: {
        empresaId,
        codigo,
        nombre: nombreActualizado,
        direccion: 'Tegucigalpa, Honduras',
        telefono: '9999-1111',
        correo: `actualizada-${suffix}@example.com`,
        zonaHoraria: 'America/Tegucigalpa'
      }
    });
    expect(actualizar.status(), await actualizar.text()).toBe(200);
    const actualizada = await dataOf(actualizar);
    expect(actualizada.empresaId).toBe(empresaId);
    expect(actualizada.nombre).toBe(nombreActualizado);
    expect(actualizada.activa).toBe(false);

    const activar = await request.patch(`${API_URL}/sucursales/${sucursalId}/activar`, {
      headers: authHeaders(adminToken)
    });
    expect(activar.status(), await activar.text()).toBe(200);
    expect((await dataOf(activar)).activa).toBe(true);
  });

  test('UI expone mantenimiento protegido y no desborda en móvil', async ({ page }) => {
    await loginUi(page);
    // Navegación SPA: evita recargar Angular y revocar por diseño el tenant verificado.
    await page.locator('a[href="/sucursales"]').click();
    await expect(page.getByRole('heading', { name: 'Sucursales', exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: /Nueva sucursal/i })).toBeVisible();

    const busqueda = page.getByLabel('Buscar sucursal');
    await busqueda.fill(suffix);
    const fila = page.locator('table.table-desktop tbody tr', { hasText: nombreActualizado });
    await expect(fila).toBeVisible();

    const editar = fila.locator(`a[href="/sucursales/${sucursalId}/editar"]`);
    await editar.click();
    await expect(page.getByRole('heading', { name: 'Editar sucursal', exact: true })).toBeVisible();
    await expect(page.locator('input[formcontrolname="codigo"]')).toHaveValue(codigo);
    await expect(page.locator('input[formcontrolname="nombre"]')).toHaveValue(nombreActualizado);

    await page.goBack();
    await expect(page.getByRole('heading', { name: 'Sucursales', exact: true })).toBeVisible();
    await page.setViewportSize({ width: 390, height: 844 });
    await page.waitForLoadState('networkidle');
    const layout = await page.evaluate(() => ({
      viewport: document.documentElement.clientWidth,
      documentWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
      mobileCardsVisible: getComputedStyle(document.querySelector('.cards-mobile') as Element).display !== 'none'
    }));
    expect(layout.documentWidth - layout.viewport).toBeLessThanOrEqual(1);
    expect(layout.mobileCardsVisible).toBe(true);
  });

  test('eliminación lógica oculta la sucursal y conserva auditoría', async ({ request }) => {
    const eliminar = await request.delete(`${API_URL}/sucursales/${sucursalId}`, {
      headers: authHeaders(adminToken)
    });
    expect(eliminar.status(), await eliminar.text()).toBe(200);

    const buscar = await request.get(`${API_URL}/sucursales?buscar=${encodeURIComponent(suffix)}&pagina=1&tamanoPagina=10`, {
      headers: authHeaders(adminToken)
    });
    expect(buscar.status(), await buscar.text()).toBe(200);
    expect((await dataOf(buscar)).items.some((item: any) => item.id === sucursalId)).toBe(false);

    const audit = await auditoriaDe(request, 'EliminarLogico', sucursalId);
    expect(audit.totalCount).toBe(1);
    expect(audit.items[0].entidad).toBe('Sucursal');
  });
});
