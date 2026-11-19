import { expect, test, type Page } from '@playwright/test';

// Ensayo UI aislado con session sintetica. NO JWT real ni certificacion tenant.
async function sesion(page: Page, resultado: () => { estado: number; demora?: number }) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-not-a-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    const { estado, demora } = resultado();
    if (demora) await new Promise(resolve => setTimeout(resolve, demora));
    return route.fulfill({ status: estado, headers, contentType: 'application/json',
      body: JSON.stringify(estado === 200
        ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'INTERNAL_NEVER_RENDER_THIS_MESSAGE' }) });
  });
}

test('C14-02.M04.06 densidad móvil 1 columna, tablet y desktop 2 sin scroll horizontal', async ({ page }) => {
  await sesion(page, () => ({ estado: 200 }));
  for (const ancho of [320, 768, 1280]) {
    await page.setViewportSize({ width: ancho, height: 844 });
    await page.goto('/plataforma/revision-amenazas-v5');
    await expect(page.locator('.threat-list li')).toHaveCount(4);
    const m = await page.locator('.threat-list').evaluate(lista => {
      const items = Array.from(lista.querySelectorAll('li'));
      return {
        columnas: getComputedStyle(lista).gridTemplateColumns.split(' ').filter(Boolean).length,
        mismaFila: Math.abs(items[0].getBoundingClientRect().top - items[1].getBoundingClientRect().top) <= 2,
        overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth
      };
    });
    expect(m.columnas).toBe(ancho <= 720 ? 1 : 2);
    expect(m.mismaFila).toBe(ancho > 720);
    expect(m.overflow).toBeLessThanOrEqual(2);
  }
});

test('C14-02.M04.06 control adversarial de densidad detecta columna CSS defectuosa', async ({ page }) => {
  await sesion(page, () => ({ estado: 200 }));
  await page.setViewportSize({ width: 320, height: 844 });
  await page.goto('/plataforma/revision-amenazas-v5');
  await expect(page.locator('.threat-list li')).toHaveCount(4);
  const antes = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(antes).toBeLessThanOrEqual(2);
  await page.addStyleTag({ content: '.threat-list { grid-template-columns: 600px !important; }' });
  const despues = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(despues).toBeGreaterThan(2);
});

test('C14-02.M04.06 carga progresiva lenta oculta guía hasta revalidacion exitosa', async ({ page }) => {
  let lenta = false;
  await sesion(page, () => ({ estado: 200, demora: lenta ? 700 : 0 }));
  await page.goto('/plataforma/revision-amenazas-v5');
  await expect(page.locator('.threat-list li')).toHaveCount(4);
  lenta = true;
  await page.getByRole('button', { name: 'Revalidar sesión de plataforma' }).click();
  await expect(page.locator('.threat-guide section.notice[role="status"]')).toContainText('Comprobando nuevamente la sesión');
  await expect(page.locator('.threat-list li')).toHaveCount(0);
  await expect(page.getByText(/Sesión de plataforma confirmada/)).toBeVisible();
  await expect(page.locator('.threat-list li')).toHaveCount(4);
});

test('C14-02.M04.06 503 posterior oculta datos y diagnóstico sin mutaciones', async ({ page }) => {
  let fallo = false;
  const mutaciones: string[] = [];
  page.on('request', req => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) mutaciones.push(req.method());
  });
  await sesion(page, () => ({ estado: fallo ? 503 : 200, demora: fallo ? 150 : 0 }));
  await page.goto('/plataforma/revision-amenazas-v5');
  await expect(page.locator('.threat-list li')).toHaveCount(4);
  fallo = true;
  await page.getByRole('button', { name: 'Revalidar sesión de plataforma' }).click();
  await expect(page.getByRole('alert')).toContainText('No se pudo comprobar la sesión');
  await expect(page.locator('.threat-list li')).toHaveCount(0);
  await expect(page.getByText('INTERNAL_NEVER_RENDER_THIS_MESSAGE')).toHaveCount(0);
  expect(mutaciones).toEqual([]);
});

test('C14-02.M04.06 humo DOMContentLoaded y ausencia de acciones comerciales', async ({ page }) => {
  const mutaciones: string[] = [];
  page.on('request', req => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) mutaciones.push(req.method());
  });
  await sesion(page, () => ({ estado: 200 }));
  for (const ancho of [320, 1280]) {
    await page.setViewportSize({ width: ancho, height: 844 });
    await page.goto('/plataforma/revision-amenazas-v5');
    await expect(page.getByRole('heading', { name: 'Verificaciones de acceso y abuso' })).toBeVisible();
    const t = await page.evaluate(() => {
      const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
      return nav ? nav.domContentLoadedEventEnd - nav.startTime : -1;
    });
    expect(t).toBeGreaterThan(0);
    expect(t).toBeLessThan(15000);
    await expect(page.locator('.threat-guide input, .threat-guide form')).toHaveCount(0);
  }
  expect(mutaciones).toEqual([]);
});
