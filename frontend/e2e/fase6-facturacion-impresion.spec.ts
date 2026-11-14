import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const EVIDENCE_DIR = 'test-results/fase6';

const formatos = [
  { codigo: 'a4', ancho: 595.28, alto: 841.89, continuo: false },
  { codigo: 'carta', ancho: 612, alto: 792, continuo: false },
  { codigo: 'legal', ancho: 612, alto: 1008, continuo: false },
  { codigo: 'oficio', ancho: 612, alto: 936, continuo: false },
  { codigo: 'a5', ancho: 419.53, alto: 595.28, continuo: false },
  { codigo: 'pos58', ancho: 164.41, alto: 0, continuo: true },
  { codigo: 'pos80', ancho: 226.77, alto: 0, continuo: true }
] as const;

type FormatoCodigo = typeof formatos[number]['codigo'];

let token = '';
let facturaId = 0;
let numeroFactura = '';

function authHeaders(): Record<string, string> {
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
  return data.token;
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function crearProducto(
  request: APIRequestContext,
  indice: number
): Promise<{ id: number; varianteTecnicaId: number }> {
  const response = await request.post(`${API_URL}/productos`, {
    headers: authHeaders(),
    multipart: {
      Nombre: `Producto impresión Fase 6 número ${indice} con descripción extensa`,
      Marca: 'Solqaryn Printing',
      Modelo: `Modelo-PDF-${indice}`,
      Descripcion: 'Producto temporal para validar ajuste de líneas, tablas y rollos térmicos.',
      Cantidad: '40',
      Costo: String(80 + indice * 10),
      Precio: String(170 + indice * 25),
      UmbralStockBajo: '3'
    }
  });
  expect(response.status(), await response.text()).toBe(201);
  const producto = await dataOf(response);
  const variantesResponse = await request.get(`${API_URL}/productos/${producto.id}/variantes`, {
    headers: authHeaders()
  });
  expect(variantesResponse.status(), await variantesResponse.text()).toBe(200);
  const variantes = await dataOf(variantesResponse);
  const tecnica = variantes.find((item: any) => item.esTecnica === true);
  expect(tecnica, `El producto ${producto.id} debe tener una variante técnica.`).toBeTruthy();
  return { id: Number(producto.id), varianteTecnicaId: Number(tecnica.id) };
}

function leerMediaBox(pdf: Buffer): { ancho: number; alto: number } {
  const contenido = pdf.toString('latin1');
  const match = contenido.match(/\/MediaBox\s*\[\s*0(?:\.0+)?\s+0(?:\.0+)?\s+([0-9]+(?:\.[0-9]+)?)\s+([0-9]+(?:\.[0-9]+)?)\s*\]/);
  expect(match, 'El PDF debe exponer un MediaBox verificable.').toBeTruthy();
  return { ancho: Number(match![1]), alto: Number(match![2]) };
}

function nombreVisible(codigo: FormatoCodigo): string {
  return ({
    a4: 'A4',
    carta: 'Carta',
    legal: 'Legal',
    oficio: 'Oficio',
    a5: 'A5',
    pos58: 'POS 58 mm',
    pos80: 'POS 80 mm'
  } as const)[codigo];
}

function escaparRegExp(valor: string): string {
  return valor.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

async function seleccionarFormato(page: Page, codigo: FormatoCodigo): Promise<void> {
  const preview = page.locator('.preview-shell');
  if (await preview.getAttribute('data-formato') === codigo) {
    await expect(preview).toHaveAttribute('data-formato', codigo);
    return;
  }

  await page.locator('.formato-field mat-select').click();
  const listbox = page.getByRole('listbox', { name: 'Formato de papel para la factura' });
  await expect(listbox).toBeVisible();
  await page.getByRole('option', {
    name: new RegExp(`^${escaparRegExp(nombreVisible(codigo))}`)
  }).click();
  await expect(listbox).toBeHidden();
  await expect(preview).toHaveAttribute('data-formato', codigo);
  await page.waitForTimeout(180);
}

async function expectNoDocumentOverflow(page: Page, tolerancia = 2): Promise<void> {
  const overflow = await page.evaluate(() =>
    Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(tolerancia);
}

test.describe('Fase 6 — facturación e impresión', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    mkdirSync(EVIDENCE_DIR, { recursive: true });
    token = await loginApi(request);

    const productos = await Promise.all([1, 2, 3, 4].map((i) => crearProducto(request, i)));
    const ventaResponse = await request.post(`${API_URL}/ventas`, {
      headers: authHeaders(),
      data: {
        clienteNombre: 'Cliente Fase 6 con nombre deliberadamente largo para impresión',
        clienteTelefono: '33425030',
        clienteIdentidadORTN: '0801199012345',
        clienteCorreo: 'fase6@example.invalid',
        metodoPago: 'Efectivo',
        estadoPago: 'Pagado',
        notas: 'Certificación de Carta, Legal, Oficio, A4, A5, POS 58 mm y POS 80 mm.',
        detalles: productos.map((producto, index) => ({
          productoId: producto.id,
          productoVarianteId: producto.varianteTecnicaId,
          cantidad: index + 1,
          precioUnitario: 170 + (index + 1) * 25
        }))
      }
    });
    expect(ventaResponse.status(), await ventaResponse.text()).toBe(201);
    const venta = await dataOf(ventaResponse);

    const confirmar = await request.post(`${API_URL}/ventas/${venta.id}/confirmar`, {
      headers: authHeaders()
    });
    expect(confirmar.status(), await confirmar.text()).toBe(200);

    const facturaResponse = await request.get(`${API_URL}/facturas/venta/${venta.id}`, {
      headers: authHeaders()
    });
    expect(facturaResponse.status(), await facturaResponse.text()).toBe(200);
    const factura = await dataOf(facturaResponse);
    facturaId = factura.id;
    numeroFactura = factura.numeroFactura;
  });

  test('El catálogo publica los siete perfiles autorizados', async ({ request }) => {
    const response = await request.get(`${API_URL}/facturas/formatos-pdf`, {
      headers: authHeaders()
    });
    expect(response.status(), await response.text()).toBe(200);
    const catalogo = await dataOf(response);
    expect(catalogo.map((x: any) => x.codigo)).toEqual(formatos.map((x) => x.codigo));
    expect(catalogo.filter((x: any) => x.esContinuo).map((x: any) => x.codigo)).toEqual(['pos58', 'pos80']);
  });

  test('Cada PDF tiene firma válida, perfil declarado y dimensiones físicas correctas', async ({ request }) => {
    for (const formato of formatos) {
      const response = await request.get(`${API_URL}/facturas/${facturaId}/pdf?formato=${formato.codigo}`, {
        headers: authHeaders()
      });
      expect(response.status(), `${formato.codigo}: ${await response.text()}`).toBe(200);
      expect(response.headers()['content-type']).toContain('application/pdf');
      expect(response.headers()['x-factura-formato']).toBe(formato.codigo);
      expect(response.headers()['content-disposition']).toContain(`${formato.codigo}.pdf`);

      const pdf = await response.body();
      expect(pdf.subarray(0, 4).toString()).toBe('%PDF');
      expect(pdf.length).toBeGreaterThan(3_000);

      const mediaBox = leerMediaBox(pdf);
      expect(Math.abs(mediaBox.ancho - formato.ancho)).toBeLessThanOrEqual(2.5);
      if (formato.continuo) {
        expect(mediaBox.alto).toBeGreaterThan(mediaBox.ancho);
        expect(mediaBox.alto, `${formato.codigo} no debe convertirse en una hoja de 297 mm`).toBeLessThan(800);
        expect(Math.abs(mediaBox.alto - 841.89)).toBeGreaterThan(30);
      } else {
        expect(Math.abs(mediaBox.alto - formato.alto)).toBeLessThanOrEqual(2.5);
      }

      writeFileSync(join(EVIDENCE_DIR, `${numeroFactura}-${formato.codigo}.pdf`), pdf);
    }

    const invalido = await request.get(`${API_URL}/facturas/${facturaId}/pdf?formato=desconocido`, {
      headers: authHeaders()
    });
    expect(invalido.status()).toBe(400);
  });

  test('La interfaz cambia la vista previa, advierte el control del driver y no desborda', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 1000 });
    await loginUi(page);
    await page.goto(`/facturas/${facturaId}`);
    await expect(page.locator('.factura-sheet')).toBeVisible();
    await expect(page.getByText('Correo, WhatsApp y enlaces públicos conservan A4')).toBeVisible();

    for (const formato of formatos) {
      await seleccionarFormato(page, formato.codigo);
      await expect(page.locator('.profile-summary strong')).toHaveText(nombreVisible(formato.codigo));
      if (formato.continuo) {
        await expect(page.getByText(/El PDF térmico usa altura automática/)).toBeVisible();
      }
      await page.screenshot({ path: join(EVIDENCE_DIR, `preview-${formato.codigo}.png`), fullPage: true });
      await expectNoDocumentOverflow(page);
    }

    await page.setViewportSize({ width: 390, height: 844 });
    await seleccionarFormato(page, 'pos58');
    await expect(page.locator('.factura-sheet[data-formato="pos58"]')).toBeVisible();
    await expectNoDocumentOverflow(page, 3);
    await page.screenshot({ path: join(EVIDENCE_DIR, 'preview-pos58-mobile.png'), fullPage: true });

    await seleccionarFormato(page, 'a4');
    await expect(page.locator('.preview-shell')).toHaveCSS('overflow-x', 'auto');
    await expectNoDocumentOverflow(page, 3);
  });

  test('Descarga solicita POS80 y la impresión muestra el tamaño real antes del driver', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 900 });
    await loginUi(page);
    await page.goto(`/facturas/${facturaId}`);
    await seleccionarFormato(page, 'pos80');

    const [download, downloadResponse] = await Promise.all([
      page.waitForEvent('download'),
      page.waitForResponse((response) => response.url().includes(`/facturas/${facturaId}/pdf?formato=pos80`)),
      page.getByRole('button', { name: /Descargar POS 80 mm/i }).click()
    ]);
    expect(downloadResponse.status()).toBe(200);
    expect(download.suggestedFilename()).toContain('pos80.pdf');
    await expect(page.getByText(/Altura real del PDF:/)).toBeVisible();

    const [popup, printResponse] = await Promise.all([
      page.waitForEvent('popup'),
      page.waitForResponse((response) => response.url().includes(`/facturas/${facturaId}/pdf?formato=pos80`)),
      page.getByRole('button', { name: /Preparar POS 80 mm para imprimir/i }).click()
    ]);
    expect(printResponse.status()).toBe(200);
    await expect(popup.getByRole('heading', { name: 'Ticket térmico listo' })).toBeVisible();
    await expect(popup.locator('.medida')).toHaveText(/80\.0 × [0-9.]+ mm/);
    await expect(popup.getByText(/si el diálogo de impresión muestra 80 × 297 mm/i)).toBeVisible();
    await expect(popup.getByRole('button', { name: 'Abrir PDF e imprimir' })).toBeVisible();
    await popup.screenshot({ path: join(EVIDENCE_DIR, 'preparacion-pos80-driver.png'), fullPage: true });
    await popup.close();
  });
});
