import { expect, test, type Page } from '@playwright/test';

// C00-01.M04.03: regresión en Chromium real, sin mocks del Router ni AppComponent.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'X-Empresa-Id, Content-Type, Authorization',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const marca = 'Marca Uno';
const tema = {
  colorPrimario: '#950000', colorSecundario: '#550000', colorAcento: '#b42222',
  fondoPrincipal: '#ffffff', fondoTarjetas: '#ffffff', menuLateral: '#950000',
  barraSuperior: '#ffffff', encabezados: '#111111', botonesPrincipales: '#950000',
  textoPrincipal: '#111111', textoSecundario: '#555555', colorExito: '#008000',
  colorAdvertencia: '#a06000', colorError: '#b00020', colorInformacion: '#005ea8'
};
const body = JSON.stringify({ success: true, data: {
  identidad: {
    id: 901, nombreComercial: marca, nombreVisibleSistema: marca,
    eslogan: 'Auditoría de rutas', descripcionSistema: 'Tienda de prueba',
    moneda: 'HNL', zonaHoraria: 'America/Tegucigalpa', formatoFecha: 'dd/MM/yyyy',
    encabezadoActivo: true, piePaginaActivo: false, copyright: '',
    mostrarCopyright: false, usarAnioAutomaticoCopyright: true
  }, tema, categorias: [], destacados: []
} });

async function retenerBootstrap(page: Page) {
  let marcar!: () => void;
  let abrir!: () => void;
  const solicitado = new Promise<void>(resolve => { marcar = resolve; });
  const retenido = new Promise<void>(resolve => { abrir = resolve; });
  await page.route(/http:\/\/localhost:5005\/tienda\/bootstrap(?:\?.*)?$/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    marcar();
    await retenido;
    await route.fulfill({ status: 200, contentType: 'application/json', headers: cors, body });
  });
  return { solicitado, liberar: abrir };
}

async function entregar(page: Page, liberar: () => void) {
  const response = page.waitForResponse(r =>
    r.request().method() === 'GET' && /\/tienda\/bootstrap(?:\?|$)/.test(r.url())
  );
  liberar();
  await (await response).finished();
  await page.evaluate(() => new Promise<void>(resolve => window.setTimeout(resolve, 180)));
}

test.describe('A5 M04.03 - ruta Angular y respuesta bootstrap tardía', () => {
  test.describe.configure({ retries: 0 });

  test('positivo: respuesta oportuna aplica tema y SEO de la tienda solicitante', async ({ page }) => {
    const gate = await retenerBootstrap(page);
    await page.goto('/tienda');
    await gate.solicitado;
    await entregar(page, gate.liberar);
    await expect(page).toHaveTitle('Marca Uno | Tecnología y compras en línea');
    await expect.poll(() => page.evaluate(() =>
      document.documentElement.style.getPropertyValue('--color-primary')
    )).toBe('#950000');
    await expect(page.locator('link[rel="canonical"]')).toHaveAttribute(
      'href', 'https://solqaryn-prod.vercel.app/tienda'
    );
  });

  test('negativo: A -> login antes de resolver HTTP no restaura tema ni SEO de A', async ({ page }) => {
    const gate = await retenerBootstrap(page);
    await page.goto('/tienda');
    await gate.solicitado;
    // POPSTATE navega dentro de Angular, sin destruir documento ni cancelar por recarga.
    await page.evaluate(() => {
      window.history.pushState({}, '', '/login');
      window.dispatchEvent(new PopStateEvent('popstate', { state: window.history.state }));
    });
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    await expect(page).toHaveTitle('SOLQARYN | Acceso privado');
    await entregar(page, gate.liberar);
    await expect(page).toHaveTitle('SOLQARYN | Acceso privado');
    await expect(page.locator('meta[name="robots"]')).toHaveAttribute(
      'content', 'noindex,nofollow,noarchive'
    );
    await expect(page.locator('link[rel="canonical"]')).toHaveCount(0);
    expect(await page.evaluate(() => ({
      primary: document.documentElement.style.getPropertyValue('--color-primary'),
      button: document.documentElement.style.getPropertyValue('--color-button')
    }))).toEqual({ primary: '', button: '' });
  });

  test('negativo: A -> B no genera ni siquiera título SEO transitorio de A', async ({ page }) => {
    const gate = await retenerBootstrap(page);
    await page.goto('/tienda');
    await gate.solicitado;
    await page.locator('app-storefront-header nav.store-nav a[href="/tienda/productos"]').click();
    await expect(page).toHaveURL(/\/tienda\/productos$/);
    await page.evaluate(() => {
      const writes: string[] = [];
      (window as Window & { __m0403Titles?: string[] }).__m0403Titles = writes;
      const title = document.head.querySelector('title');
      if (!title) throw new Error('Falta el título SEO.');
      new MutationObserver(mutations => {
        for (const m of mutations) {
          if (m.type === 'childList') {
            for (const node of Array.from(m.addedNodes)) writes.push(node.textContent || '');
          } else writes.push(m.target.textContent || '');
        }
      }).observe(title, { childList: true, subtree: true, characterData: true });
    });
    await entregar(page, gate.liberar);
    await expect(page).toHaveTitle('Productos | Marca Uno');
    const writes = await page.evaluate(() =>
      (window as Window & { __m0403Titles?: string[] }).__m0403Titles ?? []
    );
    expect(writes).not.toContain('Marca Uno | Tecnología y compras en línea');
    await expect(page.locator('link[rel="canonical"]')).toHaveAttribute(
      'href', 'https://solqaryn-prod.vercel.app/tienda/productos'
    );
  });
});
