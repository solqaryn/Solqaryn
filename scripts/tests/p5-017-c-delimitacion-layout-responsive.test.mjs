import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// P5-017.C: source-level map of ONE reusable alert/confirm modal, not browser WCAG certification.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/shared/alerts/app-alert.service.ts', import.meta.url)), 'utf8');

function inspect(code) {
  const issues = [];
  const guard = (ok, issue) => { if (!ok) issues.push(issue); };
  guard((code.match(/width: 'min\(94vw, 540px\)'/g) ?? []).length === 2, 'DIALOG_VIEWPORT_WIDTH');
  guard((code.match(/maxWidth: '94vw'/g) ?? []).length === 2, 'DIALOG_MAX_VIEWPORT_WIDTH');
  guard(code.includes('@media (max-width: 480px) {'), 'MOBILE_BREAKPOINT');
  guard(/footer\s*\{\s*flex-direction:\s*column-reverse;/.test(code), 'MOBILE_ACTION_STACK');
  guard(/footer button\s*\{\s*width:\s*100%;/.test(code), 'MOBILE_ACTION_FULL_WIDTH');
  guard((code.match(/restoreFocus: true/g) ?? []).length === 2, 'FOCUS_RESTORE');
  guard(code.includes("this.ref.close(this.data.entrada ? this.valor.trim() : true);"), 'CONFIRMATION_CANCEL_SEMANTICS');
  guard(!/\binnerHTML\b|bypassSecurityTrustHtml|document\.write\s*\(/.test(code), 'HTML_INJECTION');
  return issues;
}

test('P5-017.C positive: dos dialogos comparten ancho relativo y acciones mobile', () => {
  assert.deepEqual(inspect(source), []);
});
test('P5-017.C negative: ancho de viewport sin limite para dialogo', () => {
  assert.ok(inspect(source.replaceAll("width: 'min(94vw, 540px)'", "width: '540px'")).includes('DIALOG_VIEWPORT_WIDTH'));
});
test('P5-017.C negative: desaparece breakpoint para pantallas pequenas', () => {
  assert.ok(inspect(source.replace('@media (max-width: 480px) {', '@media (min-width: 1200px) {')).includes('MOBILE_BREAKPOINT'));
});
test('P5-017.C negative: acciones no apiladas ni ocupan ancho completo', () => {
  const altered = source.replace('flex-direction: column-reverse;', 'flex-direction: row;').replace(/footer button\s*\{\s*width:\s*100%;/, 'footer button { width: auto;');
  assert.ok(inspect(altered).includes('MOBILE_ACTION_STACK'));
  assert.ok(inspect(altered).includes('MOBILE_ACTION_FULL_WIDTH'));
});
test('P5-017.C negative: foco no restaurado y sink HTML inseguro', () => {
  const changed = source.replaceAll('restoreFocus: true', 'restoreFocus: false') + '\ninnerHTML = data.mensaje;';
  assert.ok(inspect(changed).includes('FOCUS_RESTORE'));
  assert.ok(inspect(changed).includes('HTML_INJECTION'));
});
