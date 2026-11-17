# SOLQARYN V19 — C09-04.M05.04 aceptación positiva del contrato outbox existente

Repositorio exclusivo `solqaryn/Solqaryn@dev`, PLAN fila 1433, owner DEV05, V17 lock `SQV17_C09_04_M05_04` (2000001433), Scope Lock STRICT. R2 por tenant/idempotencia. El DAG exige M04.01–06+A5 reales; al reclamar, F=VALIDADO/G=SI, COLA G/O=SI.

## A0: alcance e impacto
Tres tests positivos nuevos integran **el controlador y el servicio Application reales** usando `AppDbContext` InMemory y un repositorio/scope sustitutos. No modifica API productiva, entidades, schema, EF migrations, frontend, worker, adaptadores, secretos, headers, filtros o consentimiento comercial. Se conserva la semántica POST api/outbox/tenants/{empresaId} y respuesta Accepted 202. Rutas y contratos de terceros no cambian.

## A1: oráculos que pueden detectar defecto
1. Intento de alta tenant A con cabecera normalizada: devuelve DTO del evento real y estado Pendiente; una sola intención staged, nada marcado Entregado.
2. Replay de mismo tenant/clave/payload: mismo EventoId y exactamente una alta, nunca dos efectos.
3. Tenants A y B con la misma clave: dos EventoId distintos, cada entidad con EmpresaId correcto, dos altas en espacios lógicos independientes. Si la clave se vuelve global, la tercera prueba falla.

## A2/A3 límites
La llamada es una invocación directa del controlador: NO valida middleware JWT, filtro `[Authorize]`/`RequierePermiso` ni HTTP alojado. `ScopeFalso` simula autoridad; no acredita revocación de roles real. El repositorio es una implementación fake, `AppDbContext` usa EF InMemory; no prueba atomicidad MySQL, idempotencia en carrera distribuida, outbox durable, broker, webhook, HMAC, SMTP, WhatsApp, consentimiento, RPO/RTO ni datos productivos. Estas integraciones requieren otros scopes y gates reales; **NO_APLICA para este diff test-only, nunca PASS ficticio**. No se reemplaza el control de seguridad del servidor por la UI.

## A4–A5
GitHub-hosted .NET 10 Release deberá acreditar 3/3 xUnit exact SHA, Scope Lock, procedencia V5, backend/security causal y PLAN/COLA/DAG/BITACORA readback. `IN_PROGRESS`/`QUEUED`/`SKIPPED` no son PASS. Tras CI externa pendiente, conservar VALIDANDO con owner DEV05 y SHA/run; revisor A5 MUST ser Task Javier diferente. No QA/main/PROD. Rollback selectivo con git revert normal, no force-push, preservar BITACORA.
