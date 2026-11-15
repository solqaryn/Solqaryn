import { expect, test } from '@playwright/test';

/** C01-01.M04.05: accesibilidad de la entrada privada sin credenciales ni API de producción. */
test.describe('V5 C01-01.M04.05 responsive, teclado y etiquetas', () => {
  test.describe.configure({ retries: 0 });

  test('móvil 360px conserva campos y acciones dentro del viewport con labels visibles', async ({ page }) => {
    await page.setViewportSize({ width: 360, height: 740 });
    await page.goto('/login');
    const usuario = page.getByRole('textbox', { name: 'Nombre de usuario' });
    const clave = page.getByLabel('Contraseña', { exact: true });
    const enviar = page.getByRole('button', { name: 'Ingresar al sistema', exact: true });

    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    for (const elemento of [usuario, clave, enviar]) {
      await expect(elemento).toBeVisible();
      const box = await elemento.boundingBox();
      expect(box).not.toBeNull();
      expect(box!.x).toBeGreaterThanOrEqual(-1);
      expect(box!.x + box!.width).toBeLessThanOrEqual(361);
    }
    const ancho = await page.evaluate(() => ({
      disponible: document.documentElement.clientWidth,
      contenido: document.documentElement.scrollWidth
    }));
    expect(ancho.contenido).toBeLessThanOrEqual(ancho.disponible + 2);
    await expect(enviar).toBeDisabled();
  });

  test('el teclado recorre nombre, contraseña y visibilidad sin enviar formulario', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/login');
    const usuario = page.getByRole('textbox', { name: 'Nombre de usuario' });
    const clave = page.getByLabel('Contraseña', { exact: true });
    const visibilidad = page.getByRole('button', { name: 'Mostrar contraseña' });
    const enviar = page.getByRole('button', { name: 'Ingresar al sistema', exact: true });

    await usuario.focus();
    await expect(usuario).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(clave).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(visibilidad).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.getByRole('button', { name: 'Ocultar contraseña' })).toBeVisible();
    await expect(enviar).toBeDisabled();
    await expect(page).toHaveURL(/\/login(?:$|[?#])/);
  });

  test('escritorio 1366px conserva contraste legible del título del formulario', async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 768 });
    await page.goto('/login');
    const heading = page.getByRole('heading', { name: 'Iniciar sesión' });
    await expect(heading).toBeVisible();
    const contraste = await heading.evaluate(elemento => {
      const form = elemento.closest('form');
      if (!form) throw new Error('Falta el formulario de acceso');
      const contexto = document.createElement('canvas').getContext('2d');
      if (!contexto) throw new Error('Canvas inaccesible');
      contexto.canvas.width = 1;
      contexto.canvas.height = 1;
      const color = (css: string, fondo = '#fff'): number[] => {
        contexto.fillStyle = fondo;
        contexto.fillRect(0, 0, 1, 1);
        contexto.fillStyle = css;
        contexto.fillRect(0, 0, 1, 1);
        return Array.from(contexto.getImageData(0, 0, 1, 1).data).slice(0, 3);
      };
      const lineal = (v: number): number => {
        const c = v / 255;
        return c <= .04045 ? c / 12.92 : Math.pow((c + .055) / 1.055, 2.4);
      };
      const luminancia = (rgb: number[]) =>
        .2126 * lineal(rgb[0]) + .7152 * lineal(rgb[1]) + .0722 * lineal(rgb[2]);
      const bg = color(getComputedStyle(form).backgroundColor);
      const fg = color(getComputedStyle(elemento).color);
      const [a, b] = [luminancia(bg), luminancia(fg)].sort((x, y) => y - x);
      return (a + .05) / (b + .05);
    });
    expect(contraste).toBeGreaterThanOrEqual(4.5);
  });

  test('la ruta privada niega el acceso anónimo y mantiene el login etiquetado', async ({ page }) => {
    await page.goto('/usuarios');
    await expect(page).toHaveURL(/\/login(?:$|[?#])/);
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Nombre de usuario' })).toBeVisible();
    await expect(page.getByLabel('Contraseña', { exact: true })).toBeVisible();
    await expect(page.locator('app-usuarios')).toHaveCount(0);
  });
});
