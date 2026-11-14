import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const suffix = `${Date.now()}`;

let adminId = 0;
let productName = '';

function authHeaders(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

async function dataOf(response: APIResponse): Promise<any> {
  const payload = await response.json();
  return payload.data ?? payload.Data;
}

async function loginApi(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, {
    data: { nombreUsuario: ADMIN_USERNAME, password: ADMIN_PASSWORD }
  });
  expect(response.status(), await response.text()).toBe(200);
  return (await dataOf(response)).token;
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function createCatalog(
  request: APIRequestContext,
  token: string,
  route: string,
  input: Record<string, unknown>
): Promise<Record<string, any>> {
  const response = await request.post(`${API_URL}/${route}`, {
    headers: authHeaders(token),
    data: input
  });
  expect(response.status(), await response.text()).toBe(201);
  return await dataOf(response);
}

async function assertNoDocumentOverflow(page: Page, route: string): Promise<void> {
  const overflow = await page.evaluate(() => {
    const width = Math.max(document.documentElement.scrollWidth, document.body?.scrollWidth ?? 0);
    return width - document.documentElement.clientWidth;
  });
  expect(overflow, `Desbordamiento horizontal del documento en ${route}`).toBeLessThanOrEqual(2);
}

async function assertCriticalTextFits(page: Page, route: string): Promise<void> {
  const failures = await page.locator([
    '.page-title:visible',
    '.page-subtitle:visible',
    '.info-banner:visible',
    '.section-heading:visible',
    '.gallery-header:visible',
    '.producto-card-info:visible',
    '.mat-mdc-form-field-hint:visible',
    '.mat-mdc-form-field-error:visible',
    '.form-actions .mdc-button__label:visible',
    '.item-card-actions .mdc-button__label:visible',
    '.topbar .user-name:visible',
    '.topbar .user-role:visible'
  ].join(',')).evaluateAll((elements) => elements.flatMap((element) => {
    const html = element as HTMLElement;
    const rect = html.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) return [];

    const viewportWidth = document.documentElement.clientWidth;
    const clippedHorizontally = rect.left < -1 || rect.right > viewportWidth + 1;
    const internalOverflow = html.scrollWidth - html.clientWidth > 2;

    if (!clippedHorizontally && !internalOverflow) return [];
    return [{
      tag: html.tagName,
      className: html.className,
      text: (html.textContent ?? '').trim().slice(0, 120),
      rectLeft: rect.left,
      rectRight: rect.right,
      viewportWidth,
      scrollWidth: html.scrollWidth,
      clientWidth: html.clientWidth
    }];
  }));

  expect(failures, `Textos recortados o fuera del contenedor en ${route}: ${JSON.stringify(failures)}`).toEqual([]);
}

async function assertFormFieldsDoNotOverlap(page: Page, route: string): Promise<void> {
  const overlaps = await page.locator('.fields-grid:visible, .form-fields:visible').evaluateAll((grids) => {
    const results: Array<Record<string, unknown>> = [];

    for (const grid of grids) {
      const fields = Array.from(grid.querySelectorAll('mat-form-field'))
        .map((field) => ({
          element: field as HTMLElement,
          rect: (field as HTMLElement).getBoundingClientRect()
        }))
        .filter(({ rect }) => rect.width > 0 && rect.height > 0);

      for (let first = 0; first < fields.length; first += 1) {
        for (let second = first + 1; second < fields.length; second += 1) {
          const a = fields[first];
          const b = fields[second];
          const overlapX = Math.min(a.rect.right, b.rect.right) - Math.max(a.rect.left, b.rect.left);
          const overlapY = Math.min(a.rect.bottom, b.rect.bottom) - Math.max(a.rect.top, b.rect.top);

          if (overlapX > 1 && overlapY > 1) {
            results.push({
              first: (a.element.textContent ?? '').trim().slice(0, 80),
              second: (b.element.textContent ?? '').trim().slice(0, 80),
              overlapX,
              overlapY
            });
          }
        }
      }
    }

    return results;
  });

  expect(overlaps, `Campos superpuestos en ${route}: ${JSON.stringify(overlaps)}`).toEqual([]);
}

async function certifyRoute(page: Page, route: string, screenshotName: string): Promise<void> {
  await page.goto(route);
  await expect(page.locator('main#main-content')).toBeVisible();
  await expect(page.locator('h1').first()).toBeVisible();
  await page.waitForTimeout(250);

  await assertNoDocumentOverflow(page, route);
  await assertCriticalTextFits(page, route);
  await assertFormFieldsDoNotOverlap(page, route);

  await page.screenshot({
    path: `test-results/fase3/${screenshotName}.png`,
    fullPage: true
  });
}

test.describe('Fase 3 - corrección integral de interfaz', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    const token = await loginApi(request);

    const usersResponse = await request.get(`${API_URL}/usuarios`, {
      headers: authHeaders(token)
    });
    expect(usersResponse.status(), await usersResponse.text()).toBe(200);
    const users = await dataOf(usersResponse) as Array<Record<string, any>>;
    const admin = users.find((user) => String(user.nombreUsuario).toLowerCase() === ADMIN_USERNAME.toLowerCase());
    expect(admin).toBeTruthy();
    adminId = Number(admin!.id);

    const brand = await createCatalog(request, token, 'marcas', {
      nombre: `Marca de certificación visual con nombre extenso ${suffix}`,
      orden: 1
    });
    const model = await createCatalog(request, token, 'modelos', {
      nombre: `Modelo de inventario con descripción extensa ${suffix}`,
      catalogoPadreId: brand.id,
      orden: 1
    });
    const color = await createCatalog(request, token, 'colores', {
      nombre: `Azul profundo de certificación visual ${suffix}`,
      codigoVisual: '#1D4ED8',
      orden: 1
    });
    const size = await createCatalog(request, token, 'tallas', {
      nombre: `Talla especial de demostración ${suffix}`,
      orden: 1
    });

    productName = `Producto de certificación visual con nombre deliberadamente extenso ${suffix}`;
    const productResponse = await request.post(`${API_URL}/productos`, {
      headers: authHeaders(token),
      multipart: {
        Nombre: productName,
        MarcaId: String(brand.id),
        ModeloId: String(model.id),
        ColorId: String(color.id),
        TallaId: String(size.id),
        Descripcion: 'Descripción extensa para verificar que la interfaz conserva legibilidad sin superposición ni recortes.',
        Cantidad: '0',
        Costo: '1234.56',
        Precio: '2345.67',
        UmbralStockBajo: '5'
      }
    });
    expect(productResponse.status(), await productResponse.text()).toBe(201);
  });

  test('Usuario, Perfil, Producto, Productos y cabecera no recortan textos en escritorio', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 1000 });
    await loginUi(page);

    await certifyRoute(page, `/usuarios/${adminId}/editar`, 'desktop-usuario');
    await certifyRoute(page, '/perfil', 'desktop-perfil');
    await certifyRoute(page, '/productos/nuevo', 'desktop-producto-form');
    await certifyRoute(page, '/productos', 'desktop-productos');

    const desktopProductName = page.locator('.table-desktop .product-name', { hasText: productName });
    await expect(desktopProductName).toHaveCount(1);
    await expect(desktopProductName).toBeVisible();
  });

  test('las pantallas prioritarias mantienen texto y acciones completos en teléfono', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await loginUi(page);

    await certifyRoute(page, `/usuarios/${adminId}/editar`, 'mobile-usuario');
    await certifyRoute(page, '/perfil', 'mobile-perfil');
    await certifyRoute(page, '/productos/nuevo', 'mobile-producto-form');
    await certifyRoute(page, '/productos', 'mobile-productos');

    const productCard = page.locator('.producto-card', { hasText: productName });
    await expect(productCard).toHaveCount(1);
    await expect(productCard).toBeVisible();
    await expect(productCard.getByRole('link', { name: 'Ver' })).toBeVisible();
    await expect(productCard.getByRole('link', { name: 'Editar' })).toBeVisible();
  });
});
