# SOLQARYN V5 — autoridad operativa vigente (13 noviembre 2026)

**ÚNICA CONFIGURACIÓN OPERATIVA: 6 Tasks de Javier.** Esta orden posterior expresa del propietario revoca toda disposición anterior que permitiera ejecutar, registrar como elegibles, reactivar, recuperar o usar para revisión A5 las Tasks 07/08 de Alex. Toda cita previa de 8 Tasks, roles secundarios activos o reactivación de Alex carece de autoridad operativa. Los registros de auditoría existentes no se borran ni se interpretan como permisos vigentes. Los dos TASK_ID de Alex están excluidos del allowlist SOLQARYN. Según el propietario sus Tasks físicas ya están pausadas; esta sesión no tiene acceso administrativo a ellas, por lo que no certifica su estado físico.

LEGACY_AUTOMATION_RUNTIME=RETIRED
VAEP_DISPATCH_AUTHORIZED=FALSE
JULES_WORKERS_AUTHORIZED=0
DESIGN_TOPOLOGY=OWNER_CONFIGURABLE
CURRENT_COMMERCIAL_ONLY=TRUE
AUTO_CERTIFICATION_MODE=DIRECT_DEV_FOUR_AUDITS
GOAL_MAX_LISTO_PER_RUN=NONE_WITHIN_PROVIDER_RUN
TASK_REASONING_PREFERENCE=MAX_AVAILABLE_REASONING
TASK_IDENTITY_EVIDENCE_POLICY=ACCOUNT_TASK_CONTEXT_NATIVE_PEEK_AND_SOURCE_LINKS
TASK_FALSE_SELF_OWNED_POLICY=DIAGNOSTIC_ONLY_NOT_ACCOUNT_DENIAL
TASK_DEFINITION_404_POLICY=USE_PEEK_READBACK_NO_AUTO_DENIAL
TASK_CANDIDATE_TRIAGE=SCAN_ALL_ELIGIBLE_WITH_REAL_TOOLCHAIN_PREFLIGHT
TASK_NATIVE_REASONING_CONTROL=OWNER_OR_PLATFORM_SETTING
MAX_CONCURRENT_SCOPES=1000
MAX_OPEN_WORK_BRANCHES=1000
MAX_OWNED_SCOPES_PER_TASK=1000
MAX_ACTIVE_WRITES_PER_TASK=1
CAPACITY_POLICY_VERSION=V15_SINGLE_ACTIVE_DEV_QUALITY
FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=FALSE
FORMAL_APPROVAL_GOVERNANCE_REQUIRED=FALSE
GOAL_MIN_LISTO_PER_RUN=1
GOAL_TARGET_LISTO_PER_RUN=UNBOUNDED_WITHIN_PROVIDER_RUN
CONTINUE_WHILE_ELIGIBLE=TRUE
CONTINUITY_POLICY=V16_CI_PIPELINED_SEQUENTIAL
CLAIM_PARALLEL_LIMIT=1
CLAIM_SEQUENTIAL_LIMIT=UNBOUNDED_WITHIN_PROVIDER_RUN
VALIDANDO_ASYNC_HANDOFF=TRUE
RUN_EXIT_POLICY=NO_ACTIONABLE_WORK_OR_PROVIDER_LIMIT
LOCAL_SKILL_COUNT=1
COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY
LEGACY_MASTER_EXECUTABLE=FALSE
PLATFORM=SOLQARYN
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
PROJECT_SCOPE_LOCK=STRICT
AUTOMATION_RUNTIME=ACTIVE_DEV
TASK_DISPATCH_STATE=ACTIVE_V5_JAVIER_6_ONLY
SCHEDULED_CHATGPT_TASKS_AUTHORIZED=6
PHYSICAL_JAVIER_TASKS_ENABLED=6
AUTHORIZED_COMMERCIAL_TASK_IDS=6adbb9fff1548191a1bd5b30d9e9984a,6af16bb9492c8191b8e8c7d606f11507,6adc2e4bd8a48191ac7d8641ed95a1a3,6aeacb7820ac8191b7d768a6f33dfa09,6adc2e55f47c81919f6374e561687076,6af16bbad26881918c283abe06e1f32a
REGISTERED_TASK_IDS=6adbb9fff1548191a1bd5b30d9e9984a,6af16bb9492c8191b8e8c7d606f11507,6adc2e4bd8a48191ac7d8641ed95a1a3,6aeacb7820ac8191b7d768a6f33dfa09,6adc2e55f47c81919f6374e561687076,6af16bbad26881918c283abe06e1f32a
PHYSICAL_TASKS_REGISTERED=6
ALEX_TASKS_PROJECT_AUTHORIZED=0
ALEX_PEER_WATCH_SCOPE=NONE
JAVIER_PEER_WATCH_SCOPE=SIX_EXISTING_TASK_IDS
JAVIER_PEER_AUTORESUME=AUTHORIZED_SINGLE_ATTEMPT_WITH_READBACK
WORK_BRANCH_CREATION_ALLOWED=FALSE
QUALITY_PROTOCOL_VERSION=V13_A0_A5
ACTIVE_RISK_AUDIT_POLICY=V15_RISK_ADAPTIVE_A0_A5
ACTIVE_DEV_OWNED_LIMIT_PER_TASK=1
TIMEZONE=America/Tegucigalpa
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
SHEET_ID=1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU

| Ejecutor autorizado | TASK_ID | RRULE |
| --- | --- | --- |
| Javier DEV 01 | `6adbb9fff1548191a1bd5b30d9e9984a` | FREQ=HOURLY;BYMINUTE=0;BYSECOND=0 |
| Javier DEV 02 | `6af16bb9492c8191b8e8c7d606f11507` | FREQ=HOURLY;BYMINUTE=10;BYSECOND=0 |
| Javier DEV 03 | `6adc2e4bd8a48191ac7d8641ed95a1a3` | FREQ=HOURLY;BYMINUTE=20;BYSECOND=0 |
| Javier DEV 04 | `6aeacb7820ac8191b7d768a6f33dfa09` | FREQ=HOURLY;BYMINUTE=30;BYSECOND=0 |
| Javier DEV 05 | `6adc2e55f47c81919f6374e561687076` | FREQ=HOURLY;BYMINUTE=40;BYSECOND=0 |
| Javier DEV 06 | `6af16bbad26881918c283abe06e1f32a` | FREQ=HOURLY;BYMINUTE=50;BYSECOND=0 |

## Despacho, continuidad y rescate

1. Conservar habilitadas las seis Tasks Javier existentes; no crear nuevas ni modificar IDs, horarios, TZ, RRULE o prompts sin causa y autorización específica. No activar, ejecutar ni actualizar Tasks privadas ajenas.
2. Orden por corrida: REVIEW_FIRST de una VALIDANDO propia; rescate de BLOQUEADA/VALIDANDO huérfana con diagnóstico causal; nueva PENDIENTE libre y elegible. Continuar mientras permita el proveedor sin cuota artificial de tres cierres.
3. Cada claim requiere identidad de Task Javier, estado PLAN_MAESTRO, unicidad del ID/owner, prueba de permisos y readback; la cola es reactiva por fórmulas, no un scheduler. Nunca dos escritores sobre una microtarea.
4. Para una microtarea cuyo responsable sea un ID retirado, **primero verificar ausencia de corrida/lease/escritura activa**, releer el owner y efectuar relevo exclusivo compare-and-swap hacia una de las seis Tasks Javier; registrar antiguo owner, nuevo TASK_ID, motivo, SHA y timestamp. Si no puede acreditarse la exclusividad, NO_GO al relevo y tomar otra PENDIENTE independiente. No borrar la evidencia original ni marcar LISTO sin auditorías.
5. Permitir rescate técnico asistido sin transferir owner mientras haya ejecución activa: otra Task Javier puede investigar CI, corregir fallo independiente o generar evidencia, sin reclamar cierre simultáneo.
6. Si una Task Javier está pausada sin orden de pausa vigente ni acción humana requerida, otra de las seis puede intentar reactivación física **una sola vez** con permisos reales y readback; nunca intentar con cuentas retiradas.
7. C00-01.M05.04, C01-04.M01.01 y otras asignaciones heredadas requieren revalidación real del estado del Sheet; no se declara concluido ningún bloqueo solo por esta modificación de gobernanza.

## Calidad, seguridad y dependencias

- A0 análisis estático; A1 funcional; A2 seguridad/JWT/RBAC/multitenancy; A3 integración causal; A4 post-publicación exact-SHA en dev. A5 requiere TASK_ID distinto del autor (entre las seis Javier) y revisión independiente. Para documentales, NO_APLICA motivado, nunca PASS inventado.
- CI pertinente según tecnología; Scope Lock STRICT siempre requerido; CANCELLED, SKIPPED o IN_PROGRESS no son PASS cuando la prueba es obligatoria. Validar check-runs contra SHA exacto y fuente actual.
- Documentación V5 FINAL contiene 2.340 microtareas. PLAN_MAESTRO!H es estado autoritativo; COLA deriva elegibilidad; BITACORA A:Z registra EVENT_ID e IDEMPOTENCY_KEY únicos y conserva historial inmutable.
- DAG_TECNICO puede adelantar preparación **documental** independiente ante predecesor VALIDANDO con evidencia, owner y ausencia de FAIL. No permite cierres terminales M01.06/M06, cambios IAM/JWT/RBAC/tenant, migraciones DB, deployments ni QA/main/PROD. Meta elástica de elegibles: MAX(24,3*N), N ejecutores realmente habilitados (actualmente 6), sin liberar dependencias falsas.
- En C00-01.M06.06 preservar gate A5_FAIL histórico de M04.03 hasta A5_PASS causal independiente del SHA pertinente; no retirar bloqueos técnicos reales por orden administrativa.
- Repositorio exclusivo solqaryn/Solqaryn, rama dev; commits fast-forward serializados sin PR o rama auxiliar; no force-push. QA/main/PROD, secretos, datos y dominios productivos requieren autorización diferenciada.
- Autoridad documental única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive ID arriba. No reactivar VAEP/Jules ni planes archivados.
- Si esta documentación contradice ejecución física, reportar discrepancia y aplicar límites reales; el estado físico exige readback. Se conserva el historial técnico en Git y BITACORA, no como instrucciones activas.

## V5 CONTINUO — bucle multi-scope, recuperación cruzada, monitor de corridas (2026-11-13)

- Seis Tasks Javier existentes, sin cambio de IDs, programación o habilitación: prompts nuevos con readback físico 6/6.
- **Bucle por corrida:** REVIEW_FIRST; elegir PENDIENTE elegible libre con COLA!G/O=SI; realizar A0-A4 y A5 independientes según corresponda; registrar cada estado, prueba y readback; releer la cola y continuar mientras queden candidatos seguros y recursos reales. Objetivo blando de tres LISTO, nunca techo ni licencia para omitir pruebas. No declarar cierres sin evidencia.
- **CI sin bloqueo global:** cada unidad con chequeo requerido en curso queda VALIDANDO y conserva owner; el ejecutor puede avanzar otra microtarea independiente. Pruebas requeridas SKIPPED/CANCELLED/FAIL/IN_PROGRESS no son PASS. Nuevos SHA requieren readback.
- **Rescate cruzado:** al inicio y al cierre consultar por herramientas nativas las otras cinco Tasks del allowlist. Una Task enabled/completed no requiere reejecución. Failed exige verificar ejecución posterior, autoría y ausencia de escritura concurrente antes de CAS exclusivo en la microtarea afectada; pausa inesperada solo permite un intento de reactivación autorizado preservando ID, título, RRULE, TZ y prompt con readback. Nunca cuentas retiradas ni run_now como auto-dispatch persistente: run_now inline NO inicia por sí mismo una corrida autónoma entre Tasks.
- **Observabilidad registrada, no telemetría en vivo:** TAREAS_PROGRAMADAS!AA:AN (cada Task escribe únicamente su fila 2-7), Dashboard!A41:J51 y CONFIG!A149:C150. AA=clave de corrida LOCAL sin suplantar RUN_ID; AB=RUN_ID proveedor sólo acreditado; AC=inicio UTC; AD=última señal UTC; AE=estado auto-reportado; AF=última microtarea; AG=cierres LISTO en esa corrida; AH=revisiones A5 ejecutadas; AI=fallos verdaderos; AJ=motivo parada; AK=fin UTC; AL=fuente; AM=fecha consulta Automations.peek; AN=estado nativo en esa consulta. Contadores se reinician al iniciar una corrida y suben sólo tras BITACORA/readback. Una Task fallida antes de finalizar puede dejar señales antiguas; el panel nunca certifica actividad continua. BITACORA A:Z sigue siendo historial inmutable.
- **Evolución futura no desplegada:** un dispatcher por eventos necesitaría trigger legítimo, cola duradera, idempotencia, autorizaciones a proveedores, rate-limit, exclusión simultánea, monitoreo, rollback y un alcance nuevo aprobado. No se han creado servicios, triggers, nuevas Tasks ni cambiado dev/QA/main/PROD.


## V20 — Rescate transversal de VALIDANDO sin exclusividad permanente (2026-11-18)

**Orden posterior expresa del propietario; prevalece exclusivamente ante textos anteriores que priorizan o reservan `VALIDANDO propia`.** Las seis Tasks Javier del allowlist pueden inspeccionar, reparar, acreditar A0–A4 y cerrar cualquier microtarea en `VALIDANDO` aunque PLAN_MAESTRO!I identifique a otra Task. El propietario original I conserva la autoria historica: I no constituye un candado por si solo. Se mantiene A5 independiente, sin autoaprobacion del autor ni de quien produjo el ultimo cambio revisado.

- **Un solo escritor por ID y en cada instante:** antes de cualquier intervencion cruzada comprobar seis Automations.peek (estado nativo), TAREAS_PROGRAMADAS!AA:AN, BITACORA reciente, PLAN H/I/J/K/L/M, SHA/CI, candados SQV17 y actividad especifica de esa microtarea. Una corrida activa trabajando en *otro* ID no reserva en exclusiva todo el historial de su propietario. Si la Task original esta trabajando en ese mismo ID, existe lease legitimo o hay incertidumbre de concurrencia: NO_GO a la escritura en ese ID y continuar con otra tarea.
- **Rescate con candado:** adquirir addNamedRange V17 sobre PLAN H:I exactos, releer H=VALIDANDO, I original, evidencia, SHA y ausencia de escritor. Ningun cierre sin exclusividad. Nunca eliminar candados ajenos por antiguedad; la limpieza de uno inequivocamente huerfano requiere comprobar fin de corrida, ausencia de escritor, idempotencia y readback. Liberar siempre el candado propio despues de persistir y comprobar cambios.
- **Cierre cruzado sin reasignar autor:** si el SHA causal supera los tests obligatorios A0–A4, incluidos Scope Lock, V5, seguridad y MySQL/Chromium/JWT segun el riesgo, permitir `VALIDANDO -> LISTO` con CAS, event_id y idempotency_key en BITACORA y readback PLAN/COLA. Conservar I original; identificar en la Bitacora la Task rescatista. CI obligatoria SKIPPED/CANCELLED/IN_PROGRESS/FAIL no equivale a PASS. Un fallo A5 permanece historico hasta reparacion, pruebas exact-SHA y nueva A5 genuinamente independiente de autores de los cambios.
- **DAG y continuidad:** revisar formulas y efectos del DAG despues de cada LISTO/A5 sin forzar dependencias. Una aprobacion A5 genuina ya registrada en BITACORA/L que la formula espera en K puede proyectarse de manera auditada en K con candado, sin producir aprobacion adicional ni borrar el FAIL. Cuando no sea seguro rescatar VALIDANDO, trabajar en otra microtarea PENDIENTE habilitada, sin esperar inactivo CI externa. Los seis horarios, prompts restantes, QA/main/PROD, arquitectura y limites de una sola escritura simultanea por Task permanecen vigentes.

## Política de calidad V15 vigente (2026-11-15)

Una única EN_PROGRESO y escritura de desarrollo por Task Javier: ACTIVE_DEV_OWNED_LIMIT_PER_TASK=1. Los contadores V13/1000 restantes son capacidades teóricas y no autorizan reservas simultáneas. Conserva A0–A5, A5 independiente, pruebas negativas y de integración reales según riesgo R0–R3 (ver docs/QUALITY_RISK_AUDIT_V15.md). No declarar PASS de un CI incompleto, un mock como prueba MySQL, ni un histórico como autoridad. Respaldos privados anteriores etiquetados NO OPERATIVOS; BITACORA nunca se borra.

## Continuidad secuencial V16 — autoridad complementaria del propietario (2026-11-15)

Una sola EN_PROGRESO propia por Task Javier no limita el NUMERO SECUENCIAL de microtareas que resuelve en una corrida. MAX_ACTIVE_WRITES_PER_TASK=1 mide escrituras SIMULTANEAS, no escrituras acumuladas; MAX_OWNED_SCOPES_PER_TASK=1000 sigue siendo umbral advisory de scope WIP, no reclamaciones paralelas. El objetivo por corrida es UNBOUNDED_WITHIN_PROVIDER_RUN, nunca terminar tras 1/3/5 LISTO o A5 si hay otra acción elegible y tiempo útil. Ciclo obligatorio: REVIEW_FIRST VALIDANDO propias listas y A5 elegibles de otro autor; resolver/reparar con evidencia; si CI sigue externa con run/SHA real, conservar VALIDANDO, tomar UNA PENDIENTE independiente, desarrollar, entregar o cerrar y repetir. No dejar EN_PROGRESO sin entrega ni handoff seguro por decisión voluntaria. Si el proveedor finaliza antes de que termine CI externa, es obligatorio conservar VALIDANDO con run/SHA, motivo y reanudación REVIEW_FIRST en la siguiente corrida o relevo seguro; jamás inventar PASS ni deshabilitar Tasks. Cada salida debe registrar motivo real y disponibilidad nueva en DAG/COLA, sin confundir fin de ventana del proveedor con fin de trabajo. A0–A5 y PROJECT_SCOPE_LOCK=STRICT intactos. Historial anterior no operativo si contradice este mandato; BITACORA se preserva.
