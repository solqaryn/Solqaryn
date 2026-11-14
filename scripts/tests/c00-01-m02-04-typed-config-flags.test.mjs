import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
const doc = readFileSync(new URL('../../docs/v5/c00-01-m02-04-typed-config-flags.md', import.meta.url), 'utf8');
const ids = Array.from({length:8}, (_, i) => `CFG${String(i+1).padStart(2,'0')}`);
const required = ['C00-01.M02.04', 'solqaryn/Solqaryn@dev', 'PROJECT_SCOPE_LOCK=STRICT', 'MultiEmpresa:PrivateReadPilotEnabled', 'PrivateWritePilotEnabled', 'PrivateMasterEditPilotEnabled', 'PrivateDraftPilotEnabled', 'RenderEnvironmentContractGuard', 'EnvironmentDatabaseGuard', 'CommercialTenantUiCapabilities.cs', '6805c75c939cc488dd2f392180584d6b8f175986', '2c38ebb90a8360b61c9e05df96ff3a9abd5b8f65', '896aa003876f2b4299dfe5d4f1d69b83b5d1be22', 'Jwt:Secret', 'tenant A/B', 'fail closed', 'no prueba servicio live', 'A5_PENDIENTE', 'A5_PASS', 'git revert <SHA_PUBLICADO>', 'NO_APLICA'];
function valid(s) {
 const normalized = s.normalize('NFC').toLowerCase();
 if (!required.every(t=>normalized.includes(t.normalize('NFC').toLowerCase()))) return false;
 const rows=s.split('\n').filter(x=>/^\| CFG\d\d \|/.test(x));
 if(rows.length!==8) return false;
 const actual=rows.map(x=>x.split('|')[1].trim());
 if(new Set(actual).size!==8||!ids.every(x=>actual.includes(x))) return false;
 return rows.every(x=>{const cols=x.split('|').slice(1,-1).map(v=>v.trim());return cols.length===6&&cols.slice(1,5).every(v=>v.length>=18)&&cols[5]==='DEFINIDO_NO_CERTIFICADO';});
}
test('A1 positivo: matriz y fuentes originales completas, no certificadas',()=>assert.equal(valid(doc),true));
test('A1 negativo: contrato faltante',()=>assert.equal(valid(doc.split('\n').filter(x=>!x.startsWith('| CFG01 |')).join('\n')),false));
test('A1 negativo: ID repetido',()=>assert.equal(valid(doc.replace('| CFG08 |','| CFG01 |')),false));
test('A2 negativo: pérdida de frontera tenant',()=>assert.equal(valid(doc.replaceAll('tenant A/B','empresa cualquiera')),false));
test('A2 negativo: omitir fail closed',()=>assert.equal(valid(doc.replaceAll('fail closed','allow by default')),false));
test('A2 negativo: eliminar protección de secretos',()=>assert.equal(valid(doc.replaceAll('Jwt:Secret','token visible')),false));
test('A3 negativo: Node no certifica runtime',()=>assert.equal(valid(doc.replaceAll('no prueba servicio live','certifica servicio live')),false));
test('A3 negativo: A5 propio prohibido',()=>assert.equal(valid(doc.replaceAll('A5_PENDIENTE','A5_PASS_PROPIO')),false));
test('A3 negativo: rollback destructivo prohibido',()=>assert.equal(valid(doc.replaceAll('git revert <SHA_PUBLICADO>','git reset --hard')),false));
