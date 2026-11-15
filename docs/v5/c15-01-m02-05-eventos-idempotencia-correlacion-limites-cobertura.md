# SOLQARYN V5 FINAL — C15-01.M02.05: contrato de eventos, idempotencia, correlacion y limites para auditoria de cobertura

**Autor:** PRIMARY DEV 03 JAVIER_CHATGPT, TASK_ID \`6adc2e4bd8a48191ac7d8641ed95a1a3\`. **Ambito:** \`solqaryn/Solqaryn@dev\`, \`PROJECT_SCOPE_LOCK=STRICT\`. **Tipo:** diseno DOCUMENTAL; NO se instala bus, webhook, listener, dispatcher, cola, endpoint, worker, Task, flag runtime, migracion ni esquema MySQL.

## 1. Objetivo, evidencia y fuentes

Objetivo C15-01: poder trazar requisitos de STANDARD V5 hasta microtareas, dependencias, revisiones y hallazgos sin concluir que la cobertura esta certificada. Fuente unica de plan: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); estado operativo: [PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Los 2340 IDs representan el universo planificado, NO prueba de cero requisitos huerfanos, duplicados criticos o fallos P0/P1.

Se preservan [M01.06](c15-01-m01-06-bloqueos-rollback-liberacion-cobertura.md), [M02.01](c15-01-m02-01-modelo-entidades-ownership-trazabilidad-cobertura.md), [M02.02](c15-01-m02-02-contratos-api-dto-errores-compatibilidad-cobertura.md), [M02.03](c15-01-m02-03-persistencia-indices-migraciones-fixtures-cobertura.md) y [M02.04](c15-01-m02-04-configuracion-tipada-ambitos-flags-cobertura.md). CT01–CT16, OP01–OP08, PD01–PD12 y CFG01–CFG16 son **conceptuales**, no objetos en produccion. En el preflight PLAN H2172=PENDIENTE/I vacia, COLA G2172/O2172=SI, DAG A2133=C15-01.M02.05 F=VALIDADO/G=SI; la ejecucion exige readback posterior del claim. La evidencia de HEAD y CI se obtiene de GitHub y nunca de texto libre del solicitante.

## 2. Catalogo de senales conceptuales (NO_IMPLEMENTADAS)

Cada senal representa una observacion auditada, no una orden de modificar PLAN/COLA/DAG ni de invocar otra Task. Los cambios autoritativos se escriben por los flujos de gobernanza autorizados y se corroboran con readback.

| ID | Senal documental futura | Referencia verificable | Condicion que NO prueba |
| --- | --- | --- | --- |
| EV01 | SOURCE_REVISION_OBSERVED | sourceRevision, sourceHash, corte UTC | Revision sin hash no demuestra cobertura |
| EV02 | REQUIREMENT_LINK_ASSESSED | requirementKey, microtaskId, evidenceRef | Coincidencia textual no forma enlace causal |
| EV03 | DEPENDENCY_GATE_OBSERVED | predecessorId, successorId, DAG F/G, corte | G=SI no es autorizacion de deployment |
| EV04 | OWNER_TRANSITION_OBSERVED | microtaskId, old/new TASK_ID, PLAN H/I, evento BITACORA | Autorreporte no constituye CAS atomico |
| EV05 | ARTIFACT_PUBLISHED | commitSha, blobSha, path allowlisted | Publicar Markdown no ejecuta pruebas runtime |
| EV06 | CHECK_RUN_OBSERVED | headSha, jobName, attempt, status, conclusion | Queued/skipped/cancelled/failed no son PASS |
| EV07 | INDEPENDENT_A5_OBSERVED | originSha, authorTaskId, reviewerTaskId, evidence | Mismo TASK_ID o Task fuera de Javier6 invalida A5 |
| EV08 | COVERAGE_FINDING_RECORDED | findingId, severity, sourceCut, owner, evidencia | Falta de hallazgo no implica cero P0/P1 |
| EV09 | FINDING_RETEST_OBSERVED | findingId, retestSha, resultado, evidenceRef | Repetir sin hipotesis o prueba no cierra hallazgo |
| EV10 | AUDIT_CUT_RECONCILED | sourceRevision, devSha, observedCount, unknownCount | 2340 filas no equivalen a cobertura completa |

Nombres EV01–EV10 y payloads son **PROPUESTOS_NO_IMPLEMENTADOS**. No se agregan suscripciones, transportes, disparadores en tiempo real ni consultas a APIs nuevas. En particular, ninguna observacion de estado en \`VALIDANDO\` emite un pase terminal o A5.

## 3. Envelope y correlacion, con separacion de autoridad

Envelope conceptual \`CoverageSignalV1\`: \`eventId\`, \`idempotencyKey\`, \`kind\`, \`schemaVersion\`, \`sourceRevision\`, \`microtaskId?\`, \`sourceCutUtc\`, \`observedAtUtc\`, \`gitSha?\`, \`evidenceRefs[]\`, \`originTaskId?\`, \`correlationId\`, \`causationId?\`, \`securityScope\`, \`outcome\`. Todos los identificadores se generan en el componente institucional de confianza; nunca aceptar \`originTaskId\`, \`owner\`, \`tenantId\`, \`EmpresaId\`, \`role\` ni SHA como concesion de autoridad desde el navegador.

- \`eventId\`: identificador inmutable de un hecho ya registrado. Los eventos originales de BITACORA A:Z y su \`IDEMPOTENCY_KEY\` son autoridad de auditoria; este envelope no los reemplaza ni duplica.
- \`correlationId\`: agrupa observaciones del mismo corte de auditoria sin sustituir el RUN_ID nativo. \`causationId\` enlaza el evento originador cuando existe evidencia, no se infiere por minutos contiguos.
- \`sourceRevision\` + \`gitSha\`: fijan procedencia del snapshot; si difieren durante conciliacion, el resultado es \`INDETERMINADO\` o discrepancia, no PASS.
- \`securityScope\`: enum allowlisted de \`INSTITUCIONAL\` o \`TENANT_REDACTED\`. Datos de empresa A nunca deben aparecer en el contexto de empresa B; auditoria del SaaS y auditoria de Tasks son dominios separados.
- \`evidenceRefs\`: referencias de documento/commit/blob/CI con redaccion; sin JWT, cookies, correo personal, URLs firmadas, secretos, dumps o identificadores personales innecesarios.

La telemetria \`TAREAS_PROGRAMADAS!AA:AN\` representa ultimo autorreporte; su clave local \`CORRIDA_KEY_LOCAL\` NO es el RUN_ID del proveedor ni un lock transaccional. Automations.peek es la fuente del estado nativo cuando la lectura esta disponible.

## 4. Reglas de idempotencia y causalidad propuestas

| ID | Contrato de validacion futura | Rechazo requerido |
| --- | --- | --- |
| ID01 | Igual \`eventId\` e igual contenido => una sola observacion logica | No insertar duplicado en BITACORA |
| ID02 | Igual \`idempotencyKey\` con payload distinto => conflicto | Rechazar sobrescritura silenciosa |
| ID03 | Estado/owner siempre se leen de PLAN H/I con version observada | No dar por ganador a dos writers |
| ID04 | A5 usa par \`originSha, reviewerTaskId\` y evidencia original | Autoaprobacion o Task fuera del allowlist invalida |
| ID05 | Un check se identifica por \`headSha, checkName, attempt\` | PASS de SHA anterior no cubre commit nuevo |
| ID06 | \`findingId\` permanece abierto hasta retest causal | No borrar hallazgo por recorte de fechas |
| ID07 | \`dependencyEdge\` conserva IDs exactos y gate derivado | No convertir G/O=NO a SI por evento |
| ID08 | Reenvio logico solo proyecta evidencia existente | Nunca usar \`run_now\` ni despertar Tasks |

Para un futuro procesador, las escrituras efectivas requeririan exclusividad demostrable por el proveedor, transacciones o CAS real, permisos y cuota. La lectura mas escritura de celdas **no constituye CAS atomico por si misma**. Ningun evento de este contrato concede o libera owner.

## 5. Cuotas, errores y comportamientos fail-closed

| ID | Limite futuro | Comportamiento si falla |
| --- | --- | --- |
| LM01 | Universe snapshot ligado a una revision unica | Revision distinta => INVALID_SOURCE_CUT |
| LM02 | Paginacion maxima acotada (CFG05: 1..100 propuestos) | Tamano o cursor invalido => rechazo |
| LM03 | Numero de eventos por corte acotado por cuota autorizada | Cuota agotada => detener procesamiento, no crear Tasks |
| LM04 | Reintentos solo con nueva hipotesis y presupuesto definido | No repetir bucle infinito |
| LM05 | Deduplicacion por eventId y clave original | Colision => guardar hallazgo, no aceptar |
| LM06 | Timeout de proveedor y backpressure controlados | Estado INDETERMINADO; no LISTO |
| LM07 | Redaccion de payload antes de cualquier persistencia | PII/secreto => rechazo y alerta segura |
| LM08 | Tamano de evidencia y lista de referencias limitados | Truncar solo visualizacion, no falsificar cobertura |
| LM09 | Tenant/rol verificado en backend, no desde cabecera | Falta de permiso => denegar, no revelar existencia |
| LM10 | Control de seis TASK_ID Javier, no suscripciones externas | Task ajena o pausada no obtiene autoridad |
| LM11 | No activar webhooks ni dispatcher V5 | Mensaje entre Tasks no crea servicio permanente |
| LM12 | QA/main/PROD quedan fuera de alcance | Cualquier solicitud de promocion => NO_GO especifico |

Los limites exactos de throughput/retencion precisan medicion, diseño y aprobacion futura; no se inventan SLO ni cuotas reales. Un fallo de red, GitHub Actions o Drive deja el alcance afectado pendiente, con evidencia y proxima accion; NO equivale a ausencia de trabajo en otras microtareas.

## 6. Oraculos positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo que debe fallar |
| --- | --- | --- |
| T01 | Evento con sourceRevision y SHA consistentes se correlaciona | Fuentes mezcladas son rechazadas |
| T02 | Dos observaciones identicas no duplican el evento original | Mismo key, payload diferente es conflicto |
| T03 | A5 por Javier diferente del autor y con blob/SHA se registra | Autoaprobacion o texto libre A5_PASS se ignora |
| T04 | completed/success en SHA exacto acredita check pertinente | skipped/cancelled/in_progress se muestra PASS |
| T05 | Cambio PLAN se refleja en G/O calculadas via readback | Evento cambia manualmente COLA G/O |
| T06 | Corte con enlace causal auditable se marca revisado | Requisito sin referencia recibe cobertura ficticia |
| T07 | Hallazgo P0 abierto permanece hasta retest | Se declara cero P0 por falta de lectura |
| T08 | Tenant A aislado en consulta autorizada | Tenant B recibe datos de A por EmpresaId |
| T09 | Telemetria distingue run nativo de clave local | Corrida local se presenta como RUN_ID |
| T10 | Reintento deduplicado respeta cuota y causalidad | Worker infinito/webhook disparado sin permiso |
| T11 | Error de proveedor conserva estado desconocido | Fallo 503 se transforma en PASS |
| T12 | Rollback documental conserva historia y anexos | Reset/force-push borra evidencia |

**T01–T12 no se ejecutaron.** No hay nuevo codigo de eventos ni pruebas HTTP, MySQL, Playwright, tenancy A/B vivo o performance realizadas por este changeset; tampoco se certifican cero requisitos huerfanos ni duplicados criticos.

## 7. Impacto, verificaciones y rollback

- **Componente:** contrato documental de trazabilidad C15-01.M02; consumidores futuros M02.06 y etapas posteriores sujetas a DAG propio. No cambia \`Domain/Application/Infrastructure/API\`, Angular, EF Core, datos, JWT, RBAC, TenantQueryBypassGuard, automatizaciones, entorno, permisos ni despliegues.
- **A0:** contrastar STANDARD, PLAN/COLA/DAG, Skill/AGENTS/Scope Lock, previos M01.06 y M02.01–04; comprobar EV01–EV10, ID01–ID08, LM01–LM12 y T01–T12, sin inventar ejecucion.
- **A1:** coherencia documental de escenarios P/N. **A2:** mantener limites de JWT/RBAC/tenant y privacidad; pruebas runtime NO_APLICA por diff Markdown. **A3:** integrar referencias documentales a CT, OP, PD, CFG y fuentes, no sistemas en ejecucion.
- **A4:** commit directo a dev con fast-forward y CAS del HEAD, diff limitado a este Markdown y CHANGELOG autocontenido, check \`verify-solqaryn-project-scope\` y \`V5 - Sin encabezados vacios ni atribucion cruzada\` completed/success en SHA exacto, BITACORA y readbacks.
- **A5:** PENDIENTE de una Task Javier distinta del autor, verificando diff, SHA, evidencias y controles; no promueve QA/main/PROD.
- **Rollback:** git revert normal en dev, sin force-push, sin modificar BITACORA historica. No existe rollback MySQL porque no se altera el esquema. Las migraciones/eventos/servicios futuros requieren autorizacion y CI causal separadas.
