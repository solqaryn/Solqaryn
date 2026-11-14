import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const file = new URL('../../docs/v5/c00-01-m01-05-acceptance.md', import.meta.url);
const source = readFileSync(file, 'utf8');
const expected = Array.from({ length: 12 }, (_, i) => 'AC' + String(i + 1).padStart(2, '0'));
const required = [
  '# C00-01.M01.05', 'solqaryn/Solqaryn', 'PROJECT_SCOPE_LOCK=STRICT',
  '1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',
  '1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU',
  'PLAN_MAESTRO!H', 'COLA!E/G/O', 'BITACORA',
  'A0', 'A1', 'A2', 'A3', 'A4', 'A5_PENDIENTE', 'A5_PASS',
  'otro TASK_ID', 'QA/main/PROD', 'git revert <SHA_PUBLICADO>',
  'NO_APLICA', 'no afirmar HTTP, MySQL, Angular browser ni tenancy A/B live',
];
function validate(doc) {
  if (!required.every(needle => doc.toLowerCase().includes(needle.toLowerCase()))) return false;
  const rows = doc.split('\n').filter(line => /^\| AC\d\d \|/.test(line));
  if (rows.length !== 12) return false;
  const found = rows.map(row => row.split('|').at(1).trim());
  if (new Set(found).size !== 12 || !expected.every(id => found.includes(id))) return false;
  return rows.every(row => {
    const fields = row.split('|').slice(1, -1).map(s => s.trim());
    return fields.length === 7 && fields.slice(1, 6).every(s => s.length >= 20) &&
      fields[6] === 'DEFINIDO_NO_CERTIFICADO';
  });
}

test('positivo: doce criterios y matriz completa', () => {
  assert.equal(validate(source), true);
});
test('negativo: eliminar criterio falla', () => {
  assert.equal(validate(source.split('\n').filter(x => !x.startsWith('| AC04 |')).join('\n')), false);
});
test('negativo: ID duplicado falla', () => {
  assert.equal(validate(source.replace('| AC12 |', '| AC01 |')), false);
});
test('negativo: repositorio ajeno falla', () => {
  assert.equal(validate(source.replaceAll('solqaryn/Solqaryn', 'otro/proyecto')), false);
});
test('negativo: PASS live inventado falla', () => {
  assert.equal(validate(source.replace('| DEFINIDO_NO_CERTIFICADO |', '| PASS_LIVE |')), false);
});
test('negativo: A5 de otra Task no se omite', () => {
  assert.equal(validate(source.replaceAll('A5_PENDIENTE', 'A5_AUTOAPROBADA')), false);
});
test('negativo: rollback aditivo no se omite', () => {
  assert.equal(validate(source.replaceAll('git revert <SHA_PUBLICADO>', 'git reset --hard HEAD~1')), false);
});
test('negativo: no certificar HTTP/MySQL/Browser mediante documento', () => {
  assert.equal(validate(source.replace(/no afirmar HTTP, MySQL, Angular browser ni tenancy A\/B live/gi, 'afirmar live sin pruebas')), false);
});
test('negativo: ausencia de prueba de AC07 falla', () => {
  assert.equal(validate(source.replace(/(\| AC07 \|[^\n]*?\|) [^|]+ (\| Angular 22)/, '$1 sin prueba $2')), false);
});
