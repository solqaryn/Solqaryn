import { expect, test, type Page } from '@playwright/test';

/** C02-04.M04.06: Chromium with synthetic session. No real JWT/MFA/grants or live DB. */
type SessionStatus = 200 | 403 | 503;
async function mockedSession(
  page: Page,
  status: (getIndex: number) => SessionStatus,
  hold?: (getIndex: number) => Promise<void>
): Promise<() => number> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0204-m0406-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let gets = 0;
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
    gets++;
    if (hold) await hold(gets);
    const response = status(gets);
    await route.fulfill({
      status: response, headers, contentType: 'application/json',
      body: JSON.stringify(response === 200
        ? { success: true, data: { nombreUsuario: 'Synthetic', rolNombre: 'Admin',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'PRIVATE_C0204_M0406_DIAGNOSTIC' })
    });
  });
  return () => gets;
}

test('C02-04.M04.06 densidad de consola y marca adaptativa 320 a 1280 sin overflow', async ({ page }) => {
  await mockedSession(page, () => 200);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/plataforma/consola-global-v5');
    const panel = page.locator('.control-plane');
    await expect(panel.getByRole('heading', { name: 'Consola global auditada' })).toBeVisible();
    await expect(panel.locator('.control-cards li')).toHaveCount(4);
    await expect(panel.getByTestId('c0204-branding')).toBeVisible();
    await expect(panel.locator('input, textarea, form')).toHaveCount(0);
    await expect(panel).toContainText('NO_MEDIDO');
    const layout = await panel.evaluate(el => {
      const list = el.querySelector('.control-cards');
      const actions = el.querySelector('.control-brand__actions');
      const style = list && getComputedStyle(list);
      return {
        columns: style ? style.gridTemplateColumns.trim().split(/\s+/).length : 0,
        direction: actions ? getComputedStyle(actions).flexDirection : '',
        right: el.getBoundingClientRect().right,
        viewport: document.documentElement.clientWidth
      };
    });
    expect(layout.columns).toBe(width <= 375 ? 1 : 2);
    expect(layout.direction).toBe(width <= 375 ? 'column' : 'row');
    expect(layout.right).toBeLessThanOrEqual(layout.viewport + 2);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'horizontal overflow at ' + width + 'px').toBeLessThanOrEqual(2);
  }
});

test('C02-04.M04.06 espera observable de GET sin liberar controles ni duplicar petición', async ({ page }) => {
  let release!: () => void;
  const held = new Promise<void>(resolve => { release = resolve; });
  const gets = await mockedSession(page, () => 200, async n => {
    if (n === 2) await held;
  });
  await page.goto('/plataforma/consola-global-v5');
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  const button = page.getByRole('button', { name: 'Revalidar sesión global' });
  const notice = page.locator('#control-plane-estado');
  try {
    await button.click();
    await expect.poll(() => gets()).toBe(2);
    await expect(notice).toHaveAttribute('aria-busy', 'true');
    await expect(notice.getByRole('status')).toContainText('Revalidando autorización global');
    await expect(button).toBeDisabled();
    await expect(page.locator('.control-cards li')).toHaveCount(0);
  } finally {
    release();
  }
  await expect(notice).toHaveAttribute('aria-busy', 'false');
  await expect(notice.getByRole('status')).toContainText('Solo consulta de controles');
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  expect(gets()).toBe(2);
});

test('C02-04.M04.06 error 503 es opaco, limpia espera y recupera solo tras otro GET', async ({ page }) => {
  const gets = await mockedSession(page, n => n === 2 ? 503 : 200);
  await page.goto('/plataforma/consola-global-v5');
  await page.getByRole('button', { name: 'Revalidar sesión global' }).click();
  const notice = page.locator('#control-plane-estado');
  await expect(notice).toHaveAttribute('aria-busy', 'false');
  await expect(notice.getByRole('alert')).toContainText('No se pudo verificar la sesión');
  await expect(page.locator('.control-cards li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_C0204_M0406_DIAGNOSTIC')).toHaveCount(0);
  await page.getByRole('button', { name: 'Reintentar verificación' }).click();
  await expect(notice.getByRole('status')).toContainText('Solo consulta de controles');
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  expect(gets()).toBe(3);
});
