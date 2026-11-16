import { expect, test, type Page } from '@playwright/test';

/** Synthetic HTTP fixtures exercise UI states only; they do not certify real JWT or MySQL. */
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const payload = (items: unknown[]) => ({
  success: true,
  data: { items, pagina: 1, tamano: 25, total: items.length, puedeCrear: false }
});
const empresa = { id: 101, nombre: 'Empresa de prueba', activa: true, usuariosActivos: 2 };

async function sesion(page: Page, autorizado = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0504-m0402-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    return route.fulfill({
      status: autorizado ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(autorizado
        ? { success: true, data: { nombreUsuario: 'Prueba', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'NO_EXPOSE_PRIVATE_SESSION_MESSAGE' })
    });
  });
}

async function directorio(page: Page, handler: (route: import('@playwright/test').Route) => Promise<void>) {
  await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await handler(route);
  });
}

test('C05-04.M04.02 anónimo: guard deniega estados privados', async ({ page }) => {
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByTestId('c0504-feedback')).toHaveCount(0);
});

test('C05-04.M04.02 autoridad 403: no filtra error ni datos de plataforma', async ({ page }) => {
  await sesion(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.getByText('NO_EXPOSE_PRIVATE_SESSION_MESSAGE')).toHaveCount(0);
  await expect(page.getByTestId('c0504-feedback')).toHaveCount(0);
});

test('C05-04.M04.02 estado carga y transición a vacío sin escritura HTTP', async ({ page }) => {
  await sesion(page);
  let liberar!: () => void;
  const espera = new Promise<void>(resolve => { liberar = resolve; });
  await directorio(page, async route => {
    await espera;
    await route.fulfill({ status: 200, headers, contentType: 'application/json', body: JSON.stringify(payload([])) });
  });
  const mutaciones: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) mutaciones.push(request.method());
  });
  await page.goto('/plataforma');
  await page.locator('.ui-lightness-v5 summary').click();
  await expect(page.getByTestId('c0504-loading')).toBeVisible();
  liberar();
  await expect(page.getByTestId('c0504-empty')).toBeVisible();
  expect(mutaciones).toEqual([]);
});

test('C05-04.M04.02 éxito muestra solo conteo y respeta ancho 320/1280', async ({ page }) => {
  await sesion(page);
  await directorio(page, async route => {
    await route.fulfill({ status: 200, headers, contentType: 'application/json', body: JSON.stringify(payload([empresa])) });
  });
  await page.goto('/plataforma');
  await page.locator('.ui-lightness-v5 summary').click();
  await expect(page.getByTestId('c0504-success')).toContainText('1 empresas');
  await expect(page.getByTestId('c0504-feedback')).not.toContainText('Empresa de prueba');
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'horizontal overflow at ' + width).toBeLessThanOrEqual(2);
  }
});

test('C05-04.M04.02 error 503 recuperable no muestra mensaje privado y reintenta una vez', async ({ page }) => {
  await sesion(page);
  let peticiones = 0;
  await directorio(page, async route => {
    peticiones++;
    await route.fulfill({
      status: peticiones === 1 ? 503 : 200, headers, contentType: 'application/json',
      body: peticiones === 1
        ? JSON.stringify({ success: false, message: 'SECRETO_BACKEND_NO_MOSTRAR' })
        : JSON.stringify(payload([empresa]))
    });
  });
  await page.goto('/plataforma');
  await page.locator('.ui-lightness-v5 summary').click();
  await expect(page.getByTestId('c0504-error')).toBeVisible();
  await expect(page.getByText('SECRETO_BACKEND_NO_MOSTRAR')).toHaveCount(0);
  await page.getByTestId('c0504-feedback').getByRole('button', { name: 'Reintentar directorio' }).click();
  await expect(page.getByTestId('c0504-success')).toBeVisible();
  expect(peticiones).toBe(2);
});

test('C05-04.M04.02 permiso 403 en directorio deniega datos y no ofrece reintento ciego', async ({ page }) => {
  await sesion(page);
  await directorio(page, async route => {
    await route.fulfill({ status: 403, headers, contentType: 'application/json',
      body: JSON.stringify({ success: false, message: 'NO_EXPOSE_TENANT_DETAILS' }) });
  });
  await page.goto('/plataforma');
  // Playwright auto-wait: evita evaluar count() antes de obtener la sesión.
  await page.locator('.ui-lightness-v5 summary').click();
  const panel = page.getByTestId('c0504-feedback');
  await expect(page.getByTestId('c0504-permission')).toBeVisible();
  await expect(panel.getByRole('button', { name: 'Reintentar directorio' })).toHaveCount(0);
  await expect(page.getByText('NO_EXPOSE_TENANT_DETAILS')).toHaveCount(0);
});
