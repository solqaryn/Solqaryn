# SOLQARYN V16 — C11-04.M02.03: persistencia, índices, migraciones y fixtures de observabilidad

**Microtarea:** C11-04.M02.03, PLAN_MAESTRO fila 1702. **Autor:** PRIMARY DEV 05 Javier, TASK_ID \`6adc2e55f47c81919f6374e561687076\`; **repositorio/rama:** \`solqaryn/Solqaryn@dev\`; **PROJECT_SCOPE_LOCK=STRICT**. **Clasificación:** V15 R0 DOCUMENTAL, ninguna modificación de runtime.

**Autoridad operativa:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Google Doc \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`), PLAN_MAESTRO/COLA/DAG. **Resultado requerido:** definir persistencia, índices, migraciones y datos de prueba de observabilidad (logs, métricas, trazas, alertas y costos) sin datos sensibles.

## 1. Alcance, fuentes y barreras

- C11-04.M01.06 documenta gates G01–G16, decisiones D01–D08 no resueltas y reversas futuras; NO autoriza servicios de telemetry o infraestructura de pago.
- C11-04.M02.01 define E01–E12, INV01–INV16, REL01–REL10 y T01–T12 *FUTUROS_NO_EJECUTADOS*. Esta microtarea especializa únicamente la **persistencia conceptual** de esas señales.
- C11-04.M02.02 especifica API/DTO/errores futuros OP01–OP08, DTO01–DTO08, ERR01–ERR12, VAL01–VAL16, oráculos futuros; ninguna ruta nueva.
- El repositorio ya contiene middleware como \`CorrelationIdMiddleware\`, \`RequestObservabilityMiddleware\` y \`ExceptionHandlingMiddleware\`, además de \`RequestObservability\`, \`DbQueryTimingInterceptor\` y \`RequestPerformanceContext\`. **No se altera, instrumenta, activa ni certifica su funcionamiento aquí.**
- Arquitectura: API -> Application -> Infrastructure -> MySQL cuando una implementación lo exija; Domain no recibe dependencias de almacenamiento. El middleware de autorización backend mantiene JWT/RBAC, pertenencia tenant A/B, revocación y auditoría. CorrelationId y TraceId no sustituyen credenciales ni permisos.
- La existencia de clases actuales no confirma la existencia de las tablas conceptuales que siguen. No se crean tablas SQL, DbSets, migrations, índices, collectors, exporters, servicios, dashboards, alertas, páginas Angular, logs nuevos, proveedores Cloud, cuentas, contratos ni costos de operación. Ni datos productivos ni secretos.

## 2. Doce estructuras conceptuales P01–P12 (FUTURAS, NO CREADAS)

| ID | Propuesta | Frontera de pertenencia, retención y privacidad |
| --- | --- | --- |
| P01 | SignalCatalogVersion | catálogo versionado de nombres/tipos/unidades allowlisted y propietario técnico |
| P02 | ObservationSource | identificador de componente y ambiente aprobado; sin host privado o token |
| P03 | CorrelationEnvelope | correlationId sintético, traceId seguro, timestamp UTC y scope minimizado |
| P04 | MetricWindow | métrica/ventana/entorno, numerador, denominador, unidad y muestra verificable |
| P05 | TraceSummary | span técnico y duración; sin payload, query SQL, headers sensibles o cliente |
| P06 | RedactedLogEnvelope | evento, nivel, causa tipada, UTC y hash de correlación con campo redactado |
| P07 | ErrorClassSummary | clase agregada 4xx/5xx y cantidad por ventana, sin ruta con identificadores |
| P08 | DiagnosticViewPolicy | roles autorizados por empresa/plataforma y acciones read-only versionadas |
| P09 | TenantScopedDiagnosticAudit | consulta de diagnóstico, actor servidor, resultado y razon de denegación |
| P10 | ServiceLevelIndicatorSample | SLI con fuente SHA/run, ventana, tamaño de muestra y resultado NO_MEDIDO permitido |
| P11 | AlertPolicyProposal | umbral/ventana/severidad **pendientes de aprobación**, sin contacto externo ni envío |
| P12 | CostBudgetObservation | proveedor/tipo de recurso, unidad e importe agregado autorizado, sin factura real |

**No hay tablas físicas afirmadas.** Cada forma futura necesitará diseño real de PK/FK y, si incluye empresa, propiedad multiempresa y consulta restringida en backend. No usar \`EmpresaId\` recibido del cliente como autoridad; los datos públicos no incluyen identificadores de usuario, IP completa, JWT, conexión, secretos, texto de consultas SQL, emails, teléfonos ni contenido de documentos. Se debe distinguir claramente muestra real y escenario sintético. Sin muestra, denominador o permiso: \`NO_MEDIDO\`, no \`PASS\` ni cero.

## 3. Índices candidatos IDX01–IDX12 (NO CREADOS)

| ID | Clave candidata | Garantía de diseño / riesgo |
| --- | --- | --- |
| IDX01 | (SignalCatalogVersion, SignalName) | catálogo estable, cardinalidad de nombres acotada |
| IDX02 | (Environment, Component, OccurredAtUtc) | localización temporal sin IDs privados |
| IDX03 | (CorrelationHash, OccurredAtUtc) | correlación sintética; no correlator como token de auth |
| IDX04 | (MetricName, WindowStartUtc, Environment) | ventana inequívoca y muestras atribuibles |
| IDX05 | (Component, TraceClass, OccurredAtUtc) | trazas agregadas sin query/endpoint sensible |
| IDX06 | (Level, ErrorClass, OccurredAtUtc) | diagnósticos por clase y no por datos de cliente |
| IDX07 | (ErrorClass, WindowStartUtc, Environment) | ratio con denominador separado y probado |
| IDX08 | (TenantScopeHash, PolicyVersion) | vista privada solo después de autorización server-side |
| IDX09 | (TenantScopeHash, ActionClass, OccurredAtUtc) | auditoría minimizada de acceso a diagnósticos |
| IDX10 | (SliName, Environment, WindowStartUtc) | SLI reproducible con número total de eventos |
| IDX11 | (RuleVersion, RuleState) | borradores de alertas sin activar notificaciones |
| IDX12 | (CostCategory, Environment, WindowStartUtc) | costos agregados, sin tokens de billing |

Los índices son hipótesis a contrastar con planes \`EXPLAIN\`, cargas sintéticas, retención, estimaciones de cardinalidad, autorización y proveedor real en una fase R2. **No ejecutar DDL ni asumir que ya existen.** Prohibir claves/labels por usuario, URL con IDs sensibles, payload, token, trace arbitraria o empresa sin autorización. Un índice no habilita SQL cross-tenant ni un dashboard.

## 4. Ruta de migración futura MIG01–MIG08 (0/8 EJECUTADAS)

| Paso | Prueba y puerta futuras |
| --- | --- |
| MIG01 | inventariar \`AppDbContext\`, middleware, interceptores, migraciones y política de retención reales antes de diseñar tablas |
| MIG02 | aprobar D01–D08 sobre catálogo/redacción, cardinalidad, SLO, roles, retención, exportador, cuota y rollback |
| MIG03 | diseñar entidades/DTO mínimos y relaciones por tenant/rol con autorización backend y consultas de soporte read-only |
| MIG04 | crear migración **aditiva** solamente en MySQL 8.4 efímero de CI y confirmar reversibilidad; sin Aiven real |
| MIG05 | poblar únicamente fixtures sintéticos A/B con errores 4xx/5xx, métricas y ventanas, más queries negativas |
| MIG06 | verificar índices, retención, carga, denominadores, acceso de soporte, redacción de PII y estimación de overhead |
| MIG07 | habilitar collector/exporter/alertas solo si otro scope autorizado y sus pruebas de proveedor/control de costos terminan |
| MIG08 | ensayar restauración/revert de migración sintética y asegurar no regresión ERP, JWT/RBAC/tenant ni auditoría |

**No se autoriza MIG04–MIG08 por este documento.** MySQL 8.4 real de producción o Aiven no se toca, tampoco Render, Vercel, Cloudinary, DNS ni facturación. Para R2, CI debe ejecutar integración real MySQL con \`Category=Integration\`, 0 fallos, no mocks. Un gate de CI "build success" aislado no autoriza un SLO medido ni una restauración real.

## 5. Doce pares de fixtures sintéticos futuros F01–F12 (0/12 EJECUTADOS)

| ID | Positivo futuro | Negativo/fallo obligatorio |
| --- | --- | --- |
| F01 | nombre/métrica tipados y unidad aprobada | label arbitraria/usuario sin whitelist se rechaza |
| F02 | correlationId seguro rastrea un flujo sintético | token/JWT/PII en correlación produce rechazo/redacción |
| F03 | ventana SLI conserva numerador/denominador | muestra vacía/denominador cero => NO_MEDIDO |
| F04 | conteo 5xx con ventana y SHA reales es reproducible | percentil/costo sin run o fuente no se marca PASS |
| F05 | traza privada se lee por rol autorizado | tenant B o rol revocado no lee A |
| F06 | operador de soporte con permiso scoped ve resumen | acceso sin propósito auditado se deniega |
| F07 | log censura SQL, headers y mensajes PII | payload con contraseña/email no persiste |
| F08 | costo sintético agregado conserva unidad y moneda | costo de empresa B no aparece en A |
| F09 | ventana y retención aprobadas eliminan fixture vencido | TTL indefinido o sin política aprobada no activa |
| F10 | error de collector deja sistema principal operativo | falla exportador no evade JWT/RBAC ni rompe transacciones |
| F11 | dashboard futuro presenta "NO_MEDIDO" con ausencia de datos | cero artificial o meta SLO sin decisión se rechaza |
| F12 | reversión sintética conserva auditoría ERP e identidad | despliegue/migración sin rollback ni A5 falla cerrado |

Los casos escritos **no son pruebas ejecutadas**. Cobertura runtime actual \`0/12\`; p95, latencia, disponibilidad, error budget, SLI/SLO, overhead, costos, alertas, privacidad de proveedor y retención **NO_MEDIDO**. Prohibido reportar cifras, pagos, dashboards o alertas reales desde esta tabla. Una prueba futura de soporte debe demostrar denegaciones tenant A/B, revocación y rol con evidencia real; tests de código no equivalen a pruebas de proveedor no conectado.

## 6. Gates A0–A5 y reversa

- **A0:** trazabilidad ID + Standard V5, dependencias M01/M02, pertenencia de archivos, diff solo este Markdown y \`CHANGELOG_AI.md\`, clasificación R0.
- **A1:** verificar P01–P12/IDX01–IDX12/MIG01–MIG08/F01–F12 y cobertura de positivos, negativos, frontera y falla **escritos**, no tests PASS.
- **A2:** revisión estática tenant A/B, JWT/RBAC, revocación, IDOR/BOLA, secrets, PII, redacción, labels acotadas y no interferencia ERP; no se prueba middleware real.
- **A3:** cotejo documental con fuentes y arquitectura; MySQL, EF, .NET runtime, Angular, HTTP, collector, browser, plataformas y proveedores \`NO_APLICA\` por diff Markdown solamente.
- **A4:** publicar dos Markdown por fast-forward/CAS \`dev\`, exigir \`verify-solqaryn-project-scope\` y V5 procedencia \`completed/success\` en SHA exacto, estado PLAN y BITACORA con readback. Auxiliares in_progress no son PASS.
- **A5:** otra Task Javier de ID diferente revisa independientemente documento, SHA, criterios, riesgos, pruebas y controles; A5_PENDIENTE hasta esa revisión, sin autorización QA/main/PROD.

**Rollback:** \`git revert\` normal previo readback de HEAD y concurrencia; preservar BITACORA, no forzar PLAN/DAG/COLA G/O. El contrato no habilita proveedores externos, migración/instalación, servidores, alertas, planes de pago ni datos productivos.
