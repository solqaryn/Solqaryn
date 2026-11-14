# Certificación ERP-N7.1 — Outbox Pattern

## Alcance certificado

ERP-N7.1 certifica el registro durable, tenant-bound e idempotente de una intención de efecto externo antes de su despacho. El alcance termina en el registro de la intención outbox; el claim, dispatcher, política de retry y dead-letter operativa pertenecen a microtareas posteriores y no se presentan aquí como implementados por N7.1.

Autoridad operativa: `docs/VAEP_AUTHORITY.md`. Rama certificada: `Desarrollo`.

## Contrato funcional

- Entidad: `MensajeOutbox`.
- Cada intención exige `EmpresaId`, `EventoId`, `TipoEvento`, `PayloadJson` y `ClaveIdempotencia`.
- El estado inicial es `Pendiente`; la entidad conserva timestamps UTC, contador de intentos, disponibilidad, correlation ID y error terminal/intermedio para el ciclo posterior.
- La clave de idempotencia se normaliza y se evalúa dentro del tenant; la persistencia mantiene el índice único `(EmpresaId, ClaveIdempotencia)` como defensa final ante carreras.
- `MensajeOutboxRepository.AddAsync` agrega al mismo `DbContext` pero no hace `SaveChanges`, preservando el límite de unidad de trabajo del caller.
- `MensajeOutboxService` valida autoridad server-side de la empresa y la idempotencia antes de stagear la intención.

## API y seguridad

Endpoint certificado: `POST /api/outbox/tenants/{empresaId}`.

Requisitos y comportamiento:

- Autenticación requerida.
- Permiso `Configuracion/Crear` requerido.
- Header `Idempotency-Key` obligatorio.
- El tenant autorizado por el contexto de permisos debe coincidir con `{empresaId}`; un mismatch se rechaza fail-closed con HTTP 403 y código `OUTBOX_TENANT_CONTEXT_MISMATCH`.
- Conflictos de idempotencia se expresan como RFC7807 sin filtrar detalles del provider.
- El registro exitoso y el rechazo cross-tenant emiten auditoría de aplicación sin registrar payload ni `Idempotency-Key`.
- Las respuestas de error usan `application/problem+json`, `code` estable y `traceId`.

## Evidencia de implementación

Candidate funcional congelado de N7.1.F/G: `2bb8148c1119a0a6c433b60cd9a893a3933e4a3e`.

Regresión dirigida de binding tenant/permiso: `ca9ce43e03169cbcffe33e7e35635feb544c9063`.

REVIEW_FIRST de seguridad: `cac97e05d14682d28d8b814874d73ccaa7be0083`, P0=0/P1=0.

Receipts canónicos:

- N7.1.E: `vaep/evidence/fragments/N7.1.E_LISTO_REAL_20260913T064820Z.json`.
- N7.1.F: `vaep/evidence/fragments/N7.1.F_LISTO_REAL_20260913T070217Z.json`.
- N7.1.G: `vaep/evidence/fragments/N7.1.G_LISTO_REAL_20260913T070412Z.json`.

## Gates causales certificados

Sobre el control head `cac97e05d14682d28d8b814874d73ccaa7be0083`, funcionalmente equivalente al candidate congelado:

- `API, jerarquía, RBAC, auditoría, MySQL, Angular y E2E`: run `34743908510`, job `103688163982`, SUCCESS.
- `Static contracts, tests and production build`: run `34743908521`, job `103688167028`, SUCCESS.

La regresión dirigida verifica que una ruta tenant distinta del tenant autorizado devuelve 403 RFC7807 y no invoca el servicio; los gates anteriores cubren build/tests backend, arranque API con MySQL, lint/build frontend, E2E y auditoría estática fail-closed.

## Operación, recuperación y rollback

N7.1 no certifica un dispatcher ni una política de retry. Por ello no se inventa un runbook de envío externo.

Ante un error de registro:

1. no enviar el efecto externo por un camino alterno que evite el outbox;
2. conservar la misma `Idempotency-Key` para reintentar la misma intención lógica;
3. tratar 403 como fallo de autoridad tenant, 409 como conflicto/race de idempotencia y 400 como violación del contrato;
4. usar `traceId`/correlation y la auditoría para diagnóstico sin exponer payload ni secretos.

Rollback de N7.1 significa revertir el changeset de aplicación/persistencia correspondiente en `Desarrollo` y volver a ejecutar los gates causales; no implica borrar filas outbox existentes ni manipular Producción desde VAEP.

## Aplicabilidad documental

- ADR nuevo: N/A; N7.1 no cambia una decisión arquitectónica externa a su propio contrato de outbox.
- ERD nuevo: N/A para este cierre documental; la persistencia ya fue certificada en la cadena N7.1 y este documento no altera schema.
- OpenAPI adicional: N/A para este cierre; el contrato efectivo queda definido por el controller y su RFC7807.
- Runbook de dispatcher/retry: diferido intencionalmente a N7.2+; documentarlo aquí como operativo sería filler/falso alcance.

## Estado de certificación

N7.1.A–G disponen de evidencia de cierre en la cadena VAEP. N7.1.H queda listo para REVIEW_FIRST documental únicamente cuando `CHANGELOG_AI.md` y `TASKS.md` se reconcilien de forma aditiva/history-preserving y este documento sea releído sin P0/P1 abiertos.

No se autoriza tocar `main`, Producción, deploy, secretos ni PR #2 como parte de esta certificación.
