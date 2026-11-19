import { expect, test, type Page } from '@playwright/test';

const route = '/plataforma/revision-arquitectura-v20';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function syntheticSession(page: Page, status: 200 | 403) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1502m0401-fake-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async handler => {
    if (handler.request().method() === 'OPTIONS') {
      await handler.fulfill({ status: 204, headers: cors }); return;
    }
    await handler.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'PRIVATE_DSN_SHOULD_NOT_RENDER' }) });
  });
}

test('C15-02.M04.01 anónimo y 403 nunca ven inventario privado', async ({ page }) => {
  await page.goto(route);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('architecture-item')).toHaveCount(0);
  await syntheticSession(page, 403);
  await page.goto(route);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByText('PRIVATE_DSN_SHOULD_NOT_RENDER')).toHaveCount(0);
});

test('C15-02.M04.01 sesión sintética: 5 pendientes, 4 anchos y cero mutaciones', async ({ page }) => {
  await syntheticSession(page, 200);
  const writes: string[] = [];
  page.on('request', r => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await page.goto(route);
  const view = page.getByTestId('c1502-review');
  await expect(view.getByRole('heading', { name: 'Revisión de arquitectura e integraciones' })).toBeVisible();
  await expect(view.getByTestId('architecture-item')).toHaveCount(5);
  await expect(view.getByText('NO_VERIFICADO')).toHaveCount(5);
  await expect(view.locator('button,form,input,select,textarea')).toHaveCount(0);
  for (const width of [320,375,768,1280]) {
    await page.setViewportSize({ width, height: 800 });
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'scroll horizontal ' + width).toBeLessThanOrEqual(2);
  }
  expect(writes).toEqual([]);
  await view.getByRole('link', { name: 'Volver a plataforma' }).click();
  await expect(page).toHaveURL(/\/plataforma$/);
});
