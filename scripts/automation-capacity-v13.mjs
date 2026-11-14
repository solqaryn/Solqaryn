import assert from 'node:assert/strict';
// Advisory capacity caps do not grant permissions, locks, mergers or LISTO.
export const CAPACITY_POLICY_VERSION='V13_1000_FIFO_BATCH';
export const MAX_CONCURRENT_SCOPES=1000;
export const MAX_OPEN_WORK_BRANCHES=1000;
export const MAX_OWNED_SCOPES_PER_TASK=1000;
export const MAX_ACTIVE_WRITES_PER_TASK=1000;
export function countDistinctWip(tasks) {
 if(!Array.isArray(tasks))return null;
 const active=tasks.filter(t=>t&&['VALIDANDO','EN_PROGRESO'].includes(t.state));
 if(active.some(t=>typeof t.id!=='string'||!t.id.trim()))return null;
 return new Set(active.map(t=>t.id.trim().toUpperCase())).size;
}
export function admitNewScope({wip,branches,owned,writes,resume=false}={}) {
 if(![wip,branches,owned,writes].every(n=>Number.isInteger(n)&&n>=0))return {allow:false,reason:'COUNTS_UNVERIFIED'};
 if(resume)return {allow:true,reason:'RESUME_EXISTING_SCOPE'};
 if(wip>=MAX_CONCURRENT_SCOPES)return {allow:false,reason:'GLOBAL_WIP_1000'};
 if(branches>=MAX_OPEN_WORK_BRANCHES)return {allow:false,reason:'WORK_BRANCHES_1000'};
 if(owned>=MAX_OWNED_SCOPES_PER_TASK)return {allow:false,reason:'TASK_CAPACITY_1000'};
 if(writes>=MAX_ACTIVE_WRITES_PER_TASK)return {allow:false,reason:'ACTIVE_WRITES_1000'};
 return {allow:true,reason:'CAPACITY_AVAILABLE'};
}
export function selfTestCapacityV13(){
 assert.equal(countDistinctWip([{id:'P1-001.A',state:'VALIDANDO'},{id:'P1-001.A',state:'EN_PROGRESO'},{id:'P1-001.B',state:'LISTO'}]),1);
 assert.equal(countDistinctWip([{state:'VALIDANDO'}]),null);
 assert.equal(admitNewScope({wip:999,branches:999,owned:999,writes:999}).allow,true);
 for(const key of ['wip','branches','owned','writes']) {
  const a={wip:999,branches:999,owned:999,writes:999,[key]:1000};
  assert.equal(admitNewScope(a).allow,false);
  assert.equal(admitNewScope({...a,resume:true}).allow,true);
 }
 assert.equal(admitNewScope({wip:0,branches:-1,owned:0,writes:0}).allow,false);
 console.log('SOLQARYN V13 CAPACITY 999/1000 SELF_TEST=PASS');
}
if(process.argv.includes('--self-test'))selfTestCapacityV13();
