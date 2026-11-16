# SOLQARYN V17 — C02-01.M03.06: auditoría segura de elección y revocación de contexto

Autor: PRIMARY DEV 06 JAVIER; TASK_ID=6af16bbad26881918c283abe06e1f32a. Repositorio solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. Riesgo R2 (identidad, pertenencia tenant, concurrencia y auditoría).

## Alcance ejecutable

La implementación Infrastructure .NET 10 crea C0201TenantPreferenceSafeAuditService como fachada de revisión sintética segura sobre el worker de M03.05. Acepta un único intento por clave lógica y máximo 128 intentos por instancia de una sola sesión autenticada. Utiliza SemaphoreSlim.WaitAsync(0) para rechazar una segunda selección mientras la primera esté pendiente; ninguna respuesta mezclada, revocada, obsoleta o fallida puede convertirse en éxito. Un intento rechazado o interrumpido no instala contexto, no autoriza acceso ni reemite JWT. Cada consumidor debe crear una instancia por sesión lógica; NO registrar como singleton compartido entre actores.

El evento estructurado IC0201TenantAuditSink contiene solo código cerrado y recuento; nunca incluye actor, empresa, cookie, clave de operación, JWT, IP, nombres ni texto de excepción. C0201TenantAuditLoggerSink implementa ILogger estructurado sin prometer almacenamiento durable. Si el sink informa fallo o lanza excepción, el resultado es AUDIT_SINK_UNAVAILABLE. La salida exitosa es únicamente PREVIEW y requiere nueva verificación autenticada del backend para un cambio real.

No se integra con endpoints reales, persistencia, sesiones live ni proveedores externos. No sustituye una auditoría durable, JWT HTTP, autenticación, MySQL, migraciones, jobs hospedados ni producción. No concede permisos ni certifica funcionamiento end-to-end.

## Evidencia de calidad R2 (A0–A5)

A0: nueva fachada, puerto, sink, xUnit y CHANGELOG aislados Infrastructure; no modifica API/EF/Angular ni arquitectura. A1: casos positivos, 0/1/N, duplicados, cancelación, concurrencia y fallos de dependencias. A2: tenant ajeno, rol fingido, pertenencia revocada, SHA/actor obsoleto, no filtración PII. A3: build .NET 10 Release y xUnit reales en GitHub Actions; MySQL/HTTP/Playwright NO_APLICA por no cambiar esos runtimes. A4: Scope Lock, V5, backend y controles de autorización/seguridad del SHA causal, readback PLAN/COLA/DAG/BITACORA. A5: revisor independiente Javier de TASK_ID distinta. Rollback mediante git revert normal; QA/main/PROD intactos.
