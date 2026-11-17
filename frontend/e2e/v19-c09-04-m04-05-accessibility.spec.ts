import { expect, test, type Page } from '@playwright/test';

/** C09-04.M04.05 - Browser Chromium real; sesión HTTP SINTÉTICA sin JWT/SMTP/WhatsApp. */
const path='/plataforma/mensajeria-multicanal-v19';
const headers={
  'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods':'GET, OPTIONS'
};
async function usarSesion(page:Page, autorizado=true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token','c0904-m0405-fake-not-jwt');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/,async route=>{
    if(route.request().method()==='OPTIONS'){await route.fulfill({status:204,headers});return;}
    await route.fulfill({status:autorizado?200:403,headers,contentType:'application/json',
      body:JSON.stringify(autorizado
        ?{success:true,data:{nombreUsuario:'Admin',rolNombre:'Admin',ambito:'Plataforma',esAdministrador:true}}
        :{success:false,message:'PRIVATE_COMPANY_B_NO_LEAK'})});
  });
}
test.describe('C09-04.M04.05 accesibilidad real Chromium',()=>{
  test.describe.configure({retries:0});
  test('denegación anónima y 403 oculta UI y datos no autorizados',async({page})=>{
    await page.goto(path);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c0904-canales-privados')).toHaveCount(0);
    await usarSesion(page,false);
    await page.goto(path);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c0904-canales-privados')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_COMPANY_B_NO_LEAK')).toHaveCount(0);
  });
  test('320, 375, 768 y 1280 tienen estructura, etiquetas y cero overflow',async({page})=>{
    await usarSesion(page);
    for(const width of [320,375,768,1280]){
      await page.setViewportSize({width,height:812});
      await page.goto(path);
      await expect(page.locator('main[aria-labelledby="c0904-titulo"]')).toBeVisible();
      await expect(page.getByRole('heading',{level:1,name:'Mensajería multicanal'})).toBeVisible();
      await expect(page.getByRole('heading',{level:2,name:/Canales declarados/})).toBeVisible();
      await expect(page.getByTestId('c0904-canales-privados').locator('li')).toHaveCount(3);
      expect(await page.evaluate(()=>document.documentElement.scrollWidth-document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
    }
  });
  test('Tab desde inicio, foco visible real y Enter sin .focus artificial',async({page})=>{
    await usarSesion(page);
    await page.goto(path);
    const link=page.getByRole('link',{name:'Regresar a plataforma'});
    let alcanzo=false;
    for(let n=0;n<80;n++){
      await page.keyboard.press('Tab');
      if(await link.evaluate(el=>document.activeElement===el)){alcanzo=true;break;}
    }
    expect(alcanzo).toBe(true);
    const info=await link.evaluate(el=>{
      const s=getComputedStyle(el),r=el.getBoundingClientRect();
      return{outlineStyle:s.outlineStyle,outlineWidth:parseFloat(s.outlineWidth),width:r.width,height:r.height};
    });
    expect(info.outlineStyle).not.toBe('none');
    expect(info.outlineWidth).toBeGreaterThanOrEqual(2);
    expect(info.width).toBeGreaterThanOrEqual(44);
    expect(info.height).toBeGreaterThanOrEqual(44);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/plataforma(?:\?.*)?$/);
  });
  test('contraste calculado del texto principal en tema claro, semántica y sin escrituras',async({page})=>{
    await page.emulateMedia({colorScheme:'light'});
    await usarSesion(page);
    const methods:string[]=[];
    page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))methods.push(r.method());});
    await page.goto(path);
    const data=await page.getByRole('heading',{level:1}).evaluate(el=>{
      const parse=(s:string):number[]=>{
        const nums=s.match(/[\d.]+/g)?.map(Number)||[];
        return nums.slice(0,3);
      };
      const fg=parse(getComputedStyle(el).color);
      let parent:Element|null=el;
      let bg=[255,255,255];
      while(parent){
        const style=getComputedStyle(parent);
        const color=style.backgroundColor;
        const rgba=parse(color);
        const alpha=parseFloat(color.split(',')[3]||'1');
        if(rgba.length>=3 && Number.isFinite(alpha) && alpha>=1){bg=rgba;break;}
        parent=parent.parentElement;
      }
      const luminance=(rgb:number[])=>rgb.reduce((sum,n,i)=>{
        const c=n/255,v=c<=.04045?c/12.92:Math.pow((c+.055)/1.055,2.4);
        return sum+v*[.2126,.7152,.0722][i];
      },0);
      const a=luminance(fg),b=luminance(bg);
      return{ratio:(Math.max(a,b)+.05)/(Math.min(a,b)+.05),color:fg,background:bg};
    });
    expect(data.color).toHaveLength(3);
    expect(data.ratio).toBeGreaterThanOrEqual(4.5);
    await expect(page.locator('main[aria-labelledby="c0904-titulo"]')).toHaveCount(1);
    await expect(page.locator('.mensajeria form, .mensajeria textarea, .mensajeria input, .mensajeria button')).toHaveCount(0);
    expect(methods).toEqual([]);
  });
});
