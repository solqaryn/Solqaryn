import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const evidence = readFileSync(new URL('../../docs/v5/c00-01-m01-04-risks.md', import.meta.url), 'utf8');
const must = [
  '# C00-01.M01.04', 'solqaryn/Solqaryn', 'PROJECT_SCOPE_LOCK=STRICT',
  '1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',
  '1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU',
  '304e400d5c0f89fcfdcc65caece86c34276734b5',
  '1931e1229752028229b6ad1bb3368038e2c6769b',
  '146dd3bde06a738b5741684341c0734318c241cb',
  '## Registro de riesgos y decisiones', '## Supuestos y fronteras de decisión',
  '## Aceptación, pruebas y reversión', 'A5_PENDIENTE', 'A5_PASS',
  'C00-01.M01.02', 'WITHDRAW J20', 'NO_APLICA_HISTÓRICO',
  'No crear filas comerciales adicionales', 'No producir objetos',
  'bloquear QA/main/PROD', 'sin force-push'
];
function valid(t) {
  if (!must.every(x => t.includes(x))) return false;
  const risks = t.split('\n').filter(line => /^\| R0[1-9] \|/.test(line));
  if (risks.length !== 9 || new Set(risks.map(line => line.match(/R0[1-9]/)[0])).size !== 9) return false;
  if (!risks.every(line => line.split('|').length === 8 && /\| (ABIERTO|HISTÓRICO_CON_CONTROL|NO_APLICA_HISTÓRICO|CONTROL_CONTINUO) \|/.test(line))) return false;
  return true;
}
test('registro V5 de 9 riesgos y decisiones con evidencia suficiente', () => {
  assert.equal(valid(evidence), true);
});
test('NO_GO si se sustituye la identidad del proyecto', () => {
  assert.equal(valid(evidence.replaceAll('solqaryn/Solqaryn', 'solqaryn-platform/CRECISOL')), false);
});
test('NO_GO si falta evidencia de carrera entre Tasks', () => {
  assert.equal(valid(evidence.replace('WITHDRAW J20', 'OVERRIDE AUTORÍA')), false);
});
test('NO_GO si se borra A5 independiente', () => {
  assert.equal(valid(evidence.replaceAll('A5_PENDIENTE', 'AUTO_APPROVED')), false);
});
test('NO_GO si se falsifica migración de objetos inexistentes', () => {
  assert.equal(valid(evidence.replaceAll('NO_APLICA_HISTÓRICO', 'MIGRADO')), false);
});
test('NO_GO si se suprime un riesgo obligatorio', () => {
  assert.equal(valid(evidence.split('\n').filter(line => !line.startsWith('| R06 |')).join('\n')), false);
});
