import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// DELIMITAR only: static source contract, not a browser or Angular E2E check.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/app.component.ts', import.meta.url)), 'utf8');

function inspect(code) {
  const errors = [];
  const need = (condition, key) => { if (!condition) errors.push(key); };
  need(code.includes('id="main-content"') && code.includes('tabindex="-1"') && code.includes('class="skip-link"'), 'FOCUS_TARGET_MISSING');
  need(code.includes("@HostListener('window:keydown.escape')") && code.includes('if (this.sidebarAbierto) this.cerrarSidebar(true)'), 'ESCAPE_CLOSE_MISSING');
  need(code.includes("this.document.querySelector<HTMLElement>('#main-sidebar .cerrar-sidebar')?.focus()") &&
    code.includes("this.document.getElementById('menu-toggle')?.focus()"), 'FOCUS_RETURN_MISSING');
  need(code.includes("this.document.body.style.overflow = 'hidden'") &&
    code.includes("this.document.body.style.removeProperty('overflow')"), 'SCROLL_LOCK_RELEASE_MISSING');
  need(code.includes('main.focus({ preventScroll: true })') &&
    code.includes("main.scrollIntoView({ block: 'start', behavior: 'auto' })") &&
    code.includes('this.routeAnnouncement = titulo ?'), 'NAVIGATION_FOCUS_MISSING');
  need(code.includes('this.tenantContext.limpiar()') && code.includes('this.permisosRuntime.limpiar()'), 'CONTEXT_CLEANUP_MISSING');
  return errors;
}

test('P5-009.C: real-source shell focus, Escape and scroll contract exists', () => {
  assert.deepEqual(inspect(source), []);
});
test('P5-009.C: rejects missing keyboard Escape close', () => {
  assert.ok(inspect(source.replace("@HostListener('window:keydown.escape')", "@HostListener('window:keydown.enter')")).includes('ESCAPE_CLOSE_MISSING'));
});
test('P5-009.C: rejects lost focus restoration to menu toggle', () => {
  assert.ok(inspect(source.replace("this.document.getElementById('menu-toggle')?.focus()", "this.document.getElementById('unknown')?.focus()")).includes('FOCUS_RETURN_MISSING'));
});
test('P5-009.C: rejects body scroll lock without release', () => {
  assert.ok(inspect(source.replaceAll("this.document.body.style.removeProperty('overflow')", "this.document.body.style.overflow = 'hidden'")).includes('SCROLL_LOCK_RELEASE_MISSING'));
});
test('P5-009.C: rejects lack of focus target and navigation guard', () => {
  assert.ok(inspect(source.replace('tabindex="-1"', '')).includes('FOCUS_TARGET_MISSING'));
  assert.ok(inspect(source.replace('main.focus({ preventScroll: true })', 'main.blur()')).includes('NAVIGATION_FOCUS_MISSING'));
});
test('P5-009.C: context cleanup is not silently dropped', () => {
  assert.ok(inspect(source.replaceAll('this.permisosRuntime.limpiar()', 'void 0')).includes('CONTEXT_CLEANUP_MISSING'));
});
