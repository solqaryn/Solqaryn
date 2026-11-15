# SOLQARYN V5 FINAL — C14-01.M02.05: contratos futuros de eventos, idempotencia y correlación

**Tipo:** catálogo documental de mensajes futuros, no dispatcher implementado. **Autor:** PRIMARY DEV 06 (`6af16bbad26881918c283abe06e1f32a`). **Fecha:** 2026-11-15. **Repo:** `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT.

## Gate y exclusiones

Fuentes: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). M01.01–06 LISTO con A5 independiente. El claim `C14-01.M02.05` procedió de PLAN PENDIENTE sin owner, COLA G/O=SI y DAG F=VALIDADO/G=SI. Es independiente de M02.04 y no toma su diseño de configuración como gate automático.

**Prohibición operativa:** este documento no instala webhooks, cola, worker, servicio de eventos, runtime servidor, Task ni mensaje entre Tasks. Ninguna transición nativa de Sheets despierta a otro ejecutor fuera de su horario. Los eventos son contratos a evaluar únicamente en fases técnicas autorizadas.

## Catálogo futuro EV01–EV14

| ID | Mensaje conceptual | Idempotencia propuesta | Semántica | Protección fail-closed |
| --- | --- | --- | --- | --- |
| EV01 | acceptance.run.requested | run_key + source_sha | Solicitud documental | Sin iniciar otro ejecutor |
| EV02 | acceptance.run.claimed | run_key + owner_id | Owner verificado | CAS/lock real antes de actuar |
| EV03 | acceptance.scenario.started | run_key + scenario_key | Inicio efímero | Sin token ni PII |
| EV04 | acceptance.step.observed | run_key + step_key | Observación saneada | No convertir oráculo en hecho |
| EV05 | acceptance.assertion.passed | run_key + assertion_key | PASS ejecutado real | Exige SHA y runner |
| EV06 | acceptance.assertion.failed | run_key + assertion_key | FAIL real | No reetiquetar automáticamente |
| EV07 | acceptance.evidence.sanitized | evidence_sha + run_key | Referencias limpias | No difundir rutas privadas |
| EV08 | acceptance.ci.completed | run_key + check_name | Resultado nativo de CI | CANCELLED/SKIPPED no PASS |
| EV09 | acceptance.regression.detected | run_key + scenario_key | Defecto causal | No cerrar con excepción |
| EV10 | acceptance.rollback.proposed | run_key + baseline_sha | Plan de reversión | No ejecutar despliegue |
| EV11 | acceptance.review.requested | scope_id + source_sha | Solicitud de A5 | Revisor distinto al autor |
| EV12 | acceptance.review.completed | scope_id + reviewer_id | A5 real independiente | No inferir QA/PROD |
| EV13 | acceptance.run.finalized | run_key + final_state | Cierre auditable | Sólo hechos de runner |
| EV14 | acceptance.run.reconciled | run_key + event_id | Compensación de discrepancia | No borrar historia |

No hay productores ni consumidores ejecutados; toda tabla es **FUTURO_NO_EJECUTADO**. En una futura implementación, el evento deberá incluir `event_id` único, `schema_version`, `scope_id`, `source_sha`, `occurred_at_utc`, `correlation_id` saneado, `tenant_scope` validado y estado nativo; nunca cuerpo del JWT, contraseña, datos de tarjeta, email de cliente o URL privada.

## Política de idempotencia ID01–ID10

| ID | Clave semántica | Restricción |
| --- | --- | --- |
| ID01 | run_key + scenario_key + attempt | Una ejecución por intento autorizado |
| ID02 | scope_id + state_transition + event_id | No duplicar transiciones de PLAN |
| ID03 | source_sha + check_name + status | No sobrescribir conclusión nativa |
| ID04 | reviewed_sha + reviewer_task + scope | No doble A5 del mismo revisor |
| ID05 | tenant_id + scoped_fixture_key | Separación A/B en fixtures |
| ID06 | evidence_sha + source_sha | Dedupe por contenido sin exponerlo |
| ID07 | incident_key + source_sha | Defecto registrado una vez |
| ID08 | rollback_id + baseline_sha | Evidencia de reversión sin repetir efectos |
| ID09 | run_key + retry_reason | Reintento sólo con hipótesis nueva |
| ID10 | task_id + active_scope_id | No escritor duplicado |

La deduplicación es conceptual: sólo un futuro repositorio duradero, transacción y lock atómico pueden implementarla. `READ` seguido de `WRITE` en Sheets NO constituye compare-and-swap multiobjeto. El cambio de estado sólo se acredita tras readback de PLAN, COLA, DAG y BITACORA. No tratar un eventId inventado como RUN_ID nativo.

## Límites LM01–LM12 y aislamiento

| ID | Invariante |
| --- | --- |
| LM01 | máximo un owner activo por microtask ID |
| LM02 | timeout real de runner y límite del proveedor |
| LM03 | sin dispatcher servidor permanente |
| LM04 | sin reintentos sin nueva hipótesis |
| LM05 | sin triggers entre Tasks fuera de agenda autorizada |
| LM06 | sin eventos con credenciales/PII |
| LM07 | sin pago, PSP o factura por un evento conceptual |
| LM08 | sin aumento de permisos por flag de cliente |
| LM09 | sin fuga tenant A/B por correlación |
| LM10 | sin PASS al recibir CANCELLED/SKIPPED/IN_PROGRESS |
| LM11 | sin destruir evidencia histórica al compensar |
| LM12 | sin promover QA/main/PROD por certificado dev |

Las futuras emisiones deben respetar versionado aditivo, permisos backend JWT/RBAC, tenant A/B, branch, fuente exact SHA y lease. La falta de una de estas garantías bloquea cualquier mutación; la observación sólo read-only puede continuar. Los eventos comerciales de pago, inventario o documentos fiscales no se activan por aceptación funcional documental.

## Oráculos positivos y negativos propuestos, NO EJECUTADOS

P01: evento futuro idempotente no se duplica tras replay; N01: doble entrega no produce doble transición. P02: correlación sana conserva trazabilidad exact-SHA; N02: correlación con email/token se rechaza. P03: bitácora append-only corrige mediante evento compensatorio; N03: no se sobrescribe historial. P04: CI completed/success exact SHA puede respaldar un hecho; N04: cancelled/failed nunca PASS. Negativas N05–N12 adicionales: tenant B, permisos revocados, owner paralelo, lease ausente, payload no tipado, versión de esquema incompatible, intento sin cuota y trigger a otro ejecutor fuera de agenda.

Estas afirmaciones son criterios de prueba **futuros**: no se ejecutó runner, cola ni API. Ningún nombre de evento acredita un hecho histórico.

## A0–A5 y rollback

A0: únicamente documentos Markdown; catorce contratos EV, diez ID y doce LM; sin código/secretos/event bus. A1: oráculos escritos positivos/negativos, no test runtime. A2: fail-closed, JWT/RBAC, tenant A/B, privacidad, propietario único y prohibición de despertar Tasks. A3: verificar M01.01–06 LISTO+A5 y referencias a PLAN/BITACORA; no se ejecuta mensaje ni base. A4: commit fast-forward/CAS y checks Scope Lock/V5 exact SHA, readbacks; si no hay PASS requerido, mantener VALIDANDO. A5: Task Javier distinta del autor, evidencia independiente.

Rollback exclusivamente documental por `git revert` sin force push y sin reescribir BITACORA; no deshacer datos de negocio inexistentes. No habilita dispatcher, webhooks, PSP, MySQL, QA/main/PROD.
