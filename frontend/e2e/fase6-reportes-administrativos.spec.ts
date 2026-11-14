import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const TENANT_ID = Number.parseInt(process.env['E2E_TENANT_ID'] ?? '1', 10);
const suffix = `${Date.now()}`;
const LIMITED_USERNAME = `fase6_limitado_${suffix}`;
const LIMITED_PASSWORD = 'Fase6.Limitado#2026!';

let adminToken = '';
let limitedToken = '';
let adminRoleId = 0;
let limitedRoleId = 0;

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

async function dataOf(response: APIResponse): Promise<any> {
  const payload = await response.json();
  return payload.data ?? payload.Data;
}

async function login(request: APIRequestContext, username: string, password: string): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, {
    data: { nombreUsuario: username, password }
  });
  expect(response.status(), await response.text()).toBe(200);
  const data = await dataOf(response);
  expect(data.token).toBeTruthy();
  return data.token;
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(url => url.pathname !== '/login', { timeout: 20_000 });
}

async function noHorizontalOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(() =>
    Math.max(document.documentElement.scrollWidth, document.body?.scrollWidth ?? 0) -
    document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(2);
}

test.describe('Fase 6 — permisos, auditoría y reportes administrativos', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    adminToken = await login(request, ADMIN_USERNAME, ADMIN_PASSWORD);

    const rolesResponse = await request.get(`${API_URL}/roles`, { headers: auth(adminToken) });
    expect(rolesResponse.status(), await rolesResponse.text()).toBe(200);
    const roles = await dataOf(rolesResponse) as Array<Record<string, any>>;
    const adminRole = roles.find(role => role.esAdministrador === true);
    expect(adminRole).toBeTruthy();
    adminRoleId = adminRole!.id;

    const roleResponse = await request.post(`${API_URL}/roles`, {
      headers: auth(adminToken),
      data: {
        nombre: `Fase 6 Limitado ${suffix}`,
        descripcion: 'Rol sin privilegios administrativos para certificación aislada',
        esAdministrador: false
      }
    });
    expect(roleResponse.status(), await roleResponse.text()).toBe(201);
    limitedRoleId = (await dataOf(roleResponse)).id;

    const matrixResponse = await request.get(`${API_URL}/permisos/matriz/${limitedRoleId}`, {
      headers: auth(adminToken)
    });
    expect(matrixResponse.status(), await matrixResponse.text()).toBe(200);
    const matrix = await dataOf(matrixResponse) as Array<Record<string, any>>;
    const limitedMatrix = matrix.map(item => ({
      ...item,
      permitido: item.modulo === 'Dashboard' && item.accion === 'Ver'
    }));
    const updateMatrix = await request.put(`${API_URL}/permisos/matriz/${limitedRoleId}`, {
      headers: auth(adminToken),
      data: { permisos: limitedMatrix }
    });
    expect(updateMatrix.status(), await updateMatrix.text()).toBe(200);

    const userResponse = await request.post(`${API_URL}/usuarios`, {
      headers: auth(adminToken),
      data: {
        nombreUsuario: LIMITED_USERNAME,
        nombreCompleto: 'Usuario Fase 6 Limitado',
        password: LIMITED_PASSWORD,
        rol: 'Vendedor',
        rolId: limitedRoleId
      }
    });
    expect(userResponse.status(), await userResponse.text()).toBe(200);
    const limitedUser = await dataOf(userResponse);

    // P1-005.A crea Usuario + UsuarioEmpresa atomicamente desde POST /usuarios.
    // No asignar una segunda vez la misma empresa: la API debe rechazar duplicados.
    const membershipResponse = await request.get(`${API_URL}/usuarios/${limitedUser.id}/empresas`, {
      headers: auth(adminToken)
    });
    expect(membershipResponse.status(), await membershipResponse.text()).toBe(200);
    const memberships = await dataOf(membershipResponse) as Array<Record<string, any>>;
    expect(memberships).toHaveLength(1);
    expect(memberships[0]).toMatchObject({
      usuarioId: limitedUser.id,
      empresaId: TENANT_ID,
      rolId: limitedRoleId,
      activa: true
    });

    limitedToken = await login(request, LIMITED_USERNAME, LIMITED_PASSWORD);
  });

  test('administrador obtiene grants relacionales explícitos sin mutar la matriz compartida', async ({ request }) => {
    const response = await request.get(`${API_URL}/permisos/matriz/${adminRoleId}`, {
      headers: auth(adminToken)
    });
    expect(response.status(), await response.text()).toBe(200);
    const matrix = await dataOf(response) as Array<Record<string, any>>;
    expect(matrix.length).toBeGreaterThan(20);
    expect(matrix.every(item => item.permitido === true)).toBe(true);
    expect(matrix.some(item => item.modulo === 'ReportesAdministrativos' && item.accion === 'Ver')).toBe(true);
    expect(matrix.some(item => item.modulo === 'ReportesAdministrativos' && item.accion === 'Exportar')).toBe(true);
    expect(matrix.some(item => item.modulo === 'Facturacion' && item.accion === 'Administrar')).toBe(true);
  });

  test('resumen consolida usuarios, roles, privilegios y auditoría', async ({ request }) => {
    const today = new Date().toISOString().slice(0, 10);
    const response = await request.get(
      `${API_URL}/reportes-administrativos/resumen?desde=${today}&hasta=${today}`,
      { headers: auth(adminToken) }
    );
    expect(response.status(), await response.text()).toBe(200);
    const data = await dataOf(response);
    expect(data.usuariosTotales).toBeGreaterThanOrEqual(2);
    expect(data.rolesTotales).toBeGreaterThanOrEqual(3);
    expect(data.usuariosPrivilegiados).toBeGreaterThanOrEqual(1);
    expect(data.permisosCatalogados).toBeGreaterThan(20);
    expect(Array.isArray(data.actividadPorModulo)).toBe(true);
    expect(Array.isArray(data.alertas)).toBe(true);
  });

  test('diagnóstico identifica accesos efectivos y rol limitado', async ({ request }) => {
    const usersResponse = await request.get(`${API_URL}/reportes-administrativos/usuarios-accesos`, {
      headers: auth(adminToken)
    });
    expect(usersResponse.status(), await usersResponse.text()).toBe(200);
    const users = await dataOf(usersResponse) as Array<Record<string, any>>;
    const admin = users.find(item => String(item.nombreUsuario).toLowerCase() === ADMIN_USERNAME.toLowerCase());
    const limited = users.find(item => item.nombreUsuario === LIMITED_USERNAME);
    expect(admin).toBeTruthy();
    expect(admin!.esAdministrador).toBe(true);
    expect(admin!.permisosEfectivos).toBeGreaterThan(20);
    expect(limited).toBeTruthy();
    expect(limited!.permisosEfectivos).toBe(1);
    expect(limited!.permisosSensibles).toBe(0);
    expect(limited!.estadoAcceso).toBe('Habilitado');

    const rolesResponse = await request.get(`${API_URL}/reportes-administrativos/roles-permisos`, {
      headers: auth(adminToken)
    });
    expect(rolesResponse.status(), await rolesResponse.text()).toBe(200);
    const roles = await dataOf(rolesResponse) as Array<Record<string, any>>;
    const adminRole = roles.find(item => item.rolId === adminRoleId);
    const limitedRole = roles.find(item => item.rolId === limitedRoleId);
    expect(adminRole.nivelPrivilegio).toBe('Crítico administrado');
    expect(adminRole.porcentajeCobertura).toBe(100);
    expect(limitedRole.permisosAsignados).toBe(1);
    expect(limitedRole.nivelPrivilegio).toBe('Bajo');
  });

  test('exportaciones CSV, XLSX y PDF son válidas y formatos desconocidos fallan cerrado', async ({ request }) => {
    const csv = await request.get(`${API_URL}/reportes-administrativos/exportar/usuarios?formato=csv`, {
      headers: auth(adminToken)
    });
    expect(csv.status(), await csv.text()).toBe(200);
    expect(csv.headers()['content-type']).toContain('text/csv');
    const csvText = (await csv.body()).toString('utf8');
    expect(csvText).toContain('PermisosEfectivos');
    expect(csvText).toContain(LIMITED_USERNAME);
    expect(csvText).not.toContain('PasswordHash');
    expect(csvText).not.toContain(LIMITED_PASSWORD);

    const xlsx = await request.get(`${API_URL}/reportes-administrativos/exportar/roles?formato=xlsx`, {
      headers: auth(adminToken)
    });
    expect(xlsx.status(), await xlsx.text()).toBe(200);
    expect(xlsx.headers()['content-type']).toContain('spreadsheetml');
    const xlsxBytes = await xlsx.body();
    expect(xlsxBytes.subarray(0, 2).toString()).toBe('PK');
    expect(xlsxBytes.length).toBeGreaterThan(1000);

    const pdf = await request.get(`${API_URL}/reportes-administrativos/exportar/usuarios?formato=pdf`, {
      headers: auth(adminToken)
    });
    expect(pdf.status(), await pdf.text()).toBe(200);
    expect(pdf.headers()['content-type']).toContain('application/pdf');
    const pdfBytes = await pdf.body();
    expect(pdfBytes.length).toBeGreaterThan(4);
    expect(pdfBytes.subarray(0, 4).toString('ascii')).toBe('%PDF');

    const invalid = await request.get(`${API_URL}/reportes-administrativos/exportar/usuarios?formato=html`, {
      headers: auth(adminToken)
    });
    expect(invalid.status(), await invalid.text()).toBe(400);

    const audit = await request.get(`${API_URL}/reportes-administrativos/exportar/auditoria?formato=csv`, {
      headers: auth(adminToken)
    });
    expect(audit.status(), await audit.text()).toBe(200);
    const auditText = (await audit.body()).toString('utf8');
    expect(auditText).toContain('IpEnmascarada');
    expect(auditText).not.toContain('ValoresAnteriores');
    expect(auditText).not.toContain('ValoresNuevos');
  });

  test('rol no administrativo recibe 403 en reportes, exportación y auditoría', async ({ request }) => {
    const permissions = await request.get(`${API_URL}/permisos/mis-permisos/empresa/${TENANT_ID}`, {
      headers: auth(limitedToken)
    });
    expect(permissions.status(), await permissions.text()).toBe(200);
    const data = await dataOf(permissions);
    expect(data.permisos).toContain('Dashboard:Ver');
    expect(data.permisos).not.toContain('ReportesAdministrativos:Ver');
    expect(data.permisos).not.toContain('ReportesAdministrativos:Exportar');

    for (const path of [
      '/reportes-administrativos/resumen',
      '/reportes-administrativos/usuarios-accesos',
      '/reportes-administrativos/roles-permisos',
      '/reportes-administrativos/exportar/usuarios?formato=csv',
      '/auditoria?page=1&pageSize=10'
    ]) {
      const denied = await request.get(`${API_URL}${path}`, { headers: auth(limitedToken) });
      expect(denied.status(), `Se esperaba 403 en ${path}`).toBe(403);
    }
  });

  test('centro de reportes soporta navegación directa y conserva el contrato administrativo', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 1000 });
    await loginUi(page);

    await page.goto('/centro-reportes');
    await expect(page.getByRole('heading', { name: 'Centro de Reportes' })).toBeVisible();
    await expect(page.getByRole('navigation', { name: 'Tipos de reportes disponibles' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Reportes administrativos' })).toBeVisible();

    await page.getByRole('link', { name: 'Reportes administrativos' }).click();
    await expect(page).toHaveURL(/\/centro-reportes\/administrativos$/);
    await expect(page.getByRole('heading', { name: 'Centro de Reportes' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Reportes administrativos' })).toBeVisible();
    await expect(page.getByText('Usuarios habilitados')).toBeVisible();
    await noHorizontalOverflow(page);
  });

  test('interfaz legacy de auditoría mantiene reportes y bitácora sin desbordamiento', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 1000 });
    await loginUi(page);
    await page.goto('/auditoria');
    await expect(page.getByRole('heading', { name: 'Reportes administrativos' })).toBeVisible();
    await expect(page.getByText('Usuarios habilitados')).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Roles y permisos' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Bitácora detallada' })).toBeVisible();
    await noHorizontalOverflow(page);

    await page.getByRole('tab', { name: 'Roles y permisos' }).click();
    await expect(page.getByText('Crítico administrado').first()).toBeVisible();

    await page.setViewportSize({ width: 390, height: 844 });
    await page.reload();
    await expect(page.getByRole('heading', { name: 'Reportes administrativos' })).toBeVisible();
    await noHorizontalOverflow(page);
  });
});
