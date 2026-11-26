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


  test('API real niega POST/PUT/DELETE de catalogo y conserva SQL A/B', async ({ page, request }) => {
    test.setTimeout(90_000);
    const jwtA = await sessionReal(page, request, 'c1504_admin_a', '1');
    const jwtB = await sessionReal(page, request, 'c1504_admin_b', '2');
    const headersA = { Authorization: 'Bearer ' + jwtA, 'X-Empresa-Id': '1' };
    const headersB = { Authorization: 'Bearer ' + jwtB, 'X-Empresa-Id': '2' };
    const { execFileSync } = await import('node:child_process');
    const container = execFileSync('docker',
      ['ps', '--filter', 'ancestor=mysql:8.4', '--format', '{{.ID}}'],
      { encoding: 'utf8' }).trim().split(/\r?\n/)[0];
    expect(container).toMatch(/^[0-9a-f]{12,64}$/);
    const sql = (statement: string) => execFileSync('docker', [
      'exec', '-i', container!, 'mysql', '-uroot', '-proot',
      'solqaryn_c1504_real_ci', '-Nse', statement
    ], { encoding: 'utf8' }).trim();
    const snapshot = () => sql(
      'SELECT Id,EmpresaId,Nombre,Cantidad,Precio,Activo,Eliminado ' +
      "FROM Productos WHERE Nombre IN ('C0701BrowserTenantA','C0701BrowserTenantB') " +
      'ORDER BY EmpresaId;'
    );
    const before = snapshot();
    expect(before.split(/\r?\n/), 'Dos filas A/B fisicas en MySQL').toHaveLength(2);
    const idA = Number(sql(
      "SELECT Id FROM Productos WHERE EmpresaId=1 AND Nombre='C0701BrowserTenantA' LIMIT 1;"
    ));
    expect(Number.isSafeInteger(idA) && idA > 0).toBe(true);
    const ownGet = await request.get(api + '/productos?page=1&pageSize=10', { headers: headersA });
    expect(ownGet.status(), 'Lectura autorizada A se mantiene').toBe(200);
    const multipart = {
      nombre: 'C0701ForbiddenCreate', marca: 'MarcaProhibida',
      modelo: 'SoloCI', tipoInventario: '1', cantidad: '4',
      costo: '10', precio: '30'
    };
    const attempts = [
      await request.post(api + '/productos', { headers: headersA, multipart }),
      await request.put(api + '/productos/' + idA, { headers: headersA, multipart }),
      await request.delete(api + '/productos/' + idA, { headers: headersA })
    ];
    for (const result of attempts) {
      expect([403, 503], 'Piloto privado solo lectura niega toda mutacion').toContain(result.status());
      const body = await result.text();
      expect(body).not.toContain('C0701BrowserTenantB');
      expect(body).not.toContain('MySqlException');
    }
    const foreignDelete = await request.delete(api + '/productos/' + idA, { headers: headersB });
    expect([403, 404, 503], 'JWT B nunca autoriza borrar producto A')
      .toContain(foreignDelete.status());
    expect(snapshot(), 'POST/PUT/DELETE denegados no alteran filas SQL A/B').toBe(before);
    expect(sql("SELECT COUNT(*) FROM Productos WHERE Nombre='C0701ForbiddenCreate';"))
      .toBe('0');
    const ownB = await request.get(api + '/productos?page=1&pageSize=10', { headers: headersB });
    expect(ownB.status(), 'B permanece autorizado tras los rechazos').toBe(200);
    const namesB = (await ownB.json())?.data?.items?.map((p: { nombre?: string }) => p.nombre);
    expect(namesB).toContain('C0701BrowserTenantB');
    expect(namesB).not.toContain('C0701BrowserTenantA');
  });

  test('Duplicar cotización en cuarentena no altera documentos ni detalles SQL A/B', async ({ page, request }) => {
    test.setTimeout(90_000);
    const jwtA = await sessionReal(page, request, 'c1504_admin_a', '1');
    const jwtB = await sessionReal(page, request, 'c1504_admin_b', '2');
    const headersA = { Authorization: 'Bearer ' + jwtA, 'X-Empresa-Id': '1' };
    const headersB = { Authorization: 'Bearer ' + jwtB, 'X-Empresa-Id': '2' };
    const { execFileSync } = await import('node:child_process');
    const containers = execFileSync('docker',
      ['ps', '--filter', 'ancestor=mysql:8.4', '--format', '{{.ID}}'],
      { encoding: 'utf8' }).trim().split(/\r?\n/);
    expect(containers, 'Un solo contenedor MySQL efímero').toHaveLength(1);
    const container = containers[0];
    expect(container).toMatch(/^[0-9a-f]{12,64}$/);
    const sql = (statement: string) => execFileSync('docker', [
      'exec', '-i', container!, 'mysql', '-uroot', '-proot',
      'solqaryn_c1504_real_ci', '-Nse', statement
    ], { encoding: 'utf8' }).trim();

    // Fixtures CI exclusivos. No se cambia ningún grant, piloto o middleware.
    sql(`INSERT INTO TipoClientes
      (EmpresaId,Codigo,Nombre,NombreNormalizado,ColorHex,Activo,EsSistema,Orden,EsPredeterminado,Eliminado,FechaCreacion,FechaActualizacion)
      SELECT e.Id,CONCAT('C0701_QUOTE_QA_',e.Id),CONCAT('C0701 Quote QA ',e.Id),
        CONCAT('C0701_QUOTE_QA_',e.Id),'#FFFFFF',1,0,0,0,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6)
      FROM Empresas e WHERE e.Id IN (1,2);
      INSERT INTO Clientes (EmpresaId,Nombre,Activo,TipoClienteId,FechaCreacion,FechaActualizacion)
      SELECT e.Id,CONCAT('C0701QuoteClient',e.Id),1,t.Id,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6)
      FROM Empresas e JOIN TipoClientes t ON t.EmpresaId=e.Id
        AND t.Codigo=CONCAT('C0701_QUOTE_QA_',e.Id) WHERE e.Id IN (1,2);
      INSERT INTO Cotizaciones (EmpresaId,Estado,ClienteId,ClienteNombreSnapshot,FechaCreacion,FechaActualizacion)
      SELECT EmpresaId,1,Id,Nombre,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6)
      FROM Clientes WHERE Nombre IN ('C0701QuoteClient1','C0701QuoteClient2');
      INSERT INTO CotizacionDetalles (EmpresaId,CotizacionId,ProductoId,ProductoNombreSnapshot,Cantidad,PrecioUnitario,FechaCreacion,FechaActualizacion)
      SELECT q.EmpresaId,q.Id,p.Id,p.Nombre,2,p.Precio,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6)
      FROM Cotizaciones q JOIN Productos p ON p.EmpresaId=q.EmpresaId
      WHERE q.ClienteNombreSnapshot IN ('C0701QuoteClient1','C0701QuoteClient2')
      AND p.Nombre IN ('C0701BrowserTenantA','C0701BrowserTenantB');`);
    expect(sql("SELECT COUNT(*) FROM Clientes c JOIN TipoClientes t ON t.Id=c.TipoClienteId AND t.EmpresaId=c.EmpresaId WHERE c.Nombre IN ('C0701QuoteClient1','C0701QuoteClient2') AND c.EmpresaId IN (1,2)")).toBe('2');
    const snapshot = () => [
      sql('SELECT Id,EmpresaId,Estado,ClienteId,ClienteNombreSnapshot,FechaActualizacion FROM Cotizaciones ORDER BY Id'),
      sql('SELECT Id,EmpresaId,CotizacionId,ProductoId,Cantidad,PrecioUnitario,FechaActualizacion FROM CotizacionDetalles ORDER BY Id'),
      sql("SELECT COUNT(*) FROM RegistrosAuditoria WHERE Entidad='Cotizacion' AND Resultado='Exito'")
    ];
    const quoteId = (tenant: number) => Number(sql(
      "SELECT Id FROM Cotizaciones WHERE EmpresaId=" + tenant +
      " AND ClienteNombreSnapshot='C0701QuoteClient" + tenant + "'"
    ));
    const idA = quoteId(1), idB = quoteId(2);
    expect(Number.isSafeInteger(idA) && idA > 0).toBe(true);
    expect(Number.isSafeInteger(idB) && idB > 0 && idB !== idA).toBe(true);
    expect(sql("SELECT COUNT(*) FROM CotizacionDetalles d JOIN Cotizaciones q ON q.Id=d.CotizacionId AND q.EmpresaId=d.EmpresaId WHERE q.ClienteNombreSnapshot IN ('C0701QuoteClient1','C0701QuoteClient2')")).toBe('2');
    const before = snapshot();
    const ownCatalog = async (headers: Record<string, string>, own: string, foreign: string) => {
      const res = await request.get(api + '/productos?page=1&pageSize=10', { headers });
      expect(res.status(), 'Control positivo de JWT y membresía vigente').toBe(200);
      const names = (await res.json())?.data?.items?.map((p: { nombre?: string }) => p.nombre);
      expect(names).toContain(own);
      expect(names).not.toContain(foreign);
    };
    await ownCatalog(headersA, 'C0701BrowserTenantA', 'C0701BrowserTenantB');
    await ownCatalog(headersB, 'C0701BrowserTenantB', 'C0701BrowserTenantA');
    for (const [headers, id] of [[headersA, idA], [headersA, idB], [headersB, idB], [headersB, idA]] as const) {
      const res = await request.post(api + '/cotizaciones/' + id + '/duplicar', { headers });
      expect(res.status(), 'Cuarentena comercial impide incluso duplicar el documento propio').toBe(503);
      expect(res.headers()['cache-control']).toContain('no-store');
      const body = await res.json();
      expect(body).toEqual({
        success: false, code: 'TENANT_DATA_ISOLATION_REQUIRED',
        message: 'Esta operación está temporalmente bloqueada por seguridad de los datos empresariales.'
      });
      expect(snapshot(), 'Sin duplicación, modificación ni recibo comercial exitoso').toEqual(before);
    }
    await ownCatalog(headersA, 'C0701BrowserTenantA', 'C0701BrowserTenantB');
    await ownCatalog(headersB, 'C0701BrowserTenantB', 'C0701BrowserTenantA');
  });

});
