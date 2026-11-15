# SOLQARYN V16 — C13-03.M02.05: eventos, idempotencia, correlación y límites de rendimiento

**Microtarea:** C13-03.M02.05, PLAN_MAESTRO fila 1956. **Autor:** PRIMARY DEV 06 JAVIER, Task 6af16bbad26881918c283abe06e1f32a. **Riesgo:** R0 documental, PROJECT_SCOPE_LOCK=STRICT, rama dev. Este documento describe contratos futuros y no implementa ni ejecuta eventos, publicadores, consumidores, colas, webhooks, workers, trazas, métricas, cachés ni persistencia.

**Autoridad y criterio:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA); PLAN, COLA y DAG vigentes. Salida atómica: definir eventos, idempotencia, correlación y límites para carga inicial, navegación, imágenes, tablas, datos y móvil, preservando presupuestos y mediciones repetibles cuando se autoricen. Cualquier implementación de telemetría/CI/seguridad se reclasificará R2.

## 1. Fuentes, precedentes y condiciones no resueltas

- M01.06 define G01–G16, RB01–RB08 y oráculos T01–T10 aún no ejecutados. M02.01 propone E01–E14, REL01–REL12 e INV01–INV18 de propiedad de evidencias; M02.02 describe OP01–OP08, DTO01–DTO08, errores seguros y validaciones; M02.03 propone persistencia e índices hipotéticos; M02.04 fija OPT01–OPT14, VAL01–VAL16, FAL01–FAL08 y diez pares T01–T10 futuros.
- Son contratos conceptuales, no clases, rutas API, tablas, migraciones, flags o eventos activos. Los requisitos de pruebas reales aplicarán a microtareas ejecutables posteriores, no a este artefacto R0.
- El conflicto de presupuesto de bundle **650–750 KB frente a 1–2 MiB** permanece **SIN_RESOLVER**. Nadie debe seleccionar automáticamente uno como límite aprobado. LCP, INP, CLS, TTFB, p95, coste y consumo energético = **NO_MEDIDO**; no hay muestra ni comparativo atribuible a este documento.
- La autoridad de JWT/RBAC, revocación, tenant A/B y propiedad de recurso siempre reside en ASP.NET Core 10. Angular 22.2.1 no autoriza acceder a datos. MySQL 8.4 y EF Core 10 (proveedor Oracle) no reciben modificaciones. Datos sensibles no se usan como etiquetas.

## 2. Catálogo de eventos FUTUROS EVT01–EVT12

| ID | Evento conceptual | Condición, autoridad y carga mínima no sensible |
| --- | --- | --- |
| EVT01 | MeasurementPlanProposed | Versión, SHA, responsable y estado PENDIENTE; no implica presupuesto aprobado |
| EVT02 | MeasurementPlanApproved | Decisión explícita con unidad, perfil, firma lógica y validez; rechazar conflicto |
| EVT03 | SyntheticRunRequested | Sólo CI/dev permitido, perfil sintético, cuota y run id; no iniciar proveedor externo |
| EVT04 | SyntheticRunStarted | SHA, toolchain, dispositivo, red y entorno; marcas UTC, sin usuario |
| EVT05 | SyntheticRunCompleted | Conteo de muestras, método, métricas válidas y artefacto; no emitir PASS sin datos |
| EVT06 | SyntheticRunRejected | Motivo tipado, revisión, replay y permiso faltante; nunca volcar JWT o cookies |
| EVT07 | PublicResourceBudgetObserved | Ruta pública catalogada, recurso y unidad; no incluir URL firmada ni PII |
| EVT08 | PrivateRouteMeasurementSuppressed | Solo código de supresión y contexto agregado; no grabar ruta privada |
| EVT09 | PerformanceRegressionSuspected | Comparación entre SHA equivalentes, umbral aprobado, muestra y confianza |
| EVT10 | PerformanceRegressionConfirmed | Confirmación independiente, prueba de regresión y rollback aprobado |
| EVT11 | TenantCacheBoundaryViolated | Evento de seguridad aislado sin payload ajeno, revisión R2 y bloqueo |
| EVT12 | MeasurementPlanRevoked | Revocación de plan/capacidad, invalidación de configuración futura y evidencia |

Todos los nombres son **propuestas** y no mensajes emitidos. No hay contrato de broker, topic, dispatcher, DTO ejecutable ni integración de terceros aprobada. Nada de esta lista crea autoridad para registrar datos en producción.

## 3. Envolvente de evento y correlación futura

La envolvente conceptual, si se implementa más adelante, debería incluir: event_type/version, producer_component, occurred_at_utc, source_commit_sha, correlation_id aleatorio no reversible, causation_id, idempotency_key, synthetic_run_id verificable, classification, decision_status, approved_profile_ref y payload_schema_version. Su esquema se valida antes de usarlo, nunca se confía en claims del cliente. Campos empresariales, tokens, correos, IP, URL privadas, cookies y etiquetas de usuario deben estar ausentes; tenant privado sólo puede permanecer en un contexto autorizado y no exportado.

La correlación requiere distinguir **causa** de mera coincidencia temporal: un run nuevo no hereda PASS de otro SHA, un replay no crea una medición nueva y una muestra con perfil distinto no integra el mismo agregado. Idempotency key propuesta: hash no reversible de (event_type, schema_version, authorized_run_id, source_sha, profile_ref, event_occurrence_key); no usar secretos como insumo público ni usar identificadores de usuario. El futuro receptor valida autenticidad, TTL y pertenencia, mantiene deduplicación transaccional y rechaza claves vacías o reusadas con payload distinto.

## 4. Invariantes de idempotencia, seguridad y operación INV01–INV14

| ID | Regla futura verificable |
| --- | --- |
| INV01 | Misma clave y mismo payload = un efecto lógico, respuesta repetible |
| INV02 | Misma clave y payload distinto = rechazo con evidencia, nunca sobrescritura silenciosa |
| INV03 | Mensaje fuera de TTL/revocado = rechazo, no replay privilegiado |
| INV04 | Orden no garantizado: usar causalidad y versión, no timestamps como autoridad |
| INV05 | Correlation ID opaco; JWT, correo y tenant A/B nunca aparecen en etiquetas externas |
| INV06 | Recursos privados no comparten caché, medición ni resultado entre empresas A/B |
| INV07 | Emisor no confiable no concede permisos, presupuesto ni aceptación de rendimiento |
| INV08 | Límite de tamaño y tasa previo a deserializar; rechazo fail-closed de payload excesivo |
| INV09 | Backpressure acotado, sin reintentos infinitos o pagos automáticos |
| INV10 | Run cancelado/fallido/omiso no produce métrica PASS ni cierre A4 |
| INV11 | Muestra con perfil, red o versión no equivalente no se compara como regresión |
| INV12 | Revocación invalida futuras suscripciones y registros privados, preservando auditoría |
| INV13 | Instrumentación deshabilitada por defecto; datos inexistentes = NO_MEDIDO |
| INV14 | Rollback normal y cambio de SHA conservan historia, autores y evidencia de CI |

**Cuotas propuestas, no aplicadas:** requerir aprobación explícita de max_event_bytes, max_events_per_minute, retention_days, sample_count_min y retry_budget. Si cualquiera falta, se aplica DISABLED/NO_MEDIDO, no un valor inventado. Cualquier integración futura Cloudinary/SMTP/otro proveedor conserva aislamiento, consentimiento y costos no automáticos.

## 5. Matriz de pruebas futuras T01–T12 (0/12 ejecutadas)

| ID | Positivo futuro | Negativo, frontera o fallo futuro |
| --- | --- | --- |
| T01 | Plan aprobado/versionado permite emitir solicitud sintética | Presupuestos 650–750 KB vs 1–2 MiB dejan decisión PENDIENTE |
| T02 | Clave repetida con igual payload no duplica run | Igual clave con payload modificado rechaza |
| T03 | Correlation/cause IDs vinculan etapas del mismo SHA | SHA y perfil distintos no heredan PASS |
| T04 | Revocación limpia capacidades futuras | Mensaje vencido o sesión revocada no ejecuta |
| T05 | Tenant A solo observa recursos propios autorizados | Tenant B intenta reutilizar clave A y se rechaza |
| T06 | Ruta pública instrumentada produce evidencia sin PII | Ruta privada, JWT o cookie en etiqueta se suprime |
| T07 | Muestra suficiente con unidad/perfil produce estadística auditable | Sin N, perfil o unidad la métrica queda NO_MEDIDO |
| T08 | Evento válido pequeño cumple cuota autorizada | Payload en el límite+1 o exceso de tasa falla cerrado |
| T09 | Fallo transitorio usa retry acotado e idempotente | Backoff infinito o replay sin cuota se rechaza |
| T10 | Regresión confirmada conserva diff, SHA y rollback | Alerta por timestamp o métrica vieja no certifica |
| T11 | Accesibilidad y móvil permanecen funcionales | Medición bloquea teclado o provoca CLS y falla |
| T12 | Cancelación/rollback conserva registro auditable | CANCELLED/SKIPPED/IN_PROGRESS no se presenta como PASS |

Los T01–T12 son **oráculos FUTUROS_NO_EJECUTADOS (0/12)**; no hubo test de API, xUnit, Angular, MySQL, Playwright, proveedor ni benchmark en esta microtarea. Los números de LCP/INP/CLS, budget y costos son NO_MEDIDO.

## 6. Cierre A0–A5, adopción futura y rollback

- **A0:** autoridad V5, dependencias y seis documentos precedentes; diferencia limitada a este Markdown y sección autónoma de CHANGELOG_AI.md.
- **A1:** inspección de las doce parejas P/N y catorce invariantes como **especificación**, no como tests ejecutados; carencia de implementación señalada.
- **A2:** fallos seguros de RBAC/JWT, revocación, tenant A/B, privacidad, replay y cuotas; ningún cambio efectivo de seguridad.
- **A3:** backend .NET, MySQL, Angular, HTTP, Playwright, red externa y mensajería **NO_APLICA** al diff documental R0; serán obligatorios cuando exista código.
- **A4:** commit individual en dev por fast-forward/CAS, Scope Lock y procedencia V5 exact-SHA completed/success; lectura de GitHub y PLAN/COLA/DAG/BITACORA; checks auxiliares en curso no cuentan como PASS.
- **A5:** revisión de otra Task Javier, distinta del autor, con derecho a rechazar y dejar evidencia de defecto. LISTO_DEV no es aprobación para QA/main/PROD.

**Rollback:** git revert aditivo normal contra HEAD revalidado, sin reescribir historia ni BITACORA. Próximo desarrollo de eventos, telemetría, caché, JWT o MySQL requiere nueva autorización de alcance, clasificación R2, pruebas ejecutables reales y aceptación separada. Ningún webhook, worker ni event dispatcher fue implementado.
