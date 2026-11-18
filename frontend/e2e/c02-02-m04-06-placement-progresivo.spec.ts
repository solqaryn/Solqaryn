import { expect, test, type Page } from '@playwright/test';

// C02-02.M04.06: navegador real con sesión HTTP sintética, nunca JWT o MySQL real.
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const admin = {
  success: true,
  data: { nombreUsuario: 'Simulado', rolNombre: 'Admin',
    ambito: 'Plataforma', esAdministrador: true }
};
async function grantedSyntheticSession(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-playwright-not-a-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    await route.fulfill({ status: 200, headers: cors,
      contentType: 'application/json', body: JSON.stringify(admin) });
  });
}
test('M04.06 densidad predeterminada sin overflow en 320/768/1280', async ({ page }) => {
  await grantedSyntheticSession(page);
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 820 });
    await page.goto('/plataforma/placement-v5');
    await expect(page.locator('main.placement-review')).toHaveAttribute('data-densidad', 'normal');
    await expect(page.getByRole('heading', { name: 'Placement compartido y dedicado' })).toBeVisible();
    await expect(page.locator('.modalidades article')).toHaveCount(2);
    await expect(page.locator('section.estado')).toHaveAttribute('aria-live', 'polite');
    await expect(page.locator('section.estado')).toHaveAttribute('aria-busy', 'false');
    await expect(page.getByRole('button', { name: 'Revalidar sesión de placement' })).toBeEnabled();
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow).toBeLessThanOrEqual(2);
  }
});
test('M04.06 revalidación progresiva oculta contenido hasta GET y evita doble solicitud', async ({ page }) => {
  await grantedSyntheticSession(page);
  await page.setViewportSize({ width: 320, height: 820 });
  await page.goto('/plataforma/placement-v5');
  await expect(page.locator('.modalidades article')).toHaveCount(2);
  let liberar!: () => void;
  const retenida = new Promise<void>(resolve => { liberar = resolve; });
  let consultas = 0;
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    consultas++;
    await retenida;
    await route.fulfill({ status: 200, headers: cors,
      contentType: 'application/json', body: JSON.stringify(admin) });
  });
  const boton = page.getByRole('button', { name: 'Revalidar sesión de placement' });
  await boton.click();
  await expect.poll(() => consultas).toBe(1);
  await expect(page.locator('section.estado')).toHaveAttribute('aria-busy', 'true');
  await expect(page.getByRole('status')).toContainText('Revalidando autorización');
  await expect(page.locator('.modalidades article')).toHaveCount(0);
  await expect(boton).toBeDisabled();
  liberar();
  await expect(page.locator('section.estado')).toHaveAttribute('aria-busy', 'false');
  await expect(page.locator('.modalidades article')).toHaveCount(2);
  await expect(boton).toBeEnabled();
  expect(consultas).toBe(1);
});
test('M04.06 error 503 no filtra payload ni recupera contenido privado', async ({ page }) => {
  await grantedSyntheticSession(page);
  await page.setViewportSize({ width: 390, height: 820 });
  await page.goto('/plataforma/placement-v5');
  await expect(page.locator('.modalidades article')).toHaveCount(2);
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    await route.fulfill({ status: 503, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: false, message: 'DSN_SECRETO_NO_UI' }) });
  });
  await page.getByRole('button', { name: 'Revalidar sesión de placement' }).click();
  await expect(page.locator('section.estado')).toHaveAttribute('aria-busy', 'false');
  await expect(page.getByText('No fue posible revalidar la sesión. La información permanece restringida.')).toBeVisible();
  await expect(page.locator('.modalidades article')).toHaveCount(0);
  await expect(page.getByText('DSN_SECRETO_NO_UI')).toHaveCount(0);
  await expect(page.locator('.placement-review form, .placement-review input')).toHaveCount(0);
});