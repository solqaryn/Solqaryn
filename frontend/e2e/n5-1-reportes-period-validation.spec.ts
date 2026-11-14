import { test, expect, APIRequestContext, APIResponse } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function dataOf(response: APIResponse): Promise<any> {
  const payload = await response.json();
  return payload.data ?? payload.Data;
}

async function login(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, {
    data: { nombreUsuario: ADMIN_USERNAME, password: ADMIN_PASSWORD }
  });
  expect(response.status(), await response.text()).toBe(200);
  const data = await dataOf(response);
  expect(data.token).toBeTruthy();
  return data.token;
}

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

test.describe('N5.1.G — validación causal de período de reportes', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test('A1: resumen sin token falla cerrado con 401', async ({ request }) => {
    const response = await request.get(`${API_URL}/reportes-administrativos/resumen`);
    expect(response.status(), await response.text()).toBe(401);
  });

  test('B2: Desde mayor que Hasta se rechaza fail-closed', async ({ request }) => {
    const token = await login(request);
    const response = await request.get(
      `${API_URL}/reportes-administrativos/resumen?desde=2026-09-09&hasta=2026-09-08`,
      { headers: auth(token) }
    );
    expect(response.status(), await response.text()).toBe(400);
    const body = (await response.text()).toLowerCase();
    expect(body).toContain('fecha hasta');
  });

  test('B3: período mayor de 366 días se rechaza', async ({ request }) => {
    const token = await login(request);
    const response = await request.get(
      `${API_URL}/reportes-administrativos/resumen?desde=2025-01-01&hasta=2026-01-03`,
      { headers: auth(token) }
    );
    expect(response.status(), await response.text()).toBe(400);
    const body = (await response.text()).toLowerCase();
    expect(body).toContain('366');
  });
});
