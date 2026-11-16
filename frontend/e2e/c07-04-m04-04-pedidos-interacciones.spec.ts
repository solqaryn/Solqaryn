import { expect, test, type Page, type Route } from '@playwright/test';

// Browser real, tenant/rol/token/HTTP sinteticos: no JWT ni efectos financieros probados.
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const order = {
  id: 17, cotizacionId: 4, clienteId: 7,
  clienteNombreSnapshot: 'Lector autorizado', estado: 1, total: 45.5, detalles: []
};
const paged = (items: unknown[], total = items.length) => ({
  success: true, data: { items, page: 1, pageSize: 10, totalCount: total, totalPages: Math.ceil(total / 10) }
});

async function reader(page: Page, allowed = true): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await route.fulfill({ status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        usuarioId: 7, empresaId: 71, rolId: 3, rolNombre: 'Lector', esAdministrador: false
      } }) });
  });
  await page.route(/\/permisos\/mis-permisos\/empresa\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await route.fulfill({ status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        permisos: allowed ? ['Ventas:Ver'] : [],
        capacidadesHabilitadas: allowed ? ['Ventas:Ver'] : [],
        rutasHabilitadas: allowed ? ['pedidos-venta'] : [],
        operacionMultiEmpresaRestringida: true, esAdministrador: false
      } }) });
  });
}
async function orders(page: Page, get: (route: Route) => Promise<void>): Promise<void> {
  await page.route(/\/pedidos-venta(?:\?|$)/, async route => {
    if (route.request().resourceType() === 'document') return route.continue();
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    if (route.request().method() !== 'GET') return route.abort();
    await get(route);
  });
}

test('C07-04.M04.04 anónimo no ve formularios ni criterios', async ({ page }) => {
  await page.goto('/pedidos-venta');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.pedidos-readiness-cards li')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Pedidos de venta' })).toHaveCount(0);
});

test('C07-04.M04.04 permiso Ventas:Ver denegado cierra ruta', async ({ page }) => {
  await reader(page, false);
  await page.goto('/pedidos-venta');
  await expect(page).not.toHaveURL(/\/pedidos-venta(?:\?|$)/);
  await expect(page.locator('.pedidos-readiness-cards li')).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Nuevo pedido' })).toHaveCount(0);
});

test('C07-04.M04.04 lector navega detalle sin acciones ni HTTP mutador', async ({ page }) => {
  await reader(page);
  const writes: string[] = [];
  page.on('request', req => { if (!['GET', 'HEAD', 'OPTIONS'].includes(req.method())) writes.push(req.method()); });
  await orders(page, async route => {
    await route.fulfill({ status: 200, headers, contentType: 'application/json', body: JSON.stringify(paged([order])) });
  });
  await page.route(/\/pedidos-venta\/17(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    if (route.request().method() !== 'GET') return route.abort();
    await route.fulfill({ status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: order }) });
  });
  await page.goto('/pedidos-venta');
  await expect(page.getByText('PED-17')).toBeVisible();
  await expect(page.getByRole('link', { name: 'Nuevo pedido' })).toHaveCount(0);
  await page.getByRole('link', { name: 'Ver pedido' }).click();
  await expect(page.getByRole('heading', { name: 'Pedido PED-17' })).toBeVisible();
  await expect(page.getByRole('button', { name: /Confirmar y reservar|Anular/ })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Editar' })).toHaveCount(0);
  await expect(page.locator('form')).toHaveCount(0);
  await page.getByRole('link', { name: 'Volver' }).click();
  await expect(page.getByRole('heading', { name: 'Pedidos de venta' })).toBeVisible();
  expect(writes).toEqual([]);
});

test('C07-04.M04.04 503 saneado y retry single-flight sin doble GET', async ({ page }) => {
  await reader(page);
  let calls = 0;
  let release!: () => void;
  const held = new Promise<void>(resolve => { release = resolve; });
  await orders(page, async route => {
    calls++;
    if (calls === 1) {
      await route.fulfill({ status: 503, headers, contentType: 'application/json',
        body: JSON.stringify({ success: false, message: 'PRIVATE_FISCAL_DETAIL' }) });
    } else {
      await held;
      await route.fulfill({ status: 200, headers, contentType: 'application/json',
        body: JSON.stringify(paged([order])) });
    }
  });
  await page.goto('/pedidos-venta');
  await expect(page.getByRole('alert')).toContainText('No fue posible cargar los pedidos.');
  await expect(page.getByText('PRIVATE_FISCAL_DETAIL')).toHaveCount(0);
  await page.getByRole('button', { name: 'Reintentar' }).click();
  try {
    await expect(page.locator('div.center[role="status"]')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reintentar' })).toHaveCount(0);
    await expect(page.getByRole('combobox', { name: 'Estado' })).toBeDisabled();
    expect(calls).toBe(2);
  } finally { release(); }
  await expect(page.getByText('PED-17')).toBeVisible();
  expect(calls).toBe(2);
});

test('C07-04.M04.04 paginación bloquea reentrada durante GET', async ({ page }) => {
  await reader(page);
  let calls = 0;
  let release!: () => void;
  const held = new Promise<void>(resolve => { release = resolve; });
  await orders(page, async route => {
    calls++;
    if (calls === 2) await held;
    await route.fulfill({ status: 200, headers, contentType: 'application/json',
      body: JSON.stringify(paged([order], 25)) });
  });
  await page.goto('/pedidos-venta');
  await expect(page.getByText('25 registro(s)')).toBeVisible();
  await page.getByRole('button', { name: 'Siguiente' }).click();
  try {
    await expect(page.locator('div.center[role="status"]')).toBeVisible();
    await expect(page.getByRole('combobox', { name: 'Estado' })).toBeDisabled();
    expect(calls).toBe(2);
  } finally { release(); }
  await expect(page.getByText('Página 2')).toBeVisible();
  expect(calls).toBe(2);
});
