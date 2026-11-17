import { expect, test, type Page } from '@playwright/test';

/** C15-05.M04.05 — verificación real de navegador; sesión sintética NO prueba JWT/RBAC de API. */
async function prepararSesion(page: Page, administrador = true): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'v17-accessibility-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({
      status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: {
          nombreUsuario: 'Prueba sintética',
          rolNombre: 'Plataforma',
          ambito: 'Plataforma',
          esAdministrador: administrador
        }
      })
    });
  });
}

test('C15-05.M04.05: 320, 390, 768 y 1280 px sin desbordamiento horizontal', async ({ page }) => {
  await prepararSesion(page);
  for (const width of [320, 390, 768, 1280]) {
    await page.setViewportSize({ width, height: 840 });
    await page.goto('/plataforma/auditoria-operativa-v5');
    await expect(page.getByRole('heading', { name: 'Controles de ejecución y evidencia' })).toBeVisible();
    await expect(page.locator('ol.controls > li')).toHaveCount(4);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth
    );
    expect(overflow, 'overflow de página en ' + width + ' px').toBeLessThanOrEqual(2);
  }
});

test('C15-05.M04.05: retorno navegable por teclado y foco visible', async ({ page }) => {
  await prepararSesion(page);
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('/plataforma/auditoria-operativa-v5');
  const retorno = page.getByRole('link', { name: 'Regresar a administración de plataforma' });
  await expect(retorno).toBeVisible();
  // Solamente Tab fisico alcanza el enlace: .focus() daria un falso PASS si tabindex=-1.
  let alcanzadoConTab = false;
  for (let paso = 0; paso < 48; paso++) {
    await page.keyboard.press('Tab');
    if (await retorno.evaluate(element => document.activeElement === element)) {
      alcanzadoConTab = true;
      break;
    }
  }
  expect(alcanzadoConTab, 'enlace de retorno alcanzable con Tab real').toBe(true);
  await expect(retorno).toBeFocused();
  expect(await retorno.evaluate(element => getComputedStyle(element).outlineStyle)).not.toBe('none');
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma(?:$|[?#])/);
});

test('C15-05.M04.05: adversarial tabindex=-1 no es alcanzable por Tab', async ({ page }) => {
  await prepararSesion(page);
  await page.goto('/plataforma/auditoria-operativa-v5');
  const retorno = page.getByRole('link', { name: 'Regresar a administración de plataforma' });
  await expect(retorno).toBeVisible();
  // Mutacion en DOM efimero de test; NO modifica la aplicacion ni autoriza acceso.
  await retorno.evaluate(element => element.setAttribute('tabindex', '-1'));
  for (let paso = 0; paso < 48; paso++) {
    await page.keyboard.press('Tab');
    expect(await retorno.evaluate(element => document.activeElement === element),
      'tabindex=-1 no debe recibir foco por Tab en paso ' + (paso + 1)).toBe(false);
  }
});

test('C15-05.M04.05: landmarks, jerarquía, etiquetas y contraste AA estático', async ({ page }) => {
  await prepararSesion(page);
  await page.goto('/plataforma/auditoria-operativa-v5');
  const main = page.locator('main[aria-labelledby="audit-view-title"]');
  await expect(main).toHaveCount(1);
  await expect(main.locator('h1')).toHaveCount(1);
  await expect(main.locator('ol.controls > li > h3')).toHaveCount(4);
  await expect(page.getByRole('region', { name: 'Alcance y fuente de esta pantalla' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Regresar a administración de plataforma' })).toHaveCount(1);
  // M04.02 agrega exclusivamente la revalidación de sesión: no permite formularios ni acciones operativas.
  await expect(main.getByRole('button', { name: 'Revalidar sesión de auditoría' })).toHaveCount(1);
  await expect(main.locator('form, input, button:not([aria-label="Revalidar sesión de auditoría"])')).toHaveCount(0);

  const ratios = await page.evaluate(() => {
    function rgb(value: string): number[] {
      const numbers = value.match(/[\d.]+/g)?.map(Number) ?? [];
      return numbers.slice(0, 3);
    }
    function luminance(color: number[]): number {
      const linear = color.map(channel => {
        const n = channel / 255;
        return n <= 0.04045 ? n / 12.92 : Math.pow((n + 0.055) / 1.055, 2.4);
      });
      return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
    }
    function background(element: HTMLElement): number[] {
      for (let current: HTMLElement | null = element; current; current = current.parentElement) {
        const color = getComputedStyle(current).backgroundColor;
        if (color !== 'transparent' && !color.endsWith(', 0)') && !color.endsWith(' 0)')) {
          const channels = rgb(color);
          if (channels.length === 3) return channels;
        }
      }
      return [255, 255, 255];
    }
    return ['#audit-view-title', '.eyebrow', '.return-link'].map(selector => {
      const element = document.querySelector<HTMLElement>(selector);
      if (!element) return { selector, contrast: 0 };
      const text = rgb(getComputedStyle(element).color);
      if (text.length !== 3) return { selector, contrast: 0 };
      const a = luminance(text), b = luminance(background(element));
      return { selector, contrast: (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05) };
    });
  });
  for (const item of ratios) {
    expect(item.contrast, item.selector + ' requiere contraste WCAG AA >= 4.5:1').toBeGreaterThanOrEqual(4.5);
  }
});

test('C15-05.M04.05: sesión plataforma sin privilegio admin oculta controles', async ({ page }) => {
  await prepararSesion(page, false);
  await page.goto('/plataforma/auditoria-operativa-v5');
  // El guard puede redirigir a login/plataforma antes de montar el componente.
  // Verificar negación efectiva, no un texto específico de un alert global.
  await expect(page.getByRole('heading', { name: 'Controles de ejecución y evidencia' })).toHaveCount(0);
  await expect(page.locator('ol.controls > li')).toHaveCount(0);
  await expect(page.getByText('Propiedad exclusiva')).toHaveCount(0);
});
