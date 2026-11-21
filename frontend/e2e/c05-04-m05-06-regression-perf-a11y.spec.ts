import { expect, test, type Page, type Route } from '@playwright/test';

// Fixtures are synthetic. Chromium viewport emulation is NOT a physical device,
// and intercepted requests cannot certify real JWT/RBAC/MySQL or field Web Vitals.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function session(page: Page, allowed = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0504-m0506-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    await route.fulfill({
      status: allowed ? 200 : 403, headers: cors, contentType: 'application/json',
      body: JSON.stringify(allowed
        ? { success: true, data: { nombreUsuario: 'Fixture', rolNombre: 'Admin',
          ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'PRIVATE_C0504_SESSION_SENTINEL' })
    });
  });
}

function payload(count: number) {
  return {
    success: true,
    data: {
      items: Array.from({ length: count }, (_, i) => ({
        id: i + 1, nombre: 'Empresa de prueba ' + (i + 1),
        activa: true, usuariosActivos: i + 1
      })),
      pagina: 1, tamano: 25, total: count, puedeCrear: false
    }
  };
}

async function directory(page: Page, handler: (route: Route) => Promise<void>) {
  await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    await handler(route);
  });
}

test.describe('C05-04.M05.06 regresión transversal de ligereza', () => {
  test.describe.configure({ retries: 0 });

  test('carga 25 filas, 4 viewports sin overflow ni GET duplicado y registra medición local', async ({ page }) => {
    await session(page);
    let reads = 0;
    const writes: string[] = [];
    page.on('request', request => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
    });
    await directory(page, async route => {
      reads++;
      await route.fulfill({ status: 200, headers: cors,
        contentType: 'application/json', body: JSON.stringify(payload(25)) });
    });
    await page.goto('/plataforma');
    await page.locator('details.ui-lightness-v5 > summary').click();
    const panel = page.locator('.ui-lightness-v5');
    await expect(page.getByRole('table', { name: 'Empresas registradas' })).toBeVisible();
    await expect(page.locator('table[aria-label="Empresas registradas"] tbody tr')).toHaveCount(25);
    await expect(page.getByTestId('c0504-success')).toContainText('25 empresas');
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 800 });
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth
      );
      expect(overflow, 'horizontal overflow at viewport ' + width).toBeLessThanOrEqual(2);
      const toggle = panel.getByRole('button', { name: 'Reducir detalles' });
      const rect = await toggle.boundingBox();
      expect(rect, 'visible density toggle at ' + width).not.toBeNull();
      expect(rect!.height, 'touch target 44px at ' + width).toBeGreaterThanOrEqual(44);
    }
    await panel.getByRole('button', { name: 'Reducir detalles' }).click();
    await expect(panel.locator('.ui-lightness-v5__checks li')).toHaveCount(0);
    await panel.getByRole('button', { name: 'Ver criterios' }).click();
    await expect(panel.locator('.ui-lightness-v5__checks li')).toHaveCount(3);
    expect(reads, 'visual presentation never refetches the directory').toBe(1);
    expect(writes, 'read-only lightness surface does not mutate').toEqual([]);
    const timing = await page.evaluate(() => {
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
      const paint = performance.getEntriesByName('first-contentful-paint')[0];
      return { domContentLoadedMs: navigation?.domContentLoadedEventEnd ?? null,
        firstContentfulPaintMs: paint?.startTime ?? null };
    });
    console.log('C0504_M0506_CHROMIUM_LOCAL_METRICS=' + JSON.stringify(timing));
    // The above measurements are diagnostic only, NOT field Core Web Vitals or device validation.
  });

  test('carga retenida es progresiva; acciones visuales no duplican el GET', async ({ page }) => {
    await session(page);
    let release!: () => void;
    const gate = new Promise<void>(resolve => { release = resolve; });
    let reads = 0;
    await directory(page, async route => {
      reads++;
      await gate;
      await route.fulfill({ status: 200, headers: cors,
        contentType: 'application/json', body: JSON.stringify(payload(0)) });
    });
    await page.goto('/plataforma');
    await page.locator('details.ui-lightness-v5 > summary').click();
    await expect(page.getByTestId('c0504-loading')).toBeVisible();
    const panel = page.locator('.ui-lightness-v5');
    await panel.getByRole('button', { name: 'Reducir detalles' }).click();
    await expect(panel.locator('.ui-lightness-v5__checks li')).toHaveCount(0);
    expect(reads, 'one in-flight load despite compact toggle').toBe(1);
    release();
    await expect(page.getByTestId('c0504-empty')).toBeVisible();
    await expect(page.getByTestId('c0504-success')).toHaveCount(0);
    expect(reads).toBe(1);
  });

  test('503 no revela diagnóstico ni conserva filas y solo reintenta a demanda', async ({ page }) => {
    await session(page);
    const writes: string[] = [];
    page.on('request', request => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
    });
    let reads = 0;
    await directory(page, async route => {
      reads++;
      await route.fulfill({ status: reads === 1 ? 503 : 200,
        headers: cors, contentType: 'application/json',
        body: reads === 1
          ? JSON.stringify({ success: false, message: 'C0504_PRIVATE_PROVIDER_TRACE' })
          : JSON.stringify(payload(0)) });
    });
    await page.goto('/plataforma');
    await page.locator('details.ui-lightness-v5 > summary').click();
    await expect(page.getByTestId('c0504-error')).toBeVisible();
    await expect(page.getByText('C0504_PRIVATE_PROVIDER_TRACE')).toHaveCount(0);
    await expect(page.getByTestId('c0504-success')).toHaveCount(0);
    await page.locator('.ui-lightness-v5').getByRole('button', { name: 'Reducir detalles' }).click();
    expect(reads, 'display toggle cannot retry automatically').toBe(1);
    await page.getByTestId('c0504-feedback').getByRole('button', { name: 'Reintentar directorio' }).click();
    await expect(page.getByTestId('c0504-empty')).toBeVisible();
    expect(reads, 'one explicit retry').toBe(2);
    expect(writes).toEqual([]);
  });

  test('sesión 403 y anónimo no consultan ni muestran superficie privada', async ({ page }) => {
    let reads = 0;
    await directory(page, async route => {
      reads++;
      await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
        body: JSON.stringify(payload(1)) });
    });
    await page.goto('/plataforma');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    expect(reads).toBe(0);
    await session(page, false);
    await page.goto('/plataforma');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_C0504_SESSION_SENTINEL')).toHaveCount(0);
    expect(reads, 'denied route cannot fetch company directory').toBe(0);
  });

  test('teclado real Tab y Enter sin foco programatico, sin duplicar GET', async ({ page }) => {
    await session(page);
    let reads = 0;
    await directory(page, async route => {
      reads++;
      await route.fulfill({ status: 200, headers: cors,
        contentType: 'application/json', body: JSON.stringify(payload(2)) });
    });
    await page.goto('/plataforma');
    const summary = page.locator('details.ui-lightness-v5 > summary');
    await expect(summary).toBeVisible();
    let summaryKeyboardReachable = false;
    for (let tab = 0; tab < 80; tab++) {
      await page.keyboard.press('Tab');
      if (await summary.evaluate(element => document.activeElement === element)) {
        summaryKeyboardReachable = true;
        break;
      }
    }
    expect(summaryKeyboardReachable, 'summary accesible con Tab real desde inicio').toBe(true);
    await page.keyboard.press('Enter');
    await expect(page.getByRole('table', { name: 'Empresas registradas' })).toBeVisible();
    const button = page.locator('.ui-lightness-v5').getByRole('button', { name: 'Reducir detalles' });
    let buttonKeyboardReachable = false;
    for (let tab = 0; tab < 35; tab++) {
      await page.keyboard.press('Tab');
      if (await button.evaluate(element => document.activeElement === element)) {
        buttonKeyboardReachable = true;
        break;
      }
    }
    expect(buttonKeyboardReachable, 'alternar densidad con Tab real, sin focus()').toBe(true);
    await page.keyboard.press('Enter');
    await expect(page.locator('.ui-lightness-v5__checks li')).toHaveCount(0);
    expect(reads, 'navegacion teclado nunca vuelve a cargar empresas').toBe(1);
  });
});
