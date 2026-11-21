import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  testMatch: 'c06-01-m05-04-acceptance.spec.ts',
  fullyParallel: false,
  forbidOnly: Boolean(process.env['CI']),
  retries: 0, workers: 1, timeout: 55_000,
  expect: { timeout: 10_000 },
  reporter: [['list'], ['junit', { outputFile: 'test-results/c0601-positive-acceptance.xml' }]],
  use: { baseURL: 'http://127.0.0.1:4200', trace: 'retain-on-failure', screenshot: 'only-on-failure' }
});
