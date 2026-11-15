# SOLQARYN V16 — C11-04.M02.01 entidades, ownership, relaciones e invariantes de observabilidad

**Microtarea:** `C11-04.M02.01`, PLAN_MAESTRO fila 1700. **Autor/owner:** PRIMARY DEV 04 JAVIER (`6aeacb7820ac8191b7d768a6f33dfa09`). **Fuente exclusiva:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet institucional PLAN/COLA/DAG. Repositorio `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo V15:** R0 DOCUMENTAL sin runtime.

## 1. Propósito, antecedentes y frontera

**Resultado exigido:** definir entidades conceptuales, ownership, relaciones y reglas de invariantes para logs, métricas, trazas, alertas y costos **sin datos sensibles**, preparar pruebas futuras y preservar compatibilidad del sistema ERP. **No se implementan** clases, API, servicios, middleware, collector, exporter, alertas reales, dashboard, CDN, workers, webhooks, infraestructura, compras ni migraciones.

Precedentes `C11-04.M01.01–06`: M01.03 identificó once fuentes (incluye `backend/src/API/Middleware/CorrelationIdMiddleware.cs`, `RequestObservabilityMiddleware.cs`, `ExceptionHandlingMiddleware.cs`, `backend/src/API/Observability/RequestObservability.cs`, `DbQueryTimingInterceptor.cs`, `RequestPerformanceContext.cs` y pruebas xUnit); M01.05 redactó AC01–AC18/N01–N18/RNF01–RNF12 **FUTUROS_NO_EJECUTADOS**; M01.06 conservó G01–G16, reversión y decisiones D01–D08. Sus referencias a fuentes no acreditan exportador, SLO, dashboard ni proveedor live. En claim se verificó PLAN H1700=PENDIENTE/I1700 vacío, COLA G1700/O1700=SI y DAG F1661=VALIDADO/G1661=SI.

**Arquitectura:** Domain <- Application <- Infrastructure, API composición en ASP.NET Core 10. JWT/RBAC/tenant se comprueban server-side; `X-Correlation-ID` y TraceId sólo son diagnósticos, nunca credenciales. La existencia de `Meter`, contador y pruebas en Git no significa que tengan cobertura operativa o se ejecutaron aquí. EF Core/MySQL 8.4, Angular 22.2.1 y proveedores mantienen configuración actual. Sin cambios en `Program.cs`, middleware, pruebas, configuración, `appsettings`, bases, servidores, QA/main/PROD o secretos.

**Decisiones pendientes D01–D08:** catálogo y redacción de campos, cardinalidad/labels, umbral y ventana SLI/SLO, roles de dashboard/soporte, retención/residencia, exportador/proveedor y costo, alertas/on-call/cuotas, ensayo de rollback. No inferir SLA/SLO aprobado ni cargo contratado. Valores de latencia, throughput, error budget, p95, retención, overhead y costo = `NO_MEDIDO` cuando no hay denominador/ambiente/fuente acreditados.

## 2. Entidades E01–E12 propuestas — SOLO CONCEPTUALES

| ID | Entidad conceptual | Dueño y restricción |
| --- | --- | --- |
| E01 | ObservedRequest | API/componente servido: request sintético, método normalizado, clase HTTP; no body/query ni JWT |
| E02 | CorrelationContext | ID aleatorio/saneado y TraceId, exclusivamente correlación; nunca identidad o permiso |
| E03 | TelemetrySignalDefinition | catálogo de nombre, tipo, unidad, versión y etiquetas allowlisted |
| E04 | MetricSample | valor numérico y ventana de medición, sin labels por usuario o IDs privados |
| E05 | TraceSpanSummary | duración y estado técnico; atributos redactados y sin SQL/secretos |
| E06 | LogEnvelope | nivel, evento, tiempo UTC, correlationId y error tipado minimizado |
| E07 | TenantScopeProof | servidor comprueba claims/membresía/rol antes de toda lectura privada |
| E08 | AuthorizedDiagnosticView | proyección solo lectura para plataforma/empresa/soporte según RBAC backend |
| E09 | ServiceLevelIndicator | SLI tipado con numerador, denominador, ventana, ambiente y fuente |
| E10 | ServiceLevelObjectiveProposal | umbral futuro pendiente de aprobación; no evaluar con falta de muestra |
| E11 | AlertRuleProposal | regla futura deduplicada/cuota/oncall; ningún envío o Task ejecutable |
| E12 | EvidenceRecord | SHA, prueba, UTC, responsable, limitación y rollback sin payload sensible |

No añadir nuevas tablas, clases, DTO ni tareas. El dato de usuario o empresa no debe convertirse en etiqueta libre, recurso global o serie de alta cardinalidad. Entidades de muestra no autorizan operaciones de lectura de registros reales.

## 3. Invariantes INV01–INV16 — ORÁCULOS FUTUROS, NO PROBADOS

| ID | Invariante para una futura implementación |
| --- | --- |
| INV01 | CorrelationId saneado (<=64 caracteres según inventario), CR/LF y formato inválido reemplazados |
| INV02 | CorrelationId/TraceId nunca autentican ni autorizan empresa, sesión, recurso o rol |
| INV03 | Las etiquetas de métricas usan catálogo de baja cardinalidad; nunca ID de usuario/tenant libre |
| INV04 | Logs, traces y métricas omiten token, cookie, DSN, SQL, payload, email y teléfono |
| INV05 | Empresa A/B: lectura de diagnóstico privado requiere JWT, tenant y recurso realmente autorizado |
| INV06 | Soporte read-only no edita reglas, cuotas, retención, alertas ni configuración |
| INV07 | Excepción HTTP siempre resulta en respuesta segura; no filtrar stack interno al cliente |
| INV08 | Un fallo de collector/exporter futuro no bloquea negocio ni activa retry infinito |
| INV09 | Los contadores no afirman SLO PASS con muestra nula, ventana desconocida o denominador cero |
| INV10 | Percentiles exigen unidad, intervalo, población y cálculo reproducibles |
| INV11 | Medidas de DB expresan duración agregada; jamás SQL o parámetros como atributo público |
| INV12 | Sin política y autorización no hay retención/exportación a proveedor externo |
| INV13 | Alertas futuras requieren cuota, deduplicación y aceptación humana; nunca disparan otra Task |
| INV14 | Identidad del actor, tenant y recurso se confirman server-side; headers cliente no sustituyen claims |
| INV15 | Cada cambio de observabilidad debe conservar JWT/RBAC, rate-limits, headers y aislamiento existentes |
| INV16 | Rollback y auditoría preservan SHA, evidencia y bitácora aditiva, sin borrar historia |

Aplicación de controles en la futura capa backend, no por UI ni por este documento. Un ID legado NULL, header arbitrario o tenant del navegador no otorga permisos. El acceso a soporte y alertas se define por roles reales separados del autor de cambios.

## 4. Relaciones REL01–REL10 orientativas — NO MODELOS NUEVOS

| ID | Relación conceptual | Control obligatorio |
| --- | --- | --- |
| REL01 | Request 1:1 CorrelationContext | correlación acotada al request, no enlazar usuarios en métricas |
| REL02 | Request 1:N MetricSample | solo agregados/labels allowlisted |
| REL03 | Request 1:N TraceSpanSummary | mínima información operativa, sin datos de negocio |
| REL04 | CorrelationContext 1:N LogEnvelope | IDs saneados y sin payload |
| REL05 | TelemetrySignalDefinition 1:N MetricSample | schema fijo, cardinalidad acotada |
| REL06 | TenantScopeProof 1:N AuthorizedDiagnosticView | vista autorizada, sin datos ajenos |
| REL07 | SLI 1:N MetricSample | fuente y ventana verificables |
| REL08 | SLOProposal 1:N SLI | no se considera aprobado por relación declarada |
| REL09 | AlertRuleProposal 1:N SLI | ningún sink/worker habilitado |
| REL10 | EvidenceRecord N:1 ReleaseSha | registro trazable de pruebas verdaderas |

Cardinalidades no son PK/FK, esquema SQL ni asociación EF. Cualquier futuro almacenaje deberá revisar modelos actuales, política de retención y pruebas MySQL 8.4 efímero con `Category=Integration`; un mock no prueba integración. La lectura de datos de tenant B desde A debe fallar en API aun si el frontend oculta la vista.

## 5. Doce escenarios positivos/negativos T01–T12 — FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo / fallo que detecta defectos |
| --- | --- | --- |
| T01 | CorrelationId seguro propagado por API | CRLF o >64 reemplazado sin inyección |
| T02 | Contador HTTP con método y clase permitidos | path/usuario dinámico como label rechazado |
| T03 | Error HTTP 5xx con correlación y sin payload | stack, SQL o JWT filtrado causa FAIL |
| T04 | Empresa A consulta vista diagnóstica autorizada | token B/anónimo/rol revocado sobre A da 403 |
| T05 | Soporte lectura ve agregado permitido | soporte puede mutar alerta/retención da FAIL |
| T06 | SLI con ventana y denominador >0 es reproducible | cero muestras o ventana ausente NO_MEDIDO, no PASS |
| T07 | Histograma con unidad y muestra conocida | p95 sin población/intervalo NO_MEDIDO |
| T08 | EF timing agrega ms sin parámetros | SQL o connection string en log/spans falla |
| T09 | Collector caído conserva respuesta comercial | retry infinito/caída de API/costo libre falla |
| T10 | Alerta futura con cuota y dedup | tormenta de alertas o ejecución de Tasks falla |
| T11 | Dashboard futuro con RBAC y UX accesible | UI oculta dato pero API BOLA permite B falla |
| T12 | Revert conserva código, tenant y evidencia | rollback borra bitácora o reduce JWT/RBAC falla |

**0/12 pruebas de integración o runtime ejecutadas en esta microtarea.** Cuando exista M03 real, deberá incluir xUnit con positivos, negativos, fronteras, errores, JWT/RBAC tenant A/B y data redaction, HTTP cuando API cambie, MySQL 8.4 efímero sólo si toca EF/persistencia, Playwright si introduce UI. No declarar pruebas de proveedores, SLO ni dashboard por redactar estas expectativas.

## 6. Impacto, aceptación A0–A5 y rollback

- **A0:** alcance `C11-04.M02.01`, autoridad V5, G01–G16/M01, E01–E12, INV01–INV16, REL01–REL10, T01–T12; único cambio de archivo documental + encabezado con viñetas contiguas en `CHANGELOG_AI.md`.
- **A1:** revisión estructural de 12 entidades, 16 invariantes, 10 relaciones y 12 pares futuros; **0 ejecutadas**, sin PASS de runtime atribuido.
- **A2:** contrato fail-closed para secretos/PII/SQL, tenant A/B, roles de lectura, revocación, cardinalidad, costos y logs minimizados; controles de ejecución intactos.
- **A3:** reconciliación con inventario Git M01.03; .NET/Angular/MySQL/HTTP/Playwright/exporter/proveedor real `NO_APLICA` al diff R0, no significa certificación de observabilidad.
- **A4:** publicar directamente en dev con commit fast-forward y expected HEAD; `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA exacto; PLAN/COLA/DAG/BITACORA leídos después. Cualquier required FAIL, CANCELLED o IN_PROGRESS impide su PASS.
- **A5:** revisión independiente por otra Task Javier de ID distinto, con SHA, blobs y oráculos; LISTO_DEV documental no habilita M03/QA/main/PROD ni configuración productiva.

**Rollback del documento:** `git revert` normal tras revalidar HEAD y ausencia de competidor, sin force-push ni borrar BITACORA. No existe rollback de proveedor/alertas porque nada se activó. Las decisiones D01–D08 y mediciones SLI/SLO reales siguen pendientes.
