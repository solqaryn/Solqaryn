import { defineConfig } from '@playwright/test';

/** Aislamiento: NO usar playwright.config.ts (reescribe fixtures historicos con routes). */
if (process.env['C1304_API_URL'] !== 'http://127.0.0.1:5005') {
  throw new Error('Falta API local efimera de CI');
}

export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  forbidOnly: true,
  reporter: [
    ['list'],
    ['junit', { outputFile: 'test-results/c1304-real-jwt.xml' }],
    ['html', { outputFolder: 'playwright-report', open: 'never' }]
  ],
  use: {
    baseURL: process.env['PLAYWRIGHT_TEST_BASE_URL'] ?? 'http://127.0.0.1:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure'
  }
});
