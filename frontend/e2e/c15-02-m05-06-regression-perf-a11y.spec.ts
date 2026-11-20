import { expect, test, type Page } from '@playwright/test';

const route = '/plataforma/revision-arquitectura-v20';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function sesionLocal(page: Page, autorizada: boolean): Promise<void> {
  // Fixture UI local. No acredita JWT firmado, RBAC backend, MySQL ni proveedores.
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1502m0506-ui-fixture-no-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async routeHandler => {
    if (routeHandler.request().method() === 'OPTIONS') {
      await routeHandler.fulfill({ status: 204, headers: cors });
      return;
    }
    await routeHandler.fulfill({
      status: autorizada ? 200 : 403,
      headers: cors,
      contentType: 'application/json',
      body: JSON.stringify(autorizada
        ? { success: true, data: { nombreUsuario: 'Auditor CI', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'PRIVATE_INTERNAL_DIAGNOSTIC_C1502' })
    });
  });
}

test.describe('C15-02.M05.06 regresion, rendimiento de laboratorio y accesibilidad proporcional', () => {
  test.describe.configure({ retries: 0 });

  test('cuatro viewports reales de Chromium: 5 tarjetas, sin overflow, carga medida y sin escrituras', async ({ page }) => {
    await sesionLocal(page, true);
    const metodosMutantes: string[] = [];
    page.on('request', request => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) metodosMutantes.push(request.method());
    });
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 900 });
      await page.goto(route);
      const panel = page.getByTestId('c1502-review');
      await expect(panel).toBeVisible();
      await expect(panel.getByRole('heading', { level: 1, name: 'Revisión de arquitectura e integraciones' })).toBeVisible();
      await expect(panel.getByTestId('architecture-item')).toHaveCount(5);
      await expect(panel.getByText('NO_VERIFICADO')).toHaveCount(5);
      const metricas = await panel.evaluate(element => {
        const viewport = document.documentElement.clientWidth;
        const items = Array.from(element.querySelectorAll<HTMLElement>('[data-testid="architecture-item"]'));
        const fuera = items.some(item => {
          const rect = item.getBoundingClientRect();
          return rect.width <= 0 || rect.left < -1 || rect.right > viewport + 1;
        });
        const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
        const fcp = performance.getEntriesByType('paint').find(entry => entry.name === 'first-contentful-paint');
        return {
          viewport, overflow: document.documentElement.scrollWidth - viewport,
          fuera, cantidad: items.length,
          dclMs: nav ? nav.domContentLoadedEventEnd - nav.startTime : -1,
          fcpLaboratorioMs: fcp?.startTime ?? null
        };
      });
      expect(metricas.viewport).toBeGreaterThanOrEqual(width - 2);
      expect(metricas.cantidad).toBe(5);
      expect(metricas.fuera).toBe(false);
      expect(metricas.overflow).toBeLessThanOrEqual(2);
      expect(metricas.dclMs).toBeGreaterThanOrEqual(0);
      expect(metricas.dclMs).toBeLessThan(30_000);
      if (metricas.fcpLaboratorioMs !== null) expect(metricas.fcpLaboratorioMs).toBeLessThan(30_000);
      console.log('C1502_M0506_CHROMIUM_LAB', JSON.stringify({ width, ...metricas, LCP_CLS_INP_FIELD: 'NO_MEDIDO' }));
    }
    expect(metodosMutantes, 'la consulta de arquitectura no puede emitir HTTP mutante').toEqual([]);
  });

  test('landmarks, labels y foco real de teclado tras recarga, sin focus programatico', async ({ page }) => {
    await sesionLocal(page, true);
    await page.setViewportSize({ width: 375, height: 820 });
    await page.goto(route);
    await page.reload();
    const panel = page.getByTestId('c1502-review');
    await expect(panel).toHaveAttribute('aria-labelledby', 'review-title');
    await expect(panel.locator('#review-title')).toHaveCount(1);
    await expect(panel.getByRole('heading', { level: 2, name: 'Superficies por contrastar' })).toBeVisible();
    await expect(panel.getByTestId('architecture-item')).toHaveCount(5);
    const volver = page.getByRole('link', { name: 'Volver a plataforma' });
    const caja = await volver.boundingBox();
    expect(caja?.height ?? 0, 'objetivo tactil minimo 44px').toBeGreaterThanOrEqual(44);
    let tabAlcanzoEnlace = false;
    for (let attempt = 0; attempt < 20; attempt++) {
      await page.keyboard.press('Tab');
      tabAlcanzoEnlace = await volver.evaluate(element => element === document.activeElement);
      if (tabAlcanzoEnlace) break;
    }
    expect(tabAlcanzoEnlace, 'enlace accesible usando Tab sin .focus()').toBe(true);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/plataforma(?:\?|$)/);
  });

  test('denegacion 403 local oculta inventario y diagnosticos internos', async ({ page }) => {
    await sesionLocal(page, false);
    await page.goto(route);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByTestId('architecture-item')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_INTERNAL_DIAGNOSTIC_C1502')).toHaveCount(0);
    await expect(page.getByText('NO_VERIFICADO')).toHaveCount(0);
  });
});
