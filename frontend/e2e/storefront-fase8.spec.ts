import { expect, Page, test } from '@playwright/test';

const empresaBase = {
  id: 908,
  nombreComercial: 'Storefront Fase 8',
  nombreVisibleSistema: 'Storefront Fase 8',
  eslogan: 'Encuentra lo que necesitas',
  descripcionSistema: 'Tienda pública de auditoría',
  mensajeLogin: 'Administración',
  copyright: '© 2026 Storefront Fase 8',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Catálogo público',
  piePaginaActivo: true,
  piePaginaTexto: 'Catálogo público',
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy',
  whatsApp: '9876-5432'
};

async function prepararEmpresa(page: Page): Promise<void> {
  await page.route('http://localhost:5005/empresa-configuracion/publica', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: empresaBase })
    });
  });
}

test.describe('Storefront Fase 8 — búsqueda, filtros y ordenamiento', () => {
  test.describe.configure({ retries: 0 });

  test('URL compartible hidrata y combina búsqueda, categoría, disponibilidad, rango y orden', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/tienda/productos?q=audifonos&categoria=demo-categoria-2&disponible=1&precioMin=700&precioMax=1600&orden=precio-desc');

    await expect(page.getByRole('status').filter({ hasText: '2 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(2);
    await expect(page.locator('article.product-card').nth(0)).toContainText('Audífonos Wireless Studio');
    await expect(page.locator('article.product-card').nth(1)).toContainText('Audífonos Travel');
    await expect(page.locator('app-storefront-header').getByRole('searchbox')).toHaveValue('audifonos');
    await expect(page.getByRole('checkbox', { name: 'Solo disponibles' })).toBeChecked();
    await expect(page.getByRole('spinbutton', { name: 'Precio mínimo' })).toHaveValue('700');
    await expect(page.getByRole('spinbutton', { name: 'Precio máximo' })).toHaveValue('1600');
    await expect(page.getByLabel('Ordenar por')).toHaveValue('precio-desc');

    await page.reload();
    await expect(page.getByRole('status').filter({ hasText: '2 productos encontrados' })).toBeVisible();
    await expect(page.getByRole('checkbox', { name: 'Solo disponibles' })).toBeChecked();
    await expect(page.getByLabel('Ordenar por')).toHaveValue('precio-desc');
  });

  test('búsqueda ignora acentos/mayúsculas y conserva q al confirmar', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/tienda/productos');
    const search = page.locator('app-storefront-header').getByRole('searchbox', { name: 'Buscar productos, marcas o modelos' });
    await search.fill('AUDIFONOS');
    await expect(page.getByRole('status').filter({ hasText: '2 productos encontrados' })).toBeVisible();
    await search.press('Enter');
    await expect(page).toHaveURL(/q=AUDIFONOS/);
  });

  test('paginación y orden permanecen en URL y sobreviven recarga', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/tienda/productos?orden=nombre&pagina=2');
    await expect(page.getByText('Página', { exact: false }).filter({ hasText: '2 de 2' })).toBeVisible();
    await expect(page.getByLabel('Ordenar por')).toHaveValue('nombre');
    await expect(page.locator('article.product-card')).toHaveCount(2);
    await page.reload();
    await expect(page.getByText('Página', { exact: false }).filter({ hasText: '2 de 2' })).toBeVisible();
    await expect(page).toHaveURL(/orden=nombre.*pagina=2|pagina=2.*orden=nombre/);
  });

  test('URL con página fuera de rango se autocorrige sin dejar el grid vacío', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/tienda/productos?categoria=demo-categoria-2&orden=nombre&pagina=999');

    await expect(page.getByRole('status').filter({ hasText: '3 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(3);
    await expect(page.getByText('Página', { exact: false }).filter({ hasText: '1 de 1' })).toHaveCount(0);
    await expect(page).toHaveURL(/categoria=demo-categoria-2/);
    await expect(page).toHaveURL(/orden=nombre/);
    await expect(page).not.toHaveURL(/pagina=999/);
  });

  test('orden legacy destacados se normaliza a relevancia y limpia la URL', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/tienda/productos?orden=destacados');

    await expect(page.getByLabel('Ordenar por')).toHaveValue('relevancia');
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();
    await expect(page).not.toHaveURL(/orden=destacados/);
  });

  test('sin resultados ofrece limpiar filtros y recupera URL canónica', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/tienda/productos?q=no-existe&precioMin=99999&disponible=1');
    await expect(page.getByRole('heading', { name: 'No encontramos coincidencias' })).toBeVisible();
    await page.getByRole('button', { name: 'Limpiar filtros' }).click();
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();
    await expect(page).toHaveURL(/\/storefront\/productos$/);
  });

  test('móvil mantiene filtros colapsables, acotados y sin overflow horizontal', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 360, height: 740 });
    await page.goto('/tienda/productos');
    const boton = page.getByRole('button', { name: 'Filtros', exact: true });
    await expect(boton).toHaveAttribute('aria-expanded', 'false');
    await boton.click();
    await expect(boton).toHaveAttribute('aria-expanded', 'true');
    const filtros = page.locator('aside.filters');
    await expect(filtros).toBeVisible();
    const estilo = await filtros.evaluate(element => ({
      maxHeight: getComputedStyle(element).maxHeight,
      overflowY: getComputedStyle(element).overflowY
    }));
    expect(estilo.maxHeight).not.toBe('none');
    expect(['auto', 'scroll']).toContain(estilo.overflowY);
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    expect(overflow).toBeLessThanOrEqual(0);
  });
});
