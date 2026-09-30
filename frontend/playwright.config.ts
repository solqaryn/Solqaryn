import { readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { defineConfig } from '@playwright/test';
import { ensureSeedAdminTenantMembershipForCi } from './e2e/tenant-bootstrap';

const baseURL = process.env['PLAYWRIGHT_TEST_BASE_URL'] ?? 'http://127.0.0.1:4200';
const tenantId = process.env['E2E_TENANT_ID'] ?? '1';
const connectionString = process.env['ConnectionStrings__DefaultConnection'] ?? '';
const hasTenantBootstrapContext = Boolean(
  process.env['E2E_TENANT_ID'] &&
  process.env['PHASE7_ADMIN_USERNAME'] &&
  process.env['PHASE7_ADMIN_PASSWORD']
);
const requiresExplicitTenantSelection =
  process.env['CI'] === 'true' && connectionString.includes('Database=solqaryn_n11_sucursales;');

function enableTenantAwareFixtureForCi(): void {
  if (!process.env['CI']) return;

  const e2eDir = resolve(process.cwd(), 'e2e');
  for (const entry of readdirSync(e2eDir, { withFileTypes: true })) {
    if (!entry.isFile() || !entry.name.endsWith('.spec.ts')) continue;

    const path = resolve(e2eDir, entry.name);
    const source = readFileSync(path, 'utf8');
    const tenantAwareSource = source.replace(
      /from\s+(['"])@playwright\/test\1/g,
      "from './tenant-aware-test'"
    );

    if (tenantAwareSource !== source) {
      writeFileSync(path, tenantAwareSource, 'utf8');
    }
  }
}

ensureSeedAdminTenantMembershipForCi(tenantId);
enableTenantAwareFixtureForCi();

export default defineConfig({
  testDir: './e2e',
  globalSetup: hasTenantBootstrapContext ? './e2e/global-setup.ts' : undefined,
  timeout: 45_000,
  expect: {
    timeout: 10_000
  },
  fullyParallel: false,
  forbidOnly: Boolean(process.env['CI']),
  retries: process.env['CI'] ? 1 : 0,
  workers: process.env['CI'] ? 1 : undefined,
  reporter: [
    ['list'],
    ['junit', { outputFile: 'test-results/phase7-e2e.xml' }],
    ['html', { outputFolder: 'playwright-report', open: 'never' }]
  ],
  use: {
    baseURL,
    extraHTTPHeaders: {
      'X-Empresa-Id': tenantId
    },
    storageState: requiresExplicitTenantSelection
      ? undefined
      : {
          cookies: [],
          origins: [
            {
              origin: new URL(baseURL).origin,
              localStorage: [
                { name: 'solqaryn_empresa_solicitada_id', value: tenantId }
              ]
            }
          ]
        },
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  }
});
