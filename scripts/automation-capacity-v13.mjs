import assert from 'node:assert/strict';
// Advisory capacity caps do not grant permissions, locks, mergers or LISTO.
export const CAPACITY_POLICY_VERSION='V15_SINGLE_ACTIVE_DEV_QUALITY';
export const MAX_CONCURRENT_SCOPES=1000;
export const MAX_OPEN_WORK_BRANCHES=1000;
export const MAX_OWNED_SCOPES_PER_TASK=1000;
export const MAX_ACTIVE_WRITES_PER_TASK=1; // one active EN_PROGRESO write per Task
export function countDistinctWip(tasks) {
 if(!Array.isArray(tasks))return null;
 const active=tasks.filter(t=>t&&['VALIDANDO','EN_PROGRESO'].includes(t.state));
 if(active.some(t=>typeof t.id!=='string'||!t.id.trim()))return null;
 return new Set(active.map(t=>t.id.trim().toUpperCase())).size;
}
// 'writes' is the LIVE EN_PROGRESO count of this Task, NOT the number of writes or closures completed this run.
// After LISTO or true external-CI VALIDANDO, reset writes to 0 for the next sequential claim.
export function admitNewScope({wip,branches,owned,writes,resume=false}={}) {
 if(![wip,branches,owned,writes].every(n=>Number.isInteger(n)&&n>=0))return {allow:false,reason:'COUNTS_UNVERIFIED'};
 if(resume)return {allow:true,reason:'RESUME_EXISTING_SCOPE'};
 if(wip>=MAX_CONCURRENT_SCOPES)return {allow:false,reason:'GLOBAL_WIP_1000'};
 if(branches>=MAX_OPEN_WORK_BRANCHES)return {allow:false,reason:'WORK_BRANCHES_1000'};
 if(owned>=MAX_OWNED_SCOPES_PER_TASK)return {allow:false,reason:'TASK_CAPACITY_1000'};
 if(writes>=MAX_ACTIVE_WRITES_PER_TASK)return {allow:false,reason:'ACTIVE_WRITES_SINGLE_DEV'};
 return {allow:true,reason:'CAPACITY_AVAILABLE'};
}
export function selfTestCapacityV13(){
 assert.equal(countDistinctWip([{id:'C01-01.M01.01',state:'VALIDANDO'},{id:'C01-01.M01.01',state:'EN_PROGRESO'},{id:'C01-01.M01.02',state:'LISTO'}]),1);
 assert.equal(countDistinctWip([{state:'VALIDANDO'}]),null);
 assert.equal(admitNewScope({wip:999,branches:999,owned:999,writes:0}).allow,true);
 for(const key of ['wip','branches','owned']){const x={wip:999,branches:999,owned:999,writes:0,[key]:1000};assert.equal(admitNewScope(x).allow,false);assert.equal(admitNewScope({...x,resume:true}).allow,true);}
 const running={wip:0,branches:0,owned:0,writes:1};
 assert.deepEqual(admitNewScope(running),{allow:false,reason:'ACTIVE_WRITES_SINGLE_DEV'});
 assert.deepEqual(admitNewScope({...running,resume:true}),{allow:true,reason:'RESUME_EXISTING_SCOPE'});
 assert.equal(admitNewScope({wip:0,branches:-1,owned:0,writes:0}).allow,false);
 // Pipeline: one active writer blocks another; a waiting VALIDANDO does not.
 assert.equal(admitNewScope({wip:3,branches:0,owned:3,writes:1}).allow,false);
 assert.equal(admitNewScope({wip:3,branches:0,owned:3,writes:0}).allow,true);
 for(let previousClosed=1;previousClosed<=5;previousClosed++){
  assert.equal(admitNewScope({wip:previousClosed,branches:0,owned:previousClosed,writes:0}).allow,true);
 }
 console.log('SOLQARYN V15 SINGLE_ACTIVE_DEV CAPACITY SELF_TEST=PASS');
}
if(process.argv.includes('--self-test'))selfTestCapacityV13();
