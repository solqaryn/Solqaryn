import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

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
  return (await dataOf(response)).token;
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function createInvoice(request: APIRequestContext, token: string): Promise<any> {
  const productResponse = await request.post(`${API_URL}/productos`, {
    headers: authHeaders(token),
    multipart: {
      Nombre: `Producto correo Fase 7 ${suffix}`,
      Marca: 'Solqaryn',
      Modelo: 'SMTP',
      Descripcion: 'Producto temporal para validar correo y PDF adjunto.',
      Cantidad: '10',
      Costo: '100',
      Precio: '225',
      UmbralStockBajo: '2'
    }
  });
  expect(productResponse.status(), await productResponse.text()).toBe(201);
  const product = await dataOf(productResponse);

  const variantsResponse = await request.get(`${API_URL}/productos/${product.id}/variantes`, {
    headers: authHeaders(token)
  });
  expect(variantsResponse.status(), await variantsResponse.text()).toBe(200);
  const variants = await dataOf(variantsResponse);
  const technicalVariant = variants.find((variant: any) => variant.esTecnica === true);
  expect(technicalVariant, 'El producto simple debe exponer exactamente una variante técnica.').toBeTruthy();

  const saleResponse = await request.post(`${API_URL}/ventas`, {
    headers: authHeaders(token),
    data: {
      clienteNombre: 'Cliente Correo Fase 7',
      clienteTelefono: '33425030',
      clienteCorreo: 'fase7@example.com',
      metodoPago: 'Efectivo',
      estadoPago: 'Pagado',
      notas: 'Certificación SMTP aislada',
      detalles: [{
        productoId: product.id,
        productoVarianteId: technicalVariant.id,
        cantidad: 1,
        precioUnitario: 225
      }]
    }
  });
  expect(saleResponse.status(), await saleResponse.text()).toBe(201);
  const sale = await dataOf(saleResponse);

  const confirmResponse = await request.post(`${API_URL}/ventas/${sale.id}/confirmar`, {
    headers: authHeaders(token)
  });
  expect(confirmResponse.status(), await confirmResponse.text()).toBe(200);

  const invoiceResponse = await request.get(`${API_URL}/facturas/venta/${sale.id}`, {
    headers: authHeaders(token)
  });
  expect(invoiceResponse.status(), await invoiceResponse.text()).toBe(200);
  return await dataOf(invoiceResponse);
}

async function expectNoOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(() =>
    Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) -
    document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(2);
}

test.describe('Fase 7 - correo SMTP aislado', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test('diagnostica SMTP, envía PDF A4, reintenta y evita duplicados', async ({ request, page }) => {
    const token = await loginApi(request);

    const statusResponse = await request.get(`${API_URL}/facturas/correo/estado`, {
      headers: authHeaders(token)
    });
    expect(statusResponse.status(), await statusResponse.text()).toBe(200);
    const smtpStatus = await dataOf(statusResponse);
    expect(smtpStatus.configurado).toBe(true);
    expect(smtpStatus.puerto).toBe(1025);
    expect(smtpStatus.usaTls).toBe(false);
    expect(smtpStatus.modoSeguridad).toBe('Sin TLS');
    expect(smtpStatus.requiereAutenticacion).toBe(false);
    expect(smtpStatus.maximoIntentos).toBe(3);
    expect(String(smtpStatus.remitenteEnmascarado)).not.toContain('smtp-pass');

    const diagnosticResponse = await request.post(`${API_URL}/facturas/correo/probar`, {
      headers: authHeaders(token)
    });
    expect(diagnosticResponse.status(), await diagnosticResponse.text()).toBe(200);
    const diagnostic = await dataOf(diagnosticResponse);
    expect(diagnostic.exito).toBe(true);
    expect(diagnostic.codigo).toBe('SMTP_OK');
    expect(diagnostic.autenticado).toBe(false);
    expect(diagnostic.modoSeguridad).toBe('Sin TLS');

    const invoice = await createInvoice(request, token);
    const idempotencyKey = `fase7-${suffix}`;
    const headers = { ...authHeaders(token), 'Idempotency-Key': idempotencyKey };

    const sendResponse = await request.post(`${API_URL}/facturas/${invoice.id}/compartir/correo`, {
      headers,
      data: { destinatario: 'fase7@example.com' }
    });
    expect(sendResponse.status(), await sendResponse.text()).toBe(200);
    const firstResult = await dataOf(sendResponse);
    expect(firstResult.exito).toBe(true);
    expect(firstResult.intentos).toBe(2);
    expect(firstResult.yaProcesado).toBe(false);
    expect(firstResult.codigo).toBe('ENVIADO');
    expect(firstResult.messageId).toMatch(/^solqaryn-/);

    const duplicateResponse = await request.post(`${API_URL}/facturas/${invoice.id}/compartir/correo`, {
      headers,
      data: { destinatario: 'fase7@example.com' }
    });
    expect(duplicateResponse.status(), await duplicateResponse.text()).toBe(200);
    const duplicateResult = await dataOf(duplicateResponse);
    expect(duplicateResult.exito).toBe(true);
    expect(duplicateResult.yaProcesado).toBe(true);
    expect(duplicateResult.messageId).toBe(firstResult.messageId);

    const historyResponse = await request.get(`${API_URL}/facturas/${invoice.id}/historial-envios`, {
      headers: authHeaders(token)
    });
    expect(historyResponse.status(), await historyResponse.text()).toBe(200);
    const emailHistory = (await dataOf(historyResponse)).filter((item: any) =>
      item.canal === 'Correo' && item.destinatario === 'fase7@example.com');
    expect(emailHistory).toHaveLength(1);
    expect(emailHistory[0].resultado).toBe('Enviado (2 intentos)');
    expect(emailHistory[0].error ?? null).toBeNull();

    const oversizedKey = await request.post(`${API_URL}/facturas/${invoice.id}/compartir/correo`, {
      headers: { ...authHeaders(token), 'Idempotency-Key': 'x'.repeat(129) },
      data: { destinatario: 'fase7@example.com' }
    });
    expect(oversizedKey.status()).toBe(400);

    await page.setViewportSize({ width: 390, height: 844 });
    await loginUi(page);
    await page.goto(`/facturas/${invoice.id}`);
    await page.getByRole('button', { name: 'Enviar por correo' }).click();
    const panel = page.locator('.panel-correo');
    const correoInput = panel.locator('input');
    await expect(panel).toBeVisible();
    await expect(panel.getByText('SMTP verificado')).toBeVisible({ timeout: 15_000 });
    await expect(panel.getByText(/Conexión SMTP comprobada correctamente; autenticación deshabilitada para este entorno/)).toBeVisible();
    await correoInput.fill('fase7@example.com');
    await expect(correoInput).toHaveValue('fase7@example.com');
    await expect(panel.getByRole('button', { name: 'Enviar' })).toBeEnabled();
    await expectNoOverflow(page);
    await page.screenshot({ path: 'test-results/fase7/correo-panel-mobile.png', fullPage: true });
  });
});
