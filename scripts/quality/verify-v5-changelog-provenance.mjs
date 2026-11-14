import { readFileSync } from 'node:fs';
import process from 'node:process';
// Audita atribución documental. No es una prueba funcional del SaaS.
const lines=readFileSync(new URL('../../CHANGELOG_AI.md',import.meta.url),'utf8').split(/\r?\n/u);
const headings=[];for(let i=0;i<lines.length;i++){if(lines[i].startsWith('## ')){let j=i+1;while(j<lines.length&&!lines[j].startsWith('## '))j++;headings.push({header:lines[i],line:i+1,body:lines.slice(i+1,j).filter(x=>x.trim())})}}
const fail=[];
for(const h of headings)if(!h.body.length)fail.push('EMPTY_HEADING '+h.line+' '+h.header);
const expected=[
['C01-02.M01.04',['- Inventario documental R01','- Sólo M01.04 documental;']],
['C01-03.M01.03',['- Se leyeron y fijaron cuatro blobs de código real']],
['C01-03.M01.04',['- Documento de preparación M01 para configuración dinámica']],
['C01-03.M01.05',['- Se fija documento M01 de 10 criterios observables']],
['Ampliacion V5 DAG20',['- Backups nativos del Sheet','- El Doc rector, DAG_TECNICO']],
['C00-04.M01.01:',['- Contrato documental de decisiones','- A0–A4 documentales requeridas']],
['C00-04.M01.02:',['- Inventario contractual documental','- Validar A0–A4 proporcionales']],
['C00-04.M01.03:',['- Inventario documental con fuentes','- Sin cambios runtime;']],
['C00-04.M01.04:',['- Matriz documental R01-R10','- Sin cambio ejecutable;']],
['C00-04.M01.05:',['- Contrato documental AC01–AC12','- A0–A4 dirigidos']]
];
for(const [anchor,prefixes] of expected){const found=headings.filter(x=>x.header.includes(anchor));if(found.length!==1){fail.push('HEADING_COUNT '+anchor+': '+found.length);continue}for(const p of prefixes)if(!found[0].body.some(x=>x.startsWith(p)))fail.push('INCORRECT_ATTRIBUTION '+anchor+': '+p)}
if(fail.length){for(const x of fail)process.stderr.write('CHANGELOG_PROVENANCE_FAIL '+x+'\n');process.exitCode=1}
else process.stdout.write('CHANGELOG_PROVENANCE_PASS headings='+headings.length+' anchored='+expected.length+'\n');
