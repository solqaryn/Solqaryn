# SOLQARYN V17/V15 — C02-02.M03.04 reversible isolated placement rehearsal

Microtarea C02-02.M03.04, PRIMARY DEV05 JAVIER, TASK_ID 6adc2e55f47c81919f6374e561687076. Scope dev, STRICT. PLAN_MAESTRO fila 341, V17 lock 2000000341. R2 por tenancy, CAS, rollback, idempotencia y shared/dedicated placement. STANDARD autorizado: "Implementar persistencia, migracion reversible o adaptador aislado".

## A0 — Alcance ejecutable e impacto

Entrega nueva clase Infrastructure C0202PlacementSandboxRehearsalJournal, dependiente del adaptador aislado M03.03, Gate Application M03.02 y Domain M03.01. Una simulacion de placement mantiene UN intento sintetico en memoria, con revision CAS monotona e idempotencia por tenant/actor/SHA/revision/alias. Stage y rollback son transiciones reales en el objeto de prueba, aisladas de servicios y de una base de datos. No implementa servicio HTTP, MySQL/EF, SQL, migraciones, backup, restauracion, router de conexiones, worker, DI ni cambio compartido/dedicado real. Nunca toma DSN de navegador.

El resultado nominal de Domain/Application AUTHENTICATED_STORAGE_ADAPTER_REQUIRED es una frontera no autorizante: SOLO permite una proyeccion inerte, no persistencia. Ante fuentes sinteticas incompletas, JWT/membresia/RBAC revocados, tenant B, actor o SHA adulterados, fallos de recuperacion o alias no confiable, el journal deniega. Una revision original solo se acepta una vez y rollback incrementa la revision para prevenir ABA.

## A1/A2 — pruebas dirigidas genuinas

Doce hechos xUnit cubren stage nominal sin grant, tenant B, actor ajeno, revision/SHA obsoletos, DSN/doble escritura/cutover/esquema destructivo, JWT/RBAC/revocacion y rollback, alias/idempotencia, replay divergente, CAS por 24 intentos concurrentes, clave/actor para rollback, revision monotona, cancelacion y constructor fail-closed. Requiere ejecucion .NET 10 Backend Release y checks seguridad multiempresa y Authorization exactos, no afirmar resultados antes del CI.

## A3 — integracion NO_APLICA a motores reales

La clase solo contiene estructuras en memoria, no toca AppDbContext, MySql.EntityFrameworkCore, migraciones ni HTTP/browser. Una suite MySQL 8.4 efimera sera obligatoria cuando un cambio futuro de verdad cree/modifique proveedor EF/DB. No declarar pruebas de migracion, almacenamiento duradero, reintentos distribuidos, JWT HTTP o produccion con mocks de Domain. A5 independiente de otra Task Javier.

## A4 y reversa

Un commit unico fast-forward/CAS en dev con nueva clase, sus xUnit, documento y seccion autocontenida en CHANGELOG_AI.md. Verificar Scope Lock, V5 procedencia, Backend Release, seguridad, autorizacion y readback PLAN/COLA/BITACORA sobre SHA exacto; si CI esta en progreso, estado VALIDANDO con owner DEV05, run/SHA/check y bloqueo de sucesoras. Rollback git revert normal de ese commit, nunca force-push, QA/main/PROD, datos productivos ni secretos.