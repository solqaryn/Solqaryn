import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// P5-003.C: one real shared alert/confirmation surface, not four finished business dialogs.
// Structural contract only: does not attest Angular runtime, browser or tenant authorization.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/shared/alerts/app-alert.service.ts', import.meta.url)), 'utf8');

function inspect(text) {
  const errors = [];
  const need = (condition, code) => { if (!condition) errors.push(code); };
  need(text.includes("export type AppAlertKind = 'info' | 'success' | 'warning' | 'error' | 'confirm';"), 'KINDS_CONTRACT_MISSING');
  need(text.includes('export interface AppAlertData {') && text.includes('titulo: string;') && text.includes('mensaje: string;'), 'DATA_CONTRACT_MISSING');
  need(text.includes('async confirmar(data: AppAlertData): Promise<boolean>') && text.includes('return result === true;'), 'BOOLEAN_CONFIRMATION_MISSING');
  need(text.includes('async solicitarTexto(data: AppAlertData): Promise<string | null>') && text.includes("return typeof result === 'string' && result ? result : null;"), 'TEXT_CANCELLATION_GUARD_MISSING');
  need(text.includes('ref.close(null)') && text.includes('ref.close(this.data.entrada ? this.valor.trim() : true)'), 'CANCEL_OR_CONFIRM_CLOSE_MISSING');
  need((text.match(/restoreFocus: true/g) ?? []).length >= 2 && (text.match(/autoFocus: false/g) ?? []).length >= 2, 'FOCUS_RESTORE_CONTRACT_MISSING');
  need(text.includes('maxlength="300"') && text.includes('[disabled]="!!data.entrada?.requerida && !valor.trim()"'), 'REQUIRED_TEXT_GUARD_MISSING');
  need(!/\binnerHTML\b|bypassSecurityTrustHtml|document\.write\s*\(/.test(text), 'UNSAFE_HTML_SINK');
  return errors;
}

test('P5-003.C positive: one existing MatDialog alert/confirmation source boundary', () => {
  assert.deepEqual(inspect(source), []);
});
test('P5-003.C negative: confirmation must return a strict boolean', () => {
  assert.ok(inspect(source.replace('return result === true;', 'return !!result;')).includes('BOOLEAN_CONFIRMATION_MISSING'));
});
test('P5-003.C negative: cancellation may not turn into accepted text', () => {
  assert.ok(inspect(source.replace("return typeof result === 'string' && result ? result : null;", 'return result;')).includes('TEXT_CANCELLATION_GUARD_MISSING'));
});
test('P5-003.C negative: missing focus restoration is rejected', () => {
  assert.ok(inspect(source.replaceAll('restoreFocus: true', 'restoreFocus: false')).includes('FOCUS_RESTORE_CONTRACT_MISSING'));
});
test('P5-003.C negative: disabled empty required input and HTML injection', () => {
  assert.ok(inspect(source.replace('maxlength="300"', 'maxlength="0"')).includes('REQUIRED_TEXT_GUARD_MISSING'));
  assert.ok(inspect(source + '\ninnerHTML = data.mensaje;').includes('UNSAFE_HTML_SINK'));
});
