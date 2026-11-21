import { expect, test, type Page } from '@playwright/test';

// PLAN D605: positive acceptance of the existing private, NON-COMMERCIAL reference.
// Browser is real Chromium; the session below is a synthetic fixture, NOT signed JWT.
const routePath = '/plataforma/catalogo-planes-v22';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function platformSession(page: Page, authorized: () => boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'TEST_FIXTURE_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    const allowed = authorized();
    await route.fulfill({
      status: allowed ? 200 : 403,
      headers: cors,
      contentType: 'application/json',
      body: JSON.stringify(allowed
        ? { success: true, data: { nombreUsuario: 'Referencia', rolNombre: 'Plataforma',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'No autorizado' })
    });
  });
}

test('C04-01.M05.04 aceptación positiva: referencia exacta y nunca tarifa activada', async ({ page }) => {
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await platformSession(page, () => true);
  await page.goto(routePath);
  await expect(page.getByRole('heading', { name: 'Catálogo de planes', level: 1 })).toBeVisible();
  const privatePanel = page.getByTestId('c0401-private');
  await expect(privatePanel).toBeVisible();
  for (const [key, name] of [
    ['versiones', 'Versiones y vigencia'],
    ['capacidades', 'Capacidades y límites'],
    ['transiciones', 'Cambios y conservación']
  ]) {
    await expect(privatePanel.getByTestId('c0401-section-' + key).getByRole('heading', { name })).toBeVisible();
  }
  await expect(privatePanel.locator('[data-testid^="c0401-section-"]')).toHaveCount(3);
  await expect(privatePanel.getByTestId('c0401-unapproved')).toContainText('NO_CERTIFICADO');
  await expect(privatePanel.locator('button,form,input,select,textarea,iframe,[data-cobro],[data-precio]')).toHaveCount(0);
  expect(writes, 'Referencia no puede cobrar, activar plan o escribir datos').toEqual([]);
});

test('C04-01.M05.04 teclado real: activa regreso seguro sin operación comercial', async ({ page }) => {
  await platformSession(page, () => true);
  await page.goto(routePath);
  await expect(page.getByTestId('c0401-private')).toBeVisible();
  const back = page.getByRole('link', { name: 'Volver a plataforma' });
  await page.keyboard.press('Tab');
  await expect(back).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma(?:\?|$)/);
});

test('C04-01.M05.04 frontera de aceptación: acceso válido se revoca en nueva lectura', async ({ page }) => {
  let allowed = true;
  await platformSession(page, () => allowed);
  await page.goto(routePath);
  await expect(page.getByTestId('c0401-private')).toBeVisible();
  allowed = false;
  await page.reload();
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByTestId('c0401-private')).toHaveCount(0);
  await expect(page.getByText('NO_CERTIFICADO')).toHaveCount(0);
});

test('C04-01.M05.04 aceptación responsive en Chromium: referencia sin overflow ni acciones', async ({ page }) => {
  await platformSession(page, () => true);
  await page.goto(routePath);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(page.getByTestId('c0401-private')).toBeVisible();
    await expect(page.getByTestId('c0401-unapproved')).toContainText('NO_CERTIFICADO');
    const overshoot = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overshoot, 'No horizontal scroll on width ' + width).toBeLessThanOrEqual(2);
    await expect(page.locator('main[data-testid="c0401-planes-reference"] button')).toHaveCount(0);
  }
});
