import { expect, test, type Page } from '@playwright/test';

// Las credenciales/tenant son sintéticos: estas pruebas NO acreditan JWT,
// fiscalidad, MySQL, autorización del backend ni aislamiento A/B real.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function contexto(page: Page, permitir = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0704-m0405-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        usuarioId: 7, empresaId: 71, rolId: 3, rolNombre: 'Lectura', esAdministrador: false
      } }) });
  });
  await page.route(/\/permisos\/mis-permisos\/empresa\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        permisos: permitir ? ['Ventas:Ver'] : [],
        capacidadesHabilitadas: permitir ? ['Ventas:Ver'] : [],
        rutasHabilitadas: permitir ? ['pedidos-venta'] : [],
        operacionMultiEmpresaRestringida: true, esAdministrador: false
      } }) });
  });
  await page.route(/\/pedidos-venta(?:\?|$)/, async route => {
    if (route.request().resourceType() === 'document') return route.continue();
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    if (route.request().method() !== 'GET') return route.abort();
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0
      } }) });
  });
}

test('C07-04.M04.05 accesibilidad responsive real 320/768/1280 sin overflow', async ({ page }) => {
  await contexto(page);
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/pedidos-venta');
    const panel = page.locator('.pedidos-readiness-v5');
    await expect(panel).toBeVisible();
    await expect(panel.locator('ol > li')).toHaveCount(4);
    const layout = await page.evaluate(() => ({
      client: document.documentElement.clientWidth,
      scroll: document.documentElement.scrollWidth
    }));
    expect(layout.scroll - layout.client).toBeLessThanOrEqual(2);
  }
});

test('C07-04.M04.05 Tab real accede al filtro sin bloqueo de foco', async ({ page }) => {
  await contexto(page);
  await page.goto('/pedidos-venta');
  const filtro = page.getByRole('combobox', { name: 'Estado' });
  await expect(filtro).toBeVisible();
  let alcanzado = false;
  for (let i = 0; i < 100; i++) {
    await page.keyboard.press('Tab');
    if (await filtro.evaluate(el => el === document.activeElement)) {
      alcanzado = true;
      break;
    }
  }
  expect(alcanzado).toBe(true);
  await expect(filtro).toBeFocused();
  const focusVisible = await filtro.evaluate(el => el.matches(':focus-visible'));
  expect(focusVisible).toBe(true);
});

test('C07-04.M04.05 WCAG 4.5:1 texto real 320/768/1280 y mutacion adversarial', async ({ page }) => {
  await contexto(page);
  await page.emulateMedia({ contrast: 'more' });
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/pedidos-venta');
    const panel = page.locator('.pedidos-readiness-v5');
    await expect(panel).toBeVisible();
    await expect(panel.locator('ol > li')).toHaveCount(4);
    const result = await panel.evaluate(root => {
      const rgba = (value: string): number[] => {
        if (!/^rgba?\(/.test(value)) throw new Error('CSS color no RGB: ' + value);
        const values = value.match(/[0-9.]+/g)?.map(Number) ?? [];
        if (values.length < 3) throw new Error('CSS color incompleto: ' + value);
        return [values[0], values[1], values[2], values[3] ?? 1];
      };
      const blend = (top: number[], bottom: number[]): number[] =>
        [0, 1, 2].map(i => top[i] * top[3] + bottom[i] * (1 - top[3]));
      const luminance = (rgb: number[]): number => {
        const linear = rgb.map(c => {
          const s = c / 255;
          return s <= 0.04045 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
        });
        return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
      };
      const ratioFor = (element: Element): number => {
        const ancestors: Element[] = [];
        for (let current: Element | null = element; current; current = current.parentElement) ancestors.unshift(current);
        const scheme = getComputedStyle(document.documentElement).colorScheme;
        let background: number[] = scheme.includes('dark') && !scheme.includes('light') ? [0, 0, 0] : [255, 255, 255];
        for (const ancestor of ancestors) background = blend(rgba(getComputedStyle(ancestor).backgroundColor), background);
        const foreground = blend(rgba(getComputedStyle(element).color), background);
        const a = luminance(foreground), b = luminance(background);
        return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
      };
      const samples = [...root.querySelectorAll('h2, h3, p')]
        .filter(el => el.getBoundingClientRect().width > 0)
        .map(el => ({ label: el.tagName + ':' + (el.textContent ?? '').trim().slice(0, 24), ratio: ratioFor(el) }));
      const control = (foreground: string): number => {
        const node = document.createElement('span');
        node.textContent = 'Control de contraste';
        node.style.cssText = 'background:rgb(255,255,255);color:' + foreground;
        root.appendChild(node);
        const value = ratioFor(node);
        node.remove();
        return value;
      };
      return { samples, good: control('rgb(17,17,17)'), bad: control('rgb(187,187,187)') };
    });
    expect(result.samples.length).toBeGreaterThanOrEqual(10);
    for (const sample of result.samples) {
      expect(sample.ratio, 'WCAG real ' + sample.label + ' viewport=' + width).toBeGreaterThanOrEqual(4.5);
    }
    expect(result.good).toBeGreaterThanOrEqual(4.5);
    expect(result.bad).toBeLessThan(4.5);
  }
});

test('C07-04.M04.05 permiso denegado y acceso anónimo no revelan criterios ni controles transaccionales', async ({ page }) => {
  await page.goto('/pedidos-venta');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.pedidos-readiness-cards li')).toHaveCount(0);
  await contexto(page, false);
  await page.goto('/pedidos-venta');
  await expect(page).not.toHaveURL(/\/pedidos-venta(?:\?|$)/);
  await expect(page.locator('.pedidos-readiness-cards li')).toHaveCount(0);
  await expect(page.locator('.pedidos-readiness-v5 form, .pedidos-readiness-v5 button')).toHaveCount(0);
});
