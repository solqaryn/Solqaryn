import { expect, test, type Page } from '@playwright/test';

const route = '/plataforma/revision-arquitectura-v20';
const cors = { 'Access-Control-Allow-Origin': 'http://127.0.0.1:4200', 'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id', 'Access-Control-Allow-Methods': 'GET, OPTIONS' };
async function setupSession(page: Page, allowed: boolean): Promise<void> {
  // A synthetic UI fixture, never a real JWT/RBAC/tenant certification.
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1502m0406-ui-only');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async handler => {
    if (handler.request().method() === 'OPTIONS') {
      await handler.fulfill({ status: 204, headers: cors });
      return;
    }
    await handler.fulfill({ status: allowed ? 200 : 403, headers: cors, contentType: 'application/json', body: JSON.stringify(allowed
      ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true } }
      : { success: false, message: 'PRIVATE_PROVIDER_DIAGNOSTIC_DO_NOT_RENDER' }) });
  });
}
test('C15-02.M04.06 cinco tarjetas legibles sin desborde ni escritura, carga medida', async ({ page }) => {
  await setupSession(page, true);
  const writes: string[] = [];
  page.on('request', req => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(req.method())) writes.push(req.method());
  });
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto(route);
    const view = page.getByTestId('c1502-review');
    await expect(view).toBeVisible();
    await expect(view.getByTestId('architecture-item')).toHaveCount(5);
    await expect(view.getByText('NO_VERIFICADO')).toHaveCount(5);
    const metrics = await view.evaluate(element => {
      const viewport = document.documentElement.clientWidth;
      const cards = Array.from(element.querySelectorAll<HTMLElement>('[data-testid="architecture-item"]'));
      const boxes = cards.map(card => card.getBoundingClientRect());
      const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
      const fcp = performance.getEntriesByType('paint').find(p => p.name === 'first-contentful-paint');
      return {
        cards: boxes.length,
        offscreen: boxes.some(b => b.width < 1 || b.left < -1 || b.right > viewport + 1),
        overflow: document.documentElement.scrollWidth - viewport,
        chars: cards.reduce((n, item) => n + (item.textContent?.trim().length ?? 0), 0),
        dcl: nav ? nav.domContentLoadedEventEnd - nav.startTime : -1,
        localFcp: fcp?.startTime ?? null
      };
    });
    expect(metrics.cards).toBe(5);
    expect(metrics.offscreen, 'tarjetas dentro de viewport ' + width).toBe(false);
    expect(metrics.overflow, 'sin desborde a ' + width).toBeLessThanOrEqual(2);
    expect(metrics.chars).toBeGreaterThan(100);
    expect(metrics.chars).toBeLessThan(3000);
    expect(metrics.dcl, 'métrica real de laboratorio').toBeGreaterThanOrEqual(0);
    expect(metrics.dcl, 'presupuesto conservador del runner').toBeLessThan(30000);
    console.log('C1502_M0406_LOCAL_PERFORMANCE', JSON.stringify({ width, ...metrics, fieldCwv: 'NO_MEDIDO' }));
  }
  expect(writes, 'sin métodos HTTP mutantes').toEqual([]);
});
test('C15-02.M04.06 denegación sintética no filtra tarjetas ni diagnósticos', async ({ page }) => {
  await setupSession(page, false);
  await page.goto(route);
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByTestId('architecture-item')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_PROVIDER_DIAGNOSTIC_DO_NOT_RENDER')).toHaveCount(0);
});
