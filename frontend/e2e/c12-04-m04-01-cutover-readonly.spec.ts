import { expect, test, type Page } from '@playwright/test';

/** Chromium real + GET de sesion simulado. No JWT, MySQL, backup ni restore real. */
async function simularSesion(page: Page, autorizado: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1204m0401-synthetic-not-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({
      status: autorizado ? 200 : 403,
      headers,
      contentType: 'application/json',
      body: JSON.stringify(autorizado
        ? { success: true, data: { nombreUsuario: 'Test', rolNombre: 'Global', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'PRIVATE_MYSQL_DSN_MUST_NEVER_LEAK' })
    });
  });
}

test('C12-04.M04.01 anónimo redirigido y controles privados ausentes', async ({ page }) => {
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('[data-testid=c1204-cutover-private]')).toHaveCount(0);
});

test('C12-04.M04.01 HTTP 403 sintético no revela estados ni diagnóstico', async ({ page }) => {
  await simularSesion(page, false);
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.controles li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_MYSQL_DSN_MUST_NEVER_LEAK')).toHaveCount(0);
});

test('C12-04.M04.01 grant simulado: cuatro checkpoints no operativos y responsive', async ({ page }) => {
  await simularSesion(page, true);
  const writes: string[] = [];
  page.on('request', r => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await expect(page.getByRole('heading', { name: 'Controles de preparación para cutover' })).toBeVisible();
  await expect(page.locator('.controles li')).toHaveCount(4);
  await expect(page.getByText(/Sin acceso a MySQL ni ejecución de migraciones/)).toBeVisible();
  await expect(page.locator('.cutover form, .cutover input, .cutover button:not([data-testid=c1204-m0402-revalidate])')).toHaveCount(0);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  }
  expect(writes).toEqual([]);
});

test('C12-04.M04.01 teclado real desde inicio llega a Volver sin .focus() artificial', async ({ page }) => {
  await simularSesion(page, true);
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  const volver = page.getByRole('link', { name: 'Volver a plataforma' });
  await expect(volver).toBeVisible();
  let tabReached = false;
  for (let i = 0; i < 30; i++) {
    await page.keyboard.press('Tab');
    if (await volver.evaluate(node => document.activeElement === node)) {
      tabReached = true;
      break;
    }
  }
  expect(tabReached, 'enlace alcanzable por Tab real').toBe(true);
  await expect(volver).toHaveCSS('outline-style', 'solid');
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma(?:\?.*)?$/);
});

test('M04.02 sesión real de Angular revalidada por GET simulado sin mutar datos', async ({ page }) => {
  await simularSesion(page, true);
  let gets = 0;
  const writes: string[] = [];
  page.on('request', r => {
    if (r.method()==='GET' && r.url().includes('/plataforma/sesion')) gets++;
    if (!['GET','HEAD','OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Vacío');
  await page.getByRole('button',{ name:'Revalidar sesión de plataforma' }).click();
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Éxito');
  await expect(page.locator('.controles li')).toHaveCount(4);
  expect(gets).toBe(2);
  expect(writes).toEqual([]);
});

test('M04.02 503 tras sesión válida oculta datos y no expone DSN', async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token','c1204-m0402-not-real-jwt');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  let failed = false;
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = { 'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods':'GET, OPTIONS' };
    if (route.request().method()==='OPTIONS') { await route.fulfill({status:204,headers}); return; }
    await route.fulfill({status:failed?503:200,headers,contentType:'application/json',
      body:JSON.stringify(failed?{success:false,message:'PRIVATE_DSN'}:
        {success:true,data:{nombreUsuario:'Admin',rolNombre:'Global',ambito:'Plataforma',esAdministrador:true}})});
  });
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await expect(page.locator('.controles li')).toHaveCount(4);
  failed=true;
  await page.getByRole('button',{name:'Revalidar sesión de plataforma'}).click();
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Error');
  await expect(page.locator('.controles li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_DSN')).toHaveCount(0);
});
