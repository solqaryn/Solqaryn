import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const source = readFileSync(new URL('../../docs/v5/c00-01-m01-06-release-gate.md', import.meta.url), 'utf8');
const required = [
  '# C00-01.M01.06', 'solqaryn/Solqaryn', 'PROJECT_SCOPE_LOCK=STRICT',
  '1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',
  'PLAN_MAESTRO!H', 'COLA!E/G/O', 'COLA!P:Q',
  'No crear PR, rama, ruleset, Tasks ni force-push',
  'PENDIENTE → EN_PROGRESO', 'EN_PROGRESO → VALIDANDO', 'VALIDANDO → LISTO',
  'A0', 'A1', 'A2', 'A3', 'A4', 'A5_PENDIENTE', 'A5_PASS',
  'Otro TASK_ID', 'GitHub', 'NO_GO', 'NO_APLICA histórico',
  'git revert <SHA_PUBLICADO>', 'BITACORA es apendicular',
  'C00-01.M01.01', 'C00-01.M01.06',
  'no inventar otro lock', 'no demuestra HTTP, MySQL, Angular browser ni tenancy A/B live',
  'nunca borrar ni reemplazar historia', 'QA/main/PROD'
];
function valid(t) {
  const txt = t.normalize('NFC').toLowerCase();
  const rows = t.split('\n').filter(x => x.startsWith('| ') && !x.includes('---'));
  const riskIds = ['Gobierno','Identidad','Concurrencia','Calidad','Posintegración','Independencia','Seguridad','Proveedores','Medios'];
  return required.every(x => txt.includes(x.normalize('NFC').toLowerCase())) &&
    rows.length >= 10 && riskIds.every(x => rows.some(row => row.startsWith('| ' + x + ' |')));
}
test('positivo: contrato completo de bloqueo, reversión y salida V5', () => {
  assert.equal(valid(source), true);
});
test('negativo: no autoriza PR o cambios de producción', () => {
  assert.equal(valid(source.replace('No crear PR, rama, ruleset, Tasks ni force-push', 'Crear PR y modificar producción')), false);
});
test('negativo: no transforma evidencia A5 pendiente en aprobación', () => {
  assert.equal(valid(source.replaceAll('A5_PENDIENTE', 'A5_APROBADA_POR_AUTOR')), false);
});
test('negativo: no elimina rollback aditivo', () => {
  assert.equal(valid(source.replace('git revert <SHA_PUBLICADO>', 'git reset --hard HEAD~1')), false);
});
test('negativo: no elimina límite real de pruebas sintéticas', () => {
  assert.equal(valid(source.replace('no demuestra HTTP, MySQL, Angular browser ni tenancy A/B live', 'demuestra todo sin prueba')), false);
});
test('negativo: no borra la evidencia y el ledger histórico', () => {
  assert.equal(valid(source.replace('nunca borrar ni reemplazar historia', 'borrar historial cuando estorbe')), false);
});
test('negativo: no omite filas de riesgos de tenancy ni concurrencia', () => {
  assert.equal(valid(source.split('\n').filter(x => !x.startsWith('| Seguridad |') && !x.startsWith('| Concurrencia |')).join('\n')), false);
});
