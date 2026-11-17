import {test,expect,type Page} from '@playwright/test';
// Real Chromium with synthetic session; not live backend JWT/RBAC/tenant evidence.
const route='/plataforma/experiencia-visual-v5';
async function synthetic(page:Page,allowed:boolean) {
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','c1304-synthetic-only');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/,async r=>{
    const headers={'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods':'GET, OPTIONS'};
    if(r.request().method()==='OPTIONS'){await r.fulfill({status:204,headers});return;}
    await r.fulfill({status:allowed?200:403,headers,contentType:'application/json',
      body:JSON.stringify(allowed?{success:true,data:{nombreUsuario:'Synthetic',rolNombre:'Global',ambito:'Plataforma',esAdministrador:true}}:{success:false,message:'SECRET_TENANT_B'})});
  });
}
test('anónimo y 403 no ven presets privados',async({page})=>{
  await page.goto(route);await expect(page).toHaveURL(/\/login/);
  await expect(page.locator('.visual-actions button')).toHaveCount(0);
  await synthetic(page,false);await page.goto(route);await expect(page).toHaveURL(/\/login/);
  await expect(page.getByText('SECRET_TENANT_B')).toHaveCount(0);
});
test('preset validado cambia solo apariencia y nunca envía escrituras',async({page})=>{
  await synthetic(page,true);await page.goto(route);
  await expect(page.getByRole('heading',{name:'Configuración visual permitida'})).toBeVisible();
  await page.getByTestId('c1304-contrast').click();
  await page.getByTestId('c1304-compact').click();
  await expect(page.getByTestId('c1304-visual-state')).toContainText('Contraste alto');
  await expect(page.getByTestId('c1304-visual-state')).toContainText('Compacta');
  await expect(page.locator('.visual-shell')).toHaveClass(/visual-contrast/);
  expect(await page.locator('form,input,textarea').count()).toBe(0);
});
test('320px y teclado Tab real preservan foco y acceso',async({page})=>{
  await synthetic(page,true);await page.setViewportSize({width:320,height:812});await page.goto(route);
  const link=page.getByRole('link',{name:'Volver a administración de plataforma'});
  await expect(link).toBeVisible();let reached=false;
  for(let i=0;i<100;i++){await page.keyboard.press('Tab');if(await link.evaluate(el=>document.activeElement===el)){reached=true;break;}}
  expect(reached).toBe(true);
  const d=await link.evaluate(el=>{const r=el.getBoundingClientRect(),s=getComputedStyle(el);return {w:r.width,h:r.height,outline:Number.parseFloat(s.outlineWidth)};});
  expect(d.w).toBeGreaterThanOrEqual(44);expect(d.h).toBeGreaterThanOrEqual(44);
  expect(d.outline).toBeGreaterThanOrEqual(2);
  expect(await page.evaluate(()=>document.documentElement.scrollWidth-document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
});
