import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const source = readFileSync(new URL('../../docs/v5/c00-01-m02-06-dependencies-exit.md', import.meta.url), 'utf8');
const ids = Array.from({ length: 6 }, (_, i) => `C00-01.M02.0${i+1}`);
function valid(s) {
  const lower = s.normalize('NFC').toLowerCase();
  const required = ['V5 FINAL', 'PROJECT_SCOPE_LOCK=STRICT', 'PLAN_MAESTRO!H=PENDIENTE', 'COLA!G=SI', 'COLA!O=SI', 'COLA!E', 'A0-A4', 'A5_PENDIENTE', 'A5_PASS', 'DEPENDENCY_NOT_FINAL', 'tenant', 'git revert', 'fast-forward', 'NO_APLICA'];
  const table = s.split('\n').filter(l => /^\| `C00-01\.M02\.0[1-6]`/.test(l));
  return ids.every(x => s.includes('`'+x+'`')) && table.length === 6
    && required.every(x => lower.includes(x.toLowerCase()))
    && /en `VALIDANDO` NO certifica/.test(s)
    && /`SKIPPED` no es PASS/.test(s)
    && /`EmpresaId`.*NO otorgan acceso/.test(s)
    && /no publicar si HEAD cambia/.test(s)
    && /no certificacion runtime/.test(lower)
    && /no modificar.*?QA\/main\/PROD/s.test(s) === false;
}
test('A1 positivo: seis contratos, limites de seguridad y salida documental', () => assert.equal(valid(source), true));
test('A1 negativo: ausencia de contrato M02.04 detiene salida', () => assert.equal(valid(source.replaceAll('C00-01.M02.04', 'C00-01.M02.XX')), false));
test('A1 negativo: VALIDANDO no equivale a cierre', () => assert.equal(valid(source.replace('en `VALIDANDO` NO certifica', 'en `VALIDANDO` certifica')), false));
test('A2 negativo: A5_PASS obligatorio para promocion', () => assert.equal(valid(source.replaceAll('A5_PASS', 'SELF_APPROVED')), false));
test('A2 negativo: no autorizar tenant por EmpresaId del cliente', () => assert.equal(valid(source.replace('`EmpresaId` de navegador, header, slug o JWT no validado NO otorgan acceso', '`EmpresaId` de navegador, header, slug o JWT no validado otorgan acceso')), false));
test('A3 negativo: nunca considerar SKIPPED como PASS', () => assert.equal(valid(source.replace('`SKIPPED` no es PASS', '`SKIPPED` es PASS')), false));
test('A3 negativo: no omitir rollback git revert', () => assert.equal(valid(source.replaceAll('git revert','git reset --hard')), false));
test('A3 negativo: no ocultar cambio de HEAD', () => assert.equal(valid(source.replace('no publicar si HEAD cambia', 'publicar aunque HEAD cambia')), false));
