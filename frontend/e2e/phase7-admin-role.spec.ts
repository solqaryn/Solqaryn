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
    data: {
      nombreUsuario: ADMIN_USERNAME,
      password: ADMIN_PASSWORD
    }
  });
  expect(response.status(), await response.text()).toBe(200);
  return (await dataOf(response)).token;
}

test('Administrador inicial queda vinculado al rol dinámico Administrador', async ({ request }) => {
  const token = await login(request);
  const response = await request.get(`${API_URL}/usuarios`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(response.status(), await response.text()).toBe(200);

  const users = await dataOf(response) as Array<Record<string, any>>;
  const admin = users.find((user) =>
    String(user.nombreUsuario).toLowerCase() === ADMIN_USERNAME.toLowerCase());

  expect(admin).toBeTruthy();
  expect(admin!.rolId).toBeGreaterThan(0);
  expect(String(admin!.rol).toLowerCase()).toBe('administrador');
});
