#!/usr/bin/env node
// P0-006.B: static guard/precedence regression only. Not an E2E or backend RBAC test.
// Read-only: never calls an API or changes route definitions.
import { readFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const read = (file) => readFileSync(join(root, file), 'utf8');
const router = read('frontend/src/app/app.routes.ts');
const config = read('frontend/src/app/app.config.ts');
const menu = read('frontend/src/app/shared/navigation/app-navigation-menu.component.ts');
const auth = read('frontend/src/app/core/guards/auth.guard.ts');
const permiso = read('frontend/src/app/core/guards/permiso.guard.ts');

const requireCheck = (ok, message) => {
  if (!ok) throw new Error('P0-006.B FAIL: ' + message);
};
const count = (text, regex) => [...text.matchAll(regex)].length;

function safetyChecks(source) {
  return {
    exactStoreBinding: /path:\s*['"]tiendas\/:storeKey['"]/.test(source),
    noGenericRootSlug: !/\bpath\s*:\s*['"]:(?:slug|storeKey)['"]/.test(source),
    dashboardGuards: /path:\s*['"]dashboard['"]\s*,\s*canActivate:\s*\[authGuard,\s*permisoGuard\]/.test(source),
    userGuards: /path:\s*['"]usuarios['"]\s*,\s*canActivate:\s*\[authGuard,\s*permisoGuard\]/.test(source),
    platformGuard: /path:\s*['"]plataforma['"]\s*,\s*canActivate:\s*\[plataformaGuard\]/.test(source),
    reportsParentAuth: /path:\s*['"]centro-reportes['"]\s*,\s*canActivate:\s*\[authGuard\]/.test(source),
    catchallIsRedirect: /\{\s*path:\s*['"]\*\*['"]\s*,\s*redirectTo:\s*['"]['"]\s*\}/.test(source),
  };
}

const baseline = safetyChecks(router);
for (const [name, ok] of Object.entries(baseline)) requireCheck(ok, name);
requireCheck(count(router, /\bpath\s*:/g) === 92, '92 path declarations');
requireCheck(count(router, /loadComponent\s*:/g) === 90, '90 lazy component declarations');
requireCheck(count(router, /redirectTo\s*:/g) === 2, 'two redirects');
requireCheck(count(menu, /<a\s+routerLink="/g) === 39, '39 navigation links');
requireCheck(/\.\.\.UBICACIONES_ALMACEN_ROUTES[\s\S]*\.\.\.routes/.test(config), 'configured routes order');
requireCheck(auth.includes('tieneContextoVerificado()'), 'verified tenant in authGuard');
requireCheck(permiso.includes('permisosRuntime.rutaHabilitada(rutaCompleta)'), 'full path permission check');
requireCheck(permiso.includes('permisosRuntime.puede(modulo, accion)'), 'runtime capability check');

const negativeCases = [
  ['generic-public-slug', router.replace("path: 'tiendas/:storeKey'", "path: ':slug'")],
  ['dashboard-without-guards', router.replace("path: 'dashboard', canActivate: [authGuard, permisoGuard]", "path: 'dashboard'")],
  ['reports-parent-without-auth', router.replace("path: 'centro-reportes', canActivate: [authGuard]", "path: 'centro-reportes'")],
  ['wildcard-is-component', router.replace("{ path: '**', redirectTo: '' }", "{ path: '**', loadComponent: () => null }")],
];
if (process.argv.includes('--self-test')) {
  for (const [name, mutation] of negativeCases) {
    requireCheck(mutation !== router, 'fixture mutation did not apply: ' + name);
    requireCheck(Object.values(safetyChecks(mutation)).some((ok) => !ok),
      'unsafe mutation was accepted: ' + name);
  }
}

console.log('P0-006.B STATIC PASS: 7 router guards/patterns, route counts 92/90/2, 39 links, tenant and permission source guards.');
if (process.argv.includes('--self-test')) console.log('P0-006.B NEGATIVE PASS: 4/4 synthetic mutations rejected.');
console.log('LIMIT: static checks do NOT certify live E2E navigation, server-side RBAC, tenant isolation, or DEV deployment parity.');
