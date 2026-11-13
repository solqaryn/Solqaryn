import { expect, Page, test } from '@playwright/test';

const empresaBase = {
  id: 982,
  nombreComercial: 'Storefront Device Audit',
  nombreVisibleSistema: 'Storefront Device Audit',
  eslogan: 'Compatibilidad verificable',
  descripcionSistema: 'Tienda pública para regresión responsive',
  mensajeLogin: 'Administración',
  copyright: '© 2026 Storefront Device Audit',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Auditoría de dispositivos',
  piePaginaActivo: true,
  piePaginaTexto: 'Compatibilidad N8.2',
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy',
  whatsApp: '9876-5432'
};

const perfiles = [
  { nombre: 'escritorio', width: 1440, height: 900 },
  { nombre: 'laptop', width: 1366, height: 768 },
  { nombre: 'tablet', width: 820, height: 1180 },
  { nombre: 'Android', width: 412, height: 915 },
  { nombre: 'iPhone', width: 390, height: 844 }
] as const;

async function prepararEmpresa(page: Page): Promise<void> {
  await page.route(/^http:\/\/localhost:5005\/(?:empresa-configuracion\/publica|tienda\/configuracion)$/, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: empresaBase })
    });
  });
}

test.describe('N8.2 — compatibilidad explícita por perfil de dispositivo', () => {
  test.describe.configure({ retries: 0 });

  test('escritorio, laptop, tablet, Android e iPhone conservan reflow sin overflow horizontal', async ({ page }) => {
    await prepararEmpresa(page);

    for (const perfil of perfiles) {
      await test.step(perfil.nombre, async () => {
        await page.setViewportSize({ width: perfil.width, height: perfil.height });
        await page.goto('/tienda');

        await expect(page.locator('.storefront')).toBeVisible();
        await expect(page.getByRole('heading', { name: /Todo lo que buscas/i })).toBeVisible();

        const layout = await page.evaluate(() => ({
          viewportWidth: window.innerWidth,
          documentWidth: document.documentElement.scrollWidth,
          bodyWidth: document.body.scrollWidth
        }));

        expect(layout.viewportWidth, `viewport de ${perfil.nombre}`).toBe(perfil.width);
        expect(layout.documentWidth, `overflow documentElement en ${perfil.nombre}`).toBeLessThanOrEqual(perfil.width);
        expect(layout.bodyWidth, `overflow body en ${perfil.nombre}`).toBeLessThanOrEqual(perfil.width);

        const header = page.locator('app-storefront-header');
        await expect(header).toBeVisible();
        await expect(header.getByRole('button', { name: /carrito/i })).toBeVisible();

        if (perfil.width <= 760) {
          await expect(header.getByRole('button', { name: 'Abrir navegación' })).toBeVisible();
        }
      });
    }
  });
  test('V5 M04.06: portada liviana, densidad usable y carga progresiva real', async ({ page }) => {
    await prepararEmpresa(page);

    for (const viewport of [
      { width: 390, height: 844 },
      { width: 1440, height: 900 }
    ]) {
      await test.step(String(viewport.width), async () => {
        await page.setViewportSize(viewport);
        await page.goto('/tienda', { waitUntil: 'domcontentloaded' });
        await expect(page.locator('#hero-title')).toBeVisible();
        await expect(page.getByRole('link', { name: 'Explorar productos' })).toBeVisible();

        const medida = await page.evaluate(() => {
          const titulo = document.getElementById('hero-title');
          const hero = document.querySelector('.hero');
          const enlaces = Array.from(document.querySelectorAll<HTMLAnchorElement>('.hero-actions a'));
          if (!titulo || !hero || enlaces.length !== 2) return null;
          const r = titulo.getBoundingClientRect();
          const h = hero.getBoundingClientRect();
          const a = enlaces[0].getBoundingClientRect();
          const b = enlaces[1].getBoundingClientRect();
          const separados = a.right + 7 <= b.left || b.right + 7 <= a.left ||
            a.bottom + 7 <= b.top || b.bottom + 7 <= a.top;
          return {
            titleInsideViewport: r.left >= -1 && r.right <= document.documentElement.clientWidth + 1,
            heroHasReadableHeight: h.height >= 300,
            ctasHaveTouchHeight: a.height >= 44 && b.height >= 44,
            actionsAreSeparated: separados,
            documentOverflow: document.documentElement.scrollWidth - document.documentElement.clientWidth
          };
        });

        expect(medida).not.toBeNull();
        expect(medida?.titleInsideViewport).toBe(true);
        expect(medida?.heroHasReadableHeight).toBe(true);
        expect(medida?.ctasHaveTouchHeight).toBe(true);
        expect(medida?.actionsAreSeparated).toBe(true);
        expect(medida?.documentOverflow).toBeLessThanOrEqual(1);

        await expect(page.locator('#categories-title')).toBeVisible();
        await expect(page.locator('#featured-title')).toBeVisible();
        await expect.poll(async () => page.evaluate(() =>
          (window.__SOLQARYN_PERF_BASELINE__ ?? []).length
        )).toBeGreaterThan(0);
      });
    }
  });

});
