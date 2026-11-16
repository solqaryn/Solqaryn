# SOLQARYN V17 — C08-04.M03.04: snapshot aislado y no autorizante de módulos sectoriales

Microtarea C08-04.M03.04, PLAN fila 1277, autor PRIMARY DEV 03 JAVIER TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. Solo solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. Riesgo R2 por tenant, RBAC y activación sectorial.

## A0 — Implementación
Se implementa C0804IsolatedTenantModuleSnapshotAdapter en Infrastructure utilizando el puerto IC0804TenantModuleSource de Application (M03.03). Conserva Domain <- Application <- Infrastructure. El snapshot es sintético, NO una consulta persistente autorizada. Sólo devuelve datos capturados si tenant, módulo canónico, revisión y SHA coinciden exactamente; una lectura cross-tenant, una clave con mayúsculas distintas o una versión obsoleta retornan null. Las banderas PersistedTenantReadVerified y AuditFenceAvailable se fuerzan a false incluso si la captura dice true; el fence existente M03.03 no debe conferir autoridad a un snapshot de pruebas.

No hay cambios EF/MySQL, JWT real, API, DI, transacciones, workers, webhooks, migraciones ni activación productiva. Se preserva la separación de autoridad server-side y se impide utilizar datos sintéticos como grants.

## A1/A2 — Tests
Nueve xUnit: positivo no autorizante, denegación A/B, clave ordinal exacta, revisión, SHA, flags falsificados, entradas constructor inválidas, cancelación y prueba negativa de CAS. Las pruebas sólo se certifican si GitHub Actions Release y checks pertinentes son completed/success del SHA causal. R2 exige seguridad multiempresa y autorización.

## A3/A4/A5
A3 de este diff: Infrastructure→Application compilado/probado en .NET 10. MySQL Category=Integration, Angular, HTTP, proveedor live y Chromium NO_APLICA sin cambio a esas superficies; no se declara cobertura end-to-end. A4: publicación dev fast-forward CAS, Scope Lock, procedencia V5, GitHub checks pertinentes, Sheet/BITACORA readback. Si CI está queued/in_progress, conservar VALIDANDO y owner original. A5 por revisor Javier distinto de DEV03; no autopase.

Rollback con git revert convencional después de reconciliar HEAD, sin force-push. Prohibido QA/main/PROD, secretos, datos reales y otras Tasks.
