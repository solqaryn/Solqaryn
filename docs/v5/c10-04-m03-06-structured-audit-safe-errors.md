# SOLQARYN V17 — C10-04.M03.06: auditoría estructurada segura de caché/jobs/backups

**Riesgo R2**. Autor DEV02 JAVIER. Sólo dev, PROJECT_SCOPE_LOCK STRICT. Fuente: STANDARD V5/V17, C10-04 M02, M03.01–05, PLAN_MAESTRO 1567.

## A0 — implementación/impacto

Nuevo servicio real Infrastructure .NET 10 `C1004SafeStructuredAuditService`: usa `ILogger<T>` para emitir eventos con template estructurado y datos minimizados. Obtiene evidencia ACTUAL de identidad/tenant/grant/rol desde el puerto backend `IC1004CurrentAuditAuthorityReader`. Rechaza tenant B, rol/membresía revocada, actor no autenticado, fuente y SHA inválidos, intención manipulada del cliente, payload sensible y operaciones no enumeradas. Sólo emite un código operacional fijo y correlación validada por backend, sin contraseñas, PII, mensajes de excepción, SQL ni token. Captura error del lector/sink y no afirma éxito. Operaciones: CacheRead, JobReview, BackupSnapshotReview, ExportAccessReview.

**Frontera explícita:** registra una evaluación estructurada; NO conecta a cola, worker productivo, persistencia, proveedor de respaldo, auditoría durable real o sink remoto. Resultado `EXTERNAL_DURABLE_AUDIT_ADAPTER_REQUIRED` jamás equivale a certificación de logs durables. Admite inyección de logger y lector autenticado en una integración posterior; no se modifica DI/API, EF/MySQL ni producción. No altera estado tenant. Rollback git revert normal, sin force.

## A1/A2 — pruebas y aislamiento

Nueve métodos xUnit positivos/negativos/frontera prueban cuatro eventos tipados, delimitación tenant A/B, rol y grant revocados, spoof/correlación insegura, errores de backend con log sin PII, falla del sink, cancelación e imposibilidad de mutar proveedor o autorizar recursos. Exigir GitHub Actions .NET 10 Backend Release, seguridad multiempresa y autorización, Scope Lock/V5 exact SHA. La autenticación real del puerto es responsabilidad del futuro adaptador, no de tests con mocks.

## A3/A4 — no aplicables y handoff

Infraestructura .NET 10 + interfaz ILogger reales; **MySQL 8.4 Integration, HTTP/JWT, Playwright, proveedor remoto y restauración durable = NO_APLICA a este diff**. Ningún check CANCELLED/SKIPPED/IN_PROGRESS es PASS causal. Registrar evento BITACORA y PLAN H/I; LISTO DEV sólo con tests/CI requeridos SUCCESS, A5 por otra Task Javier. No QA/main/PROD, cambios de schedules, secretos o fórmulas.
