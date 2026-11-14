# SOLQARYN V5 — C11-04.M01.05: aceptación observable de logs, métricas, trazas y SLO

**Microtarea:** C11-04.M01.05; **PLAN_MAESTRO:** fila 1698; **fase:** M01 preparación documental, no implementación. **Autor:** PRIMARY DEV 05 — JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Repositorio:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14. **Plan único:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.

## Resultado y frontera de esta unidad

Definir criterios **futuros verificables** de aceptación funcional, seguridad, resiliencia y SLI/SLO sobre observabilidad sin datos sensibles. La evidencia mínima futura de C11-04 incluye **SLO medido, dashboard y diagnóstico reproducible**; ninguno queda certificado aquí. M01.01 fija 16 invariantes de no regresión; M01.02 registra actores, superficies y fronteras; M01.03 inventaría código y brechas; M01.04 enumera riesgos, supuestos y decisiones pendientes. Sus estados y autorías permanecen independientes: este documento no aprueba M01.04 ni libera un gate terminal.

**Modificación permitida:** este Markdown y una sección autocontenida en CHANGELOG_AI.md. No modificar API .NET 10, Angular 22.2.1, MySQL/EF, JWT/RBAC/tenant, SQL, middleware, exporter, collector, log externo, dashboard, alerta, servicio de pago, eventos, Task, webhook, secretos, QA/main/PROD, DNS, host ni datos operativos. Todo criterio AC/N/RNF es **FUTURO_NO_EJECUTADO**; no se reportan resultados de pruebas HTTP/MySQL/browser ni valores estadísticos inventados.

**Superficies existentes que deben conservarse:** `backend/src/API/Middleware/CorrelationIdMiddleware.cs`, `backend/src/API/Observability/RequestObservability.cs`, `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs`, `backend/src/API/Filters/MedirRendimientoBusquedaFilter.cs`, `backend/src/API/Program.cs` y `docs/PRODUCTION_READINESS_CONTRACT.md`. Los blobs se inventariaron en M01.01–03; su existencia no acredita collector activo, métricas completas, latencia medida, dashboards, retención ni SLO operativo.

## AC01–AC18 — escenarios positivos FUTUROS_NO_EJECUTADOS

| ID | Escenario de aceptación | Oráculo y evidencia necesaria |
| --- | --- | --- |
| AC01 | Correlación HTTP | Petición válida produce un ID saneado común a respuesta y log de servidor | xUnit/HTTP con ID consistente y sin PII |
| AC02 | Header adversarial | CR/LF, unicode de control y longitud >64 son reemplazados | Pruebas de sanitización con valores centinela |
| AC03 | Conteo HTTP | Counter agregado separa método normalizado y clase HTTP | Conteos reproducibles de requests controlados |
| AC04 | Latencia | Histograma de ms reproduce percentiles sobre muestra conocida | Fixture con unidad, ventana y percentil documentado |
| AC05 | Error 5xx | Un 5xx sintético produce señal sin detalle interno | Captura de respuesta genérica y counter |
| AC06 | Slow request | Duración sobre umbral aprobado queda correlacionada | Prueba de filtro y valor de umbral documentado |
| AC07 | Tracing | Contexto propagado sin payload/secretos en spans | Collector efímero fake y atributos allowlisted |
| AC08 | Falla collector | Caída de exportador no tumba API ni crea reintento infinito | Integración simulada con circuito de degradación |
| AC09 | Diagnóstico por tenant | Administrador A obtiene solo señales autorizadas A | HTTP JWT/RBAC positivo A y negativo B |
| AC10 | Soporte lectura | Rol read-only ve sólo indicadores permitidos | Prueba de acceso y de prohibición de escritura |
| AC11 | Persistencia | Tiempo EF/MySQL agrega duración sin SQL ni parámetros | MySQL efímero y captura de logs depurados |
| AC12 | Health/readiness | Liveness y dependencia DB se reportan por separado | Prueba con DB disponible y no disponible |
| AC13 | SLI | Numerador, denominador y ventana calculan la tasa | Dataset reproducible y estado NO_MEDIDO |
| AC14 | SLO | Solo umbral aprobado + muestra suficiente habilitan evaluación | Decisión registrada, fuente, cálculo y cobertura |
| AC15 | Dashboard | Vistas futuras exponen fuente y frescura, sin información privada | Datos reconciliados, captura y permisos verificados |
| AC16 | Alerta segura | Frecuencia, deduplicación y presupuesto limitan ruido | Prueba de tormenta con cuota y confirmación humana |
| AC17 | Retención y costo | TTL, acceso y estimación con cuotas aprobadas | Política y proyección; ningún cobro automático |
| AC18 | Rollback | Desactivación no altera rutas comerciales ni políticas tenant | Revert controlado, diff, post-checks y readback |

## N01–N18 — rechazos FUTUROS_NO_EJECUTADOS

| ID | Fallo o comportamiento prohibido |
| --- | --- |
| N01 | JWT, cookies, token o API key aparecen en logs, span, tags o respuesta |
| N02 | CorrelationId con CR/LF o PII termina en logger sin reemplazo |
| N03 | ID de usuario/empresa, path con ID o query privada se usa como label sin límite |
| N04 | Body HTTP, excepción SQL o cadena de conexión se exporta |
| N05 | CorrelationId o TraceId suplanta autorización o identidad del tenant |
| N06 | Empresa B recibe diagnósticos, logs o agregados de A |
| N07 | Soporte read-only edita retención, alerta, cuotas o conexión |
| N08 | Usuario anónimo ve métricas internas o dashboard de otro actor |
| N09 | Fallos de collector bloquean API o causan bucles y cobros |
| N10 | Se declara SLO PASS solo por build, mock, Markdown o cero medidas |
| N11 | Percentil calculado sin unidad, muestra, intervalo o denominador |
| N12 | Health 200 se interpreta como DB readiness y autorización PROD |
| N13 | Error revela stack, SQL, documento privado o dato de otra empresa |
| N14 | Consulta EF registra parámetros sensibles o mezcla tenant A/B |
| N15 | Una alerta ejecuta otra Task, worker, dispatcher o despliegue |
| N16 | Tormenta de alertas crea notificaciones o costos ilimitados |
| N17 | Exporter/retención/residencia se activa sin permiso y auditoría |
| N18 | Check required skipped/cancelled/queued/in_progress/fail se registra como PASS |

## RNF01–RNF12 — requisitos no funcionales a medir

| ID | Umbral o condición que deberá acreditarse |
| --- | --- |
| RNF01 | Cada señal documenta ambiente, SHA/versión y unidad sin labels individuales |
| RNF02 | Se suprimen secretos y PII por allowlist y pruebas con centinelas |
| RNF03 | Correlación tiene longitud limitada y nunca implica autoridad |
| RNF04 | Overhead de observabilidad tiene presupuesto aprobado y medición futura |
| RNF05 | SLI/SLO informa ventana, muestra, error budget y NO_MEDIDO cuando corresponda |
| RNF06 | Umbrales y p95 requieren baseline real y aprobación, no números ficticios |
| RNF07 | Pruebas JWT/RBAC tenant A/B y soporte read-only son negativas y positivas |
| RNF08 | Cardinalidad, tamaño y memoria tienen límites medidos por ambiente |
| RNF09 | Retención, borrado, control de acceso y residencia se aprueban antes de exportar |
| RNF10 | Alertas tienen debounce, cuota, deduplicación y acción humana |
| RNF11 | Dashboard futuro será ligero, accesible, responsive y probado con Playwright |
| RNF12 | Todo cambio runtime exige CI causal exact-SHA, Scope Lock, rollback y A5 ajeno |

**Umbrales pendientes:** ninguna cifra de p95, error budget, tamaño de muestra, tasa de ingesta, retención, RPO/RTO o gasto se declara aprobada. La fuente de datos, política, límites de cardinalidad/retención, permisos, ventanas y responsables requieren las decisiones D01–D08 de M01.04 y posteriores pruebas causales.

## Matriz de validación futura y rollback

| Superficie | Prueba causal cuando exista implementación | Bloqueo de autorización |
| --- | --- | --- |
| .NET 10 API/CorrelationId/errores | xUnit y HTTP con payloads centinela; build GitHub Actions exact-SHA | Sin PASS por documentación |
| JWT/RBAC, tenant A/B, soporte lectura | Tests 401/403/404, ataques cross-tenant, logging sin PII | UI y header no son autoridad |
| Métricas/trazas/exportación | Collector simulado en ambiente efímero, histograma con muestra y tags allowlisted | Sin proveedor/servicio de pago ni exporter nuevo |
| MySQL/EF | MySQL efímero CI, consultas parametrizadas, logs depurados | No tocar datos reales ni migraciones |
| SLO/dashboard/alertas | Dataset reproducible, dueño, baseline, cuota, autorización, Playwright si hay UI | NO_MEDIDO no es PASS; sin dispatcher/eventos |
| Reversión | Git revert normal y readback del estado de API, señales y pertenencias | Sin force-push ni QA/main/PROD |

## Auditorías A0–A5 y limitaciones

- **A0:** fuente V5 única, Skill/HEAD/Scope Lock, PLAN fila 1698 reclamado mediante CAS, COLA G/O=SI, DAG F=VALIDADO/G=SI fila 1659 y archivos excluidos.
- **A1:** especificación verificable de AC01–AC18, N01–N18, RNF01–RNF12 como criterios futuros, no tests ejecutados.
- **A2:** revisión documental de PII/secretos, cardinalidad, JWT/RBAC, tenant A/B, soporte read-only y errores fail-closed; no modifica enforcement.
- **A3:** coherencia con superficies existentes y contratos M01.01–04; suites backend/frontend, HTTP, MySQL/Playwright NO_APLICA al diff exclusivamente Markdown.
- **A4:** commit serial fast-forward a dev con CAS de HEAD, diff de exactamente dos Markdown, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en completed/success para SHA exacto, readback PLAN/COLA/BITACORA.
- **A5:** otra Task Javier de TASK_ID distinto debe revisar antes de cualquier promoción. La documentación M01 no libera M01.06, M06.06 ni runtime/QA/main/PROD.

**Reversión:** revert aditivo por Git del documento y CHANGELOG sin reescribir historial. No se requirió modificar una base, cuenta, proveedor, collector ni automatización.
