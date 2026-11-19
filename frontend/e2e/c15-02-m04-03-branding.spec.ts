import { expect, test, type Page } from '@playwright/test';
const path = '/plataforma/revision-arquitectura-v20';
const headers = { 'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS' };
async function session(page: Page, status: 200 | 403) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1502-m0403-test-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    await route.fulfill({ status, headers, contentType: 'application/json',
      body: JSON.stringify(status === 200 ? { success: true,
        data: { nombreUsuario: 'Admin', rolNombre: 'Plataforma',
          ambito: 'Plataforma', esAdministrador: true }} :
        { success: false, message: 'PRIVATE_CONFIG_NEVER_RENDER' }) });
  });
}
test('C15-02.M04.03 vista institucional default, 320/768 y sin HTTP mutador', async ({ page }) => {
  await session(page, 200);
  const writes: string[] = [];
  page.on('request', r => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  for (const width of [320, 768]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto(path);
    const review = page.getByTestId('c1502-review');
    await expect(review).toHaveAttribute('data-marca', 'institucional');
    await expect(review).toHaveAttribute('data-densidad', 'regular');
    await expect(review.getByTestId('architecture-item')).toHaveCount(5);
    await expect(review.getByText('NO_VERIFICADO')).toHaveCount(5);
    await expect(review.locator('form,input,select,textarea')).toHaveCount(0);
    expect(await page.evaluate(() => document.documentElement.scrollWidth -
      document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  }
  expect(writes).toEqual([]);
});
test('C15-02.M04.03 403 oculta cinco tarjetas sin filtrar proveedor', async ({ page }) => {
  await session(page, 403);
  await page.goto(path);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('architecture-item')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_CONFIG_NEVER_RENDER')).toHaveCount(0);
});
