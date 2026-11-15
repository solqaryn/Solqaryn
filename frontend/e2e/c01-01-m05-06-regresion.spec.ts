import { expect, test } from '@playwright/test';

/**
 * C01-01.M05.06: regresión accesible de entrada sin privilegios ni servicios live.
 * La UI sólo presenta la interfaz: auth/tenancy real permanece en el backend.
 */
test.describe('V5 C01-01.M05.06 regresión accesibilidad y guard', () => {
  test.describe.configure({ retries: 0 });

  test('movil con movimiento reducido mantiene foco y etiquetas utilizables', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.emulateMedia({ reducedMotion: 'reduce' });
    await page.goto('/login');

    const usuario = page.getByRole('textbox', { name: 'Nombre de usuario' });
    const clave = page.getByLabel('Contraseña', { exact: true });
    const enviar = page.getByRole('button', { name: 'Ingresar al sistema', exact: true });
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    await expect(usuario).toBeVisible();
    await expect(clave).toBeVisible();
    await expect(enviar).toBeDisabled();

    await usuario.focus();
    await expect(usuario).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(clave).toBeFocused();
    expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);

    const ancho = await page.evaluate(() => ({
      disponible: document.documentElement.clientWidth,
      contenido: document.documentElement.scrollWidth
    }));
    expect(ancho.contenido).toBeLessThanOrEqual(ancho.disponible + 2);
  });

  test('regresion de ruta privada niega anonimo y conserva semantica al volver', async ({ page }) => {
    await page.goto('/usuarios');
    await expect(page).toHaveURL(/\/login(?:$|[?#])/);
    await expect(page.getByRole('textbox', { name: 'Nombre de usuario' })).toBeVisible();
    await expect(page.getByLabel('Contraseña', { exact: true })).toBeVisible();
    await expect(page.locator('app-usuarios')).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Ingresar al sistema', exact: true })).toBeDisabled();
  });
});
