import { defineConfig } from '@playwright/test';

const baseURL = process.env['PLAYWRIGHT_TEST_BASE_URL'] ?? 'http://127.0.0.1:4200';

export default defineConfig({
  testDir: './e2e',
  timeout: 45_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  forbidOnly: Boolean(process.env['CI']),
  retries: 0,
  workers: 1,
  reporter: [
    ['list'],
    ['junit', { outputFile: 'test-results/n83-browser-e2e.xml' }],
    ['html', { outputFolder: 'playwright-report-n83', open: 'never' }]
  ],
  use: {
    baseURL,
    viewport: { width: 1280, height: 800 },
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },
  projects: [
    { name: 'chromium', use: { browserName: 'chromium' } },
    { name: 'firefox', use: { browserName: 'firefox' } },
    { name: 'webkit', use: { browserName: 'webkit' } }
  ]
});
