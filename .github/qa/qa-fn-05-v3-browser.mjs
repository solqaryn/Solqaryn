import {createRequire} from 'node:module';
import {resolve} from 'node:path';
const require=createRequire(resolve('frontend/package.json'));
const {chromium}=require('playwright');
const {expect}=require('@playwright/test');
const chunks=[];
for await (const chunk of process.stdin) chunks.push(chunk);
const spec=JSON.parse(Buffer.concat(chunks).toString('utf8'));
const base='https://solqarynqa-nu.vercel.app';
if(spec.base!==base||process.env.GITHUB_REF!=='refs/heads/qa'||process.env.GITHUB_REPOSITORY!=='solqaryn/Solqaryn'||!['shipping','checkout'].includes(spec.mode)) throw new Error('QA browser scope');
const browser=await chromium.launch({headless:true});
let stage='START';
function mark(label){stage=label;console.log('QA_V3_UI_'+label+'=PASS');}
function requireTrue(ok){if(!ok) throw new Error('Assertion failed');}
async function overflow(page){await expect.poll(()=>page.evaluate(()=>Math.max(document.documentElement.scrollWidth,document.body.scrollWidth)-document.documentElement.clientWidth),{timeout:8000}).toBeLessThanOrEqual(2);}
async function context(viewport){
  const c=await browser.newContext({viewport,acceptDownloads:false});
  await c.route('**/*',async route=>{
    const request=route.request(),url=new URL(request.url());
    if(url.origin===base){await route.continue();return;}
    if(request.isNavigationRequest()||!['GET','HEAD'].includes(request.method())){await route.abort();return;}
    await route.continue();
  });
  return c;
}
async function login(page,identity){
  await page.goto(base+'/login');
  await page.locator('input[autocomplete="username"]').fill(identity.user);
  await page.locator('input[autocomplete="current-password"]').fill(identity.password);
  await page.locator('button[type="submit"]').click();
  await page.locator('#empresa-id').fill(String(identity.tenant));
  await page.getByRole('button',{name:'Entrar a la empresa'}).click();
  await page.waitForURL(u=>u.pathname!=='/login',{timeout:30000});
}
function scopedResponse(response,path,method,expected,tenant){
  const url=new URL(response.url());
  return url.origin===base&&url.pathname===('/api'+path)&&response.request().method()===method&&(expected==null||response.status()===expected)&&(!tenant||response.request().headers()['x-empresa-id']===String(tenant));
}
function observeQuoteTraffic(page,label){
  // Emit only a fixed endpoint label and status; never URL parameters, headers or bodies.
  function pathKind(req){
    if(req.method()!=='POST')return null;
    const url=new URL(req.url());
    if(url.origin!==base)return null;
    return ({'/api/tienda/productos/contexto':'CONTEXT','/api/tienda/checkout/validar':'QUOTE'})[url.pathname]||null;
  }
  page.on('response',r=>{const kind=pathKind(r.request());if(kind)console.log('QA_V3_UI_HTTP_'+kind+'_'+label()+'='+r.status());});
  page.on('requestfailed',r=>{const kind=pathKind(r);if(kind)console.log('QA_V3_UI_REQUEST_FAILED_'+kind+'_'+label());});
}
async function liveQuote(page,action,label,suffix,allowRateRetry){
  // Both calls are part of prepararCheckout; an unsuccessful product-context
  // response prevents the validation POST. Observe either status, not just HTTP 200.
  for(let attempt=0;attempt<(allowRateRetry?2:1);attempt++){
    stage='CHECKOUT_'+label+'_CONTEXT_'+suffix;
    const context=page.waitForResponse(r=>scopedResponse(r,'/tienda/productos/contexto','POST',null),{timeout:45000});
    const quote=page.waitForResponse(r=>scopedResponse(r,'/tienda/checkout/validar','POST',null),{timeout:65000}).then(response=>({response}),error=>({error}));
    await action();
    const cr=await context;
    let response=cr;
    if(cr.status()===200){
      stage='CHECKOUT_'+label+'_QUOTE_'+suffix;
      const result=await quote;
      if(result.error)throw result.error;
      response=result.response;
    }
    stage='CHECKOUT_'+label+'_HTTP_'+suffix;
    if(response.status()===429&&attempt===0&&allowRateRetry){
      // A real 429, not a generic timeout: respect the existing fixed one-minute
      // window, verify the UI enters its explicit error state and retry once via UI.
      const retry=page.getByRole('button',{name:'Reintentar validación',exact:true});
      await expect(retry).toBeVisible();
      await expect(page.locator('.totals')).toHaveCount(0);
      const header=response.headers()['retry-after'];
      const seconds=header&&/^\d+$/.test(header)?Number(header):60;
      requireTrue(Number.isSafeInteger(seconds)&&seconds<=120);
      console.log('QA_V3_UI_RATE_LIMIT_OBSERVED_'+label+'_'+suffix);
      await new Promise(resolve=>setTimeout(resolve,Math.max(65,seconds+2)*1000));
      // Avoid a stale waiter from the rejected context request.
      await quote;
      action=()=>retry.click();
      continue;
    }
    requireTrue(cr.status()===200&&response.status()===200&&response.headers()['x-solqaryn-environment']==='QA');
    return response;
  }
  throw new Error('Quote retry exhausted');
}
async function shipping(identity,suffix,amount,viewport){
  const c=await context(viewport);
  try{
    const page=await c.newPage();page.setDefaultTimeout(20000);
    const table=page.getByRole('table',{name:'Costos de envío de la empresa'});
    const def=spec.prefix+'-'+suffix+'-default',special=spec.prefix+'-'+suffix+'-specific';
    const row=name=>table.getByRole('row').filter({has:page.getByText(name,{exact:true})});
    const form=()=>page.locator('.solqaryn-modal--l').filter({has:page.locator('section[aria-label="Formulario de tarifa"]')});
    stage='SHIP_LOGIN_'+suffix;
    await login(page,identity);
    const load=page.waitForResponse(r=>scopedResponse(r,'/costos-envio/administracion','GET',200,identity.tenant),{timeout:30000});
    await Promise.all([load,page.goto(base+'/costos-envio')]);
    await expect(page.getByRole('heading',{name:'Costos de envío',exact:true})).toBeVisible();
    await expect(page.getByText('Sin configurar',{exact:true})).toBeVisible();
    mark('SHIP_EMPTY_'+suffix);
    async function create(name,price,isDefault){
      stage='SHIP_CREATE_'+suffix+(isDefault?'_DEFAULT':'_SPECIFIC');
      await page.getByRole('button',{name:'Nueva tarifa'}).click();
      const editor=form();
      await expect(editor).toBeVisible();
      await page.keyboard.press('Escape');
      await expect(editor).toBeVisible();
      await editor.locator('input[formcontrolname="nombre"]').fill(name);
      await editor.locator('input[formcontrolname="monto"]').fill(String(price));
      if(isDefault) await editor.getByRole('checkbox',{name:'Tarifa general predeterminada'}).check();
      else await editor.locator('input[formcontrolname="ciudad"]').fill(spec.prefix+'-city');
      const write=page.waitForResponse(r=>scopedResponse(r,'/costos-envio','POST',201,identity.tenant),{timeout:30000});
      await Promise.all([write,editor.getByRole('button',{name:'Crear',exact:true}).click()]);
      const result=await (await write).json(); requireTrue(result.success===true&&Number.isInteger(result.data.id));
      await expect(row(name)).toHaveCount(1);
      return result.data.id;
    }
    const did=await create(def,amount,true);
    await expect(row(def)).toContainText('Predeterminado');
    await expect(page.getByRole('region',{name:'Resumen de tarifas'})).toContainText(def);
    const sid=await create(special,amount+5,false);
    await expect(row(special)).toContainText('Opcional');
    const foreign=spec.prefix+'-'+(suffix==='A'?'B':'A')+'-default';
    await expect(row(foreign)).toHaveCount(0);
    stage='SHIP_SCROLL_'+suffix;
    await overflow(page);
    await expect.poll(()=>table.locator('..').evaluate(e=>e.scrollWidth<=e.clientWidth+2||['auto','scroll'].includes(getComputedStyle(e).overflowX))).toBe(true);
    mark('SHIP_TABLE_'+suffix);
    stage='SHIP_DETAIL_OPEN_'+suffix;
    await row(special).getByRole('button',{name:'Ver costo de envío '+special,exact:true}).click();
    const detail=page.locator('app-consulta-registro-dialog');
    await expect(detail).toBeVisible();
    stage='SHIP_DETAIL_CONTENT_'+suffix;
    stage='SHIP_DETAIL_TITLE_'+suffix;
    await expect(detail.getByRole('heading',{name:'Costo de envío: '+special,exact:true})).toBeVisible();
    stage='SHIP_DETAIL_READONLY_'+suffix;
    await expect(detail.locator('p.consulta-nota')).toContainText('Información de solo lectura');
    stage='SHIP_DETAIL_NAME_'+suffix;
    // The inner locator of has is evaluated RELATIVE to each candidate div.
    // Do not reuse detail.getByText here: that would search for another dialog inside the div.
    const nameField=detail.locator('dl.consulta-campos > div').filter({has:page.locator('dt').filter({hasText:/^Nombre$/})});
    await expect(nameField).toHaveCount(1);
    await expect(nameField.locator('dd')).toHaveText(special);
    stage='SHIP_DETAIL_PRICE_'+suffix;
    const priceField=detail.locator('dl.consulta-campos > div').filter({has:page.locator('dt').filter({hasText:/^Monto \(HNL\)$/})});
    await expect(priceField).toHaveCount(1);
    await expect(priceField.locator('dd')).toHaveText(String(amount+5));
    stage='SHIP_DETAIL_CLOSE_POLICY_'+suffix;
    // modalInstitucional enforces disableClose:true. Escape must not close it.
    await page.keyboard.press('Escape');
    await expect(detail).toBeVisible();
    await detail.getByRole('button',{name:'Cerrar',exact:true}).click();
    await expect(detail).toHaveCount(0);
    mark('SHIP_DETAIL_'+suffix);
    stage='SHIP_EDIT_'+suffix;
    await row(special).getByRole('button',{name:'Editar costo de envío'}).click();
    const editor=form();
    await editor.locator('input[formcontrolname="monto"]').fill(String(amount+9));
    const edit=page.waitForResponse(r=>scopedResponse(r,'/costos-envio/'+sid,'PUT',200,identity.tenant),{timeout:30000});
    await Promise.all([edit,editor.getByRole('button',{name:'Actualizar',exact:true}).click()]);
    requireTrue(Number((await (await edit).json()).data.monto)===amount+9);
    await expect(editor).toHaveCount(0);
    mark('SHIP_EDIT_'+suffix);
    for(const [action,expected,label] of [['Desactivar',false,'DISABLE'],['Activar',true,'ENABLE']]){
      stage='SHIP_'+label+'_'+suffix;
      const state=page.waitForResponse(r=>scopedResponse(r,'/costos-envio/'+sid+'/estado','PATCH',200,identity.tenant),{timeout:30000});
      await row(special).getByRole('button',{name:action+' costo de envío'}).click();
      await Promise.all([state,page.locator('.solqaryn-modal--s').getByRole('button',{name:action,exact:true}).click()]);
      requireTrue((await (await state).json()).data.activo===expected);
      await expect(row(special)).toContainText(expected?'Vigente':'Inactivo');
      mark(stage);
    }
    stage='SHIP_SIMULATE_OPEN_'+suffix;
    await page.getByRole('button',{name:'Simular tarifa',exact:true}).click();
    const simulation=page.locator('.solqaryn-modal--m').filter({has:page.locator('section[aria-label="Simulación de tarifa"]')});
    await expect(simulation).toBeVisible();
    async function verifySimulation(city,expectedId,expectedName,expectedAmount,label){
      stage='SHIP_SIMULATE_'+label+'_INPUT_'+suffix;
      const input=simulation.locator('input[formcontrolname="ciudad"]');
      await input.fill(city);
      await expect(input).toHaveValue(city);
      const button=simulation.getByRole('button',{name:'Resolver tarifa',exact:true});
      await expect(button).toBeEnabled();
      const responsePromise=page.waitForResponse(r=>scopedResponse(r,'/costos-envio/resolver','POST',null,identity.tenant),{timeout:30000});
      await button.click();
      const response=await responsePromise;
      stage='SHIP_SIMULATE_'+label+'_HTTP_'+suffix;
      console.log('QA_V3_UI_SHIP_SIMULATE_'+label+'_HTTP_'+suffix+'='+response.status());
      requireTrue(response.status()===200&&response.headers()['x-solqaryn-environment']==='QA');
      stage='SHIP_SIMULATE_'+label+'_REQUEST_'+suffix;
      const body=response.request().postDataJSON();
      requireTrue(body?.ciudad===city);
      stage='SHIP_SIMULATE_'+label+'_CONTRACT_'+suffix;
      const result=await response.json();
      requireTrue(result.success===true&&Number.isInteger(result.data?.id));
      const actual=result.data;
      stage='SHIP_SIMULATE_'+label+'_OWNERSHIP_'+suffix;
      if(actual.id!==expectedId){
        const foreign=spec.shippingReferenceA?.ids?.includes(actual.id);
        console.log('QA_V3_UI_SHIP_SIMULATE_'+label+'_MISMATCH_'+suffix+'='+(foreign?'FOREIGN_A':actual.id===did?'OWN_DEFAULT':actual.id===sid?'OWN_SPECIFIC':'OTHER'));
        throw new Error('Unexpected tariff identity');
      }
      stage='SHIP_SIMULATE_'+label+'_AMOUNT_'+suffix;
      requireTrue(actual.nombre===expectedName&&Number(actual.monto)===expectedAmount);
      stage='SHIP_SIMULATE_'+label+'_VISIBLE_'+suffix;
      await expect(simulation.getByRole('status')).toContainText(expectedName);
      mark('SHIP_SIMULATE_'+label+'_'+suffix);
    }
    await verifySimulation(spec.prefix+'-city',sid,special,amount+9,'SPECIFIC');
    await verifySimulation(spec.prefix+'-unknown',did,def,amount,'FALLBACK');
    await simulation.getByRole('button',{name:'Cerrar',exact:true}).click();
    await expect(simulation).toHaveCount(0);
    mark('SHIP_SIMULATE_'+suffix);
    stage='SHIP_HISTORY_'+suffix;
    const history=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/costos-envio/'+sid+'/historial'&&r.status()===200,{timeout:30000});
    await Promise.all([history,row(special).getByRole('button',{name:'Ver historial de tarifa'}).click()]);
    await expect(page.getByRole('heading',{name:'Historial: '+special})).toBeVisible();
    await expect(page.locator('section[aria-label="Historial de tarifa"] article').first()).toBeVisible();
    const historyModal=page.locator('.solqaryn-modal--l').filter({has:page.locator('section[aria-label="Historial de tarifa"]')});
    await expect(historyModal).toBeVisible();
    await historyModal.getByRole('button',{name:'Cerrar',exact:true}).click();
    await expect(historyModal).toHaveCount(0);
    mark('SHIP_HISTORY_'+suffix);
    stage='SHIP_CANCEL_DELETE_'+suffix;
    let deletes=0;
    page.on('request',r=>{if(r.method()==='DELETE'&&new URL(r.url()).pathname==='/api/costos-envio/'+sid)deletes++;});
    await row(special).getByRole('button',{name:'Eliminar costo de envío'}).click();
    await page.getByRole('dialog').getByRole('button',{name:'Cancelar'}).click();
    await expect(page.getByRole('dialog')).toHaveCount(0);
    await expect(row(special)).toHaveCount(1);requireTrue(deletes===0);
    mark('SHIP_CANCEL_DELETE_'+suffix);
    stage='SHIP_DELETE_'+suffix;
    await row(special).getByRole('button',{name:'Eliminar costo de envío'}).click();
    const del=page.waitForResponse(r=>scopedResponse(r,'/costos-envio/'+sid,'DELETE',200,identity.tenant),{timeout:30000});
    await Promise.all([del,page.getByRole('dialog').getByRole('button',{name:'Eliminar',exact:true}).click()]);
    // Require the refreshed table, not its transient absence while loading.
    await expect(row(def)).toHaveCount(1);
    await expect(row(special)).toHaveCount(0);
    requireTrue(deletes===1);
    stage='SHIP_DEFAULT_PROTECTION_'+suffix;
    let defaultWrites=0;
    page.on('request',r=>{if(['DELETE','PATCH'].includes(r.method())&&new URL(r.url()).pathname.startsWith('/api/costos-envio/'+did))defaultWrites++;});
    await row(def).getByRole('button',{name:'Desactivar costo de envío'}).click();
    await expect(page.getByRole('alert')).toContainText('predeterminada');
    await expect(row(def)).toContainText('Vigente');
    await row(def).getByRole('button',{name:'Eliminar costo de envío'}).click();
    await expect(page.getByRole('alert')).toContainText('predeterminada');
    await expect(row(def)).toHaveCount(1);
    await expect(page.locator('app-alert-dialog')).toHaveCount(0);
    requireTrue(defaultWrites===0);
    await overflow(page);
    mark('SHIP_CRUD_DEFAULT_'+suffix);
  } finally {await c.close();}
}
async function checkout(viewport,suffix){
  const c=await context(viewport);
  // For public checkout, permit only read operations plus the two documented non-mutating POSTs.
  await c.route(base+'/api/**',async route=>{
    const req=route.request(),p=new URL(req.url()).pathname;
    if(['GET','HEAD','OPTIONS'].includes(req.method())||(req.method()==='POST'&&['/api/tienda/productos/contexto','/api/tienda/checkout/validar'].includes(p))){await route.continue();}
    else await route.abort();
  });
  try{
    const page=await c.newPage();page.setDefaultTimeout(20000);
    let trafficLabel='BOOTSTRAP_'+suffix;
    observeQuoteTraffic(page,()=>trafficLabel);
    stage='CHECKOUT_BOOTSTRAP_HTTP_'+suffix;
    const bootstrap=await page.request.get(base+'/api/tienda/bootstrap');
    requireTrue(bootstrap.status()===200&&bootstrap.headers()['x-solqaryn-environment']==='QA');
    stage='CHECKOUT_BOOTSTRAP_PUBLIC_CONTRACT_'+suffix;
    const body=await bootstrap.json(),config=body.data?.identidad;
    // TiendaIdentidadPublicaDto deliberately does not expose the internal ID.
    // StorefrontIdentidadService overlays this DTO onto STORE_DEFAULT_CONFIG.id=0.
    // Match the production cart-key contract without exposing any tenant ID.
    requireTrue(body.success===true&&config&&typeof config.nombreComercial==='string'&&typeof config.moneda==='string'&&config.moneda.length===3&&!Object.hasOwn(config,'id'));
    const storageContextId=0;
    mark('CHECKOUT_BOOTSTRAP_'+suffix);
    stage='CHECKOUT_EMPTY_STATE_'+suffix;
    await page.goto(base+'/tienda/checkout');
    await expect(page.getByRole('heading',{name:'Tu carrito está vacío'})).toBeVisible();
    mark('CHECKOUT_EMPTY_'+suffix);
    await page.evaluate(({id,pid,vid})=>localStorage.setItem('storefront:carrito:v2:'+id+':bd',JSON.stringify([{productoId:pid,modeloClave:'variante:'+vid,unidades:2}])),{id:storageContextId,pid:spec.pid,vid:spec.vid});
    stage='CHECKOUT_CART_REHYDRATION_'+suffix;
    trafficLabel='INITIAL_'+suffix;
    await liveQuote(page,()=>page.reload(), 'INITIAL',suffix,false);
    await expect(page.getByRole('heading',{name:'Tu compra',exact:true})).toBeVisible();
    stage='CHECKOUT_REAL_PRODUCT_'+suffix;
    await expect(page.locator('.summary-lines')).toContainText(spec.product);
    mark('CHECKOUT_CART_REHYDRATED_'+suffix);
    const section=page.getByRole('region',{name:'Cobertura de envío'});
    const totals=page.locator('.totals');
    const money=value=>new Intl.NumberFormat('es-HN',{style:'currency',currency:config.moneda||'HNL'}).format(value);
    const norm=s=>s.replace(/\s+/g,' ').trim();
    async function quote(city,amount,tariff,label){
      stage='CHECKOUT_'+label+'_'+suffix;
      for(const [field,value] of [['departamento',spec.department],['ciudad',city],['zona',spec.zone],['modalidad',spec.deliveryMode]]){
        await section.locator('input[formcontrolname="'+field+'"]').fill(value);
      }
      stage='CHECKOUT_'+label+'_STALE_QUOTE_'+suffix;
      await expect(totals).toContainText('Por cotizar');
      await expect(page.getByText('No se puede continuar hasta cotizar el envío.',{exact:false})).toBeVisible();
      const prepare=page.getByRole('button',{name:'Preparar pedido por WhatsApp'});
      if(await prepare.count())await expect(prepare).toBeDisabled();
      trafficLabel=label+'_'+suffix;
      const r=await liveQuote(page,()=>section.getByRole('button',{name:'Actualizar costo de envío'}).click(),label,suffix,true);
      const j=await r.json(),d=j.data;
      stage='CHECKOUT_'+label+'_SERVER_AMOUNTS_'+suffix;
      requireTrue(j.success===true&&d.subtotal===200&&d.costoEnvio===amount&&d.total===200+(amount??0)&&d.lineas.length===1&&d.lineas[0].productoId===spec.pid&&d.lineas[0].productoVarianteId===spec.vid&&d.lineas[0].unidades===2&&d.lineas[0].precioUnitario===100&&d.lineas[0].total===200);
      stage='CHECKOUT_'+label+'_VISIBLE_AMOUNTS_'+suffix;
      await expect(page.getByRole('heading',{name:'Tu compra',exact:true})).toBeVisible();
      if(amount===null){
        await expect(totals).toContainText('Por cotizar');
        await expect(totals).toContainText('Subtotal sin envío');
        if(await prepare.count())await expect(prepare).toBeDisabled();
      }else{
        const shippingRow=totals.locator('div').filter({has:page.getByText('Envío',{exact:true})});
        await expect.poll(async()=>norm(await shippingRow.locator('dd').innerText())).toBe(norm(money(amount)));
        const grand=totals.locator('.grand-total');
        await expect(grand).toContainText('Total validado');
        await expect.poll(async()=>norm(await grand.locator('dd').innerText())).toBe(norm(money(200+amount)));
        if(tariff){requireTrue(d.tarifaEnvio===tariff);await expect(totals).toContainText(tariff);}
      }
      stage='CHECKOUT_'+label+'_GEOMETRY_'+suffix;
      await overflow(page);mark('CHECKOUT_'+label+'_'+suffix);
    }
    await quote(spec.cityZero,0,spec.tariffZero,'ZERO');
    await quote(spec.cityPaid,37.25,spec.tariffPaid,'PAID');
    await quote(spec.cityOther,spec.fallback,null,spec.fallback===null?'NO_COVERAGE':'FALLBACK');
    if(spec.fallback!==null)console.log('QA_V3_UI_NO_COVERAGE_NOT_PRESENT');
    await quote(spec.cityPaid,37.25,spec.tariffPaid,'RESTORE_PAID');
    stage='CHECKOUT_CHANNEL_SAFETY_'+suffix;
    await expect(page.getByRole('button',{name:'Tarjeta no disponible'})).toBeDisabled();
    const prepare=page.getByRole('button',{name:'Preparar pedido por WhatsApp'});
    if(await prepare.count()){
      await page.locator('input[formcontrolname="nombre"]').fill('QA Synthetic');
      await page.locator('input[formcontrolname="telefono"]').fill('00000000');
      await prepare.click();
      const link=page.getByRole('link',{name:'Abrir WhatsApp y continuar'});
      await expect(link).toBeVisible();
      const href=await link.getAttribute('href');
      requireTrue(/^https:\/\/(wa\.me|api\.whatsapp\.com)\//.test(href||''));
      // No click, no message, no external navigation.
      mark('CHECKOUT_WHATSAPP_PREPARED_'+suffix);
    }else{
      await expect(page.getByText('El comercio aún no tiene un WhatsApp válido configurado.',{exact:true})).toBeVisible();
      console.log('QA_V3_UI_WHATSAPP_NOT_CONFIGURED');
    }
    mark('CHECKOUT_CHANNEL_SAFETY_'+suffix);
  }finally{await c.close();}
}
try{
  if(spec.mode==='shipping'){
    if(spec.shippingViewportMode==='desktop'){
      requireTrue(Array.isArray(spec.shippingReferenceA?.ids)&&spec.shippingReferenceA.ids.length===2);
      await shipping(spec.b,'B',37.25,{width:1440,height:900});
      mark('SHIPPING_DESKTOP_B');
    }else{
      await shipping(spec.a,'A',0,{width:390,height:844});
      await shipping(spec.b,'B',37.25,{width:1440,height:900});
      mark('SHIPPING_AB');
    }
  }else{
    if(spec.viewportMode!=='desktop'&&spec.viewportMode!=='both')throw new Error('Invalid viewport mode');
    if(spec.viewportMode==='both')await checkout({width:390,height:844},'MOBILE');
    await checkout({width:1440,height:900},'DESKTOP');
    mark(spec.viewportMode==='desktop'?'CHECKOUT_DESKTOP_QUOTES':'CHECKOUT_QUOTES');
  }
}catch(error){
  // Do not emit error.message, stack, page content or request/response values.
  const kind=error?.name==='TimeoutError'?'TIMEOUT':error?.matcherResult?'EXPECTATION':error instanceof TypeError?'TYPE_ERROR':'ASSERTION_OR_RUNTIME';
  console.error('QA_V3_UI_FAILED_STAGE='+stage);
  console.error('QA_V3_UI_FAILURE_KIND='+kind);
  process.exitCode=1;
}
finally{await browser.close();}
