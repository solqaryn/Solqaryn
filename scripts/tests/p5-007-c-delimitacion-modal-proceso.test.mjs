import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// DELIMITAR: structural source checks only, not browser/runtime certification.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/shared/anular-dialog.component.ts', import.meta.url)), 'utf8');

function inspect(code) {
  const issues = [];
  const need = (valid, codeName) => { if (!valid) issues.push(codeName); };
  need(code.includes('export class AnularDialogComponent') && code.includes('selector: \'app-anular-dialog\''), 'PROCESS_DIALOG_MISSING');
  need(code.includes('@Inject(MAT_DIALOG_DATA)') && code.includes('MatDialogRef<AnularDialogComponent>'), 'DIALOG_DATA_BOUNDARY_MISSING');
  need(code.includes('{{ data.title }}') && code.includes('{{ data.message }}') && !code.includes('[innerHTML]'), 'SAFE_TEXT_BINDING_MISSING');
  need(code.includes('[(ngModel)]="motivo"') && code.includes('[disabled]="!motivo.trim()"'), 'EMPTY_REASON_GUARD_MISSING');
  need(code.includes('[mat-dialog-close]="undefined"') && code.includes('[mat-dialog-close]="motivo"'), 'CANCEL_OR_CONFIRM_CONTRACT_MISSING');
  return issues;
}

test('P5-007.C: contract of the existing cancellation process dialog', () => {
  assert.deepEqual(inspect(source), []);
});
test('P5-007.C: cancellation cannot silently return the submitted reason', () => {
  assert.ok(inspect(source.replace('[mat-dialog-close]="undefined"', '[mat-dialog-close]="motivo"')).includes('CANCEL_OR_CONFIRM_CONTRACT_MISSING'));
});
test('P5-007.C: disallow submission of empty reason', () => {
  assert.ok(inspect(source.replace('[disabled]="!motivo.trim()"', '[disabled]="false"')).includes('EMPTY_REASON_GUARD_MISSING'));
});
test('P5-007.C: reject raw HTML binding for supplied message', () => {
  const altered = source.replace('{{ data.message }}', '<span [innerHTML]="data.message"></span>');
  assert.ok(inspect(altered).includes('SAFE_TEXT_BINDING_MISSING'));
});
test('P5-007.C: reject missing injected dialog context', () => {
  assert.ok(inspect(source.replace('@Inject(MAT_DIALOG_DATA)', '@Inject(OTHER_DATA)')).includes('DIALOG_DATA_BOUNDARY_MISSING'));
});
