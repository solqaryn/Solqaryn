import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// P5-012.C: baseline of a two-level navigation contract, NOT proof of unlimited hierarchy.
const data = JSON.parse(readFileSync(fileURLToPath(new URL('../../frontend/src/app/shared/navigation/navigation-contract.json', import.meta.url)), 'utf8'));

function inspect(value) {
  const issues = [];
  const need = (ok, code) => { if (!ok) issues.push(code); };
  need(value?.schema === 'solqaryn-navigation-contract/v1', 'SCHEMA_UNKNOWN');
  need(typeof value?.authority === 'string' && value.authority.includes('backend authorization'), 'BACKEND_AUTHORITY_MISSING');
  need(value?.remove_safe_deletions === 0, 'DELETION_GUARD_MISSING');
  need(Array.isArray(value?.groups) && value.groups.length > 0, 'GROUPS_MISSING');
  if (!Array.isArray(value?.groups)) return issues;
  const ids = new Set(), routes = new Set();
  for (const g of value.groups) {
    need(typeof g.id === 'string' && g.id.length > 0 && !ids.has(g.id), 'DUPLICATE_OR_EMPTY_GROUP');
    ids.add(g.id);
    need(Array.isArray(g.children) && g.children.length > 0, 'CHILDREN_MISSING');
    for (const c of g.children ?? []) {
      need(typeof c.route === 'string' && c.route.startsWith('/') && !routes.has(c.route), 'DUPLICATE_OR_INVALID_ROUTE');
      routes.add(c.route);
      need(typeof c.modulo === 'string' && !!c.modulo && typeof c.accion === 'string' && !!c.accion, 'MISSING_PERMISSION_METADATA');
      need(!Object.hasOwn(c, 'children'), 'NESTED_DEPTH_NOT_SUPPORTED_V1');
      need(!Object.hasOwn(c, 'requires_admin') || c.requires_admin === true, 'ADMIN_GUARD_TAMPER');
    }
  }
  return issues;
}

test('P5-012.C positive: esquema real padres/hijos y permisos informativos', () => {
  assert.deepEqual(inspect(data), []);
  assert.ok(data.groups.length >= 2);
});
test('P5-012.C negative: invalidar version, autoridad backend y guard borrado', () => {
  const altered = { ...data, schema: 'v0', authority: '', remove_safe_deletions: 1 };
  assert.ok(inspect(altered).includes('SCHEMA_UNKNOWN'));
  assert.ok(inspect(altered).includes('BACKEND_AUTHORITY_MISSING'));
  assert.ok(inspect(altered).includes('DELETION_GUARD_MISSING'));
});
test('P5-012.C negative: duplicar grupo y ruta no pasa', () => {
  const clone = structuredClone(data);
  clone.groups.push(structuredClone(clone.groups[0]));
  assert.ok(inspect(clone).includes('DUPLICATE_OR_EMPTY_GROUP'));
  assert.ok(inspect(clone).includes('DUPLICATE_OR_INVALID_ROUTE'));
});
test('P5-012.C negative: ruta sin permiso y admin bypass', () => {
  const clone = structuredClone(data);
  clone.groups[0].children[0].modulo = '';
  clone.groups[0].children[0].requires_admin = false;
  assert.ok(inspect(clone).includes('MISSING_PERMISSION_METADATA'));
  assert.ok(inspect(clone).includes('ADMIN_GUARD_TAMPER'));
});
test('P5-012.C negative: profundidad arbitraria no certificada por v1', () => {
  const clone = structuredClone(data);
  clone.groups[0].children[0].children = [{ route: '/otra', modulo: 'Otro', accion: 'Ver' }];
  assert.ok(inspect(clone).includes('NESTED_DEPTH_NOT_SUPPORTED_V1'));
});
