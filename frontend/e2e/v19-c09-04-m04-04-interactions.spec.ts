import { expect, test, type Page } from '@playwright/test';

/** C09-04.M04.04: browser real; only /plataforma/sesion is synthetic, NOT JWT or provider integration. */
const ruta = '/plataforma/mensajeria-multicanal-v19';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function prepararSesion(page: Page, status = 200, data?: object) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'm0404-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    await route.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: data ?? {
            nombreUsuario: 'Admin sintetico', rolNombre: 'Admin',
            ambito: 'Plataforma', esAdministrador: true
          } }
        : { success: false, message: 'SECRET_TENANT_B_NO_LEAK' })
    });
  });
}
test.describe('C09-04.M04.04 interacciones fail-closed Chromium', () => {
  test.describe.configure({ retries: 0 });
  test('anonimo y 403 no montan la pantalla ni filtran secretos', async ({ page }) => {
    await page.goto(ruta);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c0904-canales-privados')).toHaveCount(0);
    await prepararSesion(page, 403);
    await page.goto(ruta);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c0904-canales-privados')).toHaveCount(0);
    await expect(page.getByText('SECRET_TENANT_B_NO_LEAK')).toHaveCount(0);
  });
  test('empresa y no admin rechazados aunque exista token sintetico', async ({ browser }) => {
    for (const data of [
      { nombreUsuario: 'A', rolNombre: 'Admin', ambito: 'Empresa', esAdministrador: true },
      { nombreUsuario: 'B', rolNombre: 'Operador', ambito: 'Plataforma', esAdministrador: false }
    ]) {
      const page = await browser.newPage();
      await prepararSesion(page, 200, data);
      await page.goto(ruta);
      await expect(page).toHaveURL(/\/login/);
      await expect(page.getByTestId('c0904-canales-privados')).toHaveCount(0);
      await page.close();
    }
  });
  test('admin solo lectura: tres canales sin controles mutantes ni solicitudes POST', async ({ page }) => {
    await prepararSesion(page);
    const writes: string[] = [];
    page.on('request', r => { if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method()); });
    await page.goto(ruta);
    const main = page.locator('main[aria-labelledby="c0904-titulo"]');
    await expect(main).toBeVisible();
    await expect(page.getByTestId('c0904-canales-privados').locator('li')).toHaveCount(3);
    await expect(main.getByText('NO OPERATIVO', { exact: false })).toBeVisible();
    await expect(main.locator('form,input,textarea,button,select')).toHaveCount(0);
    expect(writes).toEqual([]);
  });
  test('navegacion Tab y Enter real, repeticion sin doble POST, 320 a 1280', async ({ page }) => {
    await prepararSesion(page);
    const writes: string[] = [];
    page.on('request', r => { if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method()); });
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 812 });
      await page.goto(ruta);
      await expect(page.getByTestId('c0904-canales-privados')).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
    }
    const volver = page.getByRole('link', { name: 'Regresar a plataforma' });
    let alcanzado = false;
    for (let i = 0; i < 80; i++) {
      await page.keyboard.press('Tab');
      if (await volver.evaluate(el => document.activeElement === el)) { alcanzado = true; break; }
    }
    expect(alcanzado).toBe(true);
    await expect(volver).toHaveCSS('outline-style', 'solid');
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/plataforma(?:\?.*)?$/);
    await page.keyboard.press('Enter');
    expect(writes).toEqual([]);
  });
});
