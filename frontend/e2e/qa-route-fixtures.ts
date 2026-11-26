import { request as playwrightRequest } from '@playwright/test';

export type RouteFixture = { url: string } | { error: string };
export type RouteFixtureManifest = Record<string, RouteFixture>;
export const FIXTURE_ENV = 'QA_ROUTE_FIXTURE_MANIFEST';

// Only the isolated QA runner may create these records. No mocks, direct
// commercial SQL writes, external messages or payment-provider calls.
export async function prepareRouteFixtures(): Promise<void> {
  const connection = process.env.ConnectionStrings__DefaultConnection || '';
  if (process.env.CI !== 'true' || process.env.GITHUB_REF !== 'refs/heads/qa' ||
      process.env.E2E_TENANT_ID !== '1' || process.env.PHASE7_API_URL !== 'http://127.0.0.1:5005' ||
      !/^Server=127\.0\.0\.1;Port=3306;Database=solqaryn_qa_route_audit;User=root;Password=[^;]+;SslMode=Required;$/.test(connection)) {
    throw new Error('Fixtures comerciales denegados fuera de MySQL desechable QA.');
  }

  const families = {
    common: ['/productos/1', '/productos/1/editar', '/productos/1/variantes',
      '/sucursales/1/editar', '/almacenes/1/editar'],
    inventory: ['/inventario/conteos/1', '/inventario/conteos/1/editar',
      '/inventario/ajustes/1', '/inventario/ajustes/1/editar'],
    sales: ['/ventas/1', '/ventas/1/editar', '/facturas/1', '/facturas/1/pagos'],
    quotes: ['/cotizaciones/1', '/cotizaciones/1/editar', '/pedidos-venta/1', '/pedidos-venta/1/editar'],
    purchases: ['/compras/1', '/compras/1/editar', '/ordenes-compra/1/editar',
      '/compras/1/three-way-match', '/recepciones-compra/1', '/facturas-proveedor/1',
      '/facturas-proveedor/1/editar', '/devoluciones-proveedor/1', '/devoluciones-proveedor/1/editar']
  } as const;
  const manifest: RouteFixtureManifest = Object.fromEntries(
    Object.values(families).flat().map(route => [route, { error: 'Fixture no preparado.' }])
  );
  const api = await playwrightRequest.newContext({ baseURL: 'http://127.0.0.1:5005', timeout: 12000 });
  let auth: Record<string, string> = {};
  let stage = 'login';
  let operation = 0;
  let lastStatus: number | undefined;
  const nonce = Date.now().toString(36);
  const label = 'QA matriz ' + nonce;

  // Do not publish response bodies, credentials or request headers in artifacts.
  async function call(path: string, method: 'GET' | 'POST' = 'GET',
      data?: Record<string, unknown>, multipart?: Record<string, string>): Promise<any> {
    stage = method + ' ' + path;
    lastStatus = undefined;
    const response = await api.fetch(path, {
      method, headers: { ...auth, ...(method === 'POST' ? { 'Idempotency-Key': 'qa-' + nonce + '-' + (++operation) } : {}) },
      data, multipart, maxRedirects: 0
    });
    lastStatus = response.status();
    if (!response.ok()) throw new Error('HTTP ' + lastStatus);
    const payload = await response.json();
    if (payload?.success !== true && payload?.Success !== true) throw new Error('Respuesta sin éxito');
    const value = payload.data ?? payload.Data;
    if (value == null) throw new Error('Respuesta sin datos');
    return value;
  }
  function idOf(value: any): number {
    const id = Number(value?.id ?? value?.Id);
    if (!Number.isInteger(id) || id <= 0) throw new Error('Identificador no válido');
    return id;
  }
  function detailId(value: any): number {
    if (!Array.isArray(value?.detalles) || value.detalles.length !== 1) throw new Error('Detalle no materializado');
    return idOf(value.detalles[0]);
  }
  async function persisted(path: string, id: number): Promise<any> {
    const row = await call(path + '/' + id);
    if (idOf(row) !== id) throw new Error('Identidad de registro incorrecta');
    return row;
  }
  function bind(route: string, url: string): void {
    if (!(route in manifest) || !url.startsWith('/') || url.startsWith('//')) throw new Error('Ruta fixture fuera del catálogo');
    manifest[route] = { url };
  }
  async function family(routes: readonly string[], fn: () => Promise<void>): Promise<void> {
    try { await fn(); }
    catch {
      const reason = 'Fixture real no acreditado: ' + stage + (lastStatus === undefined ? '' : ' (HTTP ' + lastStatus + ')');
      for (const route of routes) if ('error' in manifest[route]) manifest[route] = { error: reason };
      console.warn('[qa-fixtures] ' + reason);
    }
  }

  try {
    if (!process.env.PHASE7_ADMIN_USERNAME || !process.env.PHASE7_ADMIN_PASSWORD) throw new Error('Credenciales CI ausentes');
    const login = await call('/auth/login', 'POST', { nombreUsuario: process.env.PHASE7_ADMIN_USERNAME, password: process.env.PHASE7_ADMIN_PASSWORD });
    if (typeof login.token !== 'string' || !login.token) throw new Error('Login sin JWT');
    auth = { Authorization: 'Bearer ' + login.token, 'X-Empresa-Id': '1' };
    await call('/tenant-context/1');
    let productoId = 0, varianteId = 0, almacenId = 0;
    await family(families.common, async () => {
      const sucursalId = idOf(await call('/sucursales', 'POST', { codigo: 'QA-' + nonce, nombre: label, direccion: 'Datos de prueba CI', zonaHoraria: 'America/Tegucigalpa' }));
      await persisted('/sucursales', sucursalId);
      bind('/sucursales/1/editar', '/sucursales/' + sucursalId + '/editar');
      almacenId = idOf(await call('/almacenes', 'POST', { sucursalId, codigo: 'ALM-' + nonce, nombre: label, tipo: 'Bodega' }));
      await persisted('/almacenes', almacenId);
      bind('/almacenes/1/editar', '/almacenes/' + almacenId + '/editar');
      const colorId = idOf(await call('/colores', 'POST', { nombre: label, descripcion: 'Fixture QA aislado', codigoVisual: '#355C7D', orden: 92 }));
      productoId = idOf(await call('/productos', 'POST', undefined, {
        Nombre: label, Descripcion: 'Producto representativo de CI', AlmacenInicialId: String(almacenId),
        Cantidad: '30', Costo: '100', Precio: '180', UmbralStockBajo: '2',
        'Variantes[0].ColorId': String(colorId), 'Variantes[0].Sku': 'QA-' + nonce,
        'Variantes[0].Cantidad': '30', 'Variantes[0].Costo': '100', 'Variantes[0].Precio': '180',
        'Variantes[0].UmbralStockBajo': '2', 'Variantes[0].Activo': 'true'
      }));
      await persisted('/productos', productoId);
      const variantes = await call('/productos/' + productoId + '/variantes');
      if (!Array.isArray(variantes)) throw new Error('Variantes no materializadas');
      varianteId = idOf(variantes.find((v: any) => v.sku === 'QA-' + nonce && v.activo === true));
      for (const suffix of ['', '/editar', '/variantes']) bind('/productos/1' + suffix, '/productos/' + productoId + suffix);
    });
    function requireStock(): void {
      stage = 'producto y almacén de prueba';
      lastStatus = undefined;
      if (!productoId || !varianteId || !almacenId) throw new Error('Fixture base no acreditado');
    }
    await family(families.inventory, async () => {
      requireStock();
      const ajusteId = idOf(await call('/inventario/ajustes', 'POST', { motivo: label, observaciones: 'Ajuste borrador CI', detalles: [{ productoId, productoVarianteId: varianteId, almacenId, ubicacionAlmacenId: null, cantidadObjetivo: 32 }] }));
      await persisted('/inventario/ajustes', ajusteId);
      bind('/inventario/ajustes/1', '/inventario/ajustes/' + ajusteId);
      bind('/inventario/ajustes/1/editar', '/inventario/ajustes/' + ajusteId + '/editar');
      const conteoId = idOf(await call('/conteos-inventario', 'POST', { tipo: 1, almacenId, esCiego: false, observaciones: label, productoVarianteIds: [varianteId] }));
      await persisted('/conteos-inventario', conteoId);
      bind('/inventario/conteos/1', '/inventario/conteos/' + conteoId);
      bind('/inventario/conteos/1/editar', '/inventario/conteos/' + conteoId + '/editar');
    });
    let clienteId = 0, proveedorId = 0, metodoPago = '';
    await family([...families.sales, ...families.quotes], async () => {
      clienteId = idOf(await call('/clientes', 'POST', { nombre: label, correo: 'qa@example.invalid' }));
      await persisted('/clientes', clienteId);
    });
    await family([...families.sales, ...families.purchases], async () => {
      const metodos = await call('/metodos-pago/activos');
      const efectivo = Array.isArray(metodos) ? metodos.find((m: any) => m.codigo === 'EFECTIVO' || m.nombre === 'Efectivo') : undefined;
      if (!efectivo) {
        const creado = await call('/metodos-pago', 'POST', { codigo: 'QA_EFECTIVO', nombre: 'Efectivo QA', tipo: 'Efectivo', activo: true, orden: 0, permiteCambio: true });
        idOf(creado);
        metodoPago = creado.nombre;
      } else { metodoPago = efectivo.nombre; }
      if (!metodoPago) throw new Error('Método de pago no acreditado');
    });
    await family(families.sales, async () => {
      requireStock();
      stage = 'cliente y método de pago';
      if (!clienteId || !metodoPago) throw new Error('Datos base de venta ausentes');
      const data = { clienteId, clienteNombre: label, metodoPago, estadoPago: 'Pendiente', notas: label,
        detalles: [{ productoId, productoVarianteId: varianteId, cantidad: 1, precioUnitario: 180 }] };
      const draft = idOf(await call('/ventas', 'POST', data));
      await persisted('/ventas', draft);
      bind('/ventas/1/editar', '/ventas/' + draft + '/editar');
      const ventaId = idOf(await call('/ventas', 'POST', data));
      await call('/ventas/' + ventaId + '/confirmar', 'POST');
      const venta = await persisted('/ventas', ventaId);
      if (venta.estado !== 'Confirmada') throw new Error('Venta no confirmada');
      bind('/ventas/1', '/ventas/' + ventaId);
      const facturaId = idOf(await call('/facturas/venta/' + ventaId));
      const factura = await persisted('/facturas', facturaId);
      if (Number(factura.ventaId) !== ventaId) throw new Error('Factura no corresponde a venta');
      bind('/facturas/1', '/facturas/' + facturaId);
      bind('/facturas/1/pagos', '/facturas/' + facturaId + '/pagos');
    });
    await family(families.quotes, async () => {
      requireStock();
      stage = 'cliente de prueba';
      if (!clienteId) throw new Error('Cliente ausente');
      const data = { clienteId, observaciones: label, detalles: [{ productoId, productoVarianteId: varianteId, cantidad: 1, precioUnitario: 180 }] };
      const draft = idOf(await call('/cotizaciones', 'POST', data));
      await persisted('/cotizaciones', draft);
      bind('/cotizaciones/1/editar', '/cotizaciones/' + draft + '/editar');
      const cotizacionId = idOf(await call('/cotizaciones', 'POST', data));
      await call('/cotizaciones/' + cotizacionId + '/enviar', 'POST');
      await call('/cotizaciones/' + cotizacionId + '/aceptar', 'POST');
      await persisted('/cotizaciones', cotizacionId);
      bind('/cotizaciones/1', '/cotizaciones/' + cotizacionId);
      const pedidoId = idOf(await call('/pedidos-venta', 'POST', { cotizacionId, observaciones: label }));
      await persisted('/pedidos-venta', pedidoId);
      bind('/pedidos-venta/1', '/pedidos-venta/' + pedidoId);
      bind('/pedidos-venta/1/editar', '/pedidos-venta/' + pedidoId + '/editar');
    });
    await family(families.purchases, async () => {
      requireStock();
      proveedorId = idOf(await call('/proveedores', 'POST', { nombre: label, correo: 'proveedor@example.invalid' }));
      await persisted('/proveedores', proveedorId);
      stage = 'método de pago de compra';
      if (!metodoPago) throw new Error('Método de pago ausente');
      const compraId = idOf(await call('/compras', 'POST', { proveedorId, proveedorNombre: label, metodoPago, estadoPago: 'Pendiente', documentoReferencia: 'QA-' + nonce, notas: label, detalles: [{ productoId, productoVarianteId: varianteId, cantidad: 2, costoUnitario: 100 }] }));
      await persisted('/compras', compraId);
      bind('/compras/1', '/compras/' + compraId);
      bind('/compras/1/editar', '/compras/' + compraId + '/editar');
      const orderData = { proveedorId, moneda: 'HNL', observaciones: label, detalles: [{ productoId, productoVarianteId: varianteId, cantidadOrdenada: 4, precioUnitario: 100, descuento: 0, impuesto: 0 }] };
      const draft = idOf(await call('/ordenes-compra', 'POST', orderData));
      await persisted('/ordenes-compra', draft);
      bind('/ordenes-compra/1/editar', '/ordenes-compra/' + draft + '/editar');
      const ordenId = idOf(await call('/ordenes-compra', 'POST', orderData));
      await call('/ordenes-compra/' + ordenId + '/enviar-aprobacion', 'POST');
      await call('/ordenes-compra/' + ordenId + '/aprobar', 'POST');
      const orden = await persisted('/ordenes-compra', ordenId);
      if (orden.estado !== 3 && orden.estado !== 'Aprobada') throw new Error('Orden no aprobada');
      const ordenDetalleId = detailId(orden);
      const recepcionId = idOf(await call('/recepciones-compra', 'POST', { ordenCompraId: ordenId, observaciones: label, detalles: [{ ordenCompraDetalleId: ordenDetalleId, almacenId, cantidadRecibida: 4, cantidadDanada: 0, cantidadFaltante: 0, cantidadSobrante: 0 }] }));
      await call('/recepciones-compra/' + recepcionId + '/confirmar', 'POST');
      const recepcion = await persisted('/recepciones-compra', recepcionId);
      if (recepcion.estado !== 2 && recepcion.estado !== 'Recibida') throw new Error('Recepción no materializada');
      bind('/recepciones-compra/1', '/recepciones-compra/' + recepcionId);
      const invoice = { proveedorId, ordenCompraId: ordenId, numeroFactura: 'QA-' + nonce, fechaEmisionUtc: new Date().toISOString(), moneda: 'HNL', detalles: [{ ordenCompraDetalleId: ordenDetalleId, cantidadFacturada: 4, precioUnitario: 100, descuento: 0, impuesto: 0 }] };
      const facturaId = idOf(await call('/facturas-proveedor', 'POST', invoice));
      await call('/facturas-proveedor/' + facturaId + '/registrar', 'POST');
      const registrada = await persisted('/facturas-proveedor', facturaId);
      if (registrada.estado !== 2 && registrada.estado !== 'Registrada') throw new Error('Factura de proveedor no registrada');
      bind('/facturas-proveedor/1', '/facturas-proveedor/' + facturaId);
      await call('/conciliacion/ordenes-compra/' + ordenId + '/three-way-match');
      bind('/compras/1/three-way-match', '/compras/' + ordenId + '/three-way-match');
      const devolucionId = idOf(await call('/devoluciones-proveedor', 'POST', { recepcionCompraId: recepcionId, facturaProveedorId: facturaId, motivo: label, detalles: [{ recepcionCompraDetalleId: detailId(recepcion), cantidad: 1 }] }));
      await persisted('/devoluciones-proveedor', devolucionId);
      bind('/devoluciones-proveedor/1', '/devoluciones-proveedor/' + devolucionId);
      bind('/devoluciones-proveedor/1/editar', '/devoluciones-proveedor/' + devolucionId + '/editar');
      // Draft invoice is independent of the proven receipt/match/return chain.
      const draftInvoiceId = idOf(await call('/facturas-proveedor', 'POST', { ...invoice, numeroFactura: 'QA-B-' + nonce, detalles: [{ ...invoice.detalles[0], cantidadFacturada: 1 }] }));
      await persisted('/facturas-proveedor', draftInvoiceId);
      bind('/facturas-proveedor/1/editar', '/facturas-proveedor/' + draftInvoiceId + '/editar');
    });
  } finally {
    await api.dispose();
    process.env[FIXTURE_ENV] = JSON.stringify(manifest);
    console.log('[qa-fixtures] rutas con registro verificado: ' + Object.values(manifest).filter(item => 'url' in item).length + '/' + Object.keys(manifest).length);
  }
}
