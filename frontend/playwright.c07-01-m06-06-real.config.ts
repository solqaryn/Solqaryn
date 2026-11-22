import { defineConfig } from '@playwright/test';

// Identidades y token nacen de la API ASP.NET real en CI efimera, sin mocks.
if (process.env['CI'] !== 'true' || process.env['C1504_API_URL'] !== 'http://127.0.0.1:5005') {
  throw new Error('C0701_BROWSER_REQUIRES_REAL_EPHEMERAL_API_MYSQL84');
}
export default defineConfig({
  testDir: './e2e',
  timeout: 90_000,
  expect: { timeout: 20_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  forbidOnly: true,
  reporter: [['list'], ['junit', { outputFile: 'test-results/c0701-m06-real-browser.xml' }]],
  use: {
    baseURL: 'http://127.0.0.1:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure'
  }
});
