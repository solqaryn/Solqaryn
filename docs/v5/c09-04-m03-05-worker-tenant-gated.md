# SOLQARYN V19 — C09-04.M03.05: worker acotado tenant-gated

Origen: solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. Autor DEV06 (6af16bbad26881918c283abe06e1f32a), PLAN_MAESTRO fila 1422, candado V17 SQV17_C09_04_M03_05 / 2000001422. Riesgo R2 de integraciones, idempotencia y aislamiento.

## A0 — Cambio real y límites

Se introduce OutboxTenantDeliveryWorker en Application. Antes de recuperar stale, tomar claims o escribir, requiere un gate empresarial. Antes de cualquier efecto externo, vuelve a verificar por EmpresaId/EventoId/TipoEvento y rechaza contaminación de tenant; no entrega payload, claves idempotentes, destinatarios ni secretos al gate. El retry, dead-letter, transición CAS, auditoría opaca y no duplicación continúan delegados al OutboxRetryProcessor existente.

No existe implementación permisiva del gate ni registro de DI, HostedService, cron, ruta, transporte SMTP/WhatsApp, webhook o infraestructura adicional. Nada se envía a un proveedor real; la activación requiere futura integración separada con consentimiento server-side, autorización tenant, opt-out, firma HMAC si aplica y anti-replay. Este changeset no sustituye la certificación de esos controles.

## A1–A3 — Regresiones pertinentes

Cinco tests xUnit: denegación tenant antes de I/O; entrega autorizada una sola vez; revocación por mensaje con fallo opaco y cero envíos; contaminación A/B de lote completo; cancelación, fecha no UTC y empresa cero sin claims. Se prueba la lógica verdadera OutboxRetryProcessor+worker con repositorio y dispatcher **dobles**. NO se afirma prueba de red, JWT real, HTTP, consentimiento real, MySQL, webhooks, entrega proveedor ni restauración de datos. No hay cambio EF/schema/migraciones, de modo que MySQL 8.4 es NO_APLICA causalmente a este código Application; un provider real necesita tests propios posteriores.

## A4/A5 — Cierre proporcional

Exigir xUnit Release, seguridad multiempresa, check Scope Lock exact SHA, V5 y readback de PLAN/COLA/DAG/BITACORA. FAIL/SKIPPED/QUEUED no equivalen a PASS. A5 de otra Task Javier para este SHA, no autocomprobación. Rollback git revert normal. Prohibido QA/main/PROD o contacto externo.
