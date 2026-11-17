# C09-04.M03.06 — auditoría Outbox estructurada y errores seguros (V19)

Origen: solqaryn/Solqaryn@dev; R2 (integración/tenant/observabilidad); owner DEV02, V17 lock SQV17_C09_04_M03_06. No alterar QA/main/PROD.

## A0 — alcance, impacto y rollback
Se añade un ILogger opcional a OutboxTenantDeliveryWorker sin activar servicios ni cambiar DI, repositorio, EF, DTO, JWT, API o proveedores. Registra eventos estructurados OUTBOX_TENANT_DENIED, OUTBOX_MESSAGE_DENIED, OUTBOX_BATCH_SUMMARY y OUTBOX_PROCESS_FAILED. Valores allowlisted: EmpresaId, código constante y contadores agregados. No se registra payload, destinatario, ClaveIdempotencia, correlación, Exception ni Exception.Message. La auditoría histórica de OutboxRetryProcessor permanece separada e intacta. Ante fallo en proceso se conserva la excepción original y se emite solo código seguro. No se modifica confirmación/CAS, retry/dead-letter, filtrado A/B ni aislamiento existente. Rollback: git revert normal del commit con SHA y HEAD revalidado.

## A1–A3 — pruebas causales por riesgo
Cinco xUnit con entradas sensibles: denegación empresarial antes de claim; éxito con contadores, sin datos sensibles; revocación del consentimiento de mensaje sin envío; lote A/B contaminado con denegación previa a despacho; excepción del proveedor con string sensible, sin filtración. Exigen 0 envíos en escenarios negativos, preservación de resultado/reenqueue y ausencia de claves/payload/error real en registros. Requiere ejecución real .NET 10 Release xUnit en GitHub-hosted. Dobles de repositorio/gate/dispatcher: NO acreditan SMTP/WhatsApp, MySQL real, JWT HTTP, consentimiento externo, HMAC ni integración productiva. MySQL 8.4 efímero no es causal al diff exclusivo de Application sin EF/cambios de DB; NO_APLICA motivado. A5 por Task Javier distinta.

## A4 — publicación y checks
Check-runs por SHA completo, Scope Lock, V5/procedencia, .NET Release, seguridad/tenant y pruebas pertinentes SUCCESS. SKIPPED/IN_PROGRESS/FAIL no son PASS de prueba requerida. PLAN_MAESTRO H/I, BITACORA 26 columnas, DAG/COLA y lock V17 con readback. CI externa todavía en progreso implica VALIDANDO, sin LISTO ficticio. No aprobar QA/main/PROD.
