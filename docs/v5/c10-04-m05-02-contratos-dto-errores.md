# C10-04.M05.02 — Contrato DTO y errores seguros

SOLQARYN dev, PROJECT_SCOPE_LOCK=STRICT. Dueño DEV01 Task 6adbb9fff1548191a1bd5b30d9e9984a. R2: evidencia autenticada, tenant y excepciones. Objetivo M05.02: validación reproducible del contrato Application DTO, NO ejecutar un API/RESTORE en producción.

## A0, dependencias e impacto
Predecesores M04.01-06+A5 comprobados en DAG y COLA. La clase existente C1004CacheJobsApplicationService.PrepareAsync no manejaba una excepción síncrona o asíncrona del proveedor ReadVerifiedAsync, que podía difundir detalles privados. Reparación mínima: traducir excepciones recuperables a VERIFIED_SOURCE_UNAVAILABLE y relanzar OperationCanceledException. El contrato Result sigue sin permisos de encolar, escribir, leer privados o conceder grants. Solo clase Application, test xUnit, workflow, esta evidencia y CHANGELOG. Sin cambios EF, MySQL, JWT, API real, worker, Cloudflare R2 ni credenciales. Revert normal.

## A1 y A2, prueba falsable y seguridad
Se cubre DTO válido sin autoridad; fallos síncrono y asíncrono del proveedor con marcador sensible nunca retornado; cancelación propagada; DTO nulo, work ID traversal, operación URL, key vacía, versión inválida sin consultar fuente; hint de tenant B denegado; snapshot ausente y membresía revocada fail-closed. Se reejecutan regresiones anteriores de Application. No implica tests reales de JWT/RBAC, HTTP, MySQL tenant A/B ni permisos de almacenamiento.

## A3 integración y límites
GitHub-hosted .NET 10 compila código real Application y xUnit; SHA exacto con Backend Release, Seguridad, Authorization, Scope Lock y V5. EF/MySQL, servidor HTTP y provider R2 no aplican al diff sin integraciones; quedan como gates futuros de M05 y R3. Sin falsificar checks en curso.

## A4, A5
VALIDANDO hasta CI dirigida completed/success más checks causales y readback H/I, COLA, DAG y BITACORA; LISTO solo del contrato DTO acotado. A5 independiente por otra Task Javier. No QA, main, PROD, modificación de schedules ni autocertificación.
