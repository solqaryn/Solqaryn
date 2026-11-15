# SOLQARYN V5 — C05-04.M02.02: contratos API, DTO, errores y compatibilidad de ligereza UI

**Responsable:** PRIMARY DEV 02 — JAVIER_CHATGPT (TASK_ID 6af16bb9492c8191b8e8c7d606f11507). **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA). **Repositorio/entorno:** solqaryn/Solqaryn, dev, Scope Lock STRICT. **Naturaleza:** documento de diseño M02.02, no implementación HTTP ni DTO compilado.

## Autoridad y fuentes existentes

La tarea corresponde a PLAN_MAESTRO fila 837; el preflight documentó H=PENDIENTE, I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI. Comparte como precedente técnico la fase C05-04.M01.01–06, cerrada con sus revisiones independientes; el contrato conceptual C05-04.M02.01 está LISTO_DEV pero todavía requiere A5 ajena. El DAG permite trabajar este documento independientemente, no permite asumir su revisión aprobada.

Fuentes verificadas en dev: frontend/angular.json (blob 8b00f8fee80fdfafe1864e233be7d5f20ad444e3), frontend/src/app/core/performance/after-render-selective-preloading.strategy.ts (a5083eac7fd882430c9550c037c17f89cb946149), frontend/src/app/shared/feedback-state/feedback-state.component.ts (7deb40475f0c492d09e649e7351b2915bd53d356) y backend/src/API/Controllers/DashboardController.cs (07b2a8a906a5c08c5d8cccfda057f38e456b5304). El controlador real expone GET dashboard/resumen, exige [Authorize] y permiso Dashboard/Ver, devuelve ApiResponse<DashboardResumenDto>. Nada aquí crea ni modifica esa ruta.

## Contratos conceptuales (NO IMPLEMENTADOS)

| ID | Operación propuesta | Entrada mínima | Salida esperada | Límite de confianza |
| --- | --- | --- | --- | --- |
| OP01 | Medir carga de superficie | ruta permitida, SHA, viewport, entorno | referencia de traza, estado NO_MEDIDO inicial | jamás enviar JWT ni datos personales |
| OP02 | Contabilizar solicitudes | flujo, método, ruta, grupo lógico | total observado y duplicados con muestra | capturar solo metadatos depurados |
| OP03 | Consultar presupuesto de bundle | SHA, artefacto de compilación | bytes reales y budget, o SIN_EVIDENCIA | nunca confundir budget configurado con medición |
| OP04 | Registrar experiencia móvil | ancho, zoom, preferencia de movimiento | pruebas accesibilidad y navegación, o NO_EJECUTADAS | no sustituye autorización API |
| OP05 | Cambiar tenant autorizado | sesión y autorización backend verificada | estado visual nuevo sin caché del tenant previo | sin confiar en slug, header o localStorage |
| OP06 | Recuperar error de API | código HTTP real, correlation ID depurado | feedback accesible loading/error/retry | nunca traducir 401/403/500 en vacío exitoso |
| OP07 | Comparar antes/después | dos SHA, mismo dispositivo y red | delta solo si muestras comparables | no certificar CWV sin entorno y evidencia |
| OP08 | Solicitar revisión independiente | ID de microtarea, SHA, evidencias | decisión A5 auditable | revisor de otra Task Javier |

OP01–OP08 son operaciones de análisis futuro, **no son endpoints**, workers, colas ni eventos instalados. Cualquier futura API .NET deberá reutilizar convenciones existentes, validar claims, RBAC y tenant en servidor y tener autorización propia antes de exponerse.

## DTO conceptuales y validación

| DTO propuesto | Campos mínimos | Reglas de aceptación futura |
| --- | --- | --- |
| UiSurfaceBudgetV1 | surfaceKey, sourceSha, artifactRef, initialBytes?, limitBytes? | SHA completo; números no negativos; valor nulo cuando no medido |
| UiRequestObservationV1 | surfaceKey, method, routeTemplate, requestGroup, count?, sampleRef | nunca URL completa con query sensible, JWT o PII |
| UiInteractionEvidenceV1 | viewportPx, networkProfile, cacheMode, sampleCount?, lcpMs?, inpMs?, cls? | enteros positivos; métricas nulas si NO_MEDIDO; condiciones reproducibles |
| UiContextVersionV1 | opaqueContextVersion, serverAuthorizedTenantRef, sessionEpoch | contexto del servidor; no derivar permiso del cliente |
| UiFailureV1 | publicCode, correlationRef, retriable, state | mensaje no sensible; error distinto de empty/loading |
| UiReviewEvidenceV1 | scopeId, authorTaskId, reviewerTaskId, sourceSha, evidenceRefs | Task revisora distinta de autora; no concede deployment |

Estos nombres documentan un contrato futuro y no deben asumirse presentes en TypeScript o C#. Mantener compatibilidad con ApiResponse y consumidores actuales; cualquier nuevo campo deberá ser aditivo y opcional hasta que todos los clientes estén probados.

## Matriz de errores y negativos futuros

| Código | Condición | Respuesta requerida si se implementa |
| --- | --- | --- |
| ERR01 | Sin sesión en recurso protegido | 401 o contrato equivalente real; nunca contenido privado |
| ERR02 | Sin permiso RBAC | 403 y feedback de acceso denegado |
| ERR03 | Acceso A a recurso de tenant B | denegar, sin metadata cruzada ni caché reutilizable |
| ERR04 | Sesión caducada durante petición | invalidar datos y no presentar respuesta tardía |
| ERR05 | Contexto de caché no coincide | descartar respuesta; refetch autorizado |
| ERR06 | Timeout, desconexión o 5xx | estado de error accesible y retry acotado |
| ERR07 | Trace sin SHA, perfil de red o viewport | SIN_EVIDENCIA; no inventar LCP/INP/CLS |
| ERR08 | Conteo de HTTP sin baseline | NO_MEDIDO; no presumir deduplicación |
| ERR09 | Presupuesto sobrepasado | fallo de CI pertinente, nunca subir límite automáticamente |
| ERR10 | Payload de telemetría con JWT/PII | rechazar y no persistir datos sensibles |
| ERR11 | Revisión A5 por autora | rechazo de independencia |
| ERR12 | Prueba CI queued/cancelled/skipped/failed | no PASS y conservar VALIDANDO donde sea obligatoria |

## Oráculos para implementación posterior (NO_EJECUTADOS)

Positivos P01–P05: ruta real con autorización backend; medición reproducible con SHA y baseline; deduplicación que conserva filtros/sesión; cambio A→B sin datos residuales; feedback 403/500 accesible. Negativos N01–N07: token caducado; rol falso en UI; respuesta de A posterior a conmutación B; metadatos sensibles en logs; lazy preload de ruta privada; incremento ficticio de budget; A5 propia. Se ejecutarán con Angular/Playwright, API JWT/RBAC y casos tenant A/B solamente al implementar M03/M04/M05.

## Impacto, auditoría y reversión

Impacto del documento: una ruta docs/v5 y una sección de CHANGELOG_AI.md; **ningún endpoint, DTO real, tabla, migración, feature flag, control de acceso, monitor ni dato productivo cambia**. A0: alcance y contenido sin secretos; A1: contratos y errores verificables textualmente; A2: amenazas tenant A/B, privacidad y RBAC explícitas; A3: concordancia con código existente y M02.01 sin asumir tests; A4: publicación en dev con FF/CAS, Scope Lock y procedencia CHANGELOG exact-SHA, diff y readback institucional. Compilación .NET/Angular no causal para este documento: NO_APLICA, nunca PASS presumido. A5 permanece pendiente de una Task Javier distinta. Rollback: git revert normal, sin force-push ni autorización para QA/main/PROD.
