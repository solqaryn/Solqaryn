import { expect, Page, test } from '@playwright/test';

const empresaBase = {
  id: 983,
  nombreComercial: 'Storefront Browser Audit',
  nombreVisibleSistema: 'Storefront Browser Audit',
  eslogan: 'Compatibilidad entre navegadores',
  descripcionSistema: 'Tienda pública para regresión N8.3',
  mensajeLogin: 'Administración',
  copyright: '© 2026 Storefront Browser Audit',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Auditoría de navegadores',
  piePaginaActivo: true,
  piePaginaTexto: 'Compatibilidad N8.3',
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy',
  whatsApp: '9876-5432'
};

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

test.describe('N8.3 — compatibilidad causal entre motores soportados', () => {
  test.describe.configure({ retries: 0 });

  test('storefront conserva navegación, layout e interacción básica', async ({ page, browserName }) => {
    const pageErrors: string[] = [];
    page.on('pageerror', error => pageErrors.push(error.message));

    await prepararEmpresa(page);
    await page.goto('/tienda');

    await expect(page.locator('.storefront'), `${browserName}: storefront`).toBeVisible();
    await expect(page.getByRole('heading', { name: /Todo lo que buscas/i }), `${browserName}: hero`).toBeVisible();

    const header = page.locator('app-storefront-header');
    await expect(header, `${browserName}: header`).toBeVisible();
    await expect(header.getByRole('button', { name: /carrito/i }), `${browserName}: carrito`).toBeVisible();

    const layout = await page.evaluate(() => ({
      viewportWidth: window.innerWidth,
      documentWidth: document.documentElement.scrollWidth,
      bodyWidth: document.body.scrollWidth
    }));

    expect(layout.documentWidth, `${browserName}: overflow documentElement`).toBeLessThanOrEqual(layout.viewportWidth);
    expect(layout.bodyWidth, `${browserName}: overflow body`).toBeLessThanOrEqual(layout.viewportWidth);
    expect(pageErrors, `${browserName}: page errors`).toEqual([]);
  });
});
