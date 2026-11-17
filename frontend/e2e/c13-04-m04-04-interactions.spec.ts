import { test, expect, type Page } from '@playwright/test';

// C13-04.M04.04: browser interaction test with synthetic global session, NOT live JWT/RBAC/A-B.
const route = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const granted = { success: true, data: { nombreUsuario: 'Synthetic',
  rolNombre: 'Global', ambito: 'Plataforma', esAdministrador: true } };

async function session(page: Page, enabled: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1304-m0404-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(route, async handler => {
    if (handler.request().method() === 'OPTIONS') {
      await handler.fulfill({ status: 204, headers: cors });
      return;
    }
    await handler.fulfill({
      status: enabled ? 200 : 403, headers: cors, contentType: 'application/json',
      body: JSON.stringify(enabled ? granted
        : { success: false, message: 'SECRET_PRIVATE_PROVIDER_DSN' })
    });
  });
}

test('C13-04.M04.04 anonymous and 403 do not reveal private navigation or errors', async ({ page }) => {
  for (const path of ['/plataforma/consola-global-v5', '/plataforma/trazabilidad-cobertura-v5']) {
    await page.goto(path);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('.control-cards li, .coverage-list li')).toHaveCount(0);
  }
  await session(page, false);
  for (const path of ['/plataforma/consola-global-v5', '/plataforma/trazabilidad-cobertura-v5']) {
    await page.goto(path);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByText('SECRET_PRIVATE_PROVIDER_DSN')).toHaveCount(0);
    await expect(page.locator('.control-cards li, .coverage-list li')).toHaveCount(0);
  }
});

test('C13-04.M04.04 503 error renders opaque feedback and retry restores read-only state', async ({ page }) => {
  await session(page, true);
  await page.goto('/plataforma/consola-global-v5');
  let failing = true, hits = 0;
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.route(route, async handler => {
    if (handler.request().method() === 'OPTIONS') {
      await handler.fulfill({ status: 204, headers: cors }); return;
    }
    hits++;
    await handler.fulfill({ status: failing ? 503 : 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify(failing
        ? { success: false, message: 'SECRET_DB_DSN_MUST_NOT_DISPLAY' } : granted) });
  });
  await page.getByRole('button', { name: 'Revalidar sesión global' }).click();
  await expect(page.getByRole('alert')).toContainText('No se pudo verificar');
  await expect(page.locator('.control-cards li')).toHaveCount(0);
  await expect(page.getByText('SECRET_DB_DSN_MUST_NOT_DISPLAY')).toHaveCount(0);
  failing = false;
  await page.getByRole('button', { name: 'Reintentar verificación' }).click();
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  expect(hits).toBe(2);
  expect(writes).toEqual([]);
});

test('C13-04.M04.04 double action while pending is blocked; only one GET', async ({ page }) => {
  await session(page, true);
  await page.goto('/plataforma/consola-global-v5');
  let release!: () => void;
  const pending = new Promise<void>(resolve => { release = resolve; });
  let hits = 0;
  await page.route(route, async handler => {
    if (handler.request().method() === 'OPTIONS') {
      await handler.fulfill({ status: 204, headers: cors }); return;
    }
    hits++;
    await pending;
    await handler.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify(granted) });
  });
  await page.getByRole('button', { name: 'Revalidar sesión global' }).click();
  await expect(page.locator('#control-plane-estado')).toHaveAttribute('aria-busy', 'true');
  const action = page.locator('.control-plane nav button');
  await expect(action).toBeDisabled();
  await action.evaluate(element => (element as HTMLButtonElement).click());
  expect(hits).toBe(1);
  release();
  await expect(page.locator('#control-plane-estado')).toHaveAttribute('aria-busy', 'false');
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  expect(hits).toBe(1);
});

test('C13-04.M04.04 labeled keyboard return links navigate without forms or writes', async ({ page }) => {
  await session(page, true);
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  const views = [
    { path: '/plataforma/consola-global-v5', name: 'Volver a administración de plataforma', root: '.control-plane' },
    { path: '/plataforma/trazabilidad-cobertura-v5', name: 'Regresar a administración de plataforma', root: '.coverage-shell' }
  ];
  for (const view of views) {
    await page.goto(view.path);
    await expect(page.locator(view.root + ' form, ' + view.root + ' input')).toHaveCount(0);
    const back = page.getByRole('link', { name: view.name });
    let reached = false;
    for (let i = 0; i < 100; i++) {
      await page.keyboard.press('Tab');
      if (await back.evaluate(el => document.activeElement === el)) {
        reached = true;
        break;
      }
    }
    expect(reached, view.path + ' keyboard reachability').toBe(true);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/plataforma(?:\?|$)/);
  }
  expect(writes).toEqual([]);
});
