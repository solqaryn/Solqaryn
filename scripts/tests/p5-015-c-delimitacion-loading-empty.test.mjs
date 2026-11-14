import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// Static delimitation of an existing Angular template; no browser/E2E or tenancy certification.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/features/usuarios/usuario-detail.component.html', import.meta.url)), 'utf8');

function inspect(html) {
  const errors = [];
  const need = (pass, code) => { if (!pass) errors.push(code); };
  need(html.includes('@if (loading()) {') && html.includes('<mat-spinner>'), 'LOADING_INDICATOR_MISSING');
  need(html.includes('} @else if (notFound()) {') && html.includes('class="empty"'), 'EMPTY_STATE_MISSING');
  need(html.includes('} @else if (usuario(); as u) {') && html.includes('class="card detail-card"'), 'LOADED_STATE_NOT_GUARDED');
  need(html.includes('{{ u.nombreCompleto }}') && html.includes('{{ u.nombreUsuario }}') && !html.includes('[innerHTML]'), 'TEXT_BINDING_UNSAFE');
  need(html.includes('El usuario no existe o fue eliminado.') && !html.includes('{{ u.email }}'), 'EMPTY_STATE_DISCLOSURE');
  return errors;
}

test('P5-015.C: existing user details loading/empty/loaded states are delimited', () => {
  assert.deepEqual(inspect(source), []);
});
test('P5-015.C: missing spinner is detected', () => {
  assert.ok(inspect(source.replace('<mat-spinner>', '<span>')).includes('LOADING_INDICATOR_MISSING'));
});
test('P5-015.C: losing mutually exclusive empty state is detected', () => {
  assert.ok(inspect(source.replace('} @else if (notFound()) {', '} @if (notFound()) {')).includes('EMPTY_STATE_MISSING'));
});
test('P5-015.C: unguarded loaded view is detected', () => {
  assert.ok(inspect(source.replace('} @else if (usuario(); as u) {', '} @if (usuario(); as u) {')).includes('LOADED_STATE_NOT_GUARDED'));
});
test('P5-015.C: unsafe HTML and PII in empty message are rejected', () => {
  assert.ok(inspect(source.replace('{{ u.nombreCompleto }}', '<span [innerHTML]="u.nombreCompleto"></span>')).includes('TEXT_BINDING_UNSAFE'));
  assert.ok(inspect(source.replace('El usuario no existe o fue eliminado.', 'El usuario no existe: {{ u.email }}')).includes('EMPTY_STATE_DISCLOSURE'));
});
