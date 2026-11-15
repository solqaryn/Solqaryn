# SOLQARYN V5 — C05-04.M02.05: eventos, idempotencia, correlación y límites de ligereza UI

**Owner:** PRIMARY DEV 02 — JAVIER_CHATGPT, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA, fila PLAN_MAESTRO 840. **Rama:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK STRICT. **Clase:** diseño documental; EVENTOS NO IMPLEMENTADOS, sin dispatcher ni webhook.

## Motivación y fronteras

La fase C05-04 pretende reducir llamadas HTTP redundantes, tiempo de carga y ruido visual sin perder accesibilidad, fiabilidad ni aislamiento de empresas. M01.01–06 son los contratos antecedentes. M02.05 estaba PENDIENTE libre con COLA G/O=SI y DAG F=VALIDADO/G=SI; las demás M02.02–04 tienen independencia expresada por el DAG, y su estado no autoriza una sucesora M02.06 sin revisiones exigidas.

**No se instala ni se llama** a event dispatcher, cron, cola de mensajes, worker, endpoint de telemetría, RUM productivo, trigger entre Tasks ni motor persistente. BITACORA institucional A:Z sigue siendo la única evidencia histórica de transiciones autorizadas. Ningún evento conceptual sustituye PLAN/COLA/DAG ni activa otra Task fuera de su horario.

## Catálogo conceptual de señales NO_EMITIDAS

| ID | Señal futura | Condición/origen pretendidos | Datos permitidos |
| --- | --- | --- | --- |
| EV01 | UiNavigationMeasured | prueba instrumentada con SHA conocido | ruta plantilla, viewport, red, medición redactada |
| EV02 | UiDuplicateRequestObserved | mismo flujo, contexto y filtros verificados | conteo agregado sin query sensible |
| EV03 | UiTenantContextChanged | backend revalidó contexto A→B | versión opaca, sin PII |
| EV04 | UiFeedbackStateChanged | UI cambia loading/empty/error con causa | estado y correlationRef depurado |
| EV05 | UiBudgetRegressionFound | build o medición reproducible falló | SHA, artefacto de CI, política aplicada |
| EV06 | UiAccessibilityRegressionFound | prueba teclado/ARIA/viewport real falló | criterio, método, resultado, referencia |
| EV07 | UiEvidenceReviewed | Task Javier distinta verificó origen y pruebas | autor, revisor, SHA y decisión A5 real |
| EV08 | UiMeasurementDiscarded | muestra inválida, duplicada o sensible | motivo sin payload rechazado |

Especificación NO_EMITIDA, no evidencia de que las señales ocurran. EV07 no confiere a la UI capacidad de emitir A5_PASS institucional; solo reportaría una revisión ya existente en fuentes autorizadas.

## Envelope conceptual y causalidad

Campos futuros permitidos: schemaVersion, eventType, eventId, idempotencyKey, sourceSha, environment=dev, routeTemplate, opaqueContextVersion, correlationRef, causationRef, occurredAtUtc, sanitizedEvidenceRef y observationStatus. Evitar full URL, request/response body, JWT, cookie, header Authorization, tenantId enviado por navegador, email, nombres, valores de facturas o imágenes privadas. Definir estados NO_MEDIDO / NO_EJECUTADO / VALIDADO_POR_PRUEBA, sin inferir PASS a partir del nombre de una señal.

La clave idempotente se diseñaría con digest estable de tipo + fuente + entorno + contexto tenant autorizado + operación/ruta plantilla + ventana de medición + versión de esquema; los reintentos con misma clave y payload distinto se rechazan. No usar timestamp aleatorio solo como garantía contra duplicados; la huella debe representar la misma causa técnica. La correlationRef conecta trazas de frontend, API y CI únicamente cuando las conexiones estén autorizadas y se hayan eliminado secretos.

## Política de límites FUTURA, pendiente de decisión

LIM01: presupuestos por entorno y actor autorizado; sin configuraciones productivas nuevas. LIM02: backoff acotado con jitter y techo explícito aprobado, sin ciclo infinito. LIM03: 429 y Retry-After respetados sin crear tormenta de solicitudes. LIM04: 401/403, tenant mismatch y revocación no se reintentan como éxitos. LIM05: batch o dedup nunca mezcla A/B ni versiones de sesión. LIM06: cola saturada rechaza o difiere con observabilidad, nunca silenciosamente certifica. LIM07: samples inválidas se marcan NO_MEDIDO. LIM08: auditoría independiente registra fallos y suspensiones, no transforma errores en PASS. LIM09: cifras de cuotas/TTL siguen PENDIENTE_DECISION, no se inventan valores. LIM10: cualquier activación futura requiere microtarea M03 o posterior, permisos y CI causal.

## Matriz de oráculos posteriores — FUTUROS_NO_EJECUTADOS

| Caso | Positivo o negativo | Resultado esperado |
| --- | --- | --- |
| P01 | mismo flujo, misma autorización y clave | evita doble procesamiento |
| P02 | error transitorio autorizado | retry bounded con mismo correlationRef |
| P03 | comparación CI exact-SHA | guarda fuente, ambiente y artefacto reproducible |
| P04 | tenant A→B | ninguna muestra pendiente de A reaparece en B |
| P05 | accesibilidad falla de verdad | evento de diagnóstico sin esconder error |
| N01 | mismo key con payload distinto | conflicto y rechazo |
| N02 | JWT/PII dentro de envelope | rechazo/depuración, sin almacenamiento |
| N03 | actor sin RBAC simula A5 | fail-closed; no transiciones institucionales |
| N04 | HTTP 401/403 o tenant B | sin retry de datos privados |
| N05 | check CANCELLED/SKIPPED | no PASS ni liberación DAG |
| N06 | cuota excedida | backpressure, sin entrega falsa |
| N07 | loop entre Tasks o webhook no aprobado | no se ejecuta |
| N08 | medición sin SHA y baseline | NO_MEDIDO, no mejora inventada |

## Impacto, seguridad, salida

A0: contrato documental, ninguna escritura runtime ni secretos; A1: señales EV01–08, envelope, idempotencia y positivos/negativos futuros; A2: privacidad, RBAC servidor, tenant A/B y límites fail-closed; A3: coherencia con M01/M02 y arquitectura existente, sin prometer CI de runtime; A4: diff solo doc+CHANGELOG, fast-forward/CAS en dev, Scope Lock/V5 exact-SHA, PLAN/COLA/DAG/BITACORA readback. A5 independiente posterior, sin aprobar propia. No hay migración, endpoint, RUM, cron, worker ni integración externa; pruebas runtime NO_EJECUTADAS y NO_APLICA para este cambio de documentación. Rollback mediante git revert normal; sin QA/main/PROD.
