import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const TENANT_ID = Number.parseInt(process.env['E2E_TENANT_ID'] ?? '1', 10);

const ROLE_NAME = 'E2E Vendedor Aislado';
const USER_A = {
  nombreUsuario: 'e2e_aislado_a',
  nombreCompleto: 'Vendedor Aislado A',
  password: 'E2E.AisladoA#2026!'
};
const USER_B = {
  nombreUsuario: 'e2e_aislado_b',
  nombreCompleto: 'Vendedor Aislado B',
  password: 'E2E.AisladoB#2026!'
};

let adminToken = '';
let tokenA = '';
let tokenB = '';
let ventaAId = 0;
let ventaBId = 0;

function authHeaders(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

async function dataOf(response: APIResponse): Promise<any> {
  const payload = await response.json();
  return payload.data ?? payload.Data;
}

async function loginApi(request: APIRequestContext, username: string, password: string): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, {
    data: { nombreUsuario: username, password }
  });
  expect(response.status(), `No fue posible iniciar sesión como ${username}: ${await response.text()}`).toBe(200);
  const data = await dataOf(response);
  expect(data?.token).toBeTruthy();
  return data.token;
}

async function loginUi(page: Page, username: string, password: string): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function getRoles(request: APIRequestContext): Promise<Array<Record<string, any>>> {
  const response = await request.get(`${API_URL}/roles`, { headers: authHeaders(adminToken) });
  expect(response.status(), await response.text()).toBe(200);
  return await dataOf(response) as Array<Record<string, any>>;
}

async function ensureRole(request: APIRequestContext): Promise<Record<string, any>> {
  const existing = (await getRoles(request))
    .find((role) => String(role.nombre).toLowerCase() === ROLE_NAME.toLowerCase());
  if (existing) return existing;

  const response = await request.post(`${API_URL}/roles`, {
    headers: authHeaders(adminToken),
    data: {
      nombre: ROLE_NAME,
      descripcion: 'Rol temporal para certificar permisos exactos y aislamiento por UsuarioId.',
      esAdministrador: false
    }
  });
  expect(response.status(), await response.text()).toBe(201);
  return await dataOf(response);
}

async function configureRole(request: APIRequestContext, roleId: number): Promise<void> {
  const matrixResponse = await request.get(`${API_URL}/permisos/matriz/${roleId}`, {
    headers: authHeaders(adminToken)
  });
  expect(matrixResponse.status(), await matrixResponse.text()).toBe(200);
  const matrix = await dataOf(matrixResponse) as Array<Record<string, any>>;

  const allowed = new Set([
    'Dashboard:Ver',
    'Productos:Ver',
    'Categorias:Ver',
    'Clientes:Ver',
    'Ventas:Ver',
    'Ventas:Crear',
    'Ventas:Editar',
    'Ventas:Confirmar',
    'Facturacion:Ver',
    'Facturacion:Exportar',
    'Facturacion:Imprimir',
    'Facturacion:Compartir',
    'Finanzas:Ver',
    'Inventario:Ver',
    'MovimientosInventario:Ver'
  ]);

  const updated = matrix.map((item) => ({
    ...item,
    permitido: allowed.has(`${item.modulo}:${item.accion}`)
  }));

  const updateResponse = await request.put(`${API_URL}/permisos/matriz/${roleId}`, {
    headers: authHeaders(adminToken),
    data: { permisos: updated }
  });
  expect(updateResponse.status(), await updateResponse.text()).toBe(200);
}

async function ensureUser(
  request: APIRequestContext,
  roleId: number,
  input: typeof USER_A
): Promise<Record<string, any>> {
  const listResponse = await request.get(`${API_URL}/usuarios`, {
    headers: authHeaders(adminToken)
  });
  expect(listResponse.status(), await listResponse.text()).toBe(200);
  const users = await dataOf(listResponse) as Array<Record<string, any>>;
  const existing = users.find((user) =>
    String(user.nombreUsuario).toLowerCase() === input.nombreUsuario.toLowerCase());

  if (!existing) {
    const createResponse = await request.post(`${API_URL}/usuarios`, {
      headers: authHeaders(adminToken),
      data: {
        ...input,
        rol: 'Vendedor',
        rolId: roleId
      }
    });
    expect(createResponse.status(), await createResponse.text()).toBe(200);
    return await dataOf(createResponse);
  }

  const updateResponse = await request.put(`${API_URL}/usuarios/${existing.id}`, {
    headers: authHeaders(adminToken),
    data: {
      nombreUsuario: input.nombreUsuario,
      nombreCompleto: input.nombreCompleto,
      rol: 'Vendedor',
      rolId: roleId,
      nuevaPassword: input.password
    }
  });
  expect(updateResponse.status(), await updateResponse.text()).toBe(200);
  return await dataOf(updateResponse);
}

async function ensureTenantMembership(
  request: APIRequestContext,
  userId: number,
  roleId: number
): Promise<void> {
  const membershipsResponse = await request.get(`${API_URL}/usuarios/${userId}/empresas`, {
    headers: authHeaders(adminToken)
  });
  expect(membershipsResponse.status(), await membershipsResponse.text()).toBe(200);
  const memberships = await dataOf(membershipsResponse) as Array<Record<string, any>>;
  const membership = memberships.find((item) => Number(item.empresaId) === TENANT_ID);

  if (!membership) {
    const assignResponse = await request.post(`${API_URL}/usuarios/${userId}/empresas`, {
      headers: authHeaders(adminToken),
      data: { empresaId: TENANT_ID, rolId: roleId }
    });
    expect(assignResponse.status(), await assignResponse.text()).toBe(200);
    return;
  }

  if (Number(membership.rolId) !== roleId) {
    const roleResponse = await request.put(
      `${API_URL}/usuarios/${userId}/empresas/${TENANT_ID}/rol`,
      { headers: authHeaders(adminToken), data: { rolId: roleId } }
    );
    expect(roleResponse.status(), await roleResponse.text()).toBe(200);
  }

  if (membership.activa === false) {
    const stateResponse = await request.put(
      `${API_URL}/usuarios/${userId}/empresas/${TENANT_ID}/estado`,
      { headers: authHeaders(adminToken), data: { activa: true } }
    );
    expect(stateResponse.status(), await stateResponse.text()).toBe(200);
  }
}

async function createConfirmedSale(
  request: APIRequestContext,
  token: string,
  productId: number,
  customer: string,
  quantity: number
): Promise<Record<string, any>> {
  const variantsResponse = await request.get(`${API_URL}/productos/${productId}/variantes`, {
    headers: authHeaders(token)
  });
  expect(variantsResponse.status(), await variantsResponse.text()).toBe(200);
  const variants = await dataOf(variantsResponse);
  const technicalVariant = variants.find((variant: any) => variant.esTecnica === true);
  expect(technicalVariant, `El producto ${productId} debe tener una variante técnica.`).toBeTruthy();

  const createResponse = await request.post(`${API_URL}/ventas`, {
    headers: authHeaders(token),
    data: {
      clienteNombre: customer,
      metodoPago: 'Efectivo',
      estadoPago: 'Pagado',
      detalles: [{
        productoId: productId,
        productoVarianteId: technicalVariant.id,
        cantidad: quantity,
        precioUnitario: 250
      }]
    }
  });
  expect(createResponse.status(), await createResponse.text()).toBe(201);
  const sale = await dataOf(createResponse);

  const confirmResponse = await request.post(`${API_URL}/ventas/${sale.id}/confirmar`, {
    headers: authHeaders(token)
  });
  expect(confirmResponse.status(), await confirmResponse.text()).toBe(200);
  return await dataOf(confirmResponse);
}

async function saleIdsFor(request: APIRequestContext, token: string): Promise<number[]> {
  const response = await request.get(`${API_URL}/ventas?page=1&pageSize=100`, {
    headers: authHeaders(token)
  });
  expect(response.status(), await response.text()).toBe(200);
  const data = await dataOf(response);
  return (data.items ?? []).map((sale: Record<string, any>) => Number(sale.id));
}

async function financialMovementsFor(request: APIRequestContext, token: string): Promise<Array<Record<string, any>>> {
  const response = await request.get(`${API_URL}/finanzas/movimientos`, {
    headers: authHeaders(token)
  });
  expect(response.status(), await response.text()).toBe(200);
  return await dataOf(response) as Array<Record<string, any>>;
}

async function movimientosInventarioFor(request: APIRequestContext, token: string): Promise<Array<Record<string, any>>> {
  const response = await request.get(`${API_URL}/inventario/movimientos`, {
    headers: authHeaders(token)
  });
  expect(response.status(), await response.text()).toBe(200);
  return await dataOf(response) as Array<Record<string, any>>;
}

test.describe('Fase 7 — permisos exactos y aislamiento por UsuarioId', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    adminToken = await loginApi(request, ADMIN_USERNAME, ADMIN_PASSWORD);
    const role = await ensureRole(request);
    await configureRole(request, Number(role.id));
    const userA = await ensureUser(request, Number(role.id), USER_A);
    const userB = await ensureUser(request, Number(role.id), USER_B);
    await ensureTenantMembership(request, Number(userA.id), Number(role.id));
    await ensureTenantMembership(request, Number(userB.id), Number(role.id));

    const productResponse = await request.post(`${API_URL}/productos`, {
      headers: authHeaders(adminToken),
      multipart: {
        Nombre: 'Producto Aislamiento E2E',
        Marca: 'Solqaryn',
        Modelo: 'ISO-USER',
        Descripcion: 'Producto temporal para comprobar aislamiento por UsuarioId.',
        Cantidad: '50',
        Costo: '50',
        Precio: '250',
        UmbralStockBajo: '2'
      }
    });
    expect(productResponse.status(), await productResponse.text()).toBe(201);
    const product = await dataOf(productResponse);

    tokenA = await loginApi(request, USER_A.nombreUsuario, USER_A.password);
    tokenB = await loginApi(request, USER_B.nombreUsuario, USER_B.password);

    const saleA = await createConfirmedSale(request, tokenA, Number(product.id), 'Cliente exclusivo A', 1);
    const saleB = await createConfirmedSale(request, tokenB, Number(product.id), 'Cliente exclusivo B', 2);
    ventaAId = Number(saleA.id);
    ventaBId = Number(saleB.id);
  });

  test('La matriz exacta no concede Compras ni Proveedores', async ({ request }) => {
    const permissionsResponse = await request.get(`${API_URL}/permisos/mis-permisos/empresa/${TENANT_ID}`, {
      headers: authHeaders(tokenA)
    });
    expect(permissionsResponse.status(), await permissionsResponse.text()).toBe(200);
    const permissions = await dataOf(permissionsResponse);

    expect(permissions.permisos).toContain('Ventas:Ver');
    expect(permissions.permisos).toContain('Finanzas:Ver');
    expect(permissions.permisos).toContain('MovimientosInventario:Ver');
    expect(permissions.permisos).not.toContain('Compras:Ver');
    expect(permissions.permisos).not.toContain('Proveedores:Ver');

    for (const path of ['/compras?page=1&pageSize=10', '/proveedores']) {
      const response = await request.get(`${API_URL}${path}`, { headers: authHeaders(tokenA) });
      expect(response.status(), `Se esperaba 403 para ${path}`).toBe(403);
    }
  });

  test('Cada vendedor solo recibe sus propias ventas por UsuarioId', async ({ request }) => {
    const idsA = await saleIdsFor(request, tokenA);
    const idsB = await saleIdsFor(request, tokenB);

    expect(idsA).toContain(ventaAId);
    expect(idsA).not.toContain(ventaBId);
    expect(idsB).toContain(ventaBId);
    expect(idsB).not.toContain(ventaAId);

    const forbiddenDetailA = await request.get(`${API_URL}/ventas/${ventaBId}`, {
      headers: authHeaders(tokenA)
    });
    expect(forbiddenDetailA.status()).toBe(404);

    const forbiddenDetailB = await request.get(`${API_URL}/ventas/${ventaAId}`, {
      headers: authHeaders(tokenB)
    });
    expect(forbiddenDetailB.status()).toBe(404);
  });

  test('Finanzas y movimientos no mezclan usuarios', async ({ request }) => {
    const summaryAResponse = await request.get(`${API_URL}/finanzas/resumen`, {
      headers: authHeaders(tokenA)
    });
    const summaryBResponse = await request.get(`${API_URL}/finanzas/resumen`, {
      headers: authHeaders(tokenB)
    });
    expect(summaryAResponse.status(), await summaryAResponse.text()).toBe(200);
    expect(summaryBResponse.status(), await summaryBResponse.text()).toBe(200);

    const summaryA = await dataOf(summaryAResponse);
    const summaryB = await dataOf(summaryBResponse);
    expect(Number(summaryA.ingresosTotales)).toBe(250);
    expect(Number(summaryB.ingresosTotales)).toBe(500);

    const financialA = await financialMovementsFor(request, tokenA);
    const financialB = await financialMovementsFor(request, tokenB);
    expect(financialA.length).toBe(1);
    expect(financialB.length).toBe(1);
    expect(financialA[0].creadoPorNombreUsuario).toBe(USER_A.nombreUsuario);
    expect(financialB[0].creadoPorNombreUsuario).toBe(USER_B.nombreUsuario);
    expect(financialA[0].concepto).toContain('Cliente exclusivo A');
    expect(financialB[0].concepto).toContain('Cliente exclusivo B');

    const movimientosA = await movimientosInventarioFor(request, tokenA);
    const movimientosB = await movimientosInventarioFor(request, tokenB);
    expect(movimientosA.length).toBe(1);
    expect(movimientosB.length).toBe(1);
    expect(movimientosA[0].creadoPorNombreUsuario).toBe(USER_A.nombreUsuario);
    expect(movimientosB[0].creadoPorNombreUsuario).toBe(USER_B.nombreUsuario);
    expect(Number(movimientosA[0].referenciaId)).toBe(ventaAId);
    expect(Number(movimientosB[0].referenciaId)).toBe(ventaBId);
  });

  test('El menú y las guardas ocultan módulos no concedidos', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await loginUi(page, USER_A.nombreUsuario, USER_A.password);

    const sidebar = page.locator('aside.sidebar');
    await expect(sidebar.getByRole('link', { name: 'Ventas', exact: true })).toBeVisible();
    await expect(sidebar.getByRole('link', { name: 'Finanzas', exact: true })).toBeVisible();
    await expect(sidebar.getByRole('link', { name: 'Movimientos', exact: true })).toBeVisible();
    await expect(sidebar.getByRole('link', { name: 'Compras', exact: true })).toHaveCount(0);
    await expect(sidebar.getByRole('link', { name: 'Proveedores', exact: true })).toHaveCount(0);

    await page.goto('/compras');
    await page.waitForTimeout(500);
    expect(new URL(page.url()).pathname).not.toBe('/compras');

    await page.goto('/proveedores');
    await page.waitForTimeout(500);
    expect(new URL(page.url()).pathname).not.toBe('/proveedores');
  });
});
