# SOLQARYN V5 FINAL — C14-04.M02.03: persistencia, índices y fixtures conceptuales de puesta en marcha

**Autor:** PRIMARY DEV 03 JAVIER_CHATGPT `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Proyecto:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Resultado:** contrato **DOCUMENTAL** M02.03; ninguna migración ni cambio a MySQL ejecutados.

## 1. Fuente y estado de partida

Fuentes institucionales: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, Scope Lock y skill canónica. `C14-04.M01.01–06` consta LISTO con A5 independiente, requisito del DAG M02. `C14-04.M02.01–02` son trabajos documentales LISTO/A0–A4, sus A5 siguen sujetas a revisión independiente. El prefiltro M02.03 se validó por ID: PLAN H=PENDIENTE/I vacía, COLA G/O=SI y DAG F=VALIDADO/G=SI.

Fuentes código reales consultadas: `backend/src/Infrastructure/Persistence/AppDbContext.cs` usa EF Core, protege la ruta de soporte temporal de solo lectura mediante `EnsureSupportRequestIsReadOnly`, y resuelve contexto de tenant desde verificaciones backend (no desde `EmpresaId` aportada por el navegador). `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` declara `net10.0`, EF Core `10.0.12`, proveedor `MySql.EntityFrameworkCore 10.0.9`. **No se incorpora ningún nuevo DbSet ni cambio de provider.**

La finalidad es describir qué **evidencia de release/soporte** se almacenaría y cómo se probaría en una fase futura con autorización específica; no proclamar un catálogo de backups o readiness que actualmente no fue verificado.

## 2. Registros persistentes propuestos: no son tablas existentes

| Código | Registro lógico propuesto | Clave de unicidad | Retención/seguridad |
| --- | --- | --- | --- |
| DB01 | ReleaseRecord | release_id | SHA, stage y permiso de ambiente separados |
| DB02 | ArtifactManifestRecord | release_id + artifact_digest | Hash sin secretos |
| DB03 | ScopeBindingRecord | scope_id + tenant_key servidor | Tenant verificado, no cabecera |
| DB04 | EvidenceRecord | release_id + case_id + evidence_hash | Observado/esperado con datos redactados |
| DB05 | CheckResultRecord | commit_sha + check_name + run_id | Status/conclusion exactos, no PASS ficticio |
| DB06 | HealthObservationRecord | service_id + observed_at_utc | Telemetría medida, no SLO supuesto |
| DB07 | AlertObservationRecord | alert_id + observation_id | Alertas reales futuras, no inventadas |
| DB08 | IncidentRecord | incident_id | Owner, estado, severidad e historial |
| DB09 | EscalationRecord | incident_id + step_id | Actor autorizado, sin PII en payload |
| DB10 | BackupManifestRecord | snapshot_id + checksum | Fuente y acceso verificados |
| DB11 | RestoreTrialRecord | snapshot_id + trial_id | Entorno efímero, prueba independiente |
| DB12 | RollbackRunbookRecord | release_id + runbook_version | Plan versionado; ejecución separada |
| DB13 | EnvironmentGrantRecord | grant_id + environment | Sólo permiso expreso, nunca derivado de A5 |
| DB14 | TransitionAuditRecord | event_id + idempotency_key | A:Z append-only, no mutar historia |

**DB01–DB14 = DISEÑO_NO_IMPLEMENTADO.** La ubicación transaccional final depende de evaluación arquitectónica, minimización de PII y autorización de esquema; no se afirma que todos estos datos deban almacenarse en MySQL. PLAN_MAESTRO sigue siendo autoridad del estado de microtareas, COLA/DAG son derivados y BITACORA guarda el historial.

## 3. Índices hipotéticos por consulta demostrable

| IDX | Claves candidatas | Pregunta de rendimiento y condición |
| --- | --- | --- |
| IX01 | release_id, commit_sha | Encontrar manifest exact-SHA; medir cardinalidad |
| IX02 | tenant_key, scope_id, observed_at_utc | Consultas por tenant; filtrado servidor obligatorio |
| IX03 | release_id, case_id | Unicidad de evidencia por caso |
| IX04 | commit_sha, check_name, run_id | Evitar conclusiones de commits diferentes |
| IX05 | service_id, observed_at_utc | Ordenar health, sin inventar SLO |
| IX06 | incident_id, updated_at_utc | Reconstruir historial de incidente |
| IX07 | incident_id, step_id | Evitar escalaciones duplicadas |
| IX08 | snapshot_id, trial_id | Conciliar restore con backup origen |
| IX09 | environment, grant_id | Verificar autorización específica |
| IX10 | event_id, idempotency_key | Idempotencia por evento, no por texto |

**IX01–IX10 son candidatos, no índices creados.** Validar tamaño, planes de ejecución, SELECT/UPDATE reales, granularidad de tenant, índices existentes y costo de escrituras antes de proponer migraciones. Un índice global que permite acceso a datos inter-tenant es inaceptable aunque acelere queries.

## 4. Estrategia futura de migración, transacción y reversión

1. Confirmar Scope Lock, autorización de migración aislada, SHA exacto, dueño del cambio, dependencias y versionado EF Core/Oracle MySQL actual.
2. Ejecutar preflight en MySQL efímero con fixtures sintéticos A/B, sin conexión a bases reales ni secretos en repositorio.
3. Adoptar migración **aditiva y reversible** solo tras estudio de modelo y esquema actual; preservar tablas de comercio/inventario/finanzas.
4. Comprobar relaciones, cardinalidad, unicidad, retención, cifrado, límites por tenant, retries e idempotencia dentro de transacciones reales.
5. Medir ejecución de índices, rollback y consistencia de eventos en base efímera; guardar logs sanitizados y digests de schema.
6. Prohibir promoción a QA/main/PROD, restore productivo y modificación DNS sin permisos posteriores. Un contrato documental NO constituye autorización para crear migration.

Rollback de **este** changeset Markdown: `git revert` normal en dev con HEAD verificado, sin reset/force-push ni borrado de BITACORA.

## 5. Doce fixtures sintéticos con positivos y negativos por ejecutar

| FX | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| FX01 | Release con SHA y manifest iguales | SHA de otro commit bloquea |
| FX02 | Usuario tenant A recupera sólo scopes A | Usuario A no lee B |
| FX03 | Grant específico permite entorno declarado | Sin grant, denegar deploy |
| FX04 | Check exact-SHA success se correlaciona | Cancelled/skipped/in_progress no PASS |
| FX05 | CaseID+digest único inserta evidencia | Inserción duplicada no duplica evento |
| FX06 | Incidente y escalación permitida auditable | Rol revocado no recibe PII |
| FX07 | Health observado tiene timestamp medido | Señal no medida no prueba disponibilidad |
| FX08 | AlertRule se correlaciona a observación real | Alertas simuladas no cumplen SLO |
| FX09 | Backup sintético íntegro ensayado | Checksum inválido bloquea restore |
| FX10 | Restore en MySQL efímero y rollback correcto | Sin snapshot o autorización no se ejecuta |
| FX11 | Dos ejecuciones con idempotency_key concilian | Carrera no crea doble evento |
| FX12 | Query paginada utiliza índice medido | Consulta cross-tenant denegada pese a índice |

**FX01–FX12 = FUTUROS_NO_EJECUTADOS.** No hay migración EF, restore, SQL, docker, HTTP ni benchmarks medidos por esta tarea. Cualquier resultado real requeriría código, pruebas positivas/negativas y checks GitHub Actions pertinentes al SHA.

## 6. Auditorías por causa, consumidores y restricciones

- A0: inventariar DB01–DB14, IX01–IX10, FX01–FX12 y comprobar la fuente EF actual, texto y ausencia de secretos.
- A1: comprobar que cada fixture tiene resultado positivo y negativa falsable; **revisión documental**, no prueba MySQL.
- A2: denegación JWT/RBAC, tenant A/B y acceso a objetos/PII, sin cambios de código.
- A3: alinear M02.01 entidades conceptuales y M02.02 contratos futuros; identificar interfaces consumidoras sin afirmar que estén implementadas.
- A4: commit fast-forward/CAS `dev`, checks exact SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` exitosos; readback PLAN/COLA/DAG/BITACORA.
- A5: Task Javier distinta de la autora y verificación de SHA/blob, independiente y posterior.

**Impacto actual:** únicamente `docs/v5` + `CHANGELOG_AI.md`; no nuevas APIs, tablas, migraciones, tokens, flags, índices, proveedores, webhooks ni despliegues. No se abre QA/main/PROD.
