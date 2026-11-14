import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
let token = '';

function headers(): Record<string, string> { return { Authorization: `Bearer ${token}` }; }
async function dataOf(response: APIResponse): Promise<any> { const body = await response.json(); return body.data ?? body.Data; }

async function loginApi(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, { data: { nombreUsuario: ADMIN_USERNAME, password: ADMIN_PASSWORD } });
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

test.describe('M12 — automatización transversal', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => { token = await loginApi(request); });

  test('expone defaults administrables y versionados', async ({ request }) => {
    const response = await request.get(`${API_URL}/automatizaciones/configuracion`, { headers: headers() });
    expect(response.status(), await response.text()).toBe(200);
    const config = await dataOf(response);
    expect(config.versionReglas).toBe('M12.1');
    expect(config.limiteAutocompletado).toBeGreaterThanOrEqual(5);
    expect(config.limiteSugerencias).toBeGreaterThanOrEqual(5);
  });

  test('actualiza preferencias de forma persistente y validada', async ({ request }) => {
    const current = await dataOf(await request.get(`${API_URL}/automatizaciones/configuracion`, { headers: headers() }));
    const update = await request.put(`${API_URL}/automatizaciones/configuracion`, {
      headers: headers(),
      data: {
        diasBorradorVentaAlerta: 3,
        diasBorradorCompraAlerta: 8,
        diasCargaPendienteAlerta: 2,
        diasMovimientoFinancieroPendienteAlerta: 8,
        limiteSugerencias: 25,
        limiteAutocompletado: 12,
        mostrarRecordatoriosDashboard: true
      }
    });
    expect(update.status(), await update.text()).toBe(200);
    const saved = await dataOf(update);
    expect(saved.versionReglas).toBe('M12.1');
    expect(saved.diasBorradorVentaAlerta).toBe(3);
    expect(saved.limiteAutocompletado).toBe(12);

    await request.put(`${API_URL}/automatizaciones/configuracion`, {
      headers: headers(),
      data: {
        diasBorradorVentaAlerta: current.diasBorradorVentaAlerta,
        diasBorradorCompraAlerta: current.diasBorradorCompraAlerta,
        diasCargaPendienteAlerta: current.diasCargaPendienteAlerta,
        diasMovimientoFinancieroPendienteAlerta: current.diasMovimientoFinancieroPendienteAlerta,
        limiteSugerencias: current.limiteSugerencias,
        limiteAutocompletado: current.limiteAutocompletado,
        mostrarRecordatoriosDashboard: current.mostrarRecordatoriosDashboard
      }
    });
  });

  test('sugerencias y acciones masivas son deterministas y no mutan', async ({ request }) => {
    const suggestionsResponse = await request.get(`${API_URL}/automatizaciones/sugerencias`, { headers: headers() });
    expect(suggestionsResponse.status(), await suggestionsResponse.text()).toBe(200);
    const suggestions = await dataOf(suggestionsResponse);
    expect(suggestions.versionReglas).toBe('M12.1');
    expect(Array.isArray(suggestions.sugerencias)).toBe(true);
    expect(suggestions.sugerencias.every((item: any) => item.requiereConfirmacion === true)).toBe(true);

    const preview = await request.post(`${API_URL}/automatizaciones/acciones-masivas/previsualizar`, {
      headers: headers(), data: { accion: 'revisar-stock-bajo', ids: [999999] }
    });
    expect(preview.status(), await preview.text()).toBe(200);
    const result = await dataOf(preview);
    expect(result.soloVistaPrevia).toBe(true);
    expect(result.requiereConfirmacion).toBe(true);
    expect(result.aplicables).toBe(0);
  });

  test('autocompletado aplica mínimo de búsqueda y falla cerrado en contextos desconocidos', async ({ request }) => {
    const short = await request.get(`${API_URL}/automatizaciones/autocompletar?contexto=clientes&q=a`, { headers: headers() });
    expect(short.status(), await short.text()).toBe(200);
    expect(await dataOf(short)).toEqual([]);

    const invalid = await request.get(`${API_URL}/automatizaciones/autocompletar?contexto=finanzas&q=ab`, { headers: headers() });
    expect(invalid.status()).toBe(400);
  });

  test('dashboard presenta el asistente operativo sin automatizar escrituras', async ({ page }) => {
    await loginUi(page);
    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: 'Asistente operativo' })).toBeVisible();
    await expect(page.getByText(/Ninguna sugerencia modifica datos automáticamente/)).toBeVisible();
  });
});
