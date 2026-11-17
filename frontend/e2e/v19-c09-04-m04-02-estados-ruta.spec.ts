import { expect, test, type Page } from '@playwright/test';

// Ruta real Angular bajo plataformaGuard. Solo /plataforma/sesion es SIMULADA:
// NO acredita JWT firmado, backend alojado, tenant DB ni proveedores.
const ruta = '/plataforma/mensajeria-multicanal-v19/estados';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function prepararSesion(page: Page, status = 200, data?: object) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0904m0402-synthetic-not-jwt');
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
          nombreUsuario: 'Sintetico', rolNombre: 'Administrador',
          ambito: 'Plataforma', esAdministrador: true
        } }
        : { success: false, message: 'SECRET_TENANT_B_NO_LEAK' })
    });
  });
}

test('anonimo y respuesta 403 no pueden montar el estado privado', async ({ page }) => {
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('m0402-vacio')).toHaveCount(0);
  await prepararSesion(page, 403);
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('m0402-vacio')).toHaveCount(0);
  await expect(page.getByText('SECRET_TENANT_B_NO_LEAK')).toHaveCount(0);
});

test('Empresa y plataforma no admin sin acceso, incluso con token sintético', async ({ browser }) => {
  for (const data of [
    { nombreUsuario: 'A', rolNombre: 'Admin', ambito: 'Empresa', esAdministrador: true },
    { nombreUsuario: 'B', rolNombre: 'Operador', ambito: 'Plataforma', esAdministrador: false }
  ]) {
    const page = await browser.newPage();
    await prepararSesion(page, 200, data);
    await page.goto(ruta);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('m0402-vacio')).toHaveCount(0);
    await page.close();
  }
});

test('administrador sintético navega ruta real y ve vacío sin envíos ni mutaciones', async ({ page }) => {
  await prepararSesion(page);
  const writes: string[] = [];
  page.on('request', r => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/plataforma\/mensajeria-multicanal-v19\/estados$/);
  await expect(page.getByTestId('m0402-vacio')).toBeVisible();
  await expect(page.getByTestId('m0402-exito')).toHaveCount(0);
  await expect(page.locator('.estado-mensajeria')).toHaveAttribute('aria-busy', 'false');
  await expect(page.locator('form,input,button,textarea')).toHaveCount(0);
  expect(writes).toEqual([]);
});

test('ruta real responsive a 320 y 1280 px sin overflow ni secretos', async ({ page }) => {
  await prepararSesion(page);
  for (const width of [320, 1280]) {
    await page.setViewportSize({ width, height: 812 });
    await page.goto(ruta);
    await expect(page.getByTestId('m0402-vacio')).toBeVisible();
    expect(await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth
    )).toBeLessThanOrEqual(2);
  }
  await expect(page.getByText('SECRET_TENANT_B_NO_LEAK')).toHaveCount(0);
});
