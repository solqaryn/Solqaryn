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
  test('storefront publico V23: 15 capturas y foco real en cinco anchos',async({page},testInfo)=>{
    test.setTimeout(180_000);
    const rutas:[string,string][]=[
      ['/tienda','#contenido-principal'],
      ['/tienda/productos','#catalogo-productos'],
      ['/tienda/categorias','#contenido-categorias']
    ];
    const escrituras:string[]=[];
    page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))escrituras.push(r.method()+' '+r.url());});
    for(const width of [320,390,768,1280,1440]){
      await page.setViewportSize({width,height:840});
      for(const [ruta,principal] of rutas){
        await test.step(width+'px '+ruta,async()=>{
          await page.goto(ruta,{waitUntil:'domcontentloaded'});
          await expect(page.locator(principal)).toBeVisible();
          await expect(page.locator('a.skip-link').first()).toHaveAttribute('href',ruta+principal);
          await expect(page.getByRole('heading',{level:1})).toHaveCount(1);
          await page.keyboard.press('Tab');
          await expect(page.locator('a.skip-link').first()).toBeFocused();
          await noOverflow(page);
          // Captura real de ruta publica sin credenciales: evidencia A0.91,
          // nunca reemplazo de pruebas JWT, role matrix o WCAG global.
          const slug = ruta.replaceAll('/', '_').replace(/^_/, '');
          const screenshot = await page.screenshot({
            path: testInfo.outputPath(`v23-a091-${slug}-${width}.png`),
            fullPage: true,
            animations: 'disabled'
          });
          expect(screenshot.length).toBeGreaterThan(1_200);
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

  // A0.12: accesibilidad ARIA tablist en cuenta; keyboard real, no .focus() en test.
  test('cuenta: ArrowRight/Left Home/End, foco, panel y cero escrituras',async({context})=>{
    test.setTimeout(150_000);
    for(const width of [375,1280]){
      const page=await context.newPage();
      const writes:string[]=[];
      try{
        await page.setViewportSize({width,height:840});
        page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method()+' '+r.url());});
        await page.goto('/tienda/cuenta',{waitUntil:'domcontentloaded'});
        const login=page.getByRole('tab',{name:'Iniciar sesión'});
        const registro=page.getByRole('tab',{name:'Crear cuenta'});
        await expect(login).toBeVisible();
        await expect(login).toHaveAttribute('tabindex','0');
        await expect(registro).toHaveAttribute('tabindex','-1');
        let focused=false;
        for(let i=0;i<48;i++){
          await page.keyboard.press('Tab');
          focused=await login.evaluate(el=>el===document.activeElement);
          if(focused)break;
        }
        expect(focused).toBe(true);
        await page.keyboard.press('ArrowRight');
        await expect(registro).toBeFocused();
        await expect(registro).toHaveAttribute('aria-selected','true');
        await expect(registro).toHaveAttribute('tabindex','0');
        await expect(login).toHaveAttribute('tabindex','-1');
        await expect(page.getByRole('tabpanel',{name:'Crear cuenta'})).toBeVisible();
        const outline=await registro.evaluate(el=>parseFloat(getComputedStyle(el).outlineWidth));
        expect(outline).toBeGreaterThan(0);
        await page.keyboard.press('ArrowLeft');
        await expect(login).toBeFocused();
        await expect(page.getByRole('tabpanel',{name:'Iniciar sesión'})).toBeVisible();
        await page.keyboard.press('End');
        await expect(registro).toBeFocused();
        await page.keyboard.press('Home');
        await expect(login).toBeFocused();
        await noOverflow(page);
      }finally{await page.close();}
      expect(writes).toEqual([]);
    }
  });

  // A0.13: defecto real P2: la cuenta usaba el skip-link heredado #catalogo
  // sin destino. El navegador real debe conservar pathname y llegar al main.
  test('cuenta: skip-link real con Tab/Enter, hash valido y cero mutaciones',async({context})=>{
    test.setTimeout(120_000);
    for(const width of [375,1280]){
      const page=await context.newPage();
      const writes:string[]=[];
      try{
        await page.setViewportSize({width,height:840});
        page.on('request',r=>{
          if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method()+' '+r.url());
        });
        await page.goto('/tienda/cuenta',{waitUntil:'domcontentloaded'});
        const skip=page.locator('a.skip-link').first();
        await expect(skip).toHaveAttribute('href','/tienda/cuenta#cuenta-principal');
        await page.keyboard.press('Tab');
        await expect(skip).toBeFocused();
        await page.keyboard.press('Enter');
        await expect.poll(()=>page.evaluate(()=>location.pathname)).toBe('/tienda/cuenta');
        await expect.poll(()=>page.evaluate(()=>location.hash)).toBe('#cuenta-principal');
        await expect(page.locator('main#cuenta-principal')).toBeVisible();
        await expect(page.locator('main#cuenta-principal')).toHaveAttribute('tabindex','-1');
        await noOverflow(page);
      }finally{await page.close();}
      expect(writes).toEqual([]);
    }
  });


  // A0.14: cobertura keyboard real de rutas compartidas, no WCAG total.
  // Ninguna navegacion realiza un pedido ni solicita mutaciones.
  test('storefront: skip-link ofertas/carrito con Tab Enter en dos viewports', async ({context}) => {
    test.setTimeout(150_000);
    const rutas: [string, string][] = [
      ['/tienda/ofertas', '#catalogo-productos'],
      ['/tienda/carrito', '#carrito-principal']
    ];
    for (const width of [375, 1280]) {
      for (const [ruta, destino] of rutas) {
        const page = await context.newPage();
        const mutaciones: string[] = [];
        try {
          await page.setViewportSize({width, height: 840});
          page.on('request', request => {
            if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
              mutaciones.push(request.method() + ' ' + request.url());
            }
          });
          await page.goto(ruta, {waitUntil: 'domcontentloaded'});
          const salto = page.locator('a.skip-link').first();
          await expect(salto).toHaveAttribute('href', ruta + destino);
          await page.keyboard.press('Tab');
          await expect(salto).toBeFocused();
          await page.keyboard.press('Enter');
          await expect.poll(() => page.evaluate(() => location.pathname)).toBe(ruta);
          await expect.poll(() => page.evaluate(() => location.hash)).toBe(destino);
          const principal = page.locator('main' + destino);
          await expect(principal).toBeVisible();
          await expect(principal).toHaveAttribute('tabindex', '-1');
          await noOverflow(page);
        } finally {
          await page.close();
        }
        expect(mutaciones).toEqual([]);
      }
    }
  });


  // A0.15: linea base Web Vitals en Chromium REAL, muestra publica sin SLA.
  // NO_MEDIDO explicito cuando el navegador no reporta LCP/FCP; no es WCAG global.
  test('muestra Web Vitals real: tienda y login en 375/1280', async ({context}) => {
    test.setTimeout(150_000);
    for (const width of [375, 1280]) {
      for (const ruta of ['/tienda', '/login']) {
        const page = await context.newPage();
        try {
          await page.setViewportSize({width, height: 840});
          await page.addInitScript(() => {
            const muestra = {lcp: null as number | null, cls: 0};
            (window as typeof window & {__sqUxMuestra?: typeof muestra}).__sqUxMuestra = muestra;
            try {
              new PerformanceObserver(items => {
                const ultimo = items.getEntries().at(-1);
                if (ultimo) muestra.lcp = ultimo.startTime;
              }).observe({type: 'largest-contentful-paint', buffered: true});
            } catch { /* NO_MEDIDO: no se inventa LCP */ }
            try {
              new PerformanceObserver(items => {
                for (const item of items.getEntries()) {
                  const movimiento = item as PerformanceEntry & {value?: number; hadRecentInput?: boolean};
                  if (!movimiento.hadRecentInput && typeof movimiento.value === 'number') {
                    muestra.cls += movimiento.value;
                  }
                }
              }).observe({type: 'layout-shift', buffered: true});
            } catch { /* CLS no certificado en navegador sin soporte */ }
          });
          await page.goto(ruta, {waitUntil: 'domcontentloaded'});
          if (ruta === '/login') {
            await expect(page.locator('#login-title')).toBeVisible();
          } else {
            await expect(page.getByRole('heading', {level: 1}).first()).toBeVisible();
          }
          await page.waitForTimeout(250);
          const muestra = await page.evaluate(() => {
            const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
            const fcp = performance.getEntriesByType('paint').find(e => e.name === 'first-contentful-paint');
            const vitals = (window as typeof window & {__sqUxMuestra?: {lcp:number|null;cls:number}}).__sqUxMuestra;
            return {
              ttfbMs: nav ? nav.responseStart - nav.requestStart : null,
              fcpMs: fcp?.startTime ?? null,
              lcpMs: vitals?.lcp ?? null,
              cls: vitals?.cls ?? null
            };
          });
          expect(muestra.ttfbMs, 'Navigation Timing real').not.toBeNull();
          expect(Number.isFinite(muestra.ttfbMs)).toBe(true);
          expect(muestra.ttfbMs!).toBeGreaterThanOrEqual(0);
          console.log('[C15-04.M05.04 PERF_SAMPLE]', JSON.stringify({ruta, width, ...muestra}));
          await noOverflow(page);
        } finally {
          await page.close();
        }
      }
    }
  });


  // A0.17: landmarks, skip-link y responsividad de seis superficies publicas
  // en cuatro viewports reales (24 navegaciones). No sustituye WCAG global,
  // JWT firmado, RBAC ni aislamiento A/B del backend.
  test('24 navegaciones storefront: landmark y skip-link en 320/375/768/1280',async({context})=>{
    test.setTimeout(240_000);
    const rutas: [string,string][] = [
      ['/tienda','#contenido-principal'],
      ['/tienda/productos','#catalogo-productos'],
      ['/tienda/categorias','#contenido-categorias'],
      ['/tienda/ofertas','#catalogo-productos'],
      ['/tienda/carrito','#carrito-principal'],
      ['/tienda/cuenta','#cuenta-principal']
    ];
    const escrituras:string[]=[];
    for(const width of [320,375,768,1280]){
      for(const [ruta,destino] of rutas){
        const page=await context.newPage();
        try{
          await page.setViewportSize({width,height:840});
          page.on('request',request=>{
            if(!['GET','HEAD','OPTIONS'].includes(request.method())){
              escrituras.push(request.method()+' '+request.url());
            }
          });
          await test.step(width+'px '+ruta,async()=>{
            await page.goto(ruta,{waitUntil:'domcontentloaded'});
            const principal=page.locator('main'+destino);
            await expect(principal).toBeVisible();
            await expect(page.getByRole('heading',{level:1})).toHaveCount(1);
            const salto=page.locator('a.skip-link').first();
            await expect(salto).toHaveAttribute('href',ruta+destino);
            await noOverflow(page);
            if(width===320||width===768){
              await page.keyboard.press('Tab');
              await expect(salto).toBeFocused();
              await page.keyboard.press('Enter');
              await expect.poll(()=>page.evaluate(()=>location.pathname)).toBe(ruta);
              await expect.poll(()=>page.evaluate(()=>location.hash)).toBe(destino);
              await expect(principal).toBeVisible();
            }
          });
        }finally{await page.close();}
      }
    }
    expect(escrituras).toEqual([]);
  });


  // A0.18: fronteras de formularios PUBLICOS de cliente con teclado y DOM real.
  // Valida bloqueo de envio pero nunca ejecuta login/registro ni crea datos.
  test('cuenta: validacion negativa y frontera de formularios sin POST',async({context})=>{
    test.setTimeout(150_000);
    for(const width of [375,1280]){
      const page=await context.newPage();
      const writes:string[]=[];
      try{
        await page.setViewportSize({width,height:840});
        page.on('request',r=>{
          if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method()+' '+r.url());
        });
        await page.goto('/tienda/cuenta',{waitUntil:'domcontentloaded'});
        const login=page.locator('#cuenta-panel-login');
        await expect(login).toBeVisible();
        const correo=login.locator('input[formcontrolname="correo"]');
        const clave=login.locator('input[formcontrolname="clave"]');
        const enviar=login.locator('button[type="submit"]');
        await expect(correo).toHaveAttribute('autocomplete','email');
        await expect(clave).toHaveAttribute('autocomplete','current-password');
        await expect(enviar).toBeDisabled();
        await correo.fill('correo-invalido');
        await expect(login.locator('#cuenta-login-correo-error')).toBeVisible();
        await expect(correo).toHaveAttribute('aria-invalid','true');
        await expect(correo).toHaveAttribute('aria-describedby','cuenta-login-correo-error');
        await clave.fill('p'.repeat(10));
        await expect(enviar).toBeDisabled();
        await correo.fill('cliente@example.test');
        await expect(login.locator('#cuenta-login-correo-error')).toHaveCount(0);
        await clave.fill('p'.repeat(9));
        await expect(login.locator('#cuenta-login-clave-error')).toBeVisible();
        await expect(clave).toHaveAttribute('aria-invalid','true');
        await expect(enviar).toBeDisabled();
        await clave.fill('p'.repeat(10));
        await expect(login.locator('#cuenta-login-clave-error')).toHaveCount(0);
        await expect(enviar).toBeEnabled();

        await page.getByRole('tab',{name:'Crear cuenta'}).click();
        const registro=page.locator('#cuenta-panel-registro');
        await expect(registro).toBeVisible();
        const nombre=registro.locator('input[formcontrolname="nombre"]');
        const correoNuevo=registro.locator('input[formcontrolname="correo"]');
        const claveNueva=registro.locator('input[formcontrolname="clave"]');
        const crear=registro.locator('button[type="submit"]');
        await expect(claveNueva).toHaveAttribute('autocomplete','new-password');
        await expect(claveNueva).toHaveAttribute('aria-describedby','clave-ayuda');
        await expect(registro.locator('#clave-ayuda')).toBeVisible();
        await correoNuevo.fill('cliente@example.test');
        await claveNueva.fill('p'.repeat(10));
        await expect(crear).toBeDisabled(); // nombre ausente
        await nombre.click();
        await page.keyboard.press('Tab');
        await expect(registro.locator('#cuenta-registro-nombre-error')).toBeVisible();
        await nombre.fill('Cliente de prueba');
        await expect(registro.locator('#cuenta-registro-nombre-error')).toHaveCount(0);
        await claveNueva.fill('p'.repeat(9));
        await expect(registro.locator('#cuenta-registro-clave-error')).toBeVisible();
        await expect(claveNueva).toHaveAttribute('aria-describedby','clave-ayuda cuenta-registro-clave-error');
        await expect(crear).toBeDisabled(); // contrasena bajo minimo
        await claveNueva.fill('p'.repeat(10));
        await expect(registro.locator('#cuenta-registro-clave-error')).toHaveCount(0);
        await expect(crear).toBeEnabled();
        await noOverflow(page);
      }finally{await page.close();}
      expect(writes).toEqual([]);
    }
  });


  // A0.20: contraste legible de error de cliente en Chromium, sin proveedor.
  // Se mide contra el fondo realmente computado del panel. No certifica JWT ni WCAG global.
  test('cuenta: error de correo y contraste real de texto a 375/1280',async({context})=>{
    test.setTimeout(150_000);
    for(const width of [375,1280]){
      const page=await context.newPage();
      const writes:string[]=[];
      try{
        await page.setViewportSize({width,height:840});
        page.on('request',r=>{
          if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method()+' '+r.url());
        });
        await page.goto('/tienda/cuenta',{waitUntil:'domcontentloaded'});
        const correo=page.locator('#cuenta-panel-login input[formcontrolname="correo"]');
        await correo.fill('invalido');
        const error=page.locator('#cuenta-login-correo-error');
        await expect(error).toBeVisible();
        await expect(correo).toHaveAttribute('aria-describedby','cuenta-login-correo-error');
        const sample=await error.evaluate(el=>{
          const panel=el.closest('.panel');
          if(!panel)throw new Error('No existe panel de error');
          const foreground=getComputedStyle(el).color;
          const background=getComputedStyle(panel).backgroundColor;
          const luminance=(css:string)=>{
            const match=css.match(/^rgba?\(\s*(\d+(?:\.\d+)?)[,\s]+(\d+(?:\.\d+)?)[,\s]+(\d+(?:\.\d+)?)/);
            if(!match)throw new Error('Color CSS no medible: '+css);
            const channels=match.slice(1,4).map(Number).map(v=>{
              const c=v/255;return c<=0.04045?c/12.92:((c+0.055)/1.055)**2.4;
            });
            return channels[0]*.2126+channels[1]*.7152+channels[2]*.0722;
          };
          const a=luminance(foreground),b=luminance(background);
          return {ratio:(Math.max(a,b)+.05)/(Math.min(a,b)+.05),foreground,background};
        });
        console.log('[C15-04.M05.04 CONTRASTE_PUNTUAL]',JSON.stringify({width,...sample}));
        expect(sample.ratio,'Texto de error WCAG AA 4.5:1 contra panel').toBeGreaterThanOrEqual(4.5);
        for(const name of ['Iniciar sesión','Crear cuenta']){
          const tab=page.getByRole('tab',{name});
          const height=await tab.evaluate(el=>el.getBoundingClientRect().height);
          expect(height,'Tab de al menos 44px').toBeGreaterThanOrEqual(44);
        }
        await noOverflow(page);
      }finally{await page.close();}
      expect(writes).toEqual([]);
    }
  });


  // A0.21: auditoria NEGATIVA en rutas privadas directas del router real del SHA.
  // No sustituye autorizacion backend/JWT/RBAC ni prueba rutas hijas importadas.
  test('router: todas las rutas directas protegidas deniegan acceso anonimo en 375/1280',async({context})=>{
    test.setTimeout(600_000);
    const {readFileSync}=await import('node:fs');
    const fuente=readFileSync('src/app/app.routes.ts','utf8');
    const declaradas=[...fuente.matchAll(/\{\s*path:\s*'([^']+)',\s*canActivate:\s*\[(?:authGuard|plataformaGuard)/g)]
      .map(match=>match[1]);
    expect(declaradas.length,'Cantidad minima de rutas declaradas con guard directo').toBeGreaterThanOrEqual(88);
    expect(new Set(declaradas).size,'No duplicar paths con guarda').toBe(declaradas.length);
    for(const clave of ['plataforma/auditoria-ux-v5','dashboard','productos/:id/editar','perfil']){
      expect(declaradas,'Falta ruta critica '+clave).toContain(clave);
    }
    const pagina=await context.newPage();
    const mutaciones:string[]=[];
    pagina.on('request',solicitud=>{
      if(!['GET','HEAD','OPTIONS'].includes(solicitud.method())){
        mutaciones.push(solicitud.method()+' '+solicitud.url());
      }
    });
    try{
      for(const width of [375,1280]){
        await pagina.setViewportSize({width,height:840});
        for(const declarada of declaradas){
          const ruta='/'+declarada.split('/').map(segmento=>segmento.startsWith(':')?'999999':segmento).join('/');
          await test.step(width+'px '+ruta,async()=>{
            const respuesta=await pagina.goto(ruta,{waitUntil:'domcontentloaded'});
            expect(respuesta?.status(),'HTML local SPA no autoriza el destino').toBe(200);
            await expect.poll(()=>new URL(pagina.url()).pathname).toBe('/login');
            await expect(pagina.locator('#login-title')).toBeVisible();
            await noOverflow(pagina);
          });
        }
      }
    }finally{
      await pagina.close();
    }
    expect(mutaciones,'Anonimato: cero peticiones HTTP mutantes').toEqual([]);
    console.log('[C15-04.M05.04 RUTAS_DIRECTAS_NEGATIVAS]',JSON.stringify({
      declaraciones:declaradas.length, dinamicas:declaradas.filter(r=>r.includes(':')).length,
      anchos:[375,1280], navegaciones:declaradas.length*2,
      alcance:'guarda Angular anonima; JWT/tenant A-B/backend NO_MEDIDO'
    }));
  });


  // A0.22: ocho archivos de rutas importadas; la prueba usa guardas reales de Angular.
  // No representa JWT firmado ni aislamiento autenticado en APIs MySQL.
  test('router: 51 rutas importadas exigen login anonimo en 375/1280',async({context})=>{
    test.setTimeout(480_000);
    const {readFileSync}=await import('node:fs');
    const archivos=[
      'src/app/features/inventario/ajustes.routes.ts',
      'src/app/features/compras/facturas-proveedor/facturas-proveedor.routes.ts',
      'src/app/features/pedidos-venta/pedidos-venta.routes.ts',
      'src/app/features/inventario/transferencias.routes.ts',
      'src/app/features/inventario/conteos.routes.ts',
      'src/app/features/inventario/reservas.routes.ts',
      'src/app/features/finanzas/cuentas-por-cobrar/cuentas-por-cobrar.routes.ts',
      'src/app/features/finanzas/cuentas-por-pagar/cuentas-por-pagar.routes.ts'
    ];
    const declaradas:string[]=[];
    for(const archivo of archivos){
      const fuente=readFileSync(archivo,'utf8');
      const rutas=[...fuente.matchAll(/\bpath\s*:\s*'([^']+)'/g)].map(m=>m[1]);
      const protegidas=[...fuente.matchAll(/\bpath\s*:\s*'([^']+)'[,\s]*canActivate\s*:\s*\[\s*authGuard\b/g)].map(m=>m[1]);
      expect(protegidas,'Todas las rutas importadas requieren authGuard: '+archivo).toEqual(rutas);
      declaradas.push(...protegidas);
    }
    expect(declaradas.length,'Inventario de ocho archivos importados').toBeGreaterThanOrEqual(51);
    expect(new Set(declaradas).size,'Rutas importadas duplicadas').toBe(declaradas.length);
    const pagina=await context.newPage();
    const mutaciones:string[]=[];
    pagina.on('request',r=>{
      if(!['GET','HEAD','OPTIONS'].includes(r.method()))mutaciones.push(r.method()+' '+r.url());
    });
    try{
      for(const width of [375,1280]){
        await pagina.setViewportSize({width,height:840});
        for(const declarada of declaradas){
          const ruta='/'+declarada.split('/').map(segmento=>segmento.startsWith(':')?'999999':segmento).join('/');
          await test.step(width+'px '+ruta,async()=>{
            const respuesta=await pagina.goto(ruta,{waitUntil:'domcontentloaded'});
            expect(respuesta?.status(),'HTML SPA no acredita permiso').toBe(200);
            await expect.poll(()=>new URL(pagina.url()).pathname).toBe('/login');
            await expect(pagina.locator('#login-title')).toBeVisible();
            await noOverflow(pagina);
          });
        }
      }
    }finally{await pagina.close();}
    expect(mutaciones,'Anonimato: cero escrituras HTTP').toEqual([]);
    console.log('[C15-04.M05.04 RUTAS_IMPORTADAS_NEGATIVAS]',JSON.stringify({
      archivos:archivos.length,rutas:declaradas.length,viewports:[375,1280],
      navegaciones:declaradas.length*2,
      alcance:'guard Angular anonima; JWT firmado, RBAC backend y tenant A/B NO_MEDIDO'
    }));
  });


  // A0.23: una redireccion a /login tambien ocurre para una ruta NO registrada.
  // Comprobar en AST de TypeScript que los ocho arrays importados son alcanzables
  // por spreads reales desde el router raiz antes de atribuir la navegacion A0.22.
  test('router: ocho modulos importados conectados por spreads AST al router raiz',async()=>{
    const {readFileSync}=await import('node:fs');
    const ts=await import('typescript');
    const modules=[
      ['AJUSTES_INVENTARIO_ROUTES','src/app/features/inventario/ajustes.routes.ts'],
      ['FACTURAS_PROVEEDOR_ROUTES','src/app/features/compras/facturas-proveedor/facturas-proveedor.routes.ts'],
      ['PEDIDOS_VENTA_ROUTES','src/app/features/pedidos-venta/pedidos-venta.routes.ts'],
      ['TRANSFERENCIAS_INVENTARIO_ROUTES','src/app/features/inventario/transferencias.routes.ts'],
      ['CONTEOS_INVENTARIO_ROUTES','src/app/features/inventario/conteos.routes.ts'],
      ['RESERVAS_INVENTARIO_ROUTES','src/app/features/inventario/reservas.routes.ts'],
      ['CUENTAS_POR_COBRAR_ROUTES','src/app/features/finanzas/cuentas-por-cobrar/cuentas-por-cobrar.routes.ts'],
      ['CUENTAS_POR_PAGAR_ROUTES','src/app/features/finanzas/cuentas-por-pagar/cuentas-por-pagar.routes.ts']
    ] as const;
    function actualSpreads(path:string,variable:string):string[]{
      const source=ts.createSourceFile(path,readFileSync(path,'utf8'),ts.ScriptTarget.Latest,true,ts.ScriptKind.TS);
      const declaration=source.statements
        .filter(ts.isVariableStatement)
        .flatMap(stmt=>[...stmt.declarationList.declarations])
        .find(decl=>ts.isIdentifier(decl.name)&&decl.name.text===variable);
      expect(declaration,variable+' debe existir en '+path).toBeDefined();
      const init=declaration?.initializer;
      expect(init && ts.isArrayLiteralExpression(init),variable+' debe ser array literal').toBeTruthy();
      if(!init || !ts.isArrayLiteralExpression(init))throw new Error('Router AST no verificable: '+variable);
      return init.elements.filter(ts.isSpreadElement)
        .map(element=>{
          expect(ts.isIdentifier(element.expression),'Spread no identificable en '+variable).toBe(true);
          return element.expression.getText(source);
        });
    }
    const reachable=new Set(actualSpreads('src/app/app.routes.ts','routes'));
    for(let step=0;step<modules.length;step++){
      for(const [name,path] of modules){
        if(!reachable.has(name))continue;
        for(const nested of actualSpreads(path,name))reachable.add(nested);
      }
    }
    const missing=modules.filter(([name])=>!reachable.has(name)).map(([name])=>name);
    expect(missing,'Arrays importados muertos: login por wildcard no demuestra authGuard').toEqual([]);
    expect(modules.length).toBe(8);
    console.log('[C15-04.M05.04 ROUTER_REACHABILITY_AST]',JSON.stringify({
      source:'app.routes.ts + 8 modulos',reachable:modules.length,
      scope:'composicion estatica + negativas anonimas A0.22; JWT/RBAC tenant A/B NO_MEDIDO'
    }));
  });


  // A0.87 — fallos de sesion API simulados; Chromium real, NO prueba JWT firmado.
  // El texto de error privado y las cinco superficies no deben filtrarse tras HTTP 500/503.
  test('sesion 500/503: sin fuga privada ni mutaciones en 375/1280',async({context})=>{
    test.setTimeout(120_000);
    for(const status of [500,503]){
      for(const width of [375,1280]){
        const page=await context.newPage();
        const writes:string[]=[];
        try{
          await page.setViewportSize({width,height:800});
          await auth(page,status);
          page.on('request',req=>{
            if(!['GET','HEAD','OPTIONS'].includes(req.method()))writes.push(req.method());
          });
          await page.goto('/plataforma/auditoria-ux-v5');
          await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
          await expect(page.getByText('PRIVATE_ERROR_DO_NOT_RENDER')).toHaveCount(0);
          await noOverflow(page);
          expect(writes,'sesion fallida: cero escrituras').toEqual([]);
        }finally{
          await page.close();
        }
      }
    }
  });


  // A0.88 — sesion 401/429 simulada; las aserciones corren en Chromium real.
  // No es prueba de JWT firmado, roles del backend ni persistencia MySQL.
  test('sesion 401/429: sin render privado ni mutaciones 375/1280',async({context})=>{
    test.setTimeout(120_000);
    for(const status of [401,429]){
      for(const width of [375,1280]){
        const page=await context.newPage();
        const writes:string[]=[];
        try{
          await page.setViewportSize({width,height:800});
          await auth(page,status);
          page.on('request',req=>{
            if(!['GET','HEAD','OPTIONS'].includes(req.method()))writes.push(req.method()+' '+req.url());
          });
          await page.goto('/plataforma/auditoria-ux-v5');
          await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
          await expect(page.getByText('PRIVATE_ERROR_DO_NOT_RENDER')).toHaveCount(0);
          await noOverflow(page);
          expect(writes,'HTTP '+status+' no autoriza mutaciones').toEqual([]);
        }finally{
          await page.close();
        }
      }
    }
  });


  // A0.89: regresion adversarial de contratos de sesion HTTP 200.
  // Browser real + respuestas simuladas; NO acredita JWT firmado ni MySQL.
  test('sesion 200 malformada y data nula: sin UI privada ni mutaciones',async({context})=>{
    test.setTimeout(120_000);
    const escenarios=[
      {nombre:'json-roto',body:'{"success":true,"data":'},
      {nombre:'exito-sin-identidad',body:JSON.stringify({success:true,data:null,message:'PRIVATE_ERROR_DO_NOT_RENDER'})}
    ];
    for(const escenario of escenarios){
      for(const width of [375,1280]){
        const page=await context.newPage();
        const writes:string[]=[];
        let intercepted=0;
        try{
          await page.setViewportSize({width,height:800});
          await page.addInitScript(()=>{
            localStorage.setItem('solqaryn_token','c1504m0504-no-jwt');
            localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
          });
          await page.route(/\/plataforma\/sesion(?:\?|$)/,async route=>{
            if(route.request().method()==='OPTIONS'){
              await route.fulfill({status:204,headers});return;
            }
            intercepted++;
            await route.fulfill({status:200,headers,contentType:'application/json',body:escenario.body});
          });
          page.on('request',request=>{
            if(!['GET','HEAD','OPTIONS'].includes(request.method())){
              writes.push(request.method()+' '+request.url());
            }
          });
          await page.goto('/plataforma/auditoria-ux-v5',{waitUntil:'domcontentloaded'});
          await expect.poll(()=>intercepted,{timeout:10000}).toBeGreaterThan(0);
          await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
          await expect(page.getByText('PRIVATE_ERROR_DO_NOT_RENDER')).toHaveCount(0);
          await noOverflow(page);
          expect(writes,'HTTP 200 sin identidad valida no permite mutaciones').toEqual([]);
        }finally{
          await page.close();
        }
      }
    }
  });


  // V23 A0.92: complementar evidencia A091 con tres rutas publicas distintas.
  // Test-only: Chromium autentico, sin JWT simulado, sin HTTP mutante y sin certificacion WCAG global.
  test('storefront V23 A092: 15 capturas ofertas carrito cuenta en cinco anchos',async({page},testInfo)=>{
    test.setTimeout(240_000);
    const rutas:[string,string][]=[
      ['/tienda/ofertas','#catalogo-productos'],
      ['/tienda/carrito','#carrito-principal'],
      ['/tienda/cuenta','#cuenta-principal']
    ];
    const escrituras:string[]=[];
    page.on('request',req=>{
      if(!['GET','HEAD','OPTIONS'].includes(req.method()))escrituras.push(req.method()+' '+req.url());
    });
    for(const width of [320,390,768,1280,1440]){
      await page.setViewportSize({width,height:840});
      for(const [ruta,destino] of rutas){
        await test.step('A092 '+width+'px '+ruta,async()=>{
          await page.goto(ruta,{waitUntil:'domcontentloaded'});
          await expect(page.locator('main'+destino)).toBeVisible();
          await expect(page.getByRole('heading',{level:1})).toHaveCount(1);
          const salto=page.locator('a.skip-link').first();
          await expect(salto).toHaveAttribute('href',ruta+destino);
          await page.keyboard.press('Tab');
          await expect(salto).toBeFocused();
          await noOverflow(page);
          const slug=ruta.replaceAll('/','_').replace(/^_/,'');
          const captura=await page.screenshot({
            path:testInfo.outputPath('v23-a092-'+slug+'-'+width+'.png'),
            fullPage:true
          });
          expect(captura.byteLength,'PNG real no trivial').toBeGreaterThan(2000);
        });
      }
    }
    expect(escrituras,'storefront de lectura sin POST/PUT/PATCH/DELETE').toEqual([]);
  });



  // V23 A0.93: real Chromium text-resize probe, not a claim of native browser zoom or WCAG audit.
  // The test enlarges root text to 200% without mutating Angular runtime or backend.
  test('V23 A093: texto raiz 200% login tienda 5 anchos y sin overflow',async({context},testInfo)=>{
    test.setTimeout(300_000);
    const rutas:[string,string][]=[['/login','#login-title'],['/tienda','#contenido-principal']];
    for(const width of [320,390,768,1280,1440]){
      for(const [ruta,selector] of rutas){
        const current=await context.newPage();
        const writes:string[]=[];
        try{
          await current.setViewportSize({width,height:840});
          current.on('request',r=>{
            if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method()+' '+r.url());
          });
          await current.goto(ruta,{waitUntil:'domcontentloaded'});
          const original=await current.locator('html').evaluate(el=>parseFloat(getComputedStyle(el).fontSize));
          expect(original,'tamano de raiz CSS real').toBeGreaterThan(0);
          await current.addStyleTag({content:'html { font-size: 200% !important; }'});
          const resized=await current.locator('html').evaluate(el=>parseFloat(getComputedStyle(el).fontSize));
          expect(resized,'200% CSS text resize efectivamente aplicado').toBeGreaterThanOrEqual(original*1.9);
          await expect(current.locator(selector)).toBeVisible();
          // /login expone h2#login-title por contrato; la tienda publica expone h1.
          // Conservar la verificacion de heading real por ruta, sin fabricar un h1 para login.
          const heading = ruta==='/login' ? current.locator('h2#login-title') : current.locator('h1');
          await expect(heading,'jerarquia de heading correspondiente a ruta').toHaveCount(1);
          if(ruta==='/tienda'){
            await current.keyboard.press('Tab');
            await expect(current.locator('a.skip-link').first()).toBeFocused();
          }
          await noOverflow(current);
          const slug=ruta.replaceAll('/','_').replace(/^_/,'');
          const capture=await current.screenshot({
            path:testInfo.outputPath('v23-a093-'+slug+'-text200-'+width+'.png'),
            fullPage:true,animations:'disabled'
          });
          expect(capture.byteLength,'PNG real de texto ampliado').toBeGreaterThan(2000);
          expect(writes,'lectura sin requests mutantes').toEqual([]);
        }finally{
          await current.close();
        }
      }
    }
  });

});
