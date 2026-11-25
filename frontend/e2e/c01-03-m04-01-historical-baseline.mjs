// Historical comparison only. No runtime patches, mocks, or current-suite rerun.
import { chromium } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, readFile, rm } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
const exec = promisify(execFile);
const parent = 'd7b460e823209125d3346de575a1b32889471773';
const phase = process.env.BASELINE_PHASE;
assert.ok(process.env.CI === 'true' && process.env.GITHUB_ACTIONS === 'true' && process.env.DISPLAY);
assert.ok(['single','multi'].includes(phase) && process.env.C1504_TEST_PASSWORD);
assert.equal((await exec('git',['rev-parse','HEAD'])).stdout.trim(), parent);
const base = 'http://127.0.0.1:4200';
const api = 'http://127.0.0.1:5005';
const root = await mkdtemp(join(tmpdir(),'sq-historical-'));
const extension = join(root,'extension');
const output = resolve('v44-c0103-m0401-historical');
const records = [];
async function setNativeZoom(worker, page, factor) {
  const actual = await worker.evaluate(async ({ url, factor }) => {
    const tabs = await chrome.tabs.query({});
    const tab = tabs.find(t => t.url === url);
    if (!tab || tab.id == null) throw Error('Native zoom tab not found');
    await chrome.tabs.setZoomSettings(tab.id, { mode: 'automatic', scope: 'per-tab' });
    await chrome.tabs.setZoom(tab.id, factor);
    return chrome.tabs.getZoom(tab.id);
  }, { url: page.url(), factor });
  assert.ok(Math.abs(actual - factor) < .001, 'chrome.tabs.getZoom mismatch');
  return actual;
}
async function physicalScreenshot(page, filename, loadingStatus = null) {
  await page.bringToFront();
  await page.waitForTimeout(3000);
  // A visible DOM node can be below the fold. Check the actual loading evidence
  // at capture time, independently of the keyboard-focus screenshot.
  if (loadingStatus) {
    for (const locator of [loadingStatus, loadingStatus.getByText(/Cargando configuración tenant/),
      loadingStatus.locator('mat-spinner')]) {
      const visible = await locator.evaluate(el => {
        const r = el.getBoundingClientRect(), css = getComputedStyle(el);
        const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
        return css.visibility !== 'hidden' && css.display !== 'none' &&
          r.width > 0 && r.height > 0 &&
          r.left >= 0 && r.right <= document.documentElement.clientWidth &&
          r.top >= 0 && r.bottom <= innerHeight && (hit === el || el.contains(hit));
      });
      assert.ok(visible, filename + ': loading indicator/message not in captured viewport');
    }
  }
  const path = join(output, filename);
  await exec('ffmpeg', ['-nostdin', '-hide_banner', '-loglevel', 'error',
    '-f', 'x11grab', '-draw_mouse', '0', '-video_size', '1600x1200',
    '-i', process.env.DISPLAY, '-frames:v', '1', '-threads', '1', '-y', path],
  { timeout: 20000 });
  const png = await readFile(path);
  assert.ok(png.length > 24 && png.subarray(0, 8)
    .equals(Buffer.from([137,80,78,71,13,10,26,10])), filename + ': missing PNG');
  assert.equal(png.readUInt32BE(16), 1600);
  assert.equal(png.readUInt32BE(20), 1200);
  return filename;
}

async function measure(page,target) {
  const dimensions = await page.evaluate(() => {
    const r = document.scrollingElement;
    const x = window.scrollX, y = window.scrollY;
    window.scrollTo({left:100000,top:y,behavior:'instant'});
    const right = window.scrollX;
    window.scrollTo({left:-100000,top:y,behavior:'instant'});
    const left = window.scrollX;
    window.scrollTo({left:x,top:y,behavior:'instant'});
    return {viewport:innerWidth,height:innerHeight,dpr:devicePixelRatio,pinch:visualViewport?.scale??1,
      client:r.clientWidth,scrollWidth:r.scrollWidth,body:document.body.scrollWidth,travel:Math.abs(right-left)};
  });
  const targetBounds = target ? await target.boundingBox() : null;
  return {...dimensions,targetBounds};
}
async function inspect(id, username) {
  const profile = join(root,'profile-'+id);
  await mkdir(join(profile,'Default'),{recursive:true});
  await writeFile(join(profile,'Default','Preferences'),JSON.stringify({credentials_enable_service:false,profile:{password_manager_enabled:false}}));
  const ctx = await chromium.launchPersistentContext(profile,{headless:false,viewport:null,args:['--window-size=1440,1000','--disable-extensions-except='+extension,'--load-extension='+extension]});
  try {
    const worker = ctx.serviceWorkers()[0] ?? await ctx.waitForEvent('serviceworker',{timeout:15000});
    const page = ctx.pages()[0] ?? await ctx.newPage();
    await page.goto(base+'/login');
    await page.getByRole('textbox',{name:'Nombre de usuario'}).fill(username);
    await page.locator('input[formcontrolname="password"]').fill(process.env.C1504_TEST_PASSWORD);
    const loginPending = page.waitForResponse(r=>r.url().endsWith('/auth/login')&&r.request().method()==='POST');
    await page.getByRole('button',{name:/Ingresar al sistema/}).click();
    const login=await loginPending;
    assert.equal(login.status(),200);
    const token=(await login.json())?.data?.token;
    assert.ok(typeof token==='string'&&token.split('.').length===3);
    const headers={Authorization:'Bearer '+token};
    assert.equal((await page.request.get(api+'/tenant-context/'+id,{headers})).status(),200);
    const grants=await page.request.get(api+'/permisos/mis-permisos/empresa/'+id,{headers});
    assert.equal(grants.status(),200);
    const policy=(await grants.json())?.data;
    assert.ok(policy,'historical policy missing');
    await page.waitForFunction(()=>Boolean(localStorage.getItem('solqaryn_token')));
    await page.evaluate(tenant=>localStorage.setItem('solqaryn_empresa_solicitada_id',String(tenant)),id);
    const reads=[];
    page.on('response', async r=>{
      if(r.url()===api+'/empresa-configuracion/tenant/'+id&&r.request().method()==='GET'){
        let data=null;
        if(r.status()===200)try{const b=(await r.json())?.data;data={empresaId:b?.empresaId,nombre:b?.nombre};}catch{}
        reads.push({status:r.status(),data});
      }
    });
    await page.goto(base+'/configuracion');
    const card=page.locator('app-empresa-configuracion-tenant-card');
    if(phase==='single') await card.getByRole('form',{name:'Configuración del tenant activo'}).waitFor({state:'visible',timeout:30000});
    else {
      await page.waitForFunction(()=>location.pathname!=='/configuracion'||!!document.querySelector('app-empresa-configuracion-tenant-card'),{},{timeout:30000});
      await page.waitForTimeout(1000);
    }
    const route= new URL(page.url()).pathname;
    const visible=route==='/configuracion'&&await card.isVisible();
    if(phase==='single') assert.ok(visible,'historical single-tenant card not reached');
    const target=visible?card.getByRole('button',{name:/Recargar/}):null;
    if(visible) {
      const form=card.getByRole('form',{name:'Configuración del tenant activo'});
      await form.waitFor({state:'visible',timeout:30000});
      await form.getByText('Empresa #'+id,{exact:true}).waitFor({state:'visible'});
      for(let i=0;i<30&&!reads.some(r=>r.status===200);i++)await page.waitForTimeout(100);
      const data=reads.find(r=>r.status===200)?.data;
      assert.equal(data?.empresaId,id);
      assert.equal(await form.locator('input[formcontrolname="nombre"]').inputValue(),data.nombre);
    }else{
      assert.notEqual(route,'/configuracion','route did not settle');
      assert.equal(await card.count(),0,'denied historical route exposed card');
    }
    const label=phase+'-tenant-'+id;
    const captures=[];
    const metrics=[];
    for(const factor of [1,2]){
      assert.equal(await setNativeZoom(worker,page,factor),factor);
      await page.waitForFunction(f=>Math.abs(devicePixelRatio-f)<.1,factor);
      if(target)await target.scrollIntoViewIfNeeded();
      const m=await measure(page,target);
      assert.ok(Math.abs(m.pinch-1)<.02);
      metrics.push({factor,...m});
      captures.push(await physicalScreenshot(page,label+'-'+(factor*100)+'.png'));
    }
    let focus=null;
    if(target){
      // The historical outcome is observed, not made to meet today's criteria.
      for(let i=0;i<200;i++){
        await page.keyboard.press('Tab');
        if(await target.evaluate(el=>el===document.activeElement)){
          focus=await target.evaluate(el=>({active:true,visible:el.matches(':focus-visible'),outline:getComputedStyle(el).outlineStyle}));break;
        }
      }
      focus??={active:false,visible:false};
      captures.push(await physicalScreenshot(page,label+'-200-focus.png'));
    }
    const record={parent,phase,tenant:id,route,cardVisible:visible,policy:{restricted:policy.operacionMultiEmpresaRestringida,routes:policy.rutasHabilitadas},reads:reads.map(r=>({status:r.status,empresaId:r.data?.empresaId})),metrics,focus,captures};
    records.push(record);
    console.log('V44_HISTORICAL_OBSERVATION '+JSON.stringify(record));
  }finally{await ctx.close();}
}
try{
  await mkdir(extension,{recursive:true});await mkdir(output,{recursive:true});
  await writeFile(join(extension,'manifest.json'),JSON.stringify({manifest_version:3,name:'SOLQARYN historical zoom evidence',version:'1.0.0',permissions:['tabs'],background:{service_worker:'background.js'}}));
  await writeFile(join(extension,'background.js'),'chrome.runtime.onInstalled.addListener(() => {});');
  await inspect(1,'c1504_admin_a');
  if(phase==='multi')await inspect(2,'c1504_admin_b');
}finally{
  await writeFile(join(output,phase+'-metrics.json'),JSON.stringify({parent,workflowSha:process.env.GITHUB_SHA,note:'Reproduction of historical parent, not a contemporaneous screenshot or current acceptance PASS.',records},null,2));
  await rm(root,{recursive:true,force:true});
}
