import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

// P5-014.C delimits a single Angular source surface; not an interactive browser test.
const source = readFileSync(fileURLToPath(new URL('../../frontend/src/app/shared/navigation/app-navigation-menu.component.ts', import.meta.url)), 'utf8');

export function inspectMenuAria(code) {
  const issues = [];
  const requireContract = (ok, label) => { if (!ok) issues.push(label); };
  requireContract(code.includes('<section class="nav-group" aria-labelledby="nav-inicio">') && code.includes('id="nav-inicio"'), 'SECTION_LABEL');
  requireContract(code.includes('<a routerLink="/dashboard" routerLinkActive="active" ariaCurrentWhenActive="page"'), 'CURRENT_PAGE_ARIA');
  requireContract(/<a routerLink="\/dashboard"/.test(code) && /a:focus-visible\s*\{/.test(code), 'NATIVE_KEYBOARD_FOCUS');
  requireContract(/a\s*\{[^}]*min-height:\s*46px;/s.test(code), 'KEYBOARD_TARGET_SIZE');
  requireContract(code.includes('aria-hidden="true"'), 'DECORATIVE_ICON_HIDDEN');
  requireContract(code.includes("@if (permisosRuntime.puede('Permisos', 'Administrar')) {"), 'ADMIN_PERMISSIONS_GUARD');
  requireContract(code.includes("@if (permisosRuntime.esAdministrador() && permisosRuntime.puede('Auditoria', 'Ver')) {"), 'AUDIT_ADMIN_GUARD');
  requireContract(!/\binnerHTML\b|bypassSecurityTrustHtml|document\.write\s*\(/.test(code), 'NO_UNSAFE_HTML');
  return issues;
}

test('P5-014.C: fuente real conserva navegación semántica ARIA y foco', () => {
  assert.deepEqual(inspectMenuAria(source), []);
});
test('P5-014.C: quitar asociación del grupo o página actual falla', () => {
  assert.ok(inspectMenuAria(source.replace('aria-labelledby="nav-inicio"', '')).includes('SECTION_LABEL'));
  assert.ok(inspectMenuAria(source.replace('<a routerLink="/dashboard" routerLinkActive="active" ariaCurrentWhenActive="page"', '<a routerLink="/dashboard" routerLinkActive="active"')).includes('CURRENT_PAGE_ARIA'));
});
test('P5-014.C: quitar foco visual o superficie táctil falla', () => {
  assert.ok(inspectMenuAria(source.replace('a:focus-visible', 'a:focus-disabled')).includes('NATIVE_KEYBOARD_FOCUS'));
  assert.ok(inspectMenuAria(source.replace('min-height: 46px;', 'min-height: 20px;')).includes('KEYBOARD_TARGET_SIZE'));
});
test('P5-014.C: no se pierde autoridad para permisos y auditoría', () => {
  assert.ok(inspectMenuAria(source.replace("@if (permisosRuntime.puede('Permisos', 'Administrar')) {", '@if (true) {')).includes('ADMIN_PERMISSIONS_GUARD'));
  assert.ok(inspectMenuAria(source.replace("@if (permisosRuntime.esAdministrador() && permisosRuntime.puede('Auditoria', 'Ver')) {", "@if (true) {")).includes('AUDIT_ADMIN_GUARD'));
});
test('P5-014.C: HTML inseguro se rechaza', () => {
  assert.ok(inspectMenuAria(source + '\nelement.innerHTML = input;').includes('NO_UNSAFE_HTML'));
});
test('P5-014.C: fuente vacía no se acepta', () => {
  assert.ok(inspectMenuAria('').length >= 6);
});
