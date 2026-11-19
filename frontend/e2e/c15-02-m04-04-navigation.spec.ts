import { expect, test, type Page } from '@playwright/test';

const ruta = '/plataforma/revision-arquitectura-v20';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function autenticar(page: Page, estado: 'plataforma' | 'empresa' | '403') {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1502m0404-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async handler => {
    if (handler.request().method() === 'OPTIONS') return handler.fulfill({ status: 204, headers: cors });
    await handler.fulfill({
      status: estado === '403' ? 403 : 200,
      headers: cors, contentType: 'application/json',
      body: JSON.stringify(estado === '403'
        ? { success: false, message: 'C1502_PRIVATE_PROVIDER_KEY_NEVER_RENDER' }
        : { success: true, data: {
          nombreUsuario: 'Admin', rolNombre: 'Plataforma',
          ambito: estado === 'empresa' ? 'Empresa' : 'Plataforma',
          esAdministrador: true
        } })
    });
  });
}

test('C15-02.M04.04 anónimo no recibe inventario', async ({ page }) => {
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('architecture-item')).toHaveCount(0);
});

test('C15-02.M04.04 403 y rol Empresa no filtran datos ni mensaje privado', async ({ page }) => {
  for (const estado of ['403', 'empresa'] as const) {
    await autenticar(page, estado);
    await page.goto(ruta);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('architecture-item')).toHaveCount(0);
    await expect(page.getByText('C1502_PRIVATE_PROVIDER_KEY_NEVER_RENDER')).toHaveCount(0);
  }
});

test('C15-02.M04.04 Tab y Enter reales navegan con foco visible', async ({ page }) => {
  await autenticar(page, 'plataforma');
  await page.goto(ruta);
  const back = page.getByRole('link', { name: 'Volver a plataforma' });
  await expect(back).toBeVisible();
  let alcanzado = false;
  for (let n = 0; n < 80; n++) {
    await page.keyboard.press('Tab');
    if (await back.evaluate(el => el.ownerDocument.activeElement === el)) {
      alcanzado = true;
      break;
    }
  }
  expect(alcanzado, 'Tab real debe alcanzar enlace').toBe(true);
  await expect(back).toBeFocused();
  expect(await back.evaluate(el => getComputedStyle(el).outlineStyle)).not.toBe('none');
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma(?:\/|$)/);
});

test('C15-02.M04.04 responsive y doble pulsación informativa no ejecutan escrituras', async ({ page }) => {
  await autenticar(page, 'plataforma');
  const writes: string[] = [];
  page.on('request', req => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(req.method())) writes.push(req.method());
  });
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto(ruta);
    const vista = page.getByTestId('c1502-review');
    await expect(vista.getByTestId('architecture-item')).toHaveCount(5);
    await expect(vista.locator('form,input,textarea,select,button')).toHaveCount(0);
    expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
    expect((await vista.getByRole('link', { name: 'Volver a plataforma' }).boundingBox())?.height ?? 0).toBeGreaterThanOrEqual(44);
    await vista.getByText('NO_VERIFICADO').first().click();
    await vista.getByText('NO_VERIFICADO').first().click();
    await expect(vista.getByTestId('architecture-item')).toHaveCount(5);
  }
  expect(writes, 'Sin POST/PUT/PATCH/DELETE ni doble acción').toEqual([]);
});
