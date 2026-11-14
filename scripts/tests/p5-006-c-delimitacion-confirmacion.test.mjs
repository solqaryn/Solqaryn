import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// P5-006.C: scope is one existing Angular confirmation component, not a new modal framework.
const path = fileURLToPath(new URL('../../frontend/src/app/shared/confirm-dialog.component.ts', import.meta.url));
const actual = readFileSync(path, 'utf8');

function inspectConfirmation(source) {
  const failed = [];
  const check = (valid, id) => { if (!valid) failed.push(id); };
  check(/export interface ConfirmDialogData\s*\{\s*title: string;\s*message: string;\s*\}/.test(source), 'DATA_CONTRACT');
  check(/selector: 'app-confirm-dialog'/.test(source) && /standalone: true/.test(source), 'STANDALONE_COMPONENT');
  check(/imports:\s*\[MatDialogModule, MatButtonModule\]/.test(source), 'MATERIAL_DIALOG_IMPORTS');
  check(/<h2 mat-dialog-title>\{\{ data\.title \}\}<\/h2>/.test(source) && /<mat-dialog-content>\{\{ data\.message \}\}<\/mat-dialog-content>/.test(source), 'SAFE_TEXT_CONTENT');
  check(/<button mat-button \[mat-dialog-close\]="false">Cancelar<\/button>/.test(source), 'CANCEL_FALSE');
  check(/<button mat-flat-button color="warn" \[mat-dialog-close\]="true">Eliminar<\/button>/.test(source), 'DESTRUCTIVE_CONFIRM_TRUE');
  check(!/\binnerHTML\b|bypassSecurityTrustHtml|\bdocument\.write\s*\(/.test(source), 'NO_UNSAFE_HTML');
  check(/@Inject\(MAT_DIALOG_DATA\) public data: ConfirmDialogData/.test(source), 'INJECT_TYPED_DATA');
  return failed;
}

test('P5-006.C: delimitacion del confirmador real valida contrato, texto seguro y cancelacion', () => {
  assert.deepEqual(inspectConfirmation(actual), []);
});
test('P5-006.C: deniega regresion en que Cancelar devuelve true', () => {
  const mutant = actual.replace('[mat-dialog-close]="false"', '[mat-dialog-close]="true"');
  assert.ok(inspectConfirmation(mutant).includes('CANCEL_FALSE'));
});
test('P5-006.C: deniega confirmacion destructiva cuando pierde warning o devuelve false', () => {
  assert.ok(inspectConfirmation(actual.replace('color="warn"', 'color="primary"')).includes('DESTRUCTIVE_CONFIRM_TRUE'));
  assert.ok(inspectConfirmation(actual.replace('[mat-dialog-close]="true"', '[mat-dialog-close]="false"')).includes('DESTRUCTIVE_CONFIRM_TRUE'));
});
test('P5-006.C: rechaza HTML sin escapar en el mensaje', () => {
  const mutant = actual.replace('{{ data.message }}', '<div [innerHTML]="data.message"></div>');
  assert.ok(inspectConfirmation(mutant).includes('SAFE_TEXT_CONTENT'));
  assert.ok(inspectConfirmation(mutant).includes('NO_UNSAFE_HTML'));
});
test('P5-006.C: rechaza perdida del contrato tipado de datos', () => {
  assert.ok(inspectConfirmation(actual.replace('message: string;', 'message: any;')).includes('DATA_CONTRACT'));
});
test('P5-006.C: rechaza componente vacio y retirada de modulo de dialogo', () => {
  assert.ok(inspectConfirmation('').length >= 6);
  assert.ok(inspectConfirmation(actual.replace('MatDialogModule, MatButtonModule', 'MatButtonModule')).includes('MATERIAL_DIALOG_IMPORTS'));
});
