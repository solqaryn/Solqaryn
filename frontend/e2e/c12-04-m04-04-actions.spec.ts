import { expect, test, type Page } from '@playwright/test';

/** C12-04.M04.04: sesión simulada en navegador real; sin JWT/MySQL/restore real. */
async function preparar(page: Page, config: { secondStatus?: number; gate?: { release?: () => void } } = {}) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1204-m0404-fake-test-token');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let gets = 0;
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods':'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status:204, headers }); return;
    }
    gets++;
    if (gets > 1 && config.gate) {
      await new Promise<void>(resolve => { config.gate!.release = resolve; });
    }
    const status = gets > 1 ? config.secondStatus ?? 200 : 200;
    await route.fulfill({ status, headers, contentType:'application/json',
      body:JSON.stringify(status === 200
        ? { success:true, data:{ nombreUsuario:'Prueba', rolNombre:'Global', ambito:'Plataforma', esAdministrador:true } }
        : { success:false, data:null, message:'PRIVATE_MYSQL_DSN_MUST_NOT_APPEAR' }) });
  });
  return { count: () => gets, writes };
}

test('M04.04 vista privada no incluye formularios ni comandos de cutover', async ({ page }) => {
  const state = await preparar(page);
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await expect(page.getByRole('heading', {name:'Controles de preparación para cutover'})).toBeVisible();
  await expect(page.locator('.cutover form, .cutover input, .cutover [data-testid^=c1204-operate]')).toHaveCount(0);
  await expect(page.getByRole('button', {name:'Revalidar sesión de plataforma'})).toBeVisible();
  await expect(page.getByText(/NO OPERATIVO/)).toBeVisible();
  expect(state.writes).toEqual([]);
});

test('M04.04 GET pendiente elimina acción duplicable y aria-busy se limpia tras éxito', async ({ page }) => {
  const gate: { release?: () => void } = {};
  const state = await preparar(page, {gate});
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await page.getByRole('button', {name:'Revalidar sesión de plataforma'}).click();
  await expect.poll(state.count).toBe(2);
  await expect(page.locator('[data-testid=c1204-m0402-panel]')).toHaveAttribute('aria-busy', 'true');
  await expect(page.getByRole('button', {name:'Revalidar sesión de plataforma'})).toHaveCount(0);
  await expect(page.locator('.controles li')).toHaveCount(0);
  if (!gate.release) throw new Error('El segundo GET no quedó pendiente en el stub');
  gate.release();
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Éxito');
  await expect(page.locator('[data-testid=c1204-m0402-panel]')).toHaveAttribute('aria-busy','false');
  expect(state.count()).toBe(2);
  expect(state.writes).toEqual([]);
});

test('M04.04 un 503 posterior muestra error opaco sin datos privados ni reintento', async ({ page }) => {
  const state = await preparar(page, {secondStatus:503});
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await page.getByRole('button', {name:'Revalidar sesión de plataforma'}).click();
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Error');
  await expect(page.locator('.controles li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_MYSQL_DSN_MUST_NOT_APPEAR')).toHaveCount(0);
  expect(state.count()).toBe(2);
  expect(state.writes).toEqual([]);
});

test('M04.04 teclado real con Tab y Enter activa solo GET de revalidación', async ({ page }) => {
  const state = await preparar(page);
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  const btn = page.getByRole('button', {name:'Revalidar sesión de plataforma'});
  await expect(btn).toBeVisible();
  let focused = false;
  for (let i=0;i<40;i++) {
    await page.keyboard.press('Tab');
    if (await btn.evaluate(el => document.activeElement === el)) { focused = true; break; }
  }
  expect(focused).toBe(true);
  await expect(btn).toHaveCSS('outline-style','solid');
  await page.keyboard.press('Enter');
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Éxito');
  expect(state.count()).toBe(2);
  expect(state.writes).toEqual([]);
});
