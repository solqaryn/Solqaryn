import { expect, test, type Page } from '@playwright/test';

/** CI sintética UI, sin certificar seguridad HTTP/JWT/tenancy real. */
async function session(page: Page, grant: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c14-02-synthetic-not-a-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await route.fulfill({ status: grant ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(grant
        ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'DO_NOT_EXPOSE_INTERNAL' }) });
  });
}
test('anónimo redirige a login', async ({ page }) => {
  await page.goto('/plataforma/revision-amenazas-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.threat-list li')).toHaveCount(0);
});
test('403 servidor no permite pantalla ni filtra mensaje', async ({ page }) => {
  await session(page, false);
  await page.goto('/plataforma/revision-amenazas-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.threat-list li')).toHaveCount(0);
  await expect(page.getByText('DO_NOT_EXPOSE_INTERNAL')).toHaveCount(0);
});
test('acceso sintético: cuatro categorías solo lectura y viewport móvil', async ({ page }) => {
  await session(page, true);
  await page.setViewportSize({ width: 390, height: 844 });
  const called: string[] = [];
  await page.route(/\/(?:restore|backup|pentest|jobs\/dispatch|security\/grant)(?:\/|\?|$)/i,
    route => { called.push(route.request().url()); return route.abort(); });
  await page.goto('/plataforma/revision-amenazas-v5');
  await expect(page.getByRole('heading', { name: 'Verificaciones de acceso y abuso' })).toBeVisible();
  await expect(page.locator('.threat-list li')).toHaveCount(4);
  await expect(page.getByText(/pentest en vivo: NO_MEDIDO/)).toBeVisible();
  await expect(page.locator('.threat-guide input, .threat-guide form')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Revalidar sesión de plataforma' })).toBeVisible();
  expect(called).toEqual([]);
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
});

test('C14-02.M04.02 vacío sin hallazgos y revalidación de sesión sintética', async ({ page }) => {
  await session(page, true);
  await page.goto('/plataforma/revision-amenazas-v5');
  await expect(page.getByRole('heading', { name: 'Sin hallazgos conectados' })).toBeVisible();
  await page.getByRole('button', { name: 'Revalidar sesión de plataforma' }).click();
  await expect(page.getByText(/Sesión de plataforma confirmada/)).toBeVisible();
  await expect(page.locator('.threat-list li')).toHaveCount(4);
});
test('C14-02.M04.02 503 posterior niega la guía sin filtrar diagnóstico', async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c14-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let error = false;
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await route.fulfill({ status: error ? 503 : 200, headers, contentType: 'application/json',
      body: JSON.stringify(error ? { success: false, message: 'INTERNAL_SECRET_DIAGNOSTIC' }
        : { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }) });
  });
  await page.goto('/plataforma/revision-amenazas-v5');
  await expect(page.locator('.threat-list li')).toHaveCount(4);
  error = true;
  await page.getByRole('button', { name: 'Revalidar sesión de plataforma' }).click();
  await expect(page.getByRole('alert')).toContainText('No se pudo comprobar la sesión');
  await expect(page.locator('.threat-list li')).toHaveCount(0);
  await expect(page.getByText('INTERNAL_SECRET_DIAGNOSTIC')).toHaveCount(0);
});
