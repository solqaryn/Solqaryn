import { expect, Page, test } from '@playwright/test';

const empresa = {
  id: 910,
  nombreComercial: 'Storefront Fase 10',
  nombreVisibleSistema: 'Storefront Fase 10',
  eslogan: 'Compra cómoda desde cualquier pantalla',
  descripcionSistema: 'Auditoría responsive, UX y accesibilidad',
  mensajeLogin: 'Administración',
  copyright: '© 2026 Storefront',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Tienda pública',
  piePaginaActivo: true,
  piePaginaTexto: 'Tienda pública',
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy',
  whatsApp: '9876-5432'
};

const imagenData = (texto: string) => `data:image/svg+xml,${encodeURIComponent(
  `<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600"><rect width="800" height="600" fill="white"/><text x="400" y="300" text-anchor="middle" font-size="48">${texto}</text></svg>`
)}`;

async function preparar(page: Page): Promise<void> {
  await page.route('http://localhost:5005/empresa-configuracion/publica', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: empresa })
  }));
}

async function sinOverflowHorizontal(page: Page, ruta: string, width: number): Promise<void> {
  await page.setViewportSize({ width, height: 844 });
  await page.goto(ruta);
  await expect(page.locator('.storefront')).toBeVisible();
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow, `overflow horizontal en ${ruta} a ${width}px`).toBeLessThanOrEqual(0);
}

test.describe('Storefront Fase 10 — responsive, UX y accesibilidad', () => {
  test.describe.configure({ retries: 0 });

  test('rutas públicas de exploración no desbordan en anchos móviles comunes', async ({ page }) => {
    await preparar(page);
    const rutas = [
      '/tienda',
      '/tienda/productos',
      '/tienda/ofertas',
      '/tienda/categorias',
      '/tienda/categoria/demo-categoria-1',
      '/tienda/producto/demo-producto-1'
    ];
    for (const width of [320, 360, 390, 430]) {
      for (const ruta of rutas) await sinOverflowHorizontal(page, ruta, width);
    }
  });

  test('rutas públicas de compra no desbordan en anchos móviles comunes', async ({ page }) => {
    await preparar(page);
    const rutas = [
      '/tienda/carrito',
      '/tienda/checkout',
      '/tienda/pedido/auditoria-fase10'
    ];
    for (const width of [320, 360, 390, 430]) {
      for (const ruta of rutas) await sinOverflowHorizontal(page, ruta, width);
    }
  });

  test('detalle móvil ofrece feedback visible, CTA seguro y targets táctiles', async ({ page }) => {
    await preparar(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/tienda/producto/demo-producto-1');

    const agregar = page.getByRole('button', { name: 'Agregar al carrito', exact: true });
    await expect(agregar).toBeEnabled();
    await agregar.click();

    await expect(page.locator('.feedback-toast')).toContainText(/unidad agregada|unidades agregadas/);
    await expect(page.locator('.mobile-buy-bar')).toBeVisible();

    const alturas = await page.locator('button, .button').evaluateAll(elements =>
      elements.filter(element => {
        const style = getComputedStyle(element);
        const rect = element.getBoundingClientRect();
        return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
      }).map(element => Math.round(element.getBoundingClientRect().height))
    );
    expect(alturas.length).toBeGreaterThan(0);
    expect(Math.min(...alturas)).toBeGreaterThanOrEqual(44);

    const bar = await page.locator('.mobile-buy-bar').boundingBox();
    expect(bar).not.toBeNull();
    expect((bar?.y || 0) + (bar?.height || 0)).toBeLessThanOrEqual(845);
  });

  test('fullscreen abre, permite swipe táctil, cierra y devuelve foco', async ({ page }) => {
    await preparar(page);
    const imagenes = [imagenData('Imagen uno'), imagenData('Imagen dos')];
    const detalle = {
      id: 9101, slug: 'producto-fase10', nombre: 'Producto Fase 10', descripcion: 'Detalle accesible.',
      categoriaId: 21, categoriaNombre: 'Tecnología', marcaNombre: 'Marca F10', precio: 1500,
      precioOferta: null, ofertaActiva: false, ofertaNombre: null, ahorro: 0, porcentajeAhorro: 0,
      cantidadDisponible: 5, estaAgotado: false, estadoDisponibilidad: 'Disponible', sku: 'F10-9101',
      activo: true, esDestacado: false, imagenes: imagenes.map((url, i) => ({ url, orden: i + 1, esPrincipal: i === 0 })),
      modelos: [{
        productoVarianteId: 91010, modeloId: 910100, modeloNombre: 'Base', marcaNombre: 'Marca F10',
        sku: 'F10-9101-A', precio: 1500, precioOferta: null, ofertaActiva: false, ofertaNombre: null,
        ahorro: 0, porcentajeAhorro: 0, cantidadDisponible: 5, estaAgotado: false,
        estadoDisponibilidad: 'Disponible', imagenes: imagenes.map((url, i) => ({ url, orden: i + 1, esPrincipal: i === 0 }))
      }]
    };

    await page.route('**/tienda/productos?*', route => route.fulfill({
      status: 200, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: { items: [detalle], page: 1, pageSize: 96, totalCount: 1 } })
    }));
    await page.route('**/tienda/productos/*', route => route.fulfill({
      status: 200, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: detalle })
    }));
    await page.route('**/tienda/categorias', route => route.fulfill({
      status: 200, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: [{ id: 21, slug: 'tecnologia-21', nombre: 'Tecnología', descripcion: '', totalProductos: 1 }] })
    }));

    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/tienda/producto/producto-fase10');
    await page.getByRole('group', { name: 'Origen de datos' }).getByRole('button', { name: 'Base de datos' }).click();
    await expect(page.getByRole('heading', { level: 1, name: 'Producto Fase 10' })).toBeVisible();

    const principal = page.locator('.main-image-button');
    await expect(principal).toBeVisible();
    await principal.click();
    const dialogo = page.locator('dialog.lightbox');
    await expect(dialogo).toBeVisible();

    const contador = dialogo.locator('.lightbox-controls span');
    await expect(contador).toHaveText('1 / 2');
    const stage = dialogo.locator('.lightbox-stage');
    await stage.dispatchEvent('pointerdown', { pointerId: 1, pointerType: 'touch', clientX: 310, clientY: 220, isPrimary: true });
    await stage.dispatchEvent('pointerup', { pointerId: 1, pointerType: 'touch', clientX: 170, clientY: 224, isPrimary: true });
    await expect(contador).toHaveText('2 / 2');

    for (const [width, height] of [[320, 700], [430, 932]] as const) {
      await page.setViewportSize({ width, height });
      const rect = await dialogo.boundingBox();
      expect(rect, `lightbox sin geometría a ${width}px`).not.toBeNull();
      expect(rect!.x).toBeGreaterThanOrEqual(0);
      expect(rect!.y).toBeGreaterThanOrEqual(0);
      expect(rect!.x + rect!.width).toBeLessThanOrEqual(width + 1);
      expect(rect!.y + rect!.height).toBeLessThanOrEqual(height + 1);
    }

    await dialogo.getByRole('button', { name: 'Cerrar imagen ampliada' }).click();
    await expect(dialogo).not.toBeVisible();
    await expect(principal).toBeFocused();

    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    expect(overflow).toBeLessThanOrEqual(0);
  });

  test('tablet y zoom alto compactan catálogo y header antes de comprimirse', async ({ page }) => {
    await preparar(page);
    await page.setViewportSize({ width: 980, height: 900 });
    await page.goto('/tienda/productos');

    await expect(page.locator('.header-whatsapp')).toBeHidden();
    await expect(page.locator('.mobile-menu-trigger')).toBeVisible();
    await expect(page.locator('.mobile-filters')).toBeVisible();
    await expect(page.locator('.filters')).not.toBeVisible();

    await page.locator('.mobile-filters').click();
    await expect(page.locator('.filters')).toBeVisible();

    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    expect(overflow).toBeLessThanOrEqual(0);
  });

  test('catálogo mantiene precio y CTAs separados con zoom equivalente y móvil', async ({ page }) => {
    await preparar(page);

    const widths = [320, 360, 390, 430, 600, 720, 768, 820, 900, 960, 980, 1024, 1100, 1152, 1180, 1280, 1440, 1800];
    for (const width of widths) {
      await page.setViewportSize({ width, height: 1000 });
      await page.goto('/tienda/productos');
      await expect(page.locator('.product-card').first()).toBeVisible();

      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow horizontal del catálogo a ${width}px`).toBeLessThanOrEqual(0);

      const geometria = await page.locator('.product-card').evaluateAll(cards => cards.map(card => {
        const cardRect = card.getBoundingClientRect();
        const price = card.querySelector('.price-copy')?.getBoundingClientRect();
        const actions = card.querySelector('.product-actions')?.getBoundingClientRect();
        const controls = [...card.querySelectorAll('.product-actions .button')].map(el => el.getBoundingClientRect());
        return {
          card: { left: cardRect.left, right: cardRect.right, top: cardRect.top, bottom: cardRect.bottom, width: cardRect.width },
          price: price ? { left: price.left, right: price.right, top: price.top, bottom: price.bottom } : null,
          actions: actions ? { left: actions.left, right: actions.right, top: actions.top, bottom: actions.bottom } : null,
          controls: controls.map(rect => ({ left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom, width: rect.width, height: rect.height }))
        };
      }));

      expect(geometria.length).toBeGreaterThan(0);
      for (const item of geometria) {
        expect(item.card.width, `card demasiado estrecha a ${width}px`).toBeGreaterThan(0);
        expect(item.price, `sin bloque de precio a ${width}px`).not.toBeNull();
        expect(item.actions, `sin bloque de acciones a ${width}px`).not.toBeNull();
        expect(item.price!.bottom, `precio invade acciones a ${width}px`).toBeLessThanOrEqual(item.actions!.top + 1);
        expect(item.price!.left, `precio sale por la izquierda a ${width}px`).toBeGreaterThanOrEqual(item.card.left - 1);
        expect(item.price!.right, `precio sale por la derecha a ${width}px`).toBeLessThanOrEqual(item.card.right + 1);

        for (const control of item.controls) {
          expect(control.left, `CTA sale por la izquierda a ${width}px`).toBeGreaterThanOrEqual(item.card.left - 1);
          expect(control.right, `CTA sale por la derecha a ${width}px`).toBeLessThanOrEqual(item.card.right + 1);
          expect(control.width, `CTA sin ancho útil a ${width}px`).toBeGreaterThan(0);
          expect(control.height, `CTA táctil demasiado bajo a ${width}px`).toBeGreaterThanOrEqual(44);
        }

        for (let i = 0; i < item.controls.length; i++) {
          for (let j = i + 1; j < item.controls.length; j++) {
            const a = item.controls[i];
            const b = item.controls[j];
            const overlapX = Math.min(a.right, b.right) - Math.max(a.left, b.left);
            const overlapY = Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top);
            expect(overlapX > 1 && overlapY > 1, `CTAs superpuestos a ${width}px`).toBe(false);
          }
        }
      }
    }
  });

  test('zoom de escritorio 80 a 200 por ciento no superpone header precio ni CTAs', async ({ page }) => {
    await preparar(page);

    const desktopFisico = 1440;
    const niveles = [0.8, 0.9, 1, 1.25, 1.5, 2];

    for (const zoom of niveles) {
      const width = Math.round(desktopFisico / zoom);
      await page.setViewportSize({ width, height: 1000 });
      await page.goto('/tienda/productos');
      await expect(page.locator('.product-card').first()).toBeVisible();

      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow con zoom ${Math.round(zoom * 100)}% (${width}px CSS)`).toBeLessThanOrEqual(0);

      const card = page.locator('.product-card').first();
      const price = card.locator('.price-copy');
      const actions = card.locator('.product-actions');
      await expect(price).toBeVisible();
      await expect(actions).toBeVisible();

      const geometry = await card.evaluate(element => {
        const cardRect = element.getBoundingClientRect();
        const priceRect = element.querySelector('.price-copy')?.getBoundingClientRect();
        const actionsRect = element.querySelector('.product-actions')?.getBoundingClientRect();
        const controls = [...element.querySelectorAll('.product-actions .button')].map(node => node.getBoundingClientRect());
        return {
          card: { left: cardRect.left, right: cardRect.right, top: cardRect.top, bottom: cardRect.bottom },
          price: priceRect ? { left: priceRect.left, right: priceRect.right, top: priceRect.top, bottom: priceRect.bottom } : null,
          actions: actionsRect ? { left: actionsRect.left, right: actionsRect.right, top: actionsRect.top, bottom: actionsRect.bottom } : null,
          controls: controls.map(rect => ({ left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom }))
        };
      });

      expect(geometry.price).not.toBeNull();
      expect(geometry.actions).not.toBeNull();
      expect(geometry.price!.bottom, `precio invade CTAs con zoom ${Math.round(zoom * 100)}%`).toBeLessThanOrEqual(geometry.actions!.top + 1);
      expect(geometry.price!.left).toBeGreaterThanOrEqual(geometry.card.left - 1);
      expect(geometry.price!.right).toBeLessThanOrEqual(geometry.card.right + 1);

      for (const control of geometry.controls) {
        expect(control.left, `CTA sale por izquierda con zoom ${Math.round(zoom * 100)}%`).toBeGreaterThanOrEqual(geometry.card.left - 1);
        expect(control.right, `CTA sale por derecha con zoom ${Math.round(zoom * 100)}%`).toBeLessThanOrEqual(geometry.card.right + 1);
      }

      for (let i = 0; i < geometry.controls.length; i++) {
        for (let j = i + 1; j < geometry.controls.length; j++) {
          const a = geometry.controls[i];
          const b = geometry.controls[j];
          const overlapX = Math.min(a.right, b.right) - Math.max(a.left, b.left);
          const overlapY = Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top);
          expect(overlapX > 1 && overlapY > 1, `CTAs superpuestos con zoom ${Math.round(zoom * 100)}%`).toBe(false);
        }
      }

      const headerOverflow = await page.locator('.header-main').evaluate(element => element.scrollWidth - element.clientWidth);
      expect(headerOverflow, `header desborda con zoom ${Math.round(zoom * 100)}%`).toBeLessThanOrEqual(1);

      if (zoom >= 1.25) {
        await expect(page.locator('.mobile-menu-trigger'), `header no compacta con zoom ${Math.round(zoom * 100)}%`).toBeVisible();
        await expect(page.locator('.header-whatsapp')).toBeHidden();
        await expect(page.locator('.mobile-filters')).toBeVisible();
        await expect(page.locator('.filters')).not.toBeVisible();
      }
    }
  });

  test('teléfono o tablet táctil con viewport ancho no recibe layout de escritorio', async ({ browser }) => {
    const context = await browser.newContext({
      baseURL: process.env['PLAYWRIGHT_TEST_BASE_URL'] ?? 'http://127.0.0.1:4200',
      viewport: { width: 1280, height: 900 },
      hasTouch: true,
      isMobile: true
    });
    const page = await context.newPage();

    try {
      await preparar(page);
      await page.goto('/tienda/productos');
      await expect(page.locator('.product-card').first()).toBeVisible();
      await expect(page.locator('.mobile-menu-trigger')).toBeVisible();
      await expect(page.locator('.header-whatsapp')).toBeHidden();
      await expect(page.locator('.mobile-filters')).toBeVisible();
      await expect(page.locator('.filters')).not.toBeVisible();

      const columnas = await page.locator('.product-grid').evaluate(element =>
        getComputedStyle(element).gridTemplateColumns.split(' ').filter(Boolean).length
      );
      expect(columnas, 'un dispositivo táctil ancho no debe recibir un grid de escritorio denso').toBeLessThanOrEqual(2);

      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, 'un dispositivo táctil ancho no debe generar overflow horizontal').toBeLessThanOrEqual(0);
    } finally {
      await context.close();
    }
  });

  test('checkout móvil asocia errores a campos y conserva navegación por teclado', async ({ page }) => {
    await preparar(page);
    await page.setViewportSize({ width: 390, height: 844 });

    await page.goto('/tienda/producto/demo-producto-1');
    await page.getByRole('button', { name: 'Agregar al carrito', exact: true }).click();
    await page.goto('/tienda/checkout');

    await expect(page.getByRole('heading', { name: 'Confirma tus datos y tu forma de compra' })).toBeVisible();

    const accion = page.getByRole('button', { name: /Simular continuación segura|Preparar pedido por WhatsApp/ }).first();
    await expect(accion).toBeVisible();
    await accion.click();

    const nombre = page.locator('input[formcontrolname="nombre"]');
    const telefono = page.locator('input[formcontrolname="telefono"]');
    await expect(nombre).toHaveAttribute('aria-invalid', 'true');
    await expect(telefono).toHaveAttribute('aria-invalid', 'true');
    await expect(nombre).toHaveAttribute('aria-describedby', /nombre-error/);
    await expect(telefono).toHaveAttribute('aria-describedby', /telefono-error/);

    await nombre.focus();
    await expect(nombre).toBeFocused();
    await page.keyboard.press('Tab');
    const activeTag = await page.evaluate(() => document.activeElement?.tagName);
    expect(activeTag).toBeTruthy();

    const controles = await page.locator('.checkout-page input, .checkout-page textarea, .checkout-page button, .checkout-page .button').evaluateAll(elements =>
      elements.filter(element => {
        const rect = element.getBoundingClientRect();
        return rect.width > 0 && rect.height > 0;
      }).map(element => Math.round(element.getBoundingClientRect().height))
    );
    expect(controles.every(height => height >= 44)).toBe(true);
  });
});
