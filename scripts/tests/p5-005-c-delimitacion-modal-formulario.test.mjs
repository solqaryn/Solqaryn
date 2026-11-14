import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

// P5-005.C delimits a real Material data-entry dialog; no Angular/browser execution is claimed.
const productPath = '../../frontend/src/app/shared/anular-dialog.component.ts';
const product = readFileSync(fileURLToPath(new URL(productPath, import.meta.url)), 'utf8');
const note = readFileSync(fileURLToPath(new URL('../../docs/P5-005-C-DELIMITACION-MODAL-FORMULARIO-DEV.md', import.meta.url)), 'utf8');

function inspect(source) {
  const problems = [];
  const check = (condition, label) => { if (!condition) problems.push(label); };
  check(source.includes("selector: 'app-anular-dialog'") && source.includes('standalone: true'), 'STANDALONE_DIALOG');
  check(/imports:\s*\[[^\]]*FormsModule[^\]]*MatDialogModule[^\]]*MatInputModule/s.test(source), 'MATERIAL_FORMS_IMPORTS');
  check(source.includes('<h2 mat-dialog-title>{{ data.title }}</h2>') && source.includes('{{ data.message }}'), 'SAFE_DIALOG_TEXT');
  check(/<textarea\s+matInput\s+\[\(ngModel\)\]="motivo"\s+rows="2"><\/textarea>/.test(source), 'DATA_ENTRY_BINDING');
  check(/<button\s+mat-button\s+\[mat-dialog-close\]="undefined">Cancelar<\/button>/.test(source), 'CANCEL_IS_NOT_CONFIRM');
  check(/<button\s+mat-flat-button[^>]*\[mat-dialog-close\]="motivo"[^>]*\[disabled\]="!motivo\.trim\(\)"[^>]*>Anular<\/button>/.test(source), 'BLANK_REASON_REJECTED');
  check(/motivo\s*=\s*'';/.test(source) && /MatDialogRef<AnularDialogComponent>/.test(source), 'LOCAL_STATE_DIALOG_REF');
  check(!/\[innerHTML\]|\binnerHTML\b|bypassSecurityTrustHtml|document\.write\s*\(/.test(source), 'NO_UNSAFE_HTML_SINK');
  return problems;
}

test('P5-005.C: prueba el unico dialogo de formulario delimitado, sin afirmar modal universal', () => {
  assert.match(note, /TARGET_PATH[^\n]*`frontend\/src\/app\/shared\/anular-dialog\.component\.ts`/);
  assert.match(note, /BASELINE_BLOB_SHA[^\n]*`d751503438f963f1deebe371cd14623ad6038e16`/);
  assert.match(note, /NO prueba[^\n]*interaccion de navegador/i);
  assert.deepEqual(inspect(product), []);
});

test('P5-005.C: rechaza perdida del binding de entrada', () => {
  assert.ok(inspect(product.replace('[(ngModel)]="motivo"', 'value="motivo"')).includes('DATA_ENTRY_BINDING'));
});

test('P5-005.C: rechaza cancelar convertido en confirmacion', () => {
  assert.ok(inspect(product.replace('[mat-dialog-close]="undefined"', '[mat-dialog-close]="motivo"')).includes('CANCEL_IS_NOT_CONFIRM'));
});

test('P5-005.C: rechaza aceptar motivo vacio o con espacios', () => {
  assert.ok(inspect(product.replace('!motivo.trim()', '!motivo')).includes('BLANK_REASON_REJECTED'));
});

test('P5-005.C: rechaza interpolacion insegura y HTML arbitrario', () => {
  assert.ok(inspect(product.replace('{{ data.message }}', '[innerHTML]="data.message"')).includes('SAFE_DIALOG_TEXT'));
  assert.ok(inspect(product + '\nnode.innerHTML = data.message;').includes('NO_UNSAFE_HTML_SINK'));
});

test('P5-005.C: rechaza interfaz vacia y perdida de dependencia Material Forms', () => {
  assert.ok(inspect('').length >= 7);
  assert.ok(inspect(product.replace('FormsModule, MatDialogModule', 'MatDialogModule')).includes('MATERIAL_FORMS_IMPORTS'));
});
