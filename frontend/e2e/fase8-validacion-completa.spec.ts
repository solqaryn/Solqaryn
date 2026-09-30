import { test, expect, APIRequestContext, Page, TestInfo } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const EXPECT_SWAGGER_DISABLED = process.env['PHASE8_EXPECT_SWAGGER_DISABLED'] === 'true';

const administrativeRoutes = [
  '/dashboard',
  '/productos',
  '/categorias',
  '/colores',
  '/tallas',
  '/marcas',
  '/modelos',
  '/compras',
  '/proveedores',
  '/ventas',
  '/clientes',
  '/finanzas',
  '/inventario/movimientos',
  '/cargas-masivas',
  '/usuarios',
  '/roles',
  '/permisos',
  '/descuentos',
  '/impuestos',
  '/costos-envio',
  '/auditoria',
  '/configuracion',
  '/perfil'
];

const criticalRoutes = [
  '/dashboard',
  '/productos',
  '/compras',
  '/ventas',
  '/costos-envio',
  '/cargas-masivas',
  '/auditoria',
  '/configuracion'
];

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(url => url.pathname !== '/login', { timeout: 20_000 });
}

async function loginApi(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, {
    data: { nombreUsuario: ADMIN_USERNAME, password: ADMIN_PASSWORD }
  });
  expect(response.status(), await response.text()).toBe(200);
  const payload = await response.json();
  return (payload.data ?? payload.Data).token;
}

async function domAudit(page: Page): Promise<{
  overflow: number;
  duplicateIds: string[];
  imagesWithoutAlt: string[];
  unnamedControls: string[];
  h1Count: number;
  mainCount: number;
}> {
  return page.evaluate(() => {
    const visible = (element: Element): boolean => {
      const htmlElement = element as HTMLElement;
      const style = getComputedStyle(htmlElement);
      return style.display !== 'none'
        && style.visibility !== 'hidden'
        && Number(style.opacity) > 0
        && htmlElement.getBoundingClientRect().width > 0
        && htmlElement.getBoundingClientRect().height > 0;
    };

    const ids = Array.from(document.querySelectorAll<HTMLElement>('[id]'))
      .map(element => element.id)
      .filter(Boolean);
    const duplicateIds = [...new Set(ids.filter((id, index) => ids.indexOf(id) !== index))];

    const imagesWithoutAlt = Array.from(document.querySelectorAll<HTMLImageElement>('img'))
      .filter(visible)
      .filter(image => !image.hasAttribute('alt') || image.alt.trim().length === 0)
      .map(image => image.outerHTML.slice(0, 160));

    const unnamedControls = Array.from(document.querySelectorAll<HTMLElement>('input:not([type="hidden"]), select, textarea, button'))
      .filter(visible)
      .filter(control => {
        const ariaLabel = control.getAttribute('aria-label')?.trim();
        if (ariaLabel) return false;

        const labelledBy = control.getAttribute('aria-labelledby')?.trim();
        if (labelledBy) {
          const text = labelledBy
            .split(/\s+/)
            .map(id => document.getElementById(id)?.textContent?.trim() ?? '')
            .join(' ')
            .trim();
          if (text) return false;
        }

        const materialHost = control.closest<HTMLElement>('mat-slide-toggle, mat-checkbox, mat-radio-button');
        if (materialHost?.getAttribute('aria-label')?.trim()) return false;
        const hostLabelledBy = materialHost?.getAttribute('aria-labelledby')?.trim();
        if (hostLabelledBy) {
          const hostText = hostLabelledBy
            .split(/\s+/)
            .map(id => document.getElementById(id)?.textContent?.trim() ?? '')
            .join(' ')
            .trim();
          if (hostText) return false;
        }

        const id = control.id;
        if (id && document.querySelector(`label[for="${CSS.escape(id)}"]`)?.textContent?.trim()) return false;
        if (control.closest('label')?.textContent?.trim()) return false;
        if (control.closest('mat-form-field')?.querySelector('mat-label')?.textContent?.trim()) return false;
        if (control.textContent?.trim()) return false;
        if (control.getAttribute('title')?.trim()) return false;
        return true;
      })
      .map(control => control.outerHTML.slice(0, 200));

    const documentWidth = Math.max(
      document.documentElement.scrollWidth,
      document.body?.scrollWidth ?? 0
    );

    return {
      overflow: documentWidth - document.documentElement.clientWidth,
      duplicateIds,
      imagesWithoutAlt,
      unnamedControls,
      h1Count: document.querySelectorAll('h1').length,
      mainCount: document.querySelectorAll('main#main-content').length
    };
  });
}

async function attachJson(testInfo: TestInfo, name: string, value: unknown): Promise<void> {
  await testInfo.attach(name, {
    body: Buffer.from(JSON.stringify(value, null, 2), 'utf8'),
    contentType: 'application/json'
  });
}

function percentile(values: number[], percentileValue: number): number {
  const sorted = [...values].sort((a, b) => a - b);
  const index = Math.min(sorted.length - 1, Math.ceil((percentileValue / 100) * sorted.length) - 1);
  return sorted[Math.max(0, index)];
}

test.describe('Fase 8 — validación completa automatizada', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test('rutas protegidas, autenticación y cabeceras de seguridad responden correctamente', async ({ request, page }) => {
    await page.goto('/productos');
    await expect(page).toHaveURL(/\/login$/);

    const unauthorized = await request.get(`${API_URL}/productos?page=1&pageSize=1`);
    expect(unauthorized.status()).toBe(401);

    const invalidToken = await request.get(`${API_URL}/productos?page=1&pageSize=1`, {
      headers: { Authorization: 'Bearer token-invalido-fase-8' }
    });
    expect(invalidToken.status()).toBe(401);

    const health = await request.get(`${API_URL}/health`);
    expect(health.status(), await health.text()).toBe(200);
    const headers = health.headers();
    expect(headers['x-content-type-options']).toBe('nosniff');
    expect(headers['x-frame-options']).toBe('DENY');
    expect(headers['referrer-policy']).toBe('no-referrer');
    expect(headers['permissions-policy']).toContain('camera=()');
    expect(headers['permissions-policy']).toContain('microphone=()');
    expect(headers['permissions-policy']).toContain('geolocation=()');

    if (EXPECT_SWAGGER_DISABLED) {
      const swagger = await request.get(`${API_URL}/swagger/index.html`);
      expect(swagger.status()).toBe(404);
    }
  });

  test('todos los módulos principales cargan sin errores, 5xx, desbordamiento ni defectos semánticos básicos', async ({ page }, testInfo) => {
    const consoleErrors: string[] = [];
    const pageErrors: string[] = [];
    const failedRequests: string[] = [];
    const serverErrors: string[] = [];
    const warnings: string[] = [];

    page.on('console', message => {
      if (message.type() === 'error') consoleErrors.push(message.text());
      if (message.type() === 'warning') warnings.push(message.text());
    });
    page.on('pageerror', error => pageErrors.push(error.message));
    page.on('requestfailed', request => {
      const url = request.url();
      if (url.startsWith('http://127.0.0.1:4200') || url.startsWith(API_URL)) {
        failedRequests.push(`${request.method()} ${url}: ${request.failure()?.errorText ?? 'error desconocido'}`);
      }
    });
    page.on('response', response => {
      const url = response.url();
      if ((url.startsWith('http://127.0.0.1:4200') || url.startsWith(API_URL)) && response.status() >= 500) {
        serverErrors.push(`${response.status()} ${response.request().method()} ${url}`);
      }
    });

    await login(page);
    const audits: Record<string, Awaited<ReturnType<typeof domAudit>>> = {};

    for (const route of administrativeRoutes) {
      await page.goto(route);
      await expect(page).toHaveURL(new RegExp(`${route.replaceAll('/', '\\/')}$`));
      await expect(page.locator('main#main-content')).toBeVisible();
      await expect(page.locator('h1').first()).toBeVisible();
      const audit = await domAudit(page);
      audits[route] = audit;
      expect(audit.overflow, `Desbordamiento horizontal en ${route}`).toBeLessThanOrEqual(2);
      expect(audit.duplicateIds, `IDs duplicados en ${route}`).toEqual([]);
      expect(audit.imagesWithoutAlt, `Imágenes sin texto alternativo en ${route}`).toEqual([]);
      expect(audit.unnamedControls, `Controles sin nombre accesible en ${route}`).toEqual([]);
      expect(audit.h1Count, `La ruta ${route} debe tener exactamente un h1`).toBe(1);
      expect(audit.mainCount, `La ruta ${route} debe tener un único main principal`).toBe(1);
    }

    await attachJson(testInfo, 'fase8-dom-audit.json', { audits, warnings });
    expect(pageErrors, `Errores JavaScript: ${pageErrors.join(' | ')}`).toEqual([]);
    expect(consoleErrors, `Errores de consola: ${consoleErrors.join(' | ')}`).toEqual([]);
    expect(failedRequests, `Solicitudes locales fallidas: ${failedRequests.join(' | ')}`).toEqual([]);
    expect(serverErrors, `Respuestas 5xx: ${serverErrors.join(' | ')}`).toEqual([]);
  });

  test('las pantallas críticas mantienen estructura en 320x568 y 3840x2160', async ({ page }, testInfo) => {
    await login(page);
    const results: Array<{ route: string; width: number; height: number; overflow: number }> = [];

    for (const viewport of [
      { width: 320, height: 568 },
      { width: 3840, height: 2160 }
    ]) {
      await page.setViewportSize(viewport);
      for (const route of criticalRoutes) {
        await page.goto(route);
        await expect(page.locator('main#main-content')).toBeVisible();
        await expect(page.locator('h1').first()).toBeVisible();
        const audit = await domAudit(page);
        expect(audit.overflow, `${route} desborda a ${viewport.width}x${viewport.height}`).toBeLessThanOrEqual(2);
        expect(audit.duplicateIds, `${route} duplica IDs a ${viewport.width}x${viewport.height}`).toEqual([]);
        results.push({ route, ...viewport, overflow: audit.overflow });
      }
    }

    await attachJson(testInfo, 'fase8-responsive-audit.json', results);
  });

  test('el enlace para saltar contenido funciona mediante teclado', async ({ page }) => {
    await login(page);
    await page.goto('/dashboard');
    const main = page.locator('main#main-content');
    const skip = page.getByRole('link', { name: 'Saltar al contenido principal' });

    // AppComponent enfoca main de forma asíncrona tras NavigationEnd. Esperar ese
    // foco productivo antes de crear el sentinel evita una carrera entre callbacks.
    await expect(main).toBeFocused();

    // Desde un origen E2E determinista, Tab debe avanzar al primer control real:
    // el skip-link, sin modificar el comportamiento accesible de producción.
    await page.evaluate(() => {
      document.getElementById('e2e-focus-sentinel')?.remove();
      const sentinel = document.createElement('button');
      sentinel.id = 'e2e-focus-sentinel';
      sentinel.type = 'button';
      sentinel.textContent = 'Inicio de navegación E2E';
      sentinel.style.position = 'fixed';
      sentinel.style.left = '-10000px';
      document.body.insertBefore(sentinel, document.body.firstChild);
      sentinel.focus();
    });
    await expect(page.locator('#e2e-focus-sentinel')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(skip).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(main).toBeFocused();
  });

  test('formularios críticos exponen nombres accesibles y mensajes sin detalles internos', async ({ page, request }) => {
    await login(page);

    for (const route of ['/productos/nuevo', '/compras/nueva', '/ventas/nueva']) {
      await page.goto(route);
      await expect(page.locator('h1').first()).toBeVisible();
      const audit = await domAudit(page);
      expect(audit.duplicateIds, `IDs duplicados en ${route}`).toEqual([]);
      expect(audit.unnamedControls, `Controles sin nombre accesible en ${route}`).toEqual([]);
      expect(audit.overflow, `Desbordamiento en ${route}`).toBeLessThanOrEqual(2);
    }

    const token = await loginApi(request);
    const response = await request.get(`${API_URL}/productos/2147483647`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(response.status()).toBe(404);
    const body = await response.text();
    expect(body).not.toMatch(/MySql|Pomelo|stack| at Solqaryn|System\./i);
  });

  test('API y navegación cumplen presupuestos de rendimiento controlados', async ({ page, request }, testInfo) => {
    const token = await loginApi(request);
    const headers = { Authorization: `Bearer ${token}` };
    const endpoints = [
      `${API_URL}/productos?page=1&pageSize=10`,
      `${API_URL}/ventas?page=1&pageSize=10`,
      `${API_URL}/compras?page=1&pageSize=10`,
      `${API_URL}/dashboard/resumen`
    ];

    const apiSamples: Record<string, number[]> = {};
    for (const endpoint of endpoints) {
      const samples: number[] = [];
      for (let i = 0; i < 3; i += 1) {
        const start = performance.now();
        const response = await request.get(endpoint, { headers });
        const elapsed = performance.now() - start;
        expect(response.status(), `${endpoint}: ${await response.text()}`).toBe(200);
        samples.push(elapsed);
      }
      apiSamples[endpoint] = samples;
      expect(percentile(samples, 95), `p95 de ${endpoint}`).toBeLessThan(1_500);
    }

    await login(page);
    const navigationSamples: Record<string, number> = {};
    for (const route of ['/dashboard', '/productos', '/ventas', '/compras']) {
      const start = performance.now();
      await page.goto(route);
      await expect(page.locator('h1').first()).toBeVisible();
      const elapsed = performance.now() - start;
      navigationSamples[route] = elapsed;
      expect(elapsed, `Navegación ${route}`).toBeLessThan(5_000);
    }

    await attachJson(testInfo, 'fase8-performance.json', { apiSamples, navigationSamples });
  });
});
