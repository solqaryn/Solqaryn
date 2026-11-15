# SOLQARYN V5 — C05-04.M02.04: configuración tipada, ámbitos y flags seguros de ligereza UI

**Owner:** PRIMARY DEV 02 — JAVIER_CHATGPT, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN_MAESTRO fila 839. **Repo:** solqaryn/Solqaryn@dev, Scope Lock STRICT. **Tipo:** diseño M02.04, sin valores de configuración ni flags implementados.

## Referencias, estado y compatibilidad

El DAG de C05-04.M02.04 estaba F=VALIDADO/G=SI, COLA G/O=SI y PLAN PENDIENTE sin responsable al reclamar. Su habilitación proviene de M01.01–06 y no equivale a aprobar automáticamente M02.01–03, ni autoriza M02.06 terminal. Las referencias reales son frontend/angular.json (blob 8b00f8fee80fdfafe1864e233be7d5f20ad444e3), app.config.ts, scripts/validate-angular-bundle-policy.mjs (8081db397b5cdc5d3799f858faaa2ed7d52761f0), y after-render-selective-preloading.strategy.ts (a5083eac7fd882430c9550c037c17f89cb946149). El contrato conservado exige initial warning 650kb y error 750kb, estilos 16kb/32kb; estos valores son política configurada, **no tamaño ni Core Web Vitals medidos**.

El selector real de precarga exige preloadAfterRender, navegación /tienda, app estable y timer 250 ms; no se debe convertir a PreloadAllModules ni invocar precarga indiscriminada. Zone.js 0.16.3 sigue activo; convertirlo a zoneless no forma parte del alcance.

## UiPerformancePolicyV1 propuesta (NO_COMPILADA)

| Clave conceptual | Tipo | Ámbito futuro | Predeterminado seguro y validación |
| --- | --- | --- | --- |
| EnableSelectivePreload | boolean | aplicación/ruta | true solo para rutas ya marcadas y autorizadas |
| EnableClientRequestDedup | boolean | sesión/operación | false hasta pruebas de identidad, filtro y cancelación |
| EnforceTenantCachePartition | boolean | seguridad invariante | true, nunca desactivable por usuario/tenant |
| MaxConcurrentUiRequests | entero positivo | frontend/ruta | pendiente de baseline; sin número mágico no aprobado |
| UiRequestTimeoutMs | entero positivo | operación | requiere compatibilidad backend; reintentos acotados |
| AllowPerformanceTelemetry | boolean | privacidad/entorno | false hasta consentimiento/finalidad validada |
| TelemetrySampleRate | decimal [0,1] | entorno autorizado | 0 si no hay permiso para capturar |
| UiSkeletonMinimumMs | entero >=0 | superficie | no ocultar error, sin spinner infinito |
| EvidenceRetentionDays | entero no negativo | artefacto CI | depende de política formal, sin retención indefinida |
| BundleWarningBytes | bytes | build/CI | reflejar 650kb vigente, no elevar sin autorización |
| BundleErrorBytes | bytes | build/CI | reflejar 750kb vigente; fail-closed |
| AllowCrossTenantCache | boolean prohibido | control de seguridad | SIEMPRE false, rechazo si alguien intenta true |

Son nombres **conceptuales**; no se añaden a environment.ts, .NET Options, base, flags remotos ni servicio alguno. Nunca materializar JWT, secretos, URL firmadas o identificadores personales en flags o logs de configuración.

## Reglas de precedencia y fail-closed

POL01: el backend decide permisos efectivos, no el flag frontend. POL02: cambio de tenant/sesión invalida cachés y resultados pendientes por versionado opaco. POL03: feature desactivada mantiene datos y trazabilidad. POL04: config inválida significa política conservadora, no bypass. POL05: sin baseline de red/dispositivo no inferir mejora. POL06: el bundle real y CWV requieren mediciones reproducibles, no los valores escritos. POL07: habilitar dedup obliga a identidad completa (método, ruta, filtros, tenant autorizado, sesión, versión). POL08: flags de animación respetan reduced-motion y foco accesible. POL09: ninguna opción cambia CORS/JWT/RBAC/tenant server-side. POL10: no habilitar RUM con datos privados sin política, consentimiento y minimización. POL11: config de producción requiere autorización separada. POL12: cada ajuste debe tener reversión y prueba de no-regresión.

## Pruebas contractuales futuras (NO_EJECUTADAS)

| Escenario | Resultado exigido |
| --- | --- |
| P01 Activación selectiva en ruta pública marcada | carga solo después de estabilidad de app |
| P02 Cambio A→B con sesión autorizada | cache y respuesta antigua A descartadas |
| P03 Flag de dedup autorizado | peticiones equivalentes comparten resultado sin suprimir errores |
| P04 Modo reducido o teclado | foco, semántica y feedback intactos |
| P05 Comparación de bundle con baseline | valores medidos y check reproducible por SHA |
| N01 AllowCrossTenantCache=true | rechazado |
| N02 Raise budget sin decisión | check falla sin maquillar PASS |
| N03 Tenant derivado de URL/header cliente | denegación server-side |
| N04 Expiración de sesión con cache | invalidación, nunca presentación privada |
| N05 Timeout HTTP traducido a empty | rechazo, error accesible |
| N06 Token en traza/flag | no serializar ni persistir |
| N07 PreloadAllModules o carga eager | verificador falla; no habilitar |
| N08 CI skipped/cancelled/failed | no contar como PASS |

## Impacto y cierre

Solo documento Markdown y sección CHANGELOG_AI.md; no hay cambios de código, runtime, budget, rutas, flags, permisos, datos ni infraestructura. A0 no secretos / no código; A1 claves, tipos y reglas; A2 limites JWT/RBAC/tenant A/B y privacidad; A3 contraste con scripts y arquitectura real; A4 commit fast-forward/CAS más Scope Lock/V5 exact-SHA y PLAN/COLA/BITACORA readback. CI .NET/Angular no causal para este diff documental, NO_APLICA sin inferir PASS. A5 requiere Task Javier diferente, nunca autoaprobación. Rollback documental mediante git revert normal; sin despliegues QA/main/PROD.
