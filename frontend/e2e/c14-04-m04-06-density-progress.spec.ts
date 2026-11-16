import { expect, test, type Page } from '@playwright/test';

/** C14-04.M04.06: real Chromium, synthetic session only. No live JWT/DB/PROD evidence. */
type ResponseStatus = 200 | 403 | 503;
async function session(
  page: Page,
  status: (requestNumber: number) => ResponseStatus,
  hold?: (requestNumber: number) => Promise<void>
): Promise<() => number> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'm0406-synthetic-not-jwt');
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
    gets += 1;
    if (hold) await hold(gets);
    const response = status(gets);
    await route.fulfill({
      status: response, headers, contentType: 'application/json',
      body: JSON.stringify(response === 200
        ? { success: true, data: { nombreUsuario: 'Synthetic', rolNombre: 'Admin',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'PRIVATE_M0406_DIAGNOSTIC_NEVER_EXPOSE' })
    });
  });
  return () => gets;
}

test('C14-04.M04.06 densidad acotada a 320/375/768/1280 sin exceso de controles', async ({ page }) => {
  await session(page, () => 200);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/plataforma/flujo-v5');
    const card = page.getByTestId('release-readiness-contract');
    await expect(card).toBeVisible();
    await expect(card.locator('ol li')).toHaveCount(4);
    await expect(card.locator('input, form, textarea, button')).toHaveCount(0);
    await expect(card).toContainText('NO_MEDIDO');
    await expect(card).not.toContainText('PASS');
    const layout = await card.evaluate(el => {
      const r = el.getBoundingClientRect();
      return { width: r.width, left: r.left, right: r.right, viewport: document.documentElement.clientWidth };
    });
    expect(layout.width).toBeGreaterThan(0);
    expect(layout.left).toBeGreaterThanOrEqual(-2);
    expect(layout.right).toBeLessThanOrEqual(layout.viewport + 2);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'horizontal overflow at ' + width + 'px').toBeLessThanOrEqual(2);
  }
});

test('C14-04.M04.06 carga progresiva muestra espera accesible y termina sin falso PASS', async ({ page }) => {
  let unblock!: () => void;
  const held = new Promise<void>(resolve => { unblock = resolve; });
  const gets = await session(page, () => 200, async n => {
    if (n === 2) await held;
  });
  await page.goto('/plataforma/flujo-v5');
  const feedback = page.getByTestId('release-readiness-feedback');
  await expect(feedback).toHaveAttribute('data-state', 'exito');
  const button = page.getByRole('button', { name: /Revalidar sesión/ });
  try {
    await button.click();
    await expect.poll(() => gets()).toBe(2);
    await expect(feedback).toHaveAttribute('data-state', 'cargando');
    await expect(page.getByTestId('release-loading')).toBeVisible();
    await expect(button).toBeDisabled();
    await expect(page.getByTestId('release-success')).toHaveCount(0);
  } finally {
    unblock();
  }
  await expect(feedback).toHaveAttribute('data-state', 'exito');
  await expect(page.getByTestId('release-loading')).toHaveCount(0);
  await expect(page.getByTestId('release-empty')).toHaveAttribute('data-evidence-state', 'vacio');
  await expect(page.getByTestId('release-readiness-contract')).not.toContainText('PASS');
  expect(gets()).toBe(2);
});

test('C14-04.M04.06 error externo no deja spinner perpetuo ni filtra detalles internos', async ({ page }) => {
  const gets = await session(page, n => n === 1 ? 200 : 503);
  await page.goto('/plataforma/flujo-v5');
  await page.getByRole('button', { name: /Revalidar sesión/ }).click();
  await expect(page.getByTestId('release-readiness-feedback')).toHaveAttribute('data-state', 'error');
  await expect(page.getByTestId('release-error')).toBeVisible();
  await expect(page.getByTestId('release-loading')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_M0406_DIAGNOSTIC_NEVER_EXPOSE')).toHaveCount(0);
  // Fail-closed: the private readiness contract is removed after 503, not populated.
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
  expect(gets()).toBe(2);
});
