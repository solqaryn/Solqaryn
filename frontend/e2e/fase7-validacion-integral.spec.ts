import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const suffix = `${Date.now()}`;

let token = '';
let marcaId = 0;
let modeloId = 0;
let productoVariantesId = 0;
let productoSimpleId = 0;
let productoSimpleVarianteTecnicaId = 0;
let varianteBlancaId = 0;
let varianteNegraId = 0;
let varianteRojaId = 0;
let envioPredeterminado: Record<string, any>;
let envioAlternativo: Record<string, any>;
let descuentoCodigo = '';

const nombres = {
  marca: `Marca F7 ${suffix}`,
  modelo: `Modelo F7 ${suffix}`,
  productoVariantes: `Buds Pro 3 F7 ${suffix}`,
  productoSimple: `Cargador F7 ${suffix}`,
  blanco: `Blanco F7 ${suffix}`,
  negro: `Negro F7 ${suffix}`,
  rojo: `Rojo F7 ${suffix}`,
  envio: `Envío alternativo F7 ${suffix}`
};

function headers(): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

async function dataOf(response: APIResponse): Promise<any> {
  const payload = await response.json();
  return payload.data ?? payload.Data;
}

async function loginApi(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, {
    data: { nombreUsuario: ADMIN_USERNAME, password: ADMIN_PASSWORD }
  });
  expect(response.status(), await response.text()).toBe(200);
  return (await dataOf(response)).token;
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(url => url.pathname !== '/login', { timeout: 20_000 });
}

async function crearCatalogo(
  request: APIRequestContext,
  ruta: string,
  data: Record<string, unknown>
): Promise<Record<string, any>> {
  const response = await request.post(`${API_URL}/${ruta}`, { headers: headers(), data });
  expect(response.status(), `${ruta}: ${await response.text()}`).toBe(201);
  return await dataOf(response);
}

async function crearProductoSimple(
  request: APIRequestContext,
  nombre: string,
  cantidad: number,
  precio: number
): Promise<Record<string, any>> {
  const response = await request.post(`${API_URL}/productos`, {
    headers: headers(),
    multipart: {
      Nombre: nombre,
      MarcaId: String(marcaId),
      ModeloId: String(modeloId),
      Cantidad: String(cantidad),
      Costo: '60',
      Precio: String(precio),
      UmbralStockBajo: '1'
    }
  });
  expect(response.status(), await response.text()).toBe(201);
  return await dataOf(response);
}

async function obtenerVarianteTecnicaId(
  request: APIRequestContext,
  productoId: number
): Promise<number> {
  const response = await request.get(`${API_URL}/productos/${productoId}/variantes`, {
    headers: headers()
  });
  expect(response.status(), await response.text()).toBe(200);
  const variantes = await dataOf(response);
  const tecnica = variantes.find((item: any) => item.esTecnica === true);
  expect(tecnica, `El producto ${productoId} debe tener una variante técnica.`).toBeTruthy();
  return Number(tecnica.id);
}

async function crearVenta(
  request: APIRequestContext,
  detalles: Array<Record<string, unknown>>,
  opciones: Record<string, unknown> = {}
): Promise<Record<string, any>> {
  const response = await request.post(`${API_URL}/ventas`, {
    headers: headers(),
    data: {
      clienteNombre: `Cliente F7 ${suffix}`,
      metodoPago: 'Efectivo',
      estadoPago: 'Pendiente',
      descuento: 0,
      impuesto: 0,
      detalles,
      ...opciones
    }
  });
  expect(response.status(), await response.text()).toBe(201);
  return await dataOf(response);
}

async function confirmarVenta(request: APIRequestContext, ventaId: number): Promise<Record<string, any>> {
  const response = await request.post(`${API_URL}/ventas/${ventaId}/confirmar`, { headers: headers() });
  expect(response.status(), await response.text()).toBe(200);
  return await dataOf(response);
}

async function obtenerProducto(request: APIRequestContext): Promise<Record<string, any>> {
  const response = await request.get(`${API_URL}/productos/${productoVariantesId}`, { headers: headers() });
  expect(response.status(), await response.text()).toBe(200);
  return await dataOf(response);
}

async function obtenerFactura(request: APIRequestContext, facturaId: number): Promise<Record<string, any>> {
  const response = await request.get(`${API_URL}/facturas/${facturaId}`, { headers: headers() });
  expect(response.status(), await response.text()).toBe(200);
  return await dataOf(response);
}

function variante(producto: Record<string, any>, id: number): Record<string, any> {
  const encontrada = producto.variantes.find((item: Record<string, any>) => item.id === id);
  expect(encontrada).toBeTruthy();
  return encontrada;
}

test.describe('Fase 7 — pruebas, validación integral y cierre', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    token = await loginApi(request);

    const marca = await crearCatalogo(request, 'marcas', {
      nombre: nombres.marca,
      descripcion: 'Marca temporal para certificación integral',
      orden: 70
    });
    marcaId = marca.id;

    const modelo = await crearCatalogo(request, 'modelos', {
      nombre: nombres.modelo,
      descripcion: 'Modelo temporal para certificación integral',
      catalogoPadreId: marcaId,
      orden: 70
    });
    modeloId = modelo.id;

    const blanco = await crearCatalogo(request, 'colores', {
      nombre: nombres.blanco,
      codigoVisual: '#FFFFFF',
      orden: 70
    });
    const negro = await crearCatalogo(request, 'colores', {
      nombre: nombres.negro,
      codigoVisual: '#111111',
      orden: 71
    });
    const rojo = await crearCatalogo(request, 'colores', {
      nombre: nombres.rojo,
      codigoVisual: '#DC2626',
      orden: 72
    });

    const producto = await request.post(`${API_URL}/productos`, {
      headers: headers(),
      multipart: {
        Nombre: nombres.productoVariantes,
        MarcaId: String(marcaId),
        ModeloId: String(modeloId),
        Cantidad: '10',
        Costo: '120',
        Precio: '300',
        UmbralStockBajo: '2',
        'Variantes[0].ColorId': String(blanco.id),
        'Variantes[0].Sku': `F7-W-${suffix}`,
        'Variantes[0].Cantidad': '4',
        'Variantes[0].UmbralStockBajo': '1',
        'Variantes[0].Costo': '120',
        'Variantes[0].Precio': '300',
        'Variantes[0].Activo': 'true',
        'Variantes[1].ColorId': String(negro.id),
        'Variantes[1].Sku': `F7-B-${suffix}`,
        'Variantes[1].Cantidad': '3',
        'Variantes[1].UmbralStockBajo': '1',
        'Variantes[1].Costo': '120',
        'Variantes[1].Precio': '300',
        'Variantes[1].Activo': 'true',
        'Variantes[2].ColorId': String(rojo.id),
        'Variantes[2].Sku': `F7-R-${suffix}`,
        'Variantes[2].Cantidad': '3',
        'Variantes[2].UmbralStockBajo': '1',
        'Variantes[2].Costo': '120',
        'Variantes[2].Precio': '300',
        'Variantes[2].Activo': 'true'
      }
    });
    expect(producto.status(), await producto.text()).toBe(201);
    const productoData = await dataOf(producto);
    productoVariantesId = productoData.id;
    varianteBlancaId = productoData.variantes.find((item: any) => item.colorId === blanco.id).id;
    varianteNegraId = productoData.variantes.find((item: any) => item.colorId === negro.id).id;
    varianteRojaId = productoData.variantes.find((item: any) => item.colorId === rojo.id).id;

    const simple = await crearProductoSimple(request, nombres.productoSimple, 20, 150);
    productoSimpleId = simple.id;
    productoSimpleVarianteTecnicaId = await obtenerVarianteTecnicaId(request, productoSimpleId);

    const envioResponse = await request.get(`${API_URL}/costos-envio/predeterminado`, { headers: headers() });
    expect(envioResponse.status(), await envioResponse.text()).toBe(200);
    envioPredeterminado = await dataOf(envioResponse);
  });

  test('producto con tres colores conserva 4 blancas, 3 negras, 3 rojas y total 10', async ({ request }) => {
    const producto = await obtenerProducto(request);
    expect(producto.cantidad).toBe(10);
    expect(producto.variantes).toHaveLength(3);
    expect(variante(producto, varianteBlancaId).cantidad).toBe(4);
    expect(variante(producto, varianteNegraId).cantidad).toBe(3);
    expect(variante(producto, varianteRojaId).cantidad).toBe(3);
    expect(producto.variantes.reduce((total: number, item: any) => total + item.cantidad, 0)).toBe(10);
  });

  test('envío parte de L. 80.00, se edita desde Angular y nunca quedan dos predeterminados', async ({ request, page }) => {
    expect(envioPredeterminado.monto).toBe(80);
    expect(envioPredeterminado.esPredeterminado).toBe(true);

    const crear = await request.post(`${API_URL}/costos-envio`, {
      headers: headers(),
      data: {
        nombre: nombres.envio,
        descripcion: 'Costo alternativo para validación de Fase 7',
        monto: 95,
        prioridad: 90,
        esPredeterminado: false,
        activo: true
      }
    });
    expect(crear.status(), await crear.text()).toBe(201);
    envioAlternativo = await dataOf(crear);

    await loginUi(page);
    await page.goto('/costos-envio');
    const fila = page.locator('tr', { hasText: nombres.envio });
    await expect(fila).toBeVisible();
    await fila.getByRole('button', { name: 'Editar costo de envío' }).click();
    await page.locator('input[formcontrolname="monto"]').fill('90');
    const actualizacion = page.waitForResponse(response =>
      response.url().endsWith(`/costos-envio/${envioAlternativo.id}`) && response.request().method() === 'PUT');
    await page.getByRole('button', { name: 'Actualizar' }).click();
    expect((await actualizacion).status()).toBe(200);

    let detalle = await request.get(`${API_URL}/costos-envio/${envioAlternativo.id}`, { headers: headers() });
    expect(detalle.status(), await detalle.text()).toBe(200);
    envioAlternativo = await dataOf(detalle);
    expect(envioAlternativo.monto).toBe(90);

    const convertirPredeterminado = await request.put(`${API_URL}/costos-envio/${envioAlternativo.id}`, {
      headers: headers(),
      data: { ...envioAlternativo, esPredeterminado: true }
    });
    expect(convertirPredeterminado.status(), await convertirPredeterminado.text()).toBe(200);

    let listado = await dataOf(await request.get(`${API_URL}/costos-envio`, { headers: headers() }));
    expect(listado.filter((item: any) => item.esPredeterminado)).toHaveLength(1);
    expect(listado.find((item: any) => item.esPredeterminado).id).toBe(envioAlternativo.id);

    const restaurar = await request.put(`${API_URL}/costos-envio/${envioPredeterminado.id}`, {
      headers: headers(),
      data: { ...envioPredeterminado, esPredeterminado: true, activo: true }
    });
    expect(restaurar.status(), await restaurar.text()).toBe(200);

    listado = await dataOf(await request.get(`${API_URL}/costos-envio`, { headers: headers() }));
    expect(listado.filter((item: any) => item.esPredeterminado)).toHaveLength(1);
    expect(listado.find((item: any) => item.esPredeterminado).id).toBe(envioPredeterminado.id);

    const desactivar = await request.patch(`${API_URL}/costos-envio/${envioAlternativo.id}/estado`, {
      headers: headers(),
      data: { activo: false }
    });
    expect(desactivar.status(), await desactivar.text()).toBe(200);
    detalle = await request.get(`${API_URL}/costos-envio/${envioAlternativo.id}`, { headers: headers() });
    expect((await dataOf(detalle)).activo).toBe(false);
  });

  test('factura combinada usa precio de variante exacta: subtotal L. 60.87, ISV L. 9.13, envío L. 80.00, descuento L. 20.00 y total L. 130.00', async ({ request }) => {
    descuentoCodigo = `F7D20${suffix}`;
    const descuento = await request.post(`${API_URL}/descuentos`, {
      headers: headers(),
      data: {
        nombre: `Descuento F7 L. 20 ${suffix}`,
        descripcion: 'Descuento fijo y acotado al producto de validación',
        codigoPromocional: descuentoCodigo,
        tipo: 'MontoFijo',
        valor: 20,
        requiereAprobacion: false,
        acumulable: false,
        prioridad: 1,
        productoIds: [productoSimpleId],
        categoriaIds: [],
        clienteIds: [],
        rolIds: []
      }
    });
    expect(descuento.status(), await descuento.text()).toBe(201);

    const calcular = await request.post(`${API_URL}/ventas/calcular`, {
      headers: headers(),
      data: {
        codigoPromocional: descuentoCodigo,
        costoEnvioId: envioPredeterminado.id,
        envioExonerado: false,
        detalles: [{ productoId: productoSimpleId, productoVarianteId: productoSimpleVarianteTecnicaId, cantidad: 1, precioUnitario: 300 }]
      }
    });
    expect(calcular.status(), await calcular.text()).toBe(200);
    const calculo = await dataOf(calcular);
    expect(calculo.importeBruto).toBe(150);
    expect(calculo.importeProductos).toBe(70);
    expect(calculo.subtotal).toBe(60.87);
    expect(calculo.impuestoIncluido).toBe(9.13);
    expect(calculo.costoEnvio).toBe(80);
    expect(calculo.totalDescuento).toBe(20);
    expect(calculo.total).toBe(130);
    expect(calculo.subtotal + calculo.impuestoIncluido + calculo.impuestoAdicional + calculo.costoEnvio - calculo.totalDescuento).toBe(130);

    const venta = await crearVenta(request,
      [{ productoId: productoSimpleId, productoVarianteId: productoSimpleVarianteTecnicaId, cantidad: 1, precioUnitario: 300 }],
      { codigoPromocional: descuentoCodigo, costoEnvioId: envioPredeterminado.id, envioExonerado: false });
    const confirmada = await confirmarVenta(request, venta.id);
    const factura = await obtenerFactura(request, confirmada.facturaId);
    expect(factura.detalles).toHaveLength(1);
    expect(factura.subtotal).toBe(60.87);
    expect(factura.impuestoIncluido).toBe(9.13);
    expect(factura.costoEnvio).toBe(80);
    expect(factura.descuento).toBe(20);
    expect(factura.total).toBe(130);

    const pagoParcial = await request.post(`${API_URL}/facturas/${factura.id}/pagos`, {
      headers: headers(),
      data: { monto: 100, metodoPago: 'Efectivo', referencia: `PARCIAL-${suffix}` }
    });
    expect(pagoParcial.status(), await pagoParcial.text()).toBe(200);
    const facturaParcial = await dataOf(pagoParcial);
    expect(facturaParcial.totalPagado).toBe(100);
    expect(facturaParcial.saldoPendiente).toBe(30);

    const pagoFinal = await request.post(`${API_URL}/facturas/${factura.id}/pagos`, {
      headers: headers(),
      data: { monto: 30, metodoPago: 'Transferencia', referencia: `TOTAL-${suffix}` }
    });
    expect(pagoFinal.status(), await pagoFinal.text()).toBe(200);
    const facturaPagada = await dataOf(pagoFinal);
    expect(facturaPagada.totalPagado).toBe(130);
    expect(facturaPagada.saldoPendiente).toBe(0);

    const pdf = await request.get(`${API_URL}/facturas/${factura.id}/pdf?formato=A4`, { headers: headers() });
    expect(pdf.status(), await pdf.text()).toBe(200);
    expect(pdf.headers()['content-type']).toContain('application/pdf');
    const bytes = await pdf.body();
    expect(bytes.subarray(0, 4).toString()).toBe('%PDF');
    expect(bytes.length).toBeGreaterThan(5_000);
  });

  test('varios productos conservan un único envío por factura y la anulación revierte existencias', async ({ request }) => {
    const antes = await obtenerProducto(request);
    const stockNegroAntes = variante(antes, varianteNegraId).cantidad;

    const venta = await crearVenta(request, [
      { productoId: productoVariantesId, productoVarianteId: varianteNegraId, cantidad: 1, precioUnitario: 300 },
      { productoId: productoSimpleId, productoVarianteId: productoSimpleVarianteTecnicaId, cantidad: 2, precioUnitario: 150 }
    ], { costoEnvioId: envioPredeterminado.id });

    expect(venta.detalles).toHaveLength(2);
    expect(venta.importeBruto).toBe(600);
    expect(venta.costoEnvio).toBe(80);
    const confirmada = await confirmarVenta(request, venta.id);
    const factura = await obtenerFactura(request, confirmada.facturaId);
    expect(factura.detalles).toHaveLength(2);
    expect(factura.costoEnvio).toBe(80);

    const despues = await obtenerProducto(request);
    expect(variante(despues, varianteNegraId).cantidad).toBe(stockNegroAntes - 1);

    const anular = await request.post(`${API_URL}/ventas/${venta.id}/anular`, {
      headers: headers(),
      data: { motivoAnulacion: 'Validación de reversión integral Fase 7' }
    });
    expect(anular.status(), await anular.text()).toBe(200);

    const restaurado = await obtenerProducto(request);
    expect(variante(restaurado, varianteNegraId).cantidad).toBe(stockNegroAntes);
    const facturaAnulada = await obtenerFactura(request, factura.id);
    expect(facturaAnulada.estado).toBe('Anulada');
  });

  test('venta de dos blancas deja 2/3/3 y total 8, impide sobreventa, compra por color y restituye al anular', async ({ request }) => {
    const venta = await crearVenta(request,
      [{ productoId: productoVariantesId, productoVarianteId: varianteBlancaId, cantidad: 2, precioUnitario: 300 }],
      { costoEnvioId: envioPredeterminado.id });
    await confirmarVenta(request, venta.id);

    let producto = await obtenerProducto(request);
    expect(variante(producto, varianteBlancaId).cantidad).toBe(2);
    expect(variante(producto, varianteNegraId).cantidad).toBe(3);
    expect(variante(producto, varianteRojaId).cantidad).toBe(3);
    expect(producto.cantidad).toBe(8);

    const sobreventa = await crearVenta(request,
      [{ productoId: productoVariantesId, productoVarianteId: varianteBlancaId, cantidad: 3, precioUnitario: 300 }],
      { costoEnvioId: envioPredeterminado.id });
    const confirmarSobreventa = await request.post(`${API_URL}/ventas/${sobreventa.id}/confirmar`, { headers: headers() });
    expect(confirmarSobreventa.status()).toBe(400);
    expect((await confirmarSobreventa.text()).toLowerCase()).toContain('stock insuficiente');

    const compraResponse = await request.post(`${API_URL}/compras`, {
      headers: headers(),
      data: {
        proveedorNombre: `Proveedor F7 ${suffix}`,
        metodoPago: 'Efectivo',
        estadoPago: 'Pendiente',
        descuento: 0,
        impuesto: 0,
        detalles: [{ productoId: productoVariantesId, productoVarianteId: varianteRojaId, cantidad: 2, costoUnitario: 100 }]
      }
    });
    expect(compraResponse.status(), await compraResponse.text()).toBe(201);
    const compra = await dataOf(compraResponse);
    const confirmarCompra = await request.post(`${API_URL}/compras/${compra.id}/confirmar`, { headers: headers() });
    expect(confirmarCompra.status(), await confirmarCompra.text()).toBe(200);

    producto = await obtenerProducto(request);
    expect(variante(producto, varianteBlancaId).cantidad).toBe(2);
    expect(variante(producto, varianteNegraId).cantidad).toBe(3);
    expect(variante(producto, varianteRojaId).cantidad).toBe(5);
    expect(producto.cantidad).toBe(10);

    const anularVenta = await request.post(`${API_URL}/ventas/${venta.id}/anular`, {
      headers: headers(),
      data: { motivoAnulacion: 'Restitución de variante blanca Fase 7' }
    });
    expect(anularVenta.status(), await anularVenta.text()).toBe(200);
    producto = await obtenerProducto(request);
    expect(variante(producto, varianteBlancaId).cantidad).toBe(4);
    expect(variante(producto, varianteNegraId).cantidad).toBe(3);
    expect(variante(producto, varianteRojaId).cantidad).toBe(5);
    expect(producto.cantidad).toBe(12);
  });

  test('exoneración exige motivo, conserva costo cero y queda trazable en auditoría', async ({ request }) => {
    const sinMotivo = await request.post(`${API_URL}/ventas`, {
      headers: headers(),
      data: {
        clienteNombre: `Cliente exoneración ${suffix}`,
        metodoPago: 'Efectivo',
        estadoPago: 'Pendiente',
        envioExonerado: true,
        detalles: [{ productoId: productoSimpleId, productoVarianteId: productoSimpleVarianteTecnicaId, cantidad: 1, precioUnitario: 150 }]
      }
    });
    expect(sinMotivo.status()).toBe(400);
    expect((await sinMotivo.text()).toLowerCase()).toContain('motivo');

    const motivo = `Exoneración autorizada F7 ${suffix}`;
    const venta = await crearVenta(request,
      [{ productoId: productoSimpleId, productoVarianteId: productoSimpleVarianteTecnicaId, cantidad: 1, precioUnitario: 150 }],
      { envioExonerado: true, motivoExoneracionEnvio: motivo });
    expect(venta.envioExonerado).toBe(true);
    expect(venta.costoEnvio).toBe(0);
    expect(venta.motivoExoneracionEnvio).toBe(motivo);
    const confirmada = await confirmarVenta(request, venta.id);
    const factura = await obtenerFactura(request, confirmada.facturaId);
    expect(factura.envioExonerado).toBe(true);
    expect(factura.costoEnvio).toBe(0);
    expect(factura.motivoExoneracionEnvio).toBe(motivo);

    const auditoriaResponse = await request.get(`${API_URL}/auditoria?page=1&pageSize=100&search=${encodeURIComponent(suffix)}`, {
      headers: headers()
    });
    expect(auditoriaResponse.status(), await auditoriaResponse.text()).toBe(200);
    const auditoria = await dataOf(auditoriaResponse);
    expect(auditoria.items.some((item: any) =>
      item.referenciaId === venta.id &&
      (String(item.valoresNuevos ?? '').includes(motivo) || String(item.descripcion ?? '').includes(venta.numeroVenta))
    )).toBe(true);
  });

  test('confirmaciones concurrentes generan números de factura únicos y persistentes', async ({ request }) => {
    const ventas: Record<string, any>[] = [];
    for (let index = 0; index < 4; index += 1) {
      const producto = await crearProductoSimple(request, `Producto concurrencia F7 ${suffix}-${index}`, 2, 120 + index);
      const varianteTecnicaId = await obtenerVarianteTecnicaId(request, Number(producto.id));
      ventas.push(await crearVenta(request,
        [{
          productoId: producto.id,
          productoVarianteId: varianteTecnicaId,
          cantidad: 1,
          precioUnitario: 120 + index
        }],
        { costoEnvioId: envioPredeterminado.id }));
    }

    const respuestas = await Promise.all(ventas.map(venta =>
      request.post(`${API_URL}/ventas/${venta.id}/confirmar`, { headers: headers() })));
    for (const respuesta of respuestas) {
      expect(respuesta.status(), await respuesta.text()).toBe(200);
    }

    const confirmadas = await Promise.all(respuestas.map(dataOf));
    const facturas = await Promise.all(confirmadas.map(item => obtenerFactura(request, item.facturaId)));
    const numeros = facturas.map(item => item.numeroFactura);
    expect(new Set(numeros).size).toBe(numeros.length);
    expect(numeros.every(numero => /^FAC-\d{6,}$/.test(numero))).toBe(true);
  });

  test('carga inválida detecta referencias inexistentes y cantidades negativas, genera reporte y auditoría', async ({ request }) => {
    const csv = [
      'Producto,Marca,Modelo,Color,Talla,SKU,CodigoBarras,Cantidad,UmbralStockBajo,Costo,Precio,Activo',
      `Producto inexistente ${suffix},${nombres.marca},${nombres.modelo},${nombres.blanco},,F7-ERR-${suffix},,2,1,10,20,Si`,
      `${nombres.productoVariantes},${nombres.marca},${nombres.modelo},${nombres.blanco},,F7-NEG-${suffix},,-3,1,10,20,Si`
    ].join('\n');

    const validar = await request.post(`${API_URL}/cargas-masivas/validar`, {
      headers: headers(),
      multipart: {
        tipo: 'VariantesInventario',
        archivo: {
          name: `fase7-errores-${suffix}.csv`,
          mimeType: 'text/csv',
          buffer: Buffer.from(`\uFEFF${csv}`, 'utf8')
        }
      }
    });
    expect(validar.status(), await validar.text()).toBe(200);
    const carga = await dataOf(validar);
    expect(carga.estado).toBe('ConErrores');
    expect(carga.puedeConfirmarse).toBe(false);
    expect(carga.filasConError).toBeGreaterThanOrEqual(2);
    const codigos = carga.errores.map((item: any) => String(item.codigo).toUpperCase());
    expect(codigos).toContain('PRODUCTO_NO_EXISTE');
    expect(codigos).toContain('ENTERO_INVALIDO');

    const reporte = await request.get(`${API_URL}/cargas-masivas/${carga.id}/errores?formato=csv`, {
      headers: headers()
    });
    expect(reporte.status()).toBe(200);
    expect(reporte.headers()['content-type']).toContain('text/csv');
    const reporteTexto = (await reporte.body()).toString('utf8');
    expect(reporteTexto).toContain('PRODUCTO_NO_EXISTE');
    expect(reporteTexto).toContain('ENTERO_INVALIDO');
    expect(reporteTexto).toContain('"-3"');

    const confirmar = await request.post(`${API_URL}/cargas-masivas/${carga.id}/confirmar`, { headers: headers() });
    expect(confirmar.status()).toBe(400);

    const auditoriaResponse = await request.get(`${API_URL}/auditoria?page=1&pageSize=100`, { headers: headers() });
    expect(auditoriaResponse.status(), await auditoriaResponse.text()).toBe(200);
    const auditoria = await dataOf(auditoriaResponse);
    expect(auditoria.items.some((item: any) => item.modulo === 'CargasMasivas')).toBe(true);
  });
});
