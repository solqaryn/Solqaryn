#!/usr/bin/env node
// P4: aceptación HTTP/JWT REAL A/B contra MySQL local descartable.
// No usa Render, Aiven ni credenciales de un entorno persistente.
import assert from 'node:assert/strict';
import { request as nodeHttpRequest } from 'node:http';

const api = process.env.P4_API_URL ?? 'http://127.0.0.1:5005';
if (!/^http:\/\/127\.0\.0\.1:\d+$/.test(api))
  throw new Error('Esta suite sólo permite API local descartable.');
const password = process.env.P4_TEST_PASSWORD;
if (!password) throw new Error('P4_TEST_PASSWORD obligatorio para CI efímero.');
const aId = Number(process.env.P4_COMPANY_A_ID ?? '1');
const bId = Number(process.env.P4_COMPANY_B_ID ?? '2');
assert(aId > 0 && bId > 0 && aId !== bId);
const unwrap = x => x?.data ?? x?.Data ?? x;

// Node fetch/undici sobrescribe el Host explícito con 127.0.0.1. Para
// certificar los bindings públicos A/B enviar el Host exacto con http.request;
// el socket sigue conectado ÚNICAMENTE a la API local efímera.
function localRequestWithHost(method, path, headers, payload) {
  return new Promise((resolve, reject) => {
    const request = nodeHttpRequest(api + path,
      { method, headers, timeout: 15000 }, response => {
        const chunks = [];
        response.on('data', chunk => chunks.push(chunk));
        response.on('error', reject);
        response.on('end', () => {
          const body = Buffer.concat(chunks).toString('utf8');
          let parsed;
          try { parsed = JSON.parse(body); } catch { parsed = body; }
          resolve({ status: response.statusCode, payload: parsed, body });
        });
      });
    request.on('timeout', () => request.destroy(new Error('HTTP local timeout')));
    request.on('error', reject);
    if (payload !== undefined) request.write(JSON.stringify(payload));
    request.end();
  });
}

async function call(method, path, token = '', company = null, payload, host = '') {
  const headers = {};
  if (token) headers.authorization = 'Bearer ' + token;
  if (company !== null) headers['X-Empresa-Id'] = String(company);
  if (host) headers.host = host;
  if (payload !== undefined) headers['content-type'] = 'application/json';
  if (host) return localRequestWithHost(method, path, headers, payload);
  const response = await fetch(api + path, {
    method, headers,
    body: payload === undefined ? undefined : JSON.stringify(payload),
    signal: AbortSignal.timeout(15000)
  });
  const body = await response.text();
  let parsed;
  try { parsed = JSON.parse(body); } catch { parsed = body; }
  return { status: response.status, payload: parsed, body };
}
function expectStatus(response, code, name) {
  assert.equal(response.status, code,
    name + ': HTTP=' + response.status + ', body=' + response.body.slice(0, 500));
}
async function deny(method, path, token, id, name) {
  const r = await call(method, path, token, id);
  assert([401, 403, 404, 503].includes(r.status),
    name + ': acceso indebido HTTP=' + r.status);
}
async function login(username) {
  const r = await call('POST', '/auth/login', '', null,
    { nombreUsuario: username, password });
  expectStatus(r, 200, 'login ' + username);
  const data = unwrap(r.payload);
  const token = data?.token ?? data?.Token;
  assert(typeof token === 'string' && token.length > 30, 'JWT ausente');
  return token;
}
function idOf(response, name) {
  const data = unwrap(response.payload);
  const id = Number(data?.id ?? data?.Id);
  assert(Number.isInteger(id) && id > 0,
    name + ': id ausente en ' + response.body.slice(0, 350));
  return id;
}
function ownNotForeign(response, own, foreign, name) {
  expectStatus(response, 200, name);
  const items = unwrap(response.payload);
  assert(Array.isArray(items), name + ': respuesta no lista');
  assert(items.some(v => Number(v.id ?? v.Id) === own), name + ': registro propio ausente');
  assert(!items.some(v => Number(v.id ?? v.Id) === foreign), name + ': exposición A/B');
}

const jwtA = await login('p4_admin_a');
const jwtB = await login('p4_admin_b');

expectStatus(await call('GET', '/tenant-context/' + aId, jwtA), 200, 'membresía A');
expectStatus(await call('GET', '/tenant-context/' + bId, jwtB), 200, 'membresía B');
await deny('GET', '/tenant-context/' + bId, jwtA, null, 'membresía A→B');
await deny('GET', '/tenant-context/' + aId, jwtB, null, 'membresía B→A');
await deny('GET', '/productos', jwtA, bId, 'spoof cabecera A→B');

const typeBody = { nombre: 'General P4 A/B', colorHex: '#2A4C68',
  activo: true, esPredeterminado: true };
const typeA = await call('POST', '/tipo-clientes', jwtA, aId, typeBody);
const typeB = await call('POST', '/tipo-clientes', jwtB, bId, typeBody);
expectStatus(typeA, 201, 'tipo A');
expectStatus(typeB, 201, 'tipo B');
const typeAId = idOf(typeA, 'tipo A'), typeBId = idOf(typeB, 'tipo B');
ownNotForeign(await call('GET', '/tipo-clientes', jwtA, aId),
  typeAId, typeBId, 'tipos A');
ownNotForeign(await call('GET', '/tipo-clientes', jwtB, bId),
  typeBId, typeAId, 'tipos B');
expectStatus(await call('GET', '/tipo-clientes/' + typeBId, jwtA, aId),
  404, 'tipo ajeno A→B');

const clientA = await call('POST', '/clientes', jwtA, aId,
  { nombre: 'Cliente privado A', tipoClienteId: typeAId });
const clientB = await call('POST', '/clientes', jwtB, bId,
  { nombre: 'Cliente privado B', tipoClienteId: typeBId });
expectStatus(clientA, 201, 'cliente A');
expectStatus(clientB, 201, 'cliente B');
const clientAId = idOf(clientA, 'cliente A'), clientBId = idOf(clientB, 'cliente B');
ownNotForeign(await call('GET', '/clientes', jwtA, aId),
  clientAId, clientBId, 'clientes A');
ownNotForeign(await call('GET', '/clientes', jwtB, bId),
  clientBId, clientAId, 'clientes B');
expectStatus(await call('GET', '/clientes/' + clientBId, jwtA, aId),
  404, 'cliente ajeno A→B');

const doc = 'P4AB-TEST-2026';
const providerA = await call('POST', '/proveedores', jwtA, aId,
  { nombre: 'Proveedor privado A', documento: doc });
const providerB = await call('POST', '/proveedores', jwtB, bId,
  { nombre: 'Proveedor privado B', documento: doc });
expectStatus(providerA, 201, 'proveedor A');
expectStatus(providerB, 201, 'proveedor B');
const providerAId = idOf(providerA, 'proveedor A');
const providerBId = idOf(providerB, 'proveedor B');
ownNotForeign(await call('GET', '/proveedores', jwtA, aId),
  providerAId, providerBId, 'proveedores A');
ownNotForeign(await call('GET', '/proveedores', jwtB, bId),
  providerBId, providerAId, 'proveedores B');
expectStatus(await call('GET', '/proveedores/' + providerBId, jwtA, aId),
  404, 'proveedor ajeno A→B');

for (const path of ['/ventas', '/compras', '/facturas']) {
  expectStatus(await call('GET', path, jwtA, aId), 200, path + ' A');
  expectStatus(await call('GET', path, jwtB, bId), 200, path + ' B');
  await deny('GET', path, jwtA, bId, 'spoof lectura ' + path);
}
for (const [method, path] of [
  ['POST', '/ventas/1/anular'], ['POST', '/compras/1/anular'],
  ['POST', '/facturas/1/pagos'], ['GET', '/facturas/1/historial-envios'],
  ['GET', '/productos/1/imagenes/descargar-todas/otro']
]) {
  expectStatus(await call(method, path, jwtA, aId), 503,
    'cuarentena ' + method + ' ' + path);
}
// El listado de documentos de compra ya está en la whitelist del piloto.
// Un ID inexistente o ajeno debe ser rechazado SIN exigir 503 global.
const missingDocs = await call('GET', '/compras/2147483647/documentos', jwtA, aId);
assert([400, 403, 404, 503].includes(missingDocs.status),
  'Documentos de compra inexistente se expusieron: HTTP=' + missingDocs.status);

expectStatus(await call('GET', '/tienda/categorias', '', null, undefined,
  'store-a.p4.test'), 200, 'tienda Host A');
expectStatus(await call('GET', '/tienda/categorias', '', null, undefined,
  'store-b.p4.test'), 200, 'tienda Host B');
expectStatus(await call('GET', '/tienda/categorias', '', null, undefined,
  'unknown.p4.test'), 503, 'Host público no aprobado');

// Etapa de borradores: permiso Crear real, dos empresas y referencias físicas propias.
// No se confunde con confirmar, afectar stock, generar factura o subir archivos.
async function productDraft(token, company, name) {
  const form = new FormData();
  form.set('Nombre', name);
  form.set('Cantidad', '10');
  form.set('Costo', '10.00');
  form.set('Precio', '25.00');
  form.set('UmbralStockBajo', '2');
  const response = await fetch(api + '/productos', {
    method: 'POST',
    headers: { authorization: 'Bearer ' + token, 'X-Empresa-Id': String(company) },
    body: form,
    signal: AbortSignal.timeout(20000)
  });
  const body = await response.text();
  let parsed;
  try { parsed = JSON.parse(body); } catch { parsed = body; }
  return { status: response.status, payload: parsed, body };
}
const productA = await productDraft(jwtA, aId, 'Producto privado A');
const productB = await productDraft(jwtB, bId, 'Producto privado B');
expectStatus(productA, 201, 'crear producto A');
expectStatus(productB, 201, 'crear producto B');
const productAId = idOf(productA, 'producto A');
const productBId = idOf(productB, 'producto B');
expectStatus(await call('GET', '/productos/' + productAId, jwtA, aId), 200, 'producto A');
expectStatus(await call('GET', '/productos/' + productBId, jwtB, bId), 200, 'producto B');
expectStatus(await call('GET', '/productos/' + productBId, jwtA, aId), 404, 'producto B invisible A');
const variantA = Number(unwrap(productA.payload)?.variantes?.[0]?.id);
const variantB = Number(unwrap(productB.payload)?.variantes?.[0]?.id);
assert(variantA > 0 && variantB > 0, 'Las variantes técnicas deben estar asociadas a sus productos.');

const purchaseA = await call('POST', '/compras', jwtA, aId, {
  proveedorId: providerAId, proveedorNombre: 'Proveedor privado A',
  detalles: [{ productoId: productAId, productoVarianteId: variantA,
    cantidad: 2, costoUnitario: 10.00 }]
});
const purchaseB = await call('POST', '/compras', jwtB, bId, {
  proveedorId: providerBId, proveedorNombre: 'Proveedor privado B',
  detalles: [{ productoId: productBId, productoVarianteId: variantB,
    cantidad: 2, costoUnitario: 10.00 }]
});
expectStatus(purchaseA, 201, 'borrador compra A');
expectStatus(purchaseB, 201, 'borrador compra B');
const purchaseAId = idOf(purchaseA, 'compra A');
const purchaseBId = idOf(purchaseB, 'compra B');
expectStatus(await call('GET', '/compras/' + purchaseAId, jwtA, aId), 200, 'compra A');
expectStatus(await call('GET', '/compras/' + purchaseBId, jwtB, bId), 200, 'compra B');
expectStatus(await call('GET', '/compras/' + purchaseBId, jwtA, aId), 404, 'compra B invisible A');

const saleA = await call('POST', '/ventas', jwtA, aId, {
  clienteId: clientAId, clienteNombre: 'Cliente privado A',
  envioExonerado: true, motivoExoneracionEnvio: 'Retiro directo del cliente A en CI',
  detalles: [{ productoId: productAId, productoVarianteId: variantA,
    cantidad: 1, precioUnitario: 25.00 }]
});
const saleB = await call('POST', '/ventas', jwtB, bId, {
  clienteId: clientBId, clienteNombre: 'Cliente privado B',
  envioExonerado: true, motivoExoneracionEnvio: 'Retiro directo del cliente B en CI',
  detalles: [{ productoId: productBId, productoVarianteId: variantB,
    cantidad: 1, precioUnitario: 25.00 }]
});
expectStatus(saleA, 201, 'borrador venta A');
expectStatus(saleB, 201, 'borrador venta B');
const saleAId = idOf(saleA, 'venta A');
const saleBId = idOf(saleB, 'venta B');
expectStatus(await call('GET', '/ventas/' + saleAId, jwtA, aId), 200, 'venta A');
expectStatus(await call('GET', '/ventas/' + saleBId, jwtB, bId), 200, 'venta B');
expectStatus(await call('GET', '/ventas/' + saleBId, jwtA, aId), 404, 'venta B invisible A');

// Un usuario B no puede apropiarse de la referencia cliente/proveedor/producto A.
for (const [path, payload] of [
  ['/ventas', { clienteId: clientBId, clienteNombre: 'Cliente B',
    envioExonerado: true, motivoExoneracionEnvio: 'Retiro directo del cliente en CI',
    detalles: [{ productoId: productAId, productoVarianteId: variantA,
      cantidad: 1, precioUnitario: 25.00 }] }],
  ['/compras', { proveedorId: providerBId, proveedorNombre: 'Proveedor B',
    detalles: [{ productoId: productAId, productoVarianteId: variantA,
      cantidad: 1, costoUnitario: 10.00 }] }]
]) {
  const denied = await call('POST', path, jwtB, bId, payload);
  assert([400, 403, 404, 409, 503].includes(denied.status),
    path + ': documento B incorporó producto ajeno A, HTTP=' + denied.status);
}

// Confirmaciones físicas con MySQL real, locks y JWT RBAC de las dos empresas.
// Primero ataques cruzados: ningún documento de B cambia desde A.
for (const [path, label] of [
  ['/compras/' + purchaseBId + '/confirmar', 'confirmación compra B desde A'],
  ['/ventas/' + saleBId + '/confirmar', 'confirmación venta B desde A']
]) {
  const denied = await call('POST', path, jwtA, aId);
  assert([400, 403, 404, 409, 503].includes(denied.status),
    label + ' indebidamente permitió acceso, HTTP=' + denied.status);
}
const bBefore = unwrap((await call('GET', '/ventas/' + saleBId, jwtB, bId)).payload);
assert.equal(String(bBefore.estado).toLowerCase(), 'borrador',
  'El intento A sobre la venta B no debe confirmar B.');

const qty = (token, company, productId) =>
  call('GET', '/productos/' + productId, token, company).then(response => {
    expectStatus(response, 200, 'consultar stock propio');
    const amount = Number(unwrap(response.payload)?.cantidad);
    assert(Number.isFinite(amount), 'Producto sin cantidad');
    return amount;
  });
const initialStockA = await qty(jwtA, aId, productAId);
const initialStockB = await qty(jwtB, bId, productBId);

for (const [token, company, purchase, label] of [
  [jwtA, aId, purchaseAId, 'A'], [jwtB, bId, purchaseBId, 'B']
]) {
  expectStatus(await call('POST', '/compras/' + purchase + '/confirmar', token, company),
    200, 'confirmar compra ' + label);
}
assert.equal(await qty(jwtA, aId, productAId), initialStockA + 2,
  'Compra de A debe incrementar sólo inventario A');
assert.equal(await qty(jwtB, bId, productBId), initialStockB + 2,
  'Compra de B debe incrementar sólo inventario B');

for (const [token, company, sale, label] of [
  [jwtA, aId, saleAId, 'A'], [jwtB, bId, saleBId, 'B']
]) {
  expectStatus(await call('POST', '/ventas/' + sale + '/confirmar', token, company),
    200, 'confirmar venta ' + label);
}
assert.equal(await qty(jwtA, aId, productAId), initialStockA + 1,
  'Venta A sólo descuenta inventario propio');
assert.equal(await qty(jwtB, bId, productBId), initialStockB + 1,
  'Venta B sólo descuenta inventario propio');

async function invoiceForSale(saleId, token, company, expectedName) {
  const sale = await call('GET', '/ventas/' + saleId, token, company);
  expectStatus(sale, 200, 'venta confirmada ' + expectedName);
  const value = unwrap(sale.payload);
  assert.equal(String(value.estado).toLowerCase(), 'confirmada');
  const facturaId = Number(value.facturaId);
  assert(facturaId > 0, expectedName + ': venta confirmada sin factura');
  const doc = await call('GET', '/facturas/' + facturaId, token, company);
  expectStatus(doc, 200, 'factura privada ' + expectedName);
  const data = unwrap(doc.payload);
  assert.equal(data.empresaNombre, expectedName,
    'Factura contiene identidad de empresa incorrecta o global.');
  return facturaId;
}
const invoiceAId = await invoiceForSale(saleAId, jwtA, aId, 'Empresa A JWT');
const invoiceBId = await invoiceForSale(saleBId, jwtB, bId, 'Empresa B JWT');
expectStatus(await call('GET', '/facturas/' + invoiceBId, jwtA, aId),
  404, 'Factura B invisible para A');
expectStatus(await call('GET', '/facturas/' + invoiceAId, jwtB, bId),
  404, 'Factura A invisible para B');

for (const [token, company, sale] of [
  [jwtA, aId, saleAId], [jwtB, bId, saleBId]
]) {
  const duplicate = await call('POST', '/ventas/' + sale + '/confirmar', token, company);
  assert([400, 409].includes(duplicate.status),
    'No debe permitir doble confirmación de la misma venta, HTTP=' + duplicate.status);
}

// Descarga de imágenes y ZIP: sin credenciales de Cloudinary en MySQL
// efímero, el controlador debe NO exponer producto de la empresa ajena y
// devolver 404 por galería vacía del dueño. Los bytes reales se probarán
// independientemente en DEV con assets controlados, nunca contra PROD.
for (const [token, empresa, propio, ajeno] of [
  [jwtA, aId, productAId, productBId],
  [jwtB, bId, productBId, productAId]
]) {
  expectStatus(await call('GET', '/productos/' + propio +
    '/imagenes/descargar-todas', token, empresa),
    404, 'ZIP propio sin imágenes registradas');
  expectStatus(await call('GET', '/productos/' + propio +
    '/imagenes/1/descargar', token, empresa),
    404, 'imagen propia no registrada');
  expectStatus(await call('GET', '/productos/' + ajeno +
    '/imagenes/descargar-todas', token, empresa),
    404, 'ZIP producto ajeno invisible');
  expectStatus(await call('GET', '/productos/' + ajeno +
    '/imagenes/1/descargar', token, empresa),
    404, 'imagen producto ajeno invisible');
}
await deny('GET', '/productos/' + productBId +
  '/imagenes/descargar-todas', jwtA, bId, 'spoof ZIP A→B');
console.log('P4 ZIP/media gate A/B PASS: JWT, owner y ausencia de imágenes no cruzan empresa.');

// Integraciones privadas GET (sólo CI): PDF real, reportes, Kardex y existencias.
async function getPdf(token, empresaId, invoiceId) {
  const r = await fetch(api + '/facturas/' + invoiceId + '/pdf?formato=a4', {
    headers: {
      authorization: 'Bearer ' + token,
      'X-Empresa-Id': String(empresaId)
    },
    signal: AbortSignal.timeout(30000)
  });
  if (r.status !== 200)
    throw new Error('PDF propio HTTP=' + r.status + ' body=' + (await r.text()).slice(0,450));
  assert((r.headers.get('content-type') ?? '').includes('application/pdf'),
    'PDF no declara MIME application/pdf');
  const bytes = Buffer.from(await r.arrayBuffer());
  assert(bytes.length > 900 && bytes.subarray(0, 5).toString('ascii') === '%PDF-',
    'QuestPDF no devolvió bytes PDF íntegros');
}
await getPdf(jwtA, aId, invoiceAId);
await getPdf(jwtB, bId, invoiceBId);
expectStatus(await call('GET', '/facturas/' + invoiceBId + '/pdf?formato=a4', jwtA, aId),
  404, 'PDF de B invisible para A');
expectStatus(await call('GET', '/facturas/' + invoiceAId + '/pdf?formato=a4', jwtB, bId),
  404, 'PDF de A invisible para B');

for (const [jwt, company, ownSale, foreignSale, product] of [
  [jwtA, aId, saleAId, saleBId, productAId],
  [jwtB, bId, saleBId, saleAId, productBId]
]) {
  const summary = await call('GET', '/ventas/reportes/resumen', jwt, company);
  expectStatus(summary, 200, 'resumen ventas');
  assert(Number(unwrap(summary.payload)?.total) > 0,
    'El resumen de la empresa debe contener su venta confirmada');
  const report = await call('GET', '/ventas/reportes/detalle?page=1&pageSize=100', jwt, company);
  expectStatus(report, 200, 'detalle ventas');
  const rows = unwrap(report.payload)?.items;
  assert(Array.isArray(rows), 'Reporte ventas no contiene items');
  assert(rows.some(x => Number(x.ventaId) === ownSale), 'Venta propia ausente del reporte');
  assert(!rows.some(x => Number(x.ventaId) === foreignSale), 'Reporte mostró venta de otra empresa');

  // La fuente de detalle de compras es OrdenCompra (no Compra). La consulta
  // debe ser autorizada y vacía para este fixture sin órdenes, sin inventar una.
  const supplierReport = await call('GET', '/compras/reportes/detalle?page=1&pageSize=50',
    jwt, company);
  expectStatus(supplierReport, 200, 'reporte compras');

  const moves = await call('GET', '/inventario/movimientos?productoId=' + product, jwt, company);
  expectStatus(moves, 200, 'Kardex de producto propio');
  const values = unwrap(moves.payload);
  assert(Array.isArray(values) && values.length >= 2, 'Kardex debe tener entrada y salida propias');
  assert(values.every(x => Number(x.productoId) === product),
    'Kardex cruzó producto de otra empresa');
  const stocks = await call('GET', '/existencias-variante', jwt, company);
  expectStatus(stocks, 200, 'inventario de variantes propio');
}
for (const path of [
  '/ventas/reportes/detalle?page=1&pageSize=30',
  '/compras/reportes/detalle?page=1&pageSize=30',
  '/inventario/movimientos',
  '/existencias-variante'
]) {
  await deny('GET', path, jwtA, bId, 'cabecera falsa en integración ' + path);
}
console.log('P4 READ CI real PASS: QuestPDF A/B, reporte ventas por ID, compras tenant, Kardex y existencias.');

// Cierre de exportaciones administrativas existentes, sin un motor Excel nuevo.
// El servicio usa EmpresaId RBAC para toda fila antes de serializar CSV/XLSX/PDF.
for (const [jwt, ownId, ownUser, forbiddenUser] of [
  [jwtA, aId, 'p4_admin_a', 'p4_admin_b'],
  [jwtB, bId, 'p4_admin_b', 'p4_admin_a']
]) {
  const members = await call('GET',
    '/reportes-administrativos/usuarios-accesos', jwt, ownId);
  expectStatus(members, 200, 'usuarios y accesos administrativos por empresa');
  const ownRows = unwrap(members.payload);
  assert(Array.isArray(ownRows), 'La lista administrativa no tiene formato esperado');
  assert(ownRows.some(x => x.nombreUsuario === ownUser),
    'El usuario propio no aparece en su empresa');
  assert(!ownRows.some(x => x.nombreUsuario === forbiddenUser),
    'Reporte administrativo expuso la membresía de otra empresa');

  const roles = await call('GET', '/reportes-administrativos/roles-permisos', jwt, ownId);
  expectStatus(roles, 200, 'roles y permisos por empresa');
  const audit = await call('GET', '/reportes-administrativos/auditoria-resumen', jwt, ownId);
  expectStatus(audit, 200, 'auditoría por empresa');

  for (const [report, format, expectedMime, magic] of [
    ['usuarios-accesos', 'xlsx',
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'PK'],
    ['roles-permisos', 'csv', 'text/csv', ''],
    ['auditoria', 'pdf', 'application/pdf', '%PDF-']
  ]) {
    const response = await fetch(api +
        '/reportes-administrativos/exportar/' + report + '?formato=' + format, {
      headers: {
        authorization: 'Bearer ' + jwt,
        'X-Empresa-Id': String(ownId)
      },
      signal: AbortSignal.timeout(30000)
    });
    assert.equal(response.status, 200,
      'Exportación propia ' + report + '/' + format + ': HTTP=' + response.status);
    assert((response.headers.get('content-type') ?? '').includes(expectedMime),
      'MIME inválido en ' + report + '/' + format);
    const bytes = Buffer.from(await response.arrayBuffer());
    assert(bytes.length > 40 && (magic === '' ||
      bytes.subarray(0, magic.length).toString('ascii') === magic),
      'Firma inválida en exportación ' + report + '/' + format);
  }
}
await deny('GET', '/reportes-administrativos/usuarios-accesos',
  jwtA, bId, 'reporte administrativo A→B');
await deny('GET', '/reportes-administrativos/exportar/usuarios-accesos?formato=xlsx',
  jwtB, aId, 'XLSX administrativo B→A');
await deny('GET', '/reportes-administrativos/exportar/usuarios-accesos?formato=xlsx',
  '', aId, 'exportación administrativa sin JWT');
expectStatus(await call('POST',
  '/reportes-administrativos/exportar/usuarios-accesos?formato=xlsx', jwtA, aId),
  503, 'POST de exportación no autorizada');
console.log('P4 CI JWT/MySQL A/B: reporte administrativo propio, roles y auditoría, XLSX/CSV/PDF válidos y accesos cruzados rechazados.');

console.log('P4 HTTP/MySQL real A/B PASS: clientes, productos, compras confirmadas, inventario, ventas confirmadas y facturas con identidad empresarial correcta.');
// Finanzas A/B: dos pagos en paralelo, anulación de pago y reversión de
// venta/compra sobre MySQL real efímero, nunca sobre datos de DEV persistente.
const invoicePairs = [
  [jwtA, aId, invoiceAId, invoiceBId, saleAId, 'A', productAId, initialStockA],
  [jwtB, bId, invoiceBId, invoiceAId, saleBId, 'B', productBId, initialStockB]
];
const paid = await Promise.all(invoicePairs.map(async ([jwt, company, invoice, , , label]) => {
  const result = await call('POST', '/facturas/' + invoice + '/pagos',
    jwt, company, {
      monto: 5.00,
      metodoPago: 'Efectivo',
      observaciones: 'Pago efímero tenant ' + label
    });
  expectStatus(result, 200, 'aplicar pago factura propia ' + label);
  const dto = unwrap(result.payload);
  assert.equal(Number(dto.totalPagado), 5.00, 'Importe pagado del tenant ' + label);
  const pagoId = Number(dto.pagos?.find(x => !x.anulado)?.id);
  assert(pagoId > 0, 'Pago confirmado no devolvió ID ' + label);
  return pagoId;
}));
await Promise.all(invoicePairs.map(async ([jwt, company, invoice, foreign, , label], i) => {
  const cross = await call('POST', '/facturas/' + foreign + '/pagos', jwt, company,
    { monto: 2.00, metodoPago: 'Efectivo' });
  assert([400, 403, 404, 503].includes(cross.status),
    'Usuario ' + label + ' pagó la factura de otra empresa, HTTP=' + cross.status);

  const crossCancel = await call('POST',
    '/facturas/' + foreign + '/pagos/' + paid[1 - i] + '/anular',
    jwt, company, { motivo: 'Prueba de rechazo cruzado' });
  assert([400, 403, 404, 503].includes(crossCancel.status),
    'Usuario ' + label + ' anuló el pago de la otra empresa, HTTP=' + crossCancel.status);
}));
await Promise.all(invoicePairs.map(async ([jwt, company, invoice, , , label], i) => {
  const undone = await call('POST',
    '/facturas/' + invoice + '/pagos/' + paid[i] + '/anular',
    jwt, company, { motivo: 'Reversión de pago efímera ' + label });
  expectStatus(undone, 200, 'anular pago propio ' + label);
  assert.equal(Number(unwrap(undone.payload)?.totalPagado), 0,
    'Anulación de pago no reversó saldo ' + label);
}));
await Promise.all(invoicePairs.map(async ([jwt, company, , foreign, saleId, label, product, stock]) => {
  const wrong = await call('POST',
    '/ventas/' + (label === 'A' ? saleBId : saleAId) + '/anular',
    jwt, company, { motivoAnulacion: 'Rechazo cruzado' });
  assert([400, 403, 404, 503].includes(wrong.status),
    'Anulación cruzada de venta no fue bloqueada: HTTP=' + wrong.status);
  const result = await call('POST', '/ventas/' + saleId + '/anular', jwt, company,
    { motivoAnulacion: 'Anulación venta de aceptación ' + label });
  expectStatus(result, 200, 'anular venta ' + label);
  const restored = await qty(jwt, company, product);
  assert.equal(restored, stock + 2, 'Anulación venta debe restaurar stock ' + label);
}));

// Compra anulable independiente para no violar el bloqueo de movimientos de
// stock posteriores que protege la compra usada para la venta anterior.
for (const [jwt, company, label, supplier] of [
  [jwtA, aId, 'A', providerAId],
  [jwtB, bId, 'B', providerBId]
]) {
  const product = await productDraft(jwt, company, 'Producto anulación compra ' + label);
  expectStatus(product, 201, 'producto de compra anulable ' + label);
  const productId = idOf(product, 'producto anulación ' + label);
  const variantId = Number(unwrap(product.payload)?.variantes?.[0]?.id);
  assert(variantId > 0, 'Falta variante técnica para anulaciones');
  const stockBefore = await qty(jwt, company, productId);
  const draft = await call('POST', '/compras', jwt, company, {
    proveedorId: supplier, proveedorNombre: 'Proveedor privado ' + label,
    detalles: [{ productoId: productId, productoVarianteId: variantId,
      cantidad: 1, costoUnitario: 10.00 }]
  });
  expectStatus(draft, 201, 'compra anulable ' + label);
  const purchaseId = idOf(draft, 'compra anulable ' + label);
  expectStatus(await call('POST', '/compras/' + purchaseId + '/confirmar', jwt, company),
    200, 'confirmar compra de prueba para anulación ' + label);
  assert.equal(await qty(jwt, company, productId), stockBefore + 1,
    'Confirmar compra no agregó inventario ' + label);
  expectStatus(await call('POST', '/compras/' + purchaseId + '/anular', jwt, company,
      { motivoAnulacion: 'Reversión compra efímera ' + label }),
    200, 'anular compra ' + label);
  assert.equal(await qty(jwt, company, productId), stockBefore,
    'Anular compra no revirtió su inventario ' + label);
}

console.log('P4 FINANZAS A/B PASS: pagos en paralelo, reversión contable, anulación ventas/compras y stock propios; rechazos cruzados.');
// Rutas de archivos de compras A/B y modificación de producto bajo JWT real.
// Se evita probar el proveedor Cloudinary con credenciales CI ficticias:
// no se presenta como certificación externa.
for (const [jwt, company, purchase, foreignPurchase, ownProduct, foreignProduct, label] of [
  [jwtA, aId, purchaseAId, purchaseBId, productAId, productBId, 'A'],
  [jwtB, bId, purchaseBId, purchaseAId, productBId, productAId, 'B']
]) {
  const own = await call('GET', '/compras/' + purchase + '/documentos', jwt, company);
  expectStatus(own, 200, 'documentos compra propia ' + label);
  assert(Array.isArray(unwrap(own.payload)), 'Listado adjuntos no es lista ' + label);
  const foreign = await call('GET', '/compras/' + foreignPurchase + '/documentos',
    jwt, company);
  assert([400, 403, 404, 503].includes(foreign.status),
    'Documentos compra ajena HTTP=' + foreign.status);
  expectStatus(await call('GET',
      '/compras/' + purchase + '/documentos/999999/descargar', jwt, company),
    404, 'documento inexistente ' + label);

  const original = await call('GET', '/productos/' + ownProduct, jwt, company);
  expectStatus(original, 200, 'leer producto antes de editar ' + label);
  const previous = unwrap(original.payload);
  // El endpoint original sincroniza además la variante técnica desde el
  // formulario: rellenar cantidad/costo/precio actuales evita cambios de
  // stock inadvertidos durante una prueba de edición de nombre.
  const form = new FormData();
  form.set('Nombre', 'Producto editado ' + label);
  for (const [key, value] of [
    ['Cantidad', previous.cantidad],
    ['Costo', previous.costo],
    ['Precio', previous.precio],
    ['UmbralStockBajo', previous.umbralStockBajo]
  ]) {
    assert(Number.isFinite(Number(value)), 'Campo requerido ausente: ' + key);
    form.set(key, String(value));
  }
  if (previous.descripcion) form.set('Descripcion', previous.descripcion);
  if (previous.categoriaId) form.set('CategoriaId', String(previous.categoriaId));
  if (previous.esDestacado !== undefined)
    form.set('EsDestacado', String(previous.esDestacado));
  const changed = await fetch(api + '/productos/' + ownProduct, {
    method: 'PUT',
    headers: {
      authorization: 'Bearer ' + jwt,
      'X-Empresa-Id': String(company)
    },
    body: form,
    signal: AbortSignal.timeout(20000)
  });
  assert.equal(changed.status, 200,
    'Editar producto propio ' + label + ': HTTP=' + changed.status +
    ' body=' + (await changed.text()).slice(0, 350));
  const read = await call('GET', '/productos/' + ownProduct, jwt, company);
  expectStatus(read, 200, 'releer producto editado ' + label);
  assert.equal(unwrap(read.payload)?.nombre, 'Producto editado ' + label);
  assert.equal(Number(unwrap(read.payload)?.cantidad), Number(previous.cantidad),
    'Editar nombre no puede cambiar stock comercial ' + label);

  const crossForm = new FormData();
  crossForm.set('Nombre', 'Intención de invasión empresarial');
  const cross = await fetch(api + '/productos/' + foreignProduct, {
    method: 'PUT',
    headers: {
      authorization: 'Bearer ' + jwt,
      'X-Empresa-Id': String(company)
    },
    body: crossForm,
    signal: AbortSignal.timeout(20000)
  });
  assert([400, 403, 404, 503].includes(cross.status),
    'Edición de producto ajeno ' + label + ' fue aceptada: HTTP=' + cross.status);
}
console.log('P4 FILES CI PASS: JWT A/B archivos de compras, lectura ajena denegada y actualización segura de producto propio.');
console.log('No certifica todavía alta/descarga física de contenido Cloudinary con dos empresas ni jobs.');

for (const [jwt, company, client, supplier, typeId, label] of [
  [jwtA, aId, clientAId, providerAId, typeAId, 'A'],
  [jwtB, bId, clientBId, providerBId, typeBId, 'B']
]) {
  const clientName = 'Cliente editado ' + label;
  const supplierName = 'Proveedor editado ' + label;
  expectStatus(await call('PUT', '/clientes/' + client, jwt, company,
    { nombre: clientName, tipoClienteId: typeId }), 200, 'editar cliente ' + label);
  expectStatus(await call('PUT', '/proveedores/' + supplier, jwt, company,
    { nombre: supplierName, documento: doc }), 200, 'editar proveedor ' + label);
  const ownClient = await call('GET', '/clientes/' + client, jwt, company);
  const ownSupplier = await call('GET', '/proveedores/' + supplier, jwt, company);
  expectStatus(ownClient, 200, 'leer cliente editado ' + label);
  expectStatus(ownSupplier, 200, 'leer proveedor editado ' + label);
  assert.equal(unwrap(ownClient.payload).nombre, clientName);
  assert.equal(unwrap(ownSupplier.payload).nombre, supplierName);
}
for (const [jwt, company, otherClient, otherSupplier, label] of [
  [jwtA, aId, clientBId, providerBId, 'A'],
  [jwtB, bId, clientAId, providerAId, 'B']
]) {
  for (const [path, payload] of [
    ['/clientes/' + otherClient, { nombre: 'Intento rechazado' }],
    ['/proveedores/' + otherSupplier, { nombre: 'Intento rechazado' }]
  ]) {
    const rejected = await call('PUT', path, jwt, company, payload);
    assert([400, 403, 404, 503].includes(rejected.status),
      'Edición fuera de empresa aceptada ' + label + ' ' + path);
  }
}
for (const [jwt, company, client, supplier, label] of [
  [jwtA, aId, clientAId, providerAId, 'A'],
  [jwtB, bId, clientBId, providerBId, 'B']
]) {
  const savedClient = await call('GET', '/clientes/' + client, jwt, company);
  const savedSupplier = await call('GET', '/proveedores/' + supplier, jwt, company);
  expectStatus(savedClient, 200, 'cliente post acceso cruzado ' + label);
  expectStatus(savedSupplier, 200, 'proveedor post acceso cruzado ' + label);
  assert.equal(unwrap(savedClient.payload).nombre, 'Cliente editado ' + label,
    'Edición ajena modificó cliente ' + label);
  assert.equal(unwrap(savedSupplier.payload).nombre, 'Proveedor editado ' + label,
    'Edición ajena modificó proveedor ' + label);
}
console.log('P4 MASTER EDIT PASS: clientes y proveedores A/B con PUT, MySQL/JWT y rechazo cruzado.');


