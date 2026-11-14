import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const source=readFileSync(new URL('../../docs/v5/c00-01-m02-03-persistence-contract.md', import.meta.url),'utf8');
const ids=Array.from({length:9},(_,i)=>'DB'+String(i+1).padStart(2,'0'));
const required=['C00-01.M02.03','solqaryn/Solqaryn','PROJECT_SCOPE_LOCK=STRICT','1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA','AppDbContext.cs','ClienteConfiguration.cs','ProductoConfiguration.cs','CommercialTenantScopedRootIndexes.cs','c4b32f6637f1b4426655bb0cd12af8dd9d229866','0a3d6e2dfb33f3d6cc49cd8a70639f9886964f16','EmpresaId','MySQL','A0-A4','A5_PENDIENTE','A5_PASS','git revert <SHA_PUBLICADO>','NO_APLICA','no ejecuta SQL','21 campos','no crear PR'];
function valid(s){
 const low=s.toLowerCase();
 if(!required.every(t=>low.includes(t.toLowerCase())))return false;
 const rows=s.split('\n').filter(line=>/^\| DB\d\d \|/.test(line));
 if(rows.length!==9)return false;
 const found=rows.map(row=>row.split('|').at(1).trim());
 if(new Set(found).size!==9||!ids.every(id=>found.includes(id)))return false;
 return rows.every(row=>{const p=row.split('|').slice(1,-1).map(x=>x.trim());return p.length===6&&p.slice(1,5).every(x=>x.length>=18)&&p[5]==='DEFINIDO_NO_CERTIFICADO';});
}
test('positivo: nueve contratos de persistencia definidos y no certificados',()=>assert.equal(valid(source),true));
test('negativo: falta de contrato de tenant',()=>assert.equal(valid(source.split('\n').filter(x=>!x.startsWith('| DB01 |')).join('\n')),false));
test('negativo: duplicacion de ID',()=>assert.equal(valid(source.replace('| DB09 |','| DB01 |')),false));
test('negativo: cambio de repositorio',()=>assert.equal(valid(source.replaceAll('solqaryn/Solqaryn','otro/repo')),false));
test('negativo: no falsificar estado de prueba live',()=>assert.equal(valid(source.replace('| DEFINIDO_NO_CERTIFICADO |','| PASS_LIVE |')),false));
test('negativo: sin plan de rollback',()=>assert.equal(valid(source.replaceAll('git revert <SHA_PUBLICADO>','git reset --hard')),false));
test('negativo: impedir A5 ficticio',()=>assert.equal(valid(source.replaceAll('A5_PENDIENTE','A5_PASS_POR_AUTOR')),false));
test('negativo: no declarar SQL real por un documento',()=>assert.equal(valid(source.replaceAll('no ejecuta SQL','ejecuta SQL real')),false));
test('negativo: no convertir imagenes historicas inexistentes en migradas',()=>assert.equal(valid(source.replaceAll('NO_APLICA','MIGRACION_REALIZADA')),false));
