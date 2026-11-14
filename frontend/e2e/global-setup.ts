import { expect, request as playwrightRequest } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'];
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'];
const TENANT_ID = Number.parseInt(process.env['E2E_TENANT_ID'] ?? '1', 10);

function dataOf(payload: any): any {
  return payload?.data ?? payload?.Data;
}

export default async function globalSetup(): Promise<void> {
  if (!ADMIN_USERNAME || !ADMIN_PASSWORD) {
    throw new Error('PHASE7_ADMIN_USERNAME y PHASE7_ADMIN_PASSWORD son obligatorios para el bootstrap E2E.');
  }
  if (!Number.isInteger(TENANT_ID) || TENANT_ID <= 0) {
    throw new Error('E2E_TENANT_ID debe ser un identificador entero positivo.');
  }

  const api = await playwrightRequest.newContext({ baseURL: API_URL });

  try {
    const login = await api.post('/auth/login', {
      data: { nombreUsuario: ADMIN_USERNAME, password: ADMIN_PASSWORD }
    });
    expect(login.status(), await login.text()).toBe(200);
    const loginData = dataOf(await login.json());
    const token = loginData?.token;
    expect(token).toBeTruthy();

    const authHeaders = { Authorization: `Bearer ${token}` };

    // La membresía E2E debe existir antes de iniciar Playwright. Se valida por el
    // mismo endpoint fail-closed que usa el frontend; este bootstrap no crea ni
    // bypass-ea contexto tenant a través de endpoints protegidos.
    const tenantContext = await api.get(`/tenant-context/${TENANT_ID}`, {
      headers: authHeaders
    });
    expect(tenantContext.status(), await tenantContext.text()).toBe(200);

    const tenantHeaders = {
      ...authHeaders,
      'X-Empresa-Id': String(TENANT_ID)
    };
    const empresas = await api.get('/empresas', { headers: tenantHeaders });
    expect(empresas.status(), await empresas.text()).toBe(200);
    const empresasData = dataOf(await empresas.json());
    const empresa = Array.isArray(empresasData)
      ? empresasData.find((item: any) => item?.id === TENANT_ID)
      : null;
    expect(empresa?.id).toBe(TENANT_ID);
  } finally {
    await api.dispose();
  }
}
