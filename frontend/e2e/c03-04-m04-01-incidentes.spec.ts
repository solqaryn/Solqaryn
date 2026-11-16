import { expect, test, type Page } from '@playwright/test';

/** Pruebas sintéticas de navegador: no validan JWT/DB/RESTORE reales. */
async function tokenSimulado(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c03-04-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}
async function sesionSimulada(page: Page, permitido: boolean) {
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
    await route.fulfill({ status: permitido ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(permitido ?
        { success: true, data: { nombreUsuario: 'Prueba', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } } :
        { success: false, data: null, message: 'DENIED_INTERNAL_DIAGNOSTIC' }) });
  });
}
test('anónimo redirige a login sin pantalla privada', async ({ page }) => {
  await page.goto('/plataforma/respuesta-incidentes-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.incident-review li')).toHaveCount(0);
});
test('403 sintético rechaza acceso y no expone diagnóstico', async ({ page }) => {
  await tokenSimulado(page);
  await sesionSimulada(page, false);
  await page.goto('/plataforma/respuesta-incidentes-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.incident-review li')).toHaveCount(0);
  await expect(page.getByText('DENIED_INTERNAL_DIAGNOSTIC')).toHaveCount(0);
});
test('grant sintético solo lectura y sin desbordamiento móvil ni API operativa', async ({ page }) => {
  await tokenSimulado(page);
  await sesionSimulada(page, true);
  await page.setViewportSize({ width: 375, height: 812 });
  const operativas: string[] = [];
  await page.route(/\/(?:restore|backup|jobs\/dispatch|revocar)(?:\/|\?|$)/i, route => {
    operativas.push(route.request().url());
    return route.abort();
  });
  await page.goto('/plataforma/respuesta-incidentes-v5');
  await expect(page.getByRole('heading', { name: 'Respuesta, revocación y recuperación segura' })).toBeVisible();
  await expect(page.locator('.incident-review li')).toHaveCount(4);
  await expect(page.getByText(/RTO\/RPO: NO_MEDIDO/)).toBeVisible();
  await expect(page.locator('.incident-guide form, .incident-guide input')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Revalidar sesión de plataforma' })).toBeVisible();
  expect(operativas).toEqual([]);
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
});

test('C03-04.M04.02 vacío y éxito de revalidación sintética', async ({ page }) => {
  await tokenSimulado(page);
  await sesionSimulada(page, true);
  await page.goto('/plataforma/respuesta-incidentes-v5');
  await expect(page.getByRole('heading', { name: 'Sin incidentes conectados' })).toBeVisible();
  await page.getByRole('button', { name: 'Revalidar sesión de plataforma' }).click();
  await expect(page.getByText(/Sesión administradora confirmada por la API/)).toBeVisible();
  await expect(page.locator('.incident-review li')).toHaveCount(4);
});
test('C03-04.M04.02 503 posterior no expone guía ni diagnóstico de servidor', async ({ page }) => {
  await tokenSimulado(page);
  let fallar = false;
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
      status: fallar ? 503 : 200, headers, contentType: 'application/json',
      body: JSON.stringify(fallar
        ? { success: false, message: 'UPSTREAM_SECRET_DIAGNOSTIC' }
        : { success: true, data: { nombreUsuario: 'Prueba', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } })
    });
  });
  await page.goto('/plataforma/respuesta-incidentes-v5');
  await expect(page.locator('.incident-review li')).toHaveCount(4);
  fallar = true;
  await page.getByRole('button', { name: 'Revalidar sesión de plataforma' }).click();
  await expect(page.getByRole('alert')).toContainText('No se pudo comprobar la sesión');
  await expect(page.locator('.incident-review li')).toHaveCount(0);
  await expect(page.getByText('UPSTREAM_SECRET_DIAGNOSTIC')).toHaveCount(0);
});

test('C03-04.M04.03 densidad compacta real, 320px, sin acciones operativas', async ({ page }) => {
  await tokenSimulado(page);
  await sesionSimulada(page, true);
  await page.setViewportSize({ width: 320, height: 720 });
  const operaciones: string[] = [];
  await page.route(url => /(?:restore|backup|dispatch|revocar)/i.test(url.toString()), route => {
    operaciones.push(route.request().url());
    return route.abort();
  });
  await page.goto('/plataforma/respuesta-incidentes-v5');
  const guia = page.locator('.incident-guide');
  await expect(guia).toHaveAttribute('data-densidad', 'comoda');
  await page.getByRole('button', { name: 'Usar vista compacta' }).click();
  await expect(guia).toHaveAttribute('data-densidad', 'compacta');
  await expect(page.locator('.incident-review li')).toHaveCount(4);
  expect(operaciones).toEqual([]);
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
});
