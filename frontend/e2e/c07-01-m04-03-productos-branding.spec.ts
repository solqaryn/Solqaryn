import { expect, test, type Page } from '@playwright/test';

// Real Angular/Chromium; all HTTP/JWT/RBAC/tenant responses here are synthetic,
// not evidence of signed-JWT enforcement or a real MySQL database.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
interface Fixture { id?: 71 | 72; denied?: boolean; roleDenied?: boolean; name?: string; }
async function setup(page: Page, options: Fixture = {}) {
  const id = options.id ?? 71;
  const name = options.name ?? (id === 71 ? 'Comercio A' : 'Comercio B');
  const writes: string[] = [];
  await page.addInitScript((tenantId) => {
    localStorage.setItem('solqaryn_token', 'C0701_SYNTHETIC_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', String(tenantId));
  }, id);
  page.on('request', request => {
    const u = new URL(request.url());
    if ((u.pathname.includes('/api/') || ['5000', '5001', '5005'].includes(u.port))
      && !['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method() + ' ' + u.pathname);
  });
  await page.route('**/*', async route => {
    const request = route.request(), url = new URL(request.url()), path = url.pathname;
    if (request.resourceType() === 'document') return route.continue();
    if (request.method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    let status = 200, data: unknown = null;
    if (path.endsWith('/tenant-context/empresas')) {
      data = [{ empresaId: id, nombre: name }];
    } else if (path.endsWith('/tenant-context/' + id)) {
      if (options.denied) status = 403;
      else data = { usuarioId: 7, empresaId: id, rolId: 3, rolNombre: 'Lector', esAdministrador: false };
    } else if (path.endsWith('/permisos/mis-permisos/empresa/' + id)) {
      const allowed = !options.roleDenied;
      data = { esAdministrador: false, operacionMultiEmpresaRestringida: true,
        permisos: allowed ? ['Productos:Ver'] : [],
        capacidadesHabilitadas: allowed ? ['Productos:Ver'] : [],
        rutasHabilitadas: allowed ? ['productos'] : [] };
    } else if (path.endsWith('/empresa-configuracion/tenant/' + id)) {
      if (options.denied) status = 403;
      else data = { empresaId: id, nombre: name, moneda: 'HNL',
        zonaHoraria: 'America/Tegucigalpa', impuestosJson: '{}', emisionJson: '{}',
        correoConfigurado: false, version: 1, plantillasCorreo: [] };
    } else if (path.endsWith('/empresa-configuracion/publica') || path.endsWith('/empresa-configuracion')) {
      data = { id: 0, nombreVisibleSistema: 'SOLQARYN', nombreComercial: 'SOLQARYN' };
    } else if (path.endsWith('/tema-visual')) data = {};
    else if (/\/productos(?:\/|$)/.test(path)) {
      if (request.method() !== 'GET') status = 405;
      else data = { items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 };
    } else if (/\/(categorias|colores|tallas|marcas|modelos)(?:\/|$)/.test(path)) data = [];
    else if (path.includes('/api/') || ['5000','5001','5005'].includes(url.port)) status = 404;
    else return route.continue();
    await route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200 ? { success: true, data } :
        { success: false, message: 'NO_PERMITIDO' }) });
  });
  return { writes };
}
test('C07-01.M04.03 verified company A branding is reactive in actual Angular DOM', async ({ page }) => {
  const io = await setup(page, { id: 71 });
  await page.goto('/productos');
  await expect(page.getByRole('heading', { name: 'Productos', level: 1 })).toBeVisible();
  await expect(page.getByText('Catálogo de Comercio A', { exact: true })).toBeVisible();
  await expect(page.getByText('Catálogo de Comercio B', { exact: true })).toHaveCount(0);
  expect(io.writes).toEqual([]);
});
test('C07-01.M04.03 separate company B does not inherit A and remains responsive', async ({ page }) => {
  const io = await setup(page, { id: 72 });
  await page.goto('/productos');
  await expect(page.getByText('Catálogo de Comercio B', { exact: true })).toBeVisible();
  await expect(page.getByText('Catálogo de Comercio A', { exact: true })).toHaveCount(0);
  for (const width of [320, 1280]) {
    await page.setViewportSize({ width, height: 840 });
    await expect(page.getByLabel('Marca del catálogo de productos')).toBeVisible();
    const bounds = await page.locator('.catalog-tenant').evaluate(el =>
      ({ viewport: window.innerWidth, left: el.getBoundingClientRect().left, right: el.getBoundingClientRect().right }));
    expect(bounds.left).toBeGreaterThanOrEqual(0);
    expect(bounds.right).toBeLessThanOrEqual(bounds.viewport + 1);
  }
  expect(io.writes).toEqual([]);
});
test('C07-01.M04.03 hostile tenant response does not inject HTML and fails neutral', async ({ page }) => {
  const io = await setup(page, { name: '<img src=x onerror=alert(1)>' });
  await page.goto('/productos');
  await expect(page.getByRole('heading', { name: 'Productos', level: 1 })).toBeVisible();
  await expect(page.getByText('Catálogo de SOLQARYN', { exact: true })).toBeVisible();
  await expect(page.locator('.catalog-tenant img')).toHaveCount(0);
  await expect(page.getByText('<img src=x onerror=alert(1)>')).toHaveCount(0);
  expect(io.writes).toEqual([]);
});
test('C07-01.M04.03 anonymous, tenant-denied, role-denied do not render catalog brand', async ({ context }) => {
  const anonymous = await context.newPage();
  await anonymous.goto('/productos');
  await expect(anonymous).toHaveURL(/\/login(?:\?|$)/);
  await expect(anonymous.locator('.catalog-tenant')).toHaveCount(0);
  await anonymous.close();
  for (const options of [{ denied: true }, { roleDenied: true }]) {
    const page = await context.newPage();
    const io = await setup(page, options);
    await page.goto('/productos');
    await expect(page).not.toHaveURL(/\/productos(?:\?|$)/);
    await expect(page.locator('.catalog-tenant')).toHaveCount(0);
    expect(io.writes).toEqual([]);
    await page.close();
  }
});
