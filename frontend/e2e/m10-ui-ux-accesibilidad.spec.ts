import { test, expect, Page } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const EMPRESA_ID = process.env['PHASE7_EMPRESA_ID'];

async function completarSeleccionTenantSiAplica(page: Page): Promise<void> {
  const selector = page.getByLabel('ID de empresa');
  try {
    await selector.waitFor({ state: 'visible', timeout: 5_000 });
  } catch {
    return;
  }

  if (!EMPRESA_ID) throw new Error('PHASE7_EMPRESA_ID es obligatorio cuando el login requiere seleccionar empresa.');
  if (new URL(page.url()).pathname !== '/login') return;

  try {
    await selector.fill(EMPRESA_ID, { timeout: 5_000 });
    await page.getByRole('button', { name: 'Entrar a la empresa', exact: true }).click({ timeout: 5_000 });
  } catch (error) {
    // LoginComponent puede revalidar automáticamente una selección persistida y
    // navegar mientras este helper observa el gate. Si el servidor ya validó el
    // tenant y la app salió de /login, la desaparición del selector es esperada.
    try {
      await page.waitForURL(url => url.pathname !== '/login', { timeout: 2_000 });
      return;
    } catch {
      throw error;
    }
  }
}

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await completarSeleccionTenantSiAplica(page);
  await page.waitForURL(url => url.pathname !== '/login', { timeout: 20_000 });
}

function parseCssColor(value: string): [number, number, number] | null {
  const hex = value.trim().match(/^#([0-9a-f]{6})$/i);
  if (hex) {
    return [
      Number.parseInt(hex[1].slice(0, 2), 16),
      Number.parseInt(hex[1].slice(2, 4), 16),
      Number.parseInt(hex[1].slice(4, 6), 16)
    ];
  }

  const rgb = value.trim().match(/^rgba?\(\s*(\d+(?:\.\d+)?)\s*,?\s*(\d+(?:\.\d+)?)\s*,?\s*(\d+(?:\.\d+)?)/i);
  if (!rgb) return null;
  return [Number(rgb[1]), Number(rgb[2]), Number(rgb[3])];
}

function luminance(rgb: [number, number, number]): number {
  const channel = (value: number): number => {
    const normalized = value / 255;
    return normalized <= 0.04045
      ? normalized / 12.92
      : Math.pow((normalized + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * channel(rgb[0]) + 0.7152 * channel(rgb[1]) + 0.0722 * channel(rgb[2]);
}

function contrast(foreground: string, background: string): number {
  const fg = parseCssColor(foreground);
  const bg = parseCssColor(background);
  if (!fg || !bg) throw new Error(`Color no parseable: '${foreground}' / '${background}'`);
  const a = luminance(fg);
  const b = luminance(bg);
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
}

function maxDurationMs(value: string): number {
  if (!value.trim()) return 0;
  return Math.max(...value.split(',').map(item => {
    const normalized = item.trim().toLowerCase();
    if (normalized.endsWith('ms')) return Number.parseFloat(normalized);
    if (normalized.endsWith('s')) return Number.parseFloat(normalized) * 1000;
    return Number.POSITIVE_INFINITY;
  }));
}

async function cssVariables(page: Page, names: string[]): Promise<Record<string, string>> {
  return page.evaluate((requested) => {
    const styles = getComputedStyle(document.documentElement);
    return Object.fromEntries(requested.map(name => [name, styles.getPropertyValue(name).trim()]));
  }, names);
}

test.describe('M10 — UI/UX empresarial y accesibilidad', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test('publica design tokens semánticos y contraste WCAG AA en el tema aplicado', async ({ page }) => {
    await login(page);
    await page.goto('/dashboard');

    const variables = await cssVariables(page, [
      '--font-sans',
      '--font-size-display',
      '--space-4',
      '--radius-lg',
      '--target-min',
      '--motion-normal',
      '--color-text',
      '--color-text-muted',
      '--color-heading',
      '--color-surface',
      '--color-button',
      '--color-on-primary',
      '--color-sidebar',
      '--color-on-sidebar'
    ]);

    expect(variables['--font-sans']).not.toBe('');
    expect(variables['--font-size-display']).not.toBe('');
    expect(variables['--space-4']).not.toBe('');
    expect(variables['--radius-lg']).not.toBe('');
    expect(variables['--target-min']).toBe('44px');
    expect(variables['--motion-normal']).not.toBe('');

    expect(contrast(variables['--color-text'], variables['--color-surface'])).toBeGreaterThanOrEqual(4.5);
    expect(contrast(variables['--color-text-muted'], variables['--color-surface'])).toBeGreaterThanOrEqual(4.5);
    expect(contrast(variables['--color-heading'], variables['--color-surface'])).toBeGreaterThanOrEqual(4.5);
    expect(contrast(variables['--color-on-primary'], variables['--color-button'])).toBeGreaterThanOrEqual(4.5);
    expect(contrast(variables['--color-on-sidebar'], variables['--color-sidebar'])).toBeGreaterThanOrEqual(4.5);
  });

  test('la navegación por teclado usa skip-link, foco visible y targets táctiles mínimos', async ({ page }) => {
    await login(page);
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/dashboard(?:$|[?#])/);
    // La aplicación traslada el foco al <main> después de NavigationEnd;
    // esperar a que termine evita que el autofocus gane una carrera al
    // centinela de teclado sin relajar la verificación de foco ni WCAG.
    await expect(page.locator('#main-content')).toBeFocused();

    const skip = page.getByRole('link', { name: 'Saltar al contenido principal' });

    // El foco inicial del navegador después de una navegación no está garantizado
    // entre runners. Un sentinel E2E previo a app-root fija un origen determinista
    // sin alterar el código productivo ni usar retries.
    await page.evaluate(() => {
      document.getElementById('e2e-m10-focus-sentinel')?.remove();
      const sentinel = document.createElement('button');
      sentinel.id = 'e2e-m10-focus-sentinel';
      sentinel.type = 'button';
      sentinel.textContent = 'Inicio de navegación M10 E2E';
      sentinel.style.position = 'fixed';
      sentinel.style.left = '-10000px';
      document.body.insertBefore(sentinel, document.body.firstChild);
    });
    const focusSentinel = page.locator('#e2e-m10-focus-sentinel');
    await focusSentinel.focus();
    await expect(focusSentinel).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(skip).toBeFocused();
    await expect(skip).toBeVisible();
    await page.keyboard.press('Enter');
    await expect(page.locator('#main-content')).toBeFocused();

    const targetAudit = await page.evaluate(() => {
      const nav = document.querySelector('aside[aria-label="Menú principal"] nav[aria-label="Navegación principal"]');
      const interactive = Array.from(document.querySelectorAll<HTMLElement>(
        'button:not([disabled]), a[role="button"], .mat-mdc-button-base:not([disabled])'
      )).filter(element => {
        const style = getComputedStyle(element);
        const rect = element.getBoundingClientRect();
        return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
      });
      const tooSmall = interactive
        .map(element => ({
          html: element.outerHTML.slice(0, 160),
          width: Math.round(element.getBoundingClientRect().width),
          height: Math.round(element.getBoundingClientRect().height)
        }))
        .filter(item => item.height < 44);
      return {
        mainCount: document.querySelectorAll('main#main-content').length,
        navExists: Boolean(nav),
        tooSmall
      };
    });

    expect(targetAudit.mainCount).toBe(1);
    expect(targetAudit.navExists).toBe(true);
    expect(targetAudit.tooSmall).toEqual([]);
  });

  test('el drawer móvil captura y devuelve el foco al activador', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await login(page);
    await page.goto('/dashboard');

    const toggle = page.getByRole('button', { name: 'Abrir menú principal' });
    await expect(toggle).toBeVisible();
    await toggle.focus();
    await page.keyboard.press('Enter');

    await expect(page.locator('#menu-toggle')).toHaveAttribute('aria-expanded', 'true');
    const close = page.getByRole('button', { name: 'Cerrar menú', exact: true });
    await expect(close).toBeFocused();

    await page.keyboard.press('Escape');
    await expect(page.locator('#menu-toggle')).toHaveAttribute('aria-expanded', 'false');
    await expect(page.locator('#menu-toggle')).toBeFocused();
  });

  test('los controles visibles de rutas críticas conservan nombre accesible', async ({ page }) => {
    await login(page);
    const routes = ['/dashboard', '/productos', '/ventas', '/cargas-masivas', '/configuracion'];

    for (const route of routes) {
      await page.goto(route);
      await page.waitForLoadState('networkidle');
      const unnamed = await page.evaluate(() => {
        const visible = (element: HTMLElement): boolean => {
          const style = getComputedStyle(element);
          const rect = element.getBoundingClientRect();
          return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
        };

        return Array.from(document.querySelectorAll<HTMLElement>(
          'button, input:not([type="hidden"]), select, textarea, [role="button"]'
        ))
          .filter(visible)
          .filter(control => {
            if (control.getAttribute('aria-label')?.trim()) return false;
            const labelledBy = control.getAttribute('aria-labelledby')?.trim();
            if (labelledBy && labelledBy.split(/\s+/).some(id => document.getElementById(id)?.textContent?.trim())) return false;
            if (control.textContent?.trim()) return false;
            if (control.getAttribute('title')?.trim()) return false;
            if (control.closest('label')?.textContent?.trim()) return false;
            if (control.closest('mat-form-field')?.querySelector('mat-label')?.textContent?.trim()) return false;
            const id = control.id;
            if (id && document.querySelector(`label[for="${CSS.escape(id)}"]`)?.textContent?.trim()) return false;
            const materialHost = control.closest<HTMLElement>('mat-slide-toggle, mat-checkbox, mat-radio-button');
            if (materialHost?.getAttribute('aria-label')?.trim() || materialHost?.textContent?.trim()) return false;
            return true;
          })
          .map(control => control.outerHTML.slice(0, 180));
      });

      expect(unnamed, `Controles sin nombre accesible en ${route}`).toEqual([]);
    }
  });

  test('320px y 390px no presentan desbordamiento horizontal del documento', async ({ page }) => {
    await login(page);
    const viewports = [
      { width: 320, height: 700 },
      { width: 390, height: 844 }
    ];
    const routes = ['/dashboard', '/productos', '/cargas-masivas'];

    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      for (const route of routes) {
        await page.goto(route);
        await page.waitForLoadState('networkidle');
        const layout = await page.evaluate(() => ({
          viewport: document.documentElement.clientWidth,
          documentWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
          mainVisible: Boolean(document.querySelector('main#main-content'))
        }));
        expect(layout.mainVisible).toBe(true);
        expect(layout.documentWidth - layout.viewport, `${route} @ ${viewport.width}px`).toBeLessThanOrEqual(1);
      }
    }
  });

  test('prefers-reduced-motion reduce elimina movimiento no esencial', async ({ page }) => {
    await page.emulateMedia({ reducedMotion: 'reduce' });
    await login(page);
    await page.goto('/dashboard');

    const reduced = await page.evaluate(() => {
      const root = getComputedStyle(document.documentElement);
      const button = document.querySelector<HTMLElement>('.profile-button, .topbar-icon-button, button');
      const style = button ? getComputedStyle(button) : null;
      return {
        token: root.getPropertyValue('--motion-normal').trim(),
        transitionDuration: style?.transitionDuration ?? '',
        animationDuration: style?.animationDuration ?? ''
      };
    });

    expect(reduced.token).toBe('0.01ms');
    expect(maxDurationMs(reduced.transitionDuration)).toBeLessThanOrEqual(0.1);
    expect(maxDurationMs(reduced.animationDuration)).toBeLessThanOrEqual(0.1);
  });
});
