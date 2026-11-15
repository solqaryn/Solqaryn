# SOLQARYN V5 — C14-03.M02.03: contrato de persistencia, índices y fixtures para promoción controlada

**Responsable:** PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`). **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15. **Autoridad:** STANDARD V5 FINAL, plan institucional C14-03.M02.03.

## Objetivo y límites inalterables

Esta es una **propuesta documental** de persistencia para el proceso *futuro* de promoción controlada. Define estructuras lógicas, claves, índices, migraciones reversibles y fixtures sintéticos. **No crea tablas MySQL, migraciones EF, endpoints, workers, webhooks, secretos, credenciales o despliegues.** No autoriza escrituras a `qa`, `main`, PROD, proveedores, DNS o datos reales. Se conserva MySQL 8.4, ASP.NET Core 10 y el proveedor EF Oracle vigente sin cambiar paquetes.

Fuentes: `docs/v5/c14-03-m02-01-modelo-entidades-ownership-promocion-controlada.md`, `docs/v5/c14-03-m02-02-api-dto-errores-compatibilidad-promocion-controlada.md`, `docs/v5/c14-03-m01-06-bloqueos-rollback-promocion-controlada.md`, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md` y el documento comercial V5. La salida documental M01 tiene LISTO+A5 independiente en el plan; el gate DAG de esta unidad estaba F=VALIDADO/G=SI y COLA G/O=SI. **Ningún artefacto M02 reemplaza una comprobación de ambiente.**

## Esquema conceptual, NO implementado

| Entidad futura | Identidad y campos mínimos propuestos | Referencias y garantía de ownership |
| --- | --- | --- |
| `ReleaseCandidate` | `candidate_id` opaco, `repo`, `branch`, `commit_sha`, `tree_sha`, `created_at_utc` | Inmutable por contenido; no se reusa para otro SHA |
| `ArtifactManifest` | `manifest_id`, `candidate_id`, `digest`, `toolchain_version` | FK a candidato; digest verificado por build |
| `EvidenceBundle` | `evidence_id`, `candidate_id`, `gate_code`, `check_sha`, `conclusion` | FK candidato; status nativo, nunca una cadena declarativa del cliente |
| `PromotionReviewRequest` | `request_id`, `candidate_id`, `target`, `purpose`, `created_at_utc` | Revisión de intención; NO permiso de despliegue |
| `PromotionApproval` | `approval_id`, `request_id`, `approver_ref`, `valid_until_utc` | Identidad, target, SHA y caducidad ligados a solicitud |
| `RollbackContract` | `rollback_id`, `candidate_id`, `prior_digest`, `restore_evidence_ref` | Sólo contrato; ejecución futura requiere autorización |
| `HealthEvidence` | `health_id`, `candidate_id`, `target`, `native_observed_at_utc` | Lectura de destino, no inferida de documento |
| `PromotionOutcome` | `outcome_id`, `request_id`, `observed_sha`, `result` | Sólo readback nativo posterior, no autoafirmación |
| `AuditDecision` | `event_id`, `request_id`, `actor_ref`, `decision`, `timestamp_utc` | Append-only e idempotencia por `event_id`; sin PII ni tokens |
| `RequestDedupe` | `key_digest`, `request_id`, `scope`, `expires_at_utc` | Ámbito objetivo+SHA+actor; rechazo de doble efecto |

**No se almacenan secretos, JWT, contraseñas, IP privadas, PII, SQL arbitrario ni credenciales en estas entidades.** En una futura implementación, la comprobación de membresía y tenancy provendrá del backend, no de un campo controlado por el navegador. Los cambios de estado y autorizaciones deben vincularse al mismo SHA/candidato y no permitir escalada por alias o cabecera.

## Índices propuestos, sujetos a EXPLAIN y MySQL efímero

| Índice futuro | Clave lógica | Propósito y riesgo a evaluar |
| --- | --- | --- |
| IX01 | `ReleaseCandidate(repo, branch, commit_sha)` UNIQUE | Evitar duplicación de candidato exacto |
| IX02 | `ArtifactManifest(candidate_id, digest)` UNIQUE | Integridad referencial del manifiesto |
| IX03 | `EvidenceBundle(candidate_id, gate_code, check_sha)` | Consultar evidencia causal y status |
| IX04 | `PromotionReviewRequest(candidate_id, target, created_at_utc)` | Leer intentos de revisión por destino |
| IX05 | `PromotionApproval(request_id, valid_until_utc)` | Revalidación de expiración por solicitud |
| IX06 | `PromotionOutcome(request_id, observed_sha)` UNIQUE | Evitar duplicación de outcome observado |
| IX07 | `AuditDecision(request_id, timestamp_utc)` | Auditoría append-only sin orden aparente inventado |
| IX08 | `RequestDedupe(scope, key_digest)` UNIQUE | Concurrencia/idempotencia antes de un efecto autorizado |

Estas claves son **hipótesis**: no se han comparado cardinalidad, planes de ejecución, coste de inserción ni colisiones reales. La semántica de `target` debe resolverse desde permisos de ambiente; usar `tenant_id` del cliente como partition key o como permiso es inválido.

## Contrato de migración futura, sin ejecución

1. **MG01 — Preflight:** comprobar authorization/owner de ambiente, Scope Lock, SHA, contrato de respaldo y posibilidad de restore; sin estos requisitos no ejecutar.
2. **MG02 — Expand:** crear tablas aditivas y FKs de manera compatible únicamente en MySQL 8.4 **efímero autorizado** para validación.
3. **MG03 — Backfill:** datos sintéticos idempotentes, con batches controlados y semántica de reintento; no tocar datos productivos.
4. **MG04 — Verify:** validar nulabilidad, FK, unicidad, índices, EXPLAIN y transacciones con dos actores/tenants sintéticos A/B.
5. **MG05 — Contract:** retirar campos antiguos sólo con evidencia de consumidores y ventana aparte; esta tarea no permite borrados.
6. **MG06 — Restore:** revertir migración o restaurar backup efímero con hash, conteos y señales reproducibles antes de permitir un futuro plan de release.

La reversión de datos requiere restore real en ambiente efímero y aprobación de ambiente. `git revert` de este documento sólo revierte texto, nunca DDL ni efectos externos.

## Fixtures sintéticos y oráculos de verificación — FUTUROS_NO_EJECUTADOS

| Fixture futuro | Entrada y resultado esperado | Negativo que debe fallar |
| --- | --- | --- |
| FX01 | Candidato `sha-A` y tree/digest consistentes -> evaluación read-only | SHA diferente reutiliza evidencia |
| FX02 | Dos manifests con digests distintos -> dos versiones explícitas | Mismo digest duplicado contra UNIQUE |
| FX03 | Check completed/success del SHA exacto -> gate favorable | skipped, cancelled o queued -> no PASS |
| FX04 | Aprobador con permiso del target y aprobación vigente -> revisión | Autor A5 se autoaprueba o permiso expirado |
| FX05 | Dos solicitudes con igual idempotency key -> una intención | Retry genera outcome doble |
| FX06 | Tenant A consulta sólo evidencia A autorizada -> no fuga | Tenant B accede registros de A |
| FX07 | Tabla sintética con FK válidas -> insertar en efímero | FK huérfana o hash alterado -> transacción falla |
| FX08 | Restore de snapshot efímero -> conteos y hash iguales | Restore parcial o schema divergente -> bloqueo |
| FX09 | Actor sin permiso PROD consulta contrato -> lectura reducida o denegada | Dev LISTO se utiliza para deploy PROD |
| FX10 | Outcome sólo después de lectura nativa -> auditabilidad | Texto de UI `deployed` se acepta como prueba |

**Ninguna fila anterior es un test ejecutado ni un resultado de MySQL.** La implementación futura deberá registrar dataset, SQL/EF real, versión, SHA, logs saneados y test-run identificable. No hay autorización de cambio de base de datos, backup de usuario ni activación de infraestructura.

## A0–A5, evidencia y rollback

- **A0:** el cambio debe limitarse a este Markdown y una sección contigua de `CHANGELOG_AI.md`; no código, SQL, secretos ni configuración de runtime.
- **A1:** comprobar diez entidades, ocho índices, seis pasos de migración, diez fixtures y sus negativos, con etiquetas FUTUROS_NO_EJECUTADOS. No asignarles PASS funcional.
- **A2:** respetar JWT firmado/backend, RBAC, tenant A/B, segregación por ambiente, A5 independiente, idempotencia, no exposición de secretos y no bypass de Scope Lock.
- **A3:** verificar M01.01–06 LISTO+A5 y coherencia documental con los contratos M02.01 y M02.02; build .NET, frontend, MySQL, Playwright y deploy = NO_APLICA al diff Markdown salvo que un gate de gobernanza causal lo requiera.
- **A4:** comprobar publicación fast-forward con CAS GitHub del HEAD esperado, diff y blobs exactos; Scope Lock y check V5 `completed/success` sobre el SHA publicado; readback PLAN/COLA/DAG/BITACORA.
- **A5:** revisión técnica por Task Javier distinta de este autor, basada en SHA, diff y fuentes; sin A5 no se liberan dependencias que exijan revisión independiente.

**Salida de esta microtarea:** sólo contrato M02 documental LISTO_DEV después de A0–A4 verdaderas, A5_PENDIENTE. No habilita por sí mismo C14-03.M02.06 ni QA/main/PROD. **Rollback:** `git revert` normal del commit de documentación, preservando el historial BITACORA.
