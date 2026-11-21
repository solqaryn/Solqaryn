import { expect, test, type Page } from '@playwright/test';

// Actual Angular 22 router + Chromium. Synthetic HTTP/JWT fixtures do not
// certify signed backend JWT, live RBAC or MySQL tenant isolation.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

interface Scenario {
  tenantId?: 71 | 72;
  tenantDenied?: boolean;
  permissionDenied?: boolean;
}

async function setup(page: Page, scenario: Scenario = {}) {
  const id = scenario.tenantId ?? 71;
  const writes: string[] = [];
  await page.addInitScript((tenantId) => {
    localStorage.setItem('solqaryn_token', 'C0701_SYNTHETIC_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', String(tenantId));
  }, id);

  page.on('request', request => {
    const u = new URL(request.url());
    if ((u.pathname.includes('/api/') || ['5000', '5001', '5005'].includes(u.port))
      && !['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
      writes.push(request.method() + ' ' + u.pathname);
    }
  });
  await page.route('**/*', async route => {
    const req = route.request(), url = new URL(req.url()), path = url.pathname;
    if (req.resourceType() === 'document') return route.continue();
    if (req.method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });

    let status = 200, data: unknown = null;
    if (path.endsWith('/tenant-context/empresas')) {
      data = [{ empresaId: id, nombre: id === 71 ? 'Comercio A' : 'Comercio B' }];
    } else if (path.endsWith('/tenant-context/' + id)) {
      if (scenario.tenantDenied) status = 403;
      else data = { usuarioId: 7, empresaId: id, rolId: 3, rolNombre: 'Lector', esAdministrador: false };
    } else if (path.endsWith('/permisos/mis-permisos/empresa/' + id)) {
      const allowed = !scenario.permissionDenied;
      data = { esAdministrador: false, operacionMultiEmpresaRestringida: true,
        permisos: allowed ? ['Productos:Ver'] : [],
        capacidadesHabilitadas: allowed ? ['Productos:Ver'] : [],
        rutasHabilitadas: allowed ? ['productos'] : [] };
    } else if (path.endsWith('/empresa-configuracion/tenant/' + id)) {
      if (scenario.tenantDenied) status = 403;
      else data = { empresaId: id, nombre: id === 71 ? 'Comercio A' : 'Comercio B',
        moneda: 'HNL', zonaHoraria: 'America/Tegucigalpa', impuestosJson: '{}',
        emisionJson: '{}', correoConfigurado: false, version: 1, plantillasCorreo: [] };
    } else if (path.endsWith('/empresa-configuracion/publica') || path.endsWith('/empresa-configuracion')) {
      data = { id: 0, nombreVisibleSistema: 'SOLQARYN', nombreComercial: 'SOLQARYN' };
    } else if (path.endsWith('/tema-visual')) {
      data = {};
    } else if (/\/productos(?:\/|$)/.test(path)) {
      if (req.method() !== 'GET') status = 405;
      else data = { items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 };
    } else if (/\/(categorias|colores|tallas|marcas|modelos)(?:\/|$)/.test(path)) {
      data = [];
    } else if (path.includes('/api/') || ['5000', '5001', '5005'].includes(url.port)) {
      status = 404;
    } else {
      return route.continue();
    }
    await route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200 ? { success: true, data } :
        { success: false, message: 'DENEGADO_SINTETICO' }) });
  });
  return writes;
}

test('C07-01.M04.01 anonymous alias ends at protected login, never catalog', async ({ page }) => {
  await page.goto('/catalogo-universal-v22');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Productos', level: 1 })).toHaveCount(0);
});

test('C07-01.M04.01 company A verifies membership/permission then redirects to canonical Productos', async ({ page }) => {
  const writes = await setup(page, { tenantId: 71 });
  await page.goto('/catalogo-universal-v22');
  await expect(page).toHaveURL(/\/productos(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Productos', level: 1 })).toBeVisible();
  await expect(page.getByText('Catálogo de Comercio A', { exact: true })).toBeVisible();
  await expect(page.getByText('Catálogo de Comercio B', { exact: true })).toHaveCount(0);
  expect(writes).toEqual([]);
});

test('C07-01.M04.01 separate verified company B never inherits A branding', async ({ page }) => {
  const writes = await setup(page, { tenantId: 72 });
  await page.goto('/catalogo-universal-v22');
  await expect(page).toHaveURL(/\/productos(?:\?|$)/);
  await expect(page.getByText('Catálogo de Comercio B', { exact: true })).toBeVisible();
  await expect(page.getByText('Catálogo de Comercio A', { exact: true })).toHaveCount(0);
  expect(writes).toEqual([]);
});

test('C07-01.M04.01 denied tenant or role cannot reach catalog via alias', async ({ context }) => {
  for (const scenario of [{ tenantDenied: true }, { permissionDenied: true }]) {
    const page = await context.newPage();
    const writes = await setup(page, scenario);
    await page.goto('/catalogo-universal-v22');
    await expect(page).not.toHaveURL(/\/productos(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Productos', level: 1 })).toHaveCount(0);
    expect(writes).toEqual([]);
    await page.close();
  }
});
