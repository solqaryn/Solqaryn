import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const TENANT_ID = Number.parseInt(process.env['E2E_TENANT_ID'] ?? '1', 10);

let adminToken = '';
let limitedRoleId = 0;
let limitedUserId = 0;
let limitedUsername = 'e2e_limitado';
let limitedPassword = 'E2E.Limitado#2026!';
const sellerUsername = 'e2e_vendedor';
const sellerPassword = 'E2E.Vendedor#2026!';

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

async function expectNoHorizontalOverflow(page: Page, tolerance = 2): Promise<void> {
  await page.waitForTimeout(250);
  const overflow = await page.evaluate(() => {
    const documentWidth = Math.max(
      document.documentElement.scrollWidth,
      document.body?.scrollWidth ?? 0
    );
    return documentWidth - document.documentElement.clientWidth;
  });
  expect(overflow).toBeLessThanOrEqual(tolerance);
}

async function getRoles(request: APIRequestContext): Promise<Array<Record<string, any>>> {
  const response = await request.get(`${API_URL}/roles`, {
    headers: authHeaders(adminToken)
  });
  expect(response.status(), await response.text()).toBe(200);
  return await dataOf(response) as Array<Record<string, any>>;
}

async function ensureRole(
  request: APIRequestContext,
  name: string,
  description: string
): Promise<Record<string, any>> {
  const existing = (await getRoles(request))
    .find((role) => String(role.nombre).toLowerCase() === name.toLowerCase());
  if (existing) return existing;

  const response = await request.post(`${API_URL}/roles`, {
    headers: authHeaders(adminToken),
    data: {
      nombre: name,
      descripcion: description,
      esAdministrador: false
    }
  });
  expect(response.status(), await response.text()).toBe(201);
  return await dataOf(response);
}

async function ensureUser(
  request: APIRequestContext,
  input: {
    nombreUsuario: string;
    nombreCompleto: string;
    password: string;
    rol: string;
    rolId: number;
  }
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
      data: input
    });
    expect(createResponse.status(), await createResponse.text()).toBe(200);
    return await dataOf(createResponse);
  }

  const updateResponse = await request.put(`${API_URL}/usuarios/${existing.id}`, {
    headers: authHeaders(adminToken),
    data: {
      nombreUsuario: input.nombreUsuario,
      nombreCompleto: input.nombreCompleto,
      rol: input.rol,
      rolId: input.rolId,
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

test.describe('Fase 7 — aceptación end-to-end aislada', () => {
  // La suite modifica usuario y contraseña deliberadamente. Reintentarla sobre
  // la misma base produciría un estado diferente; cada workflow ya parte de una
  // base MySQL nueva y descartable.
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    adminToken = await loginApi(request, ADMIN_USERNAME, ADMIN_PASSWORD);

    const sellerRole = (await getRoles(request))
      .find((role) => String(role.nombre).toLowerCase() === 'vendedor');
    expect(sellerRole, 'El rol de sistema Vendedor debe existir después del seeding.').toBeTruthy();

    const limitedRole = await ensureRole(
      request,
      'E2E Limitado',
      'Rol temporal para certificación automatizada de Fase 7'
    );
    limitedRoleId = limitedRole.id;

    const matrixResponse = await request.get(`${API_URL}/permisos/matriz/${limitedRoleId}`, {
      headers: authHeaders(adminToken)
    });
    expect(matrixResponse.status()).toBe(200);
    const matrix = await dataOf(matrixResponse) as Array<Record<string, any>>;
    expect(matrix.length).toBeGreaterThan(0);

    const allowed = new Set(['Dashboard:Ver', 'Productos:Ver']);
    const updatedMatrix = matrix.map((item) => ({
      ...item,
      permitido: allowed.has(`${item.modulo}:${item.accion}`)
    }));

    const updateMatrixResponse = await request.put(`${API_URL}/permisos/matriz/${limitedRoleId}`, {
      headers: authHeaders(adminToken),
      data: { permisos: updatedMatrix }
    });
    expect(updateMatrixResponse.status(), await updateMatrixResponse.text()).toBe(200);

    const limitedUser = await ensureUser(request, {
      nombreUsuario: limitedUsername,
      nombreCompleto: 'Usuario Limitado E2E',
      password: limitedPassword,
      rol: 'Vendedor',
      rolId: limitedRoleId
    });
    limitedUserId = limitedUser.id;
    await ensureTenantMembership(request, Number(limitedUser.id), Number(limitedRoleId));

    const sellerUser = await ensureUser(request, {
      nombreUsuario: sellerUsername,
      nombreCompleto: 'Vendedor E2E',
      password: sellerPassword,
      rol: 'Vendedor',
      rolId: sellerRole!.id
    });
    await ensureTenantMembership(request, Number(sellerUser.id), Number(sellerRole!.id));
  });

  test('Administrador conserva acceso a módulos corporativos', async ({ request }) => {
    const users = await request.get(`${API_URL}/usuarios`, { headers: authHeaders(adminToken) });
    expect(users.status()).toBe(200);

    const audit = await request.get(`${API_URL}/auditoria?pageNumber=1&pageSize=10`, {
      headers: authHeaders(adminToken)
    });
    expect(audit.status()).toBe(200);

    const permissions = await request.get(`${API_URL}/permisos/mis-permisos/empresa/${TENANT_ID}`, {
      headers: authHeaders(adminToken)
    });
    expect(permissions.status()).toBe(200);
    const permissionsData = await dataOf(permissions);
    expect(permissionsData.esAdministrador).toBe(true);
  });

  test('Rol personalizado recibe únicamente sus permisos y el backend responde 403', async ({ request }) => {
    const token = await loginApi(request, limitedUsername, limitedPassword);

    const permissions = await request.get(`${API_URL}/permisos/mis-permisos/empresa/${TENANT_ID}`, {
      headers: authHeaders(token)
    });
    expect(permissions.status()).toBe(200);
    const permissionData = await dataOf(permissions);
    expect(permissionData.esAdministrador).toBe(false);
    expect(permissionData.permisos).toContain('Dashboard:Ver');
    expect(permissionData.permisos).toContain('Productos:Ver');
    expect(permissionData.permisos).not.toContain('Usuarios:Ver');
    expect(permissionData.permisos).not.toContain('Auditoria:Ver');

    const products = await request.get(`${API_URL}/productos?pageNumber=1&pageSize=10`, {
      headers: authHeaders(token)
    });
    expect(products.status()).toBe(200);

    for (const path of ['/usuarios', '/auditoria?pageNumber=1&pageSize=10', '/compras?pageNumber=1&pageSize=10']) {
      const denied = await request.get(`${API_URL}${path}`, { headers: authHeaders(token) });
      expect(denied.status(), `Se esperaba 403 para ${path}`).toBe(403);
    }
  });

  test('Rol Vendedor no hereda acceso administrativo', async ({ request }) => {
    const token = await loginApi(request, sellerUsername, sellerPassword);
    const permissions = await request.get(`${API_URL}/permisos/mis-permisos/empresa/${TENANT_ID}`, {
      headers: authHeaders(token)
    });
    expect(permissions.status()).toBe(200);
    const permissionData = await dataOf(permissions);
    expect(permissionData.esAdministrador).toBe(false);

    for (const path of ['/usuarios', '/auditoria?pageNumber=1&pageSize=10']) {
      const denied = await request.get(`${API_URL}${path}`, { headers: authHeaders(token) });
      expect(denied.status(), `El Vendedor no debe acceder a ${path}`).toBe(403);
    }
  });

  test('Perfil se limita al usuario autenticado y aplica la política de contraseña', async ({ request }) => {
    const token = await loginApi(request, limitedUsername, limitedPassword);

    const profileResponse = await request.get(`${API_URL}/perfil`, { headers: authHeaders(token) });
    expect(profileResponse.status()).toBe(200);
    const profile = await dataOf(profileResponse);
    expect(profile.id).toBe(limitedUserId);

    limitedUsername = 'e2e_limitado_actualizado';
    const updateResponse = await request.put(`${API_URL}/perfil`, {
      headers: authHeaders(token),
      data: {
        nombreUsuario: limitedUsername,
        nombreCompleto: 'Usuario Limitado Actualizado'
      }
    });
    expect(updateResponse.status(), await updateResponse.text()).toBe(200);
    const updatedProfile = await dataOf(updateResponse);
    expect(updatedProfile.id).toBe(limitedUserId);
    expect(updatedProfile.nombreUsuario).toBe(limitedUsername);

    const weakPassword = await request.put(`${API_URL}/perfil/password`, {
      headers: authHeaders(token),
      data: {
        passwordActual: limitedPassword,
        passwordNueva: 'debil'
      }
    });
    expect(weakPassword.status()).toBe(400);

    const newPassword = 'E2E.Nueva#2026!';
    const passwordResponse = await request.put(`${API_URL}/perfil/password`, {
      headers: authHeaders(token),
      data: {
        passwordActual: limitedPassword,
        passwordNueva: newPassword
      }
    });
    expect(passwordResponse.status(), await passwordResponse.text()).toBe(200);

    const oldLogin = await request.post(`${API_URL}/auth/login`, {
      data: { nombreUsuario: limitedUsername, password: limitedPassword }
    });
    expect(oldLogin.status()).toBe(401);

    limitedPassword = newPassword;
    await loginApi(request, limitedUsername, limitedPassword);
  });

  test('Venta confirmada genera PDF privado y público con revocación segura', async ({ request }) => {
    const productResponse = await request.post(`${API_URL}/productos`, {
      headers: authHeaders(adminToken),
      multipart: {
        Nombre: 'Producto Factura E2E',
        Marca: 'Solqaryn',
        Modelo: 'F7',
        Descripcion: 'Producto temporal para validar el PDF oficial',
        Cantidad: '20',
        Costo: '100',
        Precio: '230',
        UmbralStockBajo: '3'
      }
    });
    expect(productResponse.status(), await productResponse.text()).toBe(201);
    const product = await dataOf(productResponse);
    const variantsResponse = await request.get(`${API_URL}/productos/${product.id}/variantes`, {
      headers: authHeaders(adminToken)
    });
    expect(variantsResponse.status(), await variantsResponse.text()).toBe(200);
    const variants = await dataOf(variantsResponse);
    const technicalVariant = variants.find((variant: any) => variant.esTecnica === true);
    expect(technicalVariant, 'El producto de factura debe tener una variante técnica.').toBeTruthy();

    const saleResponse = await request.post(`${API_URL}/ventas`, {
      headers: authHeaders(adminToken),
      data: {
        clienteNombre: 'Cliente Factura E2E',
        clienteTelefono: '33425030',
        clienteCorreo: 'qa@example.com',
        metodoPago: 'Efectivo',
        estadoPago: 'Pagado',
        notas: 'Venta temporal para certificación de Fase 7',
        detalles: [
          {
            productoId: product.id,
            productoVarianteId: technicalVariant.id,
            cantidad: 2,
            precioUnitario: 230
          }
        ]
      }
    });
    expect(saleResponse.status(), await saleResponse.text()).toBe(201);
    const sale = await dataOf(saleResponse);

    const confirmResponse = await request.post(`${API_URL}/ventas/${sale.id}/confirmar`, {
      headers: authHeaders(adminToken)
    });
    expect(confirmResponse.status(), await confirmResponse.text()).toBe(200);

    const invoiceResponse = await request.get(`${API_URL}/facturas/venta/${sale.id}`, {
      headers: authHeaders(adminToken)
    });
    expect(invoiceResponse.status(), await invoiceResponse.text()).toBe(200);
    const invoice = await dataOf(invoiceResponse);
    expect(invoice.id).toBeGreaterThan(0);
    expect(invoice.total).toBeGreaterThan(0);

    const privatePdfResponse = await request.get(`${API_URL}/facturas/${invoice.id}/pdf`, {
      headers: authHeaders(adminToken)
    });
    expect(privatePdfResponse.status()).toBe(200);
    expect(privatePdfResponse.headers()['content-type']).toContain('application/pdf');
    const privatePdf = await privatePdfResponse.body();
    expect(privatePdf.subarray(0, 4).toString()).toBe('%PDF');
    expect(privatePdf.length).toBeGreaterThan(5_000);

    const shareResponse = await request.post(`${API_URL}/facturas/${invoice.id}/compartir/whatsapp`, {
      headers: authHeaders(adminToken)
    });
    expect(shareResponse.status(), await shareResponse.text()).toBe(200);
    const share = await dataOf(shareResponse);
    const publicUrl = new URL(share.urlPdfPublica);
    const segments = publicUrl.pathname.split('/').filter(Boolean);
    const publicIndex = segments.indexOf('publico');
    expect(publicIndex).toBeGreaterThanOrEqual(0);
    const token = segments[publicIndex + 1];
    expect(token.length).toBeGreaterThanOrEqual(32);

    const publicPdfResponse = await request.get(`${API_URL}/facturas/publico/${encodeURIComponent(token)}/pdf`);
    expect(publicPdfResponse.status()).toBe(200);
    expect(publicPdfResponse.headers()['content-type']).toContain('application/pdf');
    expect(publicPdfResponse.headers()['cache-control']).toContain('no-store');
    expect(publicPdfResponse.headers()['referrer-policy']).toBe('no-referrer');
    expect(publicPdfResponse.headers()['x-frame-options']).toBe('DENY');
    const publicPdf = await publicPdfResponse.body();
    expect(publicPdf.subarray(0, 4).toString()).toBe('%PDF');
    expect(publicPdf.length).toBeGreaterThan(5_000);

    const sizeTolerance = Math.max(1_024, Math.ceil(privatePdf.length * 0.02));
    expect(Math.abs(privatePdf.length - publicPdf.length)).toBeLessThanOrEqual(sizeTolerance);

    const invalidEmail = await request.post(`${API_URL}/facturas/${invoice.id}/compartir/correo`, {
      headers: authHeaders(adminToken),
      data: { destinatario: 'correo-invalido' }
    });
    expect(invalidEmail.status()).toBe(400);

    const revokeResponse = await request.post(`${API_URL}/facturas/${invoice.id}/compartir/revocar`, {
      headers: authHeaders(adminToken)
    });
    expect(revokeResponse.status()).toBe(200);

    const revokedPdf = await request.get(`${API_URL}/facturas/publico/${encodeURIComponent(token)}/pdf`);
    expect(revokedPdf.status()).toBe(404);
  });

  test('Login no presenta desbordamiento en teléfono, tablet ni escritorio', async ({ page }) => {
    const viewports = [
      { width: 320, height: 720 },
      { width: 375, height: 812 },
      { width: 390, height: 844 },
      { width: 430, height: 932 },
      { width: 768, height: 1024 },
      { width: 1440, height: 900 }
    ];

    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      await page.goto('/login');
      await expect(page.locator('form.login-card')).toBeVisible();
      await expect(page.locator('input[formcontrolname="nombreUsuario"]')).toBeVisible();
      await expectNoHorizontalOverflow(page);
    }
  });

  test('Administrador accede a Usuarios desde la interfaz', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await loginUi(page, ADMIN_USERNAME, ADMIN_PASSWORD);
    await page.goto('/usuarios');
    await expect(page).toHaveURL(/\/usuarios$/);
    await expect(page.locator('h1').filter({ hasText: /Usuarios/i })).toBeVisible();
    await expectNoHorizontalOverflow(page);
  });

  test('Rol personalizado es redirigido de módulos prohibidos y mantiene Productos', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await loginUi(page, limitedUsername, limitedPassword);

    await page.goto('/usuarios');
    await page.waitForTimeout(500);
    expect(new URL(page.url()).pathname).not.toBe('/usuarios');

    await page.goto('/auditoria');
    await page.waitForTimeout(500);
    expect(new URL(page.url()).pathname).not.toBe('/auditoria');

    await page.goto('/productos');
    await expect(page).toHaveURL(/\/productos$/);
    await expectNoHorizontalOverflow(page, 3);
  });
});
