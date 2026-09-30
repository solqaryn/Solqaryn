import { expect, Page, test } from '@playwright/test';

const TOKEN = 'fase12_cliente_token_seguro_0123456789ABCDEF';
const empresa = {
  id: 1200,
  nombreComercial: 'Storefront Fase 12',
  nombreVisibleSistema: 'Administración interna',
  eslogan: 'Compra sin barreras',
  descripcionSistema: 'Administrativo',
  mensajeLogin: 'Administración',
  copyright: '© 2026 Storefront Fase 12',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Tienda pública',
  piePaginaActivo: true,
  piePaginaTexto: 'Tienda pública',
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy'
};

const producto = {
  id: 11,
  slug: 'producto-cuenta-11',
  nombre: 'Producto Cuenta 11',
  descripcion: 'Producto vigente para recompra.',
  categoriaId: 5,
  categoriaNombre: 'Tecnología',
  marcaNombre: 'Marca Cuenta',
  precio: 150,
  cantidadDisponible: 3,
  estaAgotado: false,
  estadoDisponibilidad: 'Disponible',
  activo: true,
  esDestacado: true,
  imagenes: [],
  modelos: [{
    productoVarianteId: 111,
    modeloId: 51,
    modeloNombre: 'Modelo actual',
    marcaNombre: 'Marca Cuenta',
    sku: 'CTA-111',
    precio: 150,
    cantidadDisponible: 3,
    estaAgotado: false,
    estadoDisponibilidad: 'Disponible',
    imagenes: []
  }, {
    productoVarianteId: 112,
    modeloId: 52,
    modeloNombre: 'Modelo historico seguro',
    marcaNombre: 'Marca Cuenta',
    sku: 'CTA-112',
    precio: 175,
    cantidadDisponible: 2,
    estaAgotado: false,
    estadoDisponibilidad: 'Disponible',
    imagenes: []
  }]
};

const pedidoSinVariante = {
  id: 7002,
  estado: 'Confirmado',
  total: 175,
  fechaUtc: '2026-09-17T16:00:00Z',
  lineas: [{
    productoId: 11,
    productoVarianteId: null,
    nombre: 'Producto Cuenta 11',
    modelo: 'Modelo historico seguro',
    cantidad: 1,
    precioUnitario: 175,
    total: 175
  }]
};

const pedido = {
  id: 7001,
  estado: 'Confirmado',
  total: 200,
  fechaUtc: '2026-09-18T16:00:00Z',
  lineas: [{
    productoId: 11,
    productoVarianteId: 111,
    nombre: 'Producto Cuenta 11',
    modelo: 'Modelo anterior',
    cantidad: 2,
    precioUnitario: 100,
    total: 200
  }]
};

async function prepararBase(page: Page): Promise<void> {
  await page.route('http://localhost:5005/empresa-configuracion/publica', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: empresa })
  }));

  await page.route('http://localhost:5005/tienda/productos?*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({
      success: true,
      data: { items: [producto], page: 1, pageSize: 96, totalCount: 1 }
    })
  }));

  await page.route('http://localhost:5005/tienda/productos/destacados*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: [producto] })
  }));

  await page.route('http://localhost:5005/tienda/productos/contexto', route => {
    const body = route.request().postDataJSON() as { productoIds?: number[] } | null;
    const ids = new Set(Array.isArray(body?.productoIds) ? body!.productoIds : []);
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: ids.has(producto.id) ? [producto] : [] })
    });
  });

  await page.route('http://localhost:5005/tienda/categorias', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: [] })
  }));
}

async function prepararCuentaAutenticada(
  page: Page,
  registrarCabeceras?: (ok: boolean) => void,
  pedidosMock: typeof pedido[] = [pedido]
): Promise<void> {
  await page.addInitScript(token => {
    sessionStorage.setItem('storefront:cuenta:session:v1', token);
  }, TOKEN);

  const autorizado = (pageRequest: import('@playwright/test').Request) => {
    const ok = pageRequest.headers()['x-storefront-session'] === TOKEN;
    registrarCabeceras?.(ok);
    return ok;
  };

  await page.route('http://localhost:5005/tienda/cuenta', route => {
    if (!autorizado(route.request())) return route.fulfill({ status: 401, contentType: 'application/json', body: JSON.stringify({ success: false, message: 'No autorizado' }) });
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, data: { id: 12, nombre: 'Cliente Fase 12', correo: 'cliente@example.com' } })
    });
  });

  await page.route('http://localhost:5005/tienda/cuenta/direcciones', route => {
    if (!autorizado(route.request())) return route.fulfill({ status: 401, contentType: 'application/json', body: JSON.stringify({ success: false }) });
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: [{ id: 1, alias: 'Casa', recibe: 'Cliente Fase 12', telefono: '9999-9999', direccion: 'Colonia segura', predeterminada: true }]
      })
    });
  });

  await page.route('http://localhost:5005/tienda/cuenta/favoritos', route => {
    if (!autorizado(route.request())) return route.fulfill({ status: 401, contentType: 'application/json', body: JSON.stringify({ success: false }) });
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: [11] }) });
  });

  await page.route('http://localhost:5005/tienda/cuenta/pedidos', route => {
    if (!autorizado(route.request())) return route.fulfill({ status: 401, contentType: 'application/json', body: JSON.stringify({ success: false }) });
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: pedidosMock }) });
  });

  await page.route('http://localhost:5005/tienda/cuenta/notificaciones', route => {
    if (!autorizado(route.request())) return route.fulfill({ status: 401, contentType: 'application/json', body: JSON.stringify({ success: false }) });
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, data: [{ pedidoId: 7001, estado: 'Confirmado', mensaje: 'Pedido #7001 confirmado.', fechaUtc: '2026-09-18T17:00:00Z' }] })
    });
  });
}

test.describe('Storefront Fase 12 — cuenta de cliente y evolución', () => {
  test.describe.configure({ retries: 0 });

  test('checkout y catálogo siguen disponibles sin cuenta', async ({ page }) => {
    await prepararBase(page);
    let llamadasCuenta = 0;
    await page.route('http://localhost:5005/tienda/cuenta**', route => {
      llamadasCuenta += 1;
      return route.fulfill({ status: 500, contentType: 'application/json', body: '{}' });
    });

    await page.goto('/tienda/productos');
    await expect(page).toHaveURL(/\/tienda\/productos/);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

    await page.goto('/tienda/checkout');
    await expect(page).toHaveURL(/\/tienda\/checkout/);
    await expect(page.getByRole('heading', { level: 1, name: 'Confirma tus datos y tu forma de compra' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Tu carrito está vacío' })).toBeVisible();
    expect(llamadasCuenta).toBe(0);
  });

  test('crear cuenta es opcional y guarda la sesión solo en sessionStorage', async ({ page }) => {
    await prepararBase(page);

    await page.route('http://localhost:5005/tienda/cuenta/registrar', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: {
          token: TOKEN,
          expiraUtc: '2026-09-19T12:00:00Z',
          perfil: { id: 12, nombre: 'Cliente Nuevo', correo: 'nuevo@example.com' }
        }
      })
    }));
    await page.route('http://localhost:5005/tienda/cuenta/direcciones', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: [] }) }));
    await page.route('http://localhost:5005/tienda/cuenta/favoritos', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: [] }) }));
    await page.route('http://localhost:5005/tienda/cuenta/pedidos', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: [] }) }));
    await page.route('http://localhost:5005/tienda/cuenta/notificaciones', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: [] }) }));

    await page.goto('/tienda/cuenta');
    await expect(page.getByText('La cuenta es opcional.', { exact: false })).toBeVisible();
    await page.getByRole('tab', { name: 'Crear cuenta' }).click();
    await page.getByLabel('Nombre').fill('Cliente Nuevo');
    await page.getByLabel('Correo').fill('nuevo@example.com');
    await page.getByLabel('Contraseña').fill('UnaClaveSegura12');
    await page.getByRole('button', { name: 'Crear cuenta' }).click();

    await expect(page.getByRole('heading', { level: 2, name: 'Cliente Nuevo' })).toBeVisible();
    const storage = await page.evaluate(() => ({
      session: sessionStorage.getItem('storefront:cuenta:session:v1'),
      local: localStorage.getItem('storefront:cuenta:session:v1')
    }));
    expect(storage.session).toBe(TOKEN);
    expect(storage.local).toBeNull();
  });

  test('historial direcciones favoritos y notificaciones usan la sesión de cliente', async ({ page }) => {
    await prepararBase(page);
    const cabeceras: boolean[] = [];
    await prepararCuentaAutenticada(page, ok => cabeceras.push(ok));

    await page.goto('/tienda/cuenta');

    await expect(page.getByRole('heading', { level: 2, name: 'Cliente Fase 12' })).toBeVisible();
    await expect(page.getByRole('region', { name: 'Direcciones guardadas' }).getByText('Casa', { exact: false }).first()).toBeVisible();
    await expect(page.getByText('Producto Cuenta 11', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('Pedido #7001 confirmado.', { exact: true })).toBeVisible();
    await expect(page.getByText('Pedido #7001', { exact: true })).toBeVisible();
    expect(cabeceras.length).toBeGreaterThanOrEqual(5);
    expect(cabeceras.every(Boolean)).toBe(true);
  });

  test('recompra histórica sin variante usa snapshot de modelo y no el primer modelo actual', async ({ page }) => {
    await prepararBase(page);
    await prepararCuentaAutenticada(page, undefined, [pedidoSinVariante]);

    await page.goto('/tienda/cuenta');
    await page.locator('details summary').filter({ hasText: 'Pedido #7002' }).click();
    await page.getByRole('button', { name: 'Recomprar con stock y precio actuales' }).click();
    await expect(page).toHaveURL(/\/tienda\/carrito/);

    const carrito = await page.evaluate(() => {
      const key = Object.keys(localStorage).find(item => item.startsWith('storefront:carrito:v2:') && item.endsWith(':bd')) || '';
      return key ? JSON.parse(localStorage.getItem(key) || '[]') : [];
    });
    expect(carrito).toEqual([{ productoId: 11, modeloClave: 'variante:112', unidades: 1 }]);
  });

  test('recompra guarda solo referencias y vuelve a validar catálogo vigente', async ({ page }) => {
    await prepararBase(page);
    await prepararCuentaAutenticada(page);

    await page.goto('/tienda/cuenta');
    await page.locator('details summary').filter({ hasText: 'Pedido #7001' }).click();
    await page.getByRole('button', { name: 'Recomprar con stock y precio actuales' }).click();
    await expect(page).toHaveURL(/\/tienda\/carrito/);

    const carrito = await page.evaluate(() => {
      const key = Object.keys(localStorage).find(item => item.startsWith('storefront:carrito:v2:') && item.endsWith(':bd')) || '';
      return { key, raw: key ? localStorage.getItem(key) : null };
    });
    expect(carrito.key).toContain(':1200:bd');
    expect(carrito.raw).not.toBeNull();
    const referencias = JSON.parse(carrito.raw || '[]');
    expect(referencias).toEqual([{ productoId: 11, modeloClave: 'variante:111', unidades: 2 }]);
    expect(carrito.raw).not.toContain('precio');
    expect(carrito.raw).not.toContain('100');
  });
});
