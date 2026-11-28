import {createRequire} from 'node:module';
import {resolve} from 'node:path';
import {statSync} from 'node:fs';
const require=createRequire(resolve('frontend/package.json'));
const {chromium}=require('playwright');
const {expect}=require('@playwright/test');
const chunks=[];
for await(const chunk of process.stdin) chunks.push(chunk);
const spec=JSON.parse(Buffer.concat(chunks).toString('utf8'));
const base='https://solqarynqa-nu.vercel.app';
if(process.env.GITHUB_REF!=='refs/heads/qa'||process.env.GITHUB_REPOSITORY!=='solqaryn/Solqaryn'||spec.base!==base||!['A','B'].includes(spec.side)||!Number.isSafeInteger(spec.tenant)||spec.tenant<=0||!Number.isSafeInteger(spec.category)||spec.category<=0||!/^qar[0-9a-f]{10}-[AB]-limite$/.test(spec.name)||!(/^[0-9a-fA-F]{32}$/).test(spec.version)||statSync(spec.good).size!==15*1024*1024||statSync(spec.bad).size!==15*1024*1024+1) throw new Error('QA browser scope');
const browser=await chromium.launch({headless:true});
let stage='LOGIN';
const mark=label=>console.log('QA_GALLERY_UI_'+label+'_'+spec.side+'=PASS');
try {
  const context=await browser.newContext({viewport:spec.side==='A'?{width:390,height:844}:{width:1440,height:900},acceptDownloads:false});
  // Never intercept same-origin multipart uploads: Chromium request interception
  // can re-materialize file-backed request bodies. Keep foreign mutations blocked.
  await context.route(url=>url.origin!==base,async route=>{
    const request=route.request();
    if(request.isNavigationRequest()||!['GET','HEAD'].includes(request.method())){await route.abort();return;}
    await route.continue();
  });
  // Observe the UI-created FormData without modifying arguments, requests or responses.
  // Do not log form content, headers, tokens, file names or tenant identifiers.
  await context.addInitScript(expectedVersion=>{
    Object.defineProperty(window,'__qaExpectedGalleryVersion',{value:expectedVersion});
    const append=FormData.prototype.append,forms=new WeakMap(),observed=[];
    Object.defineProperty(window,'__qaGalleryForms',{value:observed});
    FormData.prototype.append=function(name,value,...rest){
      const result=Reflect.apply(append,this,[name,value,...rest]);
      if(!['Version','Orden','Archivos'].includes(name))return result;
      let entry=forms.get(this);
      if(!entry){entry={version:0,order:[],files:[]};forms.set(this,entry);observed.push(entry);}
      if(name==='Version'){entry.version++;entry.versionMatches=typeof value==='string'&&value===window.__qaExpectedGalleryVersion;}
      if(name==='Orden')entry.order.push(typeof value==='string'&&value==='n:0');
      if(name==='Archivos')entry.files.push({blob:value instanceof Blob,size:value instanceof Blob?value.size:0,webp:value instanceof Blob&&value.type==='image/webp'});
      return result;
    };
  },spec.version);
  const page=await context.newPage();
  page.setDefaultTimeout(25000);
  await page.goto(base+'/login');
  await page.locator('input[autocomplete="username"]').fill(spec.user);
  await page.locator('input[autocomplete="current-password"]').fill(spec.password);
  await page.locator('button[type="submit"]').click();
  await page.locator('#empresa-id').fill(String(spec.tenant));
  await page.getByRole('button',{name:'Entrar a la empresa'}).click();
  await page.waitForURL(u=>u.pathname!=='/login',{timeout:30000});
  mark('LOGIN');
  stage='EDITOR';
  await page.goto(base+'/categorias');
  const row=page.getByRole('row').filter({has:page.getByText(spec.name,{exact:true})});
  await expect(row).toHaveCount(1);
  const hydrated=page.waitForResponse(r=>new URL(r.url()).origin===base&&new URL(r.url()).pathname==='/api/categorias/'+spec.category+'/imagenes'&&r.request().method()==='GET',{timeout:30000});
  await row.locator('button[title="Editar"]').click();
  const dialog=page.getByRole('dialog',{name:'Editar registro'});
  await expect(dialog).toBeVisible();
  await expect(dialog.locator('.gallery-help')).toContainText('Hasta 15 MB por fotografía');
  const input=dialog.locator('input[type=file]');
  const count=dialog.locator('.gallery-count');
  stage='HYDRATION';
  const loaded=await hydrated;
  const gallery=await loaded.json();
  if(loaded.status()!==200||loaded.headers()['x-solqaryn-environment']!=='QA'||gallery.success!==true||gallery.data?.version!==spec.version||gallery.data?.imagenes?.length!==0)throw new Error('Unexpected initial gallery');
  await expect(dialog.locator('input[formcontrolname="nombre"]')).toHaveValue(spec.name);
  await expect(dialog.locator('.gallery-upload')).toBeEnabled();
  await expect(input).toBeEnabled();
  await expect(count).toHaveText('0 / 5');
  mark('HYDRATED');
  stage='OVERSIZE';
  await input.setInputFiles(spec.bad);
  await expect(dialog.locator('.category-gallery [role=alert]')).toContainText('hasta 15 MB cada una');
  await expect(count).toHaveText('0 / 5');
  mark('PLUS_ONE_REJECTED');
  stage='EXACT';
  await input.setInputFiles(spec.good);
  await expect(count).toHaveText('1 / 5',{timeout:35000});
  await expect(dialog.locator('.category-gallery [role=alert]')).toHaveCount(0);
  await expect(input).toBeEnabled();
  await expect(dialog.getByRole('button',{name:'Guardar',exact:true})).toBeEnabled();
  await expect(dialog.locator('.gallery-item img')).toHaveCount(1);
  mark('EXACT_15_MIB_ACCEPTED');
  stage='SAVE_RESPONSE';
  const write=page.waitForResponse(r=>new URL(r.url()).origin===base&&new URL(r.url()).pathname==='/api/categorias/'+spec.category+'/imagenes'&&r.request().method()==='PUT',{timeout:60000});
  await dialog.getByRole('button',{name:'Guardar',exact:true}).click();
  await page.locator('app-alert-dialog .app-alert__confirm').click();
  const response=await write;
  console.log('QA_GALLERY_UI_HTTP_SAVE_'+spec.side+'='+response.status());
  stage='SAVE_AUTHORITY';
  const headers=await response.request().allHeaders();
  if(response.status()!==200||response.headers()['x-solqaryn-environment']!=='QA'||headers['x-empresa-id']!==String(spec.tenant))throw new Error('Unexpected gallery write');
  if(!/^multipart\/form-data;\s*boundary=.+/i.test(headers['content-type']||''))throw new Error('Missing multipart boundary');
  mark('MULTIPART_AUTHORITY');
  stage='FORMDATA';
  const forms=await page.evaluate(()=>window.__qaGalleryForms);
  const matches=forms.filter(form=>form.version===1&&form.versionMatches===true&&form.order.length===1&&form.order[0]===true&&form.files.length===1);
  console.log('QA_GALLERY_UI_TOTAL_FORMS_'+spec.side+'='+forms.length);
  console.log('QA_GALLERY_UI_MATCHING_FORMS_'+spec.side+'='+matches.length);
  if(matches.length!==1)throw new Error('UI did not construct expected FormData');
  const file=matches[0].files[0];
  console.log('QA_GALLERY_UI_OPTIMIZED_BYTES_'+spec.side+'='+file.size);
  if(!file.blob||!file.webp||file.size<=0||file.size>750*1024)throw new Error('Optimized file not proven');
  mark('OPTIMIZED_FORMDATA');
  stage='SAVE_BODY';
  const body=await response.json();
  const countSaved=Array.isArray(body.data?.imagenes)?body.data.imagenes.length:999;
  console.log('QA_GALLERY_UI_SAVED_IMAGES_'+spec.side+'='+countSaved);
  if(body.success!==true||countSaved!==1)throw new Error('Gallery result mismatch');
  stage='SAVE_CLOSE';
  await expect(dialog).toHaveCount(0);
  mark('OPTIMIZED_UPLOAD_PERSISTED');
} catch {
  console.log('QA_GALLERY_UI_'+stage+'_'+spec.side+'=FAIL');
  process.exitCode=1;
} finally {await browser.close();}
