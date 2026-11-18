import { defineConfig } from '@playwright/test';

// C15 real: no cargar playwright.config.ts, que reescribe fixtures simulados.
if (process.env['CI'] !== 'true' || process.env['C1504_API_URL'] !== 'http://127.0.0.1:5005') {
  throw new Error('C15 JWT: requiere GitHub CI aislada con API localhost.');
}
export default defineConfig({
  testDir: './e2e',
  timeout: 75_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  forbidOnly: true,
  reporter: [['list'], ['junit', { outputFile: 'test-results/c1504-real-jwt.xml' }]],
  use: {
    baseURL: 'http://127.0.0.1:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure'
  }
});
