import { expect, test, type Page } from '@playwright/test';

const ruta = '/plataforma/eventos-outbox-v22';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function sesionSintetica(page: Page, status: 200 | 403) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1103-m0401-fake-not-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async handler => {
    if (handler.request().method() === 'OPTIONS') {
      await handler.fulfill({status: 204, headers: cors}); return;
    }
    await handler.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? {success: true, data: {nombreUsuario:'Mock', rolNombre:'Plataforma', ambito:'Plataforma', esAdministrador:true}}
        : {success:false,message:'SECRET_INTERNAL_ERROR'})
    });
  });
}

test('C11-03.M04.01 no autenticado y 403 no ven información privada', async ({page}) => {
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c1103-outbox-private')).toHaveCount(0);
  await sesionSintetica(page, 403);
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByText('SECRET_INTERNAL_ERROR')).toHaveCount(0);
});

test('C11-03.M04.01 sesión UI sintética: lectura segura responsive sin mutaciones', async ({page}) => {
  await sesionSintetica(page, 200);
  const writes: string[] = [];
  page.on('request', req => {
    if (!['GET','HEAD','OPTIONS'].includes(req.method())) writes.push(req.method());
  });
  await page.goto(ruta);
  const panel = page.getByTestId('c1103-outbox-private');
  await expect(panel).toBeVisible();
  await expect(panel.locator('[data-testid^="c1103-outbox-step-"]')).toHaveCount(3);
  await expect(panel.getByText('SIN_FUENTE_CONECTADA')).toHaveCount(4);
  await expect(panel.locator('form,button,input,textarea,select')).toHaveCount(0);
  for (const width of [320,375,768,1280]) {
    await page.setViewportSize({width,height:800});
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow,'horizontal overflow at '+width).toBeLessThanOrEqual(2);
  }
  expect(writes).toEqual([]);
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link',{name:'Volver a plataforma'})).toBeFocused();
});
