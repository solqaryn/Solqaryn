import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

let adminToken = '';
const suffix = `${Date.now()}`;
const nombres = {
  marca: `Marca E2E ${suffix}`,
  modelo: `Modelo E2E ${suffix}`,
  color: `Azul E2E ${suffix}`,
  talla: `Talla E2E ${suffix}`,
  producto: `Producto variante E2E ${suffix}`,
  categoria: `Categoría eliminable E2E ${suffix}`
};

function authHeaders(token: string): Record<string, string> {
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
  const data = await dataOf(response);
  expect(data?.token).toBeTruthy();
  return data.token;
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function crearCatalogo(
  request: APIRequestContext,
  ruta: string,
  data: Record<string, unknown>
): Promise<Record<string, any>> {
  const response = await request.post(`${API_URL}/${ruta}`, {
    headers: authHeaders(adminToken),
    data
  });
  expect(response.status(), `${ruta}: ${await response.text()}`).toBe(201);
  return await dataOf(response);
}

test.describe('Mantenimientos reutilizables y correcciones críticas', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    adminToken = await loginApi(request);
  });

  test('CRUD, búsqueda, estados y relación normalizada Marca–Modelo funcionan por API', async ({ request }) => {
    const marca = await crearCatalogo(request, 'marcas', {
      nombre: nombres.marca,
      descripcion: 'Marca temporal para aceptación automatizada',
      orden: 10
    });

    const modelo = await crearCatalogo(request, 'modelos', {
      nombre: nombres.modelo,
      descripcion: 'Modelo dependiente de una marca',
      catalogoPadreId: marca.id,
      orden: 10
    });

    const color = await crearCatalogo(request, 'colores', {
      nombre: nombres.color,
      descripcion: 'Color con código visual persistido',
      codigoVisual: '#1D4ED8',
      orden: 10
    });

    const talla = await crearCatalogo(request, 'tallas', {
      nombre: nombres.talla,
      descripcion: 'Talla reutilizable',
      orden: 10
    });

    expect(modelo.catalogoPadreId).toBe(marca.id);
    expect(modelo.catalogoPadreNombre).toBe(nombres.marca);
    expect(color.codigoVisual).toBe('#1D4ED8');

    const modelosMarca = await request.get(`${API_URL}/modelos?marcaId=${marca.id}`, {
      headers: authHeaders(adminToken)
    });
    expect(modelosMarca.status(), await modelosMarca.text()).toBe(200);
    expect((await dataOf(modelosMarca)).some((item: any) => item.id === modelo.id)).toBe(true);

    const actualizarColor = await request.put(`${API_URL}/colores/${color.id}`, {
      headers: authHeaders(adminToken),
      data: {
        nombre: nombres.color,
        descripcion: 'Color actualizado por E2E',
        codigoVisual: '#2563EB',
        orden: 11
      }
    });
    expect(actualizarColor.status(), await actualizarColor.text()).toBe(200);
    expect((await dataOf(actualizarColor)).codigoVisual).toBe('#2563EB');

    const buscarColor = await request.get(`${API_URL}/colores?buscar=${encodeURIComponent(suffix)}`, {
      headers: authHeaders(adminToken)
    });
    expect(buscarColor.status()).toBe(200);
    expect((await dataOf(buscarColor)).some((item: any) => item.id === color.id)).toBe(true);

    const desactivar = await request.patch(`${API_URL}/colores/${color.id}/desactivar`, {
      headers: authHeaders(adminToken)
    });
    expect(desactivar.status(), await desactivar.text()).toBe(200);
    expect((await dataOf(desactivar)).activo).toBe(false);

    const activosSinColor = await request.get(`${API_URL}/colores/activos`, {
      headers: authHeaders(adminToken)
    });
    expect((await dataOf(activosSinColor)).some((item: any) => item.id === color.id)).toBe(false);

    const activar = await request.patch(`${API_URL}/colores/${color.id}/activar`, {
      headers: authHeaders(adminToken)
    });
    expect(activar.status(), await activar.text()).toBe(200);

    const productoResponse = await request.post(`${API_URL}/productos`, {
      headers: authHeaders(adminToken),
      multipart: {
        Nombre: nombres.producto,
        MarcaId: String(marca.id),
        ModeloId: String(modelo.id),
        ColorId: String(color.id),
        TallaId: String(talla.id),
        Cantidad: '0',
        Costo: '100',
        Precio: '175',
        UmbralStockBajo: '5'
      }
    });
    expect(productoResponse.status(), await productoResponse.text()).toBe(201);
    const producto = await dataOf(productoResponse);
    expect(producto.marcaId).toBe(marca.id);
    expect(producto.modeloId).toBe(modelo.id);
    expect(producto.colorId).toBe(color.id);
    expect(producto.tallaId).toBe(talla.id);
    expect(producto.marca).toBe(nombres.marca);
    expect(producto.modelo).toBe(nombres.modelo);

    const borrarMarcaConModelos = await request.delete(`${API_URL}/marcas/${marca.id}`, {
      headers: authHeaders(adminToken)
    });
    expect(borrarMarcaConModelos.status()).toBe(400);

    const temporal = await crearCatalogo(request, 'colores', {
      nombre: `Color temporal ${suffix}`,
      codigoVisual: '#334155',
      orden: 99
    });
    const borrarTemporal = await request.delete(`${API_URL}/colores/${temporal.id}`, {
      headers: authHeaders(adminToken)
    });
    expect(borrarTemporal.status(), await borrarTemporal.text()).toBe(200);

    const buscarTemporal = await request.get(`${API_URL}/colores?buscar=${encodeURIComponent(temporal.nombre)}`, {
      headers: authHeaders(adminToken)
    });
    expect((await dataOf(buscarTemporal)).some((item: any) => item.id === temporal.id)).toBe(false);
  });

  test('Interfaz muestra mantenimientos, selectores de variantes, Agotado y diálogo accesible', async ({ request, page }) => {
    const categoriaResponse = await request.post(`${API_URL}/categorias`, {
      headers: authHeaders(adminToken),
      data: {
        nombre: nombres.categoria,
        descripcion: 'Categoría temporal para validar eliminación lógica'
      }
    });
    expect(categoriaResponse.status(), await categoriaResponse.text()).toBe(201);

    await loginUi(page);

    for (const [ruta, titulo] of [
      ['/colores', 'Colores'],
      ['/tallas', 'Tallas'],
      ['/marcas', 'Marcas'],
      ['/modelos', 'Modelos']
    ] as const) {
      await page.goto(ruta);
      await expect(page.getByRole('heading', { name: titulo, exact: true })).toBeVisible();
      await expect(page.getByText('Administra opciones reutilizables para productos, compras, ventas e inventario.')).toBeVisible();
    }

    await page.goto('/productos/nuevo');
    const primeraVariante = page.locator('.variant-card').first();
    await expect(primeraVariante).toBeVisible();
    await expect(primeraVariante.locator('mat-select[formcontrolname="marcaId"]')).toBeVisible();
    await expect(primeraVariante.locator('mat-select[formcontrolname="modeloId"]')).toBeVisible();
    await expect(primeraVariante.locator('mat-select[formcontrolname="colorId"]')).toBeVisible();
    await expect(primeraVariante.locator('mat-select[formcontrolname="tallaId"]')).toBeVisible();

    await page.goto('/productos');
    const inputBusqueda = page.locator('mat-form-field.search-field input');
    await expect(inputBusqueda).toBeVisible();
    await inputBusqueda.fill(nombres.producto);
    const filaProducto = page.locator('table.table-desktop tbody tr', { hasText: nombres.producto });
    await expect(filaProducto).toBeVisible();
    await expect(filaProducto.getByText('Agotado')).toBeVisible();

    await page.goto('/categorias');
    const filaCategoria = page.locator('table.table-desktop tbody tr', { hasText: nombres.categoria });
    await expect(filaCategoria).toBeVisible();
    await filaCategoria.getByTitle('Eliminar').click();

    const dialogo = page.getByRole('dialog');
    await expect(dialogo).toBeVisible();
    const confirmar = dialogo.getByRole('button', { name: 'Eliminar', exact: true });
    await expect(confirmar).toBeVisible();
    const estilos = await confirmar.evaluate((elemento) => {
      const style = getComputedStyle(elemento);
      return {
        color: style.color,
        backgroundColor: style.backgroundColor,
        opacity: Number(style.opacity),
        visibility: style.visibility
      };
    });
    expect(estilos.visibility).toBe('visible');
    expect(estilos.opacity).toBeGreaterThan(0.9);
    expect(estilos.backgroundColor).not.toBe('rgba(0, 0, 0, 0)');
    expect(estilos.color).not.toBe(estilos.backgroundColor);

    await confirmar.click();
    await expect(filaCategoria).toHaveCount(0);
    await page.reload();
    await expect(page.locator('table.table-desktop tbody tr', { hasText: nombres.categoria })).toHaveCount(0);
  });
});
