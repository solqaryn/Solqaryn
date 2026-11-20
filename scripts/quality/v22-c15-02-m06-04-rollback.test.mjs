import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import test from 'node:test';

// Real git revert only inside a fresh disposable repo; never a provider or shared branch.
const sha256 = s => createHash('sha256').update(s).digest('hex');
function git(root, ...args) {
  const p = spawnSync('git', args, {cwd:root,encoding:'utf8',env:{...process.env,GIT_TERMINAL_PROMPT:'0'}});
  assert.equal(p.status, 0, 'git ' + args.join(' ') + ': ' + p.stderr);
  return p.stdout.trim();
}
function put(root, path, text) {
  const file = join(root, path);
  mkdirSync(dirname(file), {recursive:true});
  writeFileSync(file, text, 'utf8');
}
const get = (root,path) => readFileSync(join(root,path),'utf8');
function fixture(t) {
  const root=mkdtempSync(join(tmpdir(),'sq-c15-02-rollback-'));
  t.after(()=>rmSync(root,{recursive:true,force:true}));
  git(root,'init','-q','-b','dev');
  git(root,'config','user.name','SOLQARYN ephemeral test');
  git(root,'config','user.email','noreply@example.invalid');
  const rows=Array.from({length:2340},(_,i)=>({id:'SYNTH-'+(i+1),status:i%2?'LISTO':'VALIDANDO'}));
  put(root,'protected/plan.json',JSON.stringify(rows)+'\n');
  put(root,'protected/tenant-A.json','{"tenant":"A","balance":10}\n');
  put(root,'protected/tenant-B.json','{"tenant":"B","balance":20}\n');
  put(root,'protected/bitacora.jsonl','{"event":"baseline"}\n');
  put(root,'protected/evidence.json','{"scope":"synthetic","pass":true}\n');
  put(root,'ops/provider-review.json','{"enabled":true,"externalCalls":false}\n');
  git(root,'add','--','protected','ops');
  git(root,'commit','-q','-m','baseline synthetic safety ledger');
  put(root,'ops/provider-review.json','{"enabled":false,"externalCalls":false}\n');
  git(root,'add','--','ops/provider-review.json');
  git(root,'commit','-q','-m','disable synthetic review only');
  const disabled=git(root,'rev-parse','HEAD');
  put(root,'protected/tenant-A.json','{"tenant":"A","balance":15}\n');
  put(root,'protected/bitacora.jsonl','{"event":"baseline"}\n{"event":"legitimate-new"}\n');
  git(root,'add','--','protected');
  git(root,'commit','-q','-m','new legitimate tenant event');
  return {root,disabled,head:git(root,'rev-parse','HEAD')};
}
function safeRevert(root, commit, expectedHead) {
  assert.match(commit,/^[a-f0-9]{40}$/,'invalid target SHA');
  assert.equal(git(root,'rev-parse','HEAD'),expectedHead,'stale HEAD');
  assert.equal(git(root,'status','--porcelain'),'','dirty worktree');
  git(root,'merge-base','--is-ancestor',commit,'HEAD');
  const changed=git(root,'diff-tree','--no-commit-id','--name-only','-r',commit).split('\n').filter(Boolean);
  assert.deepEqual(changed,['ops/provider-review.json'],'refuse protected tenant/audit data');
  git(root,'revert','--no-edit',commit);
}
test('real isolated revert preserves 2340 IDs, A/B data, history and hashes', t=>{
  const f=fixture(t),files=['protected/plan.json','protected/tenant-A.json','protected/tenant-B.json','protected/bitacora.jsonl','protected/evidence.json'];
  const before=files.map(p=>sha256(get(f.root,p)));
  safeRevert(f.root,f.disabled,f.head);
  assert.equal(JSON.parse(get(f.root,'ops/provider-review.json')).enabled,true);
  assert.equal(JSON.parse(get(f.root,'protected/plan.json')).length,2340);
  assert.deepEqual(files.map(p=>sha256(get(f.root,p))),before);
  assert.match(get(f.root,'protected/bitacora.jsonl'),/legitimate-new/);
  assert.equal(git(f.root,'rev-list','--count','HEAD'),'4');
  assert.equal(git(f.root,'status','--porcelain'),'');
  console.log('REAL_GIT_REVERT_PASS;SYNTHETIC_IDS=2340;TENANTS_AB_AND_BITACORA_HASHES_PRESERVED');
});
test('negative: protected tenant and bitacora commit cannot be reverted',t=>{
  const f=fixture(t);assert.throws(()=>safeRevert(f.root,f.head,f.head),/refuse protected/);
  assert.equal(git(f.root,'rev-parse','HEAD'),f.head);
});
test('negative: stale HEAD cannot roll back',t=>{
  const f=fixture(t);assert.throws(()=>safeRevert(f.root,f.disabled,f.disabled),/stale HEAD/);
  assert.equal(JSON.parse(get(f.root,'ops/provider-review.json')).enabled,false);
});
test('negative: malformed SHA and dirty worktree fail closed',t=>{
  const f=fixture(t);assert.throws(()=>safeRevert(f.root,'../../HEAD',f.head),/invalid target/);
  put(f.root,'protected/untracked.txt','unsaved evidence\n');
  assert.throws(()=>safeRevert(f.root,f.disabled,f.head),/dirty worktree/);
  assert.equal(git(f.root,'rev-parse','HEAD'),f.head);
});
