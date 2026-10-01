import { test, expect, Page } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

const routes = [
  '/dashboard',
  '/productos',
  '/categorias',
  '/colores',
  '/tallas',
  '/marcas',
  '/modelos',
  '/compras',
  '/proveedores',
  '/ventas',
  '/clientes',
  '/finanzas',
  '/inventario/movimientos',
  '/usuarios',
  '/roles',
  '/permisos',
  '/descuentos',
  '/impuestos',
  '/auditoria',
  '/configuracion',
  '/perfil'
];

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function documentOverflow(page: Page): Promise<number> {
  return page.evaluate(() => {
    const width = Math.max(document.documentElement.scrollWidth, document.body?.scrollWidth ?? 0);
    return width - document.documentElement.clientWidth;
  });
}

async function assertVisibleIcons(page: Page): Promise<void> {
  const result = await page.locator('mat-icon:visible, .material-icons:visible').evaluateAll((icons) => icons.slice(0, 30).map((icon) => {
    const style = getComputedStyle(icon);
    const rect = icon.getBoundingClientRect();
    return {
      color: style.color,
      opacity: Number(style.opacity),
      visibility: style.visibility,
      width: rect.width,
      height: rect.height
    };
  }));

  expect(result.length).toBeGreaterThan(0);
  for (const icon of result) {
    expect(icon.visibility).toBe('visible');
    expect(icon.opacity).toBeGreaterThan(0.5);
    expect(icon.width).toBeGreaterThan(0);
    expect(icon.height).toBeGreaterThan(0);
    expect(icon.color).not.toBe('rgba(0, 0, 0, 0)');
  }
}

async function applyPalette(page: Page, palette: Record<string, string>): Promise<void> {
  await page.evaluate((values) => {
    const root = document.documentElement;
    Object.entries(values).forEach(([name, value]) => root.style.setProperty(name, value));
  }, palette);
  await page.waitForTimeout(100);
}

async function contrastAudit(page: Page): Promise<Array<{ name: string; ratio: number }>> {
  return page.evaluate(() => {
    function rgb(value: string): [number, number, number] {
      const srgb = value.match(/color\(srgb\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)/i);
      if (srgb) {
        return [Number(srgb[1]) * 255, Number(srgb[2]) * 255, Number(srgb[3]) * 255];
      }

      const rgbFunction = value.match(/rgba?\(\s*([\d.]+)[,\s]+([\d.]+)[,\s]+([\d.]+)/i);
      if (rgbFunction) {
        return [Number(rgbFunction[1]), Number(rgbFunction[2]), Number(rgbFunction[3])];
      }

      const hex = value.trim().match(/^#([0-9a-f]{6})$/i);
      if (hex) {
        return [
          Number.parseInt(hex[1].slice(0, 2), 16),
          Number.parseInt(hex[1].slice(2, 4), 16),
          Number.parseInt(hex[1].slice(4, 6), 16)
        ];
      }

      return [0, 0, 0];
    }

    function luminance(value: string): number {
      const channels = rgb(value).map((channel) => {
        const normalized = channel / 255;
        return normalized <= 0.03928
          ? normalized / 12.92
          : Math.pow((normalized + 0.055) / 1.055, 2.4);
      });
      return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
    }

    function ratio(foreground: string, background: string): number {
      const first = luminance(foreground);
      const second = luminance(background);
      return (Math.max(first, second) + 0.05) / (Math.min(first, second) + 0.05);
    }

    function isTransparent(background: string): boolean {
      return background === 'transparent'
        || background === 'rgba(0, 0, 0, 0)'
        || /\/\s*0\s*\)$/.test(background);
    }

    function opaqueBackground(element: Element | null): string {
      let current: Element | null = element;
      while (current) {
        const background = getComputedStyle(current).backgroundColor;
        if (!isTransparent(background)) return background;
        current = current.parentElement;
      }
      return getComputedStyle(document.body).backgroundColor;
    }

    const candidates: Array<{ name: string; selector: string; minimum: number }> = [
      { name: 'título principal', selector: '.page-title, #dashboard-title', minimum: 4.5 },
      { name: 'enlace del menú', selector: '.sidebar nav a', minimum: 3 },
      { name: 'perfil', selector: '.profile-button', minimum: 3 },
      { name: 'icono cerrar sesión', selector: '.topbar-icon-button .material-icons', minimum: 3 },
      { name: 'botón principal', selector: '.mat-mdc-unelevated-button', minimum: 3 }
    ];

    return candidates.flatMap((candidate) => {
      const element = document.querySelector(candidate.selector);
      if (!element) return [];
      const style = getComputedStyle(element);
      return [{
        name: `${candidate.name}|${candidate.minimum}`,
        ratio: ratio(style.color, opaqueBackground(element))
      }];
    });
  });
}

const lightPalette = {
  '--color-primary': '#2563EB',
  '--color-primary-dark': '#1D4ED8',
  '--color-accent': '#D97706',
  '--color-bg': '#F8FAFC',
  '--color-surface': '#FFFFFF',
  '--color-sidebar': '#0F172A',
  '--color-topbar': '#FFFFFF',
  '--color-heading': '#0F172A',
  '--color-button': '#1D4ED8',
  '--color-text': '#111827',
  '--color-text-muted': '#475569',
  '--color-success': '#15803D',
  '--color-warning': '#A16207',
  '--color-danger': '#B91C1C',
  '--color-info': '#0369A1'
};

const darkPalette = {
  '--color-primary': '#60A5FA',
  '--color-primary-dark': '#3B82F6',
  '--color-accent': '#FBBF24',
  '--color-bg': '#0B1120',
  '--color-surface': '#111827',
  '--color-sidebar': '#020617',
  '--color-topbar': '#111827',
  '--color-heading': '#F8FAFC',
  '--color-button': '#2563EB',
  '--color-text': '#F8FAFC',
  '--color-text-muted': '#CBD5E1',
  '--color-success': '#4ADE80',
  '--color-warning': '#FACC15',
  '--color-danger': '#F87171',
  '--color-info': '#38BDF8'
};

test.describe('Matriz de módulos, consola, responsive y contraste', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test('todos los módulos administrativos navegan sin errores de JavaScript', async ({ page }) => {
    const consoleErrors: string[] = [];
    const pageErrors: string[] = [];
    page.on('console', (message) => {
      if (message.type() === 'error') consoleErrors.push(message.text());
    });
    page.on('pageerror', (error) => pageErrors.push(error.message));

    await login(page);
    for (const route of routes) {
      await page.goto(route);
      await expect(page).toHaveURL(new RegExp(`${route.replace('/', '\\/')}$`));
      await expect(page.locator('main#main-content')).toBeVisible();
      await expect(page.locator('h1').first()).toBeVisible();
      await assertVisibleIcons(page);
      expect(await documentOverflow(page), `Desbordamiento horizontal en ${route}`).toBeLessThanOrEqual(2);
    }

    expect(pageErrors, `Errores de ejecución: ${pageErrors.join(' | ')}`).toEqual([]);
    expect(consoleErrors, `Errores de consola: ${consoleErrors.join(' | ')}`).toEqual([]);
  });

  test('las pantallas principales se mantienen utilizables en teléfono', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await login(page);

    for (const route of ['/dashboard', '/productos', '/categorias', '/compras', '/ventas', '/finanzas', '/perfil']) {
      await page.goto(route);
      await expect(page.locator('h1').first()).toBeVisible();
      expect(await documentOverflow(page), `Desbordamiento móvil en ${route}`).toBeLessThanOrEqual(2);
    }
  });

  for (const [name, palette] of [['claro', lightPalette], ['oscuro', darkPalette]] as const) {
    test(`el tema ${name} mantiene texto, botones e iconos con contraste`, async ({ page }) => {
      await login(page);
      await page.goto('/categorias');
      await applyPalette(page, palette);
      await assertVisibleIcons(page);

      const results = await contrastAudit(page);
      expect(results.length).toBeGreaterThanOrEqual(4);
      for (const result of results) {
        const [label, minimumText] = result.name.split('|');
        expect(result.ratio, `Contraste insuficiente en ${label} para tema ${name}`).toBeGreaterThanOrEqual(Number(minimumText));
      }
    });
  }
});
