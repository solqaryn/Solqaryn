# SOLQARYN V5 — C15-05.M02.02: contrato propuesto de API, DTO, errores y compatibilidad de auditoría operativa

**Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autor:** PRIMARY DEV 01 — JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 14 noviembre 2026. **Naturaleza:** diseño de contrato M02 exclusivamente documental, **NO_IMPLEMENTADO**, sin endpoints, despliegue, tablas, migraciones, invocación de Tasks ni cambios de seguridad.

## 1. Fuentes y precondiciones comprobables

Autoridad ejecutable: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). Estado institucional: [PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Para este ID, el preflight exige PLAN H=PENDIENTE, I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y ausencia de escritor en el scope; al reclamar cambia exclusivamente a EN_PROGRESO bajo un Task Javier autorizado. Cualquier transición debe releer fuentes y sucesoras; no se asume disponibilidad futura.

Contrato precedente: `docs/v5/c15-05-m02-01-modelo-ownership-invariantes-auditoria-operativa.md`, entidades OM01–OM14, invariantes INV01–INV16 y casos AT01–AT12, publicado en `80604b5073351aecacd25f5be595f099b3a9c740`. Los datos auditables continúan en PLAN_MAESTRO H/I/K, COLA G/N/O, DAG F/G, BITACORA A:Z y TAREAS_PROGRAMADAS AA:AN. El proveedor nativo acredita sus propios identificadores y estados. Ninguna celda por sí sola garantiza un lock distribuido ni ejecución en tiempo real.

## 2. Contratos de consulta V1 propuestos — rutas NO EXISTENTES

La siguiente tabla es **propuesta de implementación futura sujeta a autorización separada**. No habilita HTTP real ni crea rutas públicas. Evaluar primero la ampliación de `backend/src/API/Controllers/AuditoriaController.cs`, que **sí** existe con `[Authorize]`, `RequierePermiso(ModuloSistema.Auditoria, AccionPermiso.Ver)` y denegación adicional si no es administrador; no debilitar ese control.

| Operación lógica | Superficie futura, NO_IMPLEMENTADA | Salida propuesta | Seguridad |
| --- | --- | --- | --- |
| OP01 | `GET /auditoria/operacional/v1/microtareas/{id}` | `MicrotareaAuditoriaV1` | Identidad de operador autorizada; ID exacto; sin exposición pública |
| OP02 | `GET /auditoria/operacional/v1/microtareas/{id}/dependencias` | `DependenciaAuditoriaV1[]` | Sólo hechos de PLAN/COLA/DAG; nunca muta gates |
| OP03 | `GET /auditoria/operacional/v1/microtareas/{id}/eventos?cursor&limit` | `PaginaEventosV1` | BITACORA es append-only, lectura minimizada |
| OP04 | `GET /auditoria/operacional/v1/publicaciones/{sha}/checks` | `CheckResumenV1[]` | SHA exacto y fuente GitHub; no inventar PASS |
| OP05 | `GET /auditoria/operacional/v1/tasks/{taskId}/ultimo-reporte` | `EjecucionAuditoriaV1` | Sólo seis TASK_ID Javier; separar reporte local y estado nativo |
| OP06 | `GET /auditoria/operacional/v1/microtareas/{id}/revisiones` | `RevisionA5V1[]` | Verificar TASK_ID autor/revisor distintos; no incluye secrets |
| OP07 | `POST /auditoria/operacional/v1/microtareas/{id}/revisiones` | `RevisionA5V1` o problema | NO AUTORIZADO por este contrato; sería escritura privilegiada, con CAS real y auditoría |

Los seis GET no están implementados, ni se decidió si corresponde API futura: una implementación alternativa puramente administrativa de lectura del Sheet también requiere permisos explícitos. Ningún consumidor Angular, cliente externo, endpoint `POST`, nuevo privilegio o token se considera existente por esta especificación.

## 3. Esquemas lógicos mínimos y procedencia

| DTO V1 conceptual | Campos mínimos | Restricciones |
| --- | --- | --- |
| `MicrotareaAuditoriaV1` | `id`, `estado`, `ownerTaskId?`, `updatedAtUtc?`, `nextAction?`, `evidenceRef?` | `id` canónico; los textos del Sheet no son autorizaciones |
| `DependenciaAuditoriaV1` | `prerequisiteId`, `reviewState`, `gate`, `sourceRow`, `observedAtUtc` | Retornar SI/NO exactamente observado, sin recomputar sobre caché |
| `EventoAuditoriaV1` | `eventId`, `idempotencyKey`, `microtareaId`, `operation`, `previousState`, `nextState`, `timestampUtc`, `evidenceRef?` | No publicar tokens, PII, credenciales, prompts ni dumps íntegros |
| `CheckResumenV1` | `headSha`, `name`, `status`, `conclusion?`, `url`, `observedAtUtc` | Sólo `completed+success` del SHA pertinente acredita PASS |
| `EjecucionAuditoriaV1` | `taskId`, `localRunKey?`, `nativeRunId?`, `nativeState?`, `nativeObservedAtUtc?`, `selfReportState?`, `selfReportAtUtc?` | `nativeRunId` nulo si no existe prueba nativa; el reporte no es heartbeat |
| `RevisionA5V1` | `microtareaId`, `authorTaskId`, `reviewerTaskId`, `reviewedSha`, `decision`, `eventId`, `evidenceRef` | Mismos TASK_ID prohíben PASS; decisión sin fuente queda pendiente |
| `PaginaEventosV1` | `items`, `nextCursor?`, `observedAtUtc` | `limit` acotado (propuesta 1–100); paginación estable por orden de eventos |
| `ProblemaAuditoriaV1` | `code`, `message`, `correlationId`, `retryable` | Mensaje sin secretos, sin stack trace, no detallar otro tenant |

Todos los DTO son **tipos conceptuales**, no clases C# nuevas. La versión del contrato se separa del estado de GitHub y de la versión del plan; `observedAtUtc` identifica una fotografía y nunca una promesa de actualización continua. Normalizar UTC sin perder el timestamp fuente.

## 4. Validaciones, errores y fallos cerrados

| Código lógico | HTTP futuro sugerido | Condición / tratamiento |
| --- | --- | --- |
| ERR01 `INVALID_MICROTASK_ID` | 400 | ID no cumple formato canónico o excede longitud |
| ERR02 `UNAUTHENTICATED` | 401 | Sin autenticación; jamás buscar datos antes de verificar identidad |
| ERR03 `FORBIDDEN_SCOPE` | 403 | Falta rol/permiso administrativo o acceso al recurso; no exponer lista de otros tenants |
| ERR04 `NOT_FOUND` | 404 | ID inexistente o inaccesible; respuesta uniforme para evitar enumeración |
| ERR05 `STATE_CONFLICT` | 409 | Estado/owner cambió desde lectura; no resolver con sobreescritura |
| ERR06 `PRECONDITION_FAILED` | 412 | HEAD o versión esperada desactualizada; relectura, nunca force-push |
| ERR07 `DEPENDENCY_BLOCKED` | 422 | DAG/CI/A5 no permiten transición, explicar sin conceder SI |
| ERR08 `RATE_LIMITED` | 429 | Límite de consulta, reintento acotado y trazable |
| ERR09 `PROVIDER_UNAVAILABLE` | 503 | Fuente GitHub/Sheets/Tasks indisponible, mantener último reporte marcado obsoleto |
| ERR10 `REVIEW_NOT_INDEPENDENT` | 403 | A5 por mismo TASK_ID; auditar rechazo |
| ERR11 `CI_NOT_SUCCESS` | 409 | `queued/in_progress/cancelled/skipped/failure` cuando check es requerido |
| ERR12 `IDEMPOTENCY_COLLISION` | 409 | Clave repetida con payload diferente; no duplicar BITACORA |

Validaciones obligatorias para OP07 **antes de cualquier implementación real**: identidad de seis Tasks permitidas, rol y permisos del proveedor, `authorTaskId != reviewerTaskId`, SHA y blob original, estado LISTO/A5_PENDIENTE, evidencia positiva/negativa pertinente, CAS genuinamente atómico, idempotencia duradera y readback de PLAN/BITACORA/DAG/COLA. **Google Sheets lectura+escritura no constituye por sí sola una transacción multi-celda.** Un sistema real no debe afirmar exclusión absoluta sin mecanismo verificable.

## 5. Matriz de compatibilidad y límites de seguridad

| Componente existente | Compatibilidad requerida para implementación futura |
| --- | --- |
| `AuditoriaController` | Conservar `Authorize` + permiso de Auditoria.Ver + verificación administrativa; ninguna ruta propuesta se añade hoy |
| `DashboardController` | Conservar `Authorize` y `RequierePermiso`; no mezclar panel comercial con datos operativos internos |
| `ApiResponse<T>` | Evaluar envoltorio actual en lugar de crear respuestas contradictorias; versionar cambios explícitamente |
| PLAN_MAESTRO / COLA / DAG | Unir siempre por ID exacto, releer tras transición, no reinterpretar `VALIDANDO` como `LISTO` |
| BITACORA A:Z | Eventos completos con EVENT_ID/IDEMPOTENCY_KEY únicos; nunca editar historial |
| TAREAS_PROGRAMADAS AA:AN | Sólo fila del owner; diferenciación `TASK_SELF_REPORT` vs proveedor nativo |
| GitHub Actions | Comparar `head_sha`, `status`, `conclusion`; no reemplazar pruebas .NET/Angular/MySQL por Markdown |
| Empresas A/B / JWT / RBAC | Filtrado de backend, no confiar en headers, cliente o inferencias del operador; PII minimizada |

M02.03 definirá persistencia e índices; M02.04 configuraciones seguras; M02.05 eventos conceptuales; M02.06 puertas de salida **sólo** con sus prerrequisitos. No autoriza migraciones, API, automatización reactiva, servidores, secretos, QA, main, PROD ni proveedores de pago.

## 6. Pruebas futuras, rollback y criterios A0–A5

Casos **NO_EJECUTADOS**: T01 contrato de ID existente vs N01 ID inexistente; T02 consulta de dependencias validada vs N02 ausencia de DAG; T03 paginación estable vs N03 cursor corrupto; T04 check success exact-SHA vs N04 cancelled; T05 autor/revisor diferentes vs N05 self-review; T06 proveedor nativo ausente vs N06 autorreporte presentado como run nativo; T07 JWT/RBAC permitido vs N07 acceso de otro tenant; T08 CAS único ganador vs N08 doble escritor; T09 evento idempotente vs N09 duplicado distinto; T10 provider timeout vs N10 LISTO inventado; T11 403 sin PII vs N11 fuga de ID ajeno; T12 HEAD cambiado vs N12 publicación que pisa otro commit.

**A0**: consistencia estática de propuesta, cobertura de OP01–OP07, DTO, errores y trazabilidad; revisar contra M02.01 y el controlador existente. **A1**: positivos/negativos especificados, no ejecutados en runtime. **A2**: preservación conceptual del aislamiento JWT/RBAC/tenant A/B; sin pentest ni prueba HTTP real. **A3**: coherencia documental con PLAN/COLA/DAG/BITACORA y arquitectura; backend, frontend, SQL y Playwright NO_APLICA al diff exclusivamente Markdown. **A4**: después de commit fast-forward con expected HEAD, diff limitado a dos Markdown, validar Scope Lock y procedencia de CHANGELOG exact-SHA, registrar BITACORA y readback. **A5**: revisión técnica independiente por otra de las cinco Tasks Javier, nunca aprobación del autor.

**Rollback:** `git revert` normal de los dos Markdown en dev + nueva CI y evento aditivo, sin reset/force-push. Si una prueba causal no finaliza SUCCESS, conservar VALIDANDO y owner con diagnóstico y no certificar LISTO_DEV. Este archivo es sólo el contrato previo a una posible implementación aprobada.
