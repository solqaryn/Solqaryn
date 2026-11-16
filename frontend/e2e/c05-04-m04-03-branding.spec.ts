import { expect, test, type Page } from '@playwright/test';
// Fixtures HTTP sintéticas: no certifican JWT, backend ni MySQL reales.
async function sesion(page: Page, allowed = true, calls?: { n: number }) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0504-m0403-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  const headers = {
    'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
    'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
    'Access-Control-Allow-Methods': 'GET, OPTIONS'
  };
  await page.route('**/plataforma/sesion**', async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    return route.fulfill({
      status: allowed ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allowed
        ? { success: true, data: { nombreUsuario: 'Demo', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'SECRET_NEVER_TO_RENDER' })
    });
  });
  await page.route('**/plataforma/empresas**', async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    if (calls) calls.n++;
    return route.fulfill({ status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: { items: [], pagina: 1, tamano: 25, total: 0, puedeCrear: false } }) });
  });
}
async function abrir(page: Page) {
  const summary = page.locator('details.ui-lightness-v5 > summary');
  await expect(summary).toBeVisible();
  await summary.click();
  await expect(page.getByTestId('c0504-branding')).toBeVisible();
}
test('C05-04.M04.03 anónimo no accede a branding privado', async ({ page }) => {
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByTestId('c0504-branding')).toHaveCount(0);
});
test('C05-04.M04.03 403 no filtra mensaje ni control', async ({ page }) => {
  await sesion(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c0504-branding')).toHaveCount(0);
  await expect(page.getByText('SECRET_NEVER_TO_RENDER')).toHaveCount(0);
});
test('C05-04.M04.03 sólo dos marcas y cero HTTP adicional', async ({ page }) => {
  const calls = { n: 0 };
  await sesion(page, true, calls);
  await page.goto('/plataforma');
  await abrir(page);
  const group = page.getByTestId('c0504-branding');
  const normal = page.getByTestId('c0504-brand-institucional');
  const high = page.getByTestId('c0504-brand-contraste');
  await expect(group.locator('button')).toHaveCount(2);
  await expect(normal).toHaveAttribute('aria-pressed', 'true');
  const before = calls.n;
  await high.click();
  await expect(group).toHaveClass(/ui-lightness-v5__branding--contrast/);
  await expect(high).toHaveAttribute('aria-pressed', 'true');
  await high.click();
  await expect(high).toHaveAttribute('aria-pressed', 'true');
  await normal.click();
  await expect(group).not.toHaveClass(/ui-lightness-v5__branding--contrast/);
  expect(calls.n).toBe(before);
});
test('C05-04.M04.03 320/375/768/1280 y activación teclado', async ({ page }) => {
  await sesion(page);
  await page.goto('/plataforma');
  await abrir(page);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    const group = page.getByTestId('c0504-branding');
    await expect(group).toBeVisible();
    expect(await group.evaluate(el => el.scrollWidth - el.clientWidth)).toBeLessThanOrEqual(2);
  }
  const btn = page.getByTestId('c0504-brand-contraste');
  await btn.focus();
  await expect(btn).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(btn).toHaveAttribute('aria-pressed', 'true');
});
