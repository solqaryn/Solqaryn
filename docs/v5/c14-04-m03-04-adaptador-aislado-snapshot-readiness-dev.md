# SOLQARYN V17 — C14-04.M03.04: adaptador aislado de snapshots de readiness DEV

**ID:** C14-04.M03.04 · **Task autora:** PRIMARY DEV 01 Javier, \`6adbb9fff1548191a1bd5b30d9e9984a\` · **Riesgo V15:** R2 · **Ámbito:** exclusivamente \`solqaryn/Solqaryn@dev\` bajo \`PROJECT_SCOPE_LOCK=STRICT\`.

## A0 — Alcance real e impacto

Se implementa un **adaptador aislado y de solo lectura**, no una migración ni restauración: \`V5ReleaseReadinessIsolatedSnapshotAdapter\` y su interfaz \`IV5ReleaseReadinessSnapshotReader\` consumen únicamente metadatos de un snapshot suministrado por una implementación backend confiable futura. Componen el \`V5ReleaseReadinessAuthorizationGate\` de M03.03 y el validador Domain M03.01 a través del gate. Se añade xUnit y este contrato, más una sección CHANGELOG autocontenida. No se registran implementaciones DI, endpoints, SQL, EF Core, MySQL, proveedores, cuentas, secretos, backups reales, despliegues, DNS, migraciones ni datos productivos.

El Request permite **sólo** releaseId y SHA exactos; el tenant de lectura lo deriva \`IV5ReadinessAuthenticatedCallerSource\` exclusivamente del principal autenticado, nunca de un parámetro cliente. El lector recibe \`ReadForTenantAsync(verifiedTenantScope, releaseId)\` y debe aplicar ese particionado en un futuro adaptador persistente. La respuesta no expone el snapshot ni datos de tenant; sólo emite código de revisión, nunca autorización.

## A1 — Pruebas ejecutables positivas, negativas y de frontera

\`V5ReleaseReadinessIsolatedSnapshotAdapterTests\` comprueba: metadatos nominales verificables pero no autorizantes, denegación de QA/PROD, falta de autenticación o membresía/RBAC, tenant extranjero o reviewer fuera de allowlist **antes de la lectura**, tenant o SHA falsos retornados por el reader, snapshot faltante, evidencia mutable, auditoría/ownership/rollback no verificados, digest SHA256 inválido, versión cero, rollback con caracteres de control, input con traversal, SHA no canónico, CI cancelled/skipped/failure, autoaprobación y cancelación propagada. Cada test tiene asserts capaces de fallar. **A1 sólo PASS** cuando GitHub Actions .NET 10 ejecuta realmente los xUnit del SHA y 0 FAIL.

## A2 — Seguridad y tenancy

Nunca se puede promocionar un A5, conceder acceso, escribir en la institución, restaurar una base, desplegar ni certificar disponibilidad a partir de \`READ_ONLY_SNAPSHOT_EXTERNAL_REVIEW_REQUIRED\`. El gate autenticado revalida la política DEV, SHA, reviewer distinto del autor y checks obligatorios; luego se lee únicamente bajo el tenant autenticado y se compara \`Snapshot.TenantScope\` con ese scope por \`StringComparison.Ordinal\`. No se acepta una fuente no autenticada, pertenencia revocada, falta de grant, SHA mismatch, etiqueta QA/PROD, datos sin cadena auditada ni snapshot mutable. La autenticidad efectiva del futuro reader seguirá exigiendo integración y pruebas separadas.

## A3 — Integración, límites y rollback

Se prueba la composición real C# Application/Domain del contrato con mocks deterministas, **no** SQL ni MySQL reales: no hay código EF, DDL, migración, backup, restauración, endpoint ni adaptador persistente registrado. Por ello MySQL 8.4 Category=Integration, HTTP, Angular y Playwright son **NO_APLICA a este diff**; nunca se reportan PASS. La restauración real y el snapshot de proveedor quedan **NO_MEDIDO / NO_EJECUTADO**, requieren autoridad posterior. Rollback del commit de dev mediante \`git revert\` normal, preservando BITACORA, owners y avance ajeno; no fuerza push.

## A4 y A5 — Prueba causal y liberación

Publicar con fast-forward/CAS sobre HEAD dev exacto, secciones CHANGELOG autocontenidas, Scope Lock y procedencia V5; exigir xUnit/Release, seguridad multiempresa y Authorization \`completed/success\` por SHA causal, no considerar CANCELLED/SKIPPED/IN_PROGRESS como PASS. Registrar CLAIM/VALIDANDO/LISTO en PLAN y BITACORA A:Z por ID, con readback COLA/DAG y candado V17 fila 2141. Si CI sigue ejecutándose, dejar VALIDANDO con SHA/run/check, sin liberar sucesoras prematuramente. A5 sólo por otra Task Javier que revise diff, pruebas y límites; no autoaprobar. Sin QA, main ni producción.
