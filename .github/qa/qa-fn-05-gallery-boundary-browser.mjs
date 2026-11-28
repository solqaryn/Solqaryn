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
if(process.env.GITHUB_REF!=='refs/heads/qa'||process.env.GITHUB_REPOSITORY!=='solqaryn/Solqaryn'||spec.base!==base||!['A','B'].includes(spec.side)||!Number.isSafeInteger(spec.tenant)||spec.tenant<=0||!Number.isSafeInteger(spec.category)||spec.category<=0||!/^qar[0-9a-f]{10}-[AB]-limite$/.test(spec.name)||statSync(spec.good).size!==15*1024*1024||statSync(spec.bad).size!==15*1024*1024+1) throw new Error('QA browser scope');
const browser=await chromium.launch({headless:true});
let stage='LOGIN';
const mark=label=>console.log('QA_GALLERY_UI_'+label+'_'+spec.side+'=PASS');
try {
  const context=await browser.newContext({viewport:spec.side==='A'?{width:390,height:844}:{width:1440,height:900},acceptDownloads:false});
  await context.route('**/*',async route=>{
    const request=route.request(),url=new URL(request.url());
    if(url.origin===base){await route.continue();return;}
    if(request.isNavigationRequest()||!['GET','HEAD'].includes(request.method())){await route.abort();return;}
    await route.continue();
  });
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
  await row.locator('button[title="Editar"]').click();
  const dialog=page.getByRole('dialog',{name:'Editar registro'});
  await expect(dialog).toBeVisible();
  await expect(dialog.locator('.gallery-help')).toContainText('Hasta 15 MB por fotografía');
  const input=dialog.locator('input[type=file]');
  const count=dialog.locator('.gallery-count');
  await expect(count).toHaveText('0 / 5');
  stage='OVERSIZE';
  await input.setInputFiles(spec.bad);
  await expect(dialog.locator('.category-gallery [role=alert]')).toContainText('hasta 15 MB cada una');
  await expect(count).toHaveText('0 / 5');
  mark('PLUS_ONE_REJECTED');
  stage='EXACT';
  await input.setInputFiles(spec.good);
  await expect(count).toHaveText('1 / 5',{timeout:35000});
  await expect(dialog.locator('.category-gallery [role=alert]')).toHaveCount(0);
  mark('EXACT_15_MIB_ACCEPTED');
  stage='SAVE';
  const write=page.waitForResponse(r=>new URL(r.url()).origin===base&&new URL(r.url()).pathname==='/api/categorias/'+spec.category+'/imagenes'&&r.request().method()==='PUT',{timeout:60000});
  await dialog.getByRole('button',{name:'Guardar',exact:true}).click();
  await page.locator('app-alert-dialog .app-alert__confirm').click();
  const response=await write;
  if(response.status()!==200||response.headers()['x-solqaryn-environment']!=='QA'||response.request().headers()['x-empresa-id']!==String(spec.tenant))throw new Error('Unexpected gallery write');
  const sent=response.request().postDataBuffer();
  if(!sent||sent.length>800*1024||!sent.includes(Buffer.from('Content-Type: image/webp')))throw new Error('Optimized multipart not proven');
  const body=await response.json();
  if(body.success!==true||body.data?.imagenes?.length!==1)throw new Error('Gallery result mismatch');
  await expect(dialog).toHaveCount(0);
  mark('OPTIMIZED_UPLOAD_PERSISTED');
} catch {
  console.log('QA_GALLERY_UI_'+stage+'_'+spec.side+'=FAIL');
  process.exitCode=1;
} finally {await browser.close();}
