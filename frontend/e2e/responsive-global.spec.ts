import { expect, Page, test } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname } from 'node:path';
import type { RouteFixtureManifest } from './qa-route-fixtures';
import type { ApiResponse } from '../src/app/core/models/api-response.model';
import type { BancoLookup, MetodoPago } from '../src/app/core/models/metodo-pago.model';
import { REQUIRED_QA_WIDTHS, RESPONSIVE_ROUTES, RESPONSIVE_VIEWPORTS } from '../src/app/testing/qa-responsive-catalog';
export { RESPONSIVE_ROUTES, RESPONSIVE_VIEWPORTS } from '../src/app/testing/qa-responsive-catalog';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'];
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'];
const subset = process.env['QA_RESPONSIVE_WIDTHS']?.split(',').map(Number);
if (subset && (subset.some(width => !RESPONSIVE_VIEWPORTS.some(viewport => viewport.width === width)) || new Set(subset).size !== subset.length)) {
  throw new Error('QA_RESPONSIVE_WIDTHS contiene anchos inválidos o duplicados.');
}
const viewports = subset ? RESPONSIVE_VIEWPORTS.filter(viewport => subset.includes(viewport.width)) : RESPONSIVE_VIEWPORTS;

async function login(page: Page): Promise<void> {
  if (!ADMIN_USERNAME || !ADMIN_PASSWORD) throw new Error('Configura credenciales efímeras de CI para el barrido.');
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function bodyOverflow(page: Page): Promise<number> {
  return page.evaluate(() => Math.max(
    document.documentElement.scrollWidth,
    document.body?.scrollWidth ?? 0
  ) - document.documentElement.clientWidth);
}

async function overflowDiagnostic(page: Page): Promise<string> {
  return page.evaluate(() => {
    const root = document.documentElement;
    const width = root.clientWidth;
    const body = document.body;
    const offenders = Array.from(body.querySelectorAll<HTMLElement>('*')).flatMap(el => {
      const rect = el.getBoundingClientRect();
      if (rect.width <= 0 || rect.height <= 0 || rect.right <= width + 2) return [];
      const style = getComputedStyle(el);
      if (style.visibility === 'hidden' || style.display === 'none') return [];
      for (let parent = el.parentElement; parent && parent !== body; parent = parent.parentElement) {
        if (['auto', 'scroll', 'hidden', 'clip'].includes(getComputedStyle(parent).overflowX)) return [];
      }
      return [{ tag: el.tagName, cls: (el.getAttribute('class') ?? '').slice(0, 65), right: Math.round(rect.right) }];
    }).sort((a, b) => b.right - a.right).slice(0, 5);
    return JSON.stringify({ inner: innerWidth, client: width, rootScroll: root.scrollWidth,
      bodyScroll: body.scrollWidth, bodyWidth: Math.round(body.getBoundingClientRect().width),
      gutter: getComputedStyle(root).scrollbarGutter, offenders });
  });
}

async function auditGeometry(page: Page, route: string, mobile: boolean): Promise<void> {
  const result = await page.evaluate(({ route: currentRoute, mobile: isMobile }) => {
    const viewportWidth = document.documentElement.clientWidth;
    const visible = (element: Element): element is HTMLElement => {
      const html = element as HTMLElement;
      const rect = html.getBoundingClientRect();
      const style = getComputedStyle(html);
      return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
    };
    const inHorizontalScroller = (element: Element): boolean => {
      let parent = element.parentElement;
      while (parent) {
        const style = getComputedStyle(parent);
        if ((style.overflowX === 'auto' || style.overflowX === 'scroll') && parent.scrollWidth > parent.clientWidth + 2) return true;
        parent = parent.parentElement;
      }
      return false;
    };
    const outside: Array<Record<string, unknown>> = [];
    const clipped: Array<Record<string, unknown>> = [];
    const overlaps: Array<Record<string, unknown>> = [];
    const interactive = Array.from(document.querySelectorAll<HTMLElement>(
      'main a, main button, main input, main select, main textarea, main [role="button"], main h1, main h2, .topbar'
    )).filter(visible);
    for (const element of interactive) {
      if (inHorizontalScroller(element)) continue;
      const rect = element.getBoundingClientRect();
      if (rect.left < -2 || rect.right > viewportWidth + 2) {
        outside.push({ tag: element.tagName, className: element.className, text: (element.textContent ?? '').trim().slice(0, 100), left: rect.left, right: rect.right, viewportWidth });
      }
      const style = getComputedStyle(element);
      const hasText = (element.textContent ?? '').trim().length > 0;
      if (hasText && element.scrollWidth > element.clientWidth + 2 &&
          (style.overflowX === 'hidden' || style.textOverflow === 'ellipsis' || style.whiteSpace === 'nowrap')) {
        clipped.push({ tag: element.tagName, className: element.className, text: (element.textContent ?? '').trim().slice(0, 100), scrollWidth: element.scrollWidth, clientWidth: element.clientWidth });
      }
    }
    const tables = Array.from(document.querySelectorAll<HTMLTableElement>('main table')).filter(visible).flatMap((table) => {
      if (table.getBoundingClientRect().width <= viewportWidth + 2) return [];
      return inHorizontalScroller(table) ? [] : [{ width: table.getBoundingClientRect().width, viewportWidth }];
    });
    const badTargets = isMobile ? interactive.filter((element) => !inHorizontalScroller(element)).flatMap((element) => {
      if (!/^(BUTTON|A)$/.test(element.tagName)) return [];
      const rect = element.getBoundingClientRect();
      return rect.width < 36 || rect.height < 36 ? [{ tag: element.tagName, text: (element.textContent ?? '').trim().slice(0, 60), width: rect.width, height: rect.height }] : [];
    }) : [];
    const detailFields = Array.from(document.querySelectorAll<HTMLElement>('.detalle-row .mat-mdc-form-field')).filter(visible);
    if (isMobile) {
      for (const field of detailFields) {
        const rect = field.getBoundingClientRect();
        if (rect.width < 120) overlaps.push({ kind: 'detail-field-too-narrow', label: field.querySelector('mat-label')?.textContent?.trim(), width: rect.width });
      }
    }
    for (let index = 0; index < detailFields.length; index += 1) {
      const first = detailFields[index].getBoundingClientRect();
      for (let next = index + 1; next < detailFields.length; next += 1) {
        const second = detailFields[next].getBoundingClientRect();
        const width = Math.min(first.right, second.right) - Math.max(first.left, second.left);
        const height = Math.min(first.bottom, second.bottom) - Math.max(first.top, second.top);
        if (width > 4 && height > 4) overlaps.push({ kind: 'detail-field-overlap', first: index, second: next, width, height });
      }
    }
    const hiddenInteractive = Array.from(document.querySelectorAll<HTMLElement>('main button, main a, main input, main select, main textarea')).filter((element) => element.getAttribute('aria-hidden') === 'true' && visible(element)).map((element) => element.outerHTML.slice(0, 160));
    return { currentRoute, outside, clipped, overlaps, tables, badTargets, hiddenInteractive };
  }, { route, mobile });

  expect(result.outside, `Controles/encabezados fuera del viewport en ${route}: ${JSON.stringify(result.outside)}`).toEqual([]);
  expect(result.clipped, `Contenido crítico recortado en ${route}: ${JSON.stringify(result.clipped)}`).toEqual([]);
  expect(result.overlaps, `Campos estrechos o montados en ${route}: ${JSON.stringify(result.overlaps)}`).toEqual([]);
  expect(result.tables, `Tabla sin shell horizontal en ${route}: ${JSON.stringify(result.tables)}`).toEqual([]);
  expect(result.badTargets, `Acciones táctiles pequeñas en ${route}: ${JSON.stringify(result.badTargets)}`).toEqual([]);
  expect(result.hiddenInteractive, `Controles interactivos ocultos semánticamente en ${route}`).toEqual([]);
}

async function assertNavigationMode(page: Page, mobile: boolean): Promise<void> {
  const menu = page.locator('#menu-toggle');
  const sidebar = page.locator('#main-sidebar');
  if (mobile) {
    await expect(menu).toBeVisible();
    await expect(sidebar).not.toHaveClass(/abierto/);
    await menu.click();
    await expect(sidebar).toHaveClass(/abierto/);
    await expect(page.locator('.overlay')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(sidebar).not.toHaveClass(/abierto/);
    await expect(page.locator('.overlay')).toBeHidden();
  } else {
    await expect(menu).toBeHidden();
    await expect(sidebar).toBeVisible();
    const width = await sidebar.evaluate((element) => element.getBoundingClientRect().width);
    expect(width).toBeGreaterThan(180);
  }
}

// Wait for the actual browser requests, not a fixture-side catalogue read or
// an enabled attribute that can precede NgModel/options rendering.
async function navigateWithPaymentCatalogs(page: Page, route: string) {
  const metodo = process.env.QA_ROUTE_FIXTURE_PAYMENT_METHOD;
  if (!metodo) throw new Error('Falta método de pago empresarial acreditado');
  const waitCatalog = (path: string) => page.waitForResponse(response => {
    const url = new URL(response.url());
    return ['localhost', '127.0.0.1'].includes(url.hostname) && url.port === '5005' &&
      url.pathname === path && response.request().method() === 'GET';
  }, { timeout: 10_000 });
  const [methodsResponse, banksResponse, navigation] = await Promise.all([
    waitCatalog('/metodos-pago/activos'),
    waitCatalog('/metodos-pago/bancos-activos'),
    page.goto(route, { waitUntil: 'domcontentloaded' })
  ]);
  expect(methodsResponse.ok(), 'HTTP catálogo de métodos de pago: ' + methodsResponse.status()).toBe(true);
  expect(banksResponse.ok(), 'HTTP catálogo de bancos: ' + banksResponse.status()).toBe(true);
  const methods: ApiResponse<MetodoPago[]> = await methodsResponse.json();
  const banks: ApiResponse<BancoLookup[]> = await banksResponse.json();
  expect(methods.success === true && Array.isArray(methods.data) &&
    methods.data.some(item => item.activo === true && item.nombre === metodo),
    'El catálogo recibido por el navegador debe incluir el método empresarial activo acreditado').toBe(true);
  expect(banks.success === true && Array.isArray(banks.data),
    'El navegador debe recibir el catálogo de bancos válido, incluso cuando esté vacío').toBe(true);
  return navigation;
}

async function auditFirstSelectOverlay(page: Page, route: string): Promise<void> {
  const invoicePayment = /^\/facturas\/\d+\/pagos$/.test(route);
  const firstSelect = page.locator('main mat-select').first();
  if (!invoicePayment && (await firstSelect.count() === 0 || !(await firstSelect.isVisible()))) return;
  let select = firstSelect;
  if (invoicePayment) {
    // The invoice and its payment/bank catalogues load independently.
    // A visible arrow is not an enabled Material combobox. Require the real
    // registration control and wait for its own readiness, not a fixed delay.
    select = page.locator('main .formulario mat-select').first();
    await expect(select, 'Método de pago de la factura en ' + route).toBeVisible();
    await expect(select, 'Catálogo de pago listo en ' + route).toBeEnabled();
    await expect(select, 'Selector de pago habilitado en ' + route).toHaveAttribute('aria-disabled', 'false');
    // A selected value is rendered only after real active options are installed.
    // Waiting for the value avoids clicking during the empty combobox phase.
    await expect(select.locator('.mat-mdc-select-value-text'),
      'Método de pago renderizado en ' + route).toHaveText(/\S/);
  }
  // On the stock edit route, physical identity is deliberately immutable.
  // Check those controls really are disabled, then exercise an editable select.
  if (/^\/inventario\/existencias\/\d+\/editar$/.test(route)) {
    for (const name of ['productoId', 'productoVarianteId', 'almacenId']) {
      const locked = page.locator('main mat-select[name="' + name + '"]');
      await expect(locked).toHaveAttribute('aria-disabled', 'true');
      await expect(locked).toHaveAttribute('aria-expanded', 'false');
    }
    select = page.locator('main mat-select[aria-disabled="false"]:visible').first();
    await expect(select, 'Selector editable de ubicación en ' + route).toBeVisible();
  }
  // The arrow is a real pointer target of Material's combobox. Clicking the
  // center of an empty control can hit its floating label instead.
  let panel = page.locator('.mat-mdc-select-panel').first();
  if (invoicePayment) {
    // The rendered value gives the real Material trigger a stable pointer target.
    await select.locator('.mat-mdc-select-trigger').click();
    await expect(select, 'Combobox de pago expandido en ' + route).toHaveAttribute('aria-expanded', 'true');
    const panelId = await select.getAttribute('aria-controls');
    if (!panelId) throw new Error('El combobox de pago abierto no declara su panel');
    panel = page.getByRole('listbox').and(page.locator('.mat-mdc-select-panel[id]').filter({
      has: page.getByRole('option')
    }));
    await expect(panel, 'Panel exclusivo de pago en ' + route).toHaveCount(1);
    await expect(panel, 'Relación accesible del panel de pago en ' + route).toHaveAttribute('id', panelId);
  } else {
    await select.locator('.mat-mdc-select-arrow-wrapper').click();
  }
  await expect(panel, `El select no abrió panel en ${route}`).toBeVisible();
  if (invoicePayment && process.env.QA_RESPONSIVE_WIDTHS) {
    const metodo = process.env.QA_ROUTE_FIXTURE_PAYMENT_METHOD;
    if (!metodo) throw new Error('Falta método de pago empresarial acreditado');
    await expect(panel.getByRole('option', { name: metodo, exact: true }),
      'El método de pago persistido debe aparecer en el selector de la factura').toBeVisible();
  }
  const geometry = await panel.evaluate((element) => {
    const rect = element.getBoundingClientRect();
    const viewportWidth = document.documentElement.clientWidth;
    const clippedOptions = Array.from(element.querySelectorAll<HTMLElement>('.mat-mdc-option')).filter((option) => {
      const optionRect = option.getBoundingClientRect();
      return optionRect.right > viewportWidth + 2 || optionRect.left < -2 || option.scrollWidth > option.clientWidth + 2;
    }).length;
    return { left: rect.left, right: rect.right, width: rect.width, viewportWidth, clippedOptions };
  });
  expect(geometry.left, `Panel select fuera por la izquierda en ${route}`).toBeGreaterThanOrEqual(-2);
  expect(geometry.right, `Panel select fuera por la derecha en ${route}`).toBeLessThanOrEqual(geometry.viewportWidth + 2);
  expect(geometry.width, `Panel select ilegible en ${route}`).toBeGreaterThanOrEqual(Math.min(320, geometry.viewportWidth - 24));
  expect(geometry.clippedOptions, `Opciones de select recortadas en ${route}`).toBe(0);
  await page.keyboard.press('Escape');
  await expect(panel).toBeHidden();
}

function assertNoRouteErrors(route: string, errors: string[]): void {
  expect(errors, 'Errores definitivos en ' + route + ': ' + errors.join(' | ')).toEqual([]);
}

async function certifyRoute(page: Page, route: string, mobile: boolean, errors: string[]): Promise<void> {
  errors.length = 0;
  const response = process.env.QA_RESPONSIVE_WIDTHS && /^\/facturas\/\d+\/pagos$/.test(route)
    ? await navigateWithPaymentCatalogs(page, route)
    : await page.goto(route, { waitUntil: 'domcontentloaded' });
  expect(response?.ok(), `HTTP no válido en ${route}`).toBe(true);
  const esperado = route === '/' ? '/login' : route;
  await expect.poll(() => new URL(page.url()).pathname.replace(/\/$/, '') || '/', {
    message: `Ruta solicitada ${route}: no aceptar redirecciones inesperadas`, timeout: 15_000
  }).toBe(esperado);
  assertNoRouteErrors(route, errors);
  const heading = esperado === '/login' ? page.locator('form[aria-labelledby="login-title"]')
    : route.startsWith('/tienda') ? page.locator('main h1').first() : page.locator('#main-content h1').first();
  if (esperado !== '/login') {
    await expect(route.startsWith('/tienda') ? page.locator('main').first() : page.locator('#main-content')).toBeVisible();
  }
  // A recorded HTTP/console error is already fatal under the existing contract.
  // Do not wait another 15s for a heading that cannot load after that error.
  await expect.poll(async () => errors.length > 0 || await heading.isVisible(), {
    timeout: 15_000, intervals: [100, 250, 500], message: 'Contenido principal visible en ' + route
  }).toBe(true);
  assertNoRouteErrors(route, errors);
  await expect(heading).toBeVisible();
  // Evitar medir texto durante la descarga de fuentes; sus errores siguen siendo FAIL.
  await page.evaluate(async () => {
    let timer: ReturnType<typeof setTimeout> | undefined;
    try {
      await Promise.race([document.fonts.ready, new Promise<never>((_, reject) => {
        timer = setTimeout(() => reject(new Error('Fuentes no listas en 10 segundos')), 10_000);
      })]);
    } finally { clearTimeout(timer); }
  });
  assertNoRouteErrors(route, errors);
  try {
    await expect.poll(async () => errors.length > 0 || await bodyOverflow(page) <= 2, { timeout: 5_000 }).toBe(true);
  } catch {
    throw new Error('Desborde de documento en ' + route + ': ' + await overflowDiagnostic(page));
  }
  assertNoRouteErrors(route, errors);
  if (route.startsWith('/tienda')) await auditGeometry(page, route, mobile);
  if (route !== '/' && !route.startsWith('/tienda') && route !== '/login') {
    await auditGeometry(page, route, mobile);
    const superficies = await page.evaluate(() => {
      const probe = document.createElement('span');
      probe.style.backgroundColor = 'var(--color-surface)';
      document.body.append(probe);
      const esperado = getComputedStyle(probe).backgroundColor;
      probe.remove();
      return [...document.querySelectorAll<HTMLElement>('.app-list-surface, .mat-mdc-card')]
        .filter(el => el.getBoundingClientRect().width > 0 && el.getBoundingClientRect().height > 0 && getComputedStyle(el).visibility !== 'hidden')
        .filter(el => getComputedStyle(el).backgroundColor !== esperado)
        .map(el => ({ clase: el.className, color: getComputedStyle(el).backgroundColor, esperado }));
    });
    expect(superficies, 'Superficies que no respetan el token empresarial en ' + route).toEqual([]);
    if (process.env.QA_RESPONSIVE_WIDTHS && (route === '/compras/nueva' || route === '/ventas/nueva')) {
      // These forms deliberately fetch product options on search, rather than
      // preloading the entire catalogue. Exercise the genuine remote search.
      const sku = process.env.QA_ROUTE_FIXTURE_PRODUCT_SKU;
      if (!sku) throw new Error('Falta SKU acreditado para búsqueda comercial');
      if (route === '/ventas/nueva') {
        await page.getByRole('checkbox', { name: /Exonerar excepcionalmente/ }).check();
        await page.locator('textarea[formcontrolname="motivoExoneracionEnvio"]').fill('Retiro en mostrador QA');
      }
      const tipo = route === '/ventas/nueva' ? 'venta' : 'compra';
      await page.getByTestId(tipo + '-producto-autocomplete').fill(sku);
      const option = page.locator('.mat-mdc-autocomplete-panel .mat-mdc-option').filter({ hasText: sku });
      await expect(option).toHaveCount(1);
      await expect(option).toBeVisible();
      const productPanel = page.locator('.mat-mdc-autocomplete-panel');
      await expect(productPanel).toBeVisible();
      const panelRect = await productPanel.evaluate(element => {
        const rect = element.getBoundingClientRect();
        return { left: rect.left, right: rect.right, viewport: document.documentElement.clientWidth };
      });
      expect(panelRect.left, 'Búsqueda comercial fuera del viewport por la izquierda').toBeGreaterThanOrEqual(-2);
      expect(panelRect.right, 'Búsqueda comercial fuera del viewport por la derecha').toBeLessThanOrEqual(panelRect.viewport + 2);
      await option.click();
      await expect(productPanel).toBeHidden();
      await expect(page.locator('main mat-select[formcontrolname="productoId"]').first()).not.toHaveClass(/mat-mdc-select-empty/);
      await auditGeometry(page, route, mobile);
      try {
        await expect.poll(() => bodyOverflow(page), { timeout: 5_000,
          message: 'Desborde tras búsqueda comercial en ' + route }).toBeLessThanOrEqual(2);
      } catch {
        throw new Error('Desborde tras búsqueda comercial en ' + route + ': ' + await overflowDiagnostic(page));
      }
      assertNoRouteErrors(route, errors);
    }
    await auditFirstSelectOverlay(page, route);
    if (route === '/ventas/nueva') {
      const priceInputs = page.locator('main input[formcontrolname="precioUnitario"]');
      await expect(priceInputs.first(), 'Debe existir un precio unitario de catálogo').toBeVisible();
      for (let index = 0; index < await priceInputs.count(); index += 1) {
        // readonly is an HTML boolean attribute; Material serializes it as "true".
        // Verify the browser property, not a particular attribute spelling.
        await expect(priceInputs.nth(index), 'El precio unitario debe ser solo lectura').toHaveJSProperty('readOnly', true);
        await expect(priceInputs.nth(index), 'El precio unitario no debe ser editable').not.toBeEditable();
        await expect(priceInputs.nth(index), 'El precio unitario debe declarar lectura').toHaveAttribute('aria-readonly', 'true');
      }
    }
  }
  const caracteresInvalidos = await page.locator('body').innerText().then(texto => /\uFFFD|Ã[¡©­³º±]/.test(texto));
  expect(caracteresInvalidos, 'Texto con codificación defectuosa en ' + route).toBe(false);
  expect(new URL(page.url()).pathname.replace(/\/$/, '') || '/', `Redirección tardía en ${route}`).toBe(esperado);
  expect(errors, `Errores de consola en ${route}: ${errors.join(' | ')}`).toEqual([]);
}

test.describe('Responsive global — route sweep y regression gate', () => {
  test.describe.configure({ mode: process.env.QA_RESPONSIVE_WIDTHS ? 'parallel' : 'default', retries: 0 });

  test('el catálogo compartido no repite rutas y cubre los anchos QA', () => {
    expect(new Set(RESPONSIVE_ROUTES).size).toBe(RESPONSIVE_ROUTES.length);
    const widths = RESPONSIVE_VIEWPORTS.map(viewport => Number(viewport.width));
    expect(new Set(widths).size).toBe(widths.length);
    expect(REQUIRED_QA_WIDTHS.filter(width => !widths.includes(width))).toEqual([]);
  });

  for (const viewport of viewports) {
    test(`${viewport.name}x${viewport.height} recorre todas las rutas reales`, async ({ page, browser }, testInfo) => {
      test.setTimeout(45 * 60_000);
      const size = { width: viewport.width, height: viewport.height };
      await page.setViewportSize(size);
      page.setDefaultTimeout(8000);
      page.setDefaultNavigationTimeout(12000);
      const manifest: RouteFixtureManifest = JSON.parse(process.env.QA_ROUTE_FIXTURE_MANIFEST ?? '{}');
      if (process.env.QA_RESPONSIVE_WIDTHS && !process.env.QA_ROUTE_FIXTURE_MANIFEST) throw new Error('Falta manifest de fixtures reales.');
      const resultados: { ruta: string; urlProbada?: string; estado: 'PASS' | 'FAIL' | 'NOT_RUN'; motivo?: string }[] =
        RESPONSIVE_ROUTES.map(ruta => ({ ruta, estado: 'NOT_RUN' }));
      const errors: string[] = [];
      const guestErrors: string[] = [];
      const monitor = (target: Page, destino: string[]) => {
        target.on('console', (message) => { if (message.type() === 'error') destino.push(message.text()); });
        target.on('pageerror', (error) => destino.push(`pageerror: ${error.message}`));
        target.on('request', request => {
          const url = new URL(request.url());
          const header = request.headers()['x-empresa-id'];
          if (header && url.origin !== 'http://127.0.0.1:5005' && url.origin !== 'http://localhost:5005') {
            destino.push('Cabecera de tenant fuera de API local: ' + url.origin);
          }
        });
        target.on('response', response => {
          const url = new URL(response.url());
          if (['localhost', '127.0.0.1'].includes(url.hostname) && url.port === '5005' && response.status() >= 400) {
            destino.push('API ' + response.status() + ' ' + url.pathname);
          }
        });
      };
      monitor(page, errors);
      const guestContext = await browser.newContext({ baseURL: testInfo.project.use.baseURL, viewport: size });
      try {
        const guest = await guestContext.newPage();
        guest.setDefaultTimeout(8000);
        guest.setDefaultNavigationTimeout(12000);
        monitor(guest, guestErrors);
        let falloLogin: string | undefined;
        try {
          await login(page);
          await assertNavigationMode(page, viewport.mobile);
        } catch (error) {
          falloLogin = error instanceof Error ? error.message.slice(0, 500) : 'No se pudo abrir una sesión de prueba.';
        }
        for (const resultado of resultados) {
          const route = resultado.ruta;
          const publico = route === '/' || route === '/login' || route.startsWith('/tienda');
          if (!publico && falloLogin) {
            resultado.motivo = falloLogin;
            continue;
          }
          const fixture = manifest[route];
          if (fixture && 'error' in fixture) {
            resultado.estado = 'FAIL';
            resultado.motivo = fixture.error;
            continue;
          }
          const urlProbada = fixture && 'url' in fixture ? fixture.url : route;
          resultado.urlProbada = urlProbada;
          let isolated: Page | undefined;
          try {
            let target = publico ? guest : page;
            let routeErrors = publico ? guestErrors : errors;
            // A late response from a prior page must not be attributed to the next route.
            // Use the already authenticated browser context; never inject or bypass auth.
            if (process.env.QA_RESPONSIVE_WIDTHS) {
              isolated = await target.context().newPage();
              target = isolated;
              await target.setViewportSize(size);
              target.setDefaultTimeout(8000);
              target.setDefaultNavigationTimeout(12000);
              routeErrors = [];
              monitor(target, routeErrors);
            }
            await test.step(route, () => certifyRoute(target, urlProbada, viewport.mobile, routeErrors));
            resultado.estado = 'PASS';
          } catch (error) {
            resultado.estado = 'FAIL';
            resultado.motivo = error instanceof Error ? error.message.slice(0, 1600) : 'Falló una comprobación de la ruta.';
          } finally {
            if (isolated) await isolated.close();
          }
        }
      } finally {
        const path = testInfo.outputPath('qa-route-audit.json');
        mkdirSync(dirname(path), { recursive: true });
        writeFileSync(path, JSON.stringify({ width: viewport.width, sha: process.env['GITHUB_SHA'] ?? null, resultados }, null, 2));
        await guestContext.close();
      }
      // Include the precise failure in CI output as well as the JSON artifact.
      expect(resultados.filter(resultado => resultado.estado !== 'PASS')
        .map(({ ruta, estado, motivo }) => ({ ruta, estado, motivo }))).toEqual([]);
    });
  }
});
