# C11-03.M03.06 — Auditoría estructurada segura del relay

Riesgo R2, DEV01, PLAN_MAESTRO D1675. Adaptador opt-in que envuelve el worker aislado existente C11-03.M03.05. No agrega DI, HostedService, API, proveedor externo ni scheduler.

A0/A2: registra únicamente código fijo, tenant numérico y contadores. No registra payload, clave idempotente, cadena de conexión, PII ni excepción interna. Error inesperado convertido a OUTBOX_RELAY_INTERNAL_FAILURE sin inner exception; cancelación preservada. La caída de logger no cambia persistencia SQL. El caller debe autorizar tenant; el adaptador no implementa autenticación nueva.

A1/A3: prueba real Oracle EF/MySQL 8.4 efímero con tenant A/B, fallo parcial de transporte scriptado, retry SQL durable, A entregado y B pendiente, logs sin PII. Otros dos casos adversariales de error sanitizado y sink fallido son dobles aislados, no pruebas de SQL ni proveedor. TRX dirigido 3/3; un caso de integración física SQL. No se certifica entrega exactly-once ni proveedor real.

A4: GitHub-hosted exact SHA .NET10 Release, ScopeLock, V5, seguridad, TRX completed/success. Con CI pendiente conservar VALIDANDO. A5 por otra Task Javier; sin QA/main/PROD. Rollback con git revert normal FF/CAS, sin borrar filas SQL ni reescribir historia.

## V22 DEV06 — corrección P2 de excepción de repositorio y validación de frontera (20 noviembre 2026)

Hallazgo causal: en el adaptador C1103OutboxAuditedRelay la cláusula `catch (ArgumentException) { throw; }` dejaba pasar una ArgumentException de SQL/proveedor con posibles datos sensibles, aunque se sanitizaban las demás excepciones. Se trasladan las validaciones de parámetros de entrada fuera del bloque de proveedor y se sanitizan todas las excepciones del repositorio, incluyendo ArgumentException; la cancelación solicitada continúa intacta. Pruebas adversariales nuevas: repositorio que lanza `ArgumentException("SECRET_PII_CONNECTION_STRING")`, sin exposición en mensaje, inner exception ni logs; y errores de límites del llamador que no invocan SQL. No se modifica el relay durable, DI, HTTP ni proveedor externo. A1/A2/A3/A4 solo se acreditan al terminar GitHub-hosted exact-SHA y su TRX, Scope Lock/V5 y seguridad. A5 por otra Task Javier distinta de autor DEV01 y reparador DEV06, sin QA/main/PROD.
