import { expect, test, type Page } from '@playwright/test';

// C01-02.M04.06: Chromium real sobre UI local; sesión sintética exclusivamente
// para navegación de catálogo. NO acredita JWT firmado, RBAC, tenant real o SLA.
const routeUrl = '/plataforma/capacidades-v19';
const sessionMatch = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function sessionFixture(page: Page, status = 200, hold?: Promise<void>): Promise<{ calls: number }> {
  const traffic = { calls: 0 };
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0102-m0406-synthetic-not-signed-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionMatch, async request => {
    if (request.request().method() === 'OPTIONS') {
      await request.fulfill({ status: 204, headers: cors });
      return;
    }
    traffic.calls++;
    if (hold) await hold;
    await request.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: status === 200
        ? JSON.stringify({ success: true, data: {
            nombreUsuario: 'Solo pruebas de densidad', rolNombre: 'Plataforma',
            ambito: 'Plataforma', esAdministrador: true
          } })
        : JSON.stringify({ success: false, message: 'INTERNAL_PRIVATE_TRACE_NOT_FOR_UI' })
    });
  });
  return traffic;
}

test.describe('C01-02.M04.06 - densidad y carga progresiva del catálogo', () => {
  test.describe.configure({ retries: 0 });

  test('320/375/768/1280: tarjetas legibles, densidad acotada y sin desbordamiento', async ({ page }) => {
    const traffic = await sessionFixture(page);
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 820 });
      await page.goto(routeUrl);
      await expect(page.getByRole('heading', { level: 1, name: 'Catálogo global de módulos y capacidades' })).toBeVisible();
      const list = page.locator('[data-testid="c0102-listado"]');
      await expect(list.locator('li').first()).toBeVisible();
      const measured = await list.evaluate(element => {
        const main = element.closest('main');
        if (!main) throw new Error('main catalogo ausente');
        return {
          overflow: main.scrollWidth - main.clientWidth,
          count: element.querySelectorAll('li').length,
          cardWidths: Array.from(element.querySelectorAll('li'))
            .map(card => card.getBoundingClientRect().width),
          available: main.getBoundingClientRect().width
        };
      });
      expect(measured.overflow, 'sin desbordamiento horizontal interno a ' + width + 'px').toBeLessThanOrEqual(2);
      expect(measured.count, 'catálogo no vacío').toBeGreaterThan(0);
      expect(measured.count, 'tarjetas de catálogo acotadas').toBeLessThan(200);
      expect(measured.cardWidths.every(w => w > 0 && w <= measured.available + 2)).toBe(true);
    }
    expect(traffic.calls, 'la navegación debe revalidar acceso sintético').toBeGreaterThan(0);
  });

  test('sesión retenida: no muestra catálogo privado hasta completar la verificación progresiva', async ({ page }) => {
    let resume!: () => void;
    const hold = new Promise<void>(resolve => { resume = resolve; });
    const traffic = await sessionFixture(page, 200, hold);
    try {
      await page.goto(routeUrl, { waitUntil: 'domcontentloaded' });
      await expect.poll(() => traffic.calls).toBeGreaterThan(0);
      await expect(page.locator('main.catalogo')).toHaveCount(0);
    } finally {
      resume();
    }
    await expect(page.getByRole('heading', { level: 1, name: 'Catálogo global de módulos y capacidades' })).toBeVisible();
    await expect(page.locator('[data-testid="c0102-listado"] li').first()).toBeVisible();
    const navigation = await page.evaluate(() => {
      const perf = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
      return perf?.domContentLoadedEventEnd ?? -1;
    });
    expect(navigation, 'DOM ready local medido, no SLA real').toBeGreaterThan(0);
  });

  test('503 de sesión: falla cerrado y no filtra errores internos ni emite mutaciones', async ({ page }) => {
    const traffic = await sessionFixture(page, 503);
    const unsafe: string[] = [];
    page.on('request', request => {
      if (!['GET', 'OPTIONS', 'HEAD'].includes(request.method())) unsafe.push(request.method());
    });
    await page.goto(routeUrl, { waitUntil: 'domcontentloaded' });
    await expect.poll(() => traffic.calls).toBeGreaterThan(0);
    await expect(page.locator('main.catalogo')).toHaveCount(0);
    await expect(page.getByText('INTERNAL_PRIVATE_TRACE_NOT_FOR_UI')).toHaveCount(0);
    expect(unsafe, 'el catálogo informativo no muta datos por fallo de lectura').toEqual([]);
  });
});
