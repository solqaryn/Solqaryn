import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

function pagedPeriodos(pageNumber: number) {
  return {
    success: true,
    message: null,
    data: {
      items: [
        {
          id: pageNumber,
          fechaInicio: '2026-09-01',
          fechaFin: '2026-09-30',
          estado: pageNumber === 1 ? 1 : 2,
          cerradoEnUtc: pageNumber === 1 ? null : '2026-09-30T23:59:59Z'
        }
      ],
      page: pageNumber,
      pageSize: 20,
      totalCount: 21,
      totalPages: 2
    }
  };
}

test.describe('Períodos contables E2E N4.9.E', () => {
  test.beforeEach(async ({ page }) => {
    await loginUi(page);
  });

  test('renderiza el flujo y pagina usando el contrato HTTP esperado', async ({ page }) => {
    const requestedPages: string[] = [];

    await page.route('**/periodos-contables**', async (route) => {
      const request = route.request();
      if (request.method() !== 'GET' || request.resourceType() === 'document') {
        await route.continue();
        return;
      }

      const url = new URL(request.url());
      const pageNumber = Number(url.searchParams.get('page') ?? '1');
      requestedPages.push(url.searchParams.toString());
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(pagedPeriodos(pageNumber))
      });
    });

    await page.goto('/periodos-contables');

    await expect(page.getByRole('heading', { name: 'Períodos contables' })).toBeVisible({ timeout: 5000 });
    await expect(page.getByRole('form', { name: 'Filtros de períodos contables' })).toBeVisible();
    await expect(page.getByRole('navigation', { name: 'Paginación' })).toBeVisible();
    await expect(page.getByText('Página 1 de 2')).toBeVisible();

    await page.getByRole('button', { name: 'Siguiente' }).click();
    await expect(page.getByText('Página 2 de 2')).toBeVisible();
    await expect.poll(() => requestedPages.some((query) => query.includes('page=2') && query.includes('pageSize=20'))).toBe(true);
  });

  test('envía filtros de fecha y estado al backend y conserva accesibilidad básica', async ({ page }) => {
    const requestedQueries: string[] = [];

    await page.route('**/periodos-contables**', async (route) => {
      const request = route.request();
      if (request.method() !== 'GET' || request.resourceType() === 'document') {
        await route.continue();
        return;
      }

      const url = new URL(request.url());
      requestedQueries.push(url.searchParams.toString());
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(pagedPeriodos(1))
      });
    });

    await page.goto('/periodos-contables');

    await page.locator('input[formcontrolname="fechaDesde"]').fill('2026-09-01');
    await page.locator('input[formcontrolname="fechaHasta"]').fill('2026-09-30');
    await page.locator('mat-select[formcontrolname="estado"]').click();
    await page.getByRole('option', { name: 'Abierto' }).click();
    await page.getByRole('button', { name: 'Aplicar' }).click();

    await expect.poll(() => requestedQueries.some((query) =>
      query.includes('fechaDesde=2026-09-01') &&
      query.includes('fechaHasta=2026-09-30') &&
      query.includes('estado=1') &&
      query.includes('page=1')
    )).toBe(true);

    await expect(page.getByRole('columnheader', { name: 'Rango' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: 'Estado' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Abierto', exact: true })).toBeVisible();
  });
});
