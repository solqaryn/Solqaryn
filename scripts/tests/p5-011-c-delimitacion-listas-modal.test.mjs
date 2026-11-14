import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// P5-011.C: structural evidence only; does not prove real Angular/keyboard/browser behavior.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/features/inventario/ajustes-list.component.ts', import.meta.url)), 'utf8');

function inspect(text) {
  const errors = [];
  if (!text.includes('class="modal-card"') || !text.includes('role="dialog"') || !text.includes('aria-modal="true"')) errors.push('MODAL_SEMANTICS_MISSING');
  if (!/\.modal-card\s*\{[\s\S]*?max-height:\s*min\(760px,\s*calc\(100vh - 48px\)\);[\s\S]*?overflow:\s*auto;/.test(text)) errors.push('BOUNDED_SCROLL_MISSING');
  if (!text.includes('class="modal-summary"') || !text.includes('{{ ajusteDialog.detalles.length }}')) errors.push('DETAILS_COUNT_MISSING');
  if (!text.includes('class="table-shell"') || !text.includes('<mat-paginator') || !text.includes('@for (ajuste of ajustes();')) errors.push('LIST_PAGE_BOUNDARY_MISSING');
  if (!text.includes('class="modal-close"') || !text.includes('aria-label="Cerrar"')) errors.push('CLOSE_CONTROL_MISSING');
  if (/\binnerHTML\b|bypassSecurityTrustHtml|document\.write\s*\(/i.test(text)) errors.push('UNSAFE_HTML_SINK');
  return errors;
}

test('P5-011.C positive: real modal and list boundaries are documented', () => {
  assert.deepEqual(inspect(source), []);
});
test('P5-011.C negative: unbounded modal scroll is rejected', () => {
  assert.ok(inspect(source.replace('overflow: auto;', 'overflow: visible;')).includes('BOUNDED_SCROLL_MISSING'));
});
test('P5-011.C negative: missing modal semantics is rejected', () => {
  assert.ok(inspect(source.replace('aria-modal="true"', 'aria-modal="false"')).includes('MODAL_SEMANTICS_MISSING'));
});
test('P5-011.C negative: removed detail count is rejected', () => {
  assert.ok(inspect(source.replace('{{ ajusteDialog.detalles.length }}', '{{ ajusteDialog.id }}')).includes('DETAILS_COUNT_MISSING'));
});
test('P5-011.C negative: broken page pagination is rejected', () => {
  assert.ok(inspect(source.replace('<mat-paginator', '<hidden-paginator')).includes('LIST_PAGE_BOUNDARY_MISSING'));
});
test('P5-011.C negative: hidden close label is rejected', () => {
  assert.ok(inspect(source.replace('aria-label="Cerrar"', 'aria-label=""')).includes('CLOSE_CONTROL_MISSING'));
});
test('P5-011.C negative: unsafe HTML sink is rejected', () => {
  assert.ok(inspect(source + '\ninnerHTML = userInput;').includes('UNSAFE_HTML_SINK'));
});
