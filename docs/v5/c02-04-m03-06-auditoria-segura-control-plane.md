# C02-04.M03.06 — Registro estructurado y errores seguros del control plane (V18)
 
**Propietario:** PRIMARY DEV 01, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. **Rama:** solqaryn/Solqaryn@dev. **Riesgo V15:** R2 (control de acceso, auditoría, MFA, tenant). **Candado:** V17 SQV17_C02_04_M03_06, ID 2000000415. **DAG:** M02.01–06+A5, M03.01–05+A5, VALIDADO/SI.

## A0 — alcance y reversibilidad
- Se incorpora un sink separado en Infrastructure .NET 10 para los códigos discretos del resultado del worker aislado M03.05. Una fuente backend fresca verifica identidad, tenant A/B de la misma solicitud, rol de plataforma, autenticación, MFA/reautenticación, permiso explícito de auditoría, audit fence, revocación y SHA canónico idéntico.
- ILogger emite solo EventId 204306 y un código fijo del allowlist. No registra idempotency key, solicitudes, actor, tenant, SHA, JWT, credenciales, PII, excepciones ni payload. Rechaza códigos arbitrarios antes de consultar autoridad; fallos de autoridad/logger devuelven códigos opacos. La cancelación se propaga.
- No agrega endpoint, DI, BackgroundService, proveedor, datos persistidos, EF/MySQL, transacciones, tenant global, lectura cross-tenant, workflow, despliegue ni cambio de permisos. El resultado logger-accepted NO es un audit trail durable ni autorización. Requiere adaptador autenticado y auditoría transaccional antes de funcionalidades reales.
- Rollback sin borrar historia: git revert del commit propio; no alterar BITACORA, QA/main/PROD, ni secretos.

## A1–A3 — evidencia causal
- Pruebas xUnit reales de aceptación de código fijo, rechazo de inyección, actor falso, tenant B, sesión inválida, MFA, permisos, revocación, SHA stale, excepciones opacas, logger deshabilitado, cancelación y constructor; deben acreditar ejecución en GitHub-hosted .NET Release.
- Seguridad: backend es única autoridad; no se confunden mocks de evidencia con JWT HTTP autenticado ni con una auditoría de producción.
- Integración pertinente in-process Infrastructure/logger + contrato de worker. MySQL 8.4, migraciones EF, API HTTP, Angular/Chromium y proveedores externos: NO_APLICA al diff, sin afirmación de certificación global.

## A4 y A5 — barreras de publicación
- Validar checks por SHA exacto para .NET Release/xUnit, seguridad multiempresa, Authorization, Scope Lock y procedencia V5. CANCELED/SKIPPED/IN_PROGRESS no son PASS; descendiente solo con igualdad de blobs y dependencias.
- PLAN, COLA, DAG y BITACORA se revalidan después de cada transición. LISTO requiere A0–A4 completos y readback. A5 independiente por una de las otras cinco Tasks Javier, nunca por el autor. Ningún PASS autoriza QA/main/PROD.
