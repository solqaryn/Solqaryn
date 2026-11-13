import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/shared/navigation/navigation-contract.json', import.meta.url)), 'utf8');
const note = readFileSync(fileURLToPath(new URL('../../docs/P5-013-C-DELIMITACION-RAMA-MENU-DEV.md', import.meta.url)), 'utf8');
const current = JSON.parse(source);

function inspect(contract) {
  const issues = [];
  if (contract.schema !== 'solqaryn-navigation-contract/v1') issues.push('SCHEMA_CHANGED');
  if (!contract.authority?.includes('never replaces backend authorization')) issues.push('AUTHORITY_RELAXED');
  if (!Array.isArray(contract.groups) || contract.groups.length !== 8) issues.push('GROUP_SHAPE_CHANGED');
  const groups = contract.groups ?? [];
  if (new Set(groups.map(group => group.id)).size !== groups.length) issues.push('DUPLICATE_GROUP_ID');
  const allRoutes = groups.flatMap(group => group.children ?? []);
  if (new Set(allRoutes.map(child => child.route)).size !== allRoutes.length) issues.push('DUPLICATE_ROUTE');
  const admin = groups.filter(group => group.id === 'administracion');
  if (admin.length !== 1) issues.push('ADMIN_GROUP_NOT_UNIQUE');
  const permission = admin[0]?.children?.find(child => child.route === '/permisos');
  if (permission?.modulo !== 'Permisos' || permission?.accion !== 'Administrar') issues.push('PERMISSION_ACTION_WEAKENED');
  const audit = admin[0]?.children?.find(child => child.route === '/auditoria');
  if (audit?.requires_admin !== true) issues.push('AUDIT_ADMIN_GUARD_WEAKENED');
  return issues;
}

const copy = () => structuredClone(current);

test('P5-013.C: un unico archivo real de contrato y escenario acotado, no certificacion de acordeon', () => {
  assert.match(note, /TARGET_PATH[^\n]*`frontend\/src\/app\/shared\/navigation\/navigation-contract\.json`/);
  assert.match(note, /BASELINE_BLOB_SHA[^\n]*`7142082462d7bc53ff4f0cdf96f67f21405f3a44`/);
  assert.match(note, /no prueba[^\n]*una sola rama expandida/i);
  assert.deepEqual(inspect(current), []);
});

test('P5-013.C: rechaza duplicidad del grupo administracion', () => {
  const variant = copy(); variant.groups.push(structuredClone(variant.groups.find(g => g.id === 'administracion')));
  assert.ok(inspect(variant).includes('DUPLICATE_GROUP_ID'));
});

test('P5-013.C: rechaza rutas repetidas entre grupos', () => {
  const variant = copy(); variant.groups[1].children.push(structuredClone(variant.groups[0].children[0]));
  assert.ok(inspect(variant).includes('DUPLICATE_ROUTE'));
});

test('P5-013.C: rechaza rebaja de permiso de administracion', () => {
  const variant = copy(); variant.groups.find(g => g.id === 'administracion').children.find(x => x.route === '/permisos').accion = 'Ver';
  assert.ok(inspect(variant).includes('PERMISSION_ACTION_WEAKENED'));
});

test('P5-013.C: rechaza perdida de requisito de administrador en auditoria', () => {
  const variant = copy(); variant.groups.find(g => g.id === 'administracion').children.find(x => x.route === '/auditoria').requires_admin = false;
  assert.ok(inspect(variant).includes('AUDIT_ADMIN_GUARD_WEAKENED'));
});
