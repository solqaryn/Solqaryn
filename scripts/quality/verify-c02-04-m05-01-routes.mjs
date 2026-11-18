import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';import {dirname,resolve} from 'node:path';import {fileURLToPath} from 'node:url';
const root=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const inv=JSON.parse(readFileSync(resolve(root,'docs/v5/c02-04-m05-01-routes.json'),'utf8'));
const source=readFileSync(resolve(root,'frontend/src/app/app.routes.ts'),'utf8');
const actual=[...source.matchAll(/\{\s*path:\s*'([^']+)'[^\n]*canActivate:\s*\[([^\]]+)\][^\n]*loadComponent:\s*\(\)\s*=>\s*import\('([^']+)'\)/g)]
.filter(m=>m[1]==='plataforma'||m[1].startsWith('plataforma/'))
.map(m=>({path:m[1],guard:m[2].trim(),component:m[3]}));
function check(rows,src){
 assert.equal(inv.schemaVersion,'C02-04.M05.01/v18.1');
 assert.equal(rows.length,15);assert.ok(src.length>=15,'at least the 15 versioned routes must exist');
 assert.equal(new Set(rows.map(r=>r.id)).size,15);
 assert.equal(new Set(rows.map(r=>r.path)).size,15);
 const map=new Map(src.map(r=>[r.path,r]));
 for(const x of rows){
  assert.match(x.id,/^GV(?:0[1-9]|1[0-5])$/);
  assert.equal(src.filter(r=>r.path===x.path).length,1,'duplicate or missing versioned route: '+x.path);
  const ref=map.get(x.path);assert.ok(ref,'route absent: '+x.path);
  assert.equal(ref.component,x.component);
  assert.equal(ref.guard,'plataformaGuard');assert.equal(x.guard,'plataformaGuard');
  assert.equal(x.serverAuth,'NO_MEDIDO');assert.equal(x.browserState,'CI_REQUIRED');
 }
}
check(inv.routes,actual);
if(process.argv.includes('--self-test')){
 const neg=f=>assert.throws(f,assert.AssertionError);
 neg(()=>check(inv.routes.slice(1),actual));
 neg(()=>check([...inv.routes,inv.routes[0]],actual));
 neg(()=>check(inv.routes.map((x,i)=>i?x:{...x,component:'./wrong'}),actual));
 const originalIndex=actual.findIndex(r=>r.path===inv.routes[0].path);
 assert.ok(originalIndex>=0,'first catalog route missing in source');
 neg(()=>check(inv.routes,actual.map((x,i)=>i===originalIndex?{...x,guard:'authGuard'}:x)));
 neg(()=>check(inv.routes,[...actual,actual[originalIndex]]));
 neg(()=>check(inv.routes.map((x,i)=>i?x:{...x,serverAuth:'PASS'}),actual));
 console.log('6/6 negative fixtures detected');
}
console.log('15/15 versioned route source checks PASS; additional platform routes allowed; live server auth NOT MEASURED');
