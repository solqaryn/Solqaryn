import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

// P5-002.C checks a single source file. This is not an Angular/browser/E2E test.
const src = readFileSync(fileURLToPath(new URL('../../frontend/src/app/app.component.ts', import.meta.url)), 'utf8');

export function inspectShell(source) {
  const failures = [];
  const expect = (ok, name) => { if (!ok) failures.push(name); };
  expect(source.includes('@if (auth.isAuthenticated()) {'), 'AUTHENTICATED_SHELL_GATE');
  expect(/}\s*@else\s*{\s*<router-outlet><\/router-outlet>/s.test(source), 'UNAUTHENTICATED_ROUTE_OUTLET');
  expect(source.includes('href="#main-content"') && source.includes('class="skip-link"'), 'SKIP_LINK');
  expect(source.includes('<main id="main-content" class="content" tabindex="-1">'), 'MAIN_FOCUS_TARGET');
  expect(source.includes('<aside id="main-sidebar"') && source.includes('<app-navigation-menu />'), 'SIDEBAR_NAVIGATION');
  expect(source.includes('aria-controls="main-sidebar"') && source.includes('[attr.aria-expanded]="sidebarAbierto"'), 'TOGGLE_ACCESSIBILITY');
  expect(source.includes("@HostListener('window:keydown.escape')") && /onEscape\(\): void\s*{\s*if \(this\.sidebarAbierto\) this\.cerrarSidebar\(true\);/.test(source), 'ESCAPE_FOCUS_RETURN');
  const tenantChange = source.split('cambiarEmpresa(): void {')[1]?.split('\n  logout(): void')[0] ?? '';
  expect(tenantChange.includes('this.tenantContext.limpiar();') && tenantChange.includes('this.permisosRuntime.limpiar();') && tenantChange.includes("this.router.navigateByUrl('/login')"), 'TENANT_RESET_REVALIDATION');
  expect(!/\binnerHTML\b|bypassSecurityTrustHtml|document\.write\s*\(/.test(source), 'NO_RAW_HTML_SINK');
  return failures;
}

test('P5-002.C: shell real presenta puerta, navegacion y anclas accesibles', () => {
  assert.deepEqual(inspectShell(src), []);
});
test('P5-002.C: sin puerta de autenticacion se rechaza el candidato', () => {
  const mutation = src.replace('@if (auth.isAuthenticated()) {', '@if (true) {');
  assert.ok(inspectShell(mutation).includes('AUTHENTICATED_SHELL_GATE'));
});
test('P5-002.C: sin enlace de salto o destino de foco se rechaza', () => {
  assert.ok(inspectShell(src.replace('href="#main-content"', 'href="#wrong-target"')).includes('SKIP_LINK'));
  assert.ok(inspectShell(src.replace('tabindex="-1"', '')).includes('MAIN_FOCUS_TARGET'));
});
test('P5-002.C: acceso lateral pierde control ARIA o escape y falla', () => {
  assert.ok(inspectShell(src.replace('aria-controls="main-sidebar"', '')).includes('TOGGLE_ACCESSIBILITY'));
  assert.ok(inspectShell(src.replace("@HostListener('window:keydown.escape')", "@HostListener('window:keydown.enter')")).includes('ESCAPE_FOCUS_RETURN'));
});
test('P5-002.C: reset tenant incompleto o HTML crudo falla cerrado', () => {
  assert.ok(inspectShell(src.replace('this.permisosRuntime.limpiar();', '/* deleted */')).includes('TENANT_RESET_REVALIDATION'));
  assert.ok(inspectShell(src + '\nelement.innerHTML = payload;').includes('NO_RAW_HTML_SINK'));
});
test('P5-002.C: un fuente vacio no produce PASS', () => {
  assert.ok(inspectShell('').length >= 6);
});
