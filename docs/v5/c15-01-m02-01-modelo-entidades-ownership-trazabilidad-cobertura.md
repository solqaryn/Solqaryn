# SOLQARYN V5 FINAL — C15-01.M02.01: modelo de propiedad y trazabilidad de cobertura

**Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT — TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Alcance:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. Contrato **DOCUMENTAL** M02.01, no despliegue, migracion, endpoints, workers, ni ejecucion del censo definitivo.

## 1. Autoridad y alcance

La unica especificacion ejecutable es [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). El [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) contiene PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA. El plan declara 16 bloques, 65 fases, 390 microfases y 2340 microtareas. Esa cantidad NO demuestra cero requisitos huerfanos ni duplicados criticos.

Prerrequisito: C15-01.M01.06 esta LISTO con A5 independiente documentada; [contrato de bloqueos M01.06](c15-01-m01-06-bloqueos-rollback-liberacion-cobertura.md). Preflight M02.01: PLAN H2168=PENDIENTE / I vacia, COLA G2168/O2168=SI, DAG A2129=C15-01.M02.01, F2129=VALIDADO/G2129=SI. Solo un claim condicional de estado y owner con readback permite empezar. La telemetria AA:AN es ultimo reporte, no lock ni actividad nativa.

## 2. Entidades conceptuales y fuentes

| Codigo | Registro logico | Clave | Fuente | Invariante |
| --- | --- | --- | --- | --- |
| CT01 | RequirementSource | source_revision + requirement_key | STANDARD V5 | Requisito versionado; fuente no se reescribe |
| CT02 | Occurrence | source_revision + occurrence_path | STANDARD V5 | Una ocurrencia puede vincularse a varios IDs con justificacion |
| CT03 | PlanItem | microtask_id | PLAN_MAESTRO A:H | 2340 IDs declarados; estado H es autoritativo |
| CT04 | ActiveOwner | microtask_id + TASK_ID | PLAN I / BITACORA | Maximo un owner activo por claim CAS |
| CT05 | PhaseMembership | block + phase + microphase + ID | STANDARD y PLAN | No inferir membresia por numeros adyacentes |
| CT06 | CoverageEdge | requirement_key + microtask_id + evidence_ref | Matriz de auditoria futura | Relacion N:M con evidencia explicita |
| CT07 | TechnicalDependency | predecessor_id + successor_id | DAG_TECNICO A:G | F=VALIDADO no basta sin gate G=SI |
| CT08 | QueueProjection | microtask_id + read_at | COLA G/N/O | Formula derivada nunca alterada para forzar SI |
| CT09 | AuditEvent | EVENT_ID + IDEMPOTENCY_KEY | BITACORA A:Z | Unicidad y 26 campos; historial inmutable |
| CT10 | GitEvidence | commit_sha + blob_sha | GitHub dev | Diff, parent y procedencia en SHA publicado |
| CT11 | CheckEvidence | head_sha + check_name | GitHub Actions | Exigir completed/success del SHA aplicable |
| CT12 | A5Review | microtask_id + author_task + reviewer_task | PLAN K y BITACORA | Revisor distinto del autor, Javier allowlist |
| CT13 | Finding | finding_id + severity + proof | Auditoria futura | Huerfano, duplicado critico o brecha no se oculta |
| CT14 | Remediation | finding_id + retest_sha | Auditoria futura | Correccion causal y repeticion de prueba |
| CT15 | RunSnapshot | TASK_ID + observed_at + native_run_id | Automations.peek | Clave LOCAL nunca suplanta run nativo |
| CT16 | ReleaseAuthority | environment + explicit_grant | Gobierno SOLQARYN | LISTO_DEV/A5 no autorizan QA/main/PROD |

Relaciones: CT01 1:N CT02; CT02 N:M CT03 mediante CT06; CT03 0:1 CT04 activo y 1:N CT09; CT03 N:N CT03 mediante CT07, sin ciclos; CT10 1:N CT11 con SHA exacto; CT12 conserva autor, revisor y blob de la entrega original. Son entidades LOGICAS, no se propone crear tablas MySQL por este documento. No se presume atomicidad distribuida GitHub/Sheets/Tasks.

## 3. Reglas de ownership y conciliacion

1. Unir STANDARD, PLAN, COLA y DAG por microtask_id exacto, no por numero de fila; cada ocurrencia documental necesita identificador de origen y vinculo explicito a microtarea. Una ausencia crea hallazgo, no permiso de crear backlog nuevo.
2. Separar requisito sin ID, ID sin implementacion demostrada y duplicado semantico: una repeticion textual no es automaticamente colision critica. Cada excepcion requiere criterio, prueba, severidad y responsable autorizado.
3. PLAN H/I mandan sobre estado y owner; COLA G/N/O y DAG F/G son observaciones reactivas. No forzar formulas ni liberar un sucesor dependiente de VALIDANDO, CI fallida o A5 faltante. Solo documentos preparatorios expresamente habilitados pueden avanzar con evidencia limitada.
4. BITACORA agrega unicamente eventos nuevos con EVENT_ID e IDEMPOTENCY_KEY distintos, los 26 campos A:Z y readback. No reescribir historia ni presentar un RUN_ID local como nativo.
5. Un rescate requiere comprobar falla/terminacion, ausencia de nueva corrida activa, lease, trabajo simultaneo y CI en curso; si no hay CAS atomico disponible para el relevo, no transferir owner.
6. Los seis TASK_ID Javier son el unico allowlist operativo; A5 de autor distinto y revision exact-SHA. Ningun documento confiere privilegios de tenant, JWT/RBAC, MySQL, DNS, QA/main/PROD o ejecucion de webhooks.

## 4. Oraculos positivos y negativos de auditoria posterior

| Control | Positivo futuro exigible | Negativo que impide certificacion |
| --- | --- | --- |
| CV01 | IDs de PLAN se unen por coincidencia exacta con la fuente | ID duplicado, ausente o sin fuente |
| CV02 | Cada enlace requisito-ID conserva motivo, revision y evidencia | Correspondencia inferida solo por similitud textual |
| CV03 | Un solo owner con CAS y readback por ID | Dos autores reclaman simultaneamente |
| CV04 | A5 firma TASK_ID distinto del autor y permitido | Autoaprobacion o cuenta retirada |
| CV05 | CI requerida completed/success exact-SHA | FAIL/CANCELLED/SKIPPED/IN_PROGRESS usados como PASS |
| CV06 | DAG y COLA respetan prerequisitos autenticos | SI forzado o bloqueo terminal saltado |
| CV07 | BITACORA evento unico 26 campos append-only | Reescritura o duplicado de idempotency key |
| CV08 | Git commit fast-forward serializa CHANGELOG | Sobreescritura de seccion ajena o reset |
| CV09 | Hallazgo P0/P1 queda abierto hasta retest | Ausencia de reportes tratada como cero defectos |
| CV10 | Seguridad tenant A/B, JWT/RBAC exige prueba real al cambiar runtime | Documento presentado como bypass de prueba |
| CV11 | Reporte local y estado nativo de Task estan diferenciados | Heartbeat textual considerado actividad continua |
| CV12 | Prohibidos eventos y migraciones sin nueva autorizacion | Modelo documental ejecutado como codigo |

**CV01–CV12 son contratos de prueba FUTUROS_NO_EJECUTADOS.** El preflight de un ID, las lecturas del plan y la CI documental no son auditoria exhaustiva de 2340 filas ni certifican cero huerfanos, cero P0/P1, cero duplicados criticos, cero carreras o pruebas de seguridad multiempresa runtime.

## 5. Impacto tecnico, A0–A5 y rollback

- **Consumidores:** M02.02 define futuros contratos API/DTO, M02.03 persistencia conceptual, M02.04 configuracion, M02.05 eventos como diseno y M02.06 gate documental. Cada uno exige nuevo preflight de PLAN/COLA/DAG. Ninguna dependencia posterior se promueve por este contrato.
- **Cambios previstos:** solo este Markdown y seccion autocontenida de CHANGELOG_AI.md. No se modifica .NET 10, Angular 22.2.1, EF/MySQL 8.4, infraestructura, codigo, API, tablas, datos, secretos, tests existentes, JWT, RBAC, empresas A/B o automatizaciones fisicas.
- **A0:** revision de M01.06, fuente V5, tablas CT y CV, exclusiones y diff. **A1:** coherencia documental positiva/negativa; pruebas de CV FUTURAS. **A2:** sin cambio de autorizacion, tenant ni PII; pruebas HTTP NO_APLICA al diff Markdown. **A3:** referencia cruzada del contrato M01.06 y PLAN/COLA/DAG; builds y MySQL/Playwright NO_APLICA, nunca PASS ficticio.
- **A4:** commit fast-forward con parent/HEAD y Scope Lock + V5 completados con success en el SHA del cambio, readback de PLAN/COLA/BITACORA. Si CI sigue pending/failed, mantener VALIDANDO. **A5:** pendiente por Task Javier distinta de DEV06, sin autoaprobacion.
- **Rollback:** git revert normal en dev, nuevos checks y evento aditivo; ni force-push ni edicion historica. Estado de cobertura total: NO_CERTIFICADO hasta auditoria con matriz completa, pruebas reproducibles y retest de hallazgos.
