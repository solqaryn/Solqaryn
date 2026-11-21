import { expect, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

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
  const evidenceDir = join(process.cwd(), 'v23-artifacts', 'c0601-m0403');
  await mkdir(evidenceDir, { recursive: true });
  for (const width of [320, 390, 768, 1280, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/conoce-solqaryn');
    await expect(shell).toHaveAttribute('data-brand-theme', 'institucional');
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    const beforePath = join(evidenceDir, 'antes-institucional-' + width + '.png');
    await page.screenshot({ path: beforePath, fullPage: true });
    await test.info().attach('antes-institucional-' + width, { path: beforePath, contentType: 'image/png' });
    await toggle.click();
    await expect(shell).toHaveAttribute('data-brand-theme', 'contraste');
    const afterPath = join(evidenceDir, 'despues-contraste-' + width + '.png');
    await page.screenshot({ path: afterPath, fullPage: true });
    await test.info().attach('despues-contraste-' + width, { path: afterPath, contentType: 'image/png' });
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

test('C06-01.M04.03 high contrast palette computed WCAG AA across public surfaces', async ({ page }) => {
  const surfacePairs = [
    ['.site-header nav a', '.landing-shell'],
    ['.hero .lede', '.landing-shell'],
    ['.hero-aside .aside-note', '.hero-aside'],
    ['.benefit-grid p', '.section'],
    ['.closing p', '.landing-shell']
  ] as const;
  const luminance = (value: string): number => {
    const channels = value.match(/\d+(?:\.\d+)?/g)?.slice(0, 3).map(Number);
    if (!channels || channels.length !== 3) throw new Error('Not a computed RGB color: ' + value);
    const rgb = channels.map(c => {
      const normalized = c / 255;
      return normalized <= 0.04045 ? normalized / 12.92 : ((normalized + 0.055) / 1.055) ** 2.4;
    });
    return rgb[0]! * 0.2126 + rgb[1]! * 0.7152 + rgb[2]! * 0.0722;
  };
  for (const width of [390, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/conoce-solqaryn');
    await page.getByTestId('c0601-brand-toggle').click();
    await expect(page.getByTestId('c0601-public-saas-landing')).toHaveAttribute('data-brand-theme', 'contraste');
    for (const [foreground, background] of surfacePairs) {
      const colors = await page.evaluate(([fg, bg]) => ({
        fg: getComputedStyle(document.querySelector(fg)!).color,
        bg: getComputedStyle(document.querySelector(bg)!).backgroundColor
      }), [foreground, background]);
      const front = luminance(colors.fg), back = luminance(colors.bg);
      const contrast = (Math.max(front, back) + 0.05) / (Math.min(front, back) + 0.05);
      expect(contrast, 'computed WCAG AA contrast for ' + foreground + ' at ' + width).toBeGreaterThanOrEqual(4.5);
    }
  }
});


// V23 DEV05 P1 A5 regression: the global color-heading token must never mask
// headings in either local public brand palette. This failed in ten real PNGs
// despite the preceding 5/5 Chrome tests passing because they omitted headings.
test('C06-01.M04.03 heading contrast adversarial WCAG AA across both palettes', async ({ page }) => {
  const pairs = [
    ['.hero h1', '.landing-shell'],
    ['#benefits-heading', '.section'],
    ['.benefit-grid article:nth-child(1) h3', '.section'],
    ['.benefit-grid article:nth-child(2) h3', '.section'],
    ['.benefit-grid article:nth-child(3) h3', '.section'],
    ['#c0601-status-heading', '.landing-shell'],
    ['#scope-heading', '.landing-shell']
  ] as const;
  const luminosity = (value: string): number => {
    const channels = value.match(/\\d+(?:\\.\\d+)?/g)?.slice(0, 3).map(Number);
    if (!channels || channels.length !== 3) throw new Error('RGB not computed: ' + value);
    const v = channels.map(c => {
      const s = c / 255;
      return s <= 0.04045 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
    });
    return 0.2126 * v[0]! + 0.7152 * v[1]! + 0.0722 * v[2]!;
  };
  for (const width of [390, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    for (const theme of ['institucional', 'contraste'] as const) {
      await page.goto('/conoce-solqaryn');
      if (theme === 'contraste') await page.getByTestId('c0601-brand-toggle').click();
      await expect(page.getByTestId('c0601-public-saas-landing')).toHaveAttribute('data-brand-theme', theme);
      for (const [foreground, background] of pairs) {
        const colors = await page.evaluate(([fg, bg]) => {
          const f = document.querySelector(fg), b = document.querySelector(bg);
          if (!f || !b) throw new Error('Missing contrast landmark ' + fg + '/' + bg);
          return { fg: getComputedStyle(f).color, bg: getComputedStyle(b).backgroundColor };
        }, [foreground, background]);
        const contrast = (Math.max(luminosity(colors.fg), luminosity(colors.bg)) + 0.05) /
          (Math.min(luminosity(colors.fg), luminosity(colors.bg)) + 0.05);
        expect(contrast, `WCAG 4.5 ${theme} ${width} ${foreground}`).toBeGreaterThanOrEqual(4.5);
      }
    }
  }
});
