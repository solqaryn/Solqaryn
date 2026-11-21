import { defineConfig } from '@playwright/test';

// Isolated public-route browser testing: no global tenant headers, auth storage
// state, private bootstrap or page.route mocks.
export default defineConfig({
  testDir: './e2e',
  testMatch: 'c06-01-m04-02-public-status.spec.ts',
  fullyParallel: false,
  forbidOnly: Boolean(process.env['CI']),
  retries: 0,
  workers: 1,
  timeout: 45_000,
  expect: { timeout: 10_000 },
  reporter: [
    ['list'],
    ['junit', { outputFile: 'test-results/c0601-public-status.xml' }]
  ],
  use: {
    baseURL: 'http://127.0.0.1:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure'
  }
});
