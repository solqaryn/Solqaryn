# SOLQARYN V17 / V15 — C10-04.M03.05: worker acotado de revisión de jobs (sin despachos productivos)

**Microtarea:** C10-04.M03.05, PLAN fila 1566. **Autor:** PRIMARY DEV 05 JAVIER (6adc2e55f47c81919f6374e561687076). **Repositorio:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. Riesgo R2 por concurrencia, idempotencia, jobs, tenant y recursos privados. Gate predecesor M02 6/6 + A5 y M03.01–04+A5 validado en DAG, COLA elegible, V17 candado SQV17_C10_04_M03_05 / 2000001566.

## Entrega ejecutable, límites y revisión arquitectónica

Se implementa `backend/src/Infrastructure/AutomationV5/C1004CacheJobsIsolatedReviewWorker.cs`, worker **real en memoria, síncrono por solicitud en cuanto a ciclo de vida, de consumo asíncrono de Channel acotado (máximo 32 entradas)**. Integra el adaptador Infrastructure `C1004CacheJobsIsolatedSnapshotAdapter` de M03.04, el gate Application `C1004CacheJobsAuthorizationGate` de M03.03 y reglas Domain anteriores. Cada intento revalida work ID, actor, tenant, versión, política, RBAC de snapshot, servicio, deadline, retry e idempotencia antes de devolver un veredicto.

La clave de replay se particiona por `(TenantId, WorkId, IdempotencyKey)` con comparación ordinal de digest. Misma clave/payload = no-op; misma clave/payload distinto = conflicto; una denegación previa **no** consume idempotencia del intento admisible. A/B, recursos stale, tokens del cliente y restore sin operación allowlist resultan denegados. Batches nulos/vacíos o de más de 32 se rechazan; cancelación se propaga; un fallo de revisión no detiene la revisión de otros intentos independientes.

**Límite fundamental:** esta es una cola efímera **exclusivamente de revisión**, nunca persistente ni servicio desplegado. No registra HostedService, no abre conexión a MySQL, R2, Cloudflare o storage, no obtiene lease, no restaura backup, no exporta documentos ni ejecuta una operación de negocio. Todas las propiedades de efectos de Review son false. La idempotencia en memoria NO constituye CAS distribuido ni reemplaza el contrato de un worker real con estado persistente. Se requerirá implementación de provider y pruebas auténticas de recuperación en microtarea futura con autorización; NO proclamar restauración real ni aislamiento en HTTP productivo.

## Matriz de control proporcional

- **A0:** solo clase worker Infrastructure, 12 xUnit, este documento y sección CHANGELOG autocontenida; sin editar Application/Domain existentes, EF, API, frontend, contratos de producción o secretos. Rollback git revert normal.
- **A1:** pruebas xUnit incluyen positivo no ejecutante, duplicado, conflicto de digest, tenant B, actor B, versión obsoleta, membresía revocada, grant de servicio ausente, operación backup no allowlisted, rechazo por retry agotado/expiración, batch 0/33, intento nulo, SHA payload inválido y cancelación.
- **A2:** validación por tenant/actor/version en adapter existente, ausencia de grants y acceso a R2, allowlist, sin cross-tenant; *no* autentica JWT ni valida permisos reales de proveedor. Backend Security/Authorization GitHub Actions deben completarse SUCCESS por SHA.
- **A3:** composición REAL de Domain–Application–Infrastructure en la cola in-memory y xUnit .NET. MySQL 8.4 Category=Integration, backups restaurados, browser y proveedor Cloudflare **NO_APLICA** al diff sin provider, EF ni cambios de red/UI. Esa limitación NO permite cerrar una certificación de restore real.
- **A4:** fast-forward CAS dev exact HEAD; checks Scope Lock, V5 provenance, Backend Release/xUnit, Seguridad multiempresa, Authorization y readback institucional deben completar SUCCESS por SHA o equivalencia auditada. Si siguen QUEUED/IN_PROGRESS dejar VALIDANDO y propietario, sin LISTO ficticio.
- **A5:** solo por una Task distinta de las seis Javier y con revisión independiente real.

No se modifica QA/main/PROD, datos MySQL, secrets, DNS, infraestructura ni reglas de los proveedores. La reanudación tras CI se realiza con REVIEW_FIRST y una nueva adquisición V17 para cerrar, sin falsificar PASS.
