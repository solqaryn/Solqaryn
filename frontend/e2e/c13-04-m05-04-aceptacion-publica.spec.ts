import { expect, test } from '@playwright/test';

/** E2E de superficies públicas reales. No page.route(), mocks de red ni credenciales.
 * Los servicios externos sin conexión pueden mostrar errores legítimos: no se
 * atribuyen productos/tenants ficticios ni se certifican compras.
 */
test.describe('C13-04.M05.04 aceptación positiva pública sin mocks', () => {
  test.describe.configure({ retries: 0 });

  test('raíz real lleva a la landing pública, acceso por enlace real y validación local de login', async ({ page }) => {
    const response = await page.goto('/');
    expect(response?.ok()).toBe(true);
    await expect(page).toHaveURL(/\/conoce-solqaryn(?:\?|$)/);
    await expect(page.locator('h1#c0601-heading')).toBeVisible();
    const acceso = page.getByRole('link', { name: 'Acceder', exact: true });
    await expect(acceso).toHaveAttribute('href', '/login');
    await acceso.click();
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    const submit = page.getByRole('button', { name: /Ingresar al sistema/ });
    await expect(submit).toBeDisabled();
    await expect(page.getByRole('textbox', { name: 'Nombre de usuario' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Mostrar contraseña' })).toBeVisible();
    await page.getByRole('button', { name: 'Mostrar contraseña' }).click();
    await expect(page.getByRole('button', { name: 'Ocultar contraseña' })).toBeVisible();
  });

  test('navegación de tienda a listado de productos por enlace real', async ({ page }) => {
    const response = await page.goto('/tienda');
    expect(response?.ok()).toBe(true);
    await expect(page.locator('h1#hero-title')).toBeVisible();
    await page.locator('.hero-actions a[href="/tienda/productos"]').click();
    await expect(page).toHaveURL(/\/tienda\/productos(?:\?|$)/);
    await expect(page.locator('main#catalogo-productos h1')).toHaveText('Productos');
    await expect(page.locator('main#catalogo-productos')).toBeVisible();
  });

  test('navegación de tienda a categorías y ofertas públicas sin interceptar HTTP', async ({ page }) => {
    const response = await page.goto('/tienda');
    expect(response?.ok()).toBe(true);
    await expect(page.locator('h1#hero-title')).toBeVisible();
    await page.locator('.hero-actions a[href="/tienda/categorias"]').click();
    await expect(page).toHaveURL(/\/tienda\/categorias(?:\?|$)/);
    await expect(page.locator('main#contenido-categorias h1')).toHaveText('Categorías');
    const ofertas = await page.goto('/tienda/ofertas');
    expect(ofertas?.ok()).toBe(true);
    await expect(page.locator('main#catalogo-productos h1')).toHaveText('Ofertas');
  });

  test('acción visible de tienda abre carrito público sin crear pedido', async ({ page }) => {
    const response = await page.goto('/tienda');
    expect(response?.ok()).toBe(true);
    await expect(page.locator('h1#hero-title')).toBeVisible();
    await page.locator('.hero-checkout button.text-link').click();
    await expect(page).toHaveURL(/\/tienda\/carrito(?:\?|$)/);
    await expect(page.locator('main#carrito-principal h1')).toHaveText('Mi carrito');
  });

  // Guardas reales de Angular con sesión anónima. No se intercepta la red ni se
  // fabrican credenciales. Son NEGATIVOS de navegación, no JWT firmado/API real.
  for (const rutaPrivada of ['/dashboard', '/productos', '/plataforma/inventario-rutas-v19']) {
    test(`sin sesión se rechaza el ingreso directo a ${rutaPrivada}`, async ({ page }) => {
      const response = await page.goto(rutaPrivada);
      expect(response?.ok()).toBe(true);
      await expect(page).toHaveURL(/\/login(?:\?|$)/);
      await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
      expect(page.url()).not.toContain(rutaPrivada);
    });
  }

});
