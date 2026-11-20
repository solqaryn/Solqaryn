# C11-03.M03.06 — Auditoría estructurada segura del relay

Riesgo R2, DEV01, PLAN_MAESTRO D1675. Adaptador opt-in que envuelve el worker aislado existente C11-03.M03.05. No agrega DI, HostedService, API, proveedor externo ni scheduler.

A0/A2: registra únicamente código fijo, tenant numérico y contadores. No registra payload, clave idempotente, cadena de conexión, PII ni excepción interna. Error inesperado convertido a OUTBOX_RELAY_INTERNAL_FAILURE sin inner exception; cancelación preservada. La caída de logger no cambia persistencia SQL. El caller debe autorizar tenant; el adaptador no implementa autenticación nueva.

A1/A3: prueba real Oracle EF/MySQL 8.4 efímero con tenant A/B, fallo parcial de transporte scriptado, retry SQL durable, A entregado y B pendiente, logs sin PII. Otros dos casos adversariales de error sanitizado y sink fallido son dobles aislados, no pruebas de SQL ni proveedor. TRX dirigido 3/3; un caso de integración física SQL. No se certifica entrega exactly-once ni proveedor real.

A4: GitHub-hosted exact SHA .NET10 Release, ScopeLock, V5, seguridad, TRX completed/success. Con CI pendiente conservar VALIDANDO. A5 por otra Task Javier; sin QA/main/PROD. Rollback con git revert normal FF/CAS, sin borrar filas SQL ni reescribir historia.
