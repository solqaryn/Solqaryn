import { expect, test, type Page } from '@playwright/test';

// C01-03.M04.04: real server/login/GET with a controlled slow PUT response.
// Simulated 409 only tests UI concurrency: never count it as real backend CAS evidence.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('M04.04 requires GitHub-hosted MySQL/JWT/Chromium fixture.');
}
const target = '/configuracion/dinamica-v5';
const formSelector = 'form[aria-label="Configuración del tenant activo"]';

async function loginA(page: Page): Promise<void> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill('c1504_admin_a');
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const pending = page.waitForResponse(r => r.url().endsWith('/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const res = await pending;
  expect(res.status()).toBe(200);
  const token: unknown = (await res.json())?.data?.token;
  expect(typeof token).toBe('string');
  expect((token as string).split('.')).toHaveLength(3);
  const membership = await page.request.get(api + '/tenant-context/1', {
    headers: { Authorization: 'Bearer ' + token }
  });
  expect(membership.status()).toBe(200);
  await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
  await page.goto(target);
  await expect(page.locator(formSelector)).toBeVisible();
}

test.describe('V24 M04.04 form: real GET, guarded duplicate mutation, error and navigation', () => {
  test.describe.configure({ retries: 0 });

  test('double submit under an outstanding PUT sends precisely one request and recovers from conflict', async ({ page }) => {
    test.setTimeout(180_000);
    await loginA(page);
    let putCount = 0;
    await page.route('**/empresa-configuracion/tenant/1', async route => {
      if (route.request().method() !== 'PUT') {
        await route.continue();
        return;
      }
      putCount++;
      await new Promise(resolve => setTimeout(resolve, 900));
      await route.fulfill({ status: 409, contentType: 'application/json', body: '{}' });
    });
    const form = page.locator(formSelector);
    const button = page.getByRole('button', { name: 'Guardar configuración tenant' });
    await button.click();
    await expect(form).toHaveAttribute('aria-busy', 'true');
    await form.evaluate(el => el.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })));
    await expect(page.getByText(/La configuración cambió en otra sesión/)).toBeVisible();
    expect(putCount, 'no duplicate PUT even with bypassed disabled button').toBe(1);
    await expect(button).toBeDisabled();
    await page.getByRole('button', { name: 'Recargar versión' }).click();
    await expect(form).toHaveAttribute('aria-busy', 'false');
    await expect(page.getByText(/La configuración cambió en otra sesión/)).toHaveCount(0);
  });

  test('invalid JSON is rejected client-side before any PUT; navigation stays guarded', async ({ page }) => {
    await loginA(page);
    let putCount = 0;
    await page.route('**/empresa-configuracion/tenant/1', async route => {
      if (route.request().method() === 'PUT') putCount++;
      await route.continue();
    });
    const form = page.locator(formSelector);
    await page.locator('textarea[formcontrolname="impuestosJson"]').fill('{invalid');
    await form.evaluate(el => el.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })));
    await expect(page.getByText('La política de impuestos debe ser JSON válido.')).toBeVisible();
    expect(putCount, 'invalid JSON must not reach API').toBe(0);
    await expect(page.getByRole('link', { name: 'Ver todos los ajustes' })).toHaveAttribute('href', '/configuracion');
    await page.getByRole('link', { name: 'Ver todos los ajustes' }).click();
    await expect(page).toHaveURL(/\/configuracion$/);
    await page.keyboard.press('Tab');
    expect(await page.evaluate(() => document.activeElement?.tagName)).not.toBe('BODY');
  });
});
