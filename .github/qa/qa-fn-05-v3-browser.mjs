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
  return url.origin===base&&url.pathname===('/api'+path)&&response.request().method()===method&&response.status()===expected&&(!tenant||response.request().headers()['x-empresa-id']===String(tenant));
}
async function shipping(identity,suffix,amount,viewport){
  const c=await context(viewport);
  try{
    const page=await c.newPage();page.setDefaultTimeout(20000);
    const table=page.getByRole('table',{name:'Costos de envío de la empresa'});
    const def=spec.prefix+'-'+suffix+'-default',special=spec.prefix+'-'+suffix+'-specific';
    const row=name=>table.getByRole('row').filter({has:page.getByText(name,{exact:true})});
    const form=()=>page.getByRole('region',{name:'Formulario de tarifa'});
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
      const editor=page.locator('section[aria-label="Formulario de tarifa"]');
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
    stage='SHIP_DETAIL_'+suffix;
    await row(special).getByRole('button',{name:'Ver costo de envío '+special,exact:true}).click();
    const detail=page.getByRole('dialog');
    await expect(detail).toBeVisible();
    await expect(detail).toContainText(special);
    await page.keyboard.press('Escape');await expect(detail).toHaveCount(0);
    mark('SHIP_DETAIL_'+suffix);
    stage='SHIP_EDIT_'+suffix;
    await row(special).getByRole('button',{name:'Editar costo de envío'}).click();
    const editor=page.locator('section[aria-label="Formulario de tarifa"]');
    await editor.locator('input[formcontrolname="monto"]').fill(String(amount+9));
    const edit=page.waitForResponse(r=>scopedResponse(r,'/costos-envio/'+sid,'PUT',200,identity.tenant),{timeout:30000});
    await Promise.all([edit,editor.getByRole('button',{name:'Actualizar',exact:true}).click()]);
    requireTrue(Number((await (await edit).json()).data.monto)===amount+9);
    await expect(editor).toHaveCount(0);
    mark('SHIP_EDIT_'+suffix);
    for(const [action,expected,label] of [['Desactivar',false,'DISABLE'],['Activar',true,'ENABLE']]){
      stage='SHIP_'+label+'_'+suffix;
      const state=page.waitForResponse(r=>scopedResponse(r,'/costos-envio/'+sid+'/estado','PATCH',200,identity.tenant),{timeout:30000});
      await Promise.all([state,row(special).getByRole('button',{name:action+' costo de envío'}).click()]);
      requireTrue((await (await state).json()).data.activo===expected);
      await expect(row(special)).toContainText(expected?'Vigente':'Inactivo');
      mark(stage);
    }
    stage='SHIP_SIMULATE_'+suffix;
    const simulation=page.locator('section[aria-label="Simulación de tarifa"]');
    await simulation.locator('input[formcontrolname="ciudad"]').fill(spec.prefix+'-city');
    const resolveRate=page.waitForResponse(r=>scopedResponse(r,'/costos-envio/resolver','POST',200,identity.tenant),{timeout:30000});
    await Promise.all([resolveRate,simulation.getByRole('button',{name:'Resolver tarifa'}).click()]);
    requireTrue((await (await resolveRate).json()).data.id===sid);
    await expect(simulation.getByRole('status')).toContainText(special);
    await simulation.locator('input[formcontrolname="ciudad"]').fill(spec.prefix+'-unknown');
    const fallback=page.waitForResponse(r=>scopedResponse(r,'/costos-envio/resolver','POST',200,identity.tenant),{timeout:30000});
    await Promise.all([fallback,simulation.getByRole('button',{name:'Resolver tarifa'}).click()]);
    requireTrue((await (await fallback).json()).data.id===did);
    await expect(simulation.getByRole('status')).toContainText(def);
    mark('SHIP_SIMULATE_'+suffix);
    stage='SHIP_HISTORY_'+suffix;
    const history=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/costos-envio/'+sid+'/historial'&&r.status()===200,{timeout:30000});
    await Promise.all([history,row(special).getByRole('button',{name:'Ver historial de tarifa'}).click()]);
    await expect(page.getByRole('heading',{name:'Historial: '+special})).toBeVisible();
    await expect(page.locator('section[aria-label="Historial de tarifa"] article').first()).toBeVisible();
    await page.locator('section[aria-label="Historial de tarifa"]').getByRole('button',{name:'Cerrar'}).click();
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
    await expect(row(special)).toHaveCount(0);
    requireTrue(deletes===1);
    await row(def).getByRole('button',{name:'Desactivar costo de envío'}).click();
    await expect(page.getByRole('alert')).toContainText('predeterminada');
    await expect(row(def)).toContainText('Vigente');
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
    stage='CHECKOUT_BOOTSTRAP_'+suffix;
    const bootstrap=await page.request.get(base+'/api/tienda/bootstrap');
    requireTrue(bootstrap.status()===200&&bootstrap.headers()['x-solqaryn-environment']==='QA');
    const body=await bootstrap.json(),config=body.data.identidad;
    requireTrue(body.success===true&&Number.isInteger(config.id));
    await page.goto(base+'/tienda/checkout');
    await expect(page.getByRole('heading',{name:'Tu carrito está vacío'})).toBeVisible();
    mark('CHECKOUT_EMPTY_'+suffix);
    await page.evaluate(({id,pid,vid})=>localStorage.setItem('storefront:carrito:v2:'+id+':bd',JSON.stringify([{productoId:pid,modeloClave:'variante:'+vid,unidades:2}])),{id:config.id,pid:spec.pid,vid:spec.vid});
    const initial=page.waitForResponse(r=>scopedResponse(r,'/tienda/checkout/validar','POST',200),{timeout:30000});
    await Promise.all([initial,page.reload()]);
    await expect(page.getByRole('heading',{name:'Tu compra',exact:true})).toBeVisible();
    await expect(page.locator('.summary-lines')).toContainText(spec.product);
    const section=page.getByRole('region',{name:'Cobertura de envío'});
    const totals=page.locator('.totals');
    const money=value=>new Intl.NumberFormat('es-HN',{style:'currency',currency:config.moneda||'HNL'}).format(value);
    const norm=s=>s.replace(/\s+/g,' ').trim();
    async function quote(city,amount,tariff,label){
      stage='CHECKOUT_'+label+'_'+suffix;
      for(const [field,value] of [['departamento',spec.department],['ciudad',city],['zona',spec.zone],['modalidad',spec.deliveryMode]]){
        await section.locator('input[formcontrolname="'+field+'"]').fill(value);
      }
      await expect(totals).toContainText('Por cotizar');
      await expect(page.getByText('No se puede continuar hasta cotizar el envío.',{exact:false})).toBeVisible();
      const prepare=page.getByRole('button',{name:'Preparar pedido por WhatsApp'});
      if(await prepare.count())await expect(prepare).toBeDisabled();
      const response=page.waitForResponse(r=>scopedResponse(r,'/tienda/checkout/validar','POST',200),{timeout:30000});
      await Promise.all([response,section.getByRole('button',{name:'Actualizar costo de envío'}).click()]);
      const r=await response;requireTrue(r.headers()['x-solqaryn-environment']==='QA');
      const j=await r.json(),d=j.data;
      requireTrue(j.success===true&&d.subtotal===200&&d.costoEnvio===amount&&d.total===200+(amount??0)&&d.lineas.length===1&&d.lineas[0].productoId===spec.pid&&d.lineas[0].productoVarianteId===spec.vid&&d.lineas[0].unidades===2);
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
      await overflow(page);mark(stage);
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
    await shipping(spec.a,'A',0,{width:390,height:844});
    await shipping(spec.b,'B',37.25,{width:1440,height:900});
    mark('SHIPPING_AB');
  }else{
    await checkout({width:390,height:844},'MOBILE');
    await checkout({width:1440,height:900},'DESKTOP');
    mark('CHECKOUT_QUOTES');
  }
}catch(error){console.error('QA_V3_UI_FAILED_STAGE='+stage);process.exitCode=1;}
finally{await browser.close();}
