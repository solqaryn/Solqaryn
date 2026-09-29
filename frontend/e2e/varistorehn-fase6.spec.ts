import { expect, Page, test } from '@playwright/test';

const empresaBase = {
  id: 905,
  nombreComercial: 'VariStore Checkout Audit',
  nombreVisibleSistema: 'VariStore Checkout Audit',
  eslogan: 'Compra simple y segura',
  descripcionSistema: 'Tienda pública de auditoría',
  mensajeLogin: 'Administración',
  copyright: '© 2026 VariStore Checkout Audit',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Atención pública',
  piePaginaActivo: true,
  piePaginaTexto: 'Pie público',
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy',
  whatsApp: '9876-5432'
};

const referenciaValidada = '0123456789abcdef0123456789abcdef';
const productoVarianteReal = 95010;
const modeloClaveReal = JSON.stringify([5010, 'Modelo auditoría', 'Marca real']);
const modeloClaveDemo = JSON.stringify([101, '8 GB / 256 GB', 'Demo']);

async function prepararEmpresa(page: Page, whatsApp: string | null = empresaBase.whatsApp): Promise<void> {
  await page.route('http://localhost:5005/empresa-configuracion/publica', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: { ...empresaBase, whatsApp } })
    });
  });
}

function productoReal(precio = 1750, stock = 3) {
  return {
    id: 501,
    slug: 'producto-checkout-real-501',
    nombre: 'Producto Checkout Real',
    descripcion: 'Producto real para auditar el checkout.',
    categoriaId: 21,
    categoriaNombre: 'Computadoras',
    marcaNombre: 'Marca real',
    precio,
    precioOferta: null,
    cantidadDisponible: stock,
    estaAgotado: stock <= 0,
    sku: 'SKU-501',
    activo: true,
    esDestacado: false,
    imagenes: [],
    modelos: [{
      productoVarianteId: productoVarianteReal,
      modeloId: 5010,
      modeloNombre: 'Modelo auditoría',
      marcaNombre: 'Marca real',
      sku: 'SKU-501-A',
      precio,
      cantidadDisponible: stock,
      estaAgotado: stock <= 0,
      imagenes: []
    }]
  };
}

async function mockCatalogoReal(page: Page, precio = 1750, stock = 3): Promise<void> {
  const producto = productoReal(precio, stock);
  await page.route('**/tienda/productos?*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: { items: [producto], page: 1, pageSize: 96, totalCount: 1 } })
    });
  });
  await page.route('**/tienda/productos/contexto', async route => {
    const body = route.request().postDataJSON() as { productoIds?: number[] } | null;
    const ids = new Set(Array.isArray(body?.productoIds) ? body!.productoIds : []);
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: ids.has(producto.id) ? [producto] : [] })
    });
  });
}

async function mockCheckoutValido(page: Page, total = 4200): Promise<void> {
  await page.route('**/tienda/checkout/validar', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({
        success: true,
        data: {
          validacionId: referenciaValidada,
          expiraUtc: new Date(Date.now() + 10 * 60 * 1000).toISOString(),
          subtotal: total,
          total,
          lineas: [{
            productoId: 501,
            productoVarianteId: productoVarianteReal,
            modeloId: 5010,
            nombre: 'Producto Checkout Real',
            modelo: 'Modelo auditoría',
            sku: 'SKU-501-A',
            unidades: 2,
            stockDisponible: 3,
            precioUnitario: total / 2,
            total
          }]
        }
      })
    });
  });
}

async function sembrarCarrito(page: Page, fuente: 'bd' | 'demo', modeloClave: string, productoId: number, unidades: number): Promise<void> {
  await page.addInitScript(({ key, value }) => localStorage.setItem(key, value), {
    key: `varistorehn:carrito:v2:905:${fuente}`,
    value: JSON.stringify([{ productoId, modeloClave, unidades }])
  });
}

async function llenarComprador(page: Page): Promise<void> {
  await page.getByRole('textbox', { name: 'Nombre completo' }).fill('Cliente Auditoría');
  await page.getByRole('textbox', { name: 'Teléfono' }).fill('+504 9999-1111');
  await page.getByRole('textbox', { name: 'Correo electrónico' }).fill('cliente@example.com');
  await page.getByRole('textbox', { name: 'Notas para el comercio' }).fill('Entregar por la tarde.');
}

test.describe('VariStoreHn Fase 6 — checkout y pedido', () => {
  test.describe.configure({ retries: 0 });

  test('checkout vacío bloquea la compra y no inventa un pedido', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/varistorehn/checkout');

    await expect(page).toHaveURL(/\/varistorehn\/checkout$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Confirma tus datos y tu forma de compra' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Tu carrito está vacío' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Preparar pedido por WhatsApp' })).toHaveCount(0);
    await expect(page.getByText(/Fase 6/)).toHaveCount(0);
  });

  test('variante física con modelo general llega exacta al checkout y habilita WhatsApp', async ({ page }) => {
    await prepararEmpresa(page, null);
    await page.route('**/whatsapp/publico', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ numeroTelefonoE164: '+50498765432', disponible: true })
    }));
    const varianteId = 95011;
    const productoId = 509;
    const producto = {
      id: productoId,
      slug: 'uat-modelo-general-509',
      nombre: 'UAT Modelo General',
      descripcion: '',
      categoriaId: null,
      categoriaNombre: null,
      marcaNombre: 'Samsung',
      modeloNombre: '',
      precio: 150,
      precioOferta: null,
      cantidadDisponible: 10,
      estaAgotado: false,
      estadoDisponibilidad: 'Disponible',
      sku: 'UAT-GENERAL',
      activo: true,
      esDestacado: false,
      imagenes: [],
      modelos: [{
        productoVarianteId: varianteId,
        modeloId: null,
        modeloNombre: null,
        marcaNombre: 'Samsung',
        sku: 'UAT-GENERAL',
        precio: 150,
        cantidadDisponible: 10,
        estaAgotado: false,
        estadoDisponibilidad: 'Disponible',
        imagenes: []
      }]
    };

    await sembrarCarrito(page, 'bd', `variante:${varianteId}`, productoId, 1);
    let requestContexto: unknown = null;
    await page.route('**/tienda/productos/contexto', route => {
      requestContexto = route.request().postDataJSON();
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: [producto] })
      });
    });

    let requestCheckout: unknown = null;
    await page.route('**/tienda/checkout/validar', route => {
      requestCheckout = route.request().postDataJSON();
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: {
            validacionId: referenciaValidada,
            expiraUtc: new Date(Date.now() + 10 * 60 * 1000).toISOString(),
            subtotal: 150,
            total: 150,
            lineas: [{
              productoId,
              productoVarianteId: varianteId,
              modeloId: null,
              nombre: 'UAT Modelo General',
              modelo: null,
              sku: 'UAT-GENERAL',
              unidades: 1,
              stockDisponible: 10,
              precioUnitario: 150,
              total: 150
            }]
          }
        })
      });
    });

    await page.goto('/varistorehn/checkout?fuente=bd');
    await expect(page.getByRole('heading', { name: 'Tu compra' })).toBeVisible();
    expect(requestContexto).toEqual({ productoIds: [productoId] });
    expect(requestCheckout).toEqual({
      items: [{
        productoId,
        productoVarianteId: varianteId,
        modeloId: null,
        modeloNombre: null,
        marcaNombre: null,
        unidades: 1
      }]
    });

    await llenarComprador(page);
    await page.getByRole('button', { name: 'Preparar pedido por WhatsApp' }).click();
    const enlace = page.getByRole('link', { name: 'Abrir WhatsApp y continuar' });
    await expect(enlace).toBeVisible();
    const href = await enlace.getAttribute('href');
    expect(href).toBeTruthy();
    const destino = new URL(href!);
    expect(destino.origin).toBe('https://wa.me');
    expect(destino.pathname).toBe('/50498765432');
    const mensaje = destino.searchParams.get('text') || '';
    expect(mensaje).toContain('🛍️ *Nueva solicitud de compra — VariStore Checkout Audit*');
    expect(mensaje).toContain('📦 *Producto 1*');
    expect(mensaje).toContain('*UAT Modelo General*');
    expect(mensaje).toContain('SKU: UAT-GENERAL');
    expect(mensaje).toMatch(/🔗 Ver producto: http:\/\/(?:localhost|127\.0\.0\.1):4200\/varistorehn\/producto\/uat-modelo-general-509/);
    expect(mensaje).toContain('Cantidad: 1');
    expect(mensaje).toContain('Precio unitario:');
    expect(mensaje).toContain('💰 *TOTAL:');
    expect(mensaje).toContain('📝 *Nota del cliente*');
    expect(mensaje).toContain('✅ Solicitud generada desde *VariStore Checkout Audit*.');
  });

  test('fuente real revalida en servidor sin enviar precio/stock y WhatsApp usa el total autoritativo', async ({ page }) => {
    await prepararEmpresa(page);
    await sembrarCarrito(page, 'bd', modeloClaveReal, 501, 2);
    await mockCatalogoReal(page, 1750, 3);

    let requestCheckout: unknown = null;
    await page.route('**/tienda/checkout/validar', async route => {
      requestCheckout = route.request().postDataJSON();
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: {
            validacionId: referenciaValidada,
            expiraUtc: new Date(Date.now() + 10 * 60 * 1000).toISOString(),
            subtotal: 4200,
            total: 4200,
            lineas: [{
              productoId: 501,
              modeloId: 5010,
              nombre: 'Producto Checkout Real',
              modelo: 'Modelo auditoría',
              sku: 'SKU-501-A',
              unidades: 2,
              stockDisponible: 3,
              precioUnitario: 2100,
              total: 4200
            }]
          }
        })
      });
    });

    await page.goto('/varistorehn/checkout?fuente=bd');
    await expect(page.getByRole('heading', { name: 'Tu compra' })).toBeVisible();
    expect(requestCheckout).toEqual({
      items: [{
        productoId: 501,
        productoVarianteId: productoVarianteReal,
        modeloId: null,
        modeloNombre: null,
        marcaNombre: null,
        unidades: 2
      }]
    });
    expect(JSON.stringify(requestCheckout)).not.toMatch(/precio|stock|total/i);

    const resumen = page.locator('.summary-card');
    await expect(resumen).toContainText('Producto Checkout Real');
    await expect(resumen).toContainText('2 ×');
    await expect(resumen).toContainText('2,100');
    await expect(resumen).toContainText('4,200');
    await expect(page.getByRole('button', { name: 'Tarjeta no disponible' })).toBeDisabled();

    await llenarComprador(page);
    await page.getByRole('button', { name: 'Preparar pedido por WhatsApp' }).click();
    const enlace = page.getByRole('link', { name: 'Abrir WhatsApp y continuar' });
    await expect(enlace).toBeVisible();
    const href = await enlace.getAttribute('href');
    expect(href).toBeTruthy();
    const destino = new URL(href!);
    expect(destino.origin).toBe('https://wa.me');
    expect(destino.pathname).toBe('/50498765432');
    const mensaje = destino.searchParams.get('text') || '';
    expect(mensaje).toContain(`🔖 *Referencia:* ${referenciaValidada}`);
    expect(mensaje).toContain('Producto Checkout Real');
    expect(mensaje).toContain('🛍️ *Nueva solicitud de compra — VariStore Checkout Audit*');
    expect(mensaje).toContain('👤 *Cliente*');
    expect(mensaje).toContain('Cliente Auditoría');
    expect(mensaje).toContain('📱 *Teléfono:* 50499991111');
    expect(mensaje).toContain('✉️ *Correo:* cliente@example.com');
    expect(mensaje).toContain('SKU: SKU-501-A');
    expect(mensaje).toMatch(/🔗 Ver producto: http:\/\/(?:localhost|127\.0\.0\.1):4200\/varistorehn\/producto\/producto-checkout-real-501/);
    expect(mensaje).toContain('Cantidad: 2');
    expect(mensaje).toContain('Precio unitario:');
    expect(mensaje).toContain('Subtotal:');
    expect(mensaje).toContain('💰 *TOTAL:');
    expect(mensaje).toContain('4,200');
    expect(mensaje).toContain('📝 *Nota del cliente*');
    expect(mensaje).toContain('Entregar por la tarde.');
    expect(mensaje).toContain('La tienda confirmará disponibilidad, entrega y condiciones antes de finalizar la compra.');

    const piiAntes = await page.evaluate(() => ({
      local: Object.entries(localStorage).filter(([key]) => key.startsWith('varistorehn:')).map(([, value]) => value).join('\n'),
      recibos: Object.keys(sessionStorage).filter(key => key.startsWith('varistorehn:pedido:'))
    }));
    expect(piiAntes.local).not.toContain('Cliente Auditoría');
    expect(piiAntes.local).not.toContain('cliente@example.com');
    expect(piiAntes.local).not.toContain('99991111');
    expect(piiAntes.recibos).toHaveLength(0);

    page.on('popup', popup => { void popup.close(); });
    await enlace.click();
    await expect(page).toHaveURL(new RegExp(`/varistorehn/pedido/${referenciaValidada}$`));
    await expect(page.getByRole('heading', { level: 1, name: 'Tu solicitud quedó preparada para WhatsApp' })).toBeVisible();
    await expect(page.locator('.reference-box')).toContainText(referenciaValidada);

    const reciboRaw = await page.evaluate(ref => sessionStorage.getItem(`varistorehn:pedido:v1:${ref}`), referenciaValidada);
    expect(reciboRaw).toBeTruthy();
    const recibo = JSON.parse(reciboRaw!);
    expect(Object.keys(recibo).sort()).toEqual(['creadoUtc', 'estado', 'expiraUtc', 'lineas', 'moneda', 'referencia', 'total']);
    expect(recibo.estado).toBe('whatsapp-preparado');
    expect(recibo.total).toBe(4200);
    expect(reciboRaw).not.toContain('Cliente Auditoría');
    expect(reciboRaw).not.toContain('cliente@example.com');
    expect(reciboRaw).not.toContain('99991111');
  });

  test('si vence la validación después de preparar WhatsApp bloquea la salida y exige revalidar', async ({ page }) => {
    await prepararEmpresa(page);
    await sembrarCarrito(page, 'bd', modeloClaveReal, 501, 2);
    await mockCatalogoReal(page, 1750, 3);
    await mockCheckoutValido(page);
    await page.goto('/varistorehn/checkout?fuente=bd');
    await llenarComprador(page);
    await page.getByRole('button', { name: 'Preparar pedido por WhatsApp' }).click();

    const enlace = page.getByRole('link', { name: 'Abrir WhatsApp y continuar' });
    await expect(enlace).toBeVisible();
    let popups = 0;
    page.on('popup', popup => {
      popups += 1;
      void popup.close();
    });

    await page.evaluate(() => {
      const ahora = Date.now();
      Date.now = () => ahora + 11 * 60 * 1000;
    });
    await enlace.click();

    await expect(page).toHaveURL(/\/varistorehn\/checkout\?fuente=bd$/);
    await expect(page.getByText('La validación del carrito venció antes de abrir WhatsApp. Actualiza precios y existencias para continuar.')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Abrir WhatsApp y continuar' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Preparar pedido por WhatsApp' })).toBeDisabled();
    expect(popups).toBe(0);
    const recibos = await page.evaluate(() => Object.keys(sessionStorage).filter(key => key.startsWith('varistorehn:pedido:')));
    expect(recibos).toHaveLength(0);
  });

  test('cambio de stock/precio devuelve conflicto y bloquea cualquier canal de salida', async ({ page }) => {
    await prepararEmpresa(page);
    await sembrarCarrito(page, 'bd', modeloClaveReal, 501, 2);
    await mockCatalogoReal(page, 1750, 3);
    await page.route('**/tienda/checkout/validar', async route => {
      await route.fulfill({
        status: 409,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: false, message: 'Cambió la existencia disponible. Actualiza el carrito antes de continuar.', data: null })
      });
    });

    await page.goto('/varistorehn/checkout?fuente=bd');
    await expect(page.getByRole('heading', { name: 'No podemos continuar todavía' })).toBeVisible();
    await expect(page.getByRole('alert')).toContainText('Cambió la existencia disponible');
    await expect(page.getByRole('button', { name: 'Preparar pedido por WhatsApp' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Ir al pago seguro' })).toHaveCount(0);

    const carrito = await page.evaluate(() => JSON.parse(localStorage.getItem('varistorehn:carrito:v2:905:bd') || '[]'));
    expect(carrito).toEqual([{ productoId: 501, modeloClave: `variante:${productoVarianteReal}`, unidades: 2 }]);
  });

  test('demo valida formulario y finaliza en recibo efímero sin datos personales', async ({ page }) => {
    await prepararEmpresa(page);
    await sembrarCarrito(page, 'demo', modeloClaveDemo, 1, 1);
    await page.goto('/varistorehn/checkout');

    await expect(page.getByText('Modo demostración:')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Tu compra' })).toBeVisible();
    await page.getByRole('button', { name: 'Simular continuación segura' }).click();
    await expect(page.getByText('Revisa los datos de contacto marcados antes de continuar.')).toBeVisible();
    await expect(page.getByText('Ingresa un nombre válido de al menos 2 caracteres.')).toBeVisible();

    await llenarComprador(page);
    await page.getByRole('button', { name: 'Simular continuación segura' }).click();
    await expect(page).toHaveURL(/\/varistorehn\/pedido\/demo-[a-z0-9]+$/i);
    await expect(page.getByRole('heading', { level: 1, name: 'Vista previa completada' })).toBeVisible();

    const recibos = await page.evaluate(() => Object.entries(sessionStorage)
      .filter(([key]) => key.startsWith('varistorehn:pedido:v1:'))
      .map(([, value]) => value));
    expect(recibos).toHaveLength(1);
    expect(recibos[0]).not.toContain('Cliente Auditoría');
    expect(recibos[0]).not.toContain('cliente@example.com');
    expect(recibos[0]).not.toContain('99991111');
  });

  test('referencia ausente o expirada no se presenta como pedido confirmado', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/varistorehn/pedido/referencia-inexistente-123');

    await expect(page.getByRole('heading', { level: 1, name: 'No encontramos una confirmación vigente en esta sesión' })).toBeVisible();
    await expect(page.getByText('No inventamos un estado de pedido cuando no podemos comprobarlo.')).toBeVisible();
    await expect(page.locator('.success-icon')).toHaveCount(0);
  });

  test('checkout mantiene controles táctiles y sin overflow en 320px', async ({ page }) => {
    await prepararEmpresa(page);
    await sembrarCarrito(page, 'demo', modeloClaveDemo, 1, 1);
    await page.setViewportSize({ width: 320, height: 760 });
    await page.goto('/varistorehn/checkout');

    await expect(page.getByRole('button', { name: 'Preparar pedido por WhatsApp' })).toBeVisible();
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow).toBeLessThanOrEqual(1);
    await expect(page.getByRole('button', { name: 'Preparar pedido por WhatsApp' })).toHaveCSS('min-height', '44px');
    await expect(page.getByRole('button', { name: 'Simular continuación segura' })).toHaveCSS('min-height', '44px');
  });
});
