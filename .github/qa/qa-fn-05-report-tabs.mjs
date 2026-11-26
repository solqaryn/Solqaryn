import {createRequire} from 'node:module';
import {resolve} from 'node:path';
import assert from 'node:assert/strict';

// Use real pointer input, including for partially clipped tabs. No force, DOM
// click, programmatic selection or change to the application's tab state.
export async function selectReportTab(page,name){
  const group=page.locator('mat-tab-group.reports-tabs');
  const tab=group.getByRole('tab',{name,exact:true});
  await tab.waitFor({state:'attached',timeout:5000});
  await group.locator('mat-tab-header').scrollIntoViewIfNeeded();
  const measure=previous=>tab.evaluate(async (el,previous)=>{
    const clip=el.closest('.mat-mdc-tab-label-container');
    if(!clip) return null;
    let last=null, stable=0;
    const started=performance.now();
    while(performance.now()-started<3000){
      await new Promise(requestAnimationFrame);
      const r=el.getBoundingClientRect(), c=clip.getBoundingClientRect();
      const v=[r.left,r.right,r.top,r.bottom,c.left,c.right,c.top,c.bottom];
      stable=last&&v.every((x,i)=>Math.abs(x-last[i])<0.5)?stable+1:0;
      last=v;
      if(stable<4||performance.now()-started<100||previous!==null&&Math.abs(r.left-previous)<0.5) continue;
      const left=Math.max(r.left,c.left,0),right=Math.min(r.right,c.right,innerWidth);
      const top=Math.max(r.top,c.top,0),bottom=Math.min(r.bottom,c.bottom,innerHeight);
      if(right-left>2&&bottom-top>2){
        for(const xf of [0.5,0.25,0.75]) for(const yf of [0.5,0.25,0.75]){
          const x=left+(right-left)*xf,y=top+(bottom-top)*yf;
          if(el.contains(document.elementFromPoint(x,y))) return {x,y,left:r.left};
        }
      }
      return {left:r.left,direction:r.right>c.right+1?'after':r.left<c.left-1?'before':null};
    }
    return null;
  },previous);
  let previous=null;
  for(let i=0;i<16;i++){
    const state=await measure(previous);
    if(!state) throw new Error('TAB_GEOMETRY_NO_PROGRESS');
    if(state.x!==undefined){
      await page.mouse.click(state.x,state.y);
      try{await group.getByRole('tab',{name,exact:true,selected:true}).waitFor({state:'visible',timeout:5000});}
      catch{throw new Error('TAB_SELECTION_NOT_CONFIRMED');}
      return;
    }
    if(!state.direction) throw new Error('TAB_HIT_TARGET_BLOCKED');
    const pager=group.locator('.mat-mdc-tab-header-pagination-'+state.direction);
    if(!await pager.isVisible()||await pager.getAttribute('aria-disabled')==='true'||
       (await pager.getAttribute('class')??'').includes('mat-mdc-tab-header-pagination-disabled')){
      throw new Error('TAB_PAGER_UNAVAILABLE');
    }
    previous=state.left;
    await pager.click({timeout:5000});
  }
  throw new Error('TAB_PAGINATION_LIMIT');
}

async function selfTest(){
  const require=createRequire(resolve('frontend/package.json'));
  const {chromium}=require('playwright');
  const browser=await chromium.launch({headless:true,...(process.env.QA_FN05_LOCAL_CHROMIUM?{executablePath:process.env.QA_FN05_LOCAL_CHROMIUM}:{})});
  try{
    const page=await browser.newPage({viewport:{width:390,height:844}});
    async function fixture(width,offset,max,step){
      await page.setContent(`<style>body{margin:0}mat-tab-group,mat-tab-header{display:flex;align-items:center}.mat-mdc-tab-label-container{overflow:hidden;width:${width}px;height:48px}#strip{display:flex;width:max-content;transform:translateX(-${offset}px);transition:transform 180ms ease} [role=tab]{width:180px;height:48px;flex:none} [role=tab]:last-child{width:320px} .pager{width:32px;height:48px;flex:none}</style><mat-tab-group class="reports-tabs"><mat-tab-header><button class="pager mat-mdc-tab-header-pagination-before">Prev</button><div class="mat-mdc-tab-label-container"><div id="strip" role="tablist"><button role="tab" aria-selected="true">Usuarios</button><button role="tab" aria-selected="false">Roles y permisos</button><button role="tab" aria-selected="false">Actividad de auditoría</button></div></div><button class="pager mat-mdc-tab-header-pagination-after">Next</button></mat-tab-header></mat-tab-group><script>(()=>{let offset=${offset};window.clicks=0;const max=${max},step=${step},strip=document.getElementById('strip'),next=document.querySelector('.mat-mdc-tab-header-pagination-after'),prev=document.querySelector('.mat-mdc-tab-header-pagination-before');function update(){strip.style.transform='translateX(-'+offset+'px)';next.classList.toggle('mat-mdc-tab-header-pagination-disabled',offset>=max);prev.classList.toggle('mat-mdc-tab-header-pagination-disabled',offset<=0)}next.onclick=()=>{window.clicks++;offset=Math.min(max,offset+step);update()};prev.onclick=()=>{window.clicks++;offset=Math.max(0,offset-step);update()};for(const t of document.querySelectorAll('[role=tab]'))t.onclick=()=>{for(const b of document.querySelectorAll('[role=tab]'))b.setAttribute('aria-selected',String(b===t))};update()})()</script>`);
    }
    const target='Actividad de auditoría';
    await fixture(140,160,370,45);
    const tab=page.getByRole('tab',{name:target,exact:true});
    const centerVisible=()=>tab.evaluate(el=>{const r=el.getBoundingClientRect(),c=el.closest('.mat-mdc-tab-label-container').getBoundingClientRect();return r.left+r.width/2>=c.left&&r.left+r.width/2<=c.right;});
    await page.locator('.mat-mdc-tab-header-pagination-after').click();
    await page.waitForFunction(()=>getComputedStyle(document.querySelector('#strip')).transform.includes('-205'));
    assert.equal(await centerVisible(),false,'Old one-click/center condition must fail');
    await fixture(140,160,370,45);
    await selectReportTab(page,target);
    assert.ok(await page.evaluate(()=>window.clicks)>1,'Must use progressive paging');
    await fixture(140,370,370,45);
    assert.equal(await centerVisible(),false,'Disabled pager with clipped center');
    await selectReportTab(page,target);
    assert.equal(await page.evaluate(()=>window.clicks),0);
    await fixture(140,0,0,45);
    await assert.rejects(selectReportTab(page,target),/TAB_PAGER_UNAVAILABLE/);
    await page.setViewportSize({width:1440,height:900});
    await fixture(800,0,0,45);
    await selectReportTab(page,target);
    assert.equal(await page.evaluate(()=>window.clicks),0);
    console.log('QA_FN05_TAB_REGRESSION=PASS (old failure, progressive, clipped, blocked, desktop)');
  }finally{await browser.close();}
}
if(process.argv.includes('--self-test')) await selfTest();
