import { expect, test, type Page } from '@playwright/test';

/** Solo navegador con sesión y API sintéticas; no acredita JWT, MySQL ni restauraciones. */
async function mockSesion(page: Page, allow = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-accessibility-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if(route.request().method()==='OPTIONS') {
      await route.fulfill({status:204,headers});
      return;
    }
    await route.fulfill({status:allow?200:403,headers,contentType:'application/json',
      body:JSON.stringify(allow
        ? {success:true,data:{nombreUsuario:'Operador',rolNombre:'Admin',ambito:'Plataforma',esAdministrador:true}}
        : {success:false,data:null,message:'INTERNAL_ONLY_REJECTED'})});
  });
}
test('anónimo no ve datos protegidos', async ({page}) => {
  await page.goto('/plataforma/respuesta-incidentes-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.incident-review li')).toHaveCount(0);
});
test('teclado llega al título y diseño resiste anchuras móvil y escritorio', async ({page}) => {
  await mockSesion(page);
  await page.setViewportSize({width:320,height:720});
  await page.goto('/plataforma/respuesta-incidentes-v5');
  const salto=page.getByRole('link',{name:'Ir al contenido de incidentes'});
  // Navegación real: no llamar .focus() ni construir un PASS artificial.
  let alcanzadoConTab = false;
  for (let intento = 0; intento < 40; intento++) {
    await page.keyboard.press('Tab');
    if (await salto.evaluate(elemento => elemento === document.activeElement)) {
      alcanzadoConTab = true;
      break;
    }
  }
  expect(alcanzadoConTab, 'El skip-link debe ser alcanzable con Tab desde el inicio').toBe(true);
  await expect(salto).toBeFocused();
  const estiloFoco = await salto.evaluate(elemento => getComputedStyle(elemento).outlineStyle);
  expect(estiloFoco, 'El foco de teclado debe tener contorno visible').not.toBe('none');
  await page.keyboard.press('Enter');
  await expect(page.getByRole('heading',{name:'Respuesta, revocación y recuperación segura'})).toBeFocused();
  await expect(page.locator('.incident-review li')).toHaveCount(4);
  for(const width of [320,375,768,1440]) {
    await page.setViewportSize({width,height:720});
    const exceso=await page.evaluate(()=>document.documentElement.scrollWidth-document.documentElement.clientWidth);
    expect(exceso).toBeLessThanOrEqual(2);
  }
  await expect(page.getByRole('link',{name:'Regresar a plataforma'})).toBeVisible();
});
test('contraste alto conserva controles legibles y 403 no filtra contenido', async ({page}) => {
  await mockSesion(page);
  await page.emulateMedia({forcedColors:'active',reducedMotion:'reduce'});
  await page.goto('/plataforma/respuesta-incidentes-v5');
  await expect(page.locator('.incident-review li')).toHaveCount(4);
  const relacion = await page.locator('.incident-guide').evaluate(elemento => {
    if (!matchMedia('(forced-colors: active)').matches) return 0;
    const luminosidad = (color: string): number | null => {
      const canales = color.match(/[0-9]+(?:[.][0-9]+)?/g)?.map(Number) ?? [];
      if (canales.length < 3 || canales.length > 4 || canales.some(v => !Number.isFinite(v))) return null;
      if (canales.length === 4 && canales[3] < 1) return null;
      const rgb = canales.slice(0, 3).map(v => {
        const normalizado = v / 255;
        return normalizado <= 0.04045 ? normalizado / 12.92 : ((normalizado + 0.055) / 1.055) ** 2.4;
      });
      return rgb[0] * 0.2126 + rgb[1] * 0.7152 + rgb[2] * 0.0722;
    };
    const estilo = getComputedStyle(elemento);
    const texto = luminosidad(estilo.color);
    const fondo = luminosidad(estilo.backgroundColor);
    if (texto === null || fondo === null) return 0;
    return (Math.max(texto, fondo) + 0.05) / (Math.min(texto, fondo) + 0.05);
  });
  expect(relacion, 'El contraste real en forced-colors debe ser >= 3:1').toBeGreaterThanOrEqual(3);
  await expect(page.getByRole('note')).toContainText('NO_MEDIDO');
});
test('sesión 403 sintética oculta decisiones y mensaje interno', async ({page}) => {
  await mockSesion(page,false);
  await page.goto('/plataforma/respuesta-incidentes-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.incident-review li')).toHaveCount(0);
  await expect(page.getByText('INTERNAL_ONLY_REJECTED')).toHaveCount(0);
});
