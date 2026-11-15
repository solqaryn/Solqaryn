# SOLQARYN V5 FINAL — C15-01.M02.02: contratos API/DTO, errores y compatibilidad de auditoria de cobertura

**Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Ambito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. Es un contrato **DOCUMENTAL/PROPUESTO**, no agrega endpoints, DTO compilados, rutas, nuevos permisos, datos, servidores ni llamadas entre Tasks.

## 1. Fuentes y superficie real existente

Autoridad: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [M01.06](c15-01-m01-06-bloqueos-rollback-liberacion-cobertura.md) y [M02.01](c15-01-m02-01-modelo-entidades-ownership-trazabilidad-cobertura.md). El M02.01 define CT01–CT16, modelos logicos y oraculos CV01–CV12, NO implementados.

Revision del arbol real en dev: `backend/src/API/Controllers/AuditoriaController.cs` publica `GET auditoria` con `[Authorize]`, `[RequierePermiso(ModuloSistema.Auditoria, AccionPermiso.Ver)]` y comprobacion adicional `ICurrentUserService.EsAdministrador`; retorna `ApiResponse<PagedResult<RegistroAuditoriaDto>>`. `backend/src/API/Controllers/AutomatizacionesController.cs` gestiona preferencias y sugerencias comerciales, **no** el orquestador V5 de las seis Tasks. `backend/src/API/Controllers/PlataformaSesionController.cs` comprueba autoridad global desde MySQL sin usar cabeceras empresariales. Ninguno prueba la existencia de API V5 de cobertura: no reutilizar su autoridad ni exponer su contenido sin diseño y pruebas posteriores.

## 2. Contratos futuros de lectura (no rutas instaladas)

| Operacion | Proposito conceptual | Entrada propuesta | DTO de salida propuesto | Condicion negativa |
| --- | --- | --- | --- | --- |
| OP01 | Solicitar corte de cobertura | revision_fuente, sha_dev, cursor nullable, limite | CoverageAuditSnapshotV1 | Sin snapshot de fuentes consistente: 409, no conteo estimado |
| OP02 | Listar trazas de requisito | requirement_key, revision, cursor | RequirementTracePageV1 | Requisito sin origen autorizado: 404 generico |
| OP03 | Consultar microtarea exacta | microtask_id, source_revision | MicrotaskCoverageV1 | ID fuera del universo: 404, no crear fila |
| OP04 | Revisar aristas del DAG | microtask_id, sha_fuente | DependencyEvidenceV1 | G=NO no se convierte a SI por request |
| OP05 | Enumerar hallazgos | corte_id, clase, severidad, cursor | CoverageFindingPageV1 | No ocultar P0/P1 por filtro o paginacion |
| OP06 | Previsualizar reconciliacion | corte_id, fuente_esperada, dry_run=true | ReconciliationPreviewV1 | Prohibida escritura de PLAN/COLA/DAG |
| OP07 | Obtener evidencia | evidence_ref, commit_sha | EvidenceDescriptorV1 | No exponer tokens, PII, cabeceras o blobs ajenos |
| OP08 | Obtener estado A5 | microtask_id, origin_sha | IndependentReviewEvidenceV1 | Un texto A5_PASS sin revisor distinto no acredita cierre |

**DTOs propuestos:** `CoverageAuditSnapshotV1` {sourceRevision, devSha, observedAtUtc, universeCount, assessedCount, unknownCount, coverageStatus}; `RequirementTracePageV1` {items, opaqueNextCursor}; `MicrotaskCoverageV1` {microtaskId, state, ownerTaskId?, sourceKeys, evidenceRefs, a5State}; `DependencyEvidenceV1` {microtaskId, predecessorKeys, dagValidated, dagGate, queueGate, observedAtUtc}; `CoverageFindingPageV1` {findingId, kind, severity, ownerTaskId?, evidenceRefs, remediationStatus, opaqueNextCursor}; `ReconciliationPreviewV1` {sourceRevision, differences, proposedActions, writesApplied:false}; `EvidenceDescriptorV1` {uriRedacted, commitSha, blobSha, scope, kind}; `IndependentReviewEvidenceV1` {authorTask, reviewerTask, originSha, evidenceRef, result}. Los nombres son ejemplos de diseño, no tipos existentes en el binario.

Para una futura API, la ruta, el controlador, los DTO reales y cualquier permiso nuevo requieren nueva microtarea de implementación, pruebas de autorización/tenant y aprobación de arquitectura cuando corresponda. La fuente de `devSha`, estado, owner y A5 debe obtenerse de GitHub/PLAN/BITACORA autenticados, no de valores suministrados por el cliente. Las respuestas de cobertura son exclusivamente lectura o simulación sin escrituras. No convertir una API comercial existente en canal de control de Tasks.

## 3. Errores y validaciones de contrato

| Codigo | HTTP propuesto | Causa | Respuesta segura |
| --- | --- | --- | --- |
| E01 | 400 | Esquema malformado o limite de pagina fuera de rango | No hacer mutacion |
| E02 | 401 | Sesion ausente o vencida | No devolver datos auditables |
| E03 | 403 | Falta autoridad de plataforma y permiso verificable | Sin filtracion de existencia |
| E04 | 404 | ID o referencia inexistente dentro del scope autorizado | Mensaje uniforme sin enumeracion |
| E05 | 409 | Fuente, revision, SHA o token de corte cambiaron | Releer; no fusionar evidencias incompatibles |
| E06 | 409 | Owner/lease concurrente detectado | No suplantar CAS ni hacer claim implicito |
| E07 | 422 | Prerequisito DAG no validado o gate NO | No forzar G/O |
| E08 | 422 | A5 propia, sin blob, o review de Task retirada | Rechazar acreditacion |
| E09 | 429 | Presupuesto de consultas agotado | Reintento con cuota, no crear Task |
| E10 | 503 | GitHub/Drive/Automations inaccesible | Estado INDETERMINADO, no PASSED |
| E11 | 503 | CI requerida queued/in_progress/cancelled/failed | No certificar LISTO |
| E12 | 500 | Error interno sin correlacion emitible | Fail closed y log redacted |

Validaciones `VAL01–VAL10`: ID exacto no vacio y de formato V5; revision de fuente consistente; SHA Git de 40 caracteres; paginacion acotada; cursor opaco y ligado a corte/permisos; enum de severidad allowlisted; sin expresiones SQL ni rutas arbitrarias; redaccion de referencias con secretos; autoridad JWT/RBAC/plataforma obtenida del backend; error/correlationId no filtrante. Combinaciones inconsistentes fallan cerrado. No afirmar que estas validaciones ya corren en el backend.

## 4. Compatibilidad, seguridad y degradacion

- Se conserva el contrato real de `AuditoriaController`: no modificar su `GET`, ni rebajar permisos ni utilizarlo como puerta de entrada del V5. La auditoria comercial de empresa y la auditoria institucional de la cola son dominios distintos.
- Ningun request `EmpresaId`, `tenant_id`, slug, host, cookie o header concede autoridad. JWT/RBAC/tenant A/B se verifican en backend; PII, secrets, IP, correo y tokens se omiten o redactan; no exponer traces de otro tenant.
- Para errores de fuente externa, usar `INDETERMINADO` y evidencia temporal; `completed/success` exact SHA para CI requerida, no interpretar `queued`, `skipped`, `cancelled`, `failure` como PASS. Telemetria AA:AN es ultimo reporte y no servicio de streaming.
- Version `V1` conceptual admite futuras extensiones aditivas, con error uniforme, correlacion, filtro estable y cursor de corte fijo; ninguna accion publica cambio de PLAN H/I/K, DAG F/G ni COLA G/O. Nunca despertar Tasks por webhook no instalado.
- Sin nuevas rutas, migraciones, feature flags activadas o cambios de despliegue; Hostinger/Cloudflare son topologia objetivo, no servicios autorizados para provisionar aqui.

## 5. Matriz positiva/negativa futura

| Caso | Positivo propuesto | Negativo que obliga FAIL |
| --- | --- | --- |
| T01 | Microtask_id existente se resuelve exactamente en PLAN/COLA/DAG | Fila desplazada se trata como ID distinto |
| T02 | Revision y SHA coherentes fijan corte reproducible | Cambiar SHA durante paginacion produce 409 |
| T03 | Solo autoridad de plataforma verificada consulta hallazgos | Token empresarial o parametro tenant_id suplanta autorizacion |
| T04 | Permiso de auditoria restringe reportes sensibles | Permiso de dashboard expone auditoria completa |
| T05 | El paginado no pierde hallazgos P0/P1 | Cursor inconsistente oculta elementos |
| T06 | A5 exige TASK_ID diferente del autor y SHA correcto | Autoaprobacion o tarea Alex acredita PASS |
| T07 | CI completada success del SHA requerido se muestra PASS | Skipped/cancelled/failed se presenta PASS |
| T08 | El error 404 no filtra entidades de otra empresa | ID de tenant ajeno es enumerable |
| T09 | Previsualizar solo reporta diferencias | Previsualizar cambia columnas H/I/K |
| T10 | El API respeta cuota y retorno sin PII | Datos de session, secretos o tokens en logs |
| T11 | Hallazgo puede permanecer abierto hasta retest causal | Un documento declara cero defectos sin prueba |
| T12 | Falla de proveedor conserva resultado INDETERMINADO | Excepcion externa es interpretada como auditoria aprobada |

**T01–T12 son oraculos FUTUROS_NO_EJECUTADOS.** Ningun test HTTP, de DB, seguridad en vivo o paginacion fue ejecutado por este documento; tampoco se certificaron cero requisitos huerfanos, duplicados criticos o P0/P1.

## 6. Analisis de impacto, evidencia y rollback

- **A0:** contraste de Standard, M01.06, M02.01, Codigo real de tres controllers, ocho operaciones, doce errores, diez validaciones, doce pares de escenarios y alcance de diff exclusivamente documental.
- **A1:** coherencia de respuesta y casos positivos/negativos **documentales**; no PASS de comportamiento HTTP no probado. **A2:** seguridad de aislamiento en diseño; JWT/RBAC/tenant runtime NO_APLICA al diff Markdown. **A3:** contratos y nombres de rutas reales contrastados, no servicios existentes ni MySQL ejecutados.
- **A4:** commit fast-forward de Markdown y seccion indivisible de CHANGELOG, Scope Lock y V5 `completed/success` exact SHA y readback PLAN/COLA/BITACORA. Si check causal sigue queued/failed, conservar VALIDANDO.
- **A5:** revision independiente posterior por otra Task Javier con diff, blob, checks y autor. No permite QA/main/PROD, eventos, migraciones ni elevacion de privilegios.
- **Rollback:** `git revert` normal en dev con nuevo SHA, nueva CI y BITACORA append-only; no force-push ni alteracion de historia. El futuro M02.03–06 mantiene su DAG y pruebas propias.
