# SOLQARYN V19 — C09-04.M03.01: identificadores seguros en el dominio de mensajería

Autor: PRIMARY DEV05 JAVIER; TASK_ID 6adc2e55f47c81919f6374e561687076. Repositorio exclusivo solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. PLAN_MAESTRO fila 1418, V17 lock 2000001418. Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES. Riesgo R2 por tenant, idempotencia, privacidad y correlación.

## A0 — Alcance, arquitectura e impacto

Se refuerza exclusivamente el constructor existente de MensajeOutbox, utilizado por MensajeOutboxService.RegistrarAsync. Los identificadores opacos TipoEvento, ClaveIdempotencia, TipoAgregado, IdAgregado y CorrelationId rechazan caracteres de control (CR, LF, TAB, NUL), incluso antes del trim. Se conservan firma pública, normalización de espacios, máximos previos, estados de dominio, idempotencia y tenant. PayloadJson admite su formato normal de JSON multilinea y no se sujeta a esta regla de identificadores. El rechazo es fail-closed con ArgumentException y nombre de parámetro, sin registrar contenido sensible.

No cambia Application, DTO HTTP, API, Infrastructure, Entity Framework, MySQL, migraciones, workers, SMTP/WhatsApp, configuraciones, secretos, Angular ni despliegue. No se introducen nuevas dependencias ni rutas. La validación deliberadamente puede rechazar identificadores corruptos que antes pasaban.

## A1 — Pruebas causales

Se agregan casos xUnit al assembly existente: positivos con normalización, Unicode imprimible, dos tenants distintos y JSON multilinea; fronteras de longitud igual y superior a 200; negativos CR/LF, TAB, NUL en tipo de evento y clave de idempotencia; controles en tipo/id de agregado y correlación; carácter de control final rechazado y nombre exacto del parámetro. Regresiones originales de estados Pendiente, Procesando, Entregado, Fallido y DeadLetter permanecen presentes.

Los tests deben ejecutarse en GitHub-hosted .NET 10, Release, sobre el SHA real. Publicar código o tests no constituye PASS. No contabilizar resultados queued, in_progress, skipped, cancelled ni failed como aprobados.

## A2 — Seguridad y aislamiento

Se conserva EmpresaId obligatorio positivo, clave tenant-bound, membresía server-side anterior al staging en MensajeOutboxService y rechazo de entradas adversariales. Las pruebas de dominio sobre empresas A/B no sustituyen pruebas de autenticación HTTP, JWT/RBAC real, consentimiento, webhook HMAC, idempotencia durable ni aislamiento MySQL. No se relajan validadores existentes ni se escriben secretos.

## A3 — Integración y reversión

Integración pertinente: construcción de agregado Domain invocada por el servicio de Application y xUnit in-process de backend. El diff no modifica EF/DB, endpoints ni proveedores; MySQL 8.4 efímero, Playwright, HTTP autenticado, SMTP y WhatsApp son NO_APLICA para este changeset específico, no se declaran certificados. Rollback por git revert normal tras verificar HEAD, nunca reset/force-push.

## A4–A5 — Puertas obligatorias

A4: exigir build Release y pruebas xUnit .NET 10 dirigidas, seguridad y autorización pertinentes, verify-solqaryn-project-scope y V5/CHANGELOG en SUCCESS del SHA exacto; leer PLAN/COLA/DAG/BITACORA tras transición, conservar blob hashes, registrar RUN/check/SHA y revisar P0/P1. Si la CI externa está queued o in_progress, conservar VALIDANDO y owner sin LISTO falso.

A5: solo por otra Task Javier distinta del autor, con revisión adversarial propia. Ningún PASS dev autoriza QA, main, PROD, webhooks ni entrega real. Todas las métricas no observadas se declaran NO_MEDIDO.
