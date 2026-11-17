import { expect, test, type Page } from '@playwright/test';

// C12-04.M05.04: Angular + Chromium reales con GET de sesión simulado; no MySQL/JWT/restore.
const ruta = '/plataforma/mysql-cutover-readonly-v5';
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function preparar(page: Page, estadoPosterior = 200) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1204-m0504-ficticio-no-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let gets = 0;
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    gets++;
    const status = gets === 1 ? 200 : estadoPosterior;
    await route.fulfill({
      status, headers, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Global QA', rolNombre: 'Administrador', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'PRIVATE_MYSQL_DSN_DO_NOT_RENDER' })
    });
  });
  return { gets: () => gets, writes };
}
test('M05.04 aceptación inicial: cuatro límites no operativos y cero escrituras', async ({ page }) => {
  const state = await preparar(page);
  await page.goto(ruta);
  await expect(page.getByRole('heading', { name: 'Controles de preparación para cutover' })).toBeVisible();
  const controles = page.locator('[data-testid=c1204-cutover-private] .controles li');
  await expect(controles).toHaveCount(4);
  await expect(controles.nth(0)).toContainText('Exportación aislada');
  await expect(controles.nth(1)).toContainText('Restauración de ensayo');
  await expect(controles.nth(2)).toContainText('NO_MEDIDO');
  await expect(controles.nth(3)).toContainText('NO_AUTORIZADO');
  await expect(page.getByText(/Sin acceso a MySQL ni ejecución de migraciones/)).toBeVisible();
  await expect(page.locator('.cutover form, .cutover input, .cutover select')).toHaveCount(0);
  expect(state.gets()).toBe(1);
  expect(state.writes).toEqual([]);
});
test('M05.04 aceptación de revalidación GET: éxito de sesión no certifica cutover', async ({ page }) => {
  const state = await preparar(page);
  await page.goto(ruta);
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Vacío');
  await page.getByRole('button', { name: 'Revalidar sesión de plataforma' }).click();
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Éxito: sesión global revalidada');
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('SIN_VERIFICAR');
  await expect(page.locator('[data-testid=c1204-m0402-panel]')).toHaveAttribute('aria-busy', 'false');
  await expect(page.locator('.controles li')).toHaveCount(4);
  expect(state.gets()).toBe(2);
  expect(state.writes).toEqual([]);
});
test('M05.04 aceptación teclado real Tab y Enter, sin foco artificial', async ({ page }) => {
  const state = await preparar(page);
  await page.goto(ruta);
  const button = page.getByRole('button', { name: 'Revalidar sesión de plataforma' });
  await expect(button).toBeVisible();
  let reached = false;
  for (let i = 0; i < 40; i++) {
    await page.keyboard.press('Tab');
    if (await button.evaluate(node => document.activeElement === node)) { reached = true; break; }
  }
  expect(reached, 'el control debe ser alcanzable por Tab real').toBe(true);
  await expect(button).toHaveCSS('outline-style', 'solid');
  await page.keyboard.press('Enter');
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Éxito');
  expect(state.gets()).toBe(2);
  expect(state.writes).toEqual([]);
});
test('M05.04 aceptación responsive y tema local en cuatro anchuras', async ({ page }) => {
  const state = await preparar(page);
  await page.goto(ruta);
  const tema = page.getByRole('group', { name: 'Tema visual permitido' });
  await expect(tema.getByRole('button')).toHaveCount(2);
  await tema.getByRole('button', { name: 'Alto contraste' }).click();
  await expect(page.locator('.cutover')).toHaveClass(/contraste-alto/);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(page.locator('.controles li')).toHaveCount(4);
    expect(await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth),
      'sin desbordamiento horizontal en ' + width).toBeLessThanOrEqual(2);
  }
  await tema.getByRole('button', { name: 'Institucional' }).click();
  await expect(page.locator('.cutover')).not.toHaveClass(/contraste-alto/);
  expect(state.gets()).toBe(1);
  expect(state.writes).toEqual([]);
});
test('M05.04 frontera: 503 posterior elimina aceptación privada y oculta DSN', async ({ page }) => {
  const state = await preparar(page, 503);
  await page.goto(ruta);
  await expect(page.locator('.controles li')).toHaveCount(4);
  await page.getByRole('button', { name: 'Revalidar sesión de plataforma' }).click();
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Error');
  await expect(page.locator('.controles li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_MYSQL_DSN_DO_NOT_RENDER')).toHaveCount(0);
  expect(state.gets()).toBe(2);
  expect(state.writes).toEqual([]);
});
