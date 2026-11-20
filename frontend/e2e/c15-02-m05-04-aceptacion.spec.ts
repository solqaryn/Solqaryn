import { expect, test, type Page } from '@playwright/test';

const route = '/plataforma/revision-arquitectura-v20';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function allowedPlatformSession(page: Page): Promise<void> {
  // Solo fixture positiva de UI; nunca se presenta como JWT real del backend.
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1502m0504-ui-only-not-a-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async handler => {
    if (handler.request().method() === 'OPTIONS') {
      await handler.fulfill({ status: 204, headers: cors });
      return;
    }
    await handler.fulfill({
      status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: { nombreUsuario: 'Auditor', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true }
      })
    });
  });
}

test('C15-02.M05.04 acepta recorrido positivo completo sin certificar proveedores', async ({ page }) => {
  await allowedPlatformSession(page);
  const mutations: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) mutations.push(request.method());
  });
  await page.setViewportSize({ width: 375, height: 820 });
  await page.goto(route);
  const view = page.getByTestId('c1502-review');
  await expect(view).toBeVisible();
  await expect(view.getByRole('heading', { name: 'Revisión de arquitectura e integraciones' })).toBeVisible();
  await expect(view.getByRole('heading', { name: 'Superficies por contrastar' })).toBeVisible();
  await expect(view.getByTestId('architecture-item')).toHaveCount(5);
  for (const name of ['Hosting y ejecución', 'Cloudflare y DNS', 'R2 y archivos', 'MySQL y datos', 'Proveedores e integración']) {
    await expect(view.getByRole('heading', { name, exact: true })).toBeVisible();
  }
  await expect(view.getByText('NO_VERIFICADO')).toHaveCount(5);
  await expect(view.getByText('Ningún cambio de DNS', { exact: false })).toBeVisible();
  await expect(view.getByTestId('architecture-denied')).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Volver a plataforma' })).toHaveAttribute('href', '/plataforma');
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow, 'sin desborde horizontal móvil').toBeLessThanOrEqual(2);
  expect(mutations, 'ninguna petición mutante durante consulta').toEqual([]);
});

test('C15-02.M05.04 conserva pantalla positiva tras recarga y permite teclado real', async ({ page }) => {
  await allowedPlatformSession(page);
  await page.goto(route);
  await expect(page.getByTestId('architecture-item')).toHaveCount(5);
  await page.reload();
  await expect(page.getByTestId('architecture-item')).toHaveCount(5);
  const back = page.getByRole('link', { name: 'Volver a plataforma' });
  let focused = false;
  for (let attempt = 0; attempt < 15; attempt++) {
    await page.keyboard.press('Tab');
    focused = await back.evaluate(element => element === document.activeElement);
    if (focused) break;
  }
  expect(focused, 'alcanzar enlace usando Tab físico').toBe(true);
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma(?:\?|$)/);
});
