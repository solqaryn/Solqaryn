import { expect, test, type Page } from '@playwright/test';

// Las credenciales/tenant son sintéticos: estas pruebas NO acreditan JWT,
// fiscalidad, MySQL, autorización del backend ni aislamiento A/B real.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function contexto(page: Page, permitir = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0704-m0405-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        usuarioId: 7, empresaId: 71, rolId: 3, rolNombre: 'Lectura', esAdministrador: false
      } }) });
  });
  await page.route(/\/permisos\/mis-permisos\/empresa\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        permisos: permitir ? ['Ventas:Ver'] : [],
        capacidadesHabilitadas: permitir ? ['Ventas:Ver'] : [],
        rutasHabilitadas: permitir ? ['pedidos-venta'] : [],
        operacionMultiEmpresaRestringida: true, esAdministrador: false
      } }) });
  });
  await page.route(/\/pedidos-venta(?:\?|$)/, async route => {
    if (route.request().resourceType() === 'document') return route.continue();
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    if (route.request().method() !== 'GET') return route.abort();
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0
      } }) });
  });
}

test('C07-04.M04.05 accesibilidad responsive real 320/768/1280 sin overflow', async ({ page }) => {
  await contexto(page);
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/pedidos-venta');
    const panel = page.locator('.pedidos-readiness-v5');
    await expect(panel).toBeVisible();
    await expect(panel.locator('ol > li')).toHaveCount(4);
    const layout = await page.evaluate(() => ({
      client: document.documentElement.clientWidth,
      scroll: document.documentElement.scrollWidth
    }));
    expect(layout.scroll - layout.client).toBeLessThanOrEqual(2);
  }
});

test('C07-04.M04.05 Tab real accede al filtro sin bloqueo de foco', async ({ page }) => {
  await contexto(page);
  await page.goto('/pedidos-venta');
  const filtro = page.getByRole('combobox', { name: 'Estado' });
  await expect(filtro).toBeVisible();
  let alcanzado = false;
  for (let i = 0; i < 100; i++) {
    await page.keyboard.press('Tab');
    if (await filtro.evaluate(el => el === document.activeElement)) {
      alcanzado = true;
      break;
    }
  }
  expect(alcanzado).toBe(true);
  await expect(filtro).toBeFocused();
  const focusVisible = await filtro.evaluate(el => el.matches(':focus-visible'));
  expect(focusVisible).toBe(true);
});

test('C07-04.M04.05 contraste alto conserva texto y tarjetas no transparentes', async ({ page }) => {
  await contexto(page);
  await page.emulateMedia({ contrast: 'more' });
  await page.setViewportSize({ width: 320, height: 800 });
  await page.goto('/pedidos-venta');
  const panel = page.locator('.pedidos-readiness-v5');
  await expect(panel).toBeVisible();
  const color = await panel.evaluate(el => {
    const s = getComputedStyle(el);
    const card = getComputedStyle(el.querySelector('li')!);
    return { border: s.borderTopStyle, borderWidth: parseFloat(s.borderTopWidth),
      cardBorder: card.borderTopStyle, cardWidth: parseFloat(card.borderTopWidth),
      foreground: s.color, cardForeground: card.color };
  });
  expect(color.border).toBe('solid');
  expect(color.cardBorder).toBe('solid');
  expect(color.borderWidth).toBeGreaterThan(0);
  expect(color.cardWidth).toBeGreaterThan(0);
  expect(color.foreground).not.toBe('rgba(0, 0, 0, 0)');
  expect(color.cardForeground).not.toBe('rgba(0, 0, 0, 0)');
});

test('C07-04.M04.05 permiso denegado y acceso anónimo no revelan criterios ni controles transaccionales', async ({ page }) => {
  await page.goto('/pedidos-venta');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.pedidos-readiness-cards li')).toHaveCount(0);
  await contexto(page, false);
  await page.goto('/pedidos-venta');
  await expect(page).not.toHaveURL(/\/pedidos-venta(?:\?|$)/);
  await expect(page.locator('.pedidos-readiness-cards li')).toHaveCount(0);
  await expect(page.locator('.pedidos-readiness-v5 form, .pedidos-readiness-v5 button')).toHaveCount(0);
});
