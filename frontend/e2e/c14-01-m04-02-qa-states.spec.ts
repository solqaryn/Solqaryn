import { expect, test, type Page } from '@playwright/test';

const path = '/plataforma/qa-global-v19';
async function grant(page: Page, status = 200) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: {
        'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
        'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
        'Access-Control-Allow-Methods': 'GET, OPTIONS'
      } }); return;
    }
    await route.fulfill({status,contentType:'application/json',headers:{'Access-Control-Allow-Origin':'http://127.0.0.1:4200'},
      body: JSON.stringify(status===200 ? {success:true,data:{nombreUsuario:'Synthetic',rolNombre:'Plataforma',ambito:'Plataforma',esAdministrador:true}} : {success:false,message:'SECRET_TENANT_B_PRIVATE_DSN'})});
  });
}
test.describe('C14-01.M04.02 - Chromium estados y permisos', () => {
  test.describe.configure({ retries: 0 });
  test('ruta privada deniega anonimo y 403 sin exponer estado interno', async ({page}) => {
    await page.goto(path);
    await expect(page).toHaveURL(/\/login/);
    await grant(page,403);
    await page.goto(path);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c1401-qa-ready')).toHaveCount(0);
    await expect(page.getByText('SECRET_TENANT_B_PRIVATE_DSN')).toHaveCount(0);
  });
  test('inventario de cinco categorias solo NO_VERIFICADO sin requests mutantes', async ({page}) => {
    await grant(page);
    const writes: string[]=[];
    page.on('request',req=>{if(!['GET','HEAD','OPTIONS'].includes(req.method())) writes.push(req.method());});
    await page.goto(path);
    await expect(page.getByTestId('c1401-qa-ready')).toBeVisible();
    await expect(page.locator('.qa-controles li')).toHaveCount(5);
    await expect(page.locator('.pendiente')).toHaveCount(5);
    await expect(page.locator('[aria-busy="true"]')).toHaveCount(0);
    await expect(page.getByTestId('c1401-qa-global').locator('button,form,input,textarea,select')).toHaveCount(0);
    expect(writes).toEqual([]);
  });
});
