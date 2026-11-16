import { expect, test, type Page } from '@playwright/test';

const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function session(page: Page, permitted = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0504-m0504-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    await route.fulfill({
      status: permitted ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(permitted
        ? { success: true, data: { nombreUsuario: 'Demo', rolNombre: 'Admin',
          ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'DO_NOT_EXPOSE_PRIVATE_SESSION' })
    });
  });
}

test('C05-04.M05.04: acepta 25 filas con dos controles cosméticos y un solo GET', async ({ page }) => {
  await session(page);
  let gets = 0;
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    gets++;
    const items = Array.from({ length: 25 }, (_, i) => ({
      id: i + 1, nombre: 'Empresa de prueba ' + (i + 1), activa: true, usuariosActivos: 1
    }));
    await route.fulfill({ status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: { items, pagina: 1, tamano: 25,
        total: 25, puedeCrear: false } }) });
  });
  await page.goto('/plataforma');
  const summary = page.locator('details.ui-lightness-v5 > summary');
  await expect(summary).toBeVisible();
  await summary.click();
  const view = page.locator('.ui-lightness-v5');
  await expect(page.locator('table[aria-label="Empresas registradas"] tbody tr')).toHaveCount(25);
  await expect(page.getByTestId('c0504-success')).toContainText('25 empresas');
  await view.getByRole('button', { name: 'Reducir detalles' }).click();
  await expect(view.locator('.ui-lightness-v5__checks li')).toHaveCount(0);
  await view.getByRole('button', { name: 'Ver criterios' }).click();
  await expect(view.locator('.ui-lightness-v5__checks li')).toHaveCount(3);
  await page.getByTestId('c0504-brand-contraste').click();
  await expect(page.getByTestId('c0504-brand-contraste')).toHaveAttribute('aria-pressed', 'true');
  await page.getByTestId('c0504-brand-institucional').click();
  await expect(page.getByTestId('c0504-brand-institucional')).toHaveAttribute('aria-pressed', 'true');
  for (const width of [320, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth
    );
    expect(overflow).toBeLessThanOrEqual(2);
  }
  expect(gets, 'presentación no puede volver a consultar directorio').toBe(1);
  expect(writes, 'presentación read-only no puede realizar mutaciones').toEqual([]);
});

test('C05-04.M05.04: respuesta válida vacía informa cero sin falsa autorización', async ({ page }) => {
  await session(page);
  await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    await route.fulfill({ status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: { items: [], pagina: 1,
        tamano: 25, total: 0, puedeCrear: false } }) });
  });
  await page.goto('/plataforma');
  await page.locator('details.ui-lightness-v5 > summary').click();
  await expect(page.getByTestId('c0504-empty')).toBeVisible();
  await expect(page.locator('table[aria-label="Empresas registradas"] tbody tr')).toHaveCount(0);
  await expect(page.getByTestId('c0504-feedback')).not.toContainText('Autorización concedida');
});

test('C05-04.M05.04: sesión denegada no permite aceptación positiva', async ({ page }) => {
  await session(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
  await expect(page.getByText('DO_NOT_EXPOSE_PRIVATE_SESSION')).toHaveCount(0);
});

test('C05-04.M05.04: 503 muestra error genérico, no éxito ficticio', async ({ page }) => {
  await session(page);
  await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    await route.fulfill({ status: 503, headers, contentType: 'application/json',
      body: JSON.stringify({ success: false, message: 'PROVIDER_PRIVATE_DETAILS' }) });
  });
  await page.goto('/plataforma');
  await page.locator('details.ui-lightness-v5 > summary').click();
  await expect(page.getByTestId('c0504-error')).toBeVisible();
  await expect(page.getByTestId('c0504-success')).toHaveCount(0);
  await expect(page.getByText('PROVIDER_PRIVATE_DETAILS')).toHaveCount(0);
});
