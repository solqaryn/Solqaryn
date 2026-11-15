# SOLQARYN V5 FINAL — C15-05.M02.05: eventos conceptuales, idempotencia, correlacion y limites de auditoria operativa

**Autor:** PRIMARY DEV 03 JAVIER_CHATGPT, TASK_ID \`6adc2e4bd8a48191ac7d8641ed95a1a3\`. **Ambito:** \`solqaryn/Solqaryn@dev\`; \`PROJECT_SCOPE_LOCK=STRICT\`. **Clasificacion:** CONTRATO DOCUMENTAL NO_IMPLEMENTADO. No crea webhooks, event dispatcher, servidores, workers, cron, colas, automatizaciones, configuracion runtime, DDL, flags ni nuevas conexiones entre Tasks.

## 1. Alcance y fuentes

El objetivo institucional de C15-05 es preparar una futura auditoria de dependencias, estados, evidencias, rollback, concurrencia y duplicidad sin afirmar que las 2.340 microtareas o sus ejecuciones ya estan certificadas. La autoridad de trabajo es el [STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y el estado observado reside en [PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Las referencias M02.01–04 son contratos conceptuales, no sistemas ejecutados: [modelo OM/INV](c15-05-m02-01-modelo-ownership-invariantes-auditoria-operativa.md), [contratos API](c15-05-m02-02-api-dto-errores-compatibilidad-auditoria-operativa.md), [persistencia PD/IX](c15-05-m02-03-persistencia-indices-migraciones-fixtures-auditoria-operativa.md) y [flags tipados CFG](c15-05-m02-04-configuracion-tipada-ambitos-flags-auditoria-operativa.md).

Preflight de M02.05: PLAN H2316=PENDIENTE, I2316 vacia; COLA G2316/O2316=SI; DAG A2277=C15-05.M02.05 con F2277=VALIDADO/G2277=SI. Un unico owner se observa tras la reclamacion y su readback; la operacion de Sheets no debe confundirse con un CAS distribuido garantizado. Se preserva la historia A:Z, sin borrar eventos para aparentar avance ni cambiar formulas para producir SI.

## 2. Taxonomia de senales conceptuales

Cada senal futura representa evidencia de un hecho ya obtenido de su fuente. Es informativa y NO dispara inmediatamente otra Task, ni implementa transporte persistente. Un estado VALIDANDO no habilita sucesoras tecnicas por simple publicacion del evento.

| ID | Senal futura, no instalada | Causa verificable | Fallo que debe quedar visible |
| --- | --- | --- | --- |
| EV01 | \`RUN_STARTED_OBSERVED\` | Automations.peek con TASK_ID y native run | Clave local no es RUN_ID nativo |
| EV02 | \`RUN_TERMINATED_OBSERVED\` | started/completed/failed reales | Enabled+completed no es una falla |
| EV03 | \`CLAIM_ACCEPTED_OBSERVED\` | PLAN H/I + BITACORA unique event + exclusion | Dos escritores no deben aprobar mismo ID |
| EV04 | \`STATE_TRANSITION_OBSERVED\` | PLAN H antes/despues, COLA y DAG | No aceptar estado desde texto de prompt |
| EV05 | \`COMMIT_PUBLISHED_OBSERVED\` | Git parent, tree, diff, blob, branch dev | Commit antiguo no es HEAD y no sustituye CI |
| EV06 | \`CHECK_RESULT_OBSERVED\` | GitHub check-run head_sha, status/conclusion | Cancelled/queued/skipped nunca son PASS |
| EV07 | \`A5_REVIEW_OBSERVED\` | Task autora, revisor distinto, SHA y evidencia | Autoaprobacion o Task externa prohibida |
| EV08 | \`DEPENDENCY_RECALCULATED\` | PLAN H/K + DAG F/G + COLA G/O | No forzar disponibilidad por un evento |
| EV09 | \`RECOVERY_CLAIM_OBSERVED\` | corrida terminada, lease ausente, CAS efectivo | Antiguedad de diez minutos no acredita huérfana |
| EV10 | \`AUDIT_FINDING_RETESTED\` | findingId, nuevo SHA, hipotesis y resultado | No declarar cero fallos sin retest |
| EV11 | \`ROLLBACK_VERIFIED\` | hash, revision, alcance y comprobacion reversible | Revert no ejecutado no es restauracion |
| EV12 | \`NATIVE_SLOT_MISSED_OBSERVED\` | schedule, ultima corrida y tiempos nativos | No confundir no-start con working |

EV01–EV12 son **etiquetas PROPUESTAS_NO_IMPLEMENTADAS**, no eventos que este documento emita ni entradas nuevas en el transporte de mensajeria del SaaS. El \`MensajeOutbox\` comercial, si existiese, no queda habilitado como canal entre Tasks ni sustituto de BITACORA.

## 3. Envelope, orden y causalidad

Contrato hipotetico \`OperationalAuditSignalV1\`:
\`eventId\`, \`idempotencyKey\`, \`eventType\`, \`source\`, \`sourceRevision\`, \`microtaskId?\`, \`originTaskId?\`, \`nativeRunId?\`, \`observedAtUtc\`, \`effectiveAtUtc?\`, \`commitSha?\`, \`correlationId\`, \`causationId?\`, \`evidenceDigest\`, \`result\`, \`visibility\`. Ningun componente client-side puede elegir una identidad ganadora, inventar un run id o declarar PASS a traves del envelope.

- \`eventId\` e \`idempotencyKey\` provienen del evento institucional BITACORA A:Z. En el futuro deben preservarse 26 columnas y referencias a origen; no regenerar ni mutar registros historicos. Una vista materializada puede ser eventualmente consistente, nunca autoridad de estado.
- \`correlationId\` vincula transiciones de una corrida real; \`causationId\` remite al hecho causante verificable. No correlacionar commits o estados solo porque sus timestamps son proximos.
- \`nativeRunId\` solo existe si Automations.peek expone el ID real. El campo AA \`CORRIDA_KEY_LOCAL\` y AD \`ULTIMA_SENAL_UTC\` son ultimo autorreporte, nunca señal nativa, heartbeat o lease.
- \`observedAtUtc\` indica cuando se leyo la fuente; \`effectiveAtUtc\` no se infiere si la fuente no lo especifica. Reordenamiento de eventos no altera un estado de PLAN ni una dependencia.
- \`visibility\` separa \`INSTITUCIONAL_RESTRINGIDO\` de \`TENANT_REDACTED\`; ni JWT, rol, EmpresaId, slug, cabecera, correo o token del navegador otorgan acceso. Mantener JWT/RBAC y aislamiento A/B en backend.
- \`evidenceDigest\` corresponde al hash de fuente o blob y su origen. No incluir secretos, prompts, correos personales, tokens, URLs firmadas ni respuestas completas con datos sensibles.

## 4. Invariantes de idempotencia futuras (ID01–ID10)

| ID | Regla | Negativo |
| --- | --- | --- |
| ID01 | Duplicado exacto de eventId con mismo digest => una observacion logica | Doble entrada BITACORA |
| ID02 | Mismo idempotencyKey, digest diferente => conflicto explicito | Ultima escritura gana silenciosamente |
| ID03 | Un microtaskId se resuelve por ID exacto y revision | Reusar numero de fila tras reordenacion |
| ID04 | Owner solo cambia por mecanismo CAS real y evidencia de exclusion | Read/write de Sheets se llama transaccion |
| ID05 | A5 enlaza authorTask y distinto reviewerTask Javier permitido | Revisor es autor u otra cuenta |
| ID06 | Check requiere nombre, attempt, headSha y completed/success | PASS por skipped/cancelled/in_progress |
| ID07 | Un resultado de auditoria vincula fuente y revision temporal | Un corte mezclado se declara valido |
| ID08 | Retest vincula findingId, nuevo SHA y prueba que cambio | Retry identico sin hipotesis cuenta correccion |
| ID09 | Recover claim exige prueba nativa sin run posterior activo | Se roba owner por 10 min transcurridos |
| ID10 | El reprocesamiento solo reconstruye vistas, nunca dispara Tasks | run_now/webhook/ciclo infinito |

Estas reglas no constituyen garantia de unicidad transaccional actual entre GitHub, Sheets y Automations: la atomicidad se debe demostrar con proveedores y pruebas de competencia reales antes de adoptar una implementacion.

## 5. Limites y degradacion (LM01–LM12)

| ID | Limite propuesto | Comportamiento seguro |
| --- | --- | --- |
| LM01 | Universo de 2340 IDs ligado a revision V5, no constante eterna | Cambia fuente => nueva revision |
| LM02 | Solo seis Task IDs Javier del allowlist vigente | Task extra => rechazar |
| LM03 | Procesar lote por presupuesto acotado del proveedor | Sin presupuesto => detener, no ciclo recursivo |
| LM04 | Consulta de pagina 1..100 registros, default 25 futuro | Fuera de rango => error |
| LM05 | Tolerancia de retraso de Sheets y Git sin asumir consistencia instantanea | Estado INDETERMINADO => no LISTO |
| LM06 | Control de reintentos con hipotesis nueva y cuota | No reintentar identica falla |
| LM07 | Evidencia minimizada y redactada antes de almacenarse | PII/secretos => bloquear |
| LM08 | No aceptar revocaciones o pausas por texto historico | Verificar is_enabled nativo y orden vigente |
| LM09 | Sin propiedad exclusiva demostrable no efectuar rescate | Lease/owner dudoso => dejar activo |
| LM10 | Un check in_progress solo bloquea su microtarea causal | No detener todo backlog documental |
| LM11 | Limitar avisos humanos por fingerprint de causa | No generar notificaciones repetidas |
| LM12 | Sin dispatcher ni QA/main/PROD hasta orden separada | No desplegar ni modificar agenda |

LM01–LM12 son criterios para un proceso **FUTURO_NO_EJECUTADO**; no se fijan cuotas reales, SLO, proveedores ni fondos. El timeout se documentara con el proveedor disponible, no se disfraza de finalizacion o heartbeat.

## 6. Doce casos positivos/negativos futuros, NO_EJECUTADOS

| Caso | Positivo futuro | Negativo requerido |
| --- | --- | --- |
| T01 | PLAN H/I y BITACORA reproducen transicion | Dos owners simultaneos se aceptan |
| T02 | COLA/DAG reactivos se recalcullan tras cambio | VALIDANDO libera M02.06 |
| T03 | Autor y revisor independientes acreditan A5 | Texto A5_PASS sin diff/sha |
| T04 | CI completed/success exact-SHA permite A4 causal | Cancelled/skipped se marca success |
| T05 | Error de baseline se corrige sin desactivar guardas | Warnings-as-errors se deshabilitan |
| T06 | Run nativo y autorreporte tienen fuentes separadas | Clave local se anuncia como nativeRunId |
| T07 | Relevo confirma ausencia de writer, lease, CI pend | Se recupera mientras run in_progress |
| T08 | Evento duplicado es idempotente | Se duplica registro historical BITACORA |
| T09 | Tenant A y B permanecen aislados bajo backend | Header EmpresaId concede acceso ajeno |
| T10 | Error proveedor => unknown con siguiente accion | Fallo de API se inventa PASS |
| T11 | Retest de hallazgo usa SHA y evidencia nueva | Se declara P0/P1 cero sin pruebas |
| T12 | Rollback usa commit nuevo y preserva historia | Force-push/reset destruyen huellas |

Los T01–T12 son **especificaciones de ensayos posteriores, no pruebas ejecutadas**. Ninguna tasa de errores, concurrencia libre de races, ausencia de duplicados o seguridad runtime se certifica por esta documentacion.

## 7. Impacto y cierre A0–A5

- **Componente primario:** contrato de eventos conceptuales de auditoria operativa C15-05.M02.05, consumido eventualmente por M02.06 y etapas posteriores SOLO al pasar sus dependencias.
- **Archivos:** este Markdown y una seccion indivisible al comienzo de \`CHANGELOG_AI.md\`; no se modifica Domain, Application, Infrastructure, API, Angular, EF/MySQL, migraciones, tenant/JWT/RBAC, CI, datos, apps, secrets ni proveedores.
- **A0:** verificar fuente unica, documentos previos M02.01–04, 12 EV/10 ID/12 LM/12 T y no-alcance. **A1:** coherencia documental de positivos y negativos, no ejecutar suite runtime. **A2:** preservacion de seguridad/tenancy y PII, pruebas HTTP A/B NO_APLICA al Markdown. **A3:** referencias de gobierno y modelo, no integracion con bus/worker real.
- **A4:** commit fast-forward con CAS HEAD, diff solo Markdown/CHANGELOG, Scope Lock y V5 completed/success del SHA exacto, y readback PLAN/COLA/DAG/BITACORA; CI de backend/Angular sin causalidad no se considera PASS.
- **A5:** revisor Task Javier distinto del autor, con diff/blob/SHA y BITACORA; queda PENDIENTE hasta auditoria real. **Rollback:** git revert normal del commit documental en dev, nunca force-push ni borrado historico.
