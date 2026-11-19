import { expect, test, type Page } from '@playwright/test';

const ruta = '/plataforma/revision-arquitectura-v20';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function sesionSintetica(page: Page, status: 200 | 403) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1502m0405-synthetic-not-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async handler => {
    if (handler.request().method() === 'OPTIONS') {
      await handler.fulfill({ status: 204, headers: cors });
      return;
    }
    await handler.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'C1502_PRIVATE_ERROR_SHOULD_NOT_RENDER' })
    });
  });
}

test('C15-02.M04.05 roles, encabezados y tarjetas semánticas', async ({ page }) => {
  await sesionSintetica(page, 200);
  await page.goto(ruta);
  const vista = page.getByTestId('c1502-review');
  await expect(vista.getByRole('heading', { level: 1, name: 'Revisión de arquitectura e integraciones' })).toBeVisible();
  await expect(vista.getByRole('heading', { level: 2, name: 'Superficies por contrastar' })).toBeVisible();
  await expect(vista.getByRole('listitem')).toHaveCount(5);
  await expect(vista.getByRole('heading', { level: 3 })).toHaveCount(5);
  await expect(vista.getByText('NO_VERIFICADO')).toHaveCount(5);
  await expect(vista.locator('button,form,input,select,textarea')).toHaveCount(0);
});

test('C15-02.M04.05 Tab y Enter reales alcanzan navegación con foco visible', async ({ page }) => {
  await sesionSintetica(page, 200);
  await page.goto(ruta);
  await expect(page.getByTestId('c1502-review')).toBeVisible();
  let alcanzado = false;
  for (let i = 0; i < 80; i++) {
    await page.keyboard.press('Tab');
    const destino = await page.evaluate(() => {
      const activo = document.activeElement;
      return activo?.tagName === 'A' && activo.getAttribute('aria-label') === 'Volver a plataforma';
    });
    if (destino) { alcanzado = true; break; }
  }
  expect(alcanzado, 'Enlace alcanzable con Tab sin foco programático').toBe(true);
  const estilo = await page.evaluate(() => getComputedStyle(document.activeElement!).outlineStyle);
  expect(estilo, 'Indicador de foco visible').not.toBe('none');
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma$/);
});

test('C15-02.M04.05 responsive, target táctil y no mutaciones', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await sesionSintetica(page, 200);
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.goto(ruta);
  const enlace = page.getByRole('link', { name: 'Volver a plataforma' });
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(enlace).toBeVisible();
    const caja = await enlace.boundingBox();
    expect(caja?.height ?? 0, 'Alto mínimo de objetivo táctil').toBeGreaterThanOrEqual(44);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'Desbordamiento horizontal a ' + width).toBeLessThanOrEqual(2);
  }
  expect(writes, 'Lectura privada sin HTTP mutante').toEqual([]);
});

test('C15-02.M04.05 403 no expone inventario ni detalles privados', async ({ page }) => {
  await sesionSintetica(page, 403);
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('architecture-item')).toHaveCount(0);
  await expect(page.getByText('C1502_PRIVATE_ERROR_SHOULD_NOT_RENDER')).toHaveCount(0);
});
test('V21 C15-02.M04.05 contraste real de texto y fondos transparentes en Chromium', async ({ page }) => {
  await sesionSintetica(page, 200);
  for (const width of [320, 1280]) {
    await page.setViewportSize({ width, height: 850 });
    await page.goto(ruta);
    const objectives = [
      { label: 'titulo', locator: page.getByRole('heading', { name: 'Revisión de arquitectura e integraciones' }) },
      { label: 'identificador', locator: page.locator('.review .eyebrow') },
      { label: 'navegacion', locator: page.getByRole('link', { name: 'Volver a plataforma' }) },
      { label: 'tarjeta', locator: page.getByRole('heading', { level: 3 }).first() }
    ];
    for (const objective of objectives) {
      await expect(objective.locator).toBeVisible();
      const contrast = await objective.locator.evaluate(element => {
        const canvas = document.createElement('canvas');
        canvas.width = canvas.height = 1;
        const context = canvas.getContext('2d', { willReadFrequently: true });
        if (!context) throw new Error('CANVAS_COLOR_PROBE_UNAVAILABLE');
        const rgba = (value: string): number[] => {
          context.clearRect(0, 0, 1, 1);
          context.fillStyle = value;
          context.fillRect(0, 0, 1, 1);
          return Array.from(context.getImageData(0, 0, 1, 1).data);
        };
        const overlay = (background: number[], foreground: number[]): number[] => {
          const a = (foreground[3] ?? 255) / 255;
          return [0, 1, 2].map(index =>
            (foreground[index] ?? 0) * a + (background[index] ?? 255) * (1 - a));
        };
        const chain: Element[] = [];
        for (let ancestor: Element | null = element; ancestor; ancestor = ancestor.parentElement) {
          chain.unshift(ancestor);
        }
        let background: number[] = [255, 255, 255];
        for (const ancestor of chain) {
          background = overlay(background, rgba(getComputedStyle(ancestor).backgroundColor));
        }
        const foreground = overlay(background, rgba(getComputedStyle(element).color));
        const luminance = (values: number[]): number => values.reduce((total, value, index) => {
          const component = (value ?? 0) / 255;
          const linear = component <= 0.04045 ? component / 12.92 : ((component + 0.055) / 1.055) ** 2.4;
          return total + linear * [0.2126, 0.7152, 0.0722][index]!;
        }, 0);
        const foreL = luminance(foreground);
        const backL = luminance(background);
        const ratio = (Math.max(foreL, backL) + 0.05) / (Math.min(foreL, backL) + 0.05);
        const negativeOracle = (backL + 0.05) / (backL + 0.05);
        return { ratio, negativeOracle };
      });
      // Los colores identicos deben fallar la misma puerta; no aceptar un algoritmo siempre verde.
      expect(contrast.negativeOracle, 'Control negativo de contraste').toBeLessThan(4.5);
      expect(contrast.ratio, 'Contraste WCAG AA de ' + objective.label + ' a ' + width + 'px').toBeGreaterThanOrEqual(4.5);
    }
  }
});
