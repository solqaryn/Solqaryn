# SOLQARYN V16 — C11-04.M02.05: eventos, idempotencia, correlación y límites de observabilidad

**Microtarea:** C11-04.M02.05 (PLAN_MAESTRO fila 1704). **Autor:** PRIMARY DEV 06 — JAVIER, Task `6af16bbad26881918c283abe06e1f32a`. **Riesgo V15:** R0 documental sin runtime; `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO/COLA/DAG_TECNICO vivos. El resultado exigido es definir eventos, idempotencia, correlación y límites de logs, métricas, trazas, alertas y costos sin datos sensibles. No se añade ningún endpoint, SDK, middleware, exporter, collector, worker, webhook, tabla, migración, dashboard, alerta real o integración externa.

## 1. Dependencias e incertidumbres

- M01.06 propone gates G01–G16 y decisiones pendientes D01–D08. M02.01 propone E01–E12, INV01–INV16, REL01–REL10; M02.02 especifica OP01–OP08, DTO01–DTO08, ERR01–ERR12, VAL01–VAL16; M02.03 propone P01–P12, IDX01–IDX12 y MIG01–MIG08; M02.04 define OPT01–OPT20, FL01–FL12, VAL01–VAL18. Son **contratos conceptuales**, no artefactos runtime instalados.
- El repositorio contiene middleware como `CorrelationIdMiddleware`, `RequestObservabilityMiddleware`, `ExceptionHandlingMiddleware`, `RequestObservability`, `DbQueryTimingInterceptor` y `RequestPerformanceContext` preexistentes; este documento NO los cambia ni certifica.
- SLO, p95/p99, disponibilidad, error budget, retención, costo, cardinalidad y sobrecarga = **NO_MEDIDO**; faltan umbrales/versiones/muestras aprobados. No afirmar PASS de rendimiento. Solo datos sintéticos en entornos autorizados podrían medirse en una futura fase.
- Angular 22.2.1 no concede permisos; la autorización JWT, RBAC, revocación, tenant A/B y propiedad de recurso corresponden a ASP.NET Core 10. EF Core/Oracle MySQL permanece intacto. JWT, cookies, PII, SQL con valores, claves de cliente y datos financieros jamás se incluyen en etiquetas/telemetría externa.

## 2. Contratos de eventos FUTUROS EVT01–EVT14 (NO EMITIDOS)

| ID | Evento propuesto | Condición y evidencia mínima futura |
| --- | --- | --- |
| EVT01 | ObservabilityPolicyProposed | Versión, autor, SHA y estado pendiente, sin activar SLO |
| EVT02 | ObservabilityPolicyApproved | Aprobación explícita, cuota, TTL y clasificación |
| EVT03 | MeasurementWindowStarted | Run permitido, perfil sintético, UTC, denominador |
| EVT04 | MeasurementWindowClosed | Conteos procesados/descartados y completitud |
| EVT05 | DiagnosticTraceSampled | Span agregado, correlación opaca, tasa autorizada |
| EVT06 | DiagnosticTraceSuppressed | Motivo de privacidad, tenant, cuota o falta de permiso |
| EVT07 | ServiceLevelIndicatorCalculated | Fórmula, numerador/denominador, unidad, entorno y SHA |
| EVT08 | ServiceLevelObjectiveEvaluated | Objetivo aprobado, ventana y resultado medido/no medido |
| EVT09 | ErrorBudgetThresholdSuspected | Anomalía provisional, sin deploy/alarma automática |
| EVT10 | ErrorBudgetThresholdConfirmed | Prueba reproducible y decisión independiente |
| EVT11 | ObservabilityCostEstimateRecorded | Costo estimado, moneda, volumen y NO_FACTURADO |
| EVT12 | ObservabilityPrivacyBoundaryRejected | Motivo sin almacenar payload sensible |
| EVT13 | ObservabilityDeliveryFailed | Error tipado, reintento acotado y pérdidas contadas |
| EVT14 | ObservabilityPolicyRevoked | Revocación efectiva futura y auditoría conservada |

EVT01–EVT14 no son mensajes ya publicados ni autorización de broker, worker, proveedor, paquete ni servicio de pago.

## 3. Envolvente, correlación y causalidad

La envolvente **hipotética** contendría `event_type`, `schema_version`, `source_commit_sha`, `occurred_at_utc`, `correlation_id` opaco no reversible, `causation_id`, `idempotency_key`, `environment`, `synthetic_run_id`, `classification`, `measurement_window`, `payload_schema_ref`. El tenant solo permanece en un contexto protegido backend no exportable. Rechazar campos desconocidos, PII, token, correo, dirección IP, URL privada, identificadores de órdenes, SQL con datos, cookies o secretos antes de serializar.

Correlación no es autorización ni prueba causal por sí sola. Solo comparar muestras con mismo SHA/comportamiento, ambiente, perfil, navegador, red, ventana, número de observaciones y versión de herramienta. Correlation ID nunca es un token bearer y dos empresas no comparten resultados privados aunque usen el mismo host.

Clave idempotente futura: valor no reversible basado en `event_type+version+authorized_run_id+source_sha+profile_ref+event_occurrence_key`, creado/validado por componente de confianza, con TTL y deduplicación transaccional cuando corresponda. Misma key+payload puede repetir la respuesta sin repetir efecto; misma key+payload distinto = rechazo auditable, nunca overwrite.

## 4. Invariantes de seguridad y operación INV01–INV16

| ID | Invariante verificable en una implementación futura |
| --- | --- |
| INV01 | Mismo key/payload = un efecto lógico; reintentos no duplican run |
| INV02 | Mismo key y payload distinto = conflicto y rechazo |
| INV03 | TTL vencido, sesión revocada o firma inválida = rechazo |
| INV04 | Versión, SHA y causation ID establecen procedencia, no timestamp aislado |
| INV05 | Correlation ID jamás autoriza acceso ni revela usuario |
| INV06 | Tenant A no consulta etiquetas, métricas, costos o trazas privadas de B |
| INV07 | Revocación de rol invalida futuras suscripciones y caché privada |
| INV08 | JWT, cookies, secretos, SQL y PII se censuran antes de serializar |
| INV09 | Etiquetas arbitrarias o de alta cardinalidad se rechazan fail-closed |
| INV10 | Tamaño, tasa, retención y sampling exigen límites aprobados |
| INV11 | Sin aprobación de quota/exporter, telemetría permanece DISABLED |
| INV12 | Reintentos con tope y backoff, nunca bucles ni pagos automáticos |
| INV13 | CI FAIL/CANCELLED/SKIPPED/QUEUED/IN_PROGRESS no equivale a PASS |
| INV14 | SLI sin ventana o numerador/denominador es NO_MEDIDO |
| INV15 | Error de proveedor no permite fallback no autorizado o cobros |
| INV16 | Rollback normal conserva historiales SHA, autor y auditoría |

**Parámetros sin aprobación:** max_event_bytes, allowed_dimension_keys, max_cardinality, sampling_rate, max_events_per_minute, retention_days, delivery_retry_budget, slo_window, slo_target y cost_limit. Si falta alguno necesario: `DISABLED` o `NO_MEDIDO` (no un valor inventado); caídas de collector cuentan pérdidas, jamás una disponibilidad ficticia.

## 5. Matriz de pruebas T01–T14: FUTUROS_NO_EJECUTADOS (0/14)

| ID | Positivo futuro | Negativo o límite futuro |
| --- | --- | --- |
| T01 | Política versionada autoriza ventana dev | Política caducada o sin aprobación no habilita |
| T02 | Clave duplicada con igual carga conserva un efecto | Misma clave/payload distinto rechaza |
| T03 | Causation/correlation vincula SHA y run válidos | Correlación de tenant B no da acceso a A |
| T04 | Rol autorizado consulta agregado propio | Rol revocado o tenant ajeno recibe denegación |
| T05 | Solo dimensiones aprobadas aparecen en log | JWT/cookie/PII o SQL con valor se suprime |
| T06 | Dimensión de baja cardinalidad agrupa bien | Dimensión no allowlist o cardinalidad excesiva falla |
| T07 | Evento de tamaño en límite aprobado pasa | Límite+1 y exceso de tasa se rechazan |
| T08 | N suficiente y ventana generan SLI trazable | Sin N, denominador, ambiente o unidad = NO_MEDIDO |
| T09 | SLO aprobado compara SLI con umbral vigente | Umbral no aprobado no genera PASS ni alerta |
| T10 | Retry acotado preserva idempotencia | Retry infinito/replay vencido bloqueado |
| T11 | Costo estimado tiene moneda y volumen | Costo faltante no inicia servicio de pago |
| T12 | Revocación detiene captura privada | Buffer atrasado de tenant revocado no fuga datos |
| T13 | Revert normal conserva comparabilidad | Force-push/borrado BITACORA se rechaza |
| T14 | Check requerido SUCCESS sobre SHA correcto certifica gate | CANCELLED/SKIPPED/IN_PROGRESS nunca certifica |

**Ejecución actual = 0/14.** Son oráculos futuros, no xUnit, API HTTP, MySQL, Angular, Playwright, SLIs, alertas o providers ejecutados durante este alcance documental R0.

## 6. A0–A5, implementación futura y reversión

- **A0:** V5, requisitos M02.05, DAG y precedentes M01.06/M02.01–04; diff solo este archivo y CHANGELOG_AI.md.
- **A1:** revisión de EVT14, INV16 y T14 positivos, negativos, error y frontera **propuestos**, no pruebas runtime aprobadas.
- **A2:** control estático de JWT/RBAC, revocación, privacidad, idempotencia, cuota y tenant A/B, sin cambio ejecutable.
- **A3:** backend, EF/MySQL, HTTP, Angular/Playwright, exporter, collector y broker **NO_APLICA** al diff R0; una futura implementación pasará R2 y test pertinente real.
- **A4:** un commit dev fast-forward/CAS, validación de Scope Lock y V5 provenance `completed/success` SHA exacto, readbacks GitHub, PLAN/COLA/DAG/BITACORA; CI auxiliares en progreso jamás PASS.
- **A5:** una Task JAVIER diferente a la autora evalúa por sí misma documentos, diffs, SHAs y tests. Sin autoaprobación ni promoción automática a QA/main/PROD.

**Rollback:** `git revert` normal con HEAD revalidado; BITACORA no se reescribe. No se implementó ni se activó ningún webhook, worker, dispatcher, exporter, SDK, métrica, DB, servicio o cuenta.
