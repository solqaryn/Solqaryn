import { expect, Page, test } from '@playwright/test';

const empresaBase = {
  id: 901,
  nombreComercial: 'Storefront Audit',
  nombreVisibleSistema: 'Storefront Audit',
  eslogan: 'Compra simple y segura',
  descripcionSistema: 'Tienda pública de auditoría',
  mensajeLogin: 'Administración',
  copyright: '© 2026 Storefront Audit',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Atención pública de auditoría',
  piePaginaActivo: true,
  piePaginaTexto: 'Pie de auditoría',
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy'
};

async function prepararTienda(page: Page, whatsApp = '9876-5432'): Promise<void> {
  await page.route('http://localhost:5005/empresa-configuracion/publica', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: { ...empresaBase, whatsApp } })
    });
  });

  // Desde Fase 7 el catálogo vive únicamente en su ruta canónica; Fase 1 prueba aquí el header compartido.
  await page.goto('/tienda/productos');
  await expect(page.locator('.storefront')).toBeVisible();
  await expect(page.getByRole('heading', { level: 1, name: 'Productos', exact: true })).toBeVisible();
}

async function esperarCatalogo(page: Page): Promise<void> {
  await expect(page.getByRole('status').filter({ hasText: /productos encontrados/i })).toBeVisible();
  await expect(page.locator('.product-card').first()).toBeVisible();
}

test.describe('Storefront Fase 1 — navegación y header', () => {
  test.describe.configure({ retries: 0 });

  test('desktop: identidad, navegación, búsqueda, categorías, carrito y WhatsApp usan estado real', async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 900 });
    await prepararTienda(page);
    await esperarCatalogo(page);

    const header = page.locator('app-storefront-header');
    await expect(header.getByText('Storefront Audit', { exact: true }).first()).toBeVisible();
    await expect(header.getByText('Compra simple y segura', { exact: true })).toBeVisible();
    await expect(header.getByRole('link', { name: 'Inicio', exact: true })).toHaveAttribute('href', '/tienda');
    await expect(header.getByRole('link', { name: 'Productos', exact: true })).toHaveAttribute('href', '/tienda/productos');
    await expect(header.getByRole('link', { name: 'Categorías', exact: true })).toHaveAttribute('href', '/tienda/categorias');

    const whatsapp = header.getByRole('link', { name: 'Contactar por WhatsApp' });
    await expect(whatsapp).toBeVisible();
    await expect(whatsapp).toHaveAttribute('href', 'https://wa.me/50498765432');
    await expect(header.getByRole('button', { name: 'Abrir carrito con 0 unidades' })).toBeVisible();

    const laptop = page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' });
    await expect(laptop).toBeVisible();
    await laptop.getByRole('button', { name: 'Agregar Laptop Pro 14' }).click();
    await expect(page.locator('dialog.cart-dialog')).toHaveCount(0);
    await expect(header.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await expect(header.locator('.cart-copy small')).toContainText('18,490');

    await header.getByRole('button', { name: 'Abrir carrito con 1 unidades' }).click();
    await expect(page).toHaveURL(/\/tienda\/carrito$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Mi carrito', exact: true })).toBeVisible();
    await expect(page.locator('.cart-item')).toContainText('Laptop Pro 14');

    await page.goto('/tienda/productos');
    await esperarCatalogo(page);
    const catalogHeader = page.locator('app-storefront-header');
    const search = catalogHeader.getByRole('searchbox', { name: 'Buscar productos, marcas o modelos' });
    await search.fill('Wireless Studio');
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(1);
    await expect(page.locator('article.product-card')).toContainText('Audífonos Wireless Studio');

    await search.press('Enter');
    await expect(page).toHaveURL(/\/tienda\/productos\?q=Wireless(?:%20|\+)Studio/);
    await expect.poll(
      () => page.locator('#catalogo-productos').evaluate(element => element.getBoundingClientRect().top),
      { message: 'El buscador debe mantener visible el catálogo canónico.' }
    ).toBeLessThan(220);

    await search.fill('');
    await search.press('Enter');
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();
    const nav = catalogHeader.locator('nav.store-nav');
    const audio = nav.getByRole('button', { name: 'Audio', exact: true });
    await audio.click();
    await expect(audio).toHaveAttribute('aria-pressed', 'true');
    await expect(page).toHaveURL(/categoria=demo-categoria-2/);
    await expect(page.getByRole('status').filter({ hasText: '3 productos encontrados' })).toBeVisible();

    await nav.getByRole('button', { name: /Todas/ }).click();
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();
  });

  test('desktop: header y catálogo operan con datos reales de la API pública sin fallback demo', async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 900 });
    let peticionesCatalogo = 0;

    await page.route('http://localhost:5005/tienda/productos**', async route => {
      peticionesCatalogo += 1;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: {
            items: [
              {
                id: 701,
                slug: 'teclado-http-audit-701',
                nombre: 'Teclado HTTP Audit',
                descripcion: 'Producto servido por la API pública de auditoría.',
                categoriaId: 71,
                categoriaNombre: 'Periféricos',
                marcaNombre: 'Audit',
                modeloNombre: 'Base',
                precio: 1250,
                precioOferta: null,
                cantidadDisponible: 4,
                estaAgotado: false,
                sku: 'AUD-701',
                activo: true,
                esDestacado: false,
                fechaCreacion: '2026-09-18T00:00:00Z',
                imagenPrincipalUrl: null,
                imagenes: [],
                modelos: []
              },
              {
                id: 702,
                slug: 'audifonos-http-audit-702',
                nombre: 'Audífonos HTTP Audit',
                descripcion: 'Segundo producto servido por la API pública.',
                categoriaId: 72,
                categoriaNombre: 'Audio',
                marcaNombre: 'Audit',
                modeloNombre: 'Base',
                precio: 990,
                precioOferta: null,
                cantidadDisponible: 3,
                estaAgotado: false,
                sku: 'AUD-702',
                activo: true,
                esDestacado: false,
                fechaCreacion: '2026-09-18T00:00:00Z',
                imagenPrincipalUrl: null,
                imagenes: [],
                modelos: []
              }
            ],
            page: 1,
            pageSize: 96,
            totalCount: 2
          }
        })
      });
    });

    await page.route('http://localhost:5005/tienda/categorias**', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: [
            { id: 71, slug: 'perifericos-real-71', nombre: 'Periféricos', descripcion: 'Categoría real', totalProductos: 1 },
            { id: 72, slug: 'audio-real-72', nombre: 'Audio', descripcion: 'Categoría real', totalProductos: 1 }
          ]
        })
      });
    });

    await prepararTienda(page);
    await page.getByRole('button', { name: 'Base de datos' }).click();
    await expect(page.getByRole('status').filter({ hasText: '2 productos encontrados' })).toBeVisible();
    expect(peticionesCatalogo).toBe(1);
    await expect(page.locator('article.product-card')).toHaveCount(2);
    await expect(page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' })).toHaveCount(0);

    const header = page.locator('app-storefront-header');
    const search = header.getByRole('searchbox', { name: 'Buscar productos, marcas o modelos' });
    await search.fill('Audífonos HTTP');
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(1);
    await expect(page.locator('article.product-card')).toContainText('Audífonos HTTP Audit');

    await search.fill('');
    const audio = header.locator('nav.store-nav').getByRole('button', { name: 'Audio', exact: true });
    await audio.click();
    await expect(audio).toHaveAttribute('aria-pressed', 'true');
    await expect(page).toHaveURL(/categoria=audio-real-72/);
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toContainText('Audífonos HTTP Audit');

    await header.locator('nav.store-nav').getByRole('button', { name: /Todas/ }).click();
    const teclado = page.locator('article.product-card').filter({ hasText: 'Teclado HTTP Audit' });
    await teclado.getByRole('button', { name: 'Agregar Teclado HTTP Audit' }).click();
    await expect(header.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await expect(header.locator('.cart-copy small')).toContainText('1,250');
  });
  test('tablet: header compacto conserva WhatsApp y evita overflow', async ({ page }) => {
    await page.setViewportSize({ width: 900, height: 900 });
    await prepararTienda(page);
    await esperarCatalogo(page);

    const header = page.locator('app-storefront-header');
    for (const width of [1120, 900, 761]) {
      await page.setViewportSize({ width, height: 900 });
      const toggle = header.getByRole('button', { name: 'Abrir navegación' });
      await expect(header.getByRole('link', { name: 'Contactar por WhatsApp' })).toBeHidden();
      await expect(toggle).toBeVisible();
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow horizontal a ${width}px`).toBeLessThanOrEqual(0);

      await toggle.click();
      await expect(header.locator('.mobile-whatsapp')).toBeVisible();
      await header.getByRole('button', { name: 'Cerrar navegación' }).click();
      await expect(header.locator('#storefront-menu-movil')).not.toBeVisible();
    }
  });

  test('móvil: drawer modal, foco, Escape, categoría, modo de compra y reflow funcionan', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await prepararTienda(page);
    await esperarCatalogo(page);

    let header = page.locator('app-storefront-header');
    let toggle = header.getByRole('button', { name: 'Abrir navegación' });
    let dialog = header.locator('#storefront-menu-movil');

    await expect(toggle).toBeVisible();
    await expect(toggle).toHaveAttribute('aria-expanded', 'false');
    await toggle.click();
    await expect(toggle).toHaveAttribute('aria-expanded', 'true');
    await expect(dialog).toBeVisible();
    await expect(dialog.locator('nav a').first()).toBeFocused();

    await page.keyboard.press('Escape');
    await expect(dialog).not.toBeVisible();
    await expect(toggle).toHaveAttribute('aria-expanded', 'false');
    await expect(toggle).toBeFocused();

    await toggle.click();
    const categoriaAudio = dialog.locator('.mobile-categories').getByRole('button', { name: 'Audio', exact: true });
    await categoriaAudio.click();
    await expect(dialog).not.toBeVisible();
    await expect(page).toHaveURL(/categoria=demo-categoria-2/);
    await expect(page.getByRole('status').filter({ hasText: '3 productos encontrados' })).toBeVisible();

    await toggle.click();
    await expect(dialog.locator('a.mobile-whatsapp')).toBeVisible();
    await dialog.getByRole('button', { name: 'Cerrar navegación' }).click();

    // El selector de modo pertenece al home comercial, no al catálogo independiente.
    await page.goto('/tienda');
    await expect(page.getByRole('heading', { name: /Todo lo que buscas/i })).toBeVisible();
    await page.locator('.preview-panel select').selectOption('tarjeta');
    header = page.locator('app-storefront-header');
    toggle = header.getByRole('button', { name: 'Abrir navegación' });
    dialog = header.locator('#storefront-menu-movil');
    await toggle.click();
    await expect(dialog.locator('a.mobile-whatsapp')).toHaveCount(0);
    await dialog.getByRole('button', { name: 'Cerrar navegación' }).click();

    const targetAudit = await page.evaluate(() => {
      const selectors = ['.search-box button', '.cart-trigger', '.mobile-menu-trigger'];
      return selectors.map(selector => {
        const element = document.querySelector<HTMLElement>(selector);
        if (!element) return { selector, width: 0, height: 0 };
        const rect = element.getBoundingClientRect();
        return { selector, width: Math.round(rect.width), height: Math.round(rect.height) };
      });
    });
    expect(targetAudit.every(item => item.width >= 44 && item.height >= 44), JSON.stringify(targetAudit)).toBe(true);

    for (const width of [760, 390, 320]) {
      await page.setViewportSize({ width, height: 844 });
      await expect(header.getByRole('button', { name: 'Abrir navegación' })).toBeVisible();
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow horizontal a ${width}px`).toBeLessThanOrEqual(0);
    }
  });

  test('WhatsApp inválido no genera una acción rota', async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 900 });
    await prepararTienda(page, 'https://wa.me/50499999999');
    await esperarCatalogo(page);
    const header = page.locator('app-storefront-header');
    await expect(header.getByRole('link', { name: 'Contactar por WhatsApp' })).toHaveCount(0);
  });
});
