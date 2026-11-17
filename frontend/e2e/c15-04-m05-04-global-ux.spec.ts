import { expect, test, type Page } from '@playwright/test';
// C15-04.M05.04: A1 R2 INCREMENTAL en navegador real, sesión HTTP simulada.
// No certifica WCAG global ni JWT/RBAC/tenant backend.
const widths=[320,375,768,1280];
const headers={'Access-Control-Allow-Origin':'http://127.0.0.1:4200','Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id','Access-Control-Allow-Methods':'GET, OPTIONS'};
async function auth(page:Page,status=200){
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','c1504m0504-no-jwt');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/,async r=>{
    if(r.request().method()==='OPTIONS'){await r.fulfill({status:204,headers});return;}
    await r.fulfill({status,headers,contentType:'application/json',body:JSON.stringify({
      success:status===200,
      data:status===200?{nombreUsuario:'QA simulado',rolNombre:'Plataforma',ambito:'Plataforma',esAdministrador:true}:null,
      message:status===200?'':'PRIVATE_ERROR_DO_NOT_RENDER'
    })});
  });
}
async function noOverflow(page:Page){
  const x=await page.evaluate(()=>({a:document.documentElement.scrollWidth,b:window.innerWidth}));
  expect(x.a).toBeLessThanOrEqual(x.b+1);
}
test.describe('C15-04.M05.04 UX real, incremento no global',()=>{
  test.describe.configure({retries:0});
  test('login: labels, campos y ancho 320/375/768/1280',async({page})=>{
    for(const width of widths){
      await page.setViewportSize({width,height:800}); await page.goto('/login');
      await expect(page.locator('#login-title')).toHaveText('Iniciar sesión');
      await expect(page.locator('input[formcontrolname="nombreUsuario"]')).toBeVisible();
      await expect(page.locator('input[formcontrolname="password"]')).toBeVisible();
      await expect(page.locator('mat-label').filter({hasText:'Nombre de usuario'})).toBeVisible();
      await noOverflow(page);
    }
  });
  test('superficies privadas: responsive, 5 NO_MEDIDO, cero mutaciones',async({page})=>{
    await auth(page);const writes:string[]=[];
    page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method());});
    for(const width of widths){
      await page.setViewportSize({width,height:800});await page.goto('/plataforma/auditoria-ux-v5');
      await expect(page.locator('[data-testid="ux-audit-v19"]')).toBeVisible();
      await expect(page.locator('[data-testid="ux-surface"][data-state="NO_MEDIDO"]')).toHaveCount(5);
      await expect(page.getByRole('heading',{level:1})).toHaveCount(1);
      await noOverflow(page);
    }
    expect(writes).toEqual([]);
  });
  test('Tab real, foco visible y contraste puntual del h3 >=4.5',async({page})=>{
    await auth(page);await page.goto('/plataforma/auditoria-ux-v5');
    const link=page.getByRole('link',{name:'Volver a administración global'});
    await expect(link).toBeVisible();
    let focused=false;
    for(let i=0;i<24;i++){await page.keyboard.press('Tab');focused=await link.evaluate(el=>el===document.activeElement);if(focused)break;}
    expect(focused).toBe(true);
    const outline=await link.evaluate(el=>getComputedStyle(el).outlineWidth);
    expect(parseFloat(outline)).toBeGreaterThan(0);
    const c=await page.locator('.ux-surface').first().evaluate(el=>({
      fg:getComputedStyle(el.querySelector('h3')!).color,bg:getComputedStyle(el).backgroundColor
    }));
    const lum=(str:string)=>{
      const m=str.match(/^rgba?\(\s*(\d+),\s*(\d+),\s*(\d+)/);
      if(!m)throw Error('computed color not rgb: '+str);
      const a=m.slice(1,4).map(Number).map(v=>{let x=v/255;return x<=0.04045?x/12.92:((x+0.055)/1.055)**2.4;});
      return .2126*a[0]+.7152*a[1]+.0722*a[2];
    };
    const a=lum(c.fg),b=lum(c.bg);
    expect((Math.max(a,b)+.05)/(Math.min(a,b)+.05)).toBeGreaterThanOrEqual(4.5);
  });
  test('403 y ERP anónimo no muestran contenido privado',async({page})=>{
    await auth(page,403);
    await page.goto('/plataforma/auditoria-ux-v5');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_ERROR_DO_NOT_RENDER')).toHaveCount(0);
  });
  test('ERP dashboard sin autenticacion usa contexto limpio separado',async({page})=>{
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('app-dashboard')).toHaveCount(0);
  });
});
