import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const doc = readFileSync(new URL('../../docs/v5/c00-01-m01-02-interfaces.md', import.meta.url), 'utf8');
function check(text) {
  const must = [
    'C00-01.M01.02', 'PROJECT_SCOPE_LOCK=STRICT', 'solqaryn/Solqaryn',
    'PLAN_MAESTRO!H', 'A5_PENDIENTE', 'JWT/BCrypt/RBAC', 'Tenant A/B',
    'Cloudinary/QuestPDF', 'R2 solo objetivo V5',
    'No alterar JWT/BCrypt/RBAC', '**no prueban HTTP/MySQL/Playwright ni disponibilidad real**',
    'git revert', 'No PR, rama nueva, force, qa/main/PROD', 'Migración NO_APLICA'
  ];
  const normalized = text.normalize('NFC').toLowerCase();
  return must.every(x => normalized.includes(x.normalize('NFC').toLowerCase())) &&
    text.split('\n').filter(line => line.startsWith('| ') && !line.includes('---')).length >= 11;
}

test('contrato positivo: inventario de actores, entrada, salida, superficie, confianza', () => {
  assert.equal(check(doc), true);
});
test('negativo: no borrar frontera de autorización JWT/BCrypt/RBAC', () => {
  assert.equal(check(doc.replaceAll('JWT/BCrypt/RBAC', 'autorización de cliente')), false);
});
test('negativo: no sustituir límites QA y PROD por despliegue permitido', () => {
  assert.equal(check(doc.replace('No PR, rama nueva, force, qa/main/PROD', 'Se permite desplegar directo a producción')), false);
});
test('negativo: no sustituir infraestructura objetivo por proveedor ya migrado', () => {
  assert.equal(check(doc.replaceAll('R2 solo objetivo V5', 'R2 ya migrado en producción')), false);
});
test('negativo: no declarar que fixtures prueban integración real', () => {
  assert.equal(check(doc.replace('**no prueban HTTP/MySQL/Playwright ni disponibilidad real**', 'prueban integraciones reales')), false);
});
test('negativo: no eliminar filas sustantivas del inventario', () => {
  assert.equal(check(doc.split('\n').filter(line => !line.startsWith('| ')).join('\n')), false);
});
