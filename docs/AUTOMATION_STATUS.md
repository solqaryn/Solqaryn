## PRECEDENCIA VALIDANDO_FIRST_FIFO_READY_V8_2 — 2026-11-06, Plan Comercial Inicial, dev

Objetivo: 6 microtareas LISTO por hora cuando existan PR aptos. Indicador de producción, nunca autorización para falsear pruebas ni eludir GitHub. Esta regla tiene prioridad sobre V8, V7 y ordenamientos por CI para selección, integración y cierre. No crear Tasks, CAS, reservas ni cambios de horarios.

1. REVIEW_FIRST estricto. En cada ejecución nativa: conciliar VALIDANDO ya fusionados, revisar VALIDANDO de autor distinto con evidencia y permisos reales, y después fusionar los comercialmente aptos. Revisión independiente una vez por diff/SHA, seguridad y pruebas causales; renovar si cambia el código. reviewerFor es preferencia y no reserva exclusiva cuando el HEAD lo habilite. No autoaprobar. Solo tomar PENDIENTE tras cerrar un LISTO o si no hay VALIDANDO ejecutable; como máximo una nueva PENDIENTE por LISTO de ese run.
2. FIFO por PR created_at, nunca por finalización de CI, letras A/B/C/D ni selector de Task. Entre PR elegibles fusionar primero el más antiguo que tenga COLA!G=SI, diff revisado por TASK_ID ajeno en HEAD actual, checks REQUIRED exact-head SUCCESS, mergeable=true y ningún editor activo. Respetar dependencias topológicas. Si el antiguo está pendiente de CI, en conflicto o con G=NO, registrar bloqueo una vez, revisar siguiente ejecutable y poder fusionar otro realmente elegible; no inmovilizar toda la cola. Solo un merge protegido a la vez, refrescar dev HEAD, PR, CI y COLA antes de cada merge; GitHub rechaza carreras.
3. Conflictos: autor/editor propio incorpora dev en misma rama/PR, sin force-push, conservando idempotentemente todas las entradas de CHANGELOG_AI.md y tests/funcionalidad concurrentes; nueva revisión independiente si cambia SHA. No intervenir código de autor ajeno sin traspaso. Reducir colisiones no garantiza literalmente cero conflictos.
4. Entrada y merge: COLA!O=SI permite empezar desarrollo con predecesoras VALIDANDO/LISTO; COLA!G=SI exige predecesoras LISTO para fusionar. Mantener Scope Lock, Seguridad multiempresa, Build backend y pruebas dirigidas aplicables como ordene branch protection sobre PR HEAD exacto. No esperar suites opcionales por mera existencia. FAILED/SKIPPED/CANCELLED/PENDING de REQUIRED no son SUCCESS.
5. Cierre rápido proporcional: para microtareas P1 que solo cambian tests/docs, sin código productivo, migraciones, permisos, secretos, workflows o contratos críticos, una vez comprobados merge protegido a dev, revisión independiente, COLA!G=SI y todos los required PREMERGE SUCCESS exact-HEAD, registrar LISTO y readback en PLAN_MAESTRO, COLA y BITACORA EN ESE RUN, con MERGE_SHA y etiqueta POSTMERGE_CI_ASYNC. Esto no declara CI posmerge SUCCESS: vigilarla después; un fallo requerido posmerge exige incidente único, impedir cierre de sucesoras dependientes y reabrir el scope a VALIDANDO hasta reparar/revertir por cauce protegido. Para cambios de runtime, seguridad, datos, migraciones o gobierno conservar CI requerida postmerge exact-SHA antes de LISTO. HASTA que este contrato se fusione en dev, exigir el cierre postmerge anterior y sin espera pasiva.
6. Si falta CI requerida, registrar CI_PENDING_ASYNC una vez, trabajar otro VALIDANDO sin sleep/polling. Sin CI fingida ni merges prematuros. Límite 15/15/15 es techo, no cuota. Sin QA/main/PROD ni alteraciones de protecciones.

## PRECEDENCIA OPERATIVA FAST_CLOSE_V8 — revisión libre y cierre ágil (2026-11-05)
Orden expresa del propietario: el cuello de botella es VALIDANDO -> LISTO, no la mera admisión a desarrollo. Para microtareas P1 de dev, esta regla sustituye cualquier interpretación anterior que reserve exclusivamente el review a reviewerFor o que espere todos los jobs opcionales antes del merge/cierre. No elimina seguridad, revisión independiente, CI de branch protection, dependencias LISTO para merge ni auditoría institucional.

1. Primero conciliar VALIDANDO cuyo PR ya esté MERGED, con TECH_REVIEW_PASS de TASK_ID diferente del autor sobre el diff aprobado y checks postmerge REQUERIDOS exact-SHA SUCCESS; cualquier Task comercial autorizada puede hacer esa conciliación administrativa, incluso la autora, sin reauditar ni reaprobar. Marcar LISTO y readback PLAN_MAESTRO/COLA/BITACORA en el mismo run.
2. Luego revisar cualquier VALIDANDO de otra Task sin conflicto positivo de editor o revisor, con native run/permisos/autoría vigentes; reviewerFor es una PREFERENCIA de reparto, nunca veto ni reserva temporal. La verificación técnica sustantiva se hace una vez: diff, pruebas dirigidas, seguridad multitenant/RBAC/JWT, SHA y TECH_REVIEW_PASS/REQUEST_CHANGES auténtico. No exigir aprobación formal humana en PR comercial ordinario con required_reviews=0; gobernanza y reglas especiales conservan revisión formal independiente.
3. Para fusionar: PR sin conflictos, COLA!G=SI, TECH_REVIEW_PASS independiente y ÚNICAMENTE los checks que exija el ruleset actual de dev en el HEAD exacto con resultado SUCCESS (al aprobar esta enmienda: verify-solqaryn-project-scope, SOLQARYN - Seguridad multiempresa, SOLQARYN - Build y pruebas backend). Merge protegido en el MISMO run. Los workflows no requeridos, por ejemplo las integraciones MySQL de 20+ minutos, no son gates por mera existencia, aunque sus fallos reales de seguridad deben atenderse proporcionalmente.
4. Después del merge, revisar únicamente los checks POSTMERGE requeridos que correspondan al merge SHA; cuando estén verdes, conciliar LISTO sin esperar CI opcional ni nueva aprobación. Si required sigue pendiente, registrar una sola vez CI_PENDING_ASYNC sin sleep/polling y tomar otro scope; cualquier siguiente Task puede conciliar cuando se complete. SKIPPED/CANCELLED/FAILED no equivalen a PASS requerido.
5. Si PR está dirty, no fusionar: el autor/editor autorizado debe actualizar ESA rama preservando commits y tests, sin force-push ni segundo PR, y renovar verificaciones del nuevo SHA. Ninguna revisora que cambie código puede avalar su propia contribución.
6. Después de cierres/revisiones, EN_PROGRESO propio; después, PENDIENTE libre con COLA!O=SI aunque G=NO (G=SI solo para merge/LISTO). Sin CAS, leases, bloqueos o preasignación en Sheets. Se preservan límites 15/15/15 y alcance DEV sin QA/main/PROD.
Esta enmienda no autoriza pasar scopes concretos a LISTO antes de sus controles reales ni alterar rulesets/workflows. No ejecutar el cambio de selector hasta que se integre protegidamente al HEAD dev.

# SOLQARYN — Estado estándar de automatización

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=ACTIVE_DEV
SCHEDULED_CHATGPT_TASKS_AUTHORIZED=6
AUTHORIZED_COMMERCIAL_TASK_IDS=6adbb9fff1548191a1bd5b30d9e9984a,6adc2e4bd8a48191ac7d8641ed95a1a3,6adc2e55f47c81919f6374e561687076,6adbbc9ad9e88191bea8eb69104afb25,6adcc98a80a48191b71a5156624c21df,6aeacb7820ac8191b7d768a6f33dfa09
VAEP_DISPATCH_AUTHORIZED=FALSE
JULES_WORKERS_AUTHORIZED=0
TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY
TASK_ASSIGNMENT_MODEL=OWNER_CONFIGURABLE
REGISTERED_TASK_IDS=6adbb9fff1548191a1bd5b30d9e9984a,6adbbc9ad9e88191bea8eb69104afb25,6adc2e4bd8a48191ac7d8641ed95a1a3,6adc2e55f47c81919f6374e561687076,6adcc98a80a48191b71a5156624c21df,6aeacb7820ac8191b7d768a6f33dfa09
PRIMARY_TASK_ROLE=PRIMARY_01
SECONDARY_TASK_ROLE=SECONDARY_02
PRIMARY_TASK_ACCOUNT=JAVIER_CHATGPT
SECONDARY_TASK_ACCOUNT=ALEX_CHATGPT

DESIGN_VERSION=2.0
DESIGN_TOPOLOGY=OWNER_CONFIGURABLE
CURRENT_TASK_DISTRIBUTION=4_JAVIER_2_ALEX
PHYSICAL_TASKS_REGISTERED=6
MAX_CONCURRENT_SCOPES=15
MAX_OPEN_WORK_BRANCHES=15
MAX_OWNED_SCOPES_PER_TASK=15
MAX_ACTIVE_WRITES_PER_TASK=15
STAGE_OWNERSHIP_MODE=AUTHOR_PROVENANCE_DIRECT_REVIEW_V2
REVIEW_ROUTE_MODE=DETERMINISTIC_AUTHOR_EXCLUSION
AUTO_CERTIFICATION_MODE=REQUIRED_SCOPE_LOCK
FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=FALSE
FORMAL_APPROVAL_GOVERNANCE_REQUIRED=TRUE
GITHUB_CHAT_EXECUTOR=solqaryn-platform
GITHUB_CHAT_REVIEWER=JAVIER_GITHUB_LOGIN
GITHUB_CHAT_ROLE_SEPARATION=STRICT
GITHUB_CHAT_ROLE_POLICY=docs/GITHUB_ACCOUNT_ROLES.md
LEGACY_AUTOMATION_RUNTIME=RETIRED



## Campos de runtime para custodia de etapa — 2026-11-04

El modelo vigente conserva AUTHOR_TASK_ID historico y permite revisión de TASK_ID diferente. La selección de revisora usa una ruta estable calculada y no solicita registros de ocupación artificiales; un actor externo confirmado como activo sí impide intervenir. Los eventos previos de liberación son historia y no reservas perpetuas.


## ORDEN VIGENTE — 15/15/15 CON ASIGNACION AL DISPARARSE LA TASK (2026-11-05)

**Precedencia:** orden expresa del propietario: distribucion directa por microtarea, SIN BLOQUEO EN SHEETS, sin compare-and-swap, sin escritor de reservas ni control de vencimiento. El contrato 15/15/15, la autoria independiente, los permisos y PROJECT_SCOPE_LOCK=STRICT permanecen.

- MAX_CONCURRENT_SCOPES=15; MAX_OWNED_SCOPES_PER_TASK=15; MAX_ACTIVE_WRITES_PER_TASK=15. Son techos de scopes distintos, NO instruccion de reclamar quince trabajos por Task.
- La propia Task selecciona trabajo cuando su ocurrencia nativa se inicia; no se apartan scopes ni se escriben propietarios por anticipado. Solo usa el plan comercial vigente, sus dependencias, PLAN_MAESTRO/COLA, BITACORA y GitHub live; la historia no impide un nuevo trabajo libre.
- **Reparto sin reservador:** para cada scope, el selector scripts/automation-review-routing.mjs determina exactamente una Task revisora distinta del autor usando los seis IDs autorizados y preferencia fija solo para las cinco entregas actualmente VALIDANDO. El resultado es routing consultivo (no propiedad, no lease, no bloqueo en Sheets). Si un actor no es el seleccionado, debe continuar con otro scope correspondiente a su ruta. El routing debe ser identico para todas las Tasks; no elegir a dos revisoras para un scope.
- Primero revisar VALIDANDO ajeno con PR verificable y sin actor/editor activo comprobado; despues continuar EN_PROGRESO propio o transferencia verdaderamente autorizada; finalmente PENDIENTE elegible con todas las dependencias LISTO. NO exigir demostrar inexistencia absoluta de actividad externa de Codex; solo respetar conflictos POSITIVOS vigentes. La delegacion historica o un comentario viejo no constituyen ocupacion.
- Al iniciar una edicion, contrastar GH branch/PR y el responsable actual. Mantener una sola rama/PR por scope. GitHub protege el commit y los cambios concurrentes con sus controles nativos. Nunca recrear una rama/PR existente ni forzar pushes, reescribir historia o editar otro scope activo. Ante conflicto real, parar solo ese scope y seleccionar otro.
- La revision tecnica solo puede hacerla otra TASK_ID diferente del AUTHOR_TASK_ID. No convertir un login compartido en GitHub APPROVED formal; no aprobarse a si misma, no editar entregas de Codex ocupadas. Los cierres exigen CI exact-SHA requerida, merge protegido aplicable, validacion independiente y conciliacion PLAN_MAESTRO/COLA/BITACORA.
- Cuando la CI siga en curso, registrar CI_PENDING_ASYNC una vez, continuar otra tarea de la misma ruta sin esperar el workflow. Al liberarse trabajo no hay reserva que cancelar: conservar autor y PR, documentar estado y finalizar el turno.
- Nunca crear Tasks, cambiar sus RRULE, habilitar QA/main/PROD ni desactivar checks exigidos. Si falta informacion critica de permisos o hay conflicto positivo, NO_GO solo en el scope afectado; no convertir la falta de una reserva historica en bloqueo global.

## COMPLEMENTO OPERATIVO VIGENTE — CI ASÍNCRONA Y META 6 LISTO/H (2026-11-05)

**Objetivo de rendimiento:** `THROUGHPUT_TARGET_LISTO_PER_HOUR=6` microtareas distintas realmente certificadas como LISTO en ventana móvil de `THROUGHPUT_WINDOW_MINUTES=60`. Es una **meta operativa mínima**, no una garantía, permiso para falsificar evidencia o sexto cierre ficticio. Medir `LISTO_60M`, `DEFICIT_60M=max(0,6-LISTO_60M)` y razones específicas; solo contar IDs únicos con revisión independiente, merge protegido cuando corresponda, CI obligatoria y postmerge aplicable en verde y PLAN_MAESTRO/COLA/BITACORA readback completo. Si el ledger no está verificado, reportar `METRIC_UNVERIFIED`, nunca cero supuesto ni seis imaginarios.

**CI independiente del turno:** `CI_WAIT_MODE=ASYNC_NO_TASK_IDLE`. Cuando un PR esté VALIDANDO y los checks del SHA exacto figuren QUEUED/IN_PROGRESS, consultar GitHub una vez por scope y por ejecución física; registrar evento deduplicado `CI_PENDING_ASYNC` con scope, SHA, PR, nombres de checks, autor/revisor, hora y próxima comprobación en una ejecución posterior. No hacer sleep, polling prolongado, loops de espera, no ocupar el turno de una Task ni agotar capacidad artificialmente esperando GitHub. El pipeline de Actions puede continuar sus controles por su cuenta. Al detectar fallo real, `CI_FAILED_FIX_SCOPE` y corregir solo ese scope; no certificar por excepción.

**Continuidad de revisión:** una revisión terminada no mantiene bloqueos sobre la microtarea. Si la CI está pendiente, registrar CI_PENDING_ASYNC y continuar por el selector; conservar la autoría, rama y PR. No crear ni renovar reservas de revisión en Sheets. Si otro editor realmente trabaja, no intervenir.

**Trabajo productivo sin interferencia:** tras CI pendiente, continuar VALIDANDO ajena asignable por ruta → EN_PROGRESO recuperable con traspaso explícito → PENDIENTE elegible. Reutilizar GitHub branch/PR sin crear bloqueos; un conflicto positivo detiene solo ese scope. Los comentarios antiguos de Codex no impiden tomar trabajo nuevo cuando ya no hay ejecución activa documentada.

**Cierre estricto:** los required checks no se desactivan ni se usan estados SKIPPED/PENDING como PASS. Al volver verde la CI exact-SHA, un revisor elegible verifica el merge protegido y postmerge aplicable; solo después concilia LISTO institucional. Las revisiones y autorizaciones humanas exigibles siguen intactas. La priorización y la meta no modifican el contrato 15/15/15, ni crean/reanudan las seis Tasks físicas pausadas.

**Implementación comprobable:** scripts/automation-review-routing.mjs y scripts/automation-ci-throughput.mjs se validan sin escribir en negocio. Ningún selector concede una aprobación, permiso, revisión formal o LISTO.


## Traspasos futuros — idle-first, proxima ejecucion, sin preasignacion (2026-11-05)

El selector de próxima Task libre es consultivo y no asigna propiedad antes de la ejecución. La ruta determinista por scope y autoría evita múltiples revisoras para una misma entrega; al iniciarse la Task se revisa estado actual de GitHub y Sheets, sin bloqueos de fila. Recuperaciones EN_PROGRESO requieren traspaso autorizado, ausencia de editor concurrente comprobado y preservación de rama/PR.



## Protocolo vigente de revisión técnica por TASK_ID — orden 2026-11-04

La unidad de autoría funcional de una microtarea comercial es el `TASK_ID` físico autorizado y su evidencia de `PLAN_MAESTRO`/`BITACORA`, no la coincidencia del login GitHub. Una Task habilitada **distinta de la autora** puede revisar técnica y sustantivamente el PR de una Task que use la misma cuenta GitHub, comprobar diff, aislamiento multitenant/RBAC, checks y pruebas exact-SHA, registrar `TECH_REVIEW_PASS` o `REQUEST_CHANGES` con `TASK_ID_AUTOR != TASK_ID_REVISOR`, SHA y readback. Si todo cumple y los permisos y reglas protegidas lo permiten, la Task revisora puede efectuar el merge y el cierre `LISTO` después de CI postmerge exact-SHA y readback de `PLAN_MAESTRO`, `COLA` (fórmulas intactas) y `BITACORA`. **La Task autora no puede emitir aprobación técnica cruzada ni marcar como LISTO su propia microtarea.** Un traspaso de edición requiere autorización positiva y ausencia de edición simultánea comprobada; revisión de solo lectura no concede permiso de escritura.


## Censo vigente — incorporación de Javier :30 (2026-11-04)

Orden expresa del propietario: crear una cuarta Task de `JAVIER_CHATGPT` para el minuto `:30` de cada hora en `America/Tegucigalpa`, con ID físico nuevo `6aeacb7820ac8191b7d768a6f33dfa09` y nombre `SOLQARYN — PRIMARY DEV 04 — JAVIER`. Fue creada desde la cuenta de Javier por la herramienta nativa de ChatGPT y su horario/identidad fueron leídos de vuelta. **Estado al preparar el PR: is_enabled=false (pausada preventivamente mientras se autoriza en gobierno)**. No se reutiliza la antigua Task de Alex :30, que conserva su exclusión y su ID anterior sin autorización.

La lista individual de seis IDs corresponde a Javier :00/:20/:30/:40 y Alex :10/:50. La presente autorización comercial para el ID nuevo **solo entra en vigor tras revisión formal independiente del SHA final, los checks obligatorios, merge protegido a `dev` y readback de `TAREAS_PROGRAMADAS`, `CONFIG` y bitácora institucional**. Antes de ese cierre, aun si la Task estuviera habilitada físicamente, no puede escribir, reclamar scopes ni integrarse comercialmente. La activación real será verificada por separado con `automations.peek` y pruebas nativas desde su entorno programado. No se infiere acceso por el horario ni por pruebas manuales.




## Enmienda vigente — inventario configurable de Tasks (2026-10-30)

No hay un máximo interno fijo de Tasks ni una proporción obligatoria entre cuentas. `PHYSICAL_TASKS_REGISTERED` y `REGISTERED_TASK_IDS` describen **seis IDs autorizados (cuatro Javier y dos Alex)**, no un límite absoluto de futuras Tasks. Su estado físico se comprueba en la cuenta propietaria; ninguna activación se presume a partir del gobierno. Se preserva `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=6` y la allowlist canónica, sin reactivar la antigua Task Alex :30. La concurrencia comercial queda regulada exclusivamente por 15/15/15.

El historial de cupos permanece en los commits de Git; ninguna sección histórica constituye política operativa. Prevalece exclusivamente el contrato 15/15/15 de este encabezado.



## M3.2 — habilitacion comercial individual adicional 5/6 (25-10-2026)

**Concesion de alcance DEV otorgada por el propietario, supeditada al merge protegido y conciliacion institucional:** se incorporan SOLO las Tasks PRIMARY_DEV_03 Javier :40 (`6adc2e55f47c81919f6374e561687076`), SECONDARY_REVIEW_01 Alex :10 (`6adbbc9ad9e88191bea8eb69104afb25`) y SECONDARY_REVIEW_03 Alex :50 (`6adcc98a80a48191b71a5156624c21df`) a las dos ya autorizadas. El contrato comercial sigue siendo exclusivamente DEV, no QA/main/PROD; Scope Lock STRICT y exigencia de revision formal independiente permanecen. La autoridad efectiva de esta enmienda nace solo despues de CI obligatorio 3/3 en SHA final, aprobacion independiente, merge protegido dev y readback de CONFIG!B39=5, CONFIG!B57=TRUE con allowlist, TAREAS_PROGRAMADAS!Q3/Q6/Q7=AUTORIZADA_VERIFICADA_DESDE_TASK y BITACORA de conciliacion. Hasta ese punto el contador anterior 2/6 continua operativo. No se crean ni alteran horarios, IDs o cuentas fisicas de Tasks.

**Evidencia diagnostica propia, consumida y no repetible:** Javier :40 GitHub commit `46e6a9822d89932697c93e77c96df239d73bed4f`, archivo `automation-probes/m3-2/6adc2e55f47c81919f6374e561687076.txt`, BITACORA!A85:Z85. Alex :10 GitHub commit `37fb117a38959c99c5795130a1a40365693cc0b4`, archivo `automation-probes/m3-2/6adbbc9ad9e88191bea8eb69104afb25.txt`, BITACORA!A76:Z76 (readback de fila corroborado, el indicador Y76 historico se conserva). Alex :50 GitHub commit `b0f5a77757847d623a17c0f1d9c5fde57fd909fd`, archivo `automation-probes/m3-2/6adcc98a80a48191b71a5156624c21df.txt`, BITACORA!A81:Z81. En los tres casos GitHub confirma archivo unico y commit de la cuenta asignada, y Sheets contiene Task ID, fingerprint, commit y UTC. Las pruebas desde Tasks distintas no son intercambiables.

**Pendiente independiente:** SECONDARY_REVIEW_02 Alex :30 (`6adcc97a260881919d92bb9bacae1626`) posee GH_COMMENT_WRITE=PASS y DRIVE_AUDIT_WRITE=PASS (BITACORA!A78:Z78); su intento de contents:write fue bloqueado por seguridad antes de GitHub (BITACORA!A79:Z79), sin commit ni archivo. Por eso NO entra en esta allowlist ni obtiene desarrollo comercial. No se repetirán los diagnósticos válidos de las otras Tasks; cualquier acreditacion posterior propia de :30 exigira evidencia nueva real desde su ejecucion y autorizacion protegida separada.

**Alcance individual de las tres nuevas Tasks:** trabajar en ramas comerciales work/solqaryn-<microtarea> y PR a dev con exclusividad y máximo seis scopes comerciales según contrato vivo M3.4; revisar/corregir trabajo de otro Task ID sin autoaprobar; PLAN_MAESTRO!H:M solo del scope poseido, TAREAS_PROGRAMADAS fila propia, BITACORA eventos idempotentes y readback; COLA derivada NO se edita. LISTO solo tras CI exact SHA, revision de identidad GitHub distinta del autor, merge protegido, verificacion de resultados y readback Sheets. No secretos, privilegios globales, bypass ni deployment QA/PROD.

## M3.2 — certificacion individual Javier PRIMARY_DEV_02 (25-10-2026)

**Autoridad propuesta condicionada a PR protegido y readback de dev.** El propietario autoriza comercialmente exclusivamente TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3` (JAVIER_CHATGPT, :20 America/Tegucigalpa) para desarrollar y corregir microtareas del plan comercial vigente en ramas de trabajo hacia dev; registrar estados PLAN_MAESTRO!H:M y auditoria BITACORA con readback; revisar trabajo de otro TASK_ID (revision tecnica cruzada), y tramitar merge/cierre solo si GitHub acredita una aprobacion formal genuina desde otra identidad y CI del SHA final. No transferir permisos a otros slots; max dos scopes, exclusividad real, dependencias LISTO, sin QA/main/PROD. El cambio de permiso es efectivo solo despues del merge protegido de esta enmienda y conciliacion exacta del Sheet institucional. `CONFIG!B57` pasa a TRUE para habilitar exclusivamente ejecuciones individuales enumeradas como comerciales en este documento, nunca autoriza todas las seis Tasks por defecto.

**Evidencia ya consumida, no repetir:** commit de contenido GitHub `aaca89247d9aaf61eb21a308c2e43fd5fef24526` y archivo `automation-probes/m3-2/6adc2e4bd8a48191ac7d8641ed95a1a3.txt` en rama temporal `test/solqaryn-m3-2-native-write-20261024`; autor real la cuenta GitHub autenticada de Javier (ID 93171442), UTC `2026-10-25T15:26:15.942Z`. BITACORA!A77:Z77 contiene fingerprint `M3_2_CONTENT_WRITE_PROBE:6adc2e4bd8a48191ac7d8641ed95a1a3:2026-10-25T15:26:15.942Z`, readback 26 columnas y commit coincidente. GH_CONTENT_WRITE=PASS y DRIVE_AUDIT_WRITE=PASS observables; ello no certifica autoaprobacion, merge ni acceso comercial a cualquier recurso no probado. `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=2` solo reconoce Primary 01 y Primary 02, no Primary 03 ni Alex. `ACTIVE_DEV_WRITERS_OBSERVED` historico 1 deja de ser autoridad si contradice esta evidencia.

## M3.2 — tres escrituras de auditoría verificadas y propuesta de pruebas de contenido

Tras integrar el PR #3739, se verificaron tres comentarios GitHub y sus filas de auditoría correspondientes: Alex :10 (comentario `6392818168`, `BITACORA!A63`), Javier :20 (`6392908957`, `BITACORA!A64`) y Javier :40 (`6393029857`, `BITACORA!A67`), con identidades GitHub y fingerprints reales. Los readbacks independientes desde Javier y SOLQARYN Platform se registraron en `BITACORA!A65` y `BITACORA!A68`. Los cinco informes individuales `GH_READ=PASS` y `DRIVE_READ=PASS` siguen documentados. Estas pruebas NO acreditan `contents:write` ni commits o PR comerciales, y el origen nativo exacto de cada escritura de Sheet se sustenta en reportes de Task, no en una identidad de ChatGPT expuesta por la API de Google.

La concesion actual #3739 abarca solo Alex :10, Javier :20 y Javier :40 para comentario/Bitacora. Alex :30 y :50 mantienen `READ_ONLY`. Se propone una unica siguiente enmienda para conceder a esos dos slots auditoria no comercial y a las cinco Tasks una **prueba unica de contenido GitHub** en una sola rama aislada, con lectura posterior y verificaciones de concurrencia; una autorizacion de revision independiente solo para Alex :10 sobre PR #3738 despues de probar capacidad de contenido. **Esta propuesta no es efectiva antes de su propio merge protegido.**

Conservar sin elevar: `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1`, `PHYSICAL_TASKS_REGISTERED=6`, `ACTIVE_DEV_WRITERS_OBSERVED=1`, `CONFIG!B39=1`, `CONFIG!B57=FALSE`; ningun permiso comercial masivo por documento. El resto de controles de Scope Lock, QA/main/PROD, VAEP/Jules e independencia siguen intactos.

## M3.2 — habilitación diagnóstica limitada para PRIMARY_DEV_02 (:20, Javier)

**Task individual:** `6adc2e4bd8a48191ac7d8641ed95a1a3`, propietaria `JAVIER_CHATGPT`, programada cada hora a :20 (`America/Tegucigalpa`), `is_enabled=true`. El usuario aportó un informe de su ejecución del 24-10-2026 con `GH_READ=PASS`, `DRIVE_READ=PASS`, `GH_WRITE=NO_AUTORIZADO` y `DRIVE_WRITE=NO_AUTORIZADO`; `automations.peek` confirmó `last_run_time=2026-10-24T18:26:56.644003Z`. Estas lecturas fueron **reportadas** desde la ejecución y conciliadas por Javier en `TAREAS_PROGRAMADAS!O4:Z4` y `BITACORA!A51:Z51`; no son una prueba de escritura ni permiten atribuir conexiones interactivas a la Task.

**Autorización condicionada:** tras integrar mediante PR protegido y revisión independiente esta enmienda al HEAD actual de `dev`, el propietario permite **una prueba de escritura no comercial y estrictamente delimitada** únicamente desde esta Task, según la sección «Concesión de prueba M3.2 PRIMARY_DEV_02» de la autoridad operativa canónica. Esta excepción no permite desarrollo comercial, cambios a `dev`, PR funcionales, trabajo de otras Tasks, escalamiento de permisos del proveedor ni bypass de sus protecciones.

**Estado hasta readback real:** `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1`, `PHYSICAL_TASKS_REGISTERED=6`, `ACTIVE_DEV_WRITERS_OBSERVED=1` y `CONFIG!B57=FALSE` continúan intactos. La habilitación diagnóstica no certifica `GH_WRITE` de código ni `DRIVE_WRITE` hasta que una ejecución autentique la operación y sus lecturas posteriores; tampoco concede a la Task acceso que el proveedor niegue. Antes de cualquier autorización comercial individual hace falta verificar capacidades de escritura para crear rama/commit/PR en GitHub y editar el Sheet institucional de forma controlada, y entonces conciliar GitHub/Sheet mediante un nuevo cambio protegido. `PASS_M3_2_CERTIFICADA` permanece pendiente.

## M3.2 — dos Tasks adicionales con lecturas nativas reportadas (:10 Alex y :40 Javier)

El propietario aportó informes de ejecución de **`SECONDARY_REVIEW_01` Alex :10 (`6adbbc9ad9e88191bea8eb69104afb25`)** y **`PRIMARY_DEV_03` Javier :40 (`6adc2e55f47c81919f6374e561687076`)**. Ambos reportan `GH_READ=PASS`, `DRIVE_READ=PASS`, `GH_WRITE=NO_AUTORIZADO` y `DRIVE_WRITE=NO_AUTORIZADO`, con HEAD `41d05dda3ffb57707275132f834c40cace3f8594`, gobierno canónico coincidente y Scope Lock STRICT. Se asentaron en `TAREAS_PROGRAMADAS!O3:P3`, `TAREAS_PROGRAMADAS!O6:P6` y eventos de `BITACORA!A53:Z54`; las lecturas internas del conector provienen de informes de esas Tasks, no de una conexión compartida con Javier. `automations.peek` Javier acreditó `PRIMARY_DEV_03 is_enabled=true`, `last_run_time=2026-10-24T18:46:06.710769Z`; la cuenta Alex reportó su ejecución :10 a las 18:11 UTC, sin readback nativo de `automations.peek` desde esta cuenta humana Javier.

Junto con `PRIMARY_DEV_02` Javier :20, hay **tres diagnósticos individuales con lectura `PASS/PASS` reportada** entre las cinco Tasks aún no autorizadas para escritura. Estos tres registros son **CERTIFICACIÓN DE LECTURA CON EVIDENCIA DE EJECUCIÓN REPORTADA**, no certificación del derecho a escribir. Tras aprobación independiente, CI y merge protegido de esta enmienda, estas dos Tasks podrán ejecutar **solo** las pruebas no comerciales individualizadas en la sección de concesiones diagnósticas de la autoridad operativa canónica. No heredan los permisos una de otra, ni de los chats humanos; si el proveedor rechaza la operación no se altera la autorización.

`SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1`, `ACTIVE_DEV_WRITERS_OBSERVED=1`, `CONFIG!B39=1`, `CONFIG!B57=FALSE` y `PHYSICAL_TASKS_REGISTERED=6` permanecen sin cambios; `PASS_M3_2_CERTIFICADA` sigue pendiente de escrituras autenticadas. Esta concesión no autoriza commits ni revisión/aprobación de PR comerciales, ni modifica QA/main/PROD.

## Archivo histórico M3.2 — seis Tasks físicas, autorización operativa parcial

El diseño simétrico 3+3 v2.0 fue integrado en PR #3735; este párrafo registra el estado de aquel momento, no es autoridad vigente. La microfase 3.2 confirmó seis Tasks físicas de identidad única y horarios intercalados, incluidas las dos originales con ID conservado. `PHYSICAL_TASKS_REGISTERED=6` procede de lectura nativa Javier (3/3) y readback aportado por Alex (3/3) con conciliación en Google Sheets. `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1` sigue siendo el contador de autorización conocido: habilitación y programación no equivalen a permisos efectivos en ejecución. Las cinco Tasks restantes siguen pendientes de validación operativa individual, sin concederles escritura por esta modificación. El contrato normativo es `docs/AUTOMATION_AUTHORITY.md`.

**Inventario físico M3.2 (horas `America/Tegucigalpa`, readback en `TAREAS_PROGRAMADAS!A2:Z7`, evento `BITACORA!A42:Z42`):**

| Cuenta | Slot canónico | Minuto | Task ID físico | Evidencia de habilitación |
| --- | --- | --- | --- | --- |
| Javier | `PRIMARY_DEV_01` (`PRIMARY_01` histórico) | :00 | `6adbb9fff1548191a1bd5b30d9e9984a` | ChatGPT `automations.peek` Javier; habilitada |
| Alex | `SECONDARY_REVIEW_01` (`SECONDARY_02` histórico) | :10 | `6adbbc9ad9e88191bea8eb69104afb25` | `automations.peek` Alex reportado y asentado en Sheet; habilitada |
| Javier | `PRIMARY_DEV_02` | :20 | `6adc2e4bd8a48191ac7d8641ed95a1a3` | ChatGPT `automations.peek` Javier; habilitada |
| Alex | `SECONDARY_REVIEW_02` | :30 | `6adcc97a260881919d92bb9bacae1626` | `automations.peek` Alex reportado y asentado en Sheet; habilitada |
| Javier | `PRIMARY_DEV_03` | :40 | `6adc2e55f47c81919f6374e561687076` | ChatGPT `automations.peek` Javier; habilitada |
| Alex | `SECONDARY_REVIEW_03` | :50 | `6adcc98a80a48191b71a5156624c21df` | `automations.peek` Alex reportado y asentado en Sheet; habilitada |

**Separación de estados:** `6/6` es *inventario físico*, NO `6/6` de ejecuciones, permisos, aprobación o certificación. Conservamos `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1`, `ACTIVE_DEV_WRITERS_OBSERVED=1` y permisos de las cinco adicionales en `NO_VERIFICADO` hasta evidencias desde sus ejecuciones. La certificación `PASS_M3_2_CERTIFICADA` está pendiente. Un `last_run_time` no acredita entrega. No exigir `RUN_ID` nativo inobservable.

El histórico de la topología 1+1 queda abajo como evidencia, no como autoridad vigente. VAEP y Jules permanecen retirados. No exigir RUN_ID nativo que ChatGPT no expone ni falsificar resultados. La Task privada de Alex debe verificarse desde su cuenta antes de cualquier escritura de Alex.

## Archivo histórico v1.2 — 2026-10-23 — PILOTO DEV PRIMARY AUTORIZADO POR EL PROPIETARIO (reemplaza gates historicos de microfase 3)

El propietario ordena reanudar las microtareas DEV ahora. PRIMARY_01 existente de Javier es el unico escritor automatizado autorizado, SECONDARY_02 de Alex permanece revisor READ_ONLY. Se limita a solqaryn/Solqaryn, base dev, rama especifica por microtarea del Plan Comercial Inicial y PR contra dev. Prohibido merge autonomo, QA, main, PROD, Render, Vercel, secretos o cambios productivos.

El RUN_ID, RUN_URL, TASK_ID_NATIVO_DEL_RUN, correo ChatGPT, RUN_COMPLETED_NATIVO, REVIEWED_RUN_ID y EVIDENCE_REF_AUTENTICADO no son condiciones de elegibilidad ni certificacion de trabajo DEV cuando ChatGPT no los expone. NO inventar campos. Evidencia operativa suficiente: TASK_ID configurado, ID de microtarea, HEAD base real, rama y PR/commit GitHub verdaderos, checks de CI y evidencia de readback. El 4/4 de ejecuciones de diagnostico READ_ONLY deja de ser requisito del piloto DEV.

Control de colisiones: un solo escritor Primary con WIP=1, comprobar ramas/PR activos del mismo ID antes de iniciar; continuar el PR existente en vez de duplicar. GitHub proporciona HEAD y proteccion de PR. Sin CAS ni leases en Sheets. Alex revisa un PR/SHA concreto, sin REVIEWED_RUN_ID de ChatGPT ni aprobacion ficticia. Se mantienen Project Scope Lock, permisos reales, seguridad multiempresa, CI exact-head y revision humana independiente cuando GitHub lo exija.

Esta habilitacion se vuelve efectiva unicamente al fusionar con checks obligatorios este contrato a dev y comprobar el HEAD nuevo. Los prompts programados deben validar la autoridad viva antes de escribir.
## Fuentes nativas vigentes

Documento rector: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). Seguimiento: [STANDARD DE AUTOMATIZACIONES EN SOLQARYN](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El runtime DEV usa el censo configurable del encabezado, limitado por capacidad efectiva del proveedor y decisión institucional; el allowlist comercial individual y el WIP máximo de seis scopes son controles separados. Ningún plan histórico, existencia de Task ni habilitación física concede permisos por sí solo.

## Archivo histórico — retirada inicial (22-10-2026, no describe el runtime actual)

El propietario confirmó el 22-10-2026 que eliminó las diez Tasks de ChatGPT y retiró la antigua hoja de control de Google Drive. Este repositorio no autoriza despachos, leases, reintentos ni trabajos comerciales automáticos. La eliminación de los cron VAEP y el escritor de admisión automática se integró en DEV mediante el PR #3713.

Los contratos de los proyectos anteriores son **históricos, no ejecutables**. Cualquier implementación futura debe partir de cero, con identidades nuevas y aprobación explícita, sin copiar runtime, secretos, propietarios o pruebas de otro proyecto.

## Certificación de retirada VAEP y ramas (23-10-2026)

GitHub [PR #3728](https://github.com/solqaryn/Solqaryn/pull/3728) y [run 40008279563](https://github.com/solqaryn/Solqaryn/actions/runs/40008279563) confirman la eliminación de las 47 ramas adicionales, con lectura posterior de exactamente tres ramas protegidas: `dev`, `qa` y `main` (PROD). Las 13 propuestas heredadas aún abiertas fueron cerradas sin merge. No se fusionó código histórico a DEV. Las tres ramas conservadas mantuvieron los controles de GitHub, y QA/PROD no se actualizaron.

Del árbol actual de DEV se retiraron 1.499 archivos VAEP heredados, incluidas tres acciones manuales históricas con capacidades de escritura; la historia de Git conserva los antecedentes. El mecanismo excepcional de limpieza debe ser eliminado por PR protegido posterior, sin establecer automatización recurrente. Este certificado es solo sobre gobierno del repositorio: **no** altera ni activa `AUTOMATION_RUNTIME=RETIRED`, la topología futura, la hoja comercial, los permisos de proveedores ni los despliegues pendientes por comprobar.

## Protección permanente distinta del motor

Las pruebas de compilación, aislamiento multiempresa, seguridad, respaldo, frontend y regresiones causales siguen siendo controles técnicos. No deben depender de completar un roadmap anterior ni de la terminación de Tasks inexistentes. Las integraciones del ERP (incluida la funcionalidad comercial propia de automatizaciones M12) no se confunden con el motor retirado.

## Archivo histórico — contrato de preparación de microfase 1 (sin vigencia operativa)

`docs/AUTOMATION_AUTHORITY.md` define exclusivamente la política del piloto de doble ejecutor humano paritario (`EJECUTOR_PRINCIPAL_01` Javier / `EJECUTOR_PRINCIPAL_02` Alex; `PROPIETARIA_INSTITUCIONAL` SOLQARYN Platform). Ninguna Task se crea por esta referencia. En aquella microfase los valores `AUTOMATION_RUNTIME=RETIRED`, `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`, `VAEP_DISPATCH_AUTHORIZED=FALSE` y `JULES_WORKERS_AUTHORIZED=0` eran la verdad operativa; la autoridad actual es exclusivamente el encabezado `ACTIVE_DEV` de este archivo, no este registro histórico. Las 4 microfases requieren cierres separados; el contrato no implica autorización de despliegue o escritura programada.

## Archivo histórico — enmienda inicial v1.2 (sin activación)

El contrato v1.2 autorizó inicialmente el *diseno* de dos Tasks,
una por cuenta, antes de los readbacks físicos registrados en microfase 3: `PRIMARY_01` en ChatGPT Javier y `SECONDARY_02` en ChatGPT
Alex. Ambos siguen como ejecutores humanos con igual autoridad; SOLQARYN
Platform conserva la propiedad institucional. Los marcadores de plan
`PILOT_TASK_LIMIT=2`, `PILOT_PRIMARY_TASKS=1` y `PILOT_SECONDARY_TASKS=1`
NO prueban creacion ni autorizan cron, escritura, claims o revisiones.

En aquel corte de la enmienda se mantenían `AUTOMATION_RUNTIME=RETIRED` y
`SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`. Entonces cada cuenta debía certificar
separadamente Task, identidad, conectores, programacion, readback y permisos
en microfase 3. No recuperar un motor retirado ni crear ambas Tasks bajo
Javier como alternativa a la cuenta independiente de Alex.

## Archivo histórico — microfase 2, estructura de Google Sheets (23-10-2026)

MICROFASE_2_SHEETS_READBACK=PASS
MICROFASE_2_SHEETS_ID=1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU
MICROFASE_2_SHEET_TABS=8
MICROFASE_2_PREPARED_TASK_SLOTS=2
MICROFASE_2_LOGICAL_CAPABILITIES=5
MICROFASE_2_PHYSICAL_TASKS_VERIFIED=0

La estructura del Google Sheets existente fue leida, configurada y verificada.
En el corte original de microfase 2 había dos reservas de rol (Primary Javier
y Secondary Alex), ambas `NO_CREADA` y sin `TASK_ID`. Esa evidencia
histórica no certifica instalación; la creación y habilitación posterior de
ambas Tasks se registran separadamente en microfase 3. Las cinco capacidades
comerciales permanecen `NO_ACTIVA`.

El readback comercial confirma 1.123 microtareas, 1.123 IDs unicos,
cero dependencias invalidas/ciclicas y 15.722 formulas intactas.
Hay 16 validaciones estrictas verificadas en Google Sheets y 38 casos
negativos **simulados aisladamente, no ejecuciones de escritura reales**.
La evidencia exacta, huellas y alcance de rollback figuran en
[`AUTOMATION_MICROFASE_2_CERTIFICATION_20261023.md`](AUTOMATION_MICROFASE_2_CERTIFICATION_20261023.md).

Esta constancia no significa que un PR este aprobado ni que las Tasks
existan o esten habilitadas. Antes de cualquier fase posterior se
requiere cierre por flujo GitHub protegido y autorizacion especifica.
En aquel corte se mantenían `AUTOMATION_RUNTIME=RETIRED`,
`SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`, VAEP retirado y Jules cero. El encabezado de este documento conserva la autoridad operativa actual.

## Archivo histórico — microfase 3 READ_ONLY, previa al piloto DEV (23-10-2026)

**Inventario físico informado, independiente de autorización comercial:**

- **Primary Javier:** `TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a`, propietaria de cuenta ChatGPT declarada Javier; `is_enabled=true`, `RRULE:FREQ=HOURLY;BYMINUTE=30;BYSECOND=0`, zona `America/Tegucigalpa`. Verificado directamente mediante `automations.peek` de Javier; última actividad observable `2026-10-23T22:37:15.829987Z`, que **no acredita terminación del run**.
- **Secondary Alex:** `TASK_ID=6adbbc9ad9e88191bea8eb69104afb25`, propietaria de cuenta ChatGPT declarada Alex; `is_enabled=true`, `RRULE:FREQ=HOURLY;BYMINUTE=5;BYSECOND=0`, zona `America/Tegucigalpa`, según readback independiente declarado por Alex `2026-10-23T21:00:52.262389Z`. La sesión de Javier **no tiene lectura nativa de la Task ni del historial privado de Alex**, por lo que no equivale a una verificación presente desde su cuenta.
- Referencia única del plan: Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Seguimiento único: Google Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` (`TAREAS_PROGRAMADAS!A2:Z3`, `CONFIG`, `BITACORA`); ambas cuentas humanas Javier y Alex tienen permiso `writer`, SOLQARYN Platform `owner`.
- Contrato operativo v1.2, corrección [PR #3731](https://github.com/solqaryn/Solqaryn/pull/3731) ya incorporada: `RUN_ID` nativo puede sustituirse **solo en microfase 3 READ_ONLY** por `EVIDENCE_REF` auténtica de una ejecución individual, programada y finalizada, aceptada por revisor independiente. No inventar IDs ni acreditar `RUN_COMPLETED` con `last_run_time`.
- **Certificación de ejecuciones individuales: 0/4** a la fecha de esta conciliación. `RUN_ID` y resultado finalizado no comprobados de manera suficiente en ambas cuentas. El informe de la Primary en [Issue #3730](https://github.com/solqaryn/Solqaryn/issues/3730) sigue siendo una publicación humana, no un certificado de run; Alex debe validar la fuente independiente.
- En aquel corte dos Tasks físicamente habilitadas **NO** equivalían a dos Tasks comercialmente autorizadas: se mantenían `AUTOMATION_RUNTIME=RETIRED`, `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`, `VAEP_DISPATCH_AUTHORIZED=FALSE` y cinco capacidades `NO_ACTIVA`. El piloto `ACTIVE_DEV` aprobado posteriormente permite **solo** a Primary abrir ramas y PRs de `dev` según la autoridad vigente; no reactiva VAEP, no autoriza despliegues ni añade Tasks. La programación `:05` de Alex revisa la Primary `:30` de la hora anterior cuando exista una evidencia finalizada publicada.

Esta sección registra hechos observados y límites, no crea, modifica, habilita ni eleva el permiso de ninguna Task. Los resúmenes de microfase 2 anteriores son fotografías históricas; para estado operativo manda esta sección y la lectura actual de proveedores, con la autorización global limitada por las claves de inicio de documento.

## Automatización adicional futura, fuera del único piloto DEV

Requerirá un contrato nuevo, aprobación del propietario, diseño explícito de concurrencia y derechos de escritura, aislamiento de credenciales, pruebas de cierre causal y una configuración nueva comprobada. Ningún archivo de este estado produce activaciones.

## CI vigente y archivo histórico (22-10-2026)

La rama `dev` utiliza `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa` y `SOLQARYN - Build y pruebas backend` como checks obligatorios, independientes de las antiguas fases. No se requieren checks Fase 7 ni el workflow `vaep-engine-ci.yml`.

Los recibos que se conservan en `vaep/evidence/` son **archivos históricos de auditoría, nunca estado vivo**. No se pueden usar para seleccionar, reclamar, despachar o cerrar tareas. El backup cifrado programado de DEV protege infraestructura y no es una Task comercial; se preserva.

El código de la tienda, inventario, seguridad, correo, migraciones y las regresiones que protegen funciones reales del ERP sigue sujeto a las pruebas actuales del código, independientemente del nombre de su plan de origen.

## Depuracion de scripts y CI historicos (22-10-2026)

El workflow manual `matrix-governance.yml` y sus diez validadores Python de matrices N8.17/N8.18 pertenecen al gobierno VAEP historico y se retiran del arbol de ejecucion. La evidencia original se conserva en Git y en los documentos historicos, sin reactivar su autoridad. No se eliminan pruebas reales del ERP ni checks requeridos del ruleset.

Los servidores y validadores de prueba de correo, seguridad de runtime, auditoria de codigo y comparaciones PDF conservan exactamente sus funciones con nombres neutros: `smtp_capture_server.py`, `validate_email_delivery.py`, `validate_runtime_safety.py`, `audit_source_quality.py`, `compare_pdf_manifests.py` y `compare_pdf_renders.py`. Sus consumidores de CI se actualizan conjuntamente. No se modifican los contratos de ejecucion de Tasks ni los entornos QA/PROD.
