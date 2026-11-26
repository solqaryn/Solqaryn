import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const fail = message => {
  console.error(`[angular-bundle-policy] FAIL: ${message}`);
  process.exitCode = 1;
};

const angular = JSON.parse(read('angular.json'));
const appConfig = read('src/app/app.config.ts');
const routes = read('src/app/app.routes.ts');
const app = read('src/app/app.component.ts');
const navigation = read('src/app/shared/navigation/app-navigation-menu.component.ts');
const home = read('src/app/features/storefront/storefront.component.html');

const budgets = angular.projects['solqaryn-frontend'].architect.build.configurations.production.budgets;
const initial = budgets.find(budget => budget.type === 'initial');

if (!initial || initial.maximumWarning !== '650kb' || initial.maximumError !== '750kb') {
  fail('El budget initial debe permanecer en 650kb warning / 750kb error.');
}
if (!appConfig.includes('provideAnimationsAsync()')) {
  fail('Las animaciones legacy deben cargarse de forma asíncrona.');
}
if (!appConfig.includes('withPreloading(AfterRenderSelectivePreloadingStrategy)')) {
  fail('Debe mantenerse la estrategia selectiva de preload.');
}
if (appConfig.includes('PreloadAllModules') || routes.includes('PreloadAllModules')) {
  fail('PreloadAllModules está prohibido para evitar degradar la carga inicial.');
}
if ((routes.match(/preloadAfterRender: true/g) ?? []).length < 2) {
  fail('Las rutas de storefront más probables deben conservar preload selectivo.');
}
if (!home.includes('@defer (on idle)')) {
  fail('El contenido bajo el fold de la portada debe diferirse hasta idle.');
}
if (app.includes('@angular/material') || navigation.includes('@angular/material')) {
  fail('El shell raíz y su navegación no deben volver a cargar Material JS de forma eager.');
}

// Los paginadores pertenecen a las vistas cargadas bajo demanda, no al bootstrap.
if (appConfig.includes('@angular/material/paginator') || appConfig.includes('paginador-institucional')) {
  fail('El proveedor de paginación debe permanecer fuera del bootstrap.');
}
function inspectPaginatorConsumers(dir) {
  for (const entry of fs.readdirSync(path.join(root, dir), { withFileTypes: true })) {
    const relative = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      inspectPaginatorConsumers(relative);
    } else if (entry.name.endsWith('.ts') && !/\.(spec|test)\.ts$/.test(entry.name)) {
      const source = read(relative);
      const importsPaginator = /import\s*\{[^}]*\bMatPaginator(?:Module)?\b[^}]*\}\s*from\s*['"]@angular\/material\/paginator['"]/.test(source);
      if (importsPaginator && !source.includes('viewProviders: [PAGINADOR_ESPANOL_PROVIDER]')) {
        fail('Falta proveedor local de paginación en ' + relative);
      }
    }
  }
}
inspectPaginatorConsumers('src/app/features');

if (!process.exitCode) console.log('[angular-bundle-policy] PASS');
