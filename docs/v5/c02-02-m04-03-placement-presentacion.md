# SOLQARYN V17 — C02-02.M04.03: configuración visual permitida de placement

Autor: PRIMARY DEV 02 Javier, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. Scope Lock STRICT, rama dev, PLAN fila 346, candado V17 2000000346. Riesgo R2 por superficie privada relacionada con aislamiento de tenants.

## A0: alcance, interfaces, rollback

La vista privada existente `PlacementReviewV5Component` incorpora un `Signal<unknown>` de presentación (marca `solqaryn` o `institucional`, densidad `normal` o `compacta`). La normalización deniega objetos extra, CSS, HTML, variantes no tipadas, arrays y valores ambiguos, revirtiendo a presentación segura. Las opciones cambian solo estilo y un rótulo; nunca sesión, actor, JWT/RBAC, tenant, routing del servidor, datos, DSN, migración, cutover ni proveedor. La ruta existente con `plataformaGuard` conserva autoridad backend. Rollback vía git revert normal.

## A1/A2: positivos, negativos, fronteras y revocación

La prueba `placement-presentacion-v5.spec.ts` verifica variantes reactivas, rechazo de código HTML/CSS, campos adicionales incluso tenant/DSN, nulos/arrays, ausencia de autoridad anónima y falta de operaciones DB. Mantener las pruebas existentes M04.01/M04.02 de 403/503, guard, doble-click, revocación y lectura de solo dos modalidades. Solo CI real autoriza PASS; mocks son componentes de sesión sintética sin demostrar JWT HTTP real.

## A3: integración según alcance

GitHub Actions: npm ci, Vitest real, lint y build Angular 22.2.1, Chromium Playwright sobre ruta privada localhost con API de sesión sintética `e2e/c02-02-m04-01-placement.spec.ts`. Proveedores, MySQL Oracle 8.4, EF, transacciones y migraciones reales NO_APLICA al diff porque no se conectan ni cambian. Sin operación productiva.

## A4/A5: CI y cierre

Una sola publicación fast-forward/CAS con componente, token, Vitest, workflow, documento y CHANGELOG autocontenido. A0–A4 y checks pertinentes `C02-02.M04.03 - Vitest Angular Chromium sin DSN`, frontend, seguridad, Scope Lock y procedencia V5 exigen completed/success del SHA o equivalencia auditada. Si CI externa sigue queued/in_progress mantener VALIDANDO; no anticipar PASS. A5 por otra Task Javier distinta de autora, nunca autoaprobación ni QA/main/PROD.

## Corrección P1 de selección de pruebas (DEV05, 2026-11-18)

La revisión A5 independiente observó que el job original ejecutaba 12 pruebas del componente de revisión, pero omitía las cinco pruebas `placement-presentacion-v5.spec.ts` del nuevo contrato de marca/densidad, entradas hostiles y no autoridad. Se corrige exclusivamente `.github/workflows/v17-c02-02-m04-03-placement.yml` para seleccionar y ejecutar **ambas suites** mediante dos comandos npm separados en GitHub-hosted. La ruta del propio workflow se incluye en el filtro push, de manera que este arreglo también dispare la CI dirigida. Se conserva Chromium, lint, build, aislamiento por SHA y `cancel-in-progress: false`; no se debilitan validadores.

El P1 histórico queda en BITACORA. No se declara A1/A2/A4 ni A5_PASS hasta que CI de GitHub-hosted ejecute realmente las cinco pruebas nuevas y las existentes con resultados completed/success en el SHA correctivo; su revisor A5 debe ser otra Task Javier distinta de autora DEV02 y de la Task DEV05 que hizo esta reparación. No hay JWT firmado, MySQL, migraciones ni proveedor live acreditados; rollback con git revert y sin QA/main/PROD.
