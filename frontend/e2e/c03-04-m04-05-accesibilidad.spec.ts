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
  await salto.focus();
  await expect(salto).toBeFocused();
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
  const colors=await page.locator('.incident-guide').evaluate(el=>{
    const style=getComputedStyle(el);return {color:style.color,background:style.backgroundColor};
  });
  expect(colors.color).not.toBe('transparent');
  expect(colors.background).not.toBe('transparent');
  await expect(page.getByRole('note')).toContainText('NO_MEDIDO');
});
test('sesión 403 sintética oculta decisiones y mensaje interno', async ({page}) => {
  await mockSesion(page,false);
  await page.goto('/plataforma/respuesta-incidentes-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.incident-review li')).toHaveCount(0);
  await expect(page.getByText('INTERNAL_ONLY_REJECTED')).toHaveCount(0);
});
