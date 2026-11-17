# SOLQARYN V19 — C09-04.M05.02 contrato API/DTO y errores seguros

Alcance exclusivo: `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, PLAN fila 1431, owner DEV05. Riesgo R2 (contrato HTTP, tenant, seguridad y mensajeria). Predecesor: M04.01–06 + A5, DAG F=VALIDADO/G=SI y COLA G/O=SI en el CLAIM V17 `SQV17_C09_04_M05_02` / `2000001431`.

## A0: diferencia y consumidores
Se agrega exclusivamente una suite xUnit sobre `MensajesOutboxController.Registrar`, su workflow GitHub-hosted y este documento, ademas de CHANGELOG aditivo. Se conserva el controlador productivo, `RegistrarMensajeOutboxRequest`, `ApiResponse<MensajeOutboxRegistroResponse>`, filtros, rutas, DB, proveedores y seguridad. Ningun endpoint nuevo, registro HostedService, migracion EF ni entrega comercial. Contrato existente `POST api/outbox/tenants/{empresaId:int}` con `Idempotency-Key`; no equivale a campañas/proveedores conectados.

## A1: pruebas que pueden fallar
Ocho escenarios xUnit previstos: respuesta aceptada 202 con DTO y clave normalizada; tres bordes de cabecera idempotente ausente, vacia y espacios -> 400 RFC7807 sin servicio; intento de tenant B con contexto de A -> 403 sin servicio; conflicto -> 409 opaco; membresia revocada -> 403 opaco; DTO invalido -> 400 opaco. Cada ProblemDetails comprueba tipo `application/problem+json`, codigo, status, traceId y path. Los errores inyectados contienen indicadores sensibles para detectar una filtracion accidental. La prueba positiva evita afirmar que un proveedor envio correo o WhatsApp.

## A2 y A3: frontera real
El controlador y DTO del proyecto se ejecutan realmente en memoria en .NET 10, pero los servicios se sustituyen y el controlador se invoca directamente: **no se ejecuta el pipeline de Authorize/RequierePermiso, no se valida JWT firmado, ni tenant A/B real en MySQL, ni transporte SMTP/WhatsApp, webhook o consentimientos**. Esas pruebas requieren un scope de integracion independiente. MySQL 8.4 fisico NO_APLICA causal a este diff test-only; no se convierte en PASS. No se usan proveedores, datos ni secretos reales.

## A4 y A5
Exigir xUnit Release GitHub-hosted completado con SUCCESS por SHA exacto, Scope Lock, V5 procedencia, backend y seguridad pertinentes, mas H/I y BITACORA con readback. QUEUED, IN_PROGRESS, SKIPPED, CANCELLED, FAIL nunca PASS. Si CI externa esta pendiente, conservar VALIDANDO/owner, SHA y run, sin anticipar LISTO; revisar primero al reanudar. A5 necesita Task Javier distinta de DEV05 y revision del codigo/pruebas. No QA/main/PROD, no formulas manuales ni mas de una EN_PROGRESO propia.

Rollback: `git revert` normal selectivo, con HEAD y concurrencia comprobados, preservar BITACORA historica.
