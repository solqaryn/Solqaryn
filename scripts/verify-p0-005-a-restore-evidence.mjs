#!/usr/bin/env node
// P0-005.A: inspeccion fail-closed de evidencia sanitizada, SIN ejecutar restore ni leer secretos.
import fs from 'node:fs';
const SHA40=/^[a-f0-9]{40}$/i, HASH64=/^[a-f0-9]{64}$/i;
const isUtc=x=>typeof x==='string'&&/^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d+)?Z$/.test(x)&&!Number.isNaN(Date.parse(x));
const pos=x=>Number.isSafeInteger(x)&&x>0;
export function evaluateRestoreEvidence(e,nowUtc){
  const p=[], require=(ok,code)=>{if(!ok)p.push(code);};
  if(!e||typeof e!=='object'||Array.isArray(e))return ['MISSING_EVIDENCE'];
  const s=e.source??{},b=e.backup??{},r=e.restore??{},now=Date.parse(nowUtc);
  require(isUtc(nowUtc),'INVALID_OBSERVATION_TIME');
  require(s.project==='SOLQARYN'&&s.environment==='DEV'&&s.branch==='dev'&&SHA40.test(s.commitSha??''),'WRONG_SOURCE_IDENTITY');
  require(s.productionTouched===false&&r.productionTouched===false,'PRODUCTION_TOUCHED_OR_UNKNOWN');
  require(isUtc(e.observedAtUtc)&&Date.parse(e.observedAtUtc)<=now,'MISSING_CURRENT_OBSERVATION');
  require(pos(b.runId)&&b.status==='success'&&typeof b.backupId==='string'&&b.backupId.length>8,'NO_SUCCESSFUL_REAL_BACKUP');
  require(b.encrypted===true&&b.algorithm==='AES256'&&HASH64.test(b.externalSha256??''),'ENCRYPTED_BACKUP_CHECKSUM_NOT_PROVEN');
  require(isUtc(b.createdAtUtc)&&Date.parse(b.createdAtUtc)<=Date.parse(e.observedAtUtc),'BACKUP_TIMESTAMP_INVALID');
  require(typeof b.databaseName==='string'&&/^[A-Za-z0-9_]+$/.test(b.databaseName)&&!/prod/i.test(b.databaseName),'BACKUP_DATABASE_UNSAFE');
  require(b.sourceCommitSha===s.commitSha&&b.environment==='DEV','BACKUP_SOURCE_SHA_NOT_VERIFIED');
  require(pos(r.runId)&&r.status==='success'&&r.environment==='CI_DISPOSABLE','NO_SUCCESSFUL_DISPOSABLE_RESTORE');
  require(['localhost','127.0.0.1','::1'].includes(r.targetHost)&&typeof r.databaseName==='string'&&/^solqaryn_restore_[A-Za-z0-9_]+$/.test(r.databaseName)&&r.databaseName!==b.databaseName,'UNSAFE_RESTORE_TARGET');
  require(r.sourceBackupId===b.backupId&&r.externalSha256===b.externalSha256&&r.externalChecksumVerified===true&&r.internalChecksumVerified===true,'SAME_ARTIFACT_HASH_CHAIN_NOT_VERIFIED');
  require(isUtc(r.completedAtUtc)&&Date.parse(r.completedAtUtc)>=Date.parse(b.createdAtUtc)&&Date.parse(r.completedAtUtc)<=Date.parse(e.observedAtUtc),'RESTORE_TIMESTAMP_INVALID');
  require(pos(b.tableCount)&&Number.isSafeInteger(b.migrationCount)&&b.migrationCount>=0&&r.tableCount===b.tableCount&&r.migrationCount===b.migrationCount&&r.exactTableRowCountsMatch===true,'RESTORED_COUNTS_NOT_EQUAL');
  require(r.apiReady===true&&r.disposableCleanupVerified===true&&r.secretsExposed===0,'RUNTIME_CLEANUP_OR_SECRETS_NOT_VERIFIED');
  return p;
}
function fixture(){
  const sha='a'.repeat(40);
  return {source:{project:'SOLQARYN',environment:'DEV',branch:'dev',commitSha:sha,productionTouched:false},observedAtUtc:'2026-10-26T18:00:00Z',
    backup:{runId:90001,status:'success',backupId:'SYNTHETIC-DEV-001',environment:'DEV',sourceCommitSha:sha,createdAtUtc:'2026-10-26T17:00:00Z',databaseName:'solqaryn_dev',encrypted:true,algorithm:'AES256',externalSha256:'b'.repeat(64),tableCount:100,migrationCount:50},
    restore:{runId:90002,status:'success',environment:'CI_DISPOSABLE',targetHost:'127.0.0.1',databaseName:'solqaryn_restore_fixture',productionTouched:false,sourceBackupId:'SYNTHETIC-DEV-001',externalSha256:'b'.repeat(64),externalChecksumVerified:true,internalChecksumVerified:true,completedAtUtc:'2026-10-26T17:40:00Z',tableCount:100,migrationCount:50,exactTableRowCountsMatch:true,apiReady:true,disposableCleanupVerified:true,secretsExposed:0}};
}
function selfTest(){
  const now='2026-10-26T18:00:00Z';
  if(evaluateRestoreEvidence(fixture(),now).length)throw Error('Synthetic positive fixture failed');
  const negatives=[
    ['HTML_200_ALONE',x=>{delete x.backup;delete x.restore;x.httpStatus=200;}],
    ['WRONG_PROJECT',x=>{x.source.project='OTHER';}],
    ['PROD_SOURCE',x=>{x.source.environment='PROD';}],
    ['PRODUCTION_TOUCHED',x=>{x.restore.productionTouched=true;}],
    ['SKIPPED_BACKUP',x=>{x.backup.status='skipped';}],
    ['UNENCRYPTED',x=>{x.backup.encrypted=false;}],
    ['BAD_HASH',x=>{x.backup.externalSha256='xyz';}],
    ['STALE_SOURCE_SHA',x=>{x.backup.sourceCommitSha='c'.repeat(40);}],
    ['NO_REAL_RESTORE',x=>{x.restore.status='skipped';}],
    ['REMOTE_TARGET',x=>{x.restore.targetHost='db.external.invalid';}],
    ['SAME_DATABASE',x=>{x.restore.databaseName=x.backup.databaseName;}],
    ['DIFFERENT_BACKUP',x=>{x.restore.sourceBackupId='OTHER-BACKUP';}],
    ['UNVERIFIED_INTERNAL_HASH',x=>{x.restore.internalChecksumVerified=false;}],
    ['WRONG_ROW_COUNTS',x=>{x.restore.exactTableRowCountsMatch=false;}],
    ['WRONG_MIGRATION_COUNTS',x=>{x.restore.migrationCount++;}],
    ['NOT_READY',x=>{x.restore.apiReady=false;}],
    ['NO_CLEANUP',x=>{x.restore.disposableCleanupVerified=false;}],
    ['SECRET_EXPOSED',x=>{x.restore.secretsExposed=1;}],
    ['FUTURE_RESTORE',x=>{x.restore.completedAtUtc='2026-10-27T00:00:00Z';}],
    ['NO_OBSERVATION',x=>{delete x.observedAtUtc;}],
  ];
  for(const [name,mutate] of negatives){
    const x=structuredClone(fixture());mutate(x);
    if(!evaluateRestoreEvidence(x,now).length)throw Error('Negative fixture unexpectedly passed: '+name);
  }
  console.log('P0-005.A SELF_TEST PASS: 1 POSITIVO SINTETICO + '+negatives.length+' NEGATIVOS; SIN BACKUP/RESTORE REAL');
}
if(process.argv[1]?.endsWith('verify-p0-005-a-restore-evidence.mjs')){
  if(process.argv.length===3&&process.argv[2]==='--self-test')selfTest();
  else if(process.argv.length===4&&process.argv[2]==='--evidence'){
    try{
      const evidence=JSON.parse(fs.readFileSync(process.argv[3],'utf8'));
      const fails=evaluateRestoreEvidence(evidence,new Date().toISOString());
      if(fails.length){console.error('NO_GO: '+fails.join(', '));process.exitCode=1;}
      else console.log('SCHEMA_VALIDO: requiere comprobacion nativa de procedencia/firmas y restore real para certificar');
    }catch{console.error('NO_GO: EVIDENCE_CANNOT_BE_PARSED');process.exitCode=1;}
  }else{console.error('Uso: node scripts/verify-p0-005-a-restore-evidence.mjs --self-test | --evidence <sanitized.json>');process.exitCode=2;}
}
