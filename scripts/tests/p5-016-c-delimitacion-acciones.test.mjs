import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// Scope P5-016.C: one visual-action surface (close action for nonmodal toast).
// Static source contract only, never a browser or Angular behavior attestation.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/shared/alerts/app-toast.service.ts', import.meta.url)), 'utf8');

function inspect(sourceText) {
  const problems = [];
  const expect = (pass, issue) => { if (!pass) problems.push(issue); };
  expect(/accion\?: string;/.test(sourceText), 'ACTION_OPTION_MISSING');
  expect(/this\.snackBar\.open\(mensaje, options\.accion \?\? 'Cerrar', \{/.test(sourceText), 'CLOSE_DEFAULT_MISSING');
  expect(/panelClass: \[`app-toast`, `app-toast--\$\{tipo\}`\]/.test(sourceText), 'STYLE_VARIANTS_INCONSISTENT');
  for (const kind of ['info', 'success', 'warning', 'error']) {
    expect(sourceText.includes(`this.mostrar(mensaje, { ...options, tipo: '${kind}' });`), `ACTION_ROUTE_${kind.toUpperCase()}`);
  }
  expect(!/\binnerHTML\b|bypassSecurityTrustHtml|document\.write\s*\(/.test(sourceText), 'HTML_INJECTION');
  return problems;
}

test('P5-016.C: accion Cerrar y estilos compartidos en un solo archivo real', () => {
  assert.deepEqual(inspect(source), []);
});
test('P5-016.C: falla si desaparece el fallback de accion Cerrar', () => {
  assert.ok(inspect(source.replace("options.accion ?? 'Cerrar'", "options.accion")).includes('CLOSE_DEFAULT_MISSING'));
});
test('P5-016.C: falla si un wrapper de severidad deja de pasar opciones coherentes', () => {
  assert.ok(inspect(source.replace("tipo: 'success' });", "tipo: 'error' });")).includes('ACTION_ROUTE_SUCCESS'));
});
test('P5-016.C: falla si la severidad deja de participar en clase de estilo', () => {
  assert.ok(inspect(source.replace('`app-toast--${tipo}`', '`app-toast--info`')).includes('STYLE_VARIANTS_INCONSISTENT'));
});
test('P5-016.C: falla cerrado ante HTML no saneado y contrato vacio', () => {
  assert.ok(inspect(source + '\ninnerHTML = mensaje;').includes('HTML_INJECTION'));
  assert.ok(inspect('').length > 3);
});
