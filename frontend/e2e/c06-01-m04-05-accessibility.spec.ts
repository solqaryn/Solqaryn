import { expect, test } from '@playwright/test';

// C06-01.M04.05: true Angular route + Chromium, no API or auth mocks.
// Negative assertions must fail on overflow, missing names, focus or contrast.
const sizes = [320, 375, 768, 1440];
function contrast(fg: string, bg: string): number {
  const luminance = (css: string): number => {
    const channels = css.match(/[\d.]+/g)?.slice(0, 3).map(Number);
    if (!channels || channels.length !== 3) throw new Error('Bad computed color ' + css);
    const [r, g, b] = channels.map(channel => {
      const v = channel / 255;
      return v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
    });
    return r * 0.2126 + g * 0.7152 + b * 0.0722;
  };
  const a = luminance(fg), b = luminance(bg);
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
}
test('C06-01.M04.05 responsive boundaries have no overflow', async ({ page }) => {
  for (const width of sizes) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/conoce-solqaryn');
    await expect(page.getByTestId('c0601-public-saas-landing')).toBeVisible();
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    const geometry = await page.evaluate(() => ({
      viewport: window.innerWidth,
      total: document.documentElement.scrollWidth
    }));
    expect(geometry.viewport).toBe(width);
    expect(geometry.total, 'overflow at ' + width).toBeLessThanOrEqual(width + 1);
  }
});
test('C06-01.M04.05 labeled actions, landmarks and no anonymous forms', async ({ page }) => {
  await page.goto('/conoce-solqaryn');
  const root = page.locator('app-c0601-public-saas-landing');
  await expect(root.getByRole('main')).toHaveCount(1);
  await expect(root.getByRole('heading', { level: 1 })).toHaveCount(1);
  await expect(root.getByRole('navigation', { name: 'Navegación de presentación' })).toBeVisible();
  await expect(root.getByRole('link', { name: /Ir a mi cuenta/ })).toHaveAttribute('href', '/login');
  const unnamed = await root.locator('a,button').evaluateAll(nodes =>
    nodes.filter(node => !(node.getAttribute('aria-label') || node.textContent || '').trim())
      .map(node => node.outerHTML.slice(0, 120))
  );
  expect(unnamed, 'unlabeled actions are rejected').toEqual([]);
  await expect(root.locator('form,input,textarea,select,iframe')).toHaveCount(0);
});
test('C06-01.M04.05 real Tab and Enter retain route and visible focus', async ({ page }) => {
  await page.goto('/conoce-solqaryn');
  await page.keyboard.press('Tab');
  const skip = page.getByRole('link', { name: 'Ir al contenido' });
  await expect(skip).toBeFocused();
  await expect(skip).toBeVisible();
  const outline = await skip.evaluate(el => ({
    style: getComputedStyle(el).outlineStyle,
    width: getComputedStyle(el).outlineWidth
  }));
  expect(outline.style).not.toBe('none');
  expect(parseFloat(outline.width)).toBeGreaterThanOrEqual(2);
  await page.keyboard.press('Enter');
  await expect(page.locator('#c0601-content')).toBeFocused();
  await expect(page).toHaveURL(/\/conoce-solqaryn(?:#.*)?$/);
});
test('C06-01.M04.05 computed readable text contrast >= 4.5 WCAG AA', async ({ page }) => {
  await page.goto('/conoce-solqaryn');
  const selectors = [
    '.site-header nav a:first-child', '.hero .lede', '.hero .eyebrow',
    '.hero-aside .aside-note', '.section .section-caption',
    '.benefit-grid article p', '.closing p', '.closing .closing-link', 'footer'
  ];
  for (const selector of selectors) {
    const pair = await page.locator('app-c0601-public-saas-landing').locator(selector).first()
      .evaluate(element => {
        let node: Element | null = element;
        let background = 'rgb(255, 255, 255)';
        while (node) {
          const color = getComputedStyle(node).backgroundColor;
          if (/^rgb\(/.test(color)) { background = color; break; }
          node = node.parentElement;
        }
        return { foreground: getComputedStyle(element).color, background };
      });
    expect(contrast(pair.foreground, pair.background), selector).toBeGreaterThanOrEqual(4.5);
  }
});
