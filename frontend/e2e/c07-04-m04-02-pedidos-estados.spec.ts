import { expect, test, type Page, type Route } from '@playwright/test';

// C07-04.M04.02: navegador real, token/tenant/permiso/HTTP sinteticos.
// No certifica MySQL, JWT real, autorizacion del backend o flujo fiscal.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const pedido = {
  id: 17, cotizacionId: 4, clienteId: 7,
  clienteNombreSnapshot: 'Cliente de prueba autorizado',
  estado: 1, total: 45.5, detalles: []
};
const result = (items: unknown[]) => ({
  success: true,
  data: { items, page: 1, pageSize: 10, totalCount: items.length, totalPages: items.length ? 1 : 0 }
});

async function user(page: Page, authorized = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0704-m0402-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    return route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        usuarioId: 7, empresaId: 71, rolId: 3, rolNombre: 'Lector', esAdministrador: false
      } }) });
  });
  await page.route(/\/permisos\/mis-permisos\/empresa\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    return route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        permisos: authorized ? ['Ventas:Ver'] : [],
        capacidadesHabilitadas: authorized ? ['Ventas:Ver'] : [],
        rutasHabilitadas: authorized ? ['pedidos-venta'] : [],
        operacionMultiEmpresaRestringida: true, esAdministrador: false
      } }) });
  });
}

async function orders(page: Page, handler: (route: Route) => Promise<void>) {
  await page.route(/\/pedidos-venta(?:\?|$)/, async route => {
    if (route.request().resourceType() === 'document') return route.continue();
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    await handler(route);
  });
}

test('C07-04.M04.02 sin sesion nunca renderiza estados ni documentos', async ({ page }) => {
  await page.goto('/pedidos-venta');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Pedidos de venta' })).toHaveCount(0);
  await expect(page.locator('.pedidos-readiness-cards li')).toHaveCount(0);
});

test('C07-04.M04.02 permiso Ventas:Ver denegado oculta datos y navegacion', async ({ page }) => {
  await user(page, false);
  await page.goto('/pedidos-venta');
  await expect(page).not.toHaveURL(/\/pedidos-venta(?:\?|$)/);
  await expect(page.locator('.pedidos-readiness-cards li')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Pedidos de venta' })).toHaveCount(0);
});

test('C07-04.M04.02 loading seguido de empty sin acciones financieras', async ({ page }) => {
  await user(page);
  let release!: () => void;
  const pending = new Promise<void>(resolve => { release = resolve; });
  await orders(page, async route => {
    if (route.request().method() !== 'GET') return route.abort();
    await pending;
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json', body: JSON.stringify(result([])) });
  });
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.goto('/pedidos-venta');
  await expect(page.locator('div.center[role="status"]')).toBeVisible();
  release();
  await expect(page.getByText('Sin resultados.')).toBeVisible();
  await expect(page.getByText('0 registro(s)')).toBeVisible();
  expect(writes).toEqual([]);
});

test('C07-04.M04.02 503 es mensaje seguro y retry lleva a exito', async ({ page }) => {
  await user(page);
  let calls = 0;
  await orders(page, async route => {
    if (route.request().method() !== 'GET') return route.abort();
    calls++;
    await route.fulfill({ status: calls === 1 ? 503 : 200, headers: cors,
      contentType: 'application/json', body: calls === 1
        ? JSON.stringify({ success: false, message: 'FINANCIAL_PRIVATE_FAILURE_DETAIL' })
        : JSON.stringify(result([pedido])) });
  });
  await page.goto('/pedidos-venta');
  await expect(page.getByRole('alert')).toContainText('No fue posible cargar los pedidos.');
  await expect(page.getByText('FINANCIAL_PRIVATE_FAILURE_DETAIL')).toHaveCount(0);
  await page.getByRole('button', { name: 'Reintentar' }).click();
  await expect(page.getByText('PED-17')).toBeVisible();
  await expect(page.getByText('1 registro(s)')).toBeVisible();
  expect(calls).toBe(2);
});

test('C07-04.M04.02 lector ve solo consulta autorizada sin crear ni modificar', async ({ page }) => {
  await user(page);
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await orders(page, async route => {
    if (route.request().method() !== 'GET') return route.abort();
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json', body: JSON.stringify(result([pedido])) });
  });
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/pedidos-venta');
    await expect(page.getByRole('heading', { name: 'Pedidos de venta' })).toBeVisible();
    await expect(page.getByText('PED-17')).toBeVisible();
    await expect(page.getByText('Cliente de prueba autorizado')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Nuevo pedido' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Editar pedido' })).toHaveCount(0);
    await expect(page.locator('.pedidos-readiness-cards li')).toHaveCount(4);
    const horizontal = await page.evaluate(() => {
      const overflow = document.documentElement.scrollWidth - document.documentElement.clientWidth;
      const culprits = [...document.querySelectorAll<HTMLElement>('body *')]
        .map(element => ({ element, rect: element.getBoundingClientRect() }))
        .filter(({ rect }) => rect.right > window.innerWidth + 2 && rect.left < window.innerWidth)
        .slice(0, 6)
        .map(({ element, rect }) => `${element.tagName.toLowerCase()}.${element.className?.toString().slice(0, 65)}: right=${Math.round(rect.right)}`);
      return { overflow, culprits };
    });
    expect(horizontal.overflow,
      'overflow horizontal a ' + width + '; responsables=' + horizontal.culprits.join(', '))
      .toBeLessThanOrEqual(2);
  }
  expect(writes).toEqual([]);
});
