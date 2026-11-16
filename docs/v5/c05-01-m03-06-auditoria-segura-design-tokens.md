# SOLQARYN V17 — C05-01.M03.06: auditoría estructurada y errores seguros de temas

Autor: PRIMARY DEV 06, TASK_ID=6af16bbad26881918c283abe06e1f32a. Repositorio solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. Riesgo R2 por claves de configuración, actor/tenant, revocación, simultaneidad y logs.

## Implementación A0

C0501DesignTokenSafeAuditService es una fachada Infrastructure real de auditoría sintética acotada a una sesión in-process, basada en el Worker de C05-01.M03.05. Exige identidad observada, empresa, revisión, epoch de sesión y SHA exacto, valida el lote completo de hasta doce intenciones antes del lector y utiliza un semáforo sin espera para rechazar concurrencia. Mantiene clave idempotente ordinal por operación, máximo 128 por instancia lógica. El Reader y Sink están inyectados por contrato para permitir pruebas adversariales y no dependen de proveedores reales.

El sink ILogger escribe únicamente códigos fijos y recuentos; no emite nombres, IDs, SHA, claves CSS, errores, JWT, DSN, tenant o datos personales. Ante fallo o excepción del lector/sink deniega sin filtrar el contenido de excepciones. Ningún resultado autoriza cambios de tema, RBAC, acceso tenant, escritura MySQL, ejecución de hosted worker o auditoría durable. La instancia no se registra como singleton entre tenants.

## Calidad V15 A1–A5

- A1: once pruebas xUnit ejecutables de nominal, frontera de lote, fuente inválida, tenant B, escritura directa, idempotencia, excepción, caída de sink, respuesta forjada, concurrencia y cancelación/capacidad. Ningún caso equivale a autenticación de producción.
- A2: rechazo fail-closed de actor, tenant, revisión/epoch, SHA, writes, lectura parcial, errores y respuesta con autoridad. Pruebas sintéticas de tenant A/B; ausencia de privilegio efectiva se exige al backend real separado.
- A3: .NET 10 Release/xUnit GitHub-hosted sobre el SHA publicado. MySQL 8.4 efímero, EF, HTTP firmado JWT, Angular/Playwright, CSS y proveedores reales son NO_APLICA al diff aislado sin llamadas a ellos, no se inventa PASS.
- A4: Scope Lock exact-SHA, procedencia V5, Backend Release, seguridad multiempresa, autorización y readback PLAN/COLA/DAG/BITACORA. La CI QUEUED/IN_PROGRESS no es PASS; mantener VALIDANDO si única barrera externa.
- A5: revisión independiente por otra Task Javier distinta; no hay autoaprobación ni autorización para QA/main/PROD. Rollback por git revert normal y registro aditivo, sin borrar BITACORA.
