# SOLQARYN V5 — C10-04.M03.02 servicio Application acotado de cache, trabajos y exportaciones

**Fuente:** Plan V5 Final Doc Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO fila 1563, DAG_TECNICO fila 1524. **Autor:** PRIMARY DEV 02 Task `6af16bb9492c8191b8e8c7d606f11507`, `solqaryn/Solqaryn@dev`, Scope Lock STRICT, 2026-11-15.

## Alcance implementado

Se implementa un servicio Application real `C1004CacheJobsApplicationService` que ejecuta un preflight de solicitud de trabajos de caché, exportaciones, backups y jobs antes de entregarlos a un adaptador real todavía inexistente. Usa directamente `C1004CacheJobsDomainRules` existentes (C10-04.M03.01), sin modificar Domain. Introduce `IC1004VerifiedWorkEvidenceSource` como contrato explícito para obtener un contexto autenticado, tenant y pertenencia vigente, permiso concreto, versionado y política de reintentos desde servidor. El `Request` del cliente nunca aporta autoridad por sí mismo.

**Sin efectos externos:** no crea registros, emite mensajes, ejecuta worker, cache invalidation, backup, restore, exportación, proveedor, MySQL o URL. Todos los flags de autorización del `Result` están constantes en false. Incluso una forma válida retorna `AUTHENTICATED_ADAPTER_AND_EXCLUSIVE_CAS_REQUIRED`, nunca una aprobación de efectos. No se registra este servicio en DI, no se modifica API ni config JWT/RBAC/tenant.

## Contrato y denegaciones

Preflight de Request: ID/operación allowlisted sintácticos, clave idempotente, hash SHA256 y versión esperada. El repositorio/adapter de confianza debe resolver `ContextoTenantActual`, actor, owner persistido, membresía y permiso vigente, revocación, política de operaciones y versión, presupuestos/deadline UTC. La empresa en Request sólo se compara como hint; si difiere del owner servidor se rechaza; se aplica `C1004CacheJobsDomainRules.ValidarSolicitudTrabajo` con identidad del adaptador, no con usuario forjado.

Denegaciones nombradas: `REQUEST_INVALID`; `VERIFIED_BACKEND_CONTEXT_REQUIRED`; `ACTIVE_MEMBERSHIP_AND_OPERATION_PERMISSION_REQUIRED`; `OPERATION_NOT_ALLOWLISTED`; `CLIENT_TENANT_OVERRIDE_DENIED`; `RESOURCE_VERSION_MISMATCH`; `WORK_PARAMETERS_INVALID`; `WORK_POLICY_OR_TENANT_DENIED`. No se incluyen detalles sensibles ni se escribe en estado institucional. No convertir un preflight positivo en autorización de un endpoint.

## Evidencia A0–A5 y rollback

A0: cotejo de arquitectura Domain <- Application, plan, gates y Scope Lock exact SHA. A1: pruebas xUnit reales positivas/negativas del servicio, formato, versiones, cancelación, límite, tenant y permiso; escribir pruebas no demuestra su ejecución. A2: aislamiento A/B, contexto servidor, rol y membresía revocables, service actor grant y política fail-closed; no afirma auditoría JWT por el adaptador futuro. A3: integración de Application con Domain existente vía clases compiladas, sin DB/HTTP/worker; tests de infraestructura `NO_APLICA` a este diff y no son PASS. A4: requiere backend build/tests y seguridad multiempresa y authorization gate pertinentes SUCCESS causal por GitHub Actions exact SHA, además Scope Lock/V5 SUCCESS, diff y readbacks. Mientras CI necesaria `queued/in_progress`, mantener VALIDANDO sin LISTO ficticio. A5 independiente en otra Task Javier.

Rollback: `git revert` normal del changeset tras comparación de HEAD; no reset, force-push ni BITACORA histórica editada. No tocar main/QA/PROD, proveedores, datos ni agendas.
