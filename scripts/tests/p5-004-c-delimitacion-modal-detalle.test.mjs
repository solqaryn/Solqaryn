import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// P5-004.C delimits the detail field of one real dialog component, not a browser test.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/shared/alerts/app-alert.service.ts', import.meta.url)), 'utf8');

export function inspectDetailDialog(code) {
  const failures = [];
  const requireContract = (ok, key) => { if (!ok) failures.push(key); };
  requireContract(/export interface AppAlertData\s*\{[^}]*detalle\?: string;/s.test(code), 'TYPED_OPTIONAL_DETAIL');
  requireContract(/@if \(data\.detalle\)\s*\{\s*<div class="app-alert__detail">\{\{ data\.detalle \}\}<\/div>/s.test(code), 'SAFE_INTERPOLATED_DETAIL');
  requireContract(code.includes('<h2>{{ data.titulo }}</h2>') && code.includes('<p>{{ data.mensaje }}</p>'), 'TITLE_AND_DESCRIPTION');
  requireContract(/\.app-alert__detail\s*\{[^}]*overflow-wrap:\s*anywhere;/s.test(code), 'LONG_DETAIL_WRAP');
  requireContract(code.includes('(click)="ref.close(null)"') && code.includes("data.cancelarTexto || 'Cancelar'"), 'CANCEL_WITHOUT_COMMIT');
  requireContract((code.match(/restoreFocus: true/g) ?? []).length === 2 && (code.match(/autoFocus: false/g) ?? []).length === 2, 'MODAL_FOCUS_RETURN');
  requireContract(!/\binnerHTML\b|bypassSecurityTrustHtml|document\.write\s*\(/.test(code), 'NO_UNSAFE_HTML');
  return failures;
}

test('P5-004.C: contrato real de detalle optativo interpolado y cancelable', () => {
  assert.deepEqual(inspectDetailDialog(source), []);
});
test('P5-004.C: detalle sin tipo o sin rama de visualización falla', () => {
  assert.ok(inspectDetailDialog(source.replace('detalle?: string;', 'detalle: any;')).includes('TYPED_OPTIONAL_DETAIL'));
  assert.ok(inspectDetailDialog(source.replace('@if (data.detalle)', '@if (false)')).includes('SAFE_INTERPOLATED_DETAIL'));
});
test('P5-004.C: ningún dato debe entrar como HTML crudo', () => {
  assert.ok(inspectDetailDialog(source.replace('{{ data.detalle }}', '<span [innerHTML]="data.detalle"></span>')).includes('NO_UNSAFE_HTML'));
});
test('P5-004.C: cancelar y devolver foco son invariantes', () => {
  assert.ok(inspectDetailDialog(source.replace('(click)="ref.close(null)"', '(click)="confirmar()"')).includes('CANCEL_WITHOUT_COMMIT'));
  assert.ok(inspectDetailDialog(source.replace('restoreFocus: true', 'restoreFocus: false')).includes('MODAL_FOCUS_RETURN'));
});
test('P5-004.C: evita detalles sin wrap y título vacío', () => {
  assert.ok(inspectDetailDialog(source.replace('.app-alert__detail {', '.app-alert__detail-disabled {')).includes('LONG_DETAIL_WRAP'));
  assert.ok(inspectDetailDialog(source.replace('<h2>{{ data.titulo }}</h2>', '<h2></h2>')).includes('TITLE_AND_DESCRIPTION'));
});
test('P5-004.C: fuente vacía no pasa', () => {
  assert.ok(inspectDetailDialog('').length >= 5);
});
