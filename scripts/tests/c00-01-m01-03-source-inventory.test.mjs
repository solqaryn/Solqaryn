import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
const doc = readFileSync(new URL('../../docs/v5/c00-01-m01-03-source-inventory.md', import.meta.url), 'utf8');
const required = ['C00-01.M01.03', 'solqaryn/Solqaryn', 'PROJECT_SCOPE_LOCK=STRICT', '2.340 IDs',
  '1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA', '1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU',
  'NO declarar migracion', 'NO reclamar IDs del backlog historico', 'NO editar COLA manualmente',
  'A5_PENDIENTE', 'NO constituyen firma criptografica', 'No son hashes SHA-256',
  'No se certifica ejecucion HTTP, SQL/MySQL', 'tenant A/B', 'JWT/BCrypt/RBAC', 'git revert'];
function validate(text) {
  if (!required.every(term => text.includes(term))) return false;
  const section = text.split('## Fuentes y huellas comprobadas')[1]?.split('## Matriz de contradicciones')[0] ?? '';
  const fileRows = section.split('\n').filter(line => line.startsWith('| ') && line.includes('.md`') || line.startsWith('| ') && line.includes('.csproj`') || line.startsWith('| ') && line.includes('package.json`'));
  const hashes = fileRows.map(line => line.match(/`([0-9a-f]{40})`/)?.[1]);
  return fileRows.length === 10 && hashes.length === 10 && hashes.every(h => h && !/^0+$/.test(h)) && new Set(hashes).size === 10;
}
test('inventario positivo: 10 blobs y fronteras documentadas', () => assert.equal(validate(doc), true));
test('negativo: no reemplazar repositorio autorizado', () => assert.equal(validate(doc.replaceAll('solqaryn/Solqaryn', 'otro/otro')), false));
test('negativo: no omitir Scope Lock', () => assert.equal(validate(doc.replaceAll('PROJECT_SCOPE_LOCK=STRICT', 'PROJECT_SCOPE_LOCK=OFF')), false));
test('negativo: no afirmar migracion futura como actual', () => assert.equal(validate(doc.replace('NO declarar migracion', 'Migracion completada')), false));
test('negativo: no reabrir backlog viejo', () => assert.equal(validate(doc.replace('NO reclamar IDs del backlog historico', 'Reclamar V3')), false));
test('negativo: no alterar formulas COLA', () => assert.equal(validate(doc.replace('NO editar COLA manualmente', 'Editar COLA manualmente')), false));
test('negativo: no confundir git blob con SHA-256', () => assert.equal(validate(doc.replace('No son hashes SHA-256', 'Son hashes SHA-256')), false));
test('negativo: no simular A5 independiente ni pruebas live', () => assert.equal(validate(doc.replace('A5_PENDIENTE', 'A5_PASS')), false));
test('negativo: no inventar huellas sin validacion', () => assert.equal(validate(doc.replace('304e400d5c0f89fcfdcc65caece86c34276734b5', '0000000000000000000000000000000000000000000')), false));
