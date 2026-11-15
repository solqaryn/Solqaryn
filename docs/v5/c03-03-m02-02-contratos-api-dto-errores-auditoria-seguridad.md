# SOLQARYN V5 FINAL — C03-03.M02.02: API, DTO, errores y compatibilidad de auditoría

**Entrega documental:** no modifica endpoints ni certifica runtime. **Autor:** PRIMARY DEV 02 — JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Rama:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO y Skill canónica. **Predecesor:** C03-03.M01.01–06 LISTO+A5, DAG F=VALIDADO/G=SI.

## 1. Objetivo y fronteras verificables

Definir contratos futuros para registrar accesos permitidos/denegados, cambios críticos, accesos excepcionales y correlación sin credenciales ni exposición entre tenants. La API existente contiene `AuditoriaController` con `[Authorize]`, `Auditoria.Ver` y condición administrativa; `AuditoriaService` distingue `RegistrarAsync` (best-effort) de `RegistrarEstrictoAsync` (propaga fallos); `RegistroAuditoria`, `AuditoriaRepository` y `CorrelationIdMiddleware` fueron inventariados en M01.03. Este inventario **no prueba** filtro A/B en HTTP, redacción universal de before/after ni retención real. `EmpresaId=null` no implica permiso global. Preservar respuestas actuales o introducir versionado explícito con revisión de consumidores antes de cambios de código.

## 2. Operaciones propuestas (NO implementadas por esta entrega)

| ID | Contrato | Control de acceso y seguridad |
| --- | --- | --- |
| API01 | Consulta empresarial existente `GET /auditoria`, futura proyección mínima paginada | JWT vigente, membresía tenant y `Auditoria.Ver` verificados en backend; no filas B |
| API02 | Detalle opaco por ID, ruta por decidir | No enumerar registros ajenos; 403/404 seguro según contrato vigente |
| API03 | Puerto interno de registro de decisión autorizada/denegada | Actor, acción, recurso y tenant resueltos por servidor, nunca por navegador |
| API04 | Puerto interno de registro estricto de cambio crítico | Fallo de persistencia obliga rollback/denegación; best-effort no es durable |
| API05 | Lectura excepcional aprobada de metadatos | Permiso de plataforma separado, MFA/reauth, motivo, TTL, doble aprobación |
| API06 | Consulta de metadatos de política de retención | Sin política aprobada no hay purga/exportación |
| API07 | Recuperación restringida por correlación saneada | Correlation ID no autentica, autoriza ni garantiza idempotencia |
| API08 | Exportación futura controlada, actualmente DENY | Requiere grant autónomo, filtros server-side, paginación/límites y pruebas A/B |

Las operaciones API02–API08 son **propuestas**, no rutas actuales afirmadas. No se crean controladores, workers, webhooks, tablas, EF, políticas, claims ni permisos.

## 3. Campos DTO conceptuales y fuente de confianza

| Campo | Regla |
| --- | --- |
| `eventId` | ID opaco; no usar secuencia visible ni como grant |
| `actorRef` | Pseudónimo generado del sujeto autenticado; no serializar JWT, email, credenciales |
| `tenantRef` | Del contexto server-side y ownership persistido; no confiar en header |
| `actionCode`, `resourceType` | Catálogo versionado, sin texto ejecutable ni inventar grants |
| `resourceRef` | Opaco, misma empresa; prohibido BOLA/IDOR |
| `outcome` | allowed/denied/error como evidencia histórica, nunca autoridad futura |
| `occurredAtUtc` | Marca UTC generada por servidor |
| `correlationId` | ASCII seguro ≤64 o GUID fallback; diagnóstico, no idempotencia |
| `reasonCode` | Código seguro allowlisted, sin payloads secretos |
| `policyVersion` | Política efectivamente aplicada; nunca asumir grant actual |
| `changeDigest` | Hash o proyección minimizada, no before/after arbitrarios |
| `pageCursor`, `pageSize` | Ligados a tenant/filtros/versión, límite de tamaño y orden estable |
| `retentionClass` | Metadato hipotético, no borra ni archiva |
| `writeMode` | strict/best_effort decidido en backend, jamás por cliente |

Los campos son contratos abstractos, NO clases C# o esquemas MySQL implantados.

## 4. Contrato de errores seguro

| Código | Resultado esperado en implementación posterior |
| --- | --- |
| E01 UNAUTHENTICATED | 401 sin existencia de recurso |
| E02 ACTION_FORBIDDEN | 403 sin revelar grants |
| E03 RESOURCE_NOT_VISIBLE | 403/404 opaco ante ID tenant ajeno |
| E04 TENANT_CONTEXT_INVALID | Rechazar header/filtro B con identidad A |
| E05 INVALID_AUDIT_FILTER | 400, validación tamaño/cursor/filtro |
| E06 AUDIT_STRICT_WRITE_FAILED | 5xx + reversión del cambio crítico |
| E07 AUDIT_STORAGE_UNAVAILABLE | Error genérico ante timeout, sin stacktrace |
| E08 EXCEPTION_GRANT_DENIED | 403 sin MFA/reauth/aprobador separado |
| E09 EXCEPTION_GRANT_EXPIRED | 403 ante TTL vencido/revocado |
| E10 RETENTION_POLICY_UNAPPROVED | Rechazar purga sin autoridad aprobada |
| E11 CORRELATION_INVALID | Saneamiento o rechazo consistente con middleware |
| E12 EXPORT_SCOPE_EXCEEDED | Rechazar exportación masiva o cross-tenant |
| E13 EVENT_SCHEMA_UNKNOWN | Denegar/aislar, no descartar auditoría obligatoria |
| E14 AUDIT_RETRY_CONFLICT | 409 seguro; correlation ID no es clave idempotente |

Nunca enviar PII, JWT, contraseñas, connection strings ni valores históricos privados en errores, motivos, logs o export.

## 5. Oráculos positivos y negativos FUTUROS_NO_EJECUTADOS

| Caso | + Positivo exigido | - Negativo exigido |
| --- | --- | --- |
| T01 | Admin A autorizado obtiene únicamente eventos A paginados | JWT B u operador sin Auditoria.Ver no consulta A |
| T02 | Cambio crítico y auditoría estricta guardan commit único | Fallo DB en auditoría estricta impide efecto sin evidencia |
| T03 | Correlación ASCII válida se propaga como diagnóstico | CRLF/control/65 caracteres no contaminan log |
| T04 | Denegación se registra con reasonCode minimizado | JWT/contraseña en old/new no persiste ni se exporta |
| T05 | Acceso excepcional con MFA y aprobador ajeno audita TTL | Sin MFA, revocado o autoaprobado se rechaza |
| T06 | Cursor acotado preserva tenant/filtros | Cursor B o pageSize desmedido no filtra datos A |
| T07 | Error desconocido mantiene respuesta neutral | ID B y `EmpresaId=null` no conceden acceso global |
| T08 | Retención aprobada aplica a clase/fecha permitida | Solicitud sin política legal no elimina ni exporta nada |

Estos son **ocho pares de escenarios futuros**, no 16 tests ejecutados. M03/M05 requerirán HTTP, MySQL efímero 8.4, JWT/RBAC real y regresión tenant A/B; mocks y documentos no certifican esas propiedades.

## 6. A0–A4 documental, A5 y rollback

- **A0:** preflight de fuente única, M01.06+A5, diff de este documento y sección aditiva de CHANGELOG, sin secretos.
- **A1:** ocho operaciones, campos DTO, 14 errores y ocho pares positivo/negativo cotejables documentalmente. No resultados de ejecución.
- **A2:** aislamiento A/B, denegación por defecto, no exposición, separación plataforma/tenant, MFA, auditoría estricta y correlación no autenticante.
- **A3:** compatibilidad prevista API → Application → Domain → Infrastructure con `AuditoriaController`, `AuditoriaService`, `RegistroAuditoria`, `AuditoriaRepository`, `CorrelationIdMiddleware`. Builds .NET/Angular, MySQL y Playwright **NO_APLICA a diff exclusivamente Markdown**, jamás PASS artificial.
- **A4:** commit fast-forward/CAS contra HEAD exacto, revisar blobs/diff y check-runs `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA publicado, BITACORA A:Z y PLAN/COLA/DAG readback.
- **A5:** revisión independiente por otra de las cinco Tasks Javier, diferente de DEV02. LISTO_DEV no autoriza QA/main/PROD.

**Reversión:** `git revert` normal, sin reescritura de historia ni borrado de BITACORA. Ningún cambio runtime, SQL, IAM, eventos, Cloudflare, proveedores ni producción.
