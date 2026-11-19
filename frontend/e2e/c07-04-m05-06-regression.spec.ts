import { expect, test, type Page } from '@playwright/test';

/** C07-04.M05.06: regression de UI real, API / tenant / JWT sinteticos.
 * No demuestra pagos, persistencia, autorizacion firmada ni estado fiscal.
 */
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function context(page: Page): Promise<() => number> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c07-m0506-NOT-a-signed-JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        usuarioId: 7, empresaId: 71, rolId: 3, rolNombre: 'Lector', esAdministrador: false
      } }) });
  });
  await page.route(/\/permisos\/mis-permisos\/empresa\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        permisos: ['Ventas:Ver'], capacidadesHabilitadas: ['Ventas:Ver'],
        rutasHabilitadas: ['pedidos-venta'], operacionMultiEmpresaRestringida: true,
        esAdministrador: false
      } }) });
  });
  let reads = 0;
  await page.route(/\/pedidos-venta(?:\?|$)/, async route => {
    if (route.request().resourceType() === 'document') return route.continue();
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    expect(route.request().method()).toBe('GET');
    reads++;
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0
      } }) });
  });
  return () => reads;
}

test('C07-04.M05.06 real Chromium: responsive, reduced motion, teclado y DCL', async ({ page }) => {
  const reads = await context(page);
  const mutations: string[] = [];
  page.on('request', r => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(r.method()))
      mutations.push(r.method() + ' ' + r.url());
  });
  await page.emulateMedia({ reducedMotion: 'reduce' });
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/pedidos-venta', { waitUntil: 'domcontentloaded' });
    await expect(page.getByRole('heading', { name: 'Pedidos de venta' })).toBeVisible();
    await expect(page.getByText('Sin resultados.')).toBeVisible();
    await expect(page.locator('.pedidos-readiness-v5 ol > li')).toHaveCount(4);
    expect(await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth
    )).toBeLessThanOrEqual(2);
    const ms = await page.evaluate(() => {
      const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
      return nav?.domContentLoadedEventEnd ?? NaN;
    });
    expect(Number.isFinite(ms)).toBe(true);
    expect(ms, 'DCL local smoke, not CWV or p95').toBeLessThan(30_000);
  }
  expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
  const filter = page.getByRole('combobox', { name: 'Estado' });
  let reached = false;
  for (let i = 0; i < 100; i++) {
    await page.keyboard.press('Tab');
    if (await filter.evaluate(el => document.activeElement === el)) { reached = true; break; }
  }
  expect(reached, 'Real Tab navigation; no .focus()').toBe(true);
  expect(reads()).toBeGreaterThanOrEqual(3);
  expect(mutations).toEqual([]);
});

test('C07-04.M05.06 anonymous denies orders before financial data appear', async ({ page }) => {
  const mutations: string[] = [];
  page.on('request', r => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(r.method())) mutations.push(r.method());
  });
  await page.goto('/pedidos-venta');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Pedidos de venta' })).toHaveCount(0);
  await expect(page.getByText('PED-47')).toHaveCount(0);
  await expect(page.locator('.pedidos-readiness-v5')).toHaveCount(0);
  expect(mutations).toEqual([]);
});
