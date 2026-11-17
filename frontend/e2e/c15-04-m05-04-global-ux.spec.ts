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
  // A0.2: 20 rutas declaradas; solo guardas anonimas Angular, no pruebas API/tenant.
  test('40 navegaciones privadas anonimas en 20 rutas y dos viewports',async({page})=>{
    test.setTimeout(240_000);
    const rutas=["/plataforma/auditoria-ux-v5","/plataforma/inventario-rutas-v19","/plataforma/mensajeria-multicanal-v19","/plataforma/observabilidad-v5","/plataforma/consola-global-v5","/dashboard","/productos","/productos/1/editar","/clientes/nuevo","/usuarios/1","/roles/1/editar","/inventario/ajustes/1","/inventario/transferencias/1/editar","/inventario/conteos/1","/compras/1/three-way-match","/facturas-proveedor/1/editar","/pedidos-venta/1/editar","/cuentas-por-cobrar","/cuentas-por-pagar","/configuracion"];
    const mutaciones:string[]=[];
    page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))mutaciones.push(r.method()+' '+r.url());});
    for(const width of [375,1280]){
      await page.setViewportSize({width,height:840});
      for(const ruta of rutas){
        await test.step(width+'px '+ruta,async()=>{
          await page.goto(ruta,{waitUntil:'domcontentloaded'});
          await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
          await expect(page.locator('#login-title')).toBeVisible();
          await noOverflow(page);
        });
      }
    }
    expect(mutaciones).toEqual([]);
  });
  test('login: mostrar y ocultar contrasena no genera escrituras',async({page})=>{
    const mutaciones:string[]=[];
    page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))mutaciones.push(r.method());});
    await page.goto('/login');
    const entrada=page.locator('input[formcontrolname="password"]');
    await expect(entrada).toHaveAttribute('type','password');
    await page.getByRole('button',{name:'Mostrar contraseña'}).click();
    await expect(entrada).toHaveAttribute('type','text');
    await page.getByRole('button',{name:'Ocultar contraseña'}).click();
    await expect(entrada).toHaveAttribute('type','password');
    expect(mutaciones).toEqual([]);
  });

  // A0.3: regresion UX de frontera y teclado en login real, sin submit.
  // No prueba servidor, JWT, RBAC ni tenant A/B.
  test('login: limites 50/128 caracteres y bloqueo sin solicitudes POST',async({page})=>{
    await page.goto('/login');
    const usuario=page.locator('input[formcontrolname="nombreUsuario"]');
    const clave=page.locator('input[formcontrolname="password"]');
    const enviar=page.getByRole('button',{name:'Ingresar al sistema'});
    const escrituras:string[]=[];
    page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))escrituras.push(r.method());});
    await expect(enviar).toBeDisabled();
    await usuario.fill('u'.repeat(51));
    await clave.fill('p'.repeat(128));
    await expect(enviar).toBeDisabled();
    await usuario.fill('u'.repeat(50));
    await expect(enviar).toBeEnabled();
    await clave.fill('p'.repeat(129));
    await expect(enviar).toBeDisabled();
    await clave.fill('p'.repeat(128));
    await expect(enviar).toBeEnabled();
    await clave.fill('');
    await expect(enviar).toBeDisabled();
    expect(escrituras).toEqual([]);
  });
  test('login: Tab real desde inicio y mostrar contrasena con Espacio',async({page})=>{
    await page.goto('/login');
    const recorrido:string[]=[];
    for(let i=0;i<16;i++){
      await page.keyboard.press('Tab');
      const actual=await page.evaluate(()=>{
        const el=document.activeElement;
        return el?.getAttribute('formcontrolname')??el?.getAttribute('aria-label')??el?.tagName??'SIN_FOCO';
      });
      recorrido.push(actual);
      if(actual==='Mostrar contraseña')break;
    }
    expect(recorrido).toContain('nombreUsuario');
    expect(recorrido).toContain('password');
    expect(recorrido).toContain('Mostrar contraseña');
    expect(recorrido.indexOf('nombreUsuario')).toBeLessThan(recorrido.indexOf('password'));
    expect(recorrido.indexOf('password')).toBeLessThan(recorrido.indexOf('Mostrar contraseña'));
    await expect(page.getByRole('button',{name:'Mostrar contraseña'})).toHaveAttribute('aria-pressed','false');
    await page.keyboard.press('Space');
    await expect(page.locator('input[formcontrolname="password"]')).toHaveAttribute('type','text');
    await expect(page.getByRole('button',{name:'Ocultar contraseña'})).toHaveAttribute('aria-pressed','true');
  });

  // A0.4: tres rutas publicas reales; no confundir con autorizacion backend ni compras.
  test('storefront publico: 3 rutas, seis navegaciones y enlaces skip',async({page})=>{
    test.setTimeout(120_000);
    const rutas:[string,string][]=[
      ['/tienda','#contenido-principal'],
      ['/tienda/productos','#catalogo-productos'],
      ['/tienda/categorias','#contenido-categorias']
    ];
    const escrituras:string[]=[];
    page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))escrituras.push(r.method()+' '+r.url());});
    for(const width of [375,1280]){
      await page.setViewportSize({width,height:840});
      for(const [ruta,principal] of rutas){
        await test.step(width+'px '+ruta,async()=>{
          await page.goto(ruta,{waitUntil:'domcontentloaded'});
          await expect(page.locator(principal)).toBeVisible();
          await expect(page.locator('a.skip-link').first()).toHaveAttribute('href',ruta+principal);
          await expect(page.getByRole('heading',{level:1})).toHaveCount(1);
          await noOverflow(page);
        });
      }
    }
    expect(escrituras).toEqual([]);
  });

  // A0.5: pruebas de teclado real para tienda publica y dialog movil.
  // Contrato regresivo fase2: skip-link conserva pathname real, no solo hash.
  // No son evidencia de JWT, RBAC, MySQL, WCAG global o autorizacion backend.
  test('storefront: Tab y Enter sobre skip-link de tres rutas publicas',async({context})=>{
    const routes:[string,string][]=[
      ['/tienda','#contenido-principal'],
      ['/tienda/productos','#catalogo-productos'],
      ['/tienda/categorias','#contenido-categorias']
    ];
    for(const [url,target] of routes){
      const page=await context.newPage(); // Fresh tab guarantees real initial keyboard sequence.
      try {
      await page.goto(url,{waitUntil:'domcontentloaded'});
      const skip=page.locator('a.skip-link').first();
      await page.keyboard.press('Tab');
      await expect(skip).toBeFocused();
      await expect(skip).toHaveAttribute('href',url+target);
      expect(await skip.evaluate(el=>el.getBoundingClientRect().top)).toBeGreaterThanOrEqual(0);
      await page.keyboard.press('Enter');
      await expect(page.locator(target)).toBeVisible();
      await expect.poll(()=>page.evaluate(()=>location.hash)).toBe(target);
      expect(await page.evaluate(()=>location.pathname)).toBe(url);
      } finally { await page.close(); }
    }
  });

  test('storefront: menu modal movil con Escape y retorno de foco',async({page})=>{
    await page.setViewportSize({width:375,height:840});
    const writes:string[]=[];
    page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method()+' '+r.url());});
    await page.goto('/tienda',{waitUntil:'domcontentloaded'});
    const opener=page.getByRole('button',{name:'Abrir navegación'});
    await expect(opener).toBeVisible();
    await opener.click();
    const menu=page.locator('#storefront-menu-movil');
    await expect(menu).toBeVisible();
    await expect(opener).toHaveAttribute('aria-expanded','true');
    await expect(menu.getByRole('link',{name:'Inicio'})).toBeFocused();
    await page.keyboard.press('Escape');
    await expect(menu).toBeHidden();
    await expect(opener).toHaveAttribute('aria-expanded','false');
    await expect(opener).toBeFocused();
    await noOverflow(page);
    expect(writes).toEqual([]);
  });

  // A0.11: seis navegaciones en otras tres rutas publicas reales. Sin JWT,
  // cliente autenticado, pedidos ni efectos externos; no equivale a WCAG global.
  test('storefront publico: ofertas, carrito y cuenta sin escrituras en dos viewports', async ({context}) => {
    test.setTimeout(150_000);
    const rutas: [string, string][] = [
      ['/tienda/ofertas', 'Ofertas'],
      ['/tienda/carrito', 'Mi carrito'],
      ['/tienda/cuenta', 'Compra como invitado o guarda tus preferencias']
    ];
    const mutaciones: string[] = [];
    for (const width of [375, 1280]) {
      for (const [ruta, titulo] of rutas) {
        const page = await context.newPage();
        try {
          await page.setViewportSize({width, height: 840});
          page.on('request', request => {
            if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
              mutaciones.push(request.method() + ' ' + request.url());
            }
          });
          await test.step(width + 'px ' + ruta, async () => {
            await page.goto(ruta, {waitUntil: 'domcontentloaded'});
            await expect(page.getByRole('heading', {level: 1, name: titulo, exact: true})).toBeVisible();
            await noOverflow(page);
            if (ruta === '/tienda/cuenta') {
              await expect(page.getByRole('tab', {name: 'Iniciar sesión'})).toHaveAttribute('aria-selected', 'true');
              await expect(page.getByRole('button', {name: 'Iniciar sesión'})).toBeDisabled();
              await page.getByRole('tab', {name: 'Crear cuenta'}).click();
              await expect(page.getByRole('tab', {name: 'Crear cuenta'})).toHaveAttribute('aria-selected', 'true');
              await expect(page.getByRole('button', {name: 'Crear cuenta'})).toBeDisabled();
            }
          });
        } finally {
          await page.close();
        }
      }
    }
    expect(mutaciones).toEqual([]);
  });

});
