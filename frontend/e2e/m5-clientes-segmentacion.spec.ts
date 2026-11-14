import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';
import { readFile } from 'node:fs/promises';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const suffix = `${Date.now()}`;

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

function filaCliente(page: Page, nombre: string) {
  return page.locator('table.table-desktop tbody tr', { hasText: nombre });
}

test.describe('M5 — Clientes y segmentación', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  let token = '';
  let segmentoA: any;
  let segmentoB: any;
  const clienteA = `Cliente Segmento A ${suffix}`;
  const clienteB = `Cliente Segmento B ${suffix}`;

  test.beforeAll(async ({ request }) => {
    token = await loginApi(request);

    const crearSegmentoA = await request.post(`${API_URL}/tipo-clientes`, {
      headers: authHeaders(token),
      data: {
        nombre: `Segmento E2E A ${suffix}`,
        descripcion: 'Clasificación dinámica creada por M5',
        colorHex: '#2563EB',
        activo: true,
        orden: 501,
        esPredeterminado: false
      }
    });
    expect(crearSegmentoA.status(), await crearSegmentoA.text()).toBe(201);
    segmentoA = await dataOf(crearSegmentoA);

    const crearSegmentoB = await request.post(`${API_URL}/tipo-clientes`, {
      headers: authHeaders(token),
      data: {
        nombre: `Segmento E2E B ${suffix}`,
        descripcion: 'Segunda clasificación dinámica creada por M5',
        colorHex: '#16A34A',
        activo: true,
        orden: 502,
        esPredeterminado: false
      }
    });
    expect(crearSegmentoB.status(), await crearSegmentoB.text()).toBe(201);
    segmentoB = await dataOf(crearSegmentoB);

    const crearClienteA = await request.post(`${API_URL}/clientes`, {
      headers: authHeaders(token),
      data: {
        nombre: clienteA,
        correo: `segmento-a-${suffix}@example.test`,
        direccion: 'Dirección M5 A',
        tipoClienteId: segmentoA.id
      }
    });
    expect(crearClienteA.status(), await crearClienteA.text()).toBe(201);

    const crearClienteB = await request.post(`${API_URL}/clientes`, {
      headers: authHeaders(token),
      data: {
        nombre: clienteB,
        correo: `segmento-b-${suffix}@example.test`,
        direccion: 'Dirección M5 B',
        tipoClienteId: segmentoB.id
      }
    });
    expect(crearClienteB.status(), await crearClienteB.text()).toBe(201);
  });

  test('catálogo es administrable y SIN_CLASIFICAR permanece protegido', async ({ request }) => {
    const tiposResponse = await request.get(`${API_URL}/tipo-clientes`, { headers: authHeaders(token) });
    expect(tiposResponse.status(), await tiposResponse.text()).toBe(200);
    const tipos = await dataOf(tiposResponse);

    expect(tipos.some((tipo: any) => tipo.id === segmentoA.id && tipo.nombre.includes('Segmento E2E A'))).toBe(true);
    expect(tipos.some((tipo: any) => tipo.id === segmentoB.id && tipo.nombre.includes('Segmento E2E B'))).toBe(true);

    const sinClasificar = tipos.find((tipo: any) => tipo.codigo === 'SIN_CLASIFICAR');
    expect(sinClasificar).toBeTruthy();
    expect(sinClasificar.esSistema).toBe(true);
    expect(sinClasificar.activo).toBe(true);

    const desactivar = await request.patch(`${API_URL}/tipo-clientes/${sinClasificar.id}/desactivar`, {
      headers: authHeaders(token)
    });
    expect(desactivar.status(), await desactivar.text()).toBe(400);

    const eliminar = await request.delete(`${API_URL}/tipo-clientes/${sinClasificar.id}`, {
      headers: authHeaders(token)
    });
    expect(eliminar.status(), await eliminar.text()).toBe(400);
  });

  test('UI filtra por segmento, muestra métricas y restaura navegación', async ({ page }) => {
    await loginUi(page);

    await page.goto(`/clientes?tipoClienteId=${segmentoA.id}`);
    await expect(page.getByRole('heading', { name: 'Clientes', exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Segmentación', exact: true })).toBeVisible();
    await expect(filaCliente(page, clienteA)).toBeVisible();
    await expect(filaCliente(page, clienteB)).toHaveCount(0);

    const tarjetaA = page.locator('.segment-card', { hasText: segmentoA.nombre });
    const tarjetaB = page.locator('.segment-card', { hasText: segmentoB.nombre });
    await expect(tarjetaA).toBeVisible();
    await expect(tarjetaA).toHaveClass(/selected/);
    await expect(tarjetaA).toContainText('1 cliente');
    await expect(tarjetaB).toBeVisible();
    await expect(tarjetaB).toContainText('1 cliente');

    await expect(page).toHaveURL(new RegExp(`tipoClienteId=${segmentoA.id}`));
    await expect.poll(async () => page.evaluate((id) => {
      return Object.entries(sessionStorage).some(([key, value]) =>
        key.includes('.e2e_admin.clientes') && value.includes(`"tipoClienteId":${id}`));
    }, segmentoA.id)).toBe(true);

    await page.goto('/dashboard');
    await page.goto('/clientes');
    await expect(filaCliente(page, clienteA)).toBeVisible();
    await expect(filaCliente(page, clienteB)).toHaveCount(0);
    await expect(page).toHaveURL(new RegExp(`tipoClienteId=${segmentoA.id}`));

    await page.getByRole('button', { name: 'Limpiar filtros' }).click();
    await expect(filaCliente(page, clienteA)).toBeVisible();
    await expect(filaCliente(page, clienteB)).toBeVisible();
    await expect(page).not.toHaveURL(/tipoClienteId=/);
  });

  test('exporta CSV con el conjunto filtrado completo', async ({ page }) => {
    await loginUi(page);
    await page.goto(`/clientes?tipoClienteId=${segmentoA.id}`);
    await expect(filaCliente(page, clienteA)).toBeVisible();

    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('button', { name: 'Exportar CSV' }).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/^clientes-segmentacion-\d{4}-\d{2}-\d{2}\.csv$/);

    const path = await download.path();
    expect(path).toBeTruthy();
    const contenido = await readFile(path!, 'utf8');
    expect(contenido).toContain(clienteA);
    expect(contenido).toContain(segmentoA.nombre);
    expect(contenido).not.toContain(clienteB);
    expect(contenido).toContain('Total vendido');
  });
});
