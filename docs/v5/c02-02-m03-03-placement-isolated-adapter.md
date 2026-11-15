# SOLQARYN V17 — C02-02.M03.03: comprobación de ubicación shared/dedicated aislada

**Autor:** PRIMARY DEV 04 Javier (TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09), solo solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK STRICT. **Riesgo:** V15 R2 (tenant, RBAC, datos, migración/concurrencia). **Unidad:** C02-02.M03.03, PLAN_MAESTRO fila 340, candado V17 SQV17_C02_02_M03_03 (namedRangeId 2000000340).

## A0 — Alcance real

Se entrega código Infrastructure .NET 10: C0202PlacementIsolatedSnapshotAdapter, un adaptador de snapshot **sintético e inerte** que enlaza con la lógica Domain V5SharedDedicatedPlacementDomainRules y el gate Application C0202PlacementApplicationGate implementados en M03.01/.02. La fuente NO recupera JWT en HTTP, no selecciona DSN, no abre una conexión, no crea asignaciones shared/dedicated, no ejecuta SQL ni migraciones, backups, restore, dual-write, cutover o cambios productivos. Sólo verifica límites y rechazos, sin autoridad. Se preserva arquitectura Domain → Application → Infrastructure, sin volver a escribir predecesoras.

## A1 — Pruebas genuinas de código

13 hechos xUnit de preflight nominal no autorizante, constructor y solicitud inválidos, tenant A/B, actor, SHA, revisión CAS, evidencia adulterada, propietario, DSN del cliente, live cutover, cross-tenant fallback, esquema destructivo, membresía revocada, RBAC, binding no catalogado, rollback y sandbox restore faltantes, y cancelación. Deben aprobar xUnit compilado realmente en GitHub Actions sobre SHA causal; no se declara PASS con meras especificaciones.

## A2 — Seguridad

La captura comprueba tenant, actor, revisión y SHA de origen por comparación ordinal, además de propiedad y revisiones del registro. El gate de Application revalida de forma sintética JWT/membresía/RBAC declarado, configuración confiable, catálogo de binding, posibilidad de rollback y CAS. Los booleanos AuthorizesConnection, AuthorizesMigration, AuthorizesCutover, AuthorizesDualWrite y CanReadOrWriteTenantDatabase son siempre false; ningún resultado preflight autoriza un cambio real. No se confía en headers, browser, DSN proporcionado por el cliente ni fallback a otro tenant.

## A3 — Integración/NO_APLICA

Compilar y ejecutar pruebas .NET10 Release con enlace Infrastructure/Application/Domain. Como el diff NO toca EF, proveedor Oracle/MySQL 8.4, conexión HTTP, migraciones, datos o UI, pruebas live MySQL/Playwright/API son NO_APLICA a este artefacto aislado y quedan obligatorias cuando se implemente un adaptador autenticado real. No se hacen afirmaciones sobre migraciones, aislamiento de una base de datos ni autenticación JWT real.

## A4 — Evidencia y rollback

Un único commit fast-forward/CAS en dev y un V17 lock, CHANGELOG_AI.md autocontenido; validar Scope Lock, procedencia, pruebas/backend Release y controles security/authorization completed/success sobre SHA exacto, BITACORA idempotente, PLAN/COLA/DAG y readbacks. Si CI queued/in_progress, dejar VALIDANDO y owner, no PASS. Rollback con git revert normal, sin force-push, nunca QA/main/PROD.

## A5 — Revisión independiente

Necesaria Task Javier distinta del autor para revisar diff, requisitos, 13 xUnit y CI por SHA; puede fallar y exige reparación. Ninguna activación de almacenamiento shared/dedicated ni de sistemas de producción se autoriza por LISTO_DEV.
