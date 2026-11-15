# SOLQARYN V16 — C11-04.M02.04: configuración tipada, ámbitos y flags seguros de observabilidad

**Microtarea:** C11-04.M02.04, PLAN_MAESTRO fila 1703, autor PRIMARY DEV 05 Javier, TASK_ID \`6adc2e55f47c81919f6374e561687076\`. **Rama:** \`solqaryn/Solqaryn@dev\`; **PROJECT_SCOPE_LOCK=STRICT**. **Riesgo V15 R0:** solamente documento contractual, sin cambios ejecutables.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`), PLAN_MAESTRO, COLA y DAG. **Resultado:** definir configuración tipada, ámbitos y feature flags seguros para logs, métricas, trazas, alertas, costos y diagnóstico sin PII.

## 1. Fuentes, precedentes y exclusiones

- C11-04.M01.06: gates G01–G16 y decisiones D01–D08 pendientes; no autoriza activación de proveedores, alertas on-call, costos ni deploy.
- C11-04.M02.01: conceptos E01–E12, INV01–INV16 y REL01–REL10; ningún modelo/DB creado. M02.02: OP01–OP08, DTO01–DTO08, ERR01–ERR12 y VAL01–VAL16 futuros, sin API. M02.03: persistencia P01–P12, índices IDX01–IDX12 y migraciones MIG01–MIG08 futuros, sin tocar MySQL.
- Existe código de middleware en el API: \`CorrelationIdMiddleware\`, \`RequestObservabilityMiddleware\`, \`ExceptionHandlingMiddleware\`, \`RequestObservability\`, \`DbQueryTimingInterceptor\`, \`RequestPerformanceContext\`. El presente documento **no implementa** ni cambia esos archivos, sus options, proyectos .NET 10, \`appsettings\`, frontend Angular 22.2.1, servicios de telemetry, exporters, collectors, dashboards, tablas, migraciones, reglas de firewall, secretos, DNS o cuentas de pago.
- Toda vista privada de observabilidad exige JWT, membresía, rol/permiso y tenant verificados en backend. \`CorrelationId\` es diagnóstico, jamás credencial de autorización. Una opción, request, host, navegador, header o query string no puede elevar permisos ni revelar PII.
- Presupuesto SLO, overhead, latencia p95, retención legal, cardinalidad, costo y SLA **NO_MEDIDO o PENDIENTE** sin fuente/aprobación. Ningún umbral de esta especificación se aplica en runtime.

## 2. Jerarquía de opciones y scope

1. **Invariantes de seguridad no sobrescribibles:** no capturar JWT, cookies, DSN, SQL literal, bodies, PII, identificadores privados ni atributos de cardinalidad abierta. Autorización y revocación se verifican siempre en servidor.
2. **Baseline de plataforma autorizado:** ámbitos de entorno (dev) y componente documentados; deshabilita exportación, alertas, costes externos y lectura privada por defecto.
3. **Política de plataforma con rol administrador autorizado:** puede restringir instrumentación o cuotas, nunca permitir bypass de tenant/JWT/RBAC ni acceso a datos de otra empresa.
4. **Política de empresa:** solo por EmpresaId de sesión verificada, con lectura diagnóstica restringida, nunca fallback a otro tenant ni actor; un soporte solo-lectura tampoco puede elevar funciones.
5. **Política de componente/ruta:** reduce detalle/retención/cardinalidad respecto de la capa anterior. Cliente/UI nunca tiene autoridad para activar una opción.
6. **Reglas ante conflicto:** usar la política más restrictiva; versión obsoleta, ámbito inexistente, opción desconocida, error de parseo, miembro revocado o ambiente no aprobado => fail closed. No activar provider con valor ausente.

## 3. Opciones conceptuales OPT01–OPT20 (FUTURAS, NO CONFIGURADAS)

| ID | Tipo sugerido | Valor seguro / límite contractual |
| --- | --- | --- |
| OPT01 | ObservabilityExportEnabled: bool | OFF; sin collector ni permisos de proveedor |
| OPT02 | ExternalTracingEnabled: bool | OFF; no envíos remotos ni datos privados |
| OPT03 | MetricsExportEnabled: bool | OFF; no costeo ni tráfico externo |
| OPT04 | AutoAlertsEnabled: bool | OFF; sin notificaciones ni on-call |
| OPT05 | SupportDiagnosticsEnabled: bool | OFF; acceso read-only solo con autorización |
| OPT06 | TenantDiagnosticsEnabled: bool | OFF; tenant A/B validado server-side |
| OPT07 | RequestBodyCaptureEnabled: bool | OFF irrevocable para PII, JWT y secretos |
| OPT08 | SqlLiteralCaptureEnabled: bool | OFF irrevocable; nunca DSN/SQL en telemetría |
| OPT09 | SensitiveHeadersCaptureEnabled: bool | OFF irrevocable; cookies/tokens siempre censurados |
| OPT10 | HighCardinalityLabelsEnabled: bool | OFF; usuario/ID/path/token prohibidos |
| OPT11 | EnvironmentCode: enum | dev y valores aprobados, sin descubrir datos prod |
| OPT12 | SamplingRate: decimal | rango 0..1 y política aprobada, no tasa inventada |
| OPT13 | MaxLabelKeys: int | positivo y acotado; eliminar labels arbitrarios |
| OPT14 | MaxLogPayloadBytes: int | límite mínimo, truncamiento seguro antes de persistir |
| OPT15 | MetricWindowSeconds: int | positivo y consistente con fuente/denominador |
| OPT16 | DiagnosticRetentionHours: int | pendiente de política aprobada; no infinito |
| OPT17 | AllowedDiagnosticRoles: enum[] | deny-all por defecto, RBAC backend obligatorio |
| OPT18 | AlertThresholdPolicyId: string | nulo hasta SLI/SLO/umbral aprobado y medido |
| OPT19 | ExporterProviderId: string | nulo; no activar proveedor ni cuota sin permiso |
| OPT20 | PolicyVersion: long | versión creciente y CAS con autor/rollback auditados |

**OPT01–OPT20 NO son IOptions, environment variables, settings en DB, flags de CONFIG ni campos desplegados.** La tabla no fija cifras numéricas de producción. OPT07/08/09/10 no pueden habilitarse para saltar redacción, incluso si administrador manipula payload. TTL/retención y costo real exigen decisión D01–D08 y revisión jurídica/tecnológica.

## 4. Flags restrictivos y degradación FL01–FL12

| ID | OFF/error / comportamiento futuro esperado |
| --- | --- |
| FL01 | Exporter inactivo: telemetría externa no se transmite ni simula entrega |
| FL02 | Tracing remoto OFF: spans internos mínimos y sin datos sensibles |
| FL03 | Métricas remotas OFF: p95/SLO no se reportan como medidos |
| FL04 | Alertas OFF: no se contacta on-call, email, webhook o proveedor |
| FL05 | Diagnóstico soporte OFF: 403/404 seguro, no fallback a acceso admin |
| FL06 | Vista por tenant OFF: no exponer agregados globales ni tenant B |
| FL07 | Body capture OFF: cuerpos y documentos nunca aparecen en logs |
| FL08 | SQL capture OFF: SQL literal, DSN, identificadores y credenciales censurados |
| FL09 | Labels high cardinality OFF: no usuario/URL/token como series métricas |
| FL10 | Retención sin aprobación: no habilitar persistencia indefinida |
| FL11 | Reglas SLI/SLO sin denominador: estado NO_MEDIDO, jamás PASS |
| FL12 | Kill-switch de exporter: detener nuevos envíos sin afectar transacciones ERP, JWT/RBAC o audit existente |

Los flags son **intenciones futuras**. No crean comandos de configuración, pantalla, migración, alertmanager, cola, auto-dispatch ni jobs de proveedor. Un sistema de monitoreo no es autoridad sobre negocio ni sustituye controles de acceso.

## 5. Reglas de validación VAL01–VAL18 (FUTURAS)

| ID | Regla que se deberá rechazar/obligar |
| --- | --- |
| VAL01 | distinguir tenant/plataforma y obtener owner de backend autenticado |
| VAL02 | rol de soporte solo lectura jamás modifica política |
| VAL03 | revocación de sesión/membresía impide lectura diagnóstica privada |
| VAL04 | ambiente y componente registrados en allowlist |
| VAL05 | opción desconocida/ausente no activa nada; OFF fail-closed |
| VAL06 | ninguna política anula redacción de JWT, PII, SQL/DSN y secretos |
| VAL07 | sampling está dentro de [0,1] y no desactiva auditoría obligatoria |
| VAL08 | labels allowlisted y cardinalidad acotada, sin ID de usuario |
| VAL09 | ventana/retención compatibles con decisión y privacidad aprobadas |
| VAL10 | cambio de configuración requiere versión CAS, actor y auditoría |
| VAL11 | política de B no contamina empresa A ni respuesta caché |
| VAL12 | payload/hostname/header no concede tenant ni rol |
| VAL13 | diagnóstico sin fuente/run/denominador responde NO_MEDIDO |
| VAL14 | error en exporter no rompe flujo ERP, auth ni transacciones |
| VAL15 | umbral SLO/alerta requiere evidencia y aprobación; no inferir costo |
| VAL16 | provider o token secreto no se imprime ni guarda en documento/Sheet |
| VAL17 | cambios parciales y rollback conservan política y auditoría anteriores |
| VAL18 | ningún flag habilita QA/main/PROD ni acceso a datos productivos |

## 6. Doce pares futuros de aceptación T01–T12 (0/12 EJECUTADOS)

| ID | Positivo futuro | Negativo/error o frontera futura |
| --- | --- | --- |
| T01 | admin plataforma autenticado lee política aprobada | tenant B no lee política A |
| T02 | soporte read-only autorizado consulta vista mínima | rol revocado/actor sin permiso 403 |
| T03 | exporter OFF no transmite nada | payload forjado no activa proveedor |
| T04 | sampling válido acota carga y conserva auditoría | negativo/NaN/>1 se rechaza |
| T05 | labels aprobados mantienen cardinalidad | userId, JWT, URL con ID o PII no son series |
| T06 | log minimizado conserva correlationId seguro | token/SQL/DSN sin redacción se rechaza |
| T07 | política CAS con versión actual actualiza config | versión obsoleta no modifica |
| T08 | SLI real con denominador fuente/ventana medible | falta de muestra produce NO_MEDIDO |
| T09 | kill switch detiene envío externo | no interrumpe autenticación ni transacciones |
| T10 | retención aprobada permite borrado sintético | retención infinita no aprobada NO_GO |
| T11 | provider falla sin afectar API ERP | fallo externo no crea reintentos infinitos/cargos |
| T12 | revert de policy conserva trabajo concurrente | sobrescribir config ajena o degradar seguridad falla |

**Resultados actuales:** 12 pares **escritos**, **0/12 ejecutados**. No hay evidencia causal en esta unidad para reportar HTTP, MySQL, build, exporter, Playwright, dashboard, SLO, p95, costo, alertas reales, cuotas, latencia o retención. **NO_MEDIDO**. En implementación R2 deberán ejecutarse tests xUnit adversariales, permiso tenant A/B, revocación, invalidaciones de caché, integración MySQL 8.4 efímero si persistencia cambia, browser si UI cambia y prueba/rollback de proveedor solo con acceso y autorización nuevos.

## 7. Calidad A0–A5 y reversión

- **A0:** requisito M02.04, fuentes V5 y contratos M01/M02, alcance de dos Markdown, opciones/flags con defaults seguros; clasificación R0.
- **A1:** comprobar consistencia OPT01–OPT20, FL01–FL12, VAL01–VAL18 y T01–T12 positivos/negativos/frontera/error. Son criterios documentales FUTUROS_NO_EJECUTADOS.
- **A2:** revisión estática JWT/RBAC/tenant A/B, revocación, datos mínimos, protección PII/secretos/SQL, métricas y cuotas; no se implementan controles.
- **A3:** cotejo de arquitectura y permisos: .NET/EF/MySQL/HTTP/Angular/Playwright/collector/exporter/proveedor **NO_APLICA** al diff R0 solo Markdown.
- **A4:** publicar documento y CHANGELOG autocontenido por fast-forward/CAS dev, checks Scope Lock y V5/procedencia \`completed/success\` del SHA, PLAN/BITACORA/readback por transición.
- **A5:** Task Javier distinta de DEV05 revisa independiente requisitos, SHA/diff, calidad y pruebas; A5_PENDIENTE hasta ese acto, sin QA/main/PROD.

**Rollback:** revert normal de commit con HEAD/owner/CI revalidados, preservar BITACORA y no reescribir historia. No cambiar ámbitos de observabilidad, middleware, flags, providers, tokens, precios, datos o alertas reales sin microtarea de código R2 y permisos separados.
