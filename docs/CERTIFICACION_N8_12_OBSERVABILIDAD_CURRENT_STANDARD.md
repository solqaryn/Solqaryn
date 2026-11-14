# N8.12 — Observabilidad — Certificación current-standard

## Alcance certificado

Esta certificación cubre la revalidación current-standard de **N8.12 — Observabilidad** en la rama `Desarrollo`, bajo `docs/VAEP_AUTHORITY.md`, para el alcance definido en COLA: **logs, métricas, tracing/correlation, health/readiness y señales de alerta**.

El histórico se utilizó únicamente como evidencia de apoyo. La base funcional causal revalidada es `6fd3e28cbf28164d110d6b83756b9094cec654a6`, cuyos árboles de producto permanecen equivalentes al estado certificado: backend `f8db476310130c169ddb5d8c4ea03cfbab9617f6`, API `9bd270713d66066ccdc3de9b62def4fa4df3a01f`, tests `e0cf8fcc8a0b8bbb7c4680fee395327d81de8346` y frontend `3d7847eb8817b14942746f1e31114e56c15efbdd`.

## Estado material de la observabilidad

- **Métricas de requests:** `RequestObservability` registra cantidad de requests, 5xx, requests lentos y duración. Las etiquetas se limitan a método HTTP normalizado y clase de status, evitando cardinalidad no acotada.
- **Correlation/tracing:** `CorrelationIdMiddleware` valida `X-Correlation-ID` con longitud máxima 64 y charset seguro; genera un identificador cuando el valor recibido no es válido y propaga correlation/trace en el contexto de logging.
- **Logs/señales de alerta:** `RequestObservabilityMiddleware` emite señales estructuradas para 5xx y requests lentos con método, endpoint, status y duración. No agrega body, query string, headers ni bearer tokens a esas señales.
- **Health/readiness:** `/health` expone sólo estado/servicio; `/health/ready` verifica conectividad de base de datos y responde únicamente `ready/connected` o `not_ready/unavailable`, sin nombres de base de datos, credenciales ni secretos.
- **Configuración:** `Observability.SlowRequestThresholdMs`, `ErrorAlertStatusCode` y `EnableAlertLogs` mantienen umbrales configurables. No existe una dependencia obligatoria de exporter/collector externo para el contrato vigente; la implementación certificada usa instrumentación .NET y alert logs estructurados.
- **Seguridad:** la revalidación no altera autenticación, RBAC, aislamiento tenant, límites de uploads, rate limiting ni límites de endpoints públicos. No se introdujo persistencia ni migración causal de observabilidad.

## Pruebas y gates causales

Los gates causales sobre el mismo functional head fueron:

- `35258962286` — **SUCCESS** — autorización, archivos, secretos, tenant y proof de seguridad/restauración aislada.
- `35258969104` — **SUCCESS** — restore/build/test backend completo, lint frontend y build de producción.

La superficie de tests exacta contiene `RequestObservabilityTests` (`38eb4d2fc4bf3b9255ba54f3a7323edb08337e55`) y `SecurityBoundaryContractTests` (`ed58d29072ac95d78eb837e1a9a87e4cf4a3de7b`). Se cubren counters de request/error/slow, cardinalidad acotada de método, ruta 5xx del middleware, correlation/trace context y límites de autenticación/upload/rate-limit públicos.

No aplica E2E de UI porque N8.12 no introduce flujo frontend; no aplica migración porque no existe delta de schema/persistencia; tampoco existe un nuevo requisito de benchmark de performance dentro de este alcance bounded. El frontend se preservó byte-equivalente al head funcional probado.

## Cadena A–G revalidada

| Microtarea | Resultado current-standard | Receipt / head |
|---|---|---|
| N8.12.A PRE | LISTO | `4c985d47c30beae313fe762d69554c7909970819` |
| N8.12.B DOMAIN | LISTO, N/A grounded | `fe63d1842f4e995698c0035ae1d931e424a3d6af` |
| N8.12.C DB_MIG | LISTO, N/A grounded | `8b51e8bd86d5049c122663c2a7d001c3bf29d433` |
| N8.12.D BACKEND_API | LISTO | `9b546c6f29b4162541aa23908ae4ab10d16ad33d` |
| N8.12.E FRONTEND_UX | LISTO, N/A grounded | `af6ac7087daba031f7462b2bf915a717c27f2578` |
| N8.12.F SEC_AUDIT | LISTO | `fbe60716a4d4760e83426d8b17f574f98e190737` |
| N8.12.G TEST_CI | LISTO | `6d50b29975b0e60094c3959ffd5665a1a8bdeab6` |

Cada cierre current-standard de esta cadena exige y conserva `REVIEW_FIRST`, DoD material, P0=0/P1=0, equivalencia al functional head/gates causales, receipt y readback.

## Riesgos y límites declarados

Esta certificación no afirma que un exporter externo, APM comercial o alert manager esté desplegado: el contrato vigente certifica instrumentación .NET, contexto de tracing/correlation, health/readiness y señales de alerta estructuradas. Cualquier futura adopción de collector/exporter deberá conservar cardinalidad acotada, secreto-safety y los límites de tenant/RBAC actuales.

Los logs generales de excepciones continúan siendo server-side; N8.12 no agregó captura directa de payloads, query strings, headers de autenticación ni tokens. La política de esta certificación es no sobreafirmar que texto arbitrario originado por una dependencia externa jamás pueda contener datos sensibles.

## Seguridad operacional

La revalidación y certificación se limita a `Desarrollo`. No se modifica `main`, Producción, PR #2, secretos, DNS, certificados ni datos productivos.

## Criterio de cierre DOC_CERT

N8.12.H sólo puede quedar `LISTO` después de:

1. reconciliar `TASKS.md` y `CHANGELOG_AI.md` mediante append history-preserving demostrable (`additions > 0`, `deletions = 0`, prefijo histórico exacto);
2. ejecutar `REVIEW_FIRST` final con P0=0/P1=0;
3. persistir receipt H y realizar write/readback; y
4. sincronizar el control-plane y promover `N8.13.A` únicamente después del cierre real.
