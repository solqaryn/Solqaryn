# Certificación ERP-N7.2 — Reintentos controlados del Outbox

## Alcance certificado

ERP-N7.2 certifica el procesamiento controlado de mensajes Outbox ya registrados por ERP-N7.1: recuperación de claims stale, claim tenant-bound, despacho con identidad durable, confirmación protegida por intento esperado, reprogramación con backoff+jitter, transición a dead-letter al agotar intentos y observabilidad sin persistir material sensible.

Autoridad operativa única: `docs/VAEP_AUTHORITY.md`. Rama certificada: `Desarrollo`.

El alcance de N7.2 no incluye `main`, Producción, deploy, secretos ni PR #2. Tampoco presenta como implementado un scheduler/HostedService: `OutboxRetryProcessor` expone procesamiento de lote y puede ser invocado por una etapa operativa posterior.

## Contrato de retry

- El procesamiento exige `empresaId > 0` y timestamps UTC.
- Antes de reclamar mensajes disponibles se recuperan claims `Procesando` stale dentro del tenant.
- Cada transición de entrega/fallo queda condicionada por `EmpresaId`, `MensajeId` e intento esperado; un claim supersedido no fuerza una transición obsoleta.
- El dispatcher recibe el `MensajeOutbox` durable; la implementación externa debe propagar `ClaveIdempotencia` al proveedor.
- La política canónica `OutboxRetryPolicy` usa backoff exponencial con jitter acotado, demora máxima y límite de intentos configurables/validados.
- Al alcanzar el máximo de intentos la decisión es terminal `DeadLetter`; antes de ese punto el mensaje se reprograma con `DisponibleDesdeUtc` calculado.
- Una cancelación solicitada se propaga; los fallos de entrega no persisten `Exception.Message`, payload ni datos del proveedor.

## Seguridad, auditoría y observabilidad

La auditoría de retry usa allow-list estricta con `EmpresaId`, `MensajeId`, `Intento` y `Resultado`. No registra payload, `ClaveIdempotencia`, `Authorization`, secretos, `Exception.Message` ni datos del proveedor.

Una caída del servicio de auditoría posterior a una entrega confirmada no reabre el mensaje ni lo convierte en `DELIVERY_FAILED`; el processor conserva la entrega y evita habilitar un reenvío duplicado por un fallo de observabilidad.

La telemetría frontend certificada en N7.2.G aplica el mismo principio fail-closed: las pruebas dirigidas rechazan persistencia de payload/autorización/secreto y el candidate quedó congelado sin supersession funcional posterior.

## Evidencia canónica

Evidencia backend/seguridad N7.2.F:

- Receipt: `vaep/evidence/fragments/N7.2.F_LISTO_REAL_20260913T120547Z.json`.
- Candidate funcional final: `dce32d1336a4f541e75818115b346003a7b250f6`.
- REVIEW_FIRST: `vaep/evidence/reviews/N7.2.F_REVIEW_FIRST_20260913T120046Z_SUP48.json`, P0=0/P1=0.
- Gate de hardening: run `34755931334`, job `103720179912`, SUCCESS.
- Gate backend/runtime MySQL: run `34755931419`, job `103720190895`, SUCCESS.

Evidencia QA/CI N7.2.G:

- Candidate funcional congelado: `6f297cd6107312ad9e301491d47d9f88b5497539`.
- REVIEW_FIRST: `vaep/evidence/reviews/N7.2.G_REVIEW_FIRST_20260913T123621Z_FINAL.json`, P0=0/P1=0, `candidateFrozen=true`, supersessions post-freeze=0.
- Control head equivalente: `ad0ee0eafe3ab5fd1a4ea028ea37d5597640b0d7`.
- Gate causal exact-head `Static contracts, tests and production build`: run `34757554438`, job `103724457658`, SUCCESS a `2026-09-13T12:43:51Z`.
- Receipt: `vaep/evidence/fragments/N7.2.G_LISTO_REAL_20260913T124538Z.json`, commit `12c61852ed0e5ab7f5375944721aa8eb1ecb56b1`.

## Gates y aplicabilidad

- Build/tests backend: PASS en la cadena causal N7.2.F y nuevamente cubiertos por el gate exact-head de N7.2.G.
- Hardening/auditoría: PASS en N7.2.F.
- Frontend lint/tests/build y regresión de telemetría: PASS en N7.2.G.
- Migración DB adicional para N7.2.G: N/A, sin delta de schema.
- Deploy: fuera de alcance.

Este documento no sustituye REVIEW_FIRST ni receipt de N7.2.H. El cierre documental exige reconciliación aditiva/history-preserving de los documentos canónicos afectados y un REVIEW_FIRST fresco sin P0/P1 abiertos.

## Operación y recuperación

Ante un lote de retry:

1. operar siempre dentro de un `empresaId` explícito;
2. recuperar claims stale antes de reclamar trabajo nuevo;
3. no forzar una transición si el intento esperado ya fue supersedido;
4. conservar la identidad/idempotencia durable al invocar el efecto externo;
5. reprogramar sólo según `OutboxRetryPolicy` y respetar dead-letter al alcanzar el límite;
6. usar auditoría/telemetría para diagnóstico sin copiar payload, credenciales o datos del proveedor.

La recuperación de un claim stale no equivale a confirmar una entrega previa. Si existe duda de efecto externo, la protección debe descansar en la identidad idempotente del mensaje y en la confirmación condicionada, no en un reenvío manual que evite el contrato Outbox.

## Rollback

Rollback de N7.2 significa revertir en `Desarrollo` el changeset funcional/documental correspondiente y volver a ejecutar los gates causales de la cadena afectada. No implica borrar mensajes Outbox existentes, resetear dead-letter en Producción ni manipular proveedores externos desde VAEP.

## Aplicabilidad documental

- ADR nuevo: N/A; N7.2 desarrolla la política de retry del Outbox ya adoptado y no introduce una decisión arquitectónica externa adicional.
- ERD nuevo: N/A para este cierre documental; N7.2 no requiere un nuevo delta de schema certificado por N7.2.G/H.
- OpenAPI adicional: N/A; el retry processor no publica un endpoint nuevo en este cierre.
- Runbook: este documento define operación/recuperación mínima del processor sin inventar un scheduler/deploy fuera del alcance implementado.

## Estado de certificación

N7.2.A–G quedan representados por su cadena VAEP y N7.2.G dispone de receipt `LISTO_REAL`. N7.2.H sólo puede cerrarse cuando la reconciliación documental requerida sea aditiva/history-preserving, REVIEW_FIRST quede en P0=0/P1=0 y los gates causales aplicables al exact-head sean terminales verdes o explícitamente N/A por causalidad.
