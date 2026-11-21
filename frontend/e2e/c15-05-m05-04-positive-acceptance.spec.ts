import { expect, test, type Page } from '@playwright/test';

/** C15-05.M05.04 — aceptación real de UI Angular con sesión HTTP sintetizada.
 * No acredita JWT firmado, RBAC real, tenant A/B, MySQL ni despliegue. */
const routeSession = /\/plataforma\/sesion(?:\?|$)/;

async function setupSyntheticApprovedSession(page: Page): Promise<{ count: () => number }> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c15-m0504-test-only-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let requests = 0;
  await page.route(routeSession, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    expect(route.request().method()).toBe('GET');
    requests += 1;
    await route.fulfill({
      status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: { nombreUsuario: 'Acceptance Test', rolNombre: 'Plataforma',
          ambito: 'Plataforma', esAdministrador: true }
      })
    });
  });
  return { count: () => requests };
}

test('C15-05.M05.04: aceptación positiva de controles estáticos en pantalla estrecha y amplia', async ({ page }) => {
  const session = await setupSyntheticApprovedSession(page);
  await page.emulateMedia({ reducedMotion: 'reduce' });
  for (const width of [320, 1280]) {
    await page.setViewportSize({ width, height: 840 });
    await page.goto('/plataforma/auditoria-operativa-v5');
    await expect(page.getByRole('main')).toHaveCount(1);
    await expect(page.locator('#main-content main')).toHaveCount(0);
    const view = page.locator('section.audit-view[aria-labelledby="audit-view-title"]');
    await expect(view.getByRole('heading', { name: 'Controles de ejecución y evidencia' })).toBeVisible();
    await expect(view.locator('ol.controls > li')).toHaveCount(4);
    await expect(view).toContainText('0 métricas verificadas');
    await expect(view).not.toContainText('A5_PASS');
    await expect(view.locator('form, input, textarea')).toHaveCount(0);
    expect(await page.evaluate(() => document.documentElement.scrollWidth -
      document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  }
  expect(session.count()).toBeGreaterThanOrEqual(2);
});

test('C15-05.M05.04: teclado Tab y Enter activa revalidación real de UI sin aprobación', async ({ page }) => {
  const session = await setupSyntheticApprovedSession(page);
  await page.goto('/plataforma/auditoria-operativa-v5');
  const refresh = page.getByRole('button', { name: 'Revalidar sesión de auditoría' });
  await expect(refresh).toBeVisible();
  let reached = false;
  for (let i = 0; i < 40; i++) {
    await page.keyboard.press('Tab');
    if (await refresh.evaluate(element => document.activeElement === element)) {
      reached = true;
      break;
    }
  }
  expect(reached, 'Botón alcanzable desde Tab real sin .focus()').toBe(true);
  await page.keyboard.press('Enter');
  await expect(page.getByText('Sesión de plataforma revalidada correctamente. No se consultan registros operativos en vivo.')).toBeVisible();
  await expect(page.locator('section.audit-view[aria-labelledby="audit-view-title"] ol.controls > li')).toHaveCount(4);
  expect(session.count()).toBe(2);
});

test('C15-05.M05.04: aceptación de solo lectura nunca ejecuta HTTP mutante', async ({ page }) => {
  const mutations: string[] = [];
  page.on('request', request => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method()))
      mutations.push(request.method() + ' ' + request.url());
  });
  const session = await setupSyntheticApprovedSession(page);
  await page.goto('/plataforma/auditoria-operativa-v5');
  await page.getByRole('button', { name: 'Revalidar sesión de auditoría' }).click();
  await expect(page.getByText('Sesión de plataforma revalidada correctamente. No se consultan registros operativos en vivo.')).toBeVisible();
  await expect(page.locator('section.audit-view[aria-labelledby="audit-view-title"] ol.controls > li')).toHaveCount(4);
  expect(mutations).toEqual([]);
  expect(session.count()).toBe(2);
});
