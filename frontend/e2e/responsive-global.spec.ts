import { expect, Page, test } from '@playwright/test';
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';

/**
 * This is the one route list used by the responsive regression gate.  Every
 * parameter is represented by a harmless, read-only fixture id.  The list is
 * deliberately kept here (rather than in a feature spec) so adding a route
 * without adding it to the gate fails the route catalog check below.
 */
export const RESPONSIVE_ROUTES = [
  '/', '/login',
  '/tienda', '/tienda/productos', '/tienda/categorias',
  '/tienda/carrito', '/tienda/checkout', '/tienda/producto/demo',
  '/tienda/categoria/demo', '/tienda/pedido/1',
  '/dashboard', '/productos', '/productos/nuevo', '/productos/1', '/productos/1/editar', '/productos/1/variantes',
  '/categorias', '/categorias/nueva', '/categorias/1/editar',
  '/sucursales', '/sucursales/nueva', '/sucursales/1/editar',
  '/almacenes', '/almacenes/nuevo', '/almacenes/1/editar',
  '/ubicaciones-almacen', '/ubicaciones-almacen/nueva', '/ubicaciones-almacen/1/editar',
  '/colores', '/tallas', '/marcas', '/modelos', '/metodos-pago',
  '/proveedores', '/proveedores/nuevo', '/proveedores/1/editar',
  '/clientes', '/clientes/nuevo', '/clientes/1/editar',
  '/tipo-clientes', '/tipo-clientes/nuevo', '/tipo-clientes/1/editar',
  '/usuarios', '/usuarios/1', '/usuarios/1/editar',
  '/roles', '/roles/nuevo', '/roles/1/editar', '/permisos',
  '/descuentos', '/descuentos/nuevo', '/descuentos/1/editar',
  '/impuestos', '/impuestos/nuevo', '/impuestos/1/editar',
  '/costos-envio', '/cargas-masivas', '/auditoria', '/configuracion', '/periodos-contables', '/perfil',
  '/solicitudes-compra', '/ordenes-compra', '/ordenes-compra/nueva', '/ordenes-compra/1/editar',
  '/compras', '/compras/nueva', '/compras/1', '/compras/1/editar',
  '/facturas-proveedor', '/facturas-proveedor/nueva', '/facturas-proveedor/1', '/facturas-proveedor/1/editar',
  '/compras/1/three-way-match', '/devoluciones-proveedor', '/devoluciones-proveedor/nueva', '/devoluciones-proveedor/1', '/devoluciones-proveedor/1/editar',
  '/notas-credito-proveedor', '/notas-credito-proveedor/nueva', '/notas-credito-proveedor/1', '/notas-credito-proveedor/1/editar', '/evaluaciones-proveedor',
  '/recepciones-compra', '/recepciones-compra/nueva', '/recepciones-compra/1',
  '/ventas', '/ventas/nueva', '/ventas/1', '/ventas/1/editar',
  '/cotizaciones', '/cotizaciones/nueva', '/cotizaciones/1', '/cotizaciones/1/editar',
  '/pedidos-venta', '/pedidos-venta/nuevo', '/pedidos-venta/1', '/pedidos-venta/1/editar', '/pedidos-venta/1/preparacion', '/preparaciones-pedido-venta',
  '/devoluciones-clientes', '/devoluciones-clientes/nueva', '/devoluciones-clientes/1',
  '/notas-credito-cliente', '/notas-credito-cliente/nueva', '/notas-credito-cliente/1',
  '/cuentas-por-cobrar', '/cuentas-por-pagar', '/facturas/1', '/facturas/1/pagos',
  '/finanzas', '/plan-cuentas', '/cuentas-bancarias', '/asientos-contables', '/estados-financieros', '/centros-costo',
  '/centro-reportes', '/centro-reportes/financieros', '/centro-reportes/administrativos',
  '/centro-reportes/inventario/valorizacion', '/centro-reportes/inventario/kardex', '/centro-reportes/inventario/stock-health', '/centro-reportes/inventario/reconciliacion',
  '/centro-reportes/ventas', '/centro-reportes/compras', '/centro-reportes/rentabilidad',
  '/inventario/movimientos', '/inventario/costeo', '/inventario/transferencias', '/inventario/transferencias/nueva', '/inventario/transferencias/1', '/inventario/transferencias/1/editar',
  '/inventario/conteos', '/inventario/conteos/nuevo', '/inventario/conteos/1', '/inventario/conteos/1/editar',
  '/inventario/reservas', '/inventario/reservas/nueva', '/inventario/reservas/1', '/inventario/reservas/1/editar',
  '/inventario/ajustes', '/inventario/ajustes/nuevo', '/inventario/ajustes/1', '/inventario/ajustes/1/editar',
  '/inventario/existencias', '/inventario/existencias/nueva', '/inventario/existencias/1/editar', '/inventario/trazabilidad/1'
] as const;

export const RESPONSIVE_VIEWPORTS = [
  { name: '320', width: 320, height: 568, mobile: true },
  { name: '360', width: 360, height: 800, mobile: true },
  { name: '375', width: 375, height: 812, mobile: true },
  { name: '390', width: 390, height: 844, mobile: true },
  { name: '412', width: 412, height: 915, mobile: true },
  { name: '430', width: 430, height: 932, mobile: true },
  { name: '768', width: 768, height: 1024, mobile: true },
  { name: '1024', width: 1024, height: 768, mobile: false },
  { name: '1440', width: 1440, height: 900, mobile: false }
] as const;

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

function routeSourceFiles(directory: string): string[] {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) return routeSourceFiles(path);
    return entry.isFile() && entry.name.endsWith('.routes.ts') ? [path] : [];
  });
}

function canonicalRoute(path: string): string {
  return path.split('/').filter(Boolean).map((part) => part.startsWith(':') ? (
    part.toLowerCase().includes('slug') ? 'demo' : '1'
  ) : part).join('/').replace(/^/, '/');
}

function sourceRouteCatalog(): string[] {
  const root = join(process.cwd(), 'src', 'app');
  const paths = routeSourceFiles(root).flatMap((path) => {
    const source = readFileSync(path, 'utf8');
    return [...source.matchAll(/path\s*:\s*['"]([^'"]*)['"]/g)].map((match) => match[1]);
  });
  return [...new Set(paths.filter((path) => path && path !== '**').map((path) => {
    // The report center declares its children relative to the parent route.
    // Keep that relationship explicit so the source catalog compares the
    // same URL that the browser actually navigates to.
    const canonical = canonicalRoute(path);
    const reportChild = new Set([
      '/financieros', '/administrativos', '/inventario/valorizacion',
      '/inventario/kardex', '/inventario/stock-health', '/inventario/reconciliacion',
      '/ventas', '/compras', '/rentabilidad'
    ]);
    return reportChild.has(canonical) ? `/centro-reportes${canonical}` : canonical;
  }))];
}

async function login(page: Page): Promise<void> {
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

async function auditFirstSelectOverlay(page: Page, route: string): Promise<void> {
  const select = page.locator('main mat-select').first();
  if (await select.count() === 0 || !(await select.isVisible())) return;
  await select.click();
  const panel = page.locator('.mat-mdc-select-panel').first();
  await expect(panel, `El select no abrió panel en ${route}`).toBeVisible();
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

async function certifyRoute(page: Page, route: string, mobile: boolean, errors: string[]): Promise<void> {
  errors.length = 0;
  await page.goto(route, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(150);
  if (route === '/' || route.startsWith('/tienda') || route === '/login') {
    await expect(page.locator('body')).toBeVisible();
  } else {
    await expect(page.locator('#main-content')).toBeVisible();
    await expect(page.locator('#main-content h1').first()).toBeVisible({ timeout: 15_000 });
  }
  await expect.poll(() => bodyOverflow(page), { timeout: 5_000 }).toBeLessThanOrEqual(2);
  if (route !== '/' && !route.startsWith('/tienda') && route !== '/login') {
    await auditGeometry(page, route, mobile);
    await auditFirstSelectOverlay(page, route);
    if (route === '/ventas/nueva') {
      const priceInputs = page.locator('main input[formcontrolname="precioUnitario"]');
      for (let index = 0; index < await priceInputs.count(); index += 1) {
        await expect(priceInputs.nth(index), 'El precio unitario debe ser solo lectura').toHaveAttribute('readonly', '');
        await expect(priceInputs.nth(index), 'El precio unitario debe declarar lectura').toHaveAttribute('aria-readonly', 'true');
      }
    }
  }
  expect(errors, `Errores de consola en ${route}: ${errors.join(' | ')}`).toEqual([]);
}

test.describe('Responsive global — route sweep y regression gate', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test('inventario de rutas permanece cubierto por el gate', () => {
    const listed = new Set(RESPONSIVE_ROUTES.map(canonicalRoute));
    const missing = sourceRouteCatalog().filter((route) => !listed.has(route));
    expect(missing, `Rutas nuevas sin cobertura responsive: ${missing.join(', ')}`).toEqual([]);
  });

  for (const viewport of RESPONSIVE_VIEWPORTS) {
    test(`${viewport.name}x${viewport.height} recorre todas las rutas reales`, async ({ page }) => {
      test.setTimeout(15 * 60_000);
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      const errors: string[] = [];
      page.on('console', (message) => { if (message.type() === 'error') errors.push(message.text()); });
      page.on('pageerror', (error) => errors.push(`pageerror: ${error.message}`));
      await login(page);
      await assertNavigationMode(page, viewport.mobile);
      for (const route of RESPONSIVE_ROUTES) await certifyRoute(page, route, viewport.mobile, errors);
    });
  }
});
