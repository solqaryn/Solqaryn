import { expect, test, type Page } from '@playwright/test';
const target = '/plataforma/trazabilidad-cobertura-v5';
const sessionPattern = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
type Mode = 'allow' | 'forbidden' | 'unavailable';
async function session(page: Page, mode: Mode): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1501-m0506-synthetic-NOT-JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionPattern, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    const status = mode === 'allow' ? 200 : mode === 'forbidden' ? 403 : 503;
    const body = mode === 'allow'
      ? { success: true, data: { nombreUsuario: 'Admin de prueba', rolNombre: 'Admin',
          ambito: 'Plataforma', esAdministrador: true } }
      : { success: false, message: 'PRIVATE_C1501_SECRET_NEVER_RENDER' };
    await route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(body) });
  });
}

test.describe('C15-01.M05.06 regresion rendimiento y accesibilidad proporcionales', () => {
  test.describe.configure({ retries: 0 });
  test('tres navegaciones reales miden tiempo, reflow y cero escrituras', async ({ page }) => {
    await session(page, 'allow');
    const samples: Array<{ width: number; elapsedMs: number; dclMs: number }> = [];
    const writes: string[] = [];
    page.on('request', request => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) writes.push(request.method());
    });
    for (const width of [320, 768, 1280]) {
      await page.setViewportSize({ width, height: 812 });
      const t0 = Date.now();
      await page.goto(target, { waitUntil: 'domcontentloaded' });
      await expect(page.getByTestId('c1501-loaded')).toBeVisible();
      const elapsedMs = Date.now() - t0;
      const dclMs = await page.evaluate(() =>
        (performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming)
          .domContentLoadedEventEnd);
      expect(Number.isFinite(dclMs) && dclMs >= 0).toBe(true);
      expect(elapsedMs, 'budget laboratorio local ' + width).toBeLessThan(25000);
      const overflow = await page.evaluate(() =>
        document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(overflow, 'desbordamiento ' + width).toBeLessThanOrEqual(2);
      await expect(page.locator('.coverage-list li')).toHaveCount(4);
      await expect(page.locator('.coverage-shell [role="status"]')).toContainText('NO_MEDIDO');
      samples.push({ width, elapsedMs, dclMs });
    }
    expect(writes, 'solo lectura').toEqual([]);
    console.log('C15-01.M05.06 LAB_NAVIGATION_MS ' + JSON.stringify(samples));
    console.log('LIMIT: CI localhost no es Core Web Vitals ni rendimiento de produccion');
  });
  test('accesibilidad: Tab real, foco 3px, blanco 44px y estados no certificados', async ({ page }) => {
    await session(page, 'allow');
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto(target);
    await expect(page.getByTestId('c1501-loaded')).toBeVisible();
    await expect(page.locator('section.coverage-shell[aria-labelledby="coverage-title"]')).toHaveCount(1);
    await expect(page.locator('section[aria-labelledby="coverage-dimensions"]')).toHaveCount(1);
    await expect(page.locator('.unverified[aria-label="Evidencia no verificada"]')).toHaveCount(4);
    await expect(page.getByRole('note')).toContainText('No hay operaciones');
    const back = page.getByRole('link', { name: 'Regresar a administración de plataforma' });
    let reached = false;
    for (let i = 0; i < 100; i++) {
      await page.keyboard.press('Tab');
      if (await back.evaluate(el => document.activeElement === el)) { reached = true; break; }
    }
    expect(reached, 'Tab real sin .focus()').toBe(true);
    const m = await back.evaluate(el => {
      const rect = el.getBoundingClientRect();
      return { width: rect.width, height: rect.height,
        outline: parseFloat(getComputedStyle(el).outlineWidth) };
    });
    expect(m.width).toBeGreaterThanOrEqual(44);
    expect(m.height).toBeGreaterThanOrEqual(44);
    expect(m.outline).toBeGreaterThanOrEqual(3);
    await expect(page.locator('.coverage-shell button, .coverage-shell form')).toHaveCount(0);
  });
  test('403 y 503 no filtran dimensiones ni mensajes privados; acceso sano sigue visible', async ({ browser }) => {
    for (const mode of ['forbidden', 'unavailable'] as const) {
      const page = await browser.newPage();
      try {
        await session(page, mode);
        await page.goto(target);
        await expect(page).toHaveURL(/\/login(?:\?|$)/);
        await expect(page.locator('.coverage-list li')).toHaveCount(0);
        await expect(page.getByText('PRIVATE_C1501_SECRET_NEVER_RENDER')).toHaveCount(0);
      } finally { await page.close(); }
    }
    const page = await browser.newPage();
    try {
      await session(page, 'allow');
      await page.goto(target);
      await expect(page.getByTestId('c1501-loaded')).toBeVisible();
      await expect(page.locator('.coverage-list li')).toHaveCount(4);
      await expect(page.locator('.coverage-shell [role="status"]')).toContainText('NO_MEDIDO');
    } finally { await page.close(); }
  });
});
