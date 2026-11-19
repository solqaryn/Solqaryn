#!/usr/bin/env node
// C15-01.M05.04: snapshot is audit evidence, never authority for operational claims.
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { resolve } from 'node:path';
function must(p,msg) { if(!p) throw Error('C15-01.M05.04 FAIL: '+msg); }
function indexed(rows,label) {
  const m=new Map();
  for(const r of rows) {
    must(typeof r[0]==='string' && /^C\d{2}-\d{2}\.M\d{2}\.\d{2}$/.test(r[0]),label+' invalid ID');
    must(!m.has(r[0]),label+' duplicate '+r[0]);
    m.set(r[0],r);
  } return m;
}
export function auditMatrix(d,full=false) {
  must(Array.isArray(d.plan)&&Array.isArray(d.cola)&&Array.isArray(d.dag),'bad arrays');
  if(full)must(d.plan.length===2340&&d.cola.length===2340&&d.dag.length===2301,'snapshot cardinality');
  const p=indexed(d.plan,'PLAN'),c=indexed(d.cola,'COLA'),g=indexed(d.dag,'DAG');
  must(p.size===c.size,'PLAN COLA mismatch');
  const states={},eligible=[];let historical=0;
  for(const [id,r] of p) {
    const [_,status,owner]=r,cr=c.get(id),dr=g.get(id);
    must(['LISTO','VALIDANDO','EN_PROGRESO','PENDIENTE'].includes(status),'bad state '+id);
    must(cr&&cr[1]===status,'unmapped or mismatched COLA '+id);
    if(!dr) {must(status==='LISTO','DAG missing for active '+id);historical++;}
    if(cr[3]==='SI') {
      must(status==='PENDIENTE'&&!owner&&cr[2]==='SI'&&dr&&dr[1]==='VALIDADO'&&dr[2]==='SI','ineligible positive '+id);
      eligible.push(id);
    }
    states[status]=(states[status]||0)+1;
  }
  for(const id of c.keys())must(p.has(id),'COLA orphan '+id);
  for(const id of g.keys())must(p.has(id),'DAG orphan '+id);
  if(full)must(historical===39,'historic DAG exception changed');
  return {nPlan:p.size,nCola:c.size,nDag:g.size,historicalListoNoDag:historical,eligible,states};
}
function blobSha(s) {return createHash('sha1').update('blob '+Buffer.byteLength(s)+'\0').update(s).digest('hex');}
function auditRoutes(d) {
  must(d.authority==='NON_AUTHORITATIVE_SNAPSHOT','snapshot impersonates authority');
  must(Array.isArray(d.routes)&&d.routes.length===4,'route source list incomplete');
  let total=0;const used=new Set();
  for(const r of d.routes) {
    must(r.path.startsWith('frontend/src/app/')&&r.path.endsWith('.routes.ts')&&!used.has(r.path),'unsafe route file');
    used.add(r.path);
    const body=readFileSync(resolve(r.path),'utf8');
    must(blobSha(body)===r.blobSha,'route hash drift '+r.path);
    const count=[...body.matchAll(/\bpath:\s*'[^']+'/g)].length;
    must(count===r.declarations&&count>0,'route declarations drift '+r.path);
    total+=count;
  }
  return total;
}
function selfTest() {
  const seed={plan:[['C00-01.M01.01','LISTO','x'],['C15-01.M05.04','PENDIENTE','']],
    cola:[['C00-01.M01.01','LISTO','NO','NO'],['C15-01.M05.04','PENDIENTE','SI','SI']],
    dag:[['C15-01.M05.04','VALIDADO','SI']]};
  must(auditMatrix(seed).eligible.length===1,'positive fixture');
  function fail(mut) {const x=structuredClone(seed);mut(x);let rejected=false;
    try{auditMatrix(x)}catch{rejected=true;}must(rejected,'negative not rejected');}
  fail(x=>x.plan.push([...x.plan[0]]));
  fail(x=>x.cola.pop());
  fail(x=>x.dag.pop());
  fail(x=>x.plan[1][2]='writer');
  fail(x=>x.cola[1][1]='LISTO');
  console.log('C15-01.M05.04 SELFTEST PASS: one positive, five adversarial failures');
}
const a=process.argv.slice(2);
if(a.includes('--self-test'))selfTest();
if(a.includes('--snapshot')) {
  const file=a[a.indexOf('--snapshot')+1];
  must(!!file,'missing snapshot path');
  const data=JSON.parse(readFileSync(resolve(file),'utf8'));
  const m=auditMatrix(data,true),n=auditRoutes(data);
  console.log('C15-01.M05.04 SNAPSHOT PASS '+JSON.stringify({...m,routeDeclarations:n}));
  console.log('LIMIT: not a live Sheets read; full ID to module/route evidence and JWT tenant/backend authorization remain unverified.');
}
if(!a.includes('--self-test')&&!a.includes('--snapshot'))throw Error('Specify --self-test or --snapshot FILE');
