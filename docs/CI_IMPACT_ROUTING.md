# SOLQARYN - CI por impacto en DEV

## Controles que no se retiran

Siguen obligatorios `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa`, `SOLQARYN - Build y pruebas backend` y el check dirigido comercial aplicable. El verificador de certificacion automatica conserva todos los controles requeridos, el check dirigido, la revision independiente y el rechazo de errores ya observados. Solo un conjunto conocido de checks extensos pendientes y causalmente ajenos al diff puede diferirse en la via rapida comercial, bajo prueba fail-closed.

## Enrutamiento causal

- `catalogos-aceptacion.yml`: ejecuta Playwright ante cambios frontend, backend operativo o sus otros disparadores declarados. Excluye exclusivamente cambios dentro de `backend/tests/**` del disparo de esta suite.
- `priority4-quality.yml`: no se activa por `backend/tests/**` como unico impacto; mantiene compilacion, lint y regresiones `Category!=Integration`. La integracion MySQL pertenece al job de migraciones de `dev-ci.yml`.
- `dev-ci.yml`: un preflight `impact` ejecuta el clasificador en Node 24, lo prueba y compara los SHA exactos de base y HEAD; el job MySQL solo se omite cuando el diff contiene exclusivamente frontend, documentacion, `CHANGELOG_AI.md` y/o archivos unitarios comerciales P1dddX*Tests.cs, I8dddX*Tests.cs o P2dddX*Tests.cs solo declarativos de metadatos/reflexión o tests P2 de contratos documentales de solo lectura, con evidencia positiva y sin imports de runtime/EF/DI/HTTP/servicios, en la raiz de Solqaryn.Tests o subcarpetas ordinarias como API/Controllers, sin `Category=Integration` ni uso directo de base de datos. Los prefijos de pruebas distintos de P1/I8/P2, las pruebas P2 no puramente declarativas y las rutas Integration, Database, Migrations, Fixtures, Infrastructure y E2E continúan obligando MySQL, igual que los archivos desconocidos o los cambios de runtime.
- `scripts/security/ci-impact-routing.mjs`: cambios desconocidos, eliminaciones ambiguas, rutas de gobernanza, codigo productivo, migraciones, pruebas de integracion o referencias a DbContext/MySQL obligan a ejecutar MySQL; fallos del diff o del clasificador impiden la certificacion. Dispacho manual sin base ejecuta la suite completa.
- `solqaryn-commercial-directed.yml`: solo se ejecuta para ramas comerciales deterministas `work/solqaryn-p*`; los PR de gobierno no disparan una prueba comercial ficticia. Scope Lock sigue rechazando cualquier PR comercial en rama incorrecta.
- `project-scope-lock.yml`: ejecuta los casos positivos y negativos del clasificador; permanece con revision independiente requerida para cualquier cambio de workflows o rutas de `scripts/security/**`.

## Fast lane causal de certificacion (noviembre 2026)

- **Obligatorios sin excepciones**: Scope Lock exact-HEAD, `SOLQARYN - Seguridad multiempresa`, `SOLQARYN - Build y pruebas backend`, y `SOLQARYN - Directed commercial scope` para PR comerciales. Revisiones independientes siguen siendo requeridas segun autor y cambio de gobierno.
- El verificador puede finalizar sin esperar exclusivamente checks opcionales extensos de la lista `BROAD_OPTIONAL` que aun esten `queued`/`in_progress` y sean de GitHub Actions oficial, cuando el diff sea solamente `CHANGELOG_AI.md` + Markdown no protegido bajo `docs/**`, o tests unitarios comerciales PddddX exactos con clasificacion MySQL confiable PASS y job MySQL explicitamente SKIPPED.
- Los cambios de workflows, scripts, backend/frontend productivo, migraciones, gobernanza, secretos, infraestructura, borrados/renombrados y archivos de clasificacion desconocida no entran en fast lane. Documentos de contratos de produccion, rendimiento y modernizacion tampoco.
- **Nunca se ignora un check que ya termino con failure o cancelled**, aunque fuese opcional. Los checks pendientes desconocidos tampoco se omiten. Solo se difiere la espera; las suites siguen ejecutandose en GitHub y mantienen su resultado visible.
- Logs `AUTO_CERT_DEFERRED_UNRELATED_CHECKS` auditan los checks diferidos. `shouldWaitForActiveChecks` espera solo jobs realmente relevantes del SHA evaluado; un job ajeno no puede enmascarar un check dirigido ausente.
- El fast lane no autoriza fusiones con Core incompleta ni cierres `LISTO` prematuros. Las Tasks pausadas siguen pausadas; no cambia la autorizacion individual ni se activa ninguna Task. PR de instalacion exige review independiente, CI, merge protegido y readback.
- Reversion: PR protegido que revierta el clasificador en el verificador y esta politica, sin modificar rulesets ni entornos.


## Reintento por evento de CI completada (2026-11-07)

La falla `OTHER_CHECKS_PENDING` de Scope Lock despues de su espera acotada no puede convertirse en PASS por tiempo ni por un `Impacto CI` verde: ese check valida el clasificador, no que MySQL sea omitible. Se exige que el job MySQL en el HEAD evaluado alcance `completed/success` o `completed/skipped` antes de diferir otras suites de tests unitarios; un MySQL `queued` o `in_progress` sigue siendo causalmente pendiente, especialmente en P2/I8.

`.github/workflows/retry-scope-after-ci-completion.yml` escucha la finalizacion satisfactoria de los workflows DEV, Priority 3, Core y Directed y ejecuta SOLO `scripts/security/auto-retry-scope-after-ci.mjs` desde el `dev` protegido con token limitado. No ejecuta codigo del PR ni cambia ramas, revisiones, estado de negocio, rulesets, credenciales o despliegues. Revalida repositorio, PR OPEN, base dev actual, HEAD exacto, origen GitHub Actions, totalidad de check runs, required + Directed en SUCCESS, y que el fallo anterior de Scope Lock fue UNICAMENTE por comprobaciones pendientes. Reintenta exclusivamente su job fallido, en un maximo de dos reintentos tras el primer intento, y confirma de nuevo HEAD/checks antes del POST. Nunca reintentar un fallo causal, review faltante, CI aun activa/fallida, SHA stale o PR no fusionable. Si no hay evento o permisos efectivos, el bloqueo sigue visible y la Task debe registrar `CI_PENDING_ASYNC` o la causa real sin fabricar `LISTO`.

Esta reparacion exige review formal independiente del ultimo HEAD, Scope Lock y CI protegida porque modifica workflow y verificador. No autoriza saltarse FIFO comercial: el PR normativo se integrara solo en su turno segun `docs/AUTOMATION_V13.md`.
## Aseguramiento

Para cambios solo unitarios PddddX se exige de todas formas Core, seguridad multiempresa, prueba dirigida y Scope Lock exact-SHA. Los cambios de backend fuente, migraciones e integracion mantienen cobertura MySQL y, cuando aplican, Playwright/Quality. Los workflows integrales conservan ejecucion manual mediante `workflow_dispatch`.

QA de aceptacion de este cambio: autoprueba `CI_IMPACT_ROUTING_SELF_TEST=PASS`, `CI_IMPACT_MYSQL_INTEGRATION=REQUIRED` ante workflows y fuente backend, validaciones de CI del PR protegido y aprobacion independiente del SHA final. Probar en un PR futuro con solo prueba comercial que las suites ajenas no bloqueen.

El cambio no modifica ERP, autentificacion, RBAC, PII, Aiven, grants, Render, Vercel, QA/main/PROD ni rulesets. Reversion exclusiva mediante otro PR protegido y lectura posterior.

## Estabilizacion de Scope Lock para pruebas I8 (2026-11-07)

- I8dddX*Tests.cs aislados entran en la clasificacion segura de MySQL solamente cuando no hay referencias directas a bases de datos ni rasgos Category=Integration. La clasificacion conserva fail-closed ante archivos faltantes, rutas de integracion, migraciones, fixtures, cambios de runtime o cambios mixtos.
- El preflight clasificador incorpora casos positivos I8-004.C/I8-005.C y negativos DbContext, Category=Integration, rutas Integration, runtime agregado y fuente ausente.
- Scope Lock mantiene espera acotada de cuatro minutos y reintento nativo selectivo del job fallido tras finalizar la CI exact-HEAD, conforme al workflow protegido de dev. Nunca declara SUCCESS si un check requerido sigue pendiente, fue cancelado o falló.
- No se elimina seguridad, backend, comprobacion comercial dirigida, revision independiente, CI posmerge ni branch protection. Los PR existentes con SHA anterior requieren conciliacion y nuevo CI; no se altera su historial.
- El cambio de gobernanza exige revision independiente formal y merge protegido en dev. Rollback por revert protegido de este changeset, sin cambios en QA/main/PROD ni rulesets.

## P2 de metadatos/reflexión sin MySQL ajena (2026-11-09)

- Para `P2nnnX*Tests.cs`, solo omitir la suite extensa cuando el diff exacto se limita a pruebas de reflexión declarativa y Markdown: `using System.Reflection;` y consultas `GetCustomAttribute(s)`/`GetMethod(s)`/`GetProperty(s)`, sin DB/EF, Moq/DI, servicios, HTTP, `WebApplicationFactory`, rutas de integración ni cambios de runtime. Los demás P2 y los archivos dudosos continúan ejecutando MySQL.
- El clasificador continúa fail-closed ante paths desconocidos, archivos faltantes, rutas `Integration/Database/Migrations/Fixtures/Infrastructure/E2E`, fuente backend, workflows y scripts. `docs/**` y `CHANGELOG_AI.md` no activan MySQL por sí solos, pero la gobernanza conserva su revisión formal y Scope Lock.
- No se eliminan pruebas ni se modifica el workflow `dev-ci.yml`; MySQL sigue ejecutándose para impacto real y en `workflow_dispatch`. Scope Lock, seguridad, backend, Directed, revisión independiente, merge protegido y CI posmerge mantienen sus gates.
- Por modificar `scripts/security/**`, esta corrección requiere revisión formal independiente exact-HEAD y merge respetando el FIFO. No se aplica retroactivamente hasta entrar en `dev`. Rollback por revert protegido.

## P2 documentales de solo lectura (2026-11-09)

- Los tests P2 que leen contratos versionados con `File.ReadAllText` y validan aserciones xUnit, sin importar runtime de SOLQARYN, EF, DI, Moq, HTTP o servicios, también entran en la vía de CI sin migraciones MySQL. Esto cubre P2-019.D: `P2019DReadinessGateNoPromotionTests.cs`; no confunde `PRODUCTION_READY = FALSE` con despliegue permitido.
- La clasificación exige imports estrictamente de System básico/regex/Linq y Xunit, evidencia de lectura/aserción y ausencia de escrituras, procesos, constructores arbitrarios, Category=Integration y riesgos de DB. Archivos ausentes, casos ambiguos, cambios de backend operativo, scripts de seguridad, workflows, migraciones o cualquier path desconocido siguen en MySQL obligatoria. 15 autopruebas negativas/positivas nuevas comprueban la barrera.
- No se retira ni silencia MySQL: sigue activa para cambios que realmente la requieren, para este PR que modifica el propio script de seguridad y para `workflow_dispatch`. Scope Lock, Seguridad, Backend, Directed, revisión independiente, merge protegido y CI posmerge conservan su autoridad. La mejora no cambia retrospectivamente el HEAD/CI de #3968.
- Cambio de gobernanza `scripts/security/**`, sujeto a aprobación formal independiente exact-HEAD y a su turno de integración FIFO. Rollback mediante revert protegido sin reescribir historia.

## P2 de filtros con Moq estricto sin DB real (2026-11-09)

- Una prueba P2 aislada que usa mocks estrictos (`Moq`, `MockBehavior.Strict`), `DefaultHttpContext`, filtro de autorización y un contenedor DI con objetos de mock, sin fuente backend productiva modificada, puede omitir la suite completa MySQL. Las pruebas de Backend siguen ejecutando ese caso. Ejemplo causal: P2-001.F y sus cinco GET comerciales; no equivale a seguridad HTTP/JWT o DB real.
- Fail-closed: cualquier EF, DbContext, `UseInMemoryDatabase`, SQL, HTTP real/WebApplicationFactory, migración, infraestructura productiva, construcción de servicios reales, DI no simulada, Category=Integration o archivo no clasificable mantiene MySQL obligatorio. No se debilitan los required gates de Scope Lock, Seguridad, Backend, Directed, reviews ni el merge protegido. La clasificación no cambia retrospectivamente los HEAD que ya iniciaron CI.
- 13 autopruebas adicionales verifican mocks estrictos y casos negativos de integración/DB. Este cambio requiere aprobación formal independiente por modificar `scripts/security/**`; sin excepción de FIFO ni merge anticipado.
