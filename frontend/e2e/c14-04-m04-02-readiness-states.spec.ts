import { expect, test, type Page, type Route } from '@playwright/test';

// C14-04.M04.02: HTTP y token sinteticos; no certifica JWT real, tenant backend ni PROD.
const endpoint = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

function response(status: 200 | 403 | 503) {
  return {
    status, headers: cors, contentType: 'application/json',
    body: JSON.stringify(status === 200
      ? { success: true, data: { nombreUsuario: 'Audit', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
      : { success: false, data: null, message: 'PRIVATE_READINESS_SECRET' })
  };
}

async function enterPrivate(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-only-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(endpoint, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
    } else {
      await route.fulfill(response(200));
    }
  });
  await page.goto('/plataforma/flujo-v5');
  await expect(page.getByTestId('release-readiness-contract')).toBeVisible();
}

test('C14-04.M04.02: exito de sesion NO certifica readiness y evidencia permanece vacia', async ({ page }) => {
  await enterPrivate(page);
  const feedback = page.getByTestId('release-readiness-feedback');
  await expect(feedback).toHaveAttribute('data-state', 'exito');
  await expect(page.getByTestId('release-success')).toBeVisible();
  await expect(page.getByTestId('release-empty')).toHaveAttribute('data-evidence-state', 'vacio');
  const panel = page.getByTestId('release-readiness-contract');
  await expect(panel.locator('ol li')).toHaveCount(4);
  await expect(panel.locator('button, form, input')).toHaveCount(0);
  await expect(panel).not.toContainText('PASS');
});

test('C14-04.M04.02: carga in-flight oculta panel y bloquea revalidacion duplicada', async ({ page }) => {
  await enterPrivate(page);
  let release!: () => void;
  const pending = new Promise<void>(resolve => { release = resolve; });
  const slow = async (route: Route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    await pending;
    await route.fulfill(response(200));
  };
  await page.route(endpoint, slow);
  const button = page.getByRole('button', { name: /Revalidar sesión/ });
  await button.click();
  try {
    await expect(page.getByTestId('release-readiness-feedback')).toHaveAttribute('data-state', 'cargando');
    await expect(page.getByTestId('release-loading')).toBeVisible();
    await expect(button).toBeDisabled();
    await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
  } finally {
    release();
  }
  await expect(page.getByTestId('release-readiness-feedback')).toHaveAttribute('data-state', 'exito');
  await expect(page.getByTestId('release-readiness-contract')).toBeVisible();
});

test('C14-04.M04.02: error 503 fail-closed, sin secreto y reintento GET autorizado', async ({ page }) => {
  await enterPrivate(page);
  const fail = async (route: Route) => route.fulfill(response(503));
  await page.route(endpoint, fail);
  await page.getByRole('button', { name: /Revalidar sesión/ }).click();
  await expect(page.getByTestId('release-readiness-feedback')).toHaveAttribute('data-state', 'error');
  await expect(page.getByTestId('release-error')).toBeVisible();
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_READINESS_SECRET')).toHaveCount(0);
  await page.unroute(endpoint, fail);
  await page.getByRole('button', { name: /Revalidar sesión/ }).click();
  await expect(page.getByTestId('release-readiness-feedback')).toHaveAttribute('data-state', 'exito');
  await expect(page.getByTestId('release-empty')).toBeVisible();
});

test('C14-04.M04.02: 403 revoca panel sin mostrar mensaje de servidor', async ({ page }) => {
  await enterPrivate(page);
  const deny = async (route: Route) => route.fulfill(response(403));
  await page.route(endpoint, deny);
  await page.getByRole('button', { name: /Revalidar sesión/ }).click();
  await expect(page.getByTestId('release-readiness-feedback')).toHaveAttribute('data-state', 'sin_permiso');
  await expect(page.getByTestId('release-denied')).toBeVisible();
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_READINESS_SECRET')).toHaveCount(0);
});

test('C14-04.M04.02: visitante anonimo nunca observa estados privados', async ({ page }) => {
  await page.goto('/plataforma/flujo-v5');
  await expect(page).toHaveURL(/\/login(?:\?.*)?$/);
  await expect(page.getByTestId('release-readiness-feedback')).toHaveCount(0);
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
});
