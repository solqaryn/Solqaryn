# SOLQARYN V17 — C13-02.M02.03: persistencia conceptual, índices y fixtures de accesibilidad

**Scope:** C13-02.M02.03 (PLAN_MAESTRO fila 1918). **Autor:** PRIMARY DEV 06, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Riesgo:** R0 documental; cualquier modificación futura de EF Core/MySQL se clasifica R2. Repositorio `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fuente única de la ejecución: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (V5 FINAL + V17); calidad V15.

## Objetivo, decisión y no-alcance

Definir un **contrato conceptual, no ejecutable**, que permita conservar en el futuro evidencia reproducible de teclado, foco, formularios, zoom/reflow, contraste, lector, permisos, accesibilidad y regresión, con separación por tenant y decisiones normativas explícitas. Los términos tablas/índices/fixtures son **propuestas** sujetas a M03/autoridad; no son objetos creados ni migraciones ejecutadas. No se modifican clases Domain/Application/Infrastructure/API, `AppDbContext`, migraciones, scripts SQL, Angular, CSS, JWT/RBAC, proveedores, datos ni despliegues. No se afirma WCAG 2.2 AA, certificación legal o existencia de un servicio de evidencias.

Predecesores documentales: `C13-02.M01.01–06` y revisión A5 independiente de M01.06, más `C13-02.M02.01` (modelo conceptual de 16 entidades/20 invariantes; fuente `docs/v5/c13-02-m02-01-modelo-conceptual-accesibilidad.md`; SHA `71a5f2c73ae7bafeb608d398c0548a93cbc35e20`). M02.02/M02.04/M02.05 son contratos **independientes** sujetos a sus propios owners, DAG y A5. El caso actual no crea ninguna relación de despliegue con ellos.

## Propuesta de persistencia futura — sin DDL

| Registro conceptual | Identificador y relación | Separación y retención |
| --- | --- | --- |
| SurfaceVersion | (surface_key,revision), module_key, route_pattern | `visibility` PUBLIC_GLOBAL o TENANT_SCOPED; no persistir URL con tokens |
| RequirementApplicability | (surface_key,revision,requirement_key,standard_version?) | decisión de autoridad y vigencia, sin atribuir cumplimiento |
| EvaluationCase | (case_key,version), surface_key, expected_result, class P/N | caso diseñado ≠ ejecución real; P/N FUTUROS_NO_EJECUTADOS |
| EvaluationRun | run_key UUID, commit_sha, suite_version, environment, timestamp_utc | tenant_id obligatorio para superficies privadas; proveedor/CI solo si evidencia existe |
| ExecutionContext | run_key, actor_role, tenant_key?, permission_revision, revoked | almacenar actor pseudónimo y nunca JWT/cookie/secretos |
| TestObservation | (run_key,case_key,attempt), observed_result, status, digest? | separar PASS/FAIL/NOT_RUN; PASS requiere evidencia real |
| Finding | finding_key, surface_key, severity, state, reproduction_ref | datos privados redactados y ownership explícito |
| VisualCondition | run_key, viewport, zoom_percent, theme, forced_colors, reduced_motion | medición vinculada al caso; 320/390/768/1280 son objetivos futuros |
| AuditDecision | decision_key, authority, effective_utc, expiry_utc?, reason | excepciones deben tener vigencia y revisión distinta |
| EvidenceArtifact | artifact_key, run_key, checksum, storage_key, pii_redaction_status | archivo binario futuro sólo con proveedor y permisos reales aprobados |
| GateVerdict | surface_key, revision, gate_key, status, evidence_refs | correlaciona G01–G16 de M01.06; ningún PASS por herencia |
| RollbackCheckpoint | change_sha, surface_key, regression_ref, rollback_status | `git revert` convencional y posterior revalidación |

Las relaciones propuestas son: `SurfaceVersion 1:N EvaluationCase`; `EvaluationRun 1:N TestObservation`; `EvaluationCase 1:N TestObservation`; `EvaluationRun 1:N VisualCondition/EvidenceArtifact`; `SurfaceVersion N:M RequirementApplicability`; `Finding N:M TestObservation` por referencia conceptual. `AuditDecision` no cambia permisos reales. `tenant_key` de una ruta pública es NULL sólo cuando no relaciona datos privados; una ruta empresarial requiere tenant válido y verificación server-side.

## Índices, unicidad y transacciones futuros

- **K01:** `UNIQUE(surface_key,revision)`; dos revisiones no reemplazan la evidencia histórica de otra.
- **K02:** `UNIQUE(case_key,version)`; un caso diseñado es inmutable para una ejecución ya cerrada.
- **K03:** `UNIQUE(run_key,case_key,attempt)`; registrar reintentos sin duplicar PASS.
- **K04:** `INDEX(tenant_key,surface_key,created_utc)` sólo si la cardinalidad real y privacidad lo justifican en migración R2.
- **K05:** `INDEX(commit_sha,run_started_utc)` para correlación causal, sin asumir que un commit confirma un test.
- **K06:** `UNIQUE(artifact_key)` y digest contenido con verificación de integridad; no almacenar JWT, cookies o PII.
- **K07:** `INDEX(finding_key,state,severity)` con filtros tenant en repositorio (nunca un índice como autorización).
- **K08:** `UNIQUE(decision_key,effective_utc)` y expiración; prohibir excepción tácita perpetua.
- **K09:** idempotencia por `(run_key,case_key,attempt)`; replay no muta observación ya certificada.
- **K10:** transacción futura: `EvaluationRun + TestObservation + AuditTrail` deben confirmar/abortar juntos o reflejar un estado de recuperación inequívoco.
- **K11:** claves foráneas a superficies/run/casos impiden observaciones huérfanas; borrado físico de historial requiere autorización separada.
- **K12:** todos los índices/constraints son hipótesis: medir selectividad, locks, colación MySQL y longitud real antes de DDL.

Ninguna clave, índice o FK anterior es una migración aprobada. No mezclar tenant de actor con tenant de recurso por convención visual; la autorización reside en .NET y debe fallar cerrado incluso cuando la UI se manipula.

## Plan de evolución reversible futuro (R2, NO EJECUTADO)

1. **MIG-01 preflight:** obtener autorización específica de la microtarea M03, snapshot/backup verificable, tabla de compatibilidad EF Core 10.0.12 y Oracle MySql.EntityFrameworkCore 10.0.9, baseline A/B, jobs MySQL 8.4 **efímero**. Sin estas puertas no ejecutar DDL.
2. **MIG-02 aditivo:** crear registros e índices nuevos en un servicio de prueba aislado, nunca cambiar o borrar tablas existentes en el primer paso. Versionar nombre, colación, constraints y checksum.
3. **MIG-03 compatibilidad:** ejecutar pruebas de escritura/lectura multiempresa, roles revocados, rollback de transacción y violación de FK/UNIQUE en MySQL real efímero. `Category=Integration` debe aparecer en logs con número PASS/FAIL; build o mocks no equivalen a integración.
4. **MIG-04 despliegue futuro:** no autorizado por esta unidad; si se autorizara, migración expand/contract con observabilidad, gates y evidencia de restauración, sin datos productivos ni secretos en CI.
5. **MIG-05 reversa:** rollback de código mediante `git revert`, revert de esquema sólo bajo plan autorizado sin destruir evidencia legítima; comprobar restauración y checks de consulta tenant A/B.
6. **MIG-06 cierre:** registrar SHA de migración, checks exactos, fixtures, latencia y pruebas A0–A5 independientes; estado `NO_VERIFICADO` hasta ejecución real.

## Fixtures y 12 pares P/N futuros, actualmente NO_EJECUTADOS

Definir fixtures sintéticos con empresas **TENANT_A** y **TENANT_B** aisladas; usuarios `A_OPERADOR`, `B_OPERADOR`, `A_REVOCADO`; superficies pública `PUBLICA` y privada `A_PRIVADA`; matriz de viewport 320/390/768/1280; casos de navegador sin PII, cookies ni datos reales. La autoridad WCAG/lector queda pendiente y ningún fixture equivale a prueba ejecutada.

| Caso | Positivo que habrá que demostrar | Negativo que deberá provocar FAIL |
| --- | --- | --- |
| P01/N01 | escritura de observación A recuperable para A | lectura o escritura A↔B mediante IDOR |
| P02/N02 | consulta permitida según RBAC vigente | actor revocado consulta evidencia privada |
| P03/N03 | duplicado de evento idempotente no duplica | replay crea un segundo PASS o auditoría |
| P04/N04 | restricción única detecta duplicado | mismo run/case/attempt se guarda doble |
| P05/N05 | FK preserva run y caso válidos | observación huérfana acepta referencia inválida |
| P06/N06 | transacción completa atómica | falla tras observación y deja auditoría incompleta |
| P07/N07 | evidencia digest coincide con artifact | checksum manipulado conserva PASS |
| P08/N08 | retención y redacción sin secreto | artifact expone JWT, cookie, email o dato B |
| P09/N09 | público sólo usa contexto no empresarial | público lee recurso TENANT_SCOPED |
| P10/N10 | viewport/zoom/reflow queda trazable | 320px sin evidencia recibe PASS |
| P11/N11 | rollback ensayado restaura baseline | reversa destruye evaluación válida de otro tenant |
| P12/N12 | CI exact SHA y Category=Integration identificables | SKIPPED/CANCELLED/IN_PROGRESS se cuenta PASS |

**P01–P12/N01–N12 se diseñan aquí, no se ejecutan.** Los resultados efectivos de navegador, MySQL, API, idempotencia y seguridad deben registrarse en sus microtareas futuras con entorno/versiones, instrucciones, fixtures, log/artefacto y SHA causal. No declarar `PASS` de pruebas inexistentes.

## Seguridad, observabilidad, A0–A5 y rollback

**Seguridad R0:** aplicar tenant A/B, RBAC, revocación, IDOR y GDPR/PII como restricciones conceptuales; nunca certificar JWT/HTTP o aislamiento de DB por documento. Toda clave de acceso futura fuera de Git; los artifacts con evidencias deben estar redactados, con TTL y autorizaciones explícitas.

**A0:** requisitos PLAN/COLA/DAG y fuente M01/M02.01, contrato de persistencia/índices/fixtures, diff exacto Markdown + encabezado/viñetas contiguas CHANGELOG.

**A1:** cotejar documentalmente doce relaciones de pruebas P/N, unicidad, FK y reversión; se declara FUTURO_NO_EJECUTADO, no positivo/negativo automático real.

**A2:** revisar estáticamente aislamiento, ownership, revocación, redacción, secretos y falta de autorización implícita.

**A3:** `NO_APLICA` ejecución EF/MySQL 8.4, .NET/API, Angular/Playwright y proveedores porque este diff no contiene código, DDL, esquema o infraestructura. En M03 sí se exigirá integración MySQL efímera y regresiones reales.

**A4:** commit `dev` fast-forward con CAS, dos Markdown (documento y CHANGELOG), Scope Lock exact SHA `completed/success`, check V5 procedencia `completed/success`, readback PLAN/BITACORA; demás CI no causal sin PASS ficticio.

**A5:** independiente por una Task Javier diferente a la autora, lectura propia de artefactos/pruebas/límites; puede registrar A5_FAIL. No autopase, no desatar M03/M04 por este contrato.

**Rollback:** `git revert` normal sobre commit documental, reconciliar HEAD y registrar evento aditivo; nunca force-push, reset, borrado de BITACORA o modificación QA/main/PROD/Aiven/infraestructura.

**Resultado exigido:** contrato conceptual de persistencia e índices reversible y trazable para accesibilidad, sin implementar esquema, datos ni certificaciones.
