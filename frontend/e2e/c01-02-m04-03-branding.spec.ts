import { expect, test, type Page } from '@playwright/test';
const url = '/plataforma/capacidades-branding-v19';
async function fixture(page: Page, status: 200 | 403) {
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
      }}); return;
    }
    await route.fulfill({ status, contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': 'http://127.0.0.1:4200' },
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Synthetic QA', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'TENANT_B_PRIVATE_NOT_FOR_DISPLAY' })
    });
  });
}
test.describe('C01-02.M04.03 - branding fail closed browser', () => {
  test.describe.configure({ retries: 0 });
  test('anonimo y sesion 403 no reciben marca privada', async ({ page }) => {
    await page.goto(url);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await fixture(page, 403);
    await page.goto(url);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('.muestra, [data-testid="marca-rutas"]')).toHaveCount(0);
    await expect(page.getByText('TENANT_B_PRIVATE_NOT_FOR_DISPLAY')).toHaveCount(0);
  });
  test('sesion global sintetica sin tenant: no filtra datos ni escribe, responsive 320px', async ({ page }) => {
    await fixture(page, 200);
    await page.setViewportSize({ width: 320, height: 740 });
    const writes: string[] = [];
    page.on('request', request => {
      if (page.url().includes(url) && !['GET', 'HEAD', 'OPTIONS'].includes(request.method()))
        writes.push(request.method() + ' ' + request.url());
    });
    await page.goto(url);
    const main = page.locator('main.marca-capacidades');
    await expect(main.getByRole('heading', { name: 'Branding y capacidades permitidas' })).toBeVisible();
    await expect(main.locator('[data-testid="marca-sin-tenant"]')).toBeVisible();
    await expect(main.locator('.muestra,li,form,button')).toHaveCount(0);
    expect(await main.evaluate(el => el.scrollWidth > el.clientWidth + 1)).toBe(false);
    expect(writes).toEqual([]);
  });
});
