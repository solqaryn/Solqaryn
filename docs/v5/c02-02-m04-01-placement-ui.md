# SOLQARYN V17 — C02-02.M04.01 ruta privada de placement shared/dedicated

Autor DEV03 Javier, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. PROJECT_SCOPE_LOCK=STRICT, rama dev exclusivamente. Riesgo R2: UI relativa a DB y aislamiento tenant, no autorización real de migraciones. V17 lock SQV17_C02_02_M04_01 (2000000344).

## A0 / arquitectura
Componente Angular 22.2.1 `plataforma/placement-v5` diferido y protegido por `plataformaGuard` (sesión server-side /plataforma/sesion), muestra dos modos conceptuales y cuatro condiciones necesarias para decidir placement/migración. Se deniega al revocar sesión y no contiene controles, DSN, endpoints, políticas de asignación reales ni mutaciones. La mera visualización no habilita MySQL, router server-side, migraciones, restauración ni planes de cliente. Diff limitado a rutas/frontend, test, workflow, documento y CHANGELOG. Rollback por git revert normal.

## A1/A2 — pruebas
Cinco Vitest: ruta única lazy y guard, ausencia de sesión, grant de plataforma simulado, contexto empresarial denegado y revocación reactiva; sin buttons/forms ni secretos. Tres Playwright Chromium reales con backend de sesión sintético: anónimo denegado, 403 denegado, grant simulado con diseño responsive 390, ausencia de acciones de migración/cutover. No implica certificación JWT/HTTP real ni aislamiento de base A/B.

## A3/A4 — checks requeridos
GitHub-hosted Node24/npm11 npm ci, 5 unit tests, Angular lint, build, 3 Chromium; exact SHA Scope Lock, V5/procedencia. MySQL/EF Core 8.4/SQL migraciones, provider/DSN routing y tests de tenancy de backend NO_APLICA a este diff UI sin runtime; comportamiento de BD sigue NO_MEDIDO y debe validarse antes de una microtarea que lo implemente.

## A5
Independiente por otra de las 5 Tasks Javier, revisión de código, seguridad, pruebas y SHA. Ningún QA/main/PROD habilitado.
