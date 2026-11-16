import { expect, test, type Page } from '@playwright/test';

// JWT y tenant son simulados: no se certifica API financiera, RBAC backend ni A/B real.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function escenario(page: Page, permitir = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0704-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status:204, headers:cors });
    await route.fulfill({ status:200, headers:cors, contentType:'application/json',
      body:JSON.stringify({ success:true, data:{
        usuarioId:7, empresaId:71, rolId:3, rolNombre:'Lectura', esAdministrador:false
      } }) });
  });
  await page.route(/\/permisos\/mis-permisos\/empresa\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status:204, headers:cors });
    await route.fulfill({ status:200, headers:cors, contentType:'application/json',
      body:JSON.stringify({ success:true, data:{
        permisos: permitir ? ['Ventas:Ver'] : [],
        capacidadesHabilitadas: permitir ? ['Ventas:Ver'] : [],
        rutasHabilitadas: permitir ? ['pedidos-venta'] : [],
        operacionMultiEmpresaRestringida:true, esAdministrador:false
      } }) });
  });
  await page.route(/\/pedidos-venta(?:\?|$)/, async route => {
    if (route.request().resourceType() === 'document') return route.continue();
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status:204, headers:cors });
    if (route.request().method() !== 'GET') return route.abort();
    await route.fulfill({ status:200, headers:cors, contentType:'application/json',
      body:JSON.stringify({ success:true, data:{
        items:[], page:1, pageSize:10, totalCount:0, totalPages:0
      } }) });
  });
}

test('C07-04.M04.01 anónimo no accede a pedidos ni criterios internos', async ({ page }) => {
  await page.goto('/pedidos-venta');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.pedidos-readiness-cards li')).toHaveCount(0);
});

test('C07-04.M04.01 lector autorizado ve cuatro contratos read-only a 320 y 1280', async ({ page }) => {
  await escenario(page);
  for (const width of [320, 1280]) {
    await page.setViewportSize({ width, height:800 });
    await page.goto('/pedidos-venta');
    await expect(page.getByRole('heading', {name:'Pedidos de venta'})).toBeVisible();
    const panel = page.locator('.pedidos-readiness-v5');
    await expect(panel.locator('li')).toHaveCount(4);
    await expect(panel.locator('p[role="note"]')).toContainText('NO_MEDIDO');
    await expect(panel.locator('form, input, textarea, button')).toHaveCount(0);
    const measurement = await page.evaluate(() => {
      const viewport = document.documentElement.clientWidth;
      const overflow = document.documentElement.scrollWidth - viewport;
      const offenders = [...document.querySelectorAll('body *')].map(el => {
        const rect = el.getBoundingClientRect();
        return {
          tag: el.tagName.toLowerCase(),
          cls: typeof el.className === 'string' ? el.className.slice(0, 85) : '',
          left: Math.round(rect.left), right: Math.round(rect.right),
          width: Math.round(rect.width)
        };
      }).filter(x => x.right > viewport + 2 && x.width > 0)
        .sort((a, b) => b.right - a.right).slice(0, 10);
      return { viewport, overflow, offenders };
    });
    // Diagnóstico estructural sin datos ni contenido del tenant; assert global intacto.
    console.log('C0704_LAYOUT_OVERFLOW_DIAG=' + JSON.stringify(measurement));
    if (width === 320) {
      // Exigir desplazamiento horizontal INTERNO de la tabla, no clipping de columnas.
      const shell = await page.locator('.table').evaluate(el => {
        const style = getComputedStyle(el);
        return { client: el.clientWidth, scroll: el.scrollWidth, overflowX: style.overflowX };
      });
      expect(shell.client).toBeLessThanOrEqual(width);
      expect(shell.overflowX).toBe('auto');
      expect(shell.scroll).toBeGreaterThan(shell.client);
    }
    expect(measurement.overflow).toBeLessThanOrEqual(2);
  }
});

test('C07-04.M04.01 Ventas:Ver denegado oculta criterios y ruta protegida', async ({ page }) => {
  await escenario(page, false);
  await page.goto('/pedidos-venta');
  await expect(page).not.toHaveURL(/\/pedidos-venta(?:\?|$)/);
  await expect(page.locator('.pedidos-readiness-cards li')).toHaveCount(0);
});
