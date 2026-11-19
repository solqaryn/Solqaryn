import { expect, test } from '@playwright/test';

// C02-01.M04.05: navegador REAL con Angular local, sin JWT/API/MySQL reales.
// Ninguna comprobación de este archivo certifica pertenencia o autorización del backend.
test.describe('V20 C02-01.M04.05 - responsive, teclado, foco, labels y contraste', () => {
  test.describe.configure({ retries: 0 });

  test('320/375/768/1280: acceso legible y controles dentro del viewport', async ({ page }) => {
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 850 });
      await page.goto('/login');
      await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
      const controls = [
        page.getByRole('textbox', { name: 'Nombre de usuario' }),
        page.getByLabel('Contraseña', { exact: true }),
        page.getByRole('button', { name: 'Ingresar al sistema', exact: true })
      ];
      for (const control of controls) {
        await expect(control).toBeVisible();
        const box = await control.boundingBox();
        expect(box, 'el control debe tener geometría visible').not.toBeNull();
        expect(box!.x).toBeGreaterThanOrEqual(-2);
        expect(box!.x + box!.width).toBeLessThanOrEqual(width + 2);
      }
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth
      );
      expect(overflow, 'no debe haber scroll horizontal a ninguna anchura').toBeLessThanOrEqual(2);
    }
  });

  test('Tab natural, sin .focus(), alcanza usuario, clave y mostrar contraseña', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 820 });
    await page.goto('/login');
    const usuario = page.getByRole('textbox', { name: 'Nombre de usuario' });
    const clave = page.getByLabel('Contraseña', { exact: true });
    const mostrar = page.getByRole('button', { name: 'Mostrar contraseña' });
    let alcanzado = false;
    for (let i = 0; i < 80; i++) {
      await page.keyboard.press('Tab');
      if (await usuario.evaluate(el => document.activeElement === el)) {
        alcanzado = true;
        break;
      }
    }
    expect(alcanzado, 'el campo usuario debe alcanzarse desde el inicio natural').toBe(true);
    await page.keyboard.press('Tab');
    await expect(clave).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(mostrar).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.getByRole('button', { name: 'Ocultar contraseña' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Ingresar al sistema', exact: true })).toBeDisabled();
    await expect(page).toHaveURL(/\/login(?:$|[?#])/);
  });

  test('WCAG AA: contraste efectivo de encabezado y labels con fondos transparentes', async ({ page }) => {
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 850 });
      await page.goto('/login');
      const targets = [
        page.getByRole('heading', { name: 'Iniciar sesión' }),
        page.locator('mat-label').filter({ hasText: 'Nombre de usuario' }),
        page.locator('mat-label').filter({ hasText: 'Contraseña' })
      ];
      for (const target of targets) {
        await expect(target).toBeVisible();
        const ratio = await target.evaluate(element => {
          // Los paneles/material labels pueden tener backgroundColor transparente.
          // Medir solo el fondo inmediato daria un PASS falso. Componer ancestros.
          const canvas = document.createElement('canvas');
          canvas.width = canvas.height = 1;
          const context = canvas.getContext('2d');
          if (!context) throw new Error('Canvas no disponible para verificar WCAG');
          const pixel = (color: string): number[] => {
            context.clearRect(0, 0, 1, 1);
            context.fillStyle = color;
            context.fillRect(0, 0, 1, 1);
            return Array.from(context.getImageData(0, 0, 1, 1).data);
          };
          const layers: number[][] = [];
          for (let current: Element | null = element; current; current = current.parentElement) {
            layers.unshift(pixel(getComputedStyle(current).backgroundColor));
          }
          let background = [255, 255, 255];
          for (const layer of layers) {
            const [r = 0, g = 0, b = 0, alpha = 0] = layer;
            const a = alpha / 255;
            background = [
              Math.round(r * a + (background[0] ?? 255) * (1 - a)),
              Math.round(g * a + (background[1] ?? 255) * (1 - a)),
              Math.round(b * a + (background[2] ?? 255) * (1 - a))
            ];
          }
          const [r = 0, g = 0, b = 0, alpha = 0] = pixel(getComputedStyle(element).color);
          const a = alpha / 255;
          const foreground = [
            Math.round(r * a + (background[0] ?? 255) * (1 - a)),
            Math.round(g * a + (background[1] ?? 255) * (1 - a)),
            Math.round(b * a + (background[2] ?? 255) * (1 - a))
          ];
          const luminance = (channels: number[]) =>
            channels.reduce((sum, channel, index) => {
              const s = channel / 255;
              const linear = s <= 0.04045 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
              return sum + linear * ([0.2126, 0.7152, 0.0722][index] ?? 0);
            }, 0);
          const fore = luminance(foreground);
          const back = luminance(background);
          return (Math.max(fore, back) + 0.05) / (Math.min(fore, back) + 0.05);
        });
        expect(ratio, 'Contraste WCAG AA del texto sobre el fondo real compuesto').toBeGreaterThanOrEqual(4.5);
      }
    }
  });

  test('hint tenant manipulado no concede acceso anónimo ni menú de cambio empresarial', async ({ page }) => {
    await page.addInitScript(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '999'));
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login(?:$|[?#])/);
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    await expect(page.getByRole('button', { name: /Cambiar empresa/i })).toHaveCount(0);
    await expect(page.locator('main#main-content')).toHaveCount(0);
    await expect(page.getByText('Ingresos del mes')).toHaveCount(0);
  });
});
