import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// P5-008.C is a single-file, static delimitation. It does not emulate Angular or a browser.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/shared/alerts/app-toast.service.ts', import.meta.url)), 'utf8');

function inspectToastContract(code) {
  const errors = [];
  const requireSource = (condition, code) => { if (!condition) errors.push(code); };
  requireSource(/export type AppToastKind = 'info' \| 'success' \| 'warning' \| 'error';/.test(code), 'KIND_CONTRACT');
  requireSource(/const tipo = options\.tipo \?\? 'info';/.test(code), 'SAFE_DEFAULT_KIND');
  requireSource(/const duracionMs = options\.duracionMs \?\? \(tipo === 'error' \? 6500 : 4200\);/.test(code), 'ERROR_DURATION');
  requireSource(/this\.snackBar\.open\(mensaje, options\.accion \?\? 'Cerrar', \{/.test(code), 'TEXT_MESSAGE_AND_CLOSE');
  requireSource(/politeness: tipo === 'error' \? 'assertive' : 'polite'/.test(code), 'ACCESSIBLE_URGENCY');
  requireSource(/panelClass: \[`app-toast`, `app-toast--\$\{tipo\}`\]/.test(code), 'THEME_VARIANT');
  for (const kind of ['info', 'success', 'warning', 'error']) {
    requireSource(code.includes(`this.mostrar(mensaje, { ...options, tipo: '${kind}' });`), `WRAPPER_${kind.toUpperCase()}`);
  }
  requireSource(!/\binnerHTML\b|bypassSecurityTrustHtml|document\.write\s*\(/.test(code), 'RAW_HTML_UNSAFE');
  return errors;
}

const clone = () => source;

test('P5-008.C: contrato real del toast tiene variantes y anuncio accesible', () => {
  assert.deepEqual(inspectToastContract(source), []);
});
test('P5-008.C: no acepta rebajar la prioridad accesible de error', () => {
  const mutated = clone().replace("politeness: tipo === 'error' ? 'assertive' : 'polite'", "politeness: 'polite'");
  assert.ok(inspectToastContract(mutated).includes('ACCESSIBLE_URGENCY'));
});
test('P5-008.C: no acepta duración de error igual a mensajes informativos', () => {
  const mutated = clone().replace("tipo === 'error' ? 6500 : 4200", '4200');
  assert.ok(inspectToastContract(mutated).includes('ERROR_DURATION'));
});
test('P5-008.C: no acepta eliminación de wrapper error ni HTML inseguro', () => {
  assert.ok(inspectToastContract(clone().replace("tipo: 'error' });", "tipo: 'info' });")).includes('WRAPPER_ERROR'));
  assert.ok(inspectToastContract(clone() + '\ninnerHTML = mensaje;').includes('RAW_HTML_UNSAFE'));
});
test('P5-008.C: un contrato vacío no se acepta como PASS', () => {
  assert.ok(inspectToastContract('').length >= 4);
});
