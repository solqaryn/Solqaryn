import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// DELIMITAR, no certifica paginacion Angular ni comportamiento en navegador.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/core/navigation/list-navigation-state.service.ts', import.meta.url)), 'utf8');

function inspect(code) {
  const issues = [];
  const requireSource = (testValue, issue) => { if (!testValue) issues.push(issue); };
  requireSource(code.includes('export class ListNavigationStateService'), 'TARGET_SERVICE_MISSING');
  requireSource(code.includes('for (const key of Object.keys(defaultRecord))'), 'DEFAULT_KEY_ALLOWLIST_MISSING');
  requireSource(code.includes('route.snapshot.queryParamMap.get(key)') && code.includes('if (queryValue !== null)'), 'QUERY_PARAM_OVERRIDE_MISSING');
  requireSource(code.includes('queryParams,') && code.includes('replaceUrl: true'), 'CANONICAL_URL_REPLACE_MISSING');
  requireSource(code.includes("(this.auth.nombreUsuario() || 'anonimo').trim().toLowerCase()") && code.includes('encodeURIComponent(usuario)'), 'USER_KEY_SCOPING_MISSING');
  requireSource(code.includes('try {\n        storage.setItem(') && code.includes('catch {'), 'STORAGE_DENIAL_GUARD_MISSING');
  requireSource(code.includes('Number.isFinite(numberValue) ? numberValue : fallback'), 'NUMERIC_FALLBACK_MISSING');
  requireSource(code.includes("value === 'false') return false"), 'BOOLEAN_FALSE_FALLBACK_MISSING');
  return issues;
}

test('P5-010.C: servicio real de filtros y estado de listas, solo delimitacion', () => {
  assert.deepEqual(inspect(source), []);
});
test('P5-010.C: rechaza perdida de whitelist de claves de filtro', () => {
  assert.ok(inspect(source.replaceAll('Object.keys(defaultRecord)', 'Object.keys(stateRecord)')).includes('DEFAULT_KEY_ALLOWLIST_MISSING'));
});
test('P5-010.C: rechaza ausencia de precedencia de query param y navegacion estable', () => {
  const badQuery = source.replace('route.snapshot.queryParamMap.get(key)', 'route.snapshot.queryParamMap.get(\'wrong\')');
  assert.ok(inspect(badQuery).includes('QUERY_PARAM_OVERRIDE_MISSING'));
  assert.ok(inspect(source.replace('replaceUrl: true', 'replaceUrl: false')).includes('CANONICAL_URL_REPLACE_MISSING'));
});
test('P5-010.C: rechaza quitar namespace por usuario y fallback numerico', () => {
  assert.ok(inspect(source.replace('encodeURIComponent(usuario)', 'usuario')).includes('USER_KEY_SCOPING_MISSING'));
  assert.ok(inspect(source.replace('Number.isFinite(numberValue) ? numberValue : fallback', 'numberValue')).includes('NUMERIC_FALLBACK_MISSING'));
});
test('P5-010.C: rechaza quitar la guardia de escritura ante storage bloqueado', () => {
  const unsafe = source.replace('try {\n        storage.setItem(', 'if (true) {\n        storage.setItem(');
  assert.ok(inspect(unsafe).includes('STORAGE_DENIAL_GUARD_MISSING'));
  assert.ok(inspect('').length >= 5);
});
