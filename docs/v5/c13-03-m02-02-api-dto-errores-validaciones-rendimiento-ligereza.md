# SOLQARYN V16 — C13-03.M02.02: contratos de API, DTO, errores y compatibilidad de rendimiento

**ID:** C13-03.M02.02; PLAN_MAESTRO fila 1953; autor PRIMARY DEV 03 JAVIER (\`6adc2e4bd8a48191ac7d8641ed95a1a3\`); \`solqaryn/Solqaryn@dev\`; \`PROJECT_SCOPE_LOCK=STRICT\`. Riesgo **R0: especificación documental**, sin crear controlador, ruta HTTP, DTO compilado, captura de métricas, tablas, archivos de configuración, flags ni servicio operativo.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`; PLAN/COLA/DAG institucionales. La microtarea exige **definir** API, DTO, errores, validaciones y compatibilidad para experiencia ligera en carga inicial, navegación, imágenes, tablas, datos y móviles; no entregar API ejecutable.

## 1. Dependencias y límites existentes

La microtarea se habilita documentalmente con \`C13-03.M01.01–06+A5\`; DAG F/G=VALIDADO/SI y COLA G/O=SI antes de claim exclusivo. Su hermana M02.01 propone E01–E14, REL01–REL12 e INV01–INV18 como conceptos de evidencia, propiedad y presupuesto; dichos nombres **no son clases, DbSets o rutas instaladas**. M01.06 mantiene G01–G16, RB01–RB08 y T01–T10 futuros, sin pruebas ejecutadas.

**Limitación crítica:** el presupuesto propuesto de bundle 650–750 KB frente a 1–2 MiB sigue **SIN_RESOLVER**; no elegirlo aquí como hecho contractual aprobado. LCP, INP, CLS, TTFB y presupuestos por dispositivo permanecen **NO_MEDIDO**, con 0 observaciones runtime aportadas por este documento. Toda medición futura debe indicar SHA, perfil/dispositivo, red, navegador, entorno, denominador, intervalo, percentil, artefacto y umbral previamente aprobado.

El backend ASP.NET Core 10 decide JWT, pertenencia al tenant, RBAC, revocación y autorización por objeto; Angular 22.2.1 ofrece navegación pero no concede autoridad. EF Core 10/Oracle MySQL y datos empresariales no se alteran. El contrato de caché nunca autoriza publicar contenido privado ni compartir estado empresarial A/B.

## 2. Operaciones conceptuales OP01–OP08 (sin URLs ni endpoints creados)

| ID | Operación propuesta en futura fase de código | Precondición y salida permitida |
| --- | --- | --- |
| OP01 | Consultar versión de presupuesto aprobada | Sólo metadatos no sensibles, decisión pendiente claramente marcada; sin inventar umbral |
| OP02 | Consultar catálogo de escenarios medibles | Sólo descripciones públicas; escenarios privados requieren autorización de servidor |
| OP03 | Registrar medición sintética de desarrollo autorizada | Ingesta restringida, identificadores de build/run válidos, tasa y volumen acotados |
| OP04 | Consultar resultados agregados | Sin PII, tokens, identificadores empresariales ni evidencia de tenant ajeno |
| OP05 | Consultar manifiesto de recursos públicos críticos | Tamaños y prioridad permitidos; jamás revelar URLs o tokens privados |
| OP06 | Consultar capacidades de paginación y límites | Declarar máximos y orden estable sin exponer campos restringidos |
| OP07 | Consultar política de caché de contenido público | Respetar ETag/304/Vary y prohibición de almacenar respuestas privadas |
| OP08 | Solicitar comparación de dos perfiles de evidencia | Rechazar perfiles incompatibles o métricas sin denominador/procedencia |

Estas OP son posibilidades a evaluar, **no decisión de crear ocho endpoints**. Antes de código R1/R2 comprobar necesidad real frente a servicios existentes, rutas backend/Angular, seguridad, volumen, límites y aprobación de arquitectura. Cualquier registro de mediciones en servidor requiere revisión adicional de privacidad, rate limiting y retención. Ninguna OP modifica transacciones, tenants, proveedores, DNS o producción.

## 3. DTO conceptuales DTO01–DTO08 y esquema mínimo

| ID | Campos sugeridos (no esquema persistido) | Política |
| --- | --- | --- |
| DTO01 BudgetDecision | version, metric, proposedRange, approvedThreshold?, decisionState, sourceSha | \`PENDIENTE\` cuando hay conflicto; no serializar valor por defecto como aprobado |
| DTO02 MeasurementProfile | device, viewport, networkPreset, browser, locale, reducedMotion | valores allowlist y límites de longitud |
| DTO03 ScenarioDescriptor | scenarioId, surface, publicOrPrivate, requiredRole?, cacheClass | id estable, no autoridad derivada de slug |
| DTO04 MeasurementProvenance | runId, testedSha, tool, sampleCount, unit, timestampUtc, artifactDigest | checksum verificable, sin cookies/PII |
| DTO05 MetricDistribution | metricName, unit, sampleCount, percentile, value?, status | valor \`null\` y \`NO_MEDIDO\` sin evidencias |
| DTO06 CriticalAssetView | resourceClass, dimensions, loadingPolicy, publicCachePolicy | ninguna URL privada/capability secreta |
| DTO07 DataPageContract | pageSizeMax, cursor, stableSort, includedFields, nextCursor? | campos RBAC filtrados por backend |
| DTO08 SafeError | code, correlationId, traceablePublicMessage, retryable | nada de stack trace, SQL, secretos ni datos de otro tenant |

Ninguna entidad crea \`EmpresaId\` confiable en frontend. Si se requiere scope empresarial, el backend lo deriva de identidad autenticada y verifica membresía/objeto; los identificadores del cliente son filtros no autoritativos. Las comparaciones deben usar unidades homogéneas y percentiles definidos antes de medir.

## 4. Taxonomía de errores ERR01–ERR12 (futura)

| Código conceptual | Condición adversa | Respuesta segura propuesta |
| --- | --- | --- |
| ERR01 AUTH_REQUIRED | Sin sesión auténtica | 401, sin datos protegidos |
| ERR02 ROLE_DENIED | Rol insuficiente o revocado | 403, sin filtrar existencia de recursos |
| ERR03 CROSS_TENANT | Empresa/objeto ajeno | 403/404 según contrato seguro, cero payload privado |
| ERR04 MISSING_BASELINE | No hay baseline verificable | Estado NO_MEDIDO, no fabricar dato |
| ERR05 BUDGET_UNDECIDED | Dos presupuestos incompatibles | Estado PENDIENTE_DECISION, no PASS |
| ERR06 PROFILE_INVALID | Red, viewport o navegador fuera de allowlist | Validación 400 con mensaje estable |
| ERR07 UNIT_INCOMPATIBLE | KB vs MiB, tiempo/unidades sin normalización | Rechazo de comparación hasta normalizar |
| ERR08 SAMPLES_INSUFFICIENT | Sin denominador mínimo aprobado | NO_MEDIDO, jamás porcentaje ficticio |
| ERR09 RESOURCE_PRIVATE | Intento de manifest/cache pública de privado | Rechazo, registro seguro sin PII |
| ERR10 RATE_LIMITED | Peticiones repetidas/abusivas | 429 y retry acotado sin colas ocultas |
| ERR11 EVIDENCE_MISMATCH | SHA/run/blob no corresponde al escenario | Rechazo, nunca usar otro SHA como PASS |
| ERR12 UNAVAILABLE | Instrumentación/proveedor no disponible | Fail closed, estado verificable y sin datos inventados |

Las respuestas HTTP listadas son **propuestas para implementación posterior**, no respuestas observadas de una API existente. El manejo de 401/403/404 preservará la política real existente cuando la capa sea implementada; no crear códigos duplicados de dominio sin conciliar contrato actual.

## 5. Validaciones y compatibilidad VAL01–VAL16

| ID | Regla antes de construir backend/cliente |
| --- | --- |
| VAL01 | Toda lectura privada exige JWT, rol, tenant y objeto del servidor, no de URL/query |
| VAL02 | Revocación invalidada: no conservar páginas, métricas o caché privada |
| VAL03 | Nunca almacenar token/PII/empresa en RUM, logs, tracing público o ETag |
| VAL04 | No aceptar un presupuesto sin versión, aprobador y decisión explícita |
| VAL05 | Todas las métricas usan unidades normalizadas y sampleCount real |
| VAL06 | Un percentil requiere definición de muestra, perfil y herramienta verificable |
| VAL07 | Validar viewport, navegador, red y movimiento reducido por allowlist |
| VAL08 | Un SHA probado debe corresponder a artefacto y configuración de CI |
| VAL09 | Las medidas de tenant A y B no pueden cruzarse por caché/selector |
| VAL10 | TTL, Vary y 304 no convierten contenido privado en compartible |
| VAL11 | Las respuestas paginadas mantienen sort/cursor estables y acceso por fila |
| VAL12 | Error y estado vacío no revelan existencia de recurso ajeno |
| VAL13 | Cargas de datos usan límites de tamaño/tiempo/recursos y reintentos acotados |
| VAL14 | Imágenes críticas tienen dimensiones y fallback para evitar CLS |
| VAL15 | Las variantes móviles y teclado no se degraden por optimización |
| VAL16 | Sin evidencia real de navegador, API, MySQL o seguridad requerida no se afirma PASS runtime |

**Compatibilidad:** especificar versiones y contratos sólo al abrir una tarea de implementación, mantener DTO existente cuando exista, preferir cambios aditivos, no romper rutas, respuesta de error, retención ni autorización. El \`git revert\` documental conserva el estado de sistemas porque aquí no hay cambios runtime. Cualquier futura medición debe evitar datos productivos.

## 6. Oráculos de validación futura T01–T10 (NO_EJECUTADOS)

| ID | Positivo futuro | Negativo futuro capaz de fallar |
| --- | --- | --- |
| T01 | Usuario autorizado consulta su escenario privado | Tenant B o rol revocado no recibe metadatos de A |
| T02 | Decisión aprobada aparece con versión y fuente | Presupuesto 650–750 KB vs 1–2 MiB sin decidir devuelve PENDIENTE |
| T03 | Resultados reales incluyen muestra, unidad y percentil | Serie vacía o sin perfil no produce valor PASS |
| T04 | Imágenes públicas muestran manifiesto seguro | URL/token de imagen privada nunca se expone |
| T05 | 304 público sirve variante autorizada | Respuesta de sesión privada jamás es compartida |
| T06 | Tabla conserva orden, datos e importes permitidos | Cursor corrupto/tenant cruzado falla sin filtración |
| T07 | Simulación móvil 320px preserva navegación y foco | Error/lentitud no bloquea teclado o reader |
| T08 | Fallo de proveedor deja estado no medido | No usar baseline o cache anterior como resultado nuevo |
| T09 | SHA, blob y filtro de CI acreditan el run | Resultado de SHA descendiente no equivalente se rechaza |
| T10 | Revert normal recupera contrato anterior | Se detecta pérdida de cambios concurrentes o gate de seguridad |

**Cobertura ejecutada hoy de estos T01–T10: 0/10.** Son pruebas a implementar en otra microtarea de código. No hay tests HTTP, MySQL, Chromium, Playwright ni métricas reales en este R0.

## 7. A0–A5 y reversibilidad

- **A0:** diff individual de sólo documento y CHANGELOG, fuente V5, M01.06 y M02.01 contrastados, OP08, DTO08, ERR12, VAL16, compatibilidad y riesgo R0.
- **A1:** revisión textual de oráculos positivos, negativos y fronteras; los T01–T10 quedan FUTUROS_NO_EJECUTADOS, no PASS.
- **A2:** fronteras JWT/RBAC/tenant A/B, revocación, PII, caché y ausencia de secretos documentadas; no se cambia backend.
- **A3:** integración .NET/MySQL/HTTP/browser **NO_APLICA** al diff Markdown; al implementar, probar integración causal y errores con fixtures autorizados.
- **A4:** publicar commit único con fast-forward/CAS; Scope Lock y procedencia CHANGELOG V5 completed/success exact SHA; registrar PLAN/COLA/DAG y BITACORA con readback.
- **A5:** revisión independiente obligatoria por una Task Javier distinta de la autora; A5 pendiente hasta esa evidencia, sin QA/main/PROD.

**Rollback del cambio documental:** \`git revert\` normal luego de releer HEAD, conservar BITACORA histórica y pruebas de autoría. Sigue sin decidirse el presupuesto ni medirse LCP/INP/CLS. El contrato no crea API operativa ni habilita despliegues, migraciones, secretos o proveedores.
