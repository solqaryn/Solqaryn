import { expect, test, type Page } from '@playwright/test';

/** M05.06 real Chromium; session synthetic ONLY; no real JWT, MySQL, CWV or exporter. */
type Status = 200 | 403 | 503;
async function mockSession(
  page: Page, status: (requestNumber: number) => Status, hold?: Promise<void>
): Promise<() => number> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'SYNTHETIC_NOT_JWT_C1104M0506');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let calls = 0;
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({status: 204, headers});
    calls++;
    if (calls === 2 && hold) await hold;
    const code = status(calls);
    await route.fulfill({status: code, headers, contentType: 'application/json',
      body: JSON.stringify(code === 200 ?
        {success: true, data: {nombreUsuario: 'Synthetic', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true}} :
        {success: false, data: null, message: 'PRIVATE_C1104M0506_SECRET'})});
  });
  return () => calls;
}

test('M05.06 real responsive budget, keyboard Tab and no telemetry exporter', async ({page}) => {
  await mockSession(page, () => 200);
  const restricted: string[] = [];
  page.on('request', request => {
    if (/\/(?:telemetry|metrics\/export|traces|alerts\/dispatch)(?:\/|\?|$)/i.test(request.url()))
      restricted.push(request.url());
  });
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  await expect(page.getByText(/proveedor de telemetría: NO_MEDIDO/)).toBeVisible();
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({width, height: 812});
    const b = await page.evaluate(() => ({
      points: document.querySelectorAll('.obs-points li').length,
      actions: document.querySelectorAll('.obs-navigation a,.obs-navigation button').length,
      overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth
    }));
    expect(b.points).toBe(4);
    expect(b.actions).toBe(2);
    expect(b.overflow).toBeLessThanOrEqual(2);
  }
  await page.setViewportSize({width: 375, height: 812});
  const reached = new Set<string>();
  for (let n = 0; n < 100 && reached.size < 2; n++) {
    await page.keyboard.press('Tab');
    const focused = await page.evaluate(() => {
      const el = document.activeElement as HTMLElement | null;
      if (!el?.matches(':focus-visible')) return null;
      const name = el.getAttribute('aria-label');
      if (name !== 'Regresar a plataforma' && name !== 'Revalidar acceso de observabilidad') return null;
      const style = getComputedStyle(el);
      return {name, width: parseFloat(style.outlineWidth), style: style.outlineStyle};
    });
    if (focused) {
      expect(focused.width).toBeGreaterThanOrEqual(2);
      expect(focused.style).not.toBe('none');
      reached.add(focused.name!);
    }
  }
  expect([...reached].sort()).toEqual(['Regresar a plataforma','Revalidar acceso de observabilidad']);
  expect(restricted).toEqual([]);
});

test('M05.06 delayed second GET hides old cards and preserves single-flight', async ({page}) => {
  let release!: () => void;
  const held = new Promise<void>(resolve => {release = resolve;});
  const gets = await mockSession(page, () => 200, held);
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  const button = page.getByRole('button', {name: 'Revalidar acceso de observabilidad'});
  try {
    await button.click();
    await expect.poll(() => gets()).toBe(2);
    await expect(button).toBeDisabled();
    await expect(page.locator('.obs-points li')).toHaveCount(0);
    await expect(page.locator('#obs-status[aria-live="polite"]')).toBeVisible();
  } finally {
    release();
  }
  await expect(button).toBeEnabled();
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  expect(gets()).toBe(2);
});

test('M05.06 503 after 200 fails closed without telemetry secret or stale cards', async ({page}) => {
  const gets = await mockSession(page, n => n === 1 ? 200 : 503);
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  await page.getByRole('button', {name: 'Revalidar acceso de observabilidad'}).click();
  await expect(page.getByText('No se pudo revalidar la sesión. Observabilidad privada oculta.')).toBeVisible();
  await expect(page.locator('.obs-points li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_C1104M0506_SECRET')).toHaveCount(0);
  expect(gets()).toBe(2);
});

test('M05.06 403 denies cards and private branding with synthetic session', async ({page}) => {
  await mockSession(page, () => 403);
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.obs-points li')).toHaveCount(0);
  await expect(page.getByRole('region', {name: 'Identidad visual de observabilidad'})).toHaveCount(0);
  await expect(page.getByText('PRIVATE_C1104M0506_SECRET')).toHaveCount(0);
});
