import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

// C07-01.M06.06: Angular real -> API ASP.NET 10 -> JWT firmado -> MySQL 8.4.
// CI efimera. Sin page.route, token fabricado, secretos o datos de produccion.
// La mutacion positiva con variantes, rollback ERP y A5 siguen pendientes.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('C0701_REQUIRES_GITHUB_HOSTED_REAL_JWT_MYSQL84');
}

async function sessionReal(page: Page, request: APIRequestContext, username: string, tenant: string) {
  const response = await request.post(api + '/auth/login', {
    data: { nombreUsuario: username, password }
  });
  expect(response.status(), 'Login de backend real').toBe(200);
  const payload = await response.json();
  expect(payload?.success).toBe(true);
  const session = payload?.data;
  expect(typeof session?.token).toBe('string');
  expect(session.token.split('.')).toHaveLength(3);
  expect(typeof session?.expiraEn).toBe('string');
  expect(session.nombreUsuario).toBe(username);
  await page.addInitScript((s: {
    token: string; user: string; role: string; expiry: string; requestedTenant: string;
  }) => {
    localStorage.setItem('solqaryn_token', s.token);
    localStorage.setItem('solqaryn_user', s.user);
    localStorage.setItem('solqaryn_rol', s.role);
    localStorage.setItem('solqaryn_expira_en', s.expiry);
    localStorage.setItem('solqaryn_empresa_solicitada_id', s.requestedTenant);
  }, {
    token: session.token as string,
    user: session.nombreUsuario as string,
    role: String(session.rol),
    expiry: session.expiraEn as string,
    requestedTenant: tenant
  });
  return session.token as string;
}

function isBackendResponse(url: string, route: string) {
  const parsed = new URL(url);
  return parsed.port === '5005' && parsed.pathname === route;
}

test.describe('C07-01.M06.06 navegador real y aislamiento A/B', () => {
  test.describe.configure({ retries: 0 });

  test('Angular Productos consulta API/MySQL real con JWT y refluye 320-1440', async ({ page, request }) => {
    await sessionReal(page, request, 'c1504_admin_a', '1');
    await page.setViewportSize({ width: 320, height: 840 });
    const writes: string[] = [];
    page.on('request', (r) => {
      const u = new URL(r.url());
      if (u.port === '5005' && !['GET', 'HEAD', 'OPTIONS'].includes(r.method())) {
        writes.push(r.method() + ' ' + u.pathname);
      }
    });
    const catalog = page.waitForResponse((r) =>
      isBackendResponse(r.url(), '/productos') && r.request().method() === 'GET');
    await page.goto('/productos');
    const response = await catalog;
    expect(response.status(), 'GET /productos real').toBe(200);
    const json = await response.json();
    expect(json?.success).toBe(true);
    expect(Array.isArray(json?.data?.items), 'Oracle EF produce un contrato real').toBe(true);
    expect(Number.isSafeInteger(json?.data?.totalCount)).toBe(true);
    const ownNames = json.data.items.map((p: { nombre?: string }) => p.nombre);
    expect(ownNames, 'Producto A sembrado fisicamente en MySQL').toContain('C0701BrowserTenantA');
    expect(ownNames, 'Filas del tenant B no salen en payload JWT A').not.toContain('C0701BrowserTenantB');
    await expect(page.getByRole('heading', { name: 'Productos', level: 1 })).toBeVisible();
    await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado:');
    await expect(page.locator('.catalog-error')).toHaveCount(0);
    await expect(page.locator('.cards-mobile .producto-card').filter({ hasText: 'C0701BrowserTenantA' })).toBeVisible();
    await expect(page.getByText('C0701BrowserTenantB', { exact: true })).toHaveCount(0);
    for (const width of [320, 390, 768, 1280, 1440]) {
      await page.setViewportSize({ width, height: 840 });
      await expect(page.getByRole('heading', { name: 'Productos', level: 1 })).toBeVisible();
      const overflow = await page.evaluate(() =>
        document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(overflow, 'Overflow de body a ' + width).toBeLessThanOrEqual(2);
    }
    await page.screenshot({ path: 'test-results/c0701-real-1440.png', fullPage: true });
    await page.setViewportSize({ width: 320, height: 840 });
    await page.screenshot({ path: 'test-results/c0701-real-320.png', fullPage: true });
    expect(writes).toEqual([]);
  });

  test('JWT del tenant A no accede al contexto B ni a productos ajenos', async ({ page, request }) => {
    await sessionReal(page, request, 'c1504_admin_a', '2');
    const denied = page.waitForResponse((r) =>
      isBackendResponse(r.url(), '/tenant-context/2') && r.request().method() === 'GET');
    await page.goto('/productos');
    expect([403, 404], 'Empresa solicitada no concede membresia').toContain((await denied).status());
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('.catalog-success')).toHaveCount(0);
    await expect(page.locator('.table-desktop tbody tr')).toHaveCount(0);
  });

  test('Miembro B con JWT real consulta su catálogo; no puede seleccionar A', async ({ page, request }) => {
    // Prueba positiva de un segundo actor fisico: un 503 no es conexion valida.
    const tokenB = await sessionReal(page, request, 'c1504_admin_b', '2');
    await page.setViewportSize({ width: 390, height: 840 });
    const tenantResponse = page.waitForResponse((r) =>
      isBackendResponse(r.url(), '/tenant-context/2') && r.request().method() === 'GET');
    const ownCatalog = page.waitForResponse((r) =>
      isBackendResponse(r.url(), '/productos') && r.request().method() === 'GET');
    await page.goto('/productos');
    expect((await tenantResponse).status(), 'Contexto SQL real de empresa B').toBe(200);
    const ownResponse = await ownCatalog;
    expect(ownResponse.status(), 'GET catalogo real autorizado para B').toBe(200);
    const ownJson = await ownResponse.json();
    expect(ownJson?.success).toBe(true);
    expect(Array.isArray(ownJson?.data?.items)).toBe(true);
    const bNames = ownJson.data.items.map((p: { nombre?: string }) => p.nombre);
    expect(bNames, 'Producto B de base MySQL real').toContain('C0701BrowserTenantB');
    expect(bNames, 'Tenant A nunca se filtra al JWT B').not.toContain('C0701BrowserTenantA');
    await expect(page.getByRole('heading', { name: 'Productos', level: 1 })).toBeVisible();
    await expect(page.locator('.catalog-error')).toHaveCount(0);
    await expect(page.locator('.cards-mobile .producto-card').filter({ hasText: 'C0701BrowserTenantB' })).toBeVisible();
    await expect(page.getByText('C0701BrowserTenantA', { exact: true })).toHaveCount(0);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'Empresa B no debe desbordar el body a 390px').toBeLessThanOrEqual(2);

    // El mismo JWT B nunca concede acceso al seleccionar tenant A.
    const cross = await request.get(api + '/productos?page=1&pageSize=10', {
      headers: { Authorization: 'Bearer ' + tokenB, 'X-Empresa-Id': '1' }
    });
    expect([403, 404, 503], 'Selector A no confiere membresia a B').toContain(cross.status());
    const deniedBody = await cross.text();
    expect(deniedBody).not.toContain('C0701RealProductoA');
    expect(deniedBody).not.toContain('"items":');
  });

  test('Firma JWT adulterada es rechazada por API real', async ({ page, request }) => {
    const jwt = await sessionReal(page, request, 'c1504_admin_a', '1');
    const parts = jwt.split('.');
    const sig = parts[2];
    expect(sig.length).toBeGreaterThan(8);
    parts[2] = (sig[0] === 'A' ? 'B' : 'A') + sig.slice(1);
    const rejected = await request.get(api + '/productos?page=1&pageSize=10', {
      headers: { Authorization: 'Bearer ' + parts.join('.'), 'X-Empresa-Id': '1' }
    });
    expect([401, 403]).toContain(rejected.status());
    expect(await rejected.text()).not.toContain('C0701RealProductoA');
  });
  test('Revocación MySQL real invalida catálogo Angular cargado sin renovar JWT', async ({ page, request }) => {
    test.setTimeout(120_000);
    const jwt = await sessionReal(page, request, 'c1504_admin_a', '1');
    await page.setViewportSize({ width: 390, height: 840 });
    const firstCatalog = page.waitForResponse((r) =>
      isBackendResponse(r.url(), '/productos') && r.request().method() === 'GET');
    await page.goto('/productos');
    expect((await firstCatalog).status(), 'Catálogo A antes de revocación').toBe(200);
    await expect(page.locator('.cards-mobile .producto-card').filter({
      hasText: 'C0701BrowserTenantA'
    })).toBeVisible();
    const headers = { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '1' };

    // GitHub-hosted exclusivamente: cambios de membresía en MySQL 8.4 efímero.
    // Nunca cambia usuarios, permisos ni datos fuera del contenedor de CI.
    const { execFileSync } = await import('node:child_process');
    const mysqlContainer = execFileSync('docker',
      ['ps', '--filter', 'ancestor=mysql:8.4', '--format', '{{.ID}}'],
      { encoding: 'utf8' }).trim().split(/\r?\n/)[0];
    expect(mysqlContainer, 'Contenedor efímero MySQL 8.4').toMatch(/^[0-9a-f]{12,64}$/);
    const snapshotProducts = () => {
      // Readback fisico del contenedor exclusivo de CI: sin mocks ni datos externos.
      const sql = "SELECT CONCAT(EmpresaId,'|',Nombre,'|'," +
        "COALESCE(CAST(Cantidad AS CHAR),'NULL'),'|',COALESCE(CAST(Precio AS CHAR),'NULL')) " +
        "FROM Productos WHERE (EmpresaId=1 AND Nombre='C0701BrowserTenantA') " +
        "OR (EmpresaId=2 AND Nombre='C0701BrowserTenantB') ORDER BY EmpresaId;";
      const rows = execFileSync('docker', [
        'exec', '-i', mysqlContainer!, 'mysql', '-uroot', '-proot',
        'solqaryn_c1504_real_ci', '-Nse', sql
      ], { encoding: 'utf8' }).trim().split(/\r?\n/);
      expect(rows, 'Dos productos SQL reales A/B').toHaveLength(2);
      expect(rows[0]).toMatch(/^1\|C0701BrowserTenantA\|/);
      expect(rows[1]).toMatch(/^2\|C0701BrowserTenantB\|/);
      return rows;
    };
    const beforeRevocation = snapshotProducts();
    const setMembership = (active: 0 | 1) => {
      const sql = 'UPDATE UsuarioEmpresas ue JOIN Usuarios u ON u.Id=ue.UsuarioId ' +
        'SET ue.Activa=' + active + ',ue.FechaActualizacion=UTC_TIMESTAMP(6) ' +
        "WHERE u.NombreUsuario='c1504_admin_a' AND ue.EmpresaId=1;" +
        'SELECT ue.Activa FROM UsuarioEmpresas ue JOIN Usuarios u ON u.Id=ue.UsuarioId ' +
        "WHERE u.NombreUsuario='c1504_admin_a' AND ue.EmpresaId=1;";
      const actual = execFileSync('docker', [
        'exec', '-i', mysqlContainer!, 'mysql', '-uroot', '-proot',
        'solqaryn_c1504_real_ci', '-Nse', sql
      ], { encoding: 'utf8' }).trim();
      expect(actual, 'Readback membresía SQL Activa=' + active).toBe(String(active));
    };

    try {
      setMembership(0);
      const denied = await request.get(api + '/productos?page=1&pageSize=10', { headers });
      expect([401, 403, 404, 503], 'El JWT emitido antes no autoriza el catálogo').toContain(denied.status());
      expect(await denied.text(), 'Denegación sin producto A').not.toContain('C0701BrowserTenantA');
      // La revocacion A no afecta el login, catalogo ni filas de B.
      const loginB = await request.post(api + '/auth/login', {
        data: { nombreUsuario: 'c1504_admin_b', password }
      });
      expect(loginB.status()).toBe(200);
      const tokenB = (await loginB.json())?.data?.token;
      expect(typeof tokenB).toBe('string');
      const catalogB = await request.get(api + '/productos?page=1&pageSize=10', {
        headers: { Authorization: 'Bearer ' + tokenB, 'X-Empresa-Id': '2' }
      });
      expect(catalogB.status(), 'Tenant B permanece independiente').toBe(200);
      const bData = await catalogB.json();
      const bNames = bData?.data?.items?.map((p: { nombre?: string }) => p.nombre);
      expect(bNames).toContain('C0701BrowserTenantB');
      expect(bNames).not.toContain('C0701BrowserTenantA');
      expect(snapshotProducts(), 'Revocacion A no cambia filas, precios ni cantidad A/B')
        .toEqual(beforeRevocation);
      const deniedTenant = page.waitForResponse((r) =>
        isBackendResponse(r.url(), '/tenant-context/1') && r.request().method() === 'GET');
      await page.reload();
      expect([401, 403, 404], 'Tenant revocado en API real').toContain((await deniedTenant).status());
      await expect(page).toHaveURL(/\/login(?:\?|$)/);
      await expect(page.getByText('C0701BrowserTenantA', { exact: true })).toHaveCount(0);
      await expect(page.locator('.catalog-success')).toHaveCount(0);
    } finally {
      setMembership(1); // Restaurar incluso si la prueba adversarial falla.
      expect(snapshotProducts(), 'Rollback de membresia conserva productos A/B').toEqual(beforeRevocation);
    }
    const restored = await request.get(api + '/productos?page=1&pageSize=10', { headers });
    expect(restored.status(), 'Membresía CI restaurada con el mismo JWT').toBe(200);
    const restoredJson = await restored.json();
    expect(restoredJson?.data?.items?.map((p: { nombre?: string }) => p.nombre))
      .toContain('C0701BrowserTenantA');
  });

});
