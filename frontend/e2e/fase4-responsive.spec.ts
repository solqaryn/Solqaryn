import { test, expect, Page } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

const viewports = [
  { name: 'telefono-pequeno', width: 320, height: 568, kind: 'mobile' },
  { name: 'telefono-grande', width: 430, height: 932, kind: 'mobile' },
  { name: 'tablet-vertical', width: 768, height: 1024, kind: 'mobile' },
  { name: 'tablet-horizontal', width: 1024, height: 768, kind: 'desktop' },
  { name: 'laptop', width: 1366, height: 768, kind: 'desktop' },
  { name: 'full-hd', width: 1920, height: 1080, kind: 'desktop' },
  { name: '2k', width: 2560, height: 1440, kind: 'desktop' },
  { name: '4k', width: 3840, height: 2160, kind: 'desktop' }
] as const;

const routes = [
  '/dashboard',
  '/productos',
  '/productos/nuevo',
  '/categorias',
  '/categorias/nueva',
  '/colores',
  '/tallas',
  '/marcas',
  '/modelos',
  '/compras',
  '/compras/nueva',
  '/proveedores',
  '/proveedores/nuevo',
  '/ventas',
  '/ventas/nueva',
  '/clientes',
  '/clientes/nuevo',
  '/finanzas',
  '/inventario/movimientos',
  '/usuarios',
  '/roles',
  '/roles/nuevo',
  '/permisos',
  '/descuentos',
  '/descuentos/nuevo',
  '/impuestos',
  '/impuestos/nuevo',
  '/auditoria',
  '/configuracion',
  '/perfil'
] as const;

const representativeRoutes = [
  { route: '/dashboard', name: 'dashboard' },
  { route: '/productos', name: 'productos' },
  { route: '/productos/nuevo', name: 'producto-form' },
  { route: '/ventas/nueva', name: 'venta-form' }
] as const;

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await expect(page.locator('input[formcontrolname="nombreUsuario"]')).toBeVisible();
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function documentOverflow(page: Page): Promise<number> {
  return page.evaluate(() => {
    const width = Math.max(
      document.documentElement.scrollWidth,
      document.body?.scrollWidth ?? 0
    );
    return width - document.documentElement.clientWidth;
  });
}

async function assertElementsInsideViewport(page: Page, route: string): Promise<void> {
  const failures = await page.locator([
    'main#main-content h1:visible',
    '.page-subtitle:visible',
    '.header-row:visible',
    '.form-actions:visible',
    '.item-card-actions:visible',
    'mat-paginator:visible',
    '.topbar:visible',
    '.app-footer:visible'
  ].join(',')).evaluateAll((elements) => elements.flatMap((element) => {
    const html = element as HTMLElement;
    const rect = html.getBoundingClientRect();
    const viewportWidth = document.documentElement.clientWidth;
    if (rect.width <= 0 || rect.height <= 0) return [];

    const outside = rect.left < -2 || rect.right > viewportWidth + 2;
    return outside ? [{
      tag: html.tagName,
      className: html.className,
      text: (html.textContent ?? '').trim().slice(0, 100),
      left: rect.left,
      right: rect.right,
      viewportWidth
    }] : [];
  }));

  expect(failures, `Elementos fuera del viewport en ${route}: ${JSON.stringify(failures)}`).toEqual([]);
}

async function assertNoClippedCriticalText(page: Page, route: string): Promise<void> {
  const failures = await page.locator([
    'main#main-content h1:visible',
    '.page-subtitle:visible',
    '.card h2:visible',
    '.mat-mdc-form-field-hint:visible',
    '.mat-mdc-form-field-error:visible',
    '.item-card strong:visible',
    '.item-card-actions .mdc-button__label:visible',
    '.form-actions .mdc-button__label:visible',
    '.topbar .user-name:visible',
    '.topbar .user-role:visible'
  ].join(',')).evaluateAll((elements) => elements.flatMap((element) => {
    const html = element as HTMLElement;
    const style = getComputedStyle(html);
    const horizontalOverflow = html.scrollWidth - html.clientWidth > 2;
    const verticalOverflow = html.scrollHeight - html.clientHeight > 2;
    const clips = style.overflow === 'hidden'
      || style.overflowX === 'hidden'
      || style.overflowY === 'hidden'
      || style.textOverflow === 'ellipsis';

    if (!horizontalOverflow && !verticalOverflow) return [];
    if (!clips && style.whiteSpace !== 'nowrap') return [];

    return [{
      tag: html.tagName,
      className: html.className,
      text: (html.textContent ?? '').trim().slice(0, 100),
      scrollWidth: html.scrollWidth,
      clientWidth: html.clientWidth,
      scrollHeight: html.scrollHeight,
      clientHeight: html.clientHeight,
      overflow: style.overflow,
      whiteSpace: style.whiteSpace,
      textOverflow: style.textOverflow
    }];
  }));

  expect(failures, `Textos críticos recortados en ${route}: ${JSON.stringify(failures)}`).toEqual([]);
}

async function assertTouchTargets(page: Page, route: string, minimum: number): Promise<void> {
  const failures = await page.locator([
    'button:visible:not([disabled]):not(.mdc-switch)',
    'a[mat-button]:visible',
    'a[mat-flat-button]:visible',
    'a[mat-icon-button]:visible',
    'button[mat-icon-button]:visible',
    '.menu-toggle:visible',
    '.profile-button:visible'
  ].join(',')).evaluateAll(
    (elements, min) => elements.flatMap((element) => {
      const html = element as HTMLElement;
      if (html.closest('mat-slide-toggle')) return [];

      const rect = html.getBoundingClientRect();
      if (rect.width <= 0 || rect.height <= 0) return [];

      const tooSmall = rect.width < min || rect.height < min;
      return tooSmall ? [{
        tag: html.tagName,
        className: html.className,
        ariaLabel: html.getAttribute('aria-label'),
        text: (html.textContent ?? '').trim().slice(0, 60),
        width: rect.width,
        height: rect.height
      }] : [];
    }),
    minimum
  );

  expect(failures, `Controles menores de ${minimum}px en ${route}: ${JSON.stringify(failures)}`).toEqual([]);
}

async function assertNavigationMode(page: Page, kind: 'mobile' | 'desktop'): Promise<void> {
  const menuToggle = page.locator('.menu-toggle');
  const sidebar = page.locator('.sidebar');

  if (kind === 'mobile') {
    await expect(menuToggle).toBeVisible();
    await expect(sidebar).not.toHaveClass(/abierto/);
    await menuToggle.click();
    await expect(sidebar).toHaveClass(/abierto/);
    await expect(page.locator('.overlay')).toBeVisible();
    await expect.poll(async () => (await sidebar.boundingBox())?.x ?? -999).toBeGreaterThanOrEqual(-1);

    const sidebarGeometry = await sidebar.evaluate((element) => {
      const rect = element.getBoundingClientRect();
      return {
        left: rect.left,
        right: rect.right,
        width: rect.width,
        viewportWidth: document.documentElement.clientWidth,
        clientHeight: element.clientHeight
      };
    });
    expect(sidebarGeometry.left).toBeGreaterThanOrEqual(-1);
    expect(sidebarGeometry.right).toBeLessThanOrEqual(sidebarGeometry.viewportWidth + 1);
    expect(sidebarGeometry.width).toBeLessThanOrEqual(sidebarGeometry.viewportWidth);
    expect(sidebarGeometry.clientHeight).toBeGreaterThan(0);

    const closeButton = page.locator('.cerrar-sidebar');
    await expect(closeButton).toBeVisible();
    await closeButton.click();
    await expect(sidebar).not.toHaveClass(/abierto/);
    await expect(page.locator('.overlay')).toBeHidden();
  } else {
    await expect(menuToggle).toBeHidden();
    const geometry = await sidebar.evaluate((element) => {
      const rect = element.getBoundingClientRect();
      return { left: rect.left, width: rect.width };
    });
    expect(geometry.left).toBeGreaterThanOrEqual(-1);
    expect(geometry.width).toBeGreaterThan(180);
  }
}

async function certifyRoute(
  page: Page,
  route: string,
  viewportName: string,
  mobile: boolean
): Promise<void> {
  await page.goto(route);
  await expect(page).toHaveURL(new RegExp(`${route.replaceAll('/', '\\/')}$`));
  await expect(page.locator('main#main-content')).toBeVisible();
  await expect(page.locator('main#main-content h1').first()).toBeVisible();
  await page.waitForTimeout(100);

  expect(await documentOverflow(page), `Desbordamiento horizontal en ${route} (${viewportName})`).toBeLessThanOrEqual(2);
  await assertElementsInsideViewport(page, route);
  await assertNoClippedCriticalText(page, route);
  await assertTouchTargets(page, route, mobile ? 36 : 32);
}

test.describe('Fase 4 - matriz responsive exhaustiva', () => {
  test.describe.configure({ retries: 0 });

  for (const viewport of viewports) {
    test(`${viewport.name} ${viewport.width}x${viewport.height} mantiene todos los módulos utilizables`, async ({ page }) => {
      test.setTimeout(180_000);
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await login(page);
      await assertNavigationMode(page, viewport.kind);

      const consoleErrors: string[] = [];
      const pageErrors: string[] = [];
      page.on('console', (message) => {
        if (message.type() === 'error') consoleErrors.push(message.text());
      });
      page.on('pageerror', (error) => pageErrors.push(error.message));

      for (const route of routes) {
        await certifyRoute(page, route, viewport.name, viewport.kind === 'mobile');
      }

      expect(pageErrors, `Errores de ejecución en ${viewport.name}: ${pageErrors.join(' | ')}`).toEqual([]);
      expect(consoleErrors, `Errores de consola en ${viewport.name}: ${consoleErrors.join(' | ')}`).toEqual([]);
    });

    test(`${viewport.name} genera evidencia visual representativa`, async ({ page }) => {
      test.setTimeout(90_000);
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await login(page);

      for (const representative of representativeRoutes) {
        await page.goto(representative.route);
        await expect(page.locator('main#main-content h1').first()).toBeVisible();
        await page.screenshot({
          path: `test-results/fase4/${viewport.name}/${representative.name}.png`,
          fullPage: viewport.width <= 1024,
          animations: 'disabled'
        });
      }
    });
  }

  test.afterAll(async () => {
    await mkdir('test-results/fase4', { recursive: true });
    await writeFile(
      'test-results/fase4/matriz.json',
      JSON.stringify({
        generatedAt: new Date().toISOString(),
        viewports,
        routes,
        totalNavigations: viewports.length * routes.length,
        screenshots: viewports.length * representativeRoutes.length
      }, null, 2),
      'utf8'
    );
  });
});
