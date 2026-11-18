# C14-01.M03.06 — Auditoría aislada de aceptación funcional (V19)

**Riesgo R2.** Implementación .NET 10 Application, sin nuevo endpoint, worker, DI de producción, migración ni persistencia. Cumple el alcance propio de *auditoría, logs estructurados y errores seguros* exclusivamente alrededor del preflight C14-01 existente; no sustituye la auditoría real ni las autorizaciones del producto.

## A0 — Dependencias, superficies e impacto
`C1401FunctionalAcceptanceAuditService` compone `C1401FunctionalAcceptanceAuthorizationGate` y un `ILogger` inyectado. La entrada sigue siendo el contrato read-only del preflight. La salida nunca concede acceso a tenant/recurso, escritura institucional, QA ni producción. No cambia DTO/API, schemas EF/MySQL, JWT, RBAC, datos de usuario, UI, integración externa, secretos ni despliegue. Rollback: `git revert` normal del commit tras comprobar HEAD de dev.

## A1/A2 — Errores, fronteras y seguridad
Emite un evento 140106 con propiedades nombradas `OutcomeCode` e `ItemsChecked`. No registra actor, tenant, recurso, token, SHA, evidencia ni mensajes de excepción. Código limitado a ASCII alfanumérico en mayúsculas y guiones bajos, máximo 96 caracteres, recuento 0–64. Logger apagado o averiado => `AUDIT_SINK_UNAVAILABLE`, sin informar auditoría válida. Cancelación se propaga; un error de lector produce la denegación segura ya prevista por el gate. La suite xUnit incluye preflight positivo sin concesión, tenant B, revocación, fallo opaco de fuente, fallo de sink, cancelación y lote 65.

## A3 — Integración real pertinente
Se ejecuta la composición concreta Application + ILogger a través de .NET 10 Release y xUnit en GitHub-hosted. **NO_APLICA** para MySQL/EF, JWT HTTP firmado, proveedor real, navegador y producción porque este changeset no los conecta, instala ni modifica. No se certifican superficies que continúan pendientes de otra microtarea.

## A4/A5
Exigir check dirigido `C14-01.M03.06 - .NET 10 directed audit regression`, Scope Lock, V5, build/seguridad aplicables completados SUCCESS en SHA propio o equivalencia demostrada, más PLAN/COLA/DAG/BITACORA readback. Si CI sigue QUEUED/IN_PROGRESS mantener VALIDANDO con owner y SHA; CANCELLED/SKIPPED/FAIL no son PASS. A5 sólo otra de las cinco Tasks Javier. Sin QA/main/PROD.
