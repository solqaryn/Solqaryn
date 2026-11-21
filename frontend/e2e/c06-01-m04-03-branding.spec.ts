import { expect, test } from '@playwright/test';

// Real Angular 22 public route in Chromium. No mock auth, API or tenant state.
test('C06-01.M04.03 allowed palette modes and keyboard-only activation', async ({ page }) => {
  const privateRequests: string[] = [];
  page.on('request', req => {
    if (/\/api\/|\/tenant-context|\/empresa-configuracion/.test(new URL(req.url()).pathname)) privateRequests.push(req.url());
  });
  await page.goto('/conoce-solqaryn?brand=tenant-B&theme=javascript:alert(1)');
  const shell = page.getByTestId('c0601-public-saas-landing');
  const toggle = page.getByTestId('c0601-brand-toggle');
  await expect(shell).toHaveAttribute('data-brand-theme', 'institucional');
  await expect(toggle).toHaveAttribute('aria-pressed', 'false');
  const before = await shell.evaluate(node => getComputedStyle(node).backgroundColor);
  for (let n = 0; n < 7; n++) {
    if (await toggle.evaluate(node => node === document.activeElement)) break;
    await page.keyboard.press('Tab');
  }
  await expect(toggle).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(shell).toHaveAttribute('data-brand-theme', 'contraste');
  await expect(toggle).toHaveAttribute('aria-pressed', 'true');
  const after = await shell.evaluate(node => getComputedStyle(node).backgroundColor);
  expect(after).not.toBe(before);
  await page.keyboard.press('Space');
  await expect(shell).toHaveAttribute('data-brand-theme', 'institucional');
  expect(privateRequests, 'brand choice cannot query private HTTP or trust tenant URL').toEqual([]);
  await expect(page.locator('form, input, iframe, button[type=submit]')).toHaveCount(0);
});

test('C06-01.M04.03 real screenshots before/after at 320/390/768/1280/1440', async ({ page }) => {
  const shell = page.getByTestId('c0601-public-saas-landing');
  const toggle = page.getByTestId('c0601-brand-toggle');
  for (const width of [320, 390, 768, 1280, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/conoce-solqaryn');
    await expect(shell).toHaveAttribute('data-brand-theme', 'institucional');
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await test.info().attach('antes-institucional-' + width, { body: await page.screenshot({ fullPage: true }), contentType: 'image/png' });
    await toggle.click();
    await expect(shell).toHaveAttribute('data-brand-theme', 'contraste');
    await test.info().attach('despues-contraste-' + width, { body: await page.screenshot({ fullPage: true }), contentType: 'image/png' });
    const dims = await page.evaluate(() => ({ total: document.documentElement.scrollWidth, viewport: window.innerWidth }));
    expect(dims.viewport).toBe(width);
    expect(dims.total, 'body overflow ' + width).toBeLessThanOrEqual(width + 1);
  }
});

test('C06-01.M04.03 never leaks private business API or double-submits brand selection', async ({ page }) => {
  const mutations: string[] = [];
  page.on('request', req => {
    if (/^(POST|PUT|PATCH|DELETE)$/.test(req.method())) mutations.push(req.method() + ':' + req.url());
  });
  await page.goto('/conoce-solqaryn');
  const toggle = page.getByTestId('c0601-brand-toggle');
  await toggle.click();
  const checked = await page.locator('app-c0601-public-saas-landing').evaluate(root => {
    const shell = root.querySelector('.landing-shell')!;
    const lede = root.querySelector('.hero .lede')!;
    return { bg: getComputedStyle(shell).backgroundColor, fg: getComputedStyle(lede).color, variant: shell.getAttribute('data-brand-theme') };
  });
  expect(checked.variant).toBe('contraste');
  expect(checked.bg).not.toBe(checked.fg);
  await expect(page.getByRole('link', { name: /Ir a mi cuenta/ })).toHaveAttribute('href', '/login');
  expect(mutations).toEqual([]);
});

test('C06-01.M04.03 real Chromium page-scale 200 percent keyboard use', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 800 });
  await page.goto('/conoce-solqaryn');
  const cdp = await page.context().newCDPSession(page);
  try {
    await cdp.send('Emulation.setPageScaleFactor', { pageScaleFactor: 2 });
    const scale = await page.evaluate(() => window.visualViewport?.scale ?? 1);
    expect(scale, 'real visual viewport scale, not a CSS zoom mock').toBeGreaterThanOrEqual(1.8);
    const toggle = page.getByTestId('c0601-brand-toggle');
    for (let n = 0; n < 7; n++) {
      if (await toggle.evaluate(element => element === document.activeElement)) break;
      await page.keyboard.press('Tab');
    }
    await expect(toggle).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.getByTestId('c0601-public-saas-landing')).toHaveAttribute('data-brand-theme', 'contraste');
    // Before/after captures at five viewport widths are in the preceding browser test.
    // Do not use Playwright's pointer auto-scroll or a full-page screenshot while
    // CDP pinch scale 2 clips the visual viewport: both can hang on Chromium CI.
  } finally {
    if (!page.isClosed()) {
      try { await cdp.send('Emulation.setPageScaleFactor', { pageScaleFactor: 1 }); } catch {}
    }
    try { await cdp.detach(); } catch {}
  }
});
