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
  await page.route('http://localhost:5005/empresa-configuracion/publica', async route => {
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
});
