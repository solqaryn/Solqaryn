import type { C1401QaControl } from './c14-01-m04-01-qa-global.component';

/** Inventario LOCAL estatico. No consulta GH, no contiene estados PASS ni datos empresariales. */
export const qaControles: readonly C1401QaControl[] = [
  { id: 'scope', titulo: 'Gobierno y procedencia', alcance: 'Scope Lock STRICT y procedencia V5 del SHA exacto.' },
  { id: 'backend', titulo: 'Backend y aislamiento', alcance: '.NET Release, tests y rechazos de tenant A/B, JWT y RBAC cuando correspondan.' },
  { id: 'frontend', titulo: 'Frontend y experiencia', alcance: 'Angular tipado, build y cobertura de rutas con guards.' },
  { id: 'browser', titulo: 'Navegador y accesibilidad', alcance: 'Playwright/Chromium, teclado, estados y responsive cuando aplique.' },
  { id: 'mysql', titulo: 'Persistencia e integración', alcance: 'MySQL 8.4 efimero, migraciones y tests Category=Integration cuando correspondan.' }
];
