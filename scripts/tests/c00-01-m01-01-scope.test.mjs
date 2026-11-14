import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const text = readFileSync(new URL('../../docs/v5/c00-01-m01-01-scope.md', import.meta.url), 'utf8');
function validate(source) {
  const required = [
    '# C00-01.M01.01', 'solqaryn/Solqaryn', 'PROJECT_SCOPE_LOCK=STRICT',
    '## Objetivo observable', '## No-alcance y prohibiciones',
    '## Fuentes y huellas del inventario inicial',
    '## Matriz de contradicciones y resolución segura',
    '## Criterio observable de no-regresión',
    'f4b8528f57e82625d343dd1590076df44cacaee2',
    '304e400d5c0f89fcfdcc65caece86c34276734b5',
    '1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',
    '1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU',
    'no despliegue realizado', 'No ejecutar', '**No alterar** JWT',
    'JWT', 'RBAC', 'A/B', 'Rollback', 'A5_PASS', '2.340'
  ];
  return required.every(part => source.includes(part));
}
test('contrato de delimitación V5 y fuente única, caso positivo', () => {
  assert.equal(validate(text), true);
});
test('no admite perder explícitamente la frontera de seguridad', () => {
  assert.equal(validate(text.replace('**No alterar** JWT', '**Alterar** JWT')), false);
});
test('rechaza referencia al repositorio ajeno', () => {
  assert.equal(validate(text.replaceAll('solqaryn/Solqaryn', 'solqaryn-platform/CRECISOL')), false);
});
test('rechaza confundir topología objetivo con despliegue real', () => {
  assert.equal(validate(text.replace('no despliegue realizado', 'ya está en producción')), false);
});
test('rechaza perder identidad de la Skill canónica', () => {
  assert.equal(validate(text.replace('304e400d5c0f89fcfdcc65caece86c34276734b5', '0'.repeat(40))), false);
});
