import { expect, test, type Page } from '@playwright/test';

// Real Angular/Chromium; HTTP/JWT are synthetic, not certified backend or MySQL.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
type Scenario = {
  deferProduct?: () => Promise<void>;
  productResponse?: (requestNumber: number) => unknown;
};
async function setup(page: Page, scenario: Scenario = {}): Promise<string[]> {
  const requests: string[] = [];
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C0701M0406_SYNTHETIC_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route('**/*', async route => {
    const req = route.request(), url = new URL(req.url()), path = url.pathname;
    if (req.resourceType() === 'document') return route.continue();
    if (req.method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    if (!path.startsWith('/api/') && !['5000', '5001', '5005'].includes(url.port)) {
      return route.continue();
    }
    let status = 200, data: unknown = null;
    if (path.endsWith('/tenant-context/empresas')) {
      data = [{ empresaId: 71, nombre: 'Comercio A' }];
    } else if (path.endsWith('/tenant-context/71')) {
      data = { usuarioId: 7, empresaId: 71, rolId: 3, rolNombre: 'Lector', esAdministrador: false };
    } else if (path.endsWith('/permisos/mis-permisos/empresa/71')) {
      data = { esAdministrador: false, operacionMultiEmpresaRestringida: true, permisos: ['Productos:Ver'],
        capacidadesHabilitadas: ['Productos:Ver'], rutasHabilitadas: ['productos'] };
    } else if (path.endsWith('/empresa-configuracion/tenant/71')) {
      data = { empresaId: 71, nombre: 'Comercio A', moneda: 'HNL',
        zonaHoraria: 'America/Tegucigalpa', impuestosJson: '{}',
        emisionJson: '{}', correoConfigurado: false, version: 1, plantillasCorreo: [] };
    } else if (path.endsWith('/empresa-configuracion/publica') || path.endsWith('/empresa-configuracion')) {
      data = { id: 0, nombreVisibleSistema: 'SOLQARYN', nombreComercial: 'SOLQARYN' };
    } else if (path.endsWith('/tema-visual')) {
      data = {};
    } else if (path.endsWith('/productos')) {
      if (req.method() !== 'GET') {
        status = 405;
      } else {
        requests.push(url.search);
        if (scenario.deferProduct) await scenario.deferProduct();
        data = scenario.productResponse?.(requests.length) ??
          { items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 };
      }
    } else if (/\/(categorias|colores|tallas|marcas|modelos)(?:\/|$)/.test(path)) {
      data = [];
    } else {
      status = 404;
    }
    await route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200 ? { success: true, data } :
        { success: false, message: 'DENEGADO_SINTETICO' }) });
  });
  return requests;
}

test('C07-01.M04.06 mobile 375px density desktop 1280px and only first page', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  const requests = await setup(page);
  await page.goto('/catalogo-universal-v22');
  await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado: 0 productos.');
  await expect(page.locator('.cards-mobile')).toBeVisible();
  await expect(page.locator('.table-scroll')).toBeHidden();
  await expect(page.getByRole('textbox', { name: 'Buscar productos' })).toBeVisible();
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow, 'unexpected mobile horizontal overflow').toBeLessThanOrEqual(2);
  await page.setViewportSize({ width: 1280, height: 800 });
  await expect(page.locator('.table-scroll')).toBeVisible();
  await expect(page.locator('.cards-mobile')).toBeHidden();
  expect(requests).toHaveLength(1);
  expect(new URLSearchParams(requests[0]).get('pageSize')).toBe('10');
});

test('C07-01.M04.06 delayed first page displays accessible loading not false success', async ({ page }) => {
  let release!: () => void;
  const gate = new Promise<void>(resolve => { release = resolve; });
  await setup(page, { deferProduct: () => gate });
  try {
    await page.goto('/catalogo-universal-v22');
    await expect(page.locator('.loading[role="status"]')).toContainText('Cargando productos...');
    await expect(page.locator('.catalog-success')).toHaveCount(0);
  } finally {
    release();
  }
  await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado: 0 productos.');
  await expect(page.locator('.loading')).toHaveCount(0);
});

test('C07-01.M04.06 malformed HTTP 200 fails closed and read-only retry recovers', async ({ page }) => {
  const writes: string[] = [];
  page.on('request', req => {
    const url = new URL(req.url());
    if ((url.pathname.startsWith('/api/') || ['5000', '5001', '5005'].includes(url.port))
      && !['GET', 'OPTIONS', 'HEAD'].includes(req.method())) writes.push(req.method());
  });
  const requests = await setup(page, {
    productResponse: n => n === 1 ? { items: [], totalCount: 'INVALID' } :
      { items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 }
  });
  await page.goto('/catalogo-universal-v22');
  await expect(page.locator('.catalog-error[role="alert"]')).toContainText('No se pudo cargar el catálogo');
  await expect(page.locator('.catalog-success')).toHaveCount(0);
  await page.getByRole('button', { name: 'Reintentar carga' }).click();
  await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado: 0 productos.');
  expect(requests).toHaveLength(2);
  expect(writes).toEqual([]);
});
