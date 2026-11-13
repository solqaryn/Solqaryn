# SOLQARYN V5 FINAL — C00-02.M02.02: contrato de interfaces, DTO, errores y compatibilidad

**Clase:** TIPO 1, diseño documental de la interfaz operativa para cola, dependencias, autoría, estados y cierre idempotente. **Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER, TASK_ID 6adc2e55f47c81919f6374e561687076. **Ámbito:** repositorio solqaryn/Solqaryn, rama dev; PROJECT_SCOPE_LOCK=STRICT.

**Fuentes ejecutables:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), Sheet institucional 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU; AGENTS.md, docs/PROJECT_SCOPE_LOCK.md, docs/AUTOMATION_STATUS.md, docs/AUTOMATION_AUTHORITY.md y docs/AUTOMATION_V13.md del HEAD dev. Complementa el modelo de entidades de C00-02.M02.01, configuración de M02.04 y eventos de M02.05.

**Resultado exigido de M02:** contrato de cola, dependencias, autoría, estados y cierre sin duplicación; esta M02.02 define interfaces y compatibilidad, pero NO implementa una API HTTP propia, endpoint C#, adaptador de Sheets, validación transaccional o dispatcher. El bloque previo C00-02.M01.06 constaba LISTO; PLAN H=PENDIENTE, I vacío y COLA G/O=SI al preflight. Sin regla DAG individual: aplicar puerta conservadora, sin liberar M03 ni lotes de cierre.

## 1. Fronteras y autoridades

| Superficie | Operación contractual | Fuente real / lectura obligatoria |
| --- | --- | --- |
| Catálogo | Obtener por ID inmutable la microtarea y RESULTADO_EXIGIDO | PLAN_MAESTRO A/E; exactamente 2.340 IDs |
| Estado y dueño | Leer o proponer cambio sujeto a autorización | PLAN_MAESTRO H/I; nunca editar COLA E |
| Elegibilidad | Consultar restricciones calculadas | COLA G/O y DAG_TECNICO A:G VALIDADO; no confundir fórmula con grant |
| Candidato | Asociar archivos, fingerprint, riesgos y resultados A0–A3 | Diff/blob SHA y autor original, antes de publicar |
| Publicación | Commit serializado fast-forward y SHA exacto | solqaryn/Solqaryn@dev; sin PR, rama nueva ni force-push |
| Verificación | Leer check-runs del SHA y distinguir success/failed/skipped/in_progress | GitHub GET /repos/solqaryn/Solqaryn/commits/{sha}/check-runs?per_page=100 |
| Auditoría | Append de eventos idempotentes con lectura posterior | BITACORA A:Z, 26 campos y EVENT_ID/IDEMPOTENCY_KEY únicos |
| Supervisión | Último reporte por Task, no motor de ejecución | TAREAS_PROGRAMADAS AA:AN; nunca fuente de estados de microtarea |

Estos son contratos lógicos de integración para herramientas autorizadas, no nombres de endpoints ya construidos. Los consumidores actuales son las seis Tasks Javier habilitadas y la documentación operativa. No se incorpora una nueva autoridad de ejecución ni un servicio de eventos.

## 2. DTO de lectura propuestos (no implementados)

**TaskSnapshotV5:** { microtarea_id: string, resultado_exigido: string, estado: PENDIENTE|EN_PROGRESO|VALIDANDO|LISTO, owner_task_id: string|null, dependencias: string[], cola_g: SI|NO, cola_o: SI|NO, dag_revision: VALIDADO|NO_REGISTRADO|NO_VALIDADO, source_head_sha: sha40, read_at_utc: ISO8601 }. microtarea_id debe coincidir con PLAN A y la fuente de estado es solo PLAN H; el propietario proviene exclusivamente de PLAN I. La ruta COLA E conserva su fórmula.

**ClaimProposalV5:** { microtarea_id, caller_task_id, expected_estado: PENDIENTE, expected_owner: null, expected_source_head_sha, resultado_clasificacion: TIPO1|TIPO2|TIPO3|TIPO4|TIPO5, file_impact: string[], evidence_preflight: string, idempotency_key: string }. No aceptar cuenta fuera del allowlist; validar integridad del DAG, permisos y prueba de ausencia de otro escritor. *Una lectura + batchUpdate de Sheets no constituye CAS atómico*: un reclamante debe releer/registrar colisión y jamás declarar exclusividad tecnológica que el backend no demuestra. Para relevos entre Tasks, CAS efectivo o NO_GO.

**CandidateV5:** { microtarea_id, owner_task_id, base_head_sha, files_changed, fingerprint, scope_security, rollback_plan, test_matrix }. Se registra VALIDANDO al existir candidato completo, ANTES de A0–A3. Una propuesta incompleta permanece EN_PROGRESO; el blob sin commit no equivale a publicación.

**AuditResultV5:** { audit: A0|A1|A2|A3|A4, status: PASS|FAIL|PENDIENTE, sha_or_fingerprint, checks: [{name,status,conclusion,head_sha,job_url}], positive_case, negative_case, rationale_not_applicable }. NO_APLICA se limita a subpruebas técnicas no relacionadas; no se usa para saltarse A0–A4. A5 requiere reviewer_task_id distinto de author_task_id y perteneciente a las otras cinco Tasks Javier.

**ClosureV5:** { microtarea_id, owner_task_id, published_sha, parent_sha, a0_a4_evidence, a5_state: A5_PENDIENTE, close_event_id, close_idempotency_key, verified_plan_estado: LISTO, verified_cola_estado: LISTO }. Nunca presentar el DTO como respuesta real hasta comprobar readback institucional y la CI causal exigible.

## 3. Validaciones y errores contractuales

| Código lógico (diseño, no respuesta HTTP existente) | Condición | Efecto seguro |
| --- | --- | --- |
| INVALID_ID | ID inexistente, duplicado o fuera de los 2.340 | Rechazar sin escribir filas |
| UNAUTHORIZED_ACTOR | TASK_ID, cuenta, acceso o repo ajenos al allowlist | Rechazar sin claim ni A5 |
| CLAIM_CONFLICT | PLAN I no vacío, H distinto de PENDIENTE o owner cambió | No sobrescribir ni tomar el archivo |
| DEPENDENCY_BLOCKED | COLA G/O no SI, prerequisito/DAG real incumplido | Mantener PENDIENTE; examinar otra candidata |
| CANDIDATE_INCOMPLETE | No hay diff/artefacto revisable o fingerprint | Mantener EN_PROGRESO |
| AUDIT_REQUIRED | Prueba causal requerida sin SUCCESS en SHA correcto | Mantener VALIDANDO y dueño; revisar otra tarea |
| CHECK_SHA_MISMATCH | Check de commit anterior o job SKIPPED/CANCELLED requerido | No acreditar PASS |
| EVENT_DUPLICATE | EVENT_ID o IDEMPOTENCY_KEY existentes | No agregar segundo evento |
| EVENT_SCHEMA_INVALID | Evento sin 26 campos A:Z o RUN_ID inventado | No registrar cierre ficticio |
| REF_MOVED | dev HEAD avanzó durante preparación | Rebasar el cambio aditivamente y volver a validar; no force-push |
| REVIEW_NOT_INDEPENDENT | Reviewer=autor u otro ID/cuenta no autorizado | No declarar A5_PASS |
| PROMOTION_NOT_AUTHORIZED | Intento de QA/main/PROD desde un PASS DEV | Denegar sin alterar ramas ni despliegue |

No exponer tokens, correos privados, claves JWT, cadena de conexión, PII o credenciales en errores, logs o la BITACORA. Errores de autenticación/tenancy se resuelven en el backend real con fail-closed; una clasificación de la cola nunca concede acceso al tenant A ni B.

## 4. Compatibilidad y versionado

1. La fuente V5 FINAL (2.340 IDs) es canónica; una nueva clase o interfaz no puede reabrir PR/FIFO histórico.
2. El esquema físico BITACORA A:Z contiene 26 columnas, no 21. El contrato acepta exclusivamente encabezados vigentes, EVENT_ID e IDEMPOTENCY_KEY únicos; los registros anteriores permanecen inmutables.
3. Los campos desconocidos en una eventual capa API no autorizan escribir: requerir versionado y mapping explícito. No renombrar PLAN H/I ni romper fórmulas COLA.
4. Los cuatro estados son los existentes. No introducir un quinto estado o A5_PASS automático.
5. CHECK_RUNS SHA exacto no es equivalente a combined status ni workflow PR. "Retry exact-HEAD" SKIPPED no invalida Scope Lock SUCCESS, pero jamás reemplaza check requerido.
6. El contrato documental no activa webhook, endpoint nuevo, worker, Docker, MySQL, jobs nuevos, cambios de tenant o autorización de despliegues.
7. Rollback: revert aditivo normal de este Markdown y su CHANGELOG, sin reescribir historia, fórmulas ni BITACORA.

## 5. Casos de aceptación del diseño (oráculos, NO pruebas runtime)

| Caso | Tipo | Entrada / expectativa |
| --- | --- | --- |
| P01 | Positivo | Snapshot para un ID real con PLAN H/I y COLA G/O coherentes → DTO atribuye fuente por campo y conserva owner |
| P02 | Positivo | Candidato documentado con A0–A3 pertinentes, commit FF y Scope Lock SUCCESS del mismo SHA → A4 y A5_PENDIENTE pueden registrarse con readback |
| P03 | Positivo | Reintento de lectura del mismo EVENT_ID no genera otro evento ni modifica propietario |
| N01 | Negativo | ID 2.341 o campo estado desconocido → INVALID_ID, ninguna escritura |
| N02 | Negativo | COLA O=SI pero G=NO o dependencia real pendiente → DEPENDENCY_BLOCKED |
| N03 | Negativo | PLAN I tiene otro dueño o H=VALIDANDO → CLAIM_CONFLICT, no duplicación |
| N04 | Negativo | Reviewer coincide con autor o procede de cuenta externa → REVIEW_NOT_INDEPENDENT |
| N05 | Negativo | Job required in_progress, skipped o check de otro SHA → AUDIT_REQUIRED |
| N06 | Negativo | Evento A:U de 21 campos o clave repetida → EVENT_SCHEMA_INVALID/EVENT_DUPLICATE |
| N07 | Negativo | SHA base cambió mientras se preparaba escritura → REF_MOVED, rehacer preflight |
| N08 | Negativo | Frontend intenta elevar permiso tenant o imprimir JWT en evidencia → UNAUTHORIZED_ACTOR y rechazo seguro |
| N09 | Negativo | Un actor solicita QA/PROD solo con LISTO_DEV → PROMOTION_NOT_AUTHORIZED |

Estas pruebas son lectura razonada de reglas documentales, NO evidencia de llamadas API, ejecución .NET, acceso MySQL o CAS provisto por Google Sheets.

## 6. Auditorías proporcionales, dependencia y reversión

**A0:** inspección de diff Markdown, consistencia con PLAN/COLA/DAG, 26 campos, secretos y ausencia de código ejecutable. **A1:** aceptación documental de la matriz P01–P03 y N01–N09, coherencia DTO/origen. **A2:** autorización por TASK_ID, revisión separada, fail-closed, límites de JWT/RBAC/tenant A/B, PII/secretos. **A3:** compatibilidad lógica con M02.01/M02.04/M02.05 y readback de fórmulas; sin runtime cambiado. Para dotnet/Node/Angular/MySQL/Docker/E2E la subprueba es NO_APLICA por este diff exclusivamente documental, no PASS técnico. **A4:** commit directo fast-forward, diff final y check verify-solqaryn-project-scope SUCCESS del SHA exacto si se publica, readback PLAN/COLA/BITACORA. **A5:** revisión independiente posterior por una de las otras cinco Tasks Javier.

No modificar ownership ajeno, eventos históricos, secretos, datos productivos, reglas de ramas ni infraestructura. Si un gate falla conservar VALIDANDO con evidencia y corregir dentro del scope.