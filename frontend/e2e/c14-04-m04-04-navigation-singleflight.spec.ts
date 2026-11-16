import { expect, test, type Page } from '@playwright/test';

/**
 * C14-04.M04.04 — pruebas de la superficie privada de readiness DEV.
 * Solo respuestas de sesión sintéticas. No acredita JWT/HTTP real, tenant DB,
 * backup, alertas, soporte, R2, ni autorizaciones de despliegue.
 */
type SyntheticStatus = 200 | 403 | 503;

async function mockPlatform(
  page: Page, responseFor: (getNumber: number) => SyntheticStatus,
  hold?: (getNumber: number) => Promise<void>
): Promise<() => number> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'm0404-synthetic-not-jwt');
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
    const status = responseFor(gets);
    await route.fulfill({
      status, headers, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Synthetic',
            rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'INTERNAL_READINESS_SECRET_M0404' })
    });
  });
  return () => gets;
}

test('C14-04.M04.04 anónimo no ve readiness, formularios ni dispara escritura', async ({ page }) => {
  const writes: string[] = [];
  page.on('request', request => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
      writes.push(request.method() + ' ' + request.url());
    }
  });
  await page.goto('/plataforma/flujo-v5');
  await expect(page).toHaveURL(/\/login(?:\?.*)?$/);
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
  expect(writes).toEqual([]);
});

test('C14-04.M04.04 navegación interna mantiene lectura privada sin mutadores', async ({ page }) => {
  const count = await mockPlatform(page, () => 200);
  await page.goto('/plataforma/flujo-v5');
  const panel = page.getByTestId('release-readiness-contract');
  await expect(panel.getByRole('heading', { name: 'Preparación de salida · evidencia pendiente' })).toBeVisible();
  await expect(panel.locator('button, input, form, textarea')).toHaveCount(0);
  await expect(panel).not.toContainText('PASS');
  await page.getByRole('link', { name: /Administración/ }).click();
  await expect(page).toHaveURL(/\/plataforma\/?$/);
  await page.goto('/plataforma/flujo-v5');
  await expect(panel.locator('ol li')).toHaveCount(4);
  expect(count()).toBeGreaterThanOrEqual(2);
});

test('C14-04.M04.04 revocación 403 al revalidar oculta panel y mensaje interno', async ({ page }) => {
  const count = await mockPlatform(page, get => get === 1 ? 200 : 403);
  await page.goto('/plataforma/flujo-v5');
  await expect(page.getByTestId('release-readiness-contract')).toBeVisible();
  await page.getByRole('button', { name: /Revalidar sesión/ }).click();
  await expect(page.getByTestId('release-denied')).toBeVisible();
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
  await expect(page.getByText('INTERNAL_READINESS_SECRET_M0404')).toHaveCount(0);
  expect(count()).toBe(2);
});

test('C14-04.M04.04 error 503, reintento y rechazo de doble acción en vuelo', async ({ page }) => {
  let unblock: (() => void) | undefined;
  const held = new Promise<void>(resolve => { unblock = resolve; });
  const count = await mockPlatform(
    page,
    get => get === 2 ? 503 : 200,
    async get => { if (get === 2) await held; }
  );
  await page.goto('/plataforma/flujo-v5');
  const button = page.getByRole('button', { name: /Revalidar sesión/ });
  await expect(page.getByTestId('release-readiness-contract')).toBeVisible();

  try {
    await button.click();
    await expect.poll(() => count()).toBe(2);
    await expect(button).toBeDisabled();
    expect(count()).toBe(2);
  } finally {
    unblock?.();
  }
  await expect(page.getByTestId('release-error')).toBeVisible();
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
  await expect(page.getByText('INTERNAL_READINESS_SECRET_M0404')).toHaveCount(0);
  await expect(button).toBeEnabled();
  await button.click();
  await expect(page.getByTestId('release-success')).toBeVisible();
  await expect(page.getByTestId('release-readiness-contract')).toBeVisible();
  expect(count()).toBe(3);
});
