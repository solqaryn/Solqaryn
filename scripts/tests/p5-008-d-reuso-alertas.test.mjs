import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// Reuse review: static contract only, not Angular Material browser execution.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/shared/alerts/app-toast.service.ts', import.meta.url)), 'utf8');
function inspect(code) {
  const issues = [];
  const requireRule = (passes, name) => { if (!passes) issues.push(name); };
  requireRule(code.includes("export type AppToastKind = 'info' | 'success' | 'warning' | 'error'") &&
    code.includes('export class AppToastService'), 'TYPE_UNION_MISSING');
  requireRule(code.includes("const tipo = options.tipo ?? 'info'") &&
    code.includes("tipo === 'error' ? 6500 : 4200"), 'DURATION_SEVERITY_MISSING');
  requireRule(code.includes("this.snackBar.open(mensaje, options.accion ?? 'Cerrar'") &&
    !code.includes('snackBar.openFromComponent('), 'TEXT_ONLY_SNACKBAR_MISSING');
  requireRule(code.includes("politeness: tipo === 'error' ? 'assertive' : 'polite'") &&
    code.includes('panelClass: [`app-toast`, `app-toast--${tipo}`]'), 'ARIA_VARIANT_MISSING');
  for (const kind of ['info', 'success', 'warning', 'error']) {
    requireRule(code.includes(`this.mostrar(mensaje, { ...options, tipo: '${kind}' })`), `WRAPPER_${kind.toUpperCase()}_MISSING`);
  }
  return issues;
}
test('P5-008.D: existing real-source AppToastService is reusable without runtime edits', () => {
  assert.deepEqual(inspect(source), []);
});
test('P5-008.D: negative - severity-specific dwell time must remain', () => {
  assert.ok(inspect(source.replace("tipo === 'error' ? 6500 : 4200", '4200')).includes('DURATION_SEVERITY_MISSING'));
});
test('P5-008.D: negative - alert text remains text not arbitrary component HTML', () => {
  assert.ok(inspect(source.replace('this.snackBar.open(mensaje', 'this.snackBar.openFromComponent(mensaje')).includes('TEXT_ONLY_SNACKBAR_MISSING'));
});
test('P5-008.D: negative - error announcements are assertive', () => {
  assert.ok(inspect(source.replace("tipo === 'error' ? 'assertive' : 'polite'", "'polite'")).includes('ARIA_VARIANT_MISSING'));
});
test('P5-008.D: negative - severity wrappers retain explicit kind', () => {
  assert.ok(inspect(source.replace("this.mostrar(mensaje, { ...options, tipo: 'error' })", 'this.mostrar(mensaje, options)')).includes('WRAPPER_ERROR_MISSING'));
});
test('P5-008.D: negative - text action is explicitly dismissible', () => {
  assert.ok(inspect(source.replace("options.accion ?? 'Cerrar'", "options.accion ?? ''")).includes('TEXT_ONLY_SNACKBAR_MISSING'));
});
