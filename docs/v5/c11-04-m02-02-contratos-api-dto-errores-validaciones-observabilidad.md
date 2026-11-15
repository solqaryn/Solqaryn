# SOLQARYN V16 — C11-04.M02.02 contratos de API, DTO, errores y compatibilidad de observabilidad

**ID:** C11-04.M02.02, PLAN_MAESTRO fila 1701. **Owner/autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Rama exclusiva:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo V15:** R0, solo especificación Markdown sin endpoints o tipos compilados.

**Fuente oficial:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO, COLA y DAG. **Resultado exigido:** definir contratos hipotéticos API/DTO, errores, validaciones y compatibilidad para logs, métricas, trazas, alertas, SLO y costos **sin secretos ni PII**, sin activación de servicios.

## 1. Alcance, dependencias y frontera de confianza

`C11-04.M01.01–06` documenta alcance, inventario de código, riesgos, criterios AC01–AC18/N01–N18/RNF01–RNF12 y puertas de salida G01–G16, todavía FUTUROS_NO_EJECUTADOS. `C11-04.M02.01` describió E01–E12, INV01–INV16, REL01–REL10 y T01–T12 (autor PRIMARY DEV 04, SHA `965a99a210868b2ad2663523edc4b93800344ef0`), únicamente contrato, no código ni autorización automática para M02.02. Antes de reclamar se constató PLAN H1701=PENDIENTE/I1701 vacío, COLA G1701/O1701=SI y DAG F1662=VALIDADO/G1662=SI.

**Código real existente, no tocado:** API ASP.NET Core 10 `CorrelationIdMiddleware`, `RequestObservabilityMiddleware`, `ExceptionHandlingMiddleware`, `RequestObservability` y `DbQueryTimingInterceptor`; xUnit inventariada en M01.03. Su existencia no demuestra exporter/collector, dashboard, SLI/SLO, alertas activas, consumo de proveedor ni acceso tenant A/B probado. Los nombres de operaciones y DTO de este documento **no** son rutas API, interfaces Angular, DbSets ni clases implementadas.

**Autoridad:** solo backend verifica JWT, membresía, tenant, permisos y ownership; `X-Correlation-ID` no es identidad. Tenant A/B, rol soporte read-only, revocación y datos sensibles fail-closed. No registrar JWT, cookie, secret, SQL, DSN, request body, paths con IDs o atributos de usuario como métricas. D01–D08 de configuración, retención, cardinalidad, SLO, costos y alertas siguen pendientes. SLO, p95 real, costo y presupuesto de observabilidad = `NO_MEDIDO`.

## 2. Ocho operaciones OP01–OP08 futuras — NO EXPUESTAS

| ID | Operación propuesta | Alcance y protección futura |
| --- | --- | --- |
| OP01 | ConsultarCatalogoSenales | Lectura de nombres/tipos/unidades públicos del catálogo, nunca datos de negocio |
| OP02 | ConsultarResumenHttp | Agregados método y clase HTTP por ventana autorizada |
| OP03 | ConsultarLatenciaAgregada | Histograma/percentiles con unidad, muestra, fuente, ventana |
| OP04 | ConsultarSaludServicio | Estado liveness vs readiness separado sin publicar DB/secretos |
| OP05 | ConsultarSLIPropuesto | SLI con numerador, denominador y estado NO_MEDIDO si no hay datos |
| OP06 | ConsultarRevisionSLO | Propuesta de umbral/version y aprobador; no convertir en certificado PASS |
| OP07 | ConsultarDiagnosticoTenant | Proyección privada tras JWT/RBAC y ownership server-side |
| OP08 | ConsultarEvidenciaSegura | SHA, prueba/run y resultado saneado sin logs completos ni token |

Todas las operaciones serán **solo lectura** hasta permisos de implementación específicos. Una consulta de salud o agregados no puede crear alertas ni modificaciones de proveedor. Operación privada implica autorización server-side por recurso y redacción de resultados; ninguna función de soporte aumenta rol por leer DTO.

## 3. Ocho DTO conceptuales DTO01–DTO08 — NO TIPOS COMPILADOS

| ID | DTO futuro | Campos candidatos permitidos, nunca garantía runtime |
| --- | --- | --- |
| DTO01 | SignalCatalogView | signalName,unit,type,revision,labelClasses; no label individual |
| DTO02 | HttpAggregateView | windowStartUtc,windowEndUtc,methodClass,statusClass,count |
| DTO03 | LatencyHistogramView | unit,windowUtc,sampleCount,buckets,p50,p95 NO_MEDIDO si vacío |
| DTO04 | ServiceHealthView | serviceClass,liveness,readiness,checkedAtUtc; sin URL interna |
| DTO05 | SLIReadView | numerator,denominator,window,sourceSha,measurementStatus |
| DTO06 | SLOProposalView | proposalId,metricRef,thresholdDraft,approved=false |
| DTO07 | TenantDiagnosticView | tenantRefServerValidated,aggregates,readOnly,redacted |
| DTO08 | EvidenceDigestView | sourceSha,testedSha,checkName,completedStatus,redactedRef |

Los campos son una **propuesta de contrato de datos minimizados**; antes de crear DTO revisar compatibilidad de API y versionado real. Nunca exponer logs completos o trace/span con valores arbitrarios. `NO_MEDIDO` es estado distinto de `PASS`; `approved=false` en un SLO propuesto no puede interpretarse como umbral aprobado.

## 4. Doce errores ERR01–ERR12 — PROPUESTAS NO IMPLEMENTADAS

| ID | Código lógico propuesto | Respuesta futura segura |
| --- | --- | --- |
| ERR01 | AUTH_REQUIRED | 401 o respuesta neutral sin dar existencia de empresas |
| ERR02 | ROLE_FORBIDDEN | 403 no permite alterar configuración desde soporte |
| ERR03 | TENANT_SCOPE_DENIED | 403/404 sin filtrar existencia de recurso ajeno |
| ERR04 | CORRELATION_REJECTED | 400 ID corrupto/CRLF o respuesta segura con ID nuevo |
| ERR05 | INVALID_METRIC_WINDOW | 422 ausencia/intervalo inválido sin cálculo falso |
| ERR06 | MISSING_DENOMINATOR | 422 o NO_MEDIDO, no SLO PASS |
| ERR07 | METRIC_CARDINALITY_LIMIT | 422/429 etiquetas fuera allowlist |
| ERR08 | SENSITIVE_FIELD_REJECTED | 400/redacción sin token, PII, SQL o conexión |
| ERR09 | EVIDENCE_SHA_MISMATCH | 409 SHA de check o artefacto diferente |
| ERR10 | CI_CHECK_NOT_SUCCESS | 409 si queued/in_progress/skipped/cancelled/failed |
| ERR11 | PROVIDER_NOT_AUTHORIZED | 403 sin activar exporter, webhook ni gasto |
| ERR12 | READINESS_UNAVAILABLE | 503 diagnóstico seguro, no equivale a liveness FAIL |

Los status 401/403/404/409/422/429/503 son orientativos y deberán reconciliarse con catálogo de error backend existente, sin inventar controllers/handlers. Un 503 de proveedor nunca enmascara acceso denegado 403, y un error genérico no concede diagnóstico a anónimos.

## 5. Dieciséis validaciones VAL01–VAL16 para implementación futura

| ID | Regla comprobable |
| --- | --- |
| VAL01 | JWT y membresía se validan server-side para diagnósticos privados |
| VAL02 | Rol y permiso por acción/recurso se verifican incluso si UI oculta datos |
| VAL03 | Tenant A y B se aíslan en filtrado del backend; no confiar en header/slug |
| VAL04 | Soporte de solo lectura no puede mutar alertas, retención ni fuentes |
| VAL05 | Revalidar rol y sesión revocados antes de cada consulta sensible |
| VAL06 | Usar etiquetas de baja cardinalidad allowlisted; IDs individuales prohibidos |
| VAL07 | Rechazar tokens, cookies, PII, SQL, connection strings y stack en DTO |
| VAL08 | CorrelationId saneado y acotado, no autoridad ni token de acceso |
| VAL09 | Ventanas UTC, unidad y población coherentes para p95 y error rates |
| VAL10 | Denominador cero/muestra ausente => NO_MEDIDO, jamás PASS de SLO |
| VAL11 | Limitar intervalos, tamaño de respuesta y número de series por cuota |
| VAL12 | Consulta de métricas no permite adivinar nombres/rutas internas |
| VAL13 | Separar liveness y readiness; status 200 de una no certifica la otra |
| VAL14 | Collector externo no autorizado => NO_APLICA sin realizar llamada de red |
| VAL15 | Prueba CI requiere head_sha exacto y completed/success; no SKIPPED/CANCELLED |
| VAL16 | Errores preservan correlación mínima y no revelan ID de empresa ajena |

La validación de etiquetas y ventanas deberá aplicar límites elegidos por dueño técnico y métricas con fuente reproducible, sin umbral numérico aprobado en esta entrega. La aplicación futura requiere negative tests que realmente fallen si auth, tenant, medición o redacción se debilitan. No se incluyen nombres de hosts, credenciales o rutas privadas.

## 6. Diez pares T01–T10 positivos/negativos FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo/frontera/error futuro |
| --- | --- | --- |
| T01 | Usuario plataforma autorizado consulta catálogo saneado | payload intenta pedir labels con usuario/PII y se deniega |
| T02 | Contadores HTTP por método/clase permitidos | ruta dinámica/ID único causa rechazo cardinalidad |
| T03 | p95 con 30 muestras, unidad ms y ventana fija | sin muestra o unidad, NO_MEDIDO, no éxito |
| T04 | Administrador A consulta agregados permitidos de A | token B o sesión revocada intenta ver A => 403 |
| T05 | Soporte read-only accede a resumen mínimo | soporte modifica alerta/retención => 403 |
| T06 | Error 5xx devuelve correlación saneada | stack/JWT/SQL/DSN dentro de error => FAIL |
| T07 | Liveness y readiness expuestos separadamente | liveness success con DB caída no implica readiness |
| T08 | SLI con numerador/denominador y fuente rastreable | cero denominador no define SLO PASS |
| T09 | CI check exact SHA complete/success muestra evidencia | skipped/cancelled/in_progress o SHA diferente no PASS |
| T10 | Provider no configurado retorna NO_APLICA seguro | intento activa exporter/scheduler/cargos => FAIL |

**10 pares definidos, 0/10 tests runtime ejecutados.** Cuando se implementen endpoints reales R2, exigir xUnit, HTTP 401/403 positivos/negativos tenant A/B, revocación, redacción y límites de cardinalidad, pruebas MySQL 8.4 efímero `Category=Integration` sólo si se toca persistencia, Playwright si UI cambia; costos de proveedor y SLO **NO_MEDIDO** hasta fuentes/denominadores reales. Un mock/read-only doc no certifica API/HTTP ni producción.

## 7. Compatibilidad, A0–A5 y reversión

Compatibilidad: nuevos campos deben ser aditivos, versionados y explícitos; errores tipados no deben filtrar que recurso B existe; los clientes viejos deben recibir contrato estable o error seguro medido. No aceptar cambiante `HEAD` como prueba del `SOURCE_SHA` si un check no corresponde al árbol probado.

- **A0:** autoridad, dependencias M01/M02.01, OP01–08, DTO01–08, ERR01–12, VAL01–16, T01–10; diff solo este documento y sección CHANGELOG autocontenida.
- **A1:** escenarios positivos, negativos, frontera y errores futuros definidos, sin afirmar ejecución de tests.
- **A2:** seguridad JWT/RBAC tenant A/B, revocación, datos minimizados, SLO no medido y soporte read-only contractuales; ningún enforcement cambia.
- **A3:** reconciliación con middleware y xUnit inventariado; HTTP/MySQL/EF/.NET/Angular/browser/collector **NO_APLICA** a diff Markdown R0, no PASS automático de API.
- **A4:** fast-forward/CAS directo en `dev`, Scope Lock y procedencia V5 `completed/success` del SHA exacto, readback PLAN/COLA/DAG/BITACORA por transición; checks requeridos QUEUED/SKIPPED/CANCELLED/FAIL/IN_PROGRESS no equivalen a PASS.
- **A5:** revisión independiente por otra de las cinco Tasks Javier, distinta de la autora de este cambio; puede hallar defectos y bloquear M03. LISTO_DEV R0 no autoriza QA/main/PROD.

**Rollback:** `git revert` normal del commit documental con HEAD y writer exclusivo comprobados, BITACORA aditiva conservada; no force-push, desactivar tests, crear scheduler, activar exporter, modificar alertas ni cambiar ambiente. D01–D08/SLI/SLO/retención/proveedor continúan pendientes.
