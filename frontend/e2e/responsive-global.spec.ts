import { expect, Page, test } from '@playwright/test';
import { REQUIRED_QA_WIDTHS, RESPONSIVE_ROUTES, RESPONSIVE_VIEWPORTS } from '../src/app/testing/qa-responsive-catalog';
export { RESPONSIVE_ROUTES, RESPONSIVE_VIEWPORTS } from '../src/app/testing/qa-responsive-catalog';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

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

  test('el catálogo compartido no repite rutas y cubre los anchos QA', () => {
    expect(new Set(RESPONSIVE_ROUTES).size).toBe(RESPONSIVE_ROUTES.length);
    const widths = RESPONSIVE_VIEWPORTS.map(viewport => Number(viewport.width));
    expect(new Set(widths).size).toBe(widths.length);
    expect(REQUIRED_QA_WIDTHS.filter(width => !widths.includes(width))).toEqual([]);
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
