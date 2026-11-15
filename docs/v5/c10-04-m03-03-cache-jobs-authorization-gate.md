# SOLQARYN V5 — C10-04.M03.03: gate de seguridad de cache, jobs y exportaciones

**Alcance:** una clase Application .NET 10, doce pruebas xUnit, este documento y CHANGELOG_AI.md. Autor PRIMARY DEV 04 (TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09), `solqaryn/Solqaryn@dev`, Scope Lock STRICT.

## A0 — Dependencias e impacto

C10-04.M02.01–06 y C10-04.M03.01–02 estaban LISTO con A5 independiente; DAG_TECNICO F=VALIDADO/G=SI, COLA G/O=SI en el claim institucional de la fila 1564. Se reutiliza la interfaz `IC1004VerifiedWorkEvidenceSource` y las reglas de Domain `C1004CacheJobsDomainRules`. No se modifican contratos previos, DI, HTTP, entidades, persistencia, migraciones, workers ni proveedores.

## A1/A2 — Semántica y aislamiento

`C1004CacheJobsAuthorizationGate` exige **evidencia obtenida desde backend**, no autoridad de un valor de cliente. Rechaza identidad distinta del contexto autenticado, owner tenant diferente del contexto, membresía o permiso revocado, operación no allowlisted, service account sin grant, versionado obsoleto, clave/hash, deadline y límite de intentos inválidos. Error/ausencia de la fuente se deniega sin exponer secretos. La sucursal y empresa ajenas requieren revalidación real de la fuente server-side. El resultado positivo significa sólo *precondiciones comprobadas*, siempre con `CanEnqueue/CanWrite/CanAcquireLease/CanGrantProviderAccess/CanReadPrivateArtifact/CertifiesRuntimeSecurity=false`.

Doce pruebas xUnit positivas y negativas nuevas ejercitan el **código Application/Domain real con fuente simulada de evidencia**, sin certificar autenticación JWT, middleware HTTP, consulta EF/MySQL, RBAC persistido, CAS real, emisor de trabajos, backup/restore ni Cloudflare R2. Este trabajo no concede permisos ni ejecuta tareas.

## A3/A4 — Evidencia exigida

Compilar y ejecutar tests .NET 10 Release, controles de seguridad multiempresa y autorización, Scope Lock exacto y procedencia V5 sobre SHA causal en GitHub Actions. Las pruebas MySQL Category=Integration, navegador y proveedores reales son `NO_APLICA` **a este diff** (no se cambian DB/API/UI); no se cuentan como PASS. Si CI requerida queda queued/in_progress/failed/cancelled, mantener VALIDANDO y propietario, reparar fallos reales por logs sin desactivar validadores. Documentar blob, HEAD y evento BITACORA para cada transición. Un build aislado no certifica tenant end-to-end.

## A5 y rollback

A5 requiere Task Javier diferente del autor DEV04. Rollback normal `git revert` del commit con revisión de HEAD y seguridad intacta. Sin QA/main/PROD, secretos ni modificación de infraestructura. Fuente: plan Drive V5 FINAL y Sheet PLAN_MAESTRO/COLA/DAG, nunca backlog histórico.
