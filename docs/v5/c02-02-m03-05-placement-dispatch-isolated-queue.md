# SOLQARYN V17/V15 — C02-02.M03.05: cola sintética acotada de placement

ID C02-02.M03.05; autor PRIMARY DEV 02 JAVIER (6af16bb9492c8191b8e8c7d606f11507); fila PLAN 342; V17 lock 2000000342. Rama dev, Scope Lock STRICT. Riesgo R2 (tenant shared/dedicated, concurrencia, idempotencia y potencial autoridad de almacenamiento). A5 requiere otro Task ID Javier.

## A0: alcance e impacto

Se agrega clase ejecutable Infrastructure .NET 10 C0202PlacementIsolatedDispatchQueue, compuesta sobre el journal M03.04, adaptador M03.03, gate Application M03.02 y Domain M03.01. Admite una sola previsualización sintética in-memory bajo mutex asincrónico y revision SHA/tenant/actor/clave de idempotencia exactas. Un replay ordinal igual es inerte y otro contenido/key se deniega. Ninguna respuesta autoriza almacenamiento o cambio de placement.

No hay HostedService, DI, API, broker, proveedor de cola, DbContext, EF, MySQL, SQL, migraciones, router, DSN, endpoint del cliente, worker live o cutover. Solo estructuras sintéticas de prueba en memoria. Sin escritura de datos, claves ni permisos nuevos. Dependencias existentes inalteradas; clientes productivos no usan esta clase. Reversa: git revert normal del changeset en dev preservando HEAD de otros escritores. Sin QA/main/PROD.

## A1 y A2: 11 pruebas xUnit reales a ejecutar en CI

Casos: admisión inerte sin permisos; tenant B y actor ajeno; SHA y revision falsos; endpoint/worker/DSN/cutover negados; JWT, membresía, RBAC y recuperación insuficientes; key/alias invalidos; idempotencia, conflicto de payload y segundo key; concurrencia de 24 candidatos con un solo ganador; cancelación; constructor con identidad insegura. Requeridas Backend Release, seguridad multiempresa, Authorization y checks obligatorios SHA exacto. No atribuir PASS mientras estén queued/in_progress o fallen. Guard de tenants A/B y revocación se ejercita por preflight sintético, no JWT HTTP real.

## A3: integración NO_APLICA motivado

No se cambia la capa EF/MySQL, no se conecta almacenamiento ni proveedor real, no se modifica UI. No corresponde certificar integración MySQL 8.4, Playwright ni HTTP; tampoco es correcto decir que este prototipo prueba un router real o durable queue. La integración real futura requiere nuevas pruebas con sandbox autorizado y Scope Lock específico.

## A4 / A5

Publicación fast-forward/CAS de una única microtarea, con código, suite, documento y encabezado+viñetas juntos de CHANGELOG_AI.md. Comprobar scripts de procedencia, check Scope Lock, .NET Release/tests, seguridad y autorización completados SUCCESS del SHA causal; registrar EN_PROGRESO->VALIDANDO con HEAD, RUN IDs, checks y owner, y sólo pasar LISTO con todos A0-A4 y readbacks Sheets/BITACORA. A5 separado con Task Javier distinta y posible rechazo. No desbloquear DAG por pruebas pendientes.
