import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'];
const EMPRESA_ID = process.env['E2E_TENANT_ID'] ?? process.env['PHASE7_EMPRESA_ID'];
const suffix = `${Date.now()}`;

let token = '';
let productoId = 0;

const nombres = {
  marca: `Marca Carga ${suffix}`,
  modelo: `Modelo Carga ${suffix}`,
  categoria: `Categoría Carga ${suffix}`,
  color: `Turquesa Carga ${suffix}`,
  producto: `Producto Carga ${suffix}`,
  cliente: `Cliente Carga ${suffix}`,
  proveedor: `Proveedor Carga ${suffix}`,
  sku: `CM-${suffix}`
};

function requireAdminPassword(): string {
  if (!ADMIN_PASSWORD) throw new Error('PHASE7_ADMIN_PASSWORD es obligatorio para la aceptación de cargas masivas.');
  return ADMIN_PASSWORD;
}

function headers(): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

async function dataOf(response: APIResponse): Promise<any> {
  const payload = await response.json();
  return payload.data ?? payload.Data;
}

async function loginApi(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, {
    data: { nombreUsuario: ADMIN_USERNAME, password: requireAdminPassword() }
  });
  expect(response.status(), await response.text()).toBe(200);
  return (await dataOf(response)).token;
}

async function completarSeleccionTenantSiAplica(page: Page): Promise<void> {
  const selector = page.getByLabel('ID de empresa');
  try {
    await selector.waitFor({ state: 'visible', timeout: 5_000 });
  } catch {
    return;
  }

  if (!EMPRESA_ID) throw new Error('E2E_TENANT_ID o PHASE7_EMPRESA_ID es obligatorio cuando el login requiere seleccionar empresa.');

  try {
    await selector.fill(EMPRESA_ID, { timeout: 5_000 });
  } catch (error) {
    try {
      await page.waitForURL((url) => url.pathname !== '/login', { timeout: 5_000 });
      return;
    } catch {
      throw error;
    }
  }

  try {
    await page.getByRole('button', { name: 'Entrar a la empresa', exact: true }).click({ timeout: 5_000 });
  } catch (error) {
    try {
      await page.waitForURL((url) => url.pathname !== '/login', { timeout: 5_000 });
      return;
    } catch {
      throw error;
    }
  }
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(requireAdminPassword());
  await page.locator('button[type="submit"]').click();
  await completarSeleccionTenantSiAplica(page);
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
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

async function validarCsv(
  request: APIRequestContext,
  tipo: string,
  nombre: string,
  contenido: string
): Promise<{ response: APIResponse; data?: any }> {
  const response = await request.post(`${API_URL}/cargas-masivas/validar`, {
    headers: headers(),
    multipart: {
      tipo,
      archivo: {
        name: nombre,
        mimeType: 'text/csv',
        buffer: Buffer.from(`\uFEFF${contenido}`, 'utf8')
      }
    }
  });
  return { response, data: response.ok() ? await dataOf(response) : undefined };
}

async function confirmar(request: APIRequestContext, id: number): Promise<any> {
  const response = await request.post(`${API_URL}/cargas-masivas/${id}/confirmar`, {
    headers: headers()
  });
  expect(response.status(), await response.text()).toBe(200);
  const data = await dataOf(response);
  expect(data.estado).toBe('Confirmada');
  return data;
}

test.describe('Fase 5 — cargas masivas controladas', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    token = await loginApi(request);
  });

  test('plantillas, XLSX, validación de fórmulas e informe de errores son seguros', async ({ request }) => {
    const plantilla = await request.get(`${API_URL}/cargas-masivas/plantillas/Clientes?formato=xlsx`, {
      headers: headers()
    });
    expect(plantilla.status()).toBe(200);
    expect(plantilla.headers()['content-type']).toContain('spreadsheetml');
    const plantillaBytes = await plantilla.body();
    expect(plantillaBytes.length).toBeGreaterThan(1000);

    const validacionXlsx = await request.post(`${API_URL}/cargas-masivas/validar`, {
      headers: headers(),
      multipart: {
        tipo: 'Clientes',
        archivo: {
          name: `clientes-plantilla-${suffix}.xlsx`,
          mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
          buffer: plantillaBytes
        }
      }
    });
    expect(validacionXlsx.status(), await validacionXlsx.text()).toBe(200);
    const xlsx = await dataOf(validacionXlsx);
    expect(xlsx.estado).toBe('Validada');
    expect(xlsx.filasValidas).toBe(1);

    const falsoXlsx = await request.post(`${API_URL}/cargas-masivas/validar`, {
      headers: headers(),
      multipart: {
        tipo: 'Clientes',
        archivo: {
          name: `archivo-falso-${suffix}.xlsx`,
          mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
          buffer: Buffer.from('contenido que no es un contenedor xlsx', 'utf8')
        }
      }
    });
    expect(falsoXlsx.status()).toBe(400);
    expect(await falsoXlsx.text()).toContain('XLSX');

    const contenido = [
      'Nombre,Telefono,IdentidadORTN,Correo,Direccion,Activo',
      '=HYPERLINK("https://example.invalid"),9999-9999,0801199000099,peligro@ejemplo.test,Tegucigalpa,Si'
    ].join('\n');
    const validacion = await validarCsv(request, 'Clientes', `formula-${suffix}.csv`, contenido);
    expect(validacion.response.status(), await validacion.response.text()).toBe(200);
    expect(validacion.data.estado).toBe('ConErrores');
    expect(validacion.data.puedeConfirmarse).toBe(false);
    expect(validacion.data.errores.some((error: any) => error.codigo === 'FORMULA_NO_PERMITIDA')).toBe(true);

    const reporte = await request.get(`${API_URL}/cargas-masivas/${validacion.data.id}/errores?formato=csv`, {
      headers: headers()
    });
    expect(reporte.status(), await reporte.text()).toBe(200);
    const texto = (await reporte.body()).toString('utf8');
    expect(texto).toContain('FORMULA_NO_PERMITIDA');
  });

  test('importa clientes y proveedores en una transacción e impide duplicar el archivo confirmado', async ({ request }) => {
    const clientesCsv = [
      'Nombre,Telefono,IdentidadORTN,Correo,Direccion,Activo',
      `${nombres.cliente},9999-1001,080119900${suffix.slice(-4)},cliente-${suffix}@ejemplo.test,Tegucigalpa,Si`,
      `${nombres.cliente} 2,9999-1002,080119901${suffix.slice(-4)},cliente2-${suffix}@ejemplo.test,Comayagüela,Si`
    ].join('\n');
    const clientes = await validarCsv(request, 'Clientes', `clientes-${suffix}.csv`, clientesCsv);
    expect(clientes.response.status(), await clientes.response.text()).toBe(200);
    expect(clientes.data.estado).toBe('Validada');
    expect(clientes.data.filasValidas).toBe(2);
    expect(clientes.data.filasConError).toBe(0);
    const confirmada = await confirmar(request, clientes.data.id);
    expect(confirmada.registrosCreados).toBe(2);

    const listadoClientes = await request.get(`${API_URL}/clientes`, { headers: headers() });
    expect(listadoClientes.status(), await listadoClientes.text()).toBe(200);
    expect((await dataOf(listadoClientes)).some((item: any) => item.nombre === nombres.cliente)).toBe(true);

    const repetida = await validarCsv(request, 'Clientes', `clientes-copia-${suffix}.csv`, clientesCsv);
    expect(repetida.response.status()).toBe(400);
    expect(await repetida.response.text()).toContain('ya fue confirmado');

    const proveedoresCsv = [
      'Nombre,Telefono,Documento,Correo,Direccion,Activo',
      `${nombres.proveedor},2222-1001,RTN-${suffix},proveedor-${suffix}@ejemplo.test,Tegucigalpa,Si`
    ].join('\n');
    const proveedores = await validarCsv(request, 'Proveedores', `proveedores-${suffix}.csv`, proveedoresCsv);
    expect(proveedores.response.status(), await proveedores.response.text()).toBe(200);
    expect(proveedores.data.puedeConfirmarse).toBe(true);
    const proveedoresConfirmados = await confirmar(request, proveedores.data.id);
    expect(proveedoresConfirmados.registrosCreados).toBe(1);
  });

  test('importa color, producto y variante con inventario consolidado', async ({ request }) => {
    const marca = await crearCatalogo(request, 'marcas', {
      nombre: nombres.marca,
      descripcion: 'Marca para carga masiva',
      orden: 30
    });
    await crearCatalogo(request, 'modelos', {
      nombre: nombres.modelo,
      descripcion: 'Modelo para carga masiva',
      catalogoPadreId: marca.id,
      orden: 30
    });
    await crearCatalogo(request, 'categorias', {
      nombre: nombres.categoria,
      descripcion: 'Categoría para carga masiva'
    });

    const coloresCsv = [
      'Nombre,CodigoVisual,Descripcion,Orden,Activo',
      `${nombres.color},#14B8A6,Color importado,30,Si`
    ].join('\n');
    const color = await validarCsv(request, 'Colores', `colores-${suffix}.csv`, coloresCsv);
    expect(color.response.status(), await color.response.text()).toBe(200);
    expect(color.data.puedeConfirmarse).toBe(true);
    await confirmar(request, color.data.id);

    const productosCsv = [
      'Nombre,Marca,Modelo,Categoria,Talla,Descripcion,Costo,Precio,UmbralStockBajo,Activo',
      `${nombres.producto},${nombres.marca},${nombres.modelo},${nombres.categoria},,Producto importado,125.50,299.00,4,Si`
    ].join('\n');
    const producto = await validarCsv(request, 'Productos', `productos-${suffix}.csv`, productosCsv);
    expect(producto.response.status(), await producto.response.text()).toBe(200);
    expect(producto.data.filas[0].accion).toBe('Crear');
    await confirmar(request, producto.data.id);

    const busqueda = await request.get(`${API_URL}/productos?page=1&pageSize=100&search=${encodeURIComponent(nombres.producto)}`, {
      headers: headers()
    });
    expect(busqueda.status(), await busqueda.text()).toBe(200);
    const productos = (await dataOf(busqueda)).items;
    const productoCreado = productos.find((item: any) => item.nombre === nombres.producto);
    expect(productoCreado).toBeTruthy();
    productoId = productoCreado.id;
    expect(productoCreado.cantidad).toBe(0);

    const variantesCsv = [
      'Producto,Marca,Modelo,Color,Talla,SKU,CodigoBarras,Cantidad,UmbralStockBajo,Costo,Precio,Activo',
      `${nombres.producto},${nombres.marca},${nombres.modelo},${nombres.color},,${nombres.sku},750${suffix.slice(-9)},7,2,125.50,299.00,Si`
    ].join('\n');
    const variante = await validarCsv(request, 'VariantesInventario', `variantes-${suffix}.csv`, variantesCsv);
    expect(variante.response.status(), await variante.response.text()).toBe(200);
    expect(variante.data.puedeConfirmarse).toBe(true);
    expect(variante.data.filas[0].datos.SKU).toBe(nombres.sku.toUpperCase());
    await confirmar(request, variante.data.id);

    const detalleProducto = await request.get(`${API_URL}/productos/${productoId}`, { headers: headers() });
    expect(detalleProducto.status(), await detalleProducto.text()).toBe(200);
    const productoFinal = await dataOf(detalleProducto);
    expect(productoFinal.cantidad).toBe(7);
    expect(productoFinal.totalVariantes).toBe(1);
    expect(productoFinal.variantes[0].cantidad).toBe(7);
    expect(productoFinal.variantes[0].sku).toBe(nombres.sku.toUpperCase());
    expect(productoFinal.variantes[0].colorNombre).toBe(nombres.color);
  });

  test('la interfaz muestra flujo, plantillas, vista previa e historial', async ({ page }) => {
    await loginUi(page);
    await page.goto('/cargas-masivas');
    await expect(page.getByRole('heading', { name: 'Cargas masivas' })).toBeVisible();
    await expect(page.getByText('Sin cambios parciales')).toBeVisible();
    await expect(page.getByRole('button', { name: /Plantilla Excel/ })).toBeVisible();
    await expect(page.getByRole('button', { name: /Validar y generar vista previa/ })).toBeDisabled();
    await expect(page.getByRole('heading', { name: 'Historial de cargas' })).toBeVisible();
    await expect(page.getByText(`productos-${suffix}.csv`)).toBeVisible();

    const csv = [
      'Nombre,CodigoVisual,Descripcion,Orden,Activo',
      `Color UI ${suffix},#0EA5E9,Color desde interfaz,50,Si`
    ].join('\n');
    await page.locator('mat-select').first().click();
    await page.getByRole('option', { name: 'Colores' }).click();
    await page.locator('input[type="file"]').setInputFiles({
      name: `color-ui-${suffix}.csv`,
      mimeType: 'text/csv',
      buffer: Buffer.from(csv, 'utf8')
    });
    await page.getByRole('button', { name: /Validar y generar vista previa/ }).click();
    await expect(page.getByRole('heading', { name: /Vista previa de la carga/ })).toBeVisible();
    await expect(page.getByText('La carga está lista para confirmarse.')).toBeVisible();
  });
});
