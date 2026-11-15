# SOLQARYN V5 FINAL — C15-03.M02.02: contrato de API, DTO, errores y seguridad multiempresa

**Autor:** PRIMARY DEV 04 — JAVIER_CHATGPT, Task `6aeacb7820ac8191b7d768a6f33dfa09`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. Entrega **DOCUMENTAL** M02.02; no agrega endpoints, DTO compilados, políticas ejecutables, permisos, migraciones, jobs ni rutas de despliegue.

## 1. Contexto de autoridad y fronteras

Autoridades: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [M01.06](c15-03-m01-06-bloqueos-rollback-liberacion-auditoria-seguridad-tenancy.md) con A5 independiente y [M02.01](c15-03-m02-01-modelo-entidades-ownership-auditoria-seguridad-tenancy.md), que define SEC01–SEC16 e INV01–INV12 como registros lógicos, no tablas ni permisos.

Preflight M02.02: PLAN H2241=PENDIENTE, I vacía, COLA G2241/O2241=SI y DAG F2202=VALIDADO/G2202=SI, bajo Scope Lock `STRICT` del SHA original. Un nuevo preflight y exclusividad preceden cualquier escritura. El objetivo de la fase es auditar JWT/RBAC/tenancy con pruebas reales **posteriores**, no introducir una API administrativa abierta.

El backend ASP.NET Core 10 ya administra autenticación JWT, autorización RBAC y filtros de empresa/tenant verificados. Un cliente Angular, encabezados, URL, slug, EmpresaId o `tenant_id` enviados no constituyen autoridad. Los contratos que siguen son **propuestas**, no rutas ni métodos disponibles. La auditoría institucional de las seis Tasks Javier no debe exponerse a usuarios comerciales mediante controllers de auditoría empresarial.

## 2. Operaciones de lectura hipotéticas, no endpoints implementados

| Código | Operación futura condicionada | Entrada confiable requerida | Salida conceptual | Negativo fail-closed |
| --- | --- | --- | --- | --- |
| OP01 | Solicitar catálogo de controles | Corte firmado, permisos plataforma | `SecurityControlPageV1` | Sin autoridad: 403 genérico |
| OP02 | Consultar decisión por recurso | Principal autenticado, recurso permitido | `AuthorizationEvidenceV1` | Recurso de tenant B desde A: 404 uniforme |
| OP03 | Ver matriz de aislamiento | Corte autorizado y alcance de pruebas | `TenantIsolationMatrixV1` | No listar otra empresa |
| OP04 | Consultar evidencia de CI | SHA verificado y job autorizado | `CheckEvidenceV1` | SHA o run no corresponde: 409 |
| OP05 | Enumerar hallazgos de seguridad | Scopes autorizados y corte estable | `SecurityFindingPageV1` | No ocultar P0/P1 por filtro |
| OP06 | Consultar retest causal | FindingId opaco, permiso de auditoría | `RemediationRetestV1` | Sin evidencia de retest: estado abierto |
| OP07 | Previsualizar diferencias | Revision actual y anterior autorizadas | `SecurityDiffPreviewV1` | `writesApplied=false` obligatorio |
| OP08 | Consultar A5 verificable | ID exacto y SHA de autoría | `IndependentReviewV1` | A5 propia o cuenta retirada: denegar |

Ninguna ruta se publica ni se registra en DI. Implementar cualquiera de estas operaciones posteriormente exige un gate M03 real, revisión de exposición pública, autorización backend y pruebas de no enumeración, JWT, RBAC, tenant A/B y regresión. El mero nombre DTO no constituye contrato binario.

## 3. DTO conceptuales y semántica de datos

| DTO conceptual | Campos mínimos ilustrativos | Prohibición de exposición |
| --- | --- | --- |
| `SecurityControlPageV1` | `controlKey, sourceRevision, nextCursor` | Tokens, reglas privadas de otros tenants |
| `AuthorizationEvidenceV1` | `resourceKeyRedacted, decision, policyRevision` | Principal/permiso de empresa ajena |
| `TenantIsolationMatrixV1` | `cutId, assessedCases, notEvaluatedCases` | Datos de empresas de prueba reales |
| `CheckEvidenceV1` | `headSha, checkName, status, conclusion` | Secrets de logs/runner |
| `SecurityFindingPageV1` | `findingId, severity, state, evidenceRefs, nextCursor` | PII y vulnerabilidades explotables |
| `RemediationRetestV1` | `findingId, patchSha, testSha, result` | Credenciales y payloads de ataque |
| `SecurityDiffPreviewV1` | `leftSha, rightSha, differences, writesApplied:false` | Mutaciones automáticas a PLAN/DAG |
| `IndependentReviewV1` | `microtaskId, authorTaskId, reviewerTaskId, originalSha, decision` | A5 inferida de etiqueta o propietario |

Los DTO deben tener paginación acotada, corte estable y minimización por defecto; resultados `NOT_EVALUATED`, `PENDING`, `FAIL` y `PASS` son estados diferentes y no intercambiables. Una futura lectura debe basarse en estado autenticado de PLAN/CI/BITACORA, no en parámetros de control del navegador.

## 4. Tabla de errores y decisiones de seguridad

| Error | HTTP propuesto | Significado seguro | Contraprueba |
| --- | --- | --- | --- |
| E01 | 400 | Entrada malformada o límite fuera de rango | No parsear SQL ni URL arbitraria |
| E02 | 401 | JWT ausente, inválido o revocado | No devolver evidencia |
| E03 | 403 | Falta permiso RBAC/plataforma verificable | No reconocer recurso restringido |
| E04 | 404 | ID no visible o fuera del tenant | Misma respuesta para ID ajeno/inexistente |
| E05 | 409 | Corte, SHA, lease o estado ya cambió | Releer fuentes, no fusionar evidencias |
| E06 | 409 | Dos owners disputan scope | No fingir CAS exitoso |
| E07 | 422 | DAG incompleto o A5 inválida | Nunca forzar G/O=SI |
| E08 | 422 | Prueba inconclusa, CI CANCELLED/SKIPPED/FAIL | No cerrar como PASS |
| E09 | 429 | Cuota de consultas excedida | No crear Task compensatoria |
| E10 | 503 | GitHub/Drive/Automations inaccesible | INDETERMINADO, no cero hallazgos |
| E11 | 503 | Check pertinente in_progress | Mantener VALIDANDO |
| E12 | 500 | Fallo no clasificado con correlación segura | No filtrar stack, tokens ni PII |

Las tablas son **contratos propuestos**, no códigos observados en una API existente. Las respuestas reales posteriores deben respetar las convenciones globales de API y los controles de no enumeración.

## 5. Validaciones y pruebas negativas futuras

| Caso | Comportamiento positivo futuro | Rechazo obligatorio |
| --- | --- | --- |
| V01 | Identidad JWT firmada y vigente | JWT expirado o sin firma |
| V02 | Tenant obtenido de contexto confiable | `tenant_id` de cabecera cambia permisos |
| V03 | RBAC por acción y objeto | Módulo visible en UI sin permiso API |
| V04 | Revocación de membresía efectiva | Sesión vieja sigue autorizada |
| V05 | Evidencia por SHA exacto | Check cancelled cuenta PASS |
| V06 | Cursor opaco ligado a permiso/corte | Cursor de tenant B usado por A |
| V07 | Hallazgo P0/P1 persiste sin retest | Reporte con cero por ausencia de scan |
| V08 | A5 firma Task distinta del autor | Autor firma su propia revisión |
| V09 | Error 404 uniforme evita enumeración | Código diferencia recurso ajeno |
| V10 | Rate limit y tamaño máximo acotados | Respuesta permite scraping masivo |
| V11 | Redacción de PII y secretos | URL/stack/log revela token |
| V12 | Rollback por commit aditivo y CI nueva | Force push elimina historial |

**V01–V12 = FUTUROS_NO_EJECUTADOS.** Los tests de seguridad y HTTP pertenecen a la futura implementación, no a un documento. No se afirma que las vulnerabilidades sean cero, ni que el aislamiento multiempresa haya sido certificado por esta especificación.

## 6. Auditorías A0–A5, consumidores y reversión

- **A0:** Scope Lock, fuente V5, M01.06/M02.01, OP01–OP08, ocho DTO, E01–E12, V01–V12, ausencia de secretos/encabezados Markdown vacíos.
- **A1:** consistencia positiva/negativa de contrato textual; ninguna prueba runtime ejecutada.
- **A2:** preservación explícita de JWT/RBAC/tenant A/B, no enumeración, permisos plataforma, redacción; código/API/seguridad real **sin modificar**.
- **A3:** contrastar PLAN/COLA/DAG y reglas M02.01, sin añadir endpoints o activar herramientas de administración.
- **A4:** commit fast-forward/CAS a dev, diff sólo dos Markdown, Scope Lock y procedencia V5 `completed/success` exact-SHA, readback y BITACORA idempotente.
- **A5:** revisión independiente posterior por una de las otras cinco Tasks Javier. No autoaprobación.

Consumidores futuros: M02.03 define persistencia conceptual, M02.04 configuración, M02.05 eventos diseñados, M02.06 salida terminal. Cualquier M03 requiere código real, pruebas dirigidas y DAG actualizado; M02.02 por sí solo NO habilita migraciones, OAuth/JWT nuevos, QA/main/PROD, pentest real ni ajustes de infraestructura.

**Rollback:** `git revert` normal en dev y evento de corrección append-only en BITACORA, sin force-push, reseteos, secretos, ni cambios de datos productivos.
