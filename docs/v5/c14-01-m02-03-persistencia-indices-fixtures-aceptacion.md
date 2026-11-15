# SOLQARYN V5 FINAL — C14-01.M02.03: persistencia conceptual, índices y fixtures de aceptación

**Autor:** PRIMARY DEV 06 / Task `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Repo:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK:** STRICT. Diseño documental exclusivamente; sin migración, SQL, EF, servicios de ejecución o datos reales.

## Preflight y autoridad documental

[STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) fijan `C14-01.M02.03`. A la reclamación: PLAN fila 2026=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI. Su único precedente técnico M01.01–06 tiene LISTO y A5 independiente; M02.01–02 no se convierten por conveniencia en dependencias artificiales. El alcance es el contrato futuro de persistencia de **evidencia de aceptación**, no de datos comerciales productivos.

## Modelo conceptual P01–P12 y ownership

| ID | Agregado propuesto | Identificador lógico | Ownership | Restricción obligatoria |
| --- | --- | --- | --- | --- |
| P01 | AcceptanceRun | run_key | task_id + scope_id + source_sha | No sustituir origen CI por self-report |
| P02 | AcceptanceScenario | scenario_key + revision | c14 scope + plan version | Oráculo positivo/negativo independiente |
| P03 | AcceptanceStep | scenario_key + ordinal | escenario padre | Secuencia ordenada y entrada saneada |
| P04 | ExecutionContext | run_key + context_hash | ambiente de CI efímero | No retener token, cookie o PII |
| P05 | ActorSnapshot | run_key + actor_key | rol y permiso observados en servidor | No usar claim del navegador como autoridad |
| P06 | TenantScope | run_key + tenant_ref | propiedad de tenant derivada por backend | Tenant A/B separados |
| P07 | BranchScope | run_key + branch_ref | sucursal vinculada a tenant | Nunca acceso cruzado por parámetro |
| P08 | FixtureSet | fixture_set + version | datos sintéticos por escenario | No copiar datos comerciales reales |
| P09 | ObservedAssertion | run_key + assertion_key | observación instrumentada | Resultado real distinto de oráculo futuro |
| P10 | SanitizedEvidence | evidence_sha + type | fuente Git/CI autorizada | No secretos, logs crudos ni datos sensibles |
| P11 | RollbackProof | run_key + rollback_step | commit baseline + reversión | No borrar BITACORA histórica |
| P12 | VerificationReceipt | run_key + check_name | HEAD exacto + URL del job | Queued/skipped/cancelled nunca PASS |

Relaciones lógicas: `AcceptanceRun` vincula escenarios versionados, resultados y recibos de verificación; `AcceptanceScenario` contiene pasos y oráculos, separados de hechos observados. `TenantScope` y `BranchScope` contextualizan permisos sin otorgarlos. `SanitizedEvidence` sólo retiene referencias seguras y hashes de integridad, nunca una credencial.

### Invariantes de almacenamiento

La eventual persistencia debe denegar creación de resultados PASS sin `source_sha` verificado, rechazar `run_key` ambiguo, no mezclar evidencia de tenant A con B, exigir pertenencia de sucursal a tenant, no almacenar JWT, nombres/correos de clientes o payloads bancarios y conservar historial append-only para resultados y correcciones. Un resultado `QUEUED`, `IN_PROGRESS`, `SKIPPED`, `CANCELLED` o `FAILURE` se registra con su estado nativo, nunca como éxito. Los resultados positivos y negativos siempre referencian el escenario de origen y un recibo de CI/probador real.

## Índices condicionales IX01–IX08 (NO CREADOS)

| Índice | Agregado | Clave propuesta | Motivo técnico |
| --- | --- | --- | --- |
| IX01 | AcceptanceRun | (scope_id, source_sha, started_at) | Recuperar ensayos por commit sin mezclar SHA |
| IX02 | AcceptanceScenario | (scenario_key, revision) único | Evitar duplicados de oráculos |
| IX03 | AcceptanceStep | (scenario_key, ordinal) único | Reconstrucción determinista |
| IX04 | TenantScope | (run_key, tenant_ref) | Lectura aislada por tenant |
| IX05 | BranchScope | (run_key, branch_ref) | Contexto de sucursal asociado al tenant |
| IX06 | ObservedAssertion | (run_key, assertion_key) único | Evitar doble reporte positivo |
| IX07 | SanitizedEvidence | (evidence_sha, type) único | Deduplicación sin guardar contenido sensible |
| IX08 | VerificationReceipt | (run_key, check_name) único | Idempotencia de hallazgo por check |

Cada índice dependerá de DDL real, cardinalidad, volumen y plan de consulta medidos en MySQL 8.4 efímero; no se crea índice ni se presupone que el esquema ya existe. La política de índices no sustituye la autorización backend ni habilita `SELECT` cross-tenant.

## Fixtures sintéticos F01–F12 para pruebas futuras

| Fixture | Contexto | Oráculo / negativa futura |
| --- | --- | --- |
| F01 | Visitante público, tenant A | Sólo catálogo habilitado; sin sesión |
| F02 | Usuario A, permiso orders.read | Pedido A accesible y branch own |
| F03 | Usuario B, mismo ID externo | Pedido A no visible, 403/404 sin fuga |
| F04 | Membresía revocada A | Token no equivale a permiso |
| F05 | Owner tenant nulo heredado | Denegación y sin asignación implícita |
| F06 | Pedido con versión desactualizada | Conflicto 409, ninguna mutación |
| F07 | Dos reintentos de mismo idempotency key | Una acción observable, no duplicados |
| F08 | Moneda/importe incompatibles | Error sin cobro ni factura |
| F09 | Reserva concurrente misma unidad | Sin sobreventa ni existencia negativa |
| F10 | Archivo con metadata cross-tenant | Acceso denegado, sin URI firmada |
| F11 | Interfaz 320/390/768/1280 | CTA y foco verificables en futuro browser |
| F12 | Job exact-SHA cancelado | No promover a PASS ni abrir QA/PROD |

Estas doce propuestas no son datasets ejecutados. Los futuros tests deberán incorporar pruebas de aceptación positivas/negativas, propiedad tenant A/B, revocación, trazabilidad a SHA, restauración aislada, funcionamiento de frontend y reversión; sus resultados sólo existirán cuando un job concreto los produzca.

## Etapas MIG01–MIG06 de migración reversible futura

| Etapa | Condición de avance |
| --- | --- |
| MIG01 | Inventariar esquema y políticas vigentes en entorno efímero; no suponer tabla nueva |
| MIG02 | Definir adición nullable/tabla nueva con unicidad e índices sin alterar ventas |
| MIG03 | Restringir acceso por tenant desde servicios backend, no desde filtro cliente |
| MIG04 | Aplicar fixtures sintéticos y validar conteos, relaciones, integridad y performance |
| MIG05 | Ensayar rollback en base efímera; preservar auditoría y registros históricos |
| MIG06 | Exigir aprobación diferenciada antes de siquiera planear un cambio de producción |

No ejecutar scripts SQL, migraciones EF, restauraciones, backups ni conexiones al proveedor real durante M02.03. Si hay diferencia entre modelo conceptual y esquema vivo, revisar arquitectura y precedentes antes de tocar Infrastructure. Nunca usar datos de producción para fixtures.

## Evaluación documental A0–A5 y rollback

**A0:** dos archivos Markdown, 12 agregados, 8 índices condicionales, 12 fixtures y 6 etapas; ningún código/secret/DDL. **A1:** evaluación estructural positiva de referencias por SHA y negativa del caso de job cancelado, escenarios futuros no ejecutados. **A2:** aislamiento A/B, JWT/RBAC sin autoridad de cliente, PII ausente y propiedad NULL denegada, únicamente como contrato. **A3:** referencias M01.01–06 LISTO+A5 y futura integración con backend net10/EF10/MySQL 8.4, sin fingir pruebas de base. **A4:** publicación fast-forward/CAS en dev, diff exacto y `verify-solqaryn-project-scope` más V5 provenance `completed/success` para SHA publicado o equivalencia de blobs acreditada. Si check obligatorio sigue pendiente, conservar VALIDANDO. **A5:** revisión independiente por otra Task Javier.

Rollback de artefacto: `git revert` no destructivo, sin force-push. BITACORA permanece inmutable. Sin cambios a API, pagos, archivo, secretos, DNS, QA/main/PROD.
