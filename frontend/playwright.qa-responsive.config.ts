import { defineConfig } from '@playwright/test';

// No importa la configuracion general que adapta specs con tokens sinteticos.
// Este barrido usa login, tenant-context y API reales en una BD desechable.
const baseURL = 'http://127.0.0.1:4200';
if (process.env.CI !== 'true' || process.env.GITHUB_REF !== 'refs/heads/qa' ||
    process.env.PLAYWRIGHT_TEST_BASE_URL !== baseURL ||
    process.env.PHASE7_API_URL !== 'http://127.0.0.1:5005' ||
    process.env.E2E_TENANT_ID !== '1') {
  throw new Error('Contexto no autorizado para la auditoria integral local de QA.');
}
export default defineConfig({
  testDir: './e2e',
  testMatch: 'responsive-global.spec.ts',
  globalSetup: './e2e/qa-responsive-setup.ts',
  timeout: 45_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  forbidOnly: true,
  retries: 0,
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL,
    // El interceptor Angular adjunta el tenant solo a peticiones autenticadas;
    // una cabecera global de Playwright contamina fuentes y otros recursos externos.
    storageState: {
      cookies: [],
      origins: [{ origin: baseURL, localStorage: [{ name: 'solqaryn_empresa_solicitada_id', value: '1' }] }]
    },
    trace: 'off',
    screenshot: 'off',
    video: 'off'
  }
});
