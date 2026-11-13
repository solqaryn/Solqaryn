# SOLQARYN V5 FINAL — C00-03.M02.02: contratos, DTO, errores y compatibilidad de cobertura integrada

**Entrega:** diseño documental TIPO 1, no endpoint ni implementación. **Autor:** PRIMARY DEV 05 — JAVIER, TASK_ID 6adc2e55f47c81919f6374e561687076. **Repositorio:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Fuentes:** Standard de Implementación V5 FINAL (Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA del Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU, gobernanza canónica y código HEAD dev.

**Resultado exigido M02:** convertir cada requisito previo en mejora, contrato, prueba y criterio de cierre dentro del plan V5. El ID M02.02 define interfaces, DTO lógicos, errores, validaciones y política de compatibilidad **de la matriz de cobertura**, no API HTTP comercial nueva. Se apoya en C00-03.M01.06 (matriz integrada R01–R12, LISTO), C00-03.M02.01 (identidades/relaciones), y C00-02.M02.02 (interfaces de la cola). No duplica el contrato de despacho ni crea microtareas nuevas. Preflight: PLAN H=PENDIENTE I vacío, COLA G=SI/O=SI, M01.06 LISTO, DAG sin regla específica M02.02; puerta conservadora y no autorización de M03–M06.

## 1. Frontera y contratos lógicos, no implementados

| Operación conceptual | Entrada validable | Salida y dueño |
| --- | --- | --- |
| LeerFuente | source_id, source_version, provenance | Requisito de V5 FINAL, no un plan histórico ejecutable |
| LeerMejora | microtask_id que exista entre 2.340 | PLAN_MAESTRO A/E/H/I y prerequisitos, sin inferir cumplimiento |
| ProponerCorrespondencia | source_id, microtask_id, contract_ref, test_ref, evidence_ref | CoverageCandidate tipado con estado propuesto, sin escribir automáticamente PLAN |
| ConsultarCobertura | source_id o microtask_id, revisión fuente | CoverageSnapshot con trazas versionadas, evidencia y faltantes explícitos |
| ValidarDependencias | microtask_id, HEAD dev, G/O, DAG y precedente | Elegibilidad reportada, no un claim ni concesión de permisos |
| CertificarEvidencia | fingerprint, SHA, check-runs pertinentes, readback | Informe A0–A4 por resultado exigido, sin A5 fingido |
| SolicitarRevision | SHA publicado, autor TASK_ID, reviewer diferente | A5_PENDIENTE; A5_PASS/FAIL solo por Task Javier autorizada distinta |
| RegistrarEvento | EVENT_ID, IDEMPOTENCY_KEY, 26 campos | BITACORA A:Z append-only, no truncar A:U |
| ConsultarTelemetry | TASK_ID, snapshot de TAREAS_PROGRAMADAS AA:AN | Último reporte, no streaming ni autoridad de PLAN H/I |

Estas interfaces son documentación para un posible consumidor futuro, **no rutas, clases, tablas ni llamadas REST existentes**. Las operaciones READ tienen fuente explícita; las propuestas de escritura jamás sustituyen validación de owner/claim ni los controles de seguridad backend.

## 2. DTO de diseño y procedencia

`CoverageSnapshotV5` propuesto:
```text
{
  contract_version: "V5_FINAL",
  source_ref: string,
  source_version: string,
  microtask_id: string,
  expected_result: string,
  improvement_ref: string,
  contract_ref: string | null,
  test_ref: string | null,
  evidence_ref: string | null,
  proof_kind: "DOCUMENTAL" | "RUNTIME" | "A5_INDEPENDIENTE" | "PENDIENTE",
  status_source: "PLAN_MAESTRO!H",
  owner_source: "PLAN_MAESTRO!I",
  dependencies_source: ["COLA!G", "COLA!O", "DAG_TECNICO"],
  git_head_sha: string,
  evidence_sha: string | null,
  review_state: "A5_PENDIENTE" | "A5_PASS" | "A5_FAIL" | null,
  outstanding: string[],
  checked_at_utc: string
}
```

Todos los campos de prueba, contrato o evidencia ausentes quedan `null` y `outstanding` no vacío; no rellenarlos con referencias inventadas. `git_head_sha` y `evidence_sha` distinguen el commit de observación del commit realmente probado. `review_state` no es autorización para promover. `status_source` y `owner_source` son etiquetas de procedencia, no cambios sobre la hoja.

`CoverageProposalV5` propuesto:
```text
{
  source_ref: string,
  target_microtask_id: string,
  existing_catalog_match: true,
  mapping_type: "ONE_TO_ONE" | "ONE_TO_MANY" | "MANY_TO_ONE",
  reason: string,
  expected_artifact: string,
  expected_test: string,
  owner_task_id: string | null,
  security_classification: string,
  dependency_fingerprint: string
}
```

Una relación ONE_TO_MANY no crea nuevas IDs fuera del plan. Si una fuente histórica coincide con dos mejoras preexistentes, se registra el mapeo con justificación de límites y no se implementa doble. Si no existe ID canónico de destino, registrar **sin correspondencia** y elevar revisión de plan, nunca agregar la microtarea 2.341.

## 3. Validaciones de entrada, compatibilidad y errores

| Error lógico propuesto | Disparador | Tratamiento contractual |
| --- | --- | --- |
| SOURCE_UNVERIFIED | fuente/versionado sin autoridad V5 FINAL | Rechazar como autoridad ejecutable; conservar referencia histórica descriptiva |
| MICRO_ID_NOT_FOUND | objetivo no está entre los 2.340 IDs del PLAN | No crear fila ni rama; incidencia de trazabilidad |
| COVERAGE_INCOMPLETE | faltan contrato, test o evidencia | Marcar incompleto, no reportar implementación/certificación |
| COVERAGE_COLLISION | mismo resultado se atribuye a dos unidades con trabajo duplicado | Revisar relación y delimitar consumidores, no iniciar doble desarrollo |
| CLAIM_NOT_AUTHORIZED | owner ajeno, G/O NO, DAG faltante o TASK_ID no autorizado | No escribir PLAN; seguir otra independiente |
| EVIDENCE_SHA_MISMATCH | prueba SHA diferente, SKIPPED o IN_PROGRESS requerida | No certificar A4 del ID |
| A5_NOT_INDEPENDENT | autor=revisor o revisor de otra cuenta | Rechazar A5_PASS |
| EVENT_SCHEMA_MISMATCH | evento de 21 en lugar de 26 campos | No escribir BITACORA incompleta |
| SECRET_IN_EVIDENCE | JWT, clave privada, connection string, PII real | No registrar payload sensible; guardar metadatos mínimos |
| SOURCE_REF_MOVED | HEAD o fuente cambió durante lectura | Revalidar identidad, dependencias, hash y corrección aditiva |
| PROMOTION_FORBIDDEN | propuesta implica QA/main/PROD por LISTO_DEV | Rechazar hasta autorización y A5 causal independientes |

Los códigos anteriores **no** son HTTP status ni enums actualmente implementados en SOLQARYN. Si se diseña un futuro API, mapear 400/401/403/404/409/422/500 según riesgo con pruebas negativas y compatibilidad explícita; no introducir una nueva API bajo esta entrega.

**Compatibilidad:** V5 FINAL y sus 2.340 IDs son inmutables para esta corrida. Los consumers antiguos no pueden asumir que A:U es el esquema actual de BITACORA; el físico es A:Z, con EVENT_ID A e IDEMPOTENCY_KEY R. No renombrar H/I de PLAN ni pisar fórmulas COLA E/G/O. No redefinir DAG por objetivo de capacidad MAX(24,3*N). Versionar una futura interfaz sin activar migración/dispatcher y sin convertir un documento en permiso comercial.

## 4. Integridad de ciclo y de revisión independiente

- **PRECLAIM:** comprobar H=PENDIENTE, I vacío, G/O=SI, DAG validado si existe, precedente M01.06 en LISTO y no colisión de archivos.
- **CLAIM:** asignar un solo TASK_ID vigente en H/I con readback y evento A:Z. Las APIs Sheets no garantizan CAS atómico por simple read-modify-write; un rescate de otro owner requiere exclusión efectiva o NO_GO.
- **VALIDANDO:** artefacto completo y fingerprint antes de A0–A3; se preserva I original. No equivale a PASS.
- **A0/A1/A2/A3:** revisar diff y secretos, criterios positivos y negativos, seguridad JWT/RBAC/tenant A/B e integraciones pertinentes; un DTO de diseño no demuestra API o DB real.
- **PUBLICAR:** commit directo dev fast-forward con padre concreto y sin reescribir commits ajenos.
- **A4:** query `GET /repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100`, solo checks exigibles `status=completed`, `conclusion=success`, `head_sha=SHA`; readback de PLAN/COLA/BITACORA.
- **LISTO_DEV:** después de A0–A4 PASS y A5_PENDIENTE registrado, sin autorizar QA/main/PROD.
- **A5:** otro de los cinco TASK_ID Javier verifica SHA, positivo/negativo y rollback; registro PASS/FAIL independiente. Un A5_FAIL antiguo permanece en historia hasta reparación causal.

## 5. Casos de aceptación de documento (NO ejecución live)

| Caso | Signo | Oráculo documental |
| --- | --- | --- |
| P01 | + | Requisito existente V5 se vincula con ID real y contrato/proof → CoverageSnapshot íntegro |
| P02 | + | Un requisito pertenece a dos microtareas existentes → ONE_TO_MANY sin generar ID nuevo |
| P03 | + | SHA y prueba del mismo commit resultan SUCCESS requeridos → A4 y A5_PENDIENTE permitidos con readback |
| P04 | + | Correspondencia incompleta conserva referencias nulas y faltantes, no pasa a implementada |
| N01 | - | ID objetivo inexistente o fuera de 2.340 → MICRO_ID_NOT_FOUND |
| N02 | - | Plan V3/V13 histórico pretende despacho propio → SOURCE_UNVERIFIED |
| N03 | - | Un requisito aparece duplicado como dos trabajos equivalentes → COVERAGE_COLLISION |
| N04 | - | Falta evidencia y se pretende marcar implementado → COVERAGE_INCOMPLETE |
| N05 | - | CLAIM con G=SI, O=NO o owner existente → CLAIM_NOT_AUTHORIZED |
| N06 | - | Job required SKIPPED, CANCELLED, IN_PROGRESS o SHA distinto → EVIDENCE_SHA_MISMATCH |
| N07 | - | Revisor A5=autor o cuenta ajena al allowlist → A5_NOT_INDEPENDENT |
| N08 | - | BITACORA A:U de 21 campos o clave duplicada → EVENT_SCHEMA_MISMATCH |
| N09 | - | Contrato revela JWT, passwords o PII de cliente → SECRET_IN_EVIDENCE |
| N10 | - | Fuente o HEAD cambió mientras se preparaba commit → SOURCE_REF_MOVED |
| N11 | - | Una pantalla confiere permiso tenant B basándose en configuración de A → seguridad backend fail-closed |
| N12 | - | Se intenta promover QA/PROD por tener A0–A4 DEV → PROMOTION_FORBIDDEN |

Los casos P/N son **criterios de aceptación contractual** y no pruebas reales .NET, Angular, Playwright, MySQL, Docker, CAS o autorización HTTP. No se simula un proveedor ni se presenta la matriz como servicio funcionando.

## 6. Impacto, auditorías y reversión

Este cambio de contrato afecta solo un Markdown en docs/v5 y una entrada aditiva en CHANGELOG_AI.md. No cambia DTO C#, endpoint backend, routes Angular, SQL, migraciones, tables, workers, providers, secretos, controles RBAC/tenant ni workflows. Los consumidores futuros son M02.03–M06 del mismo plan, sin habilitar ninguna fase.

A0: integridad del contrato, fuentes, 26 campos, diff y patrones de secretos. A1: P01–P04/N01–N12 como oráculos documentales. A2: fail-closed actor/tenant y A5; sin afirmar pruebas runtime. A3: modelo de cobertura M02.01, matrix R01–R12 y compatibilidad con PLAN/COLA/DAG. Subpruebas `.NET / Node/Angular / MySQL / Docker / E2E` = NO_APLICA por no afectar código ejecutable. A4: commit FF dev, SHA exacto, Scope Lock, readback. A5: Task Javier distinta antes de cualquier promoción, no se simula.

**Rollback:** git revert aditivo del commit documental; nunca reset/force-push, borrado de BITACORA ni cambios de main/qa/PROD.