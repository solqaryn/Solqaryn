import { expect, test } from '@playwright/test';

/** C13-04.M05.06: R2 TEST ONLY. Chromium/Angular actual; public pages and anonymous guards.
 * No signed JWT, tenant A/B, MySQL, transactional providers, or production metrics.
 */
test.describe('C13-04.M05.06 public visual/responsive regression', () => {
  test.describe.configure({ retries: 0 });

  const views = [
    { path: '/login', heading: 'Iniciar sesión', locator: 'h1, h2' },
    { path: '/tienda', heading: null, locator: 'h1#hero-title' },
    { path: '/tienda/productos', heading: 'Productos', locator: 'main#catalogo-productos h1' },
    { path: '/tienda/categorias', heading: 'Categorías', locator: 'main#contenido-categorias h1' },
    { path: '/tienda/ofertas', heading: 'Ofertas', locator: 'main#catalogo-productos h1' }
  ] as const;

  test('matriz de cinco pantallas en 320/768/1280, screenshot real y sin overflow', async ({ page }) => {
    for (const width of [320, 768, 1280]) {
      await page.setViewportSize({ width, height: 812 });
      for (const view of views) {
        const response = await page.goto(view.path, { waitUntil: 'domcontentloaded' });
        expect(response?.ok(), view.path + ' HTTP public document').toBe(true);
        const heading = page.locator(view.locator).first();
        await expect(heading).toBeVisible();
        if (view.heading !== null) await expect(heading).toHaveText(view.heading);
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth - document.documentElement.clientWidth);
        expect(overflow, view.path + ' width ' + width).toBeLessThanOrEqual(2);
        if (width !== 768 && (view.path === '/login' || view.path === '/tienda')) {
          const screenshot = await page.screenshot({ fullPage: true, animations: 'disabled' });
          expect(screenshot.byteLength).toBeGreaterThan(1000);
          await test.info().attach('chromium-' + view.path.replaceAll('/', '_') + '-' + width,
            { body: screenshot, contentType: 'image/png' });
        }
      }
    }
  });

  test('Tab nativo alcanza textbox y enlace de tienda, y la navegación funciona', async ({ page }) => {
    await page.goto('/login');
    const user = page.getByRole('textbox', { name: 'Nombre de usuario' });
    await expect(user).toBeVisible();
    let reached = false;
    for (let i = 0; i < 100; i++) {
      await page.keyboard.press('Tab');
      if (await user.evaluate(el => document.activeElement === el)) { reached = true; break; }
    }
    expect(reached, 'keyboard focus without .focus()').toBe(true);
    await page.goto('/tienda');
    const products = page.locator('.hero-actions a[href="/tienda/productos"]');
    await expect(products).toBeVisible();
    reached = false;
    for (let i = 0; i < 100; i++) {
      await page.keyboard.press('Tab');
      if (await products.evaluate(el => document.activeElement === el)) { reached = true; break; }
    }
    expect(reached, 'shop link reachable via physical Tab').toBe(true);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/tienda\/productos(?:\?|$)/);
    await expect(page.locator('main#catalogo-productos h1')).toHaveText('Productos');
  });

  test('anonymous guard denies private views with zero mutation requests', async ({ page }) => {
    const writes: string[] = [];
    page.on('request', request => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
        writes.push(request.method() + ' ' + request.url());
      }
    });
    for (const path of ['/dashboard', '/productos', '/plataforma/inventario-rutas-v19']) {
      await page.goto(path);
      await expect(page).toHaveURL(/\/login(?:\?|$)/);
      await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
      expect(page.url()).not.toContain(path);
    }
    expect(writes).toEqual([]);
  });

  test('reduced motion and local navigation timing are measurable, not field CWV', async ({ page }) => {
    await page.emulateMedia({ reducedMotion: 'reduce' });
    for (const path of ['/login', '/tienda', '/tienda/productos']) {
      await page.goto(path, { waitUntil: 'domcontentloaded' });
      const m = await page.evaluate(() => {
        const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
        return { dcl: nav?.domContentLoadedEventEnd ?? NaN,
          reduced: matchMedia('(prefers-reduced-motion: reduce)').matches };
      });
      expect(m.reduced).toBe(true);
      expect(Number.isFinite(m.dcl)).toBe(true);
      expect(m.dcl, 'CI-only DCL smoke; not p95 or CWV').toBeLessThan(30_000);
    }
  });

  test('adversarial overflow regression detects an intentionally oversized node', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 812 });
    await page.goto('/tienda');
    const before = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(before).toBeLessThanOrEqual(2);
    const after = await page.evaluate(() => {
      const item = document.createElement('div');
      item.id = 'adversarial-oversize-c1304';
      item.style.cssText = 'position:absolute;left:0;top:0;width:1900px;height:1px';
      document.body.appendChild(item);
      return document.documentElement.scrollWidth - document.documentElement.clientWidth;
    });
    expect(after).toBeGreaterThan(1000);
    await page.locator('#adversarial-oversize-c1304').evaluate(el => el.remove());
    expect(await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  });
});
