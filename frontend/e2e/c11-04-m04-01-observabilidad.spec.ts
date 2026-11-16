import { expect, test, type Page } from '@playwright/test';

/** API y token sintéticos; NO evidencia de autenticación productiva ni SLO. */
async function mockSession(page: Page, allow: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c11-04-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await route.fulfill({ status: allow ? 200 : 403, contentType: 'application/json', headers,
      body: JSON.stringify(allow ?
        { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } } :
        { success: false, data: null, message: 'PRIVATE_TELEMETRY_SECRET' }) });
  });
}
test('ruta de observabilidad es privada para anónimo', async ({ page }) => {
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.obs-points li')).toHaveCount(0);
});
test('403 sintético no expone contenido ni diagnóstico', async ({ page }) => {
  await mockSession(page, false);
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.obs-points li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_TELEMETRY_SECRET')).toHaveCount(0);
});
test('vista sintética read-only, móvil y sin exporter ni consultas operativas', async ({ page }) => {
  await mockSession(page, true);
  await page.setViewportSize({ width: 375, height: 812 });
  const blocked: string[] = [];
  await page.route(/\/(?:telemetry|metrics\/export|traces|alerts\/dispatch)(?:\/|\?|$)/i, route => {
    blocked.push(route.request().url());
    return route.abort();
  });
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page.getByRole('heading', { name: 'Diagnóstico y señales seguras' })).toBeVisible();
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  await expect(page.getByText(/proveedor de telemetría: NO_MEDIDO/)).toBeVisible();
  await expect(page.locator('.observability-guide button')).toHaveCount(1);
  await expect(page.locator('.observability-guide form, .observability-guide input')).toHaveCount(0);
  expect(blocked).toEqual([]);
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
});


test('M04.02: revalidacion 503 protege contenido y nunca muestra payload', async ({ page }) => {
  let status = 200;
  let llamadas = 0;
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c11-04-m04-02-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    llamadas++;
    await route.fulfill({ status, headers, contentType: 'application/json',
      body: JSON.stringify(status === 200 ?
        { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } } :
        { success: false, data: null, message: 'TELEMETRY_SECRET' }) });
  });
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  status = 503;
  await page.getByRole('button', { name: 'Revalidar acceso de observabilidad' }).click();
  await expect(page.getByText('No se pudo revalidar la sesión. Observabilidad privada oculta.')).toBeVisible();
  await expect(page.locator('.obs-points li')).toHaveCount(0);
  await expect(page.getByText('TELEMETRY_SECRET')).toHaveCount(0);
  expect(llamadas).toBe(2);
});

test('M04.02: 403 posterior no reactiva controles privados', async ({ page }) => {
  let status = 200;
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    return route.fulfill({ status, headers, contentType: 'application/json',
      body: JSON.stringify(status === 200 ?
        { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } } :
        { success: false, data: null, message: 'REDACTED' }) });
  });
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  status = 403;
  await page.getByRole('button', { name: 'Revalidar acceso de observabilidad' }).click();
  await expect(page.locator('.obs-points li')).toHaveCount(0);
  await expect(page.getByText('Sesión de plataforma no confirmada. Observabilidad privada oculta.').first()).toBeVisible();
});


test('M04.03: branding institucional sólo aparece con sesión sintética autorizada', async ({ page }) => {
  await mockSession(page, true);
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto('/plataforma/observabilidad-v5');
  const brand = page.getByRole('region', { name: 'Identidad visual de observabilidad' });
  await expect(brand).toBeVisible();
  await expect(brand.getByText('SOLQARYN · Observabilidad')).toBeVisible();
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
});
test('M04.03: respuesta 403 sintética nunca presenta branding privado', async ({ page }) => {
  await mockSession(page, false);
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.getByRole('region', { name: 'Identidad visual de observabilidad' })).toHaveCount(0);
  await expect(page.getByText('PRIVATE_TELEMETRY_SECRET')).toHaveCount(0);
});


test('M04.04: navegación accesible y salida privada sin formulario ni acción doble', async ({ page }) => {
  await mockSession(page, true);
  await page.goto('/plataforma/observabilidad-v5');
  const navigation = page.getByRole('navigation', { name: 'Acciones de observabilidad' });
  await expect(navigation).toBeVisible();
  await expect(navigation.getByRole('link', { name: 'Regresar a plataforma' })).toHaveAttribute('href', '/plataforma');
  const retry = navigation.getByRole('button', { name: 'Revalidar acceso de observabilidad' });
  await expect(retry).toHaveAttribute('aria-describedby', 'obs-status');
  await expect(page.locator('#obs-status[aria-live="polite"]')).toBeVisible();
  await expect(page.locator('.observability-guide form, .observability-guide input')).toHaveCount(0);
});
test('M04.04: revalidación lenta deshabilita segunda acción sin duplicar GET', async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c11-04-m04-04-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let calls = 0;
  let release: (() => void) | undefined;
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    calls++;
    if (calls === 2) await new Promise<void>(resolve => { release = resolve; });
    await route.fulfill({ status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
      } }) });
  });
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  const button = page.getByRole('button', { name: 'Revalidar acceso de observabilidad' });
  await button.click();
  await expect.poll(() => calls).toBe(2);
  await expect(button).toBeDisabled();
  await expect(page.locator('.obs-points li')).toHaveCount(0);
  expect(release).toBeDefined();
  release?.();
  await expect(button).toBeEnabled();
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  expect(calls).toBe(2);
});


test('M04.05: teclado Tab real alcanza enlace y boton con foco visible, sin focus programatico', async ({ page }) => {
  await mockSession(page, true);
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page.getByRole('navigation', { name: 'Acciones de observabilidad' })).toBeVisible();
  const targets = new Set<string>();
  for (let step = 0; step < 90 && targets.size < 2; step++) {
    await page.keyboard.press('Tab');
    const focused = await page.evaluate(() => {
      const element = document.activeElement as HTMLElement | null;
      if (!element || !element.matches(':focus-visible')) return null;
      const name = element.getAttribute('aria-label');
      if (name !== 'Regresar a plataforma' && name !== 'Revalidar acceso de observabilidad') return null;
      const style = getComputedStyle(element);
      return { name, outlineStyle: style.outlineStyle, outlineWidth: parseFloat(style.outlineWidth) };
    });
    if (!focused) continue;
    expect(focused.outlineStyle).not.toBe('none');
    expect(focused.outlineWidth).toBeGreaterThanOrEqual(2);
    targets.add(focused.name!);
  }
  expect([...targets].sort()).toEqual(['Regresar a plataforma', 'Revalidar acceso de observabilidad']);
});

test('M04.05: superficies 320/768/1280 no desbordan, conservan targets tactiles y rejilla adaptativa', async ({ page }) => {
  await mockSession(page, true);
  await page.goto('/plataforma/observabilidad-v5');
  await expect(page.locator('.obs-points li')).toHaveCount(4);
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 812 });
    await expect(page.getByRole('navigation', { name: 'Acciones de observabilidad' })).toBeVisible();
    const dimensions = await page.evaluate(() => {
      const nav = document.querySelector('.obs-navigation')!;
      const targets = Array.from(nav.querySelectorAll('a,button')) as HTMLElement[];
      const grid = document.querySelector('.obs-points')!;
      const columns = getComputedStyle(grid).gridTemplateColumns.split(' ').filter(Boolean).length;
      return {
        overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
        columns,
        controls: targets.map(t => ({ width: t.getBoundingClientRect().width, height: t.getBoundingClientRect().height, right: t.getBoundingClientRect().right }))
      };
    });
    expect(dimensions.overflow, 'desbordamiento en ' + width).toBeLessThanOrEqual(2);
    expect(dimensions.controls).toHaveLength(2);
    for (const control of dimensions.controls) {
      expect(control.height, 'alto tactil ' + width).toBeGreaterThanOrEqual(44);
      expect(control.width).toBeGreaterThan(30);
      expect(control.right).toBeLessThanOrEqual(width + 2);
    }
    expect(dimensions.columns).toBe(width <= 720 ? 1 : 2);
  }
});

test('M04.05: contraste real del distintivo y denegacion responsiva no filtra datos privados', async ({ page }) => {
  await mockSession(page, true);
  await page.goto('/plataforma/observabilidad-v5');
  const contrast = await page.locator('.obs-branding-mark').evaluate(element => {
    function luminance(rgb: number[]) {
      const c = rgb.map(v => {
        const s = v / 255;
        return s <= 0.04045 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
      });
      return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
    }
    const parse = (value: string) => [...value.matchAll(/[\d.]+/g)].slice(0, 3).map(x => Number(x[0]));
    const style = getComputedStyle(element);
    const foreground = luminance(parse(style.color));
    const background = luminance(parse(style.backgroundColor));
    return (Math.max(foreground, background) + 0.05) / (Math.min(foreground, background) + 0.05);
  });
  expect(contrast, 'contraste WCAG del distintivo').toBeGreaterThanOrEqual(4.5);
  await page.setViewportSize({ width: 320, height: 720 });
  await expect(page.locator('.obs-branding')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  await page.reload();
  await expect(page.locator('.obs-points li')).toHaveCount(4);
});
