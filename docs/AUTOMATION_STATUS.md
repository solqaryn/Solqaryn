# SOLQARYN — Estado operativo único de automatizaciones

## DAG24 VALIDANDO-TRIGGER 2026-11-13 — REGLA OPERATIVA AUTOACTIVABLE

Regla del propietario: cuando una tarea predecesora entre en VALIDANDO, el sistema debe **evaluar inmediatamente por formula** los sucesores PENDIENTE de preparación documental técnicamente independientes y dejarlos habilitados si tienen evidencia provisional suficiente. No se requiere aprobación humana rutinaria ni esperar que una microfase de seis tareas termine. **VALIDANDO no es LISTO** y solo sirve como prerequisito provisional para análisis de continuidad, nunca para cierres M01.06, M02, cambios IAM/JWT/RBAC/tenant, DB/migraciones, despliegues ni QA/main/PROD.

Fuente reactiva: DAG_TECNICO!G calcula las dependencias desde PLAN_MAESTRO, y COLA!G/O traduce a habilitada solo una PENDIENTE sin propietario. Para las tareas documentales C02-02.M01.01-05 se permite dependencia de C02-01.M01.01-02 si LISTO o VALIDANDO con OWNER en PLAN_MAESTRO!I y evidencia en L (sin A5_FAIL en J); para C02-03.M01.01-05, C02-02.M01.01-03 de igual modo. Base C00-01.M02.01-06, C00-03.M01.01-05, C01-01.M01.01-05 terminal; C01-04.M01.01-05 requiere adicionalmente C01-01.M01.01-02 LISTO. La transición se evalúa automáticamente al recalcular el Sheet y en cada corrida de Task, no mediante cambios manuales del estado F, ni nuevas Tasks ni un evento externo imaginario.

C02-01.M01.01-05 y C01-04.M01.01-05 son preparación documental únicamente; C02-02/03 pueden adelantarse documentalmente en VALIDANDO causal. Revalidar hipótesis y discrepancias cuando el predecesor alcance LISTO; si FAIL, falta evidencia u owner, bloqueo automático y no certificar trabajos sucesores sobre premisas invalidas. Para IDs fuera de DAG autorizado, no inventar disponibilidad: ejecutar reposición automática acotada durante los turnos de las Tasks: si elegibles son menos que la meta MAX(24,3*N), buscar PENDIENTE sin dueño existentes en el plan, clasificar ámbito y dependencias reales, simular sin ciclos/colisiones/tenancy, respaldar, documentar excepción y registrar VALIDADO solo los trabajos independientes comprobados. Requiere Scope Lock SUCCESS en HEAD exacto, pruebas pertinentes y lectura posterior; ante incertidumbre dejar NO. N son ejecutores físicamente habilitados, no una cuota a forzar.

Las cuatro M01.06 de C00-02/03/04 y C01-01 requieren todas las preparatorias propias LISTO, cierre C00-01.M06.06 certificado y A5 causal vigente. C00-01.M06.06 permanece bloqueada por el A5_FAIL histórico de M04.03 hasta A5_PASS causal independiente del SHA correcto registrado con marcador A5_PASS_CAUSAL_M0403_SHA=<SHA40> en PLAN_MAESTRO!J22. El FAIL histórico y BITACORA se preservan; nunca inventar PASS.

REVIEW_FIRST de VALIDANDO propia, claims atómicos e idempotentes, check-runs exact SHA y A0–A5 reales. Playwright M10 autorizado en CI cuando técnicamente corresponda, sin aprobación humana rutinaria. El Sheet no ejecuta automatizaciones de ChatGPT permanentemente; la fórmula reactiva libera elegibilidad, las Tasks toman trabajo en sus turnos programados. Mantener IDs, RRULE, zonas, Alex, seguridad Scope Lock STRICT, dev solo, no QA/main/PROD.


> **ENMIENDA OPERATIVA DAG20 V5 2026-11-13 — autoridad subordinada al Plan V5 y Scope Lock STRICT.** Se autoriza preparacion DOCUMENTAL M01.01–05, sin implementacion ni migracion, para C00-04, C01-01, C01-02 y C01-03 (20 IDs existentes); prerequisitos por ID: C00-01.M02.01–06 y C00-03.M01.01–05 LISTO, evidencia y unicidad en DAG_TECNICO; G=SI solo con F=VALIDADO y todos los prerequisitos terminales; O=SI solo PENDIENTE sin dueño. La salida es analisis preliminar y se revalida frente a C00-01 final y contratos C01-01 antes de M01.06, M02, cambios ejecutables o QA/main/PROD. M06.06 de C00-01 depende de M05.04 y M06.01–05 LISTO; revisar A5_FAIL/A5_PENDIENTE y no promover por contador. A5 independiente por otra Task y pruebas pertinentes reales, Playwright autorizado sin solicitud manual rutinaria, SKIPPED no PASS. Reserva dinamica = MAX(24,3*N) con N ejecutores fisicos habilitados realmente observados; es objetivo adaptable, no habilitacion ficticia ni autorizacion para crear Tasks. REVIEW_FIRST de VALIDANDO propios, reclamacion unica, readback A:Z 26 campos, consulta check-runs por SHA exacto y no sobrescribir claims concurrentes. Una nueva excepcion requiere evidencia, no ciclos, no colisiones, respaldo, simulacion y activacion VALIDADO auditada. No modificar Tasks/horarios ni QA/main/PROD.


> **ESTADO OPERATIVO V5 FINAL (readback Javier 2026-11-13).** Seis Tasks Javier originales habilitadas (is_enabled=true, 6/6); pausa del 12-nov HISTORICA REVOCADA por orden posterior. Alex solo desde cuenta propietaria. Fuente ejecutable unica: Google Doc V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, 2.340 IDs C00-01.M01.01 a C15-05.M06.06. V13 mantiene A0–A5 y seguridad, no el backlog antiguo. El commit base `f4b8528f57e82625d343dd1590076df44cacaee2` es antecesor del HEAD `4d582aa48dcdf8e9efb8d4c15ec229cbc0d603a5` (13 commits de avance); la autoridad de codigo/CI es siempre HEAD vivo y SHA exacto del candidato, no un SHA congelado. Validar con GitHub Actions (dotnet/Angular/Node/Docker/MySQL cuando los jobs pertinentes realmente corren); sin PASS si el job es SKIPPED o el SHA es anterior. Preservar horarios, IDs y recuperacion cruzada; no fingir nuevas microtareas LISTO.


PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=ACTIVE_DEV
TASK_DISPATCH_STATE=ACTIVE_V5_DEV_NATIVE_READBACK_6_OF_6
JAVIER_PHYSICAL_TASKS_PAUSED=0
JAVIER_PEER_AUTORESUME_EFFECTIVE=ACTIVE_SINGLE_ATTEMPT_WITH_READBACK
ALEX_PHYSICAL_PAUSE_STATUS=UNVERIFIED_OWNER_ACCOUNT
PHYSICAL_JAVIER_TASKS_ENABLED=6
ALEX_TASKS_STATE=OWNER_REPORTED_2_OF_2_ENABLED_V13
SCHEDULED_CHATGPT_TASKS_AUTHORIZED=8
AUTHORIZED_COMMERCIAL_TASK_IDS=6adbb9fff1548191a1bd5b30d9e9984a,6af16bb9492c8191b8e8c7d606f11507,6adc2e4bd8a48191ac7d8641ed95a1a3,6aeacb7820ac8191b7d768a6f33dfa09,6adc2e55f47c81919f6374e561687076,6af16bbad26881918c283abe06e1f32a,6adbbc9ad9e88191bea8eb69104afb25,6adcc98a80a48191b71a5156624c21df
REGISTERED_TASK_IDS=6adbb9fff1548191a1bd5b30d9e9984a,6af16bb9492c8191b8e8c7d606f11507,6adc2e4bd8a48191ac7d8641ed95a1a3,6aeacb7820ac8191b7d768a6f33dfa09,6adc2e55f47c81919f6374e561687076,6af16bbad26881918c283abe06e1f32a,6adbbc9ad9e88191bea8eb69104afb25,6adcc98a80a48191b71a5156624c21df
PHYSICAL_TASKS_REGISTERED=8
PHYSICAL_ALEX_TASKS_ENABLED_REPORTED=2
VAEP_DISPATCH_AUTHORIZED=FALSE
JULES_WORKERS_AUTHORIZED=0
LEGACY_AUTOMATION_RUNTIME=RETIRED
TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY
TASK_ASSIGNMENT_MODEL=OWNER_CONFIGURABLE
DESIGN_TOPOLOGY=OWNER_CONFIGURABLE
CONTRACT_VERSION=2.0
CAPACITY_POLICY_VERSION=V13_1000_FIFO_BATCH
MAX_CONCURRENT_SCOPES=1000
MAX_OPEN_WORK_BRANCHES=1000
MAX_OWNED_SCOPES_PER_TASK=1000
MAX_ACTIVE_WRITES_PER_TASK=1000
WORK_BRANCH_CREATION_ALLOWED=FALSE
REVIEW_ROUTE_MODE=DETERMINISTIC_AUTHOR_EXCLUSION
REVIEW_INDEPENDENCE_REQUIRED=TRUE
AUTO_CERTIFICATION_MODE=DIRECT_DEV_FOUR_AUDITS
QUALITY_PROTOCOL_VERSION=V13_A0_A5
GOAL_MAX_LISTO_PER_RUN=NONE_WITHIN_PROVIDER_RUN
TASK_REASONING_PREFERENCE=MAX_AVAILABLE_REASONING
TASK_IDENTITY_EVIDENCE_POLICY=ACCOUNT_TASK_CONTEXT_NATIVE_PEEK_AND_SOURCE_LINKS
TASK_FALSE_SELF_OWNED_POLICY=DIAGNOSTIC_ONLY_NOT_ACCOUNT_DENIAL
TASK_DEFINITION_404_POLICY=USE_PEEK_READBACK_NO_AUTO_DENIAL
TASK_CANDIDATE_TRIAGE=SCAN_ALL_ELIGIBLE_WITH_REAL_TOOLCHAIN_PREFLIGHT
TASK_NATIVE_REASONING_CONTROL=OWNER_OR_PLATFORM_SETTING
JAVIER_PEER_WATCH_SCOPE=SIX_EXISTING_TASK_IDS
JAVIER_PEER_AUTORESUME=AUTHORIZED_SINGLE_ATTEMPT_WITH_READBACK
ALEX_PEER_WATCH_SCOPE=OWNER_ONLY_AFTER_ACTIVATION
FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=FALSE
FORMAL_APPROVAL_GOVERNANCE_REQUIRED=FALSE
GOAL_MIN_LISTO_PER_RUN=1
GOAL_TARGET_LISTO_PER_RUN=3
CONTINUE_WHILE_ELIGIBLE=TRUE
EXECUTION_STYLE=SERIAL_DIRECT_DEV_FAST_FORWARD
STAGES=PENDIENTE,EN_PROGRESO,VALIDANDO,LISTO,BLOQUEADO,CANCELADO
TIMEZONE=America/Tegucigalpa

- Regla V13: hito 3 sin máximo de cierres durante una corrida. Tras cada LISTO_DEV verificar HEAD, dependencias, colisiones y tomar otra PENDIENTE elegible con A0–A4 de nuevo; no pausar por cuota. Si proveedor interrumpe, registrar estado y causa. La lectura anterior de PRIMARY DEV 03 Javier :20 fue PAUSADA con requires_user_action; en la lectura nueva las seis Javier estaban enabled. El propietario autorizó un intento nativo seguro de reactivación por compañero Javier con readback, sin autoaprobar permisos.
- Readback actual: 6/6 Tasks Javier habilitadas; PRIMARY DEV 03 había estado pausada por requires_user_action, pero el propietario la reanudó y su estado actual es enabled. Las seis están ahora autorizadas para vigilancia cruzada entre sí y reactivación nativa de una compañera pausada mediante un intento por ID y readback. Alex mantiene sus dos IDs bajo su propia cuenta. Preferencia máxima de razonamiento, sin ajuste ni verificación nativos disponibles por conector.
- Incidencia de productividad 2026-11-12: DEV 01 y 05 reportaron NO_GO por interpretar `is_self_owned_automation=false` y `Automations.get 404` como pérdida de identidad. Son señales insuficientes; en adelante aplicar evidencia positiva de TASK_ID, prompt físico, conexión Javier, `Automations.peek`, Skill/Scope Lock exact-HEAD y accesos GitHub/Sheet. 37 tareas elegibles detectadas en el corte, ninguna con dueño; una limitación .NET/Angular de un ID no bloquea otras. Siete celdas COLA!E históricas se restauraron a fórmula viva sobre PLAN_MAESTRO!H (108,110,118,120,127,128,249); conservar notas y J:M históricos. Revisar conteos vivos, no asumir 37 eternamente.
## REGISTRO HISTÓRICO — suspensión del 2026-11-12 20:56 (NO VIGENTE, revocada por reanudación V5)

ORDEN POSTERIOR EXPRESA QUE PREVALECE SOBRE V13 ANTERIOR. Las seis Tasks físicas JAVIER_CHATGPT quedan is_enabled=false por cambio pendiente del plan de implementación. El readback del conector confirmó 6/6 pausadas y sus prompts físicos contienen OWNER_PAUSE_HOLD. Ninguna Task puede reclamar una nueva microtarea, iniciar A5 independiente, entrar a PENDIENTE→EN_PROGRESO, publicar cambios nuevos no iniciados ni reactivar a otra Task. La regla de recuperación cruzada entre Tasks queda SUSPENDIDA, aunque el catálogo de ocho IDs autorizados permanezca. No reactivar ninguna Javier hasta nueva instrucción expresa del propietario; no confundir esta pausa autorizada con un error del proveedor.

CIERRE SEGURO: al hacer la pausa, DEV 03/04/05/06 se veían in_progress; DEV 01/02 completed. Desactivar is_enabled impide disparos programados nuevos, pero NO garantiza que un run ya iniciado haya recibido el cambio de prompt ni que se haya cancelado. Si una ejecución recibe la orden, sólo terminará la microtarea reclamada antes de la pausa, con las auditorías pertinentes y readback, y terminará sin reclamar otra. No inventar cierres ni cambiar tareas ya reclamadas arbitrariamente.

ALEX: la cuenta propietaria de las Tasks 07 (:05) y 08 (:35) es distinta y NO está administrable desde esta sesión. El propietario solicitó su suspensión; el estado físico de Alex no está confirmado como PAUSADO. Requiere ejecución desde cuenta Alex. No usar conexiones cruzadas ni declarar 8/8 pausadas.

CONSERVAR: ocho IDs, RRULE, zonas, historial, fuente de plan, seis controles A0–A5, arquitectura y límites QA/main/PROD. Nueva activación sólo con orden posterior del propietario y revalidación del plan y los permisos.


## Interpretación efectiva
- `AUTOMATION_RUNTIME=ACTIVE_DEV` indica que el contrato contempla DEV; NO afirma que las Tasks se encuentren físicamente habilitadas. En el readback de 2026-11-12 las seis Tasks Javier están habilitadas (`PHYSICAL_JAVIER_TASKS_ENABLED=6`). Alex transmitió verificación nativa desde su propia cuenta: 2/2 habilitadas; no equivale a lectura directa desde Javier. No reactivar por cambiar documentación.
- Las constantes `V13_1000_FIFO_BATCH` y `MAX_OPEN_WORK_BRANCHES=1000` se retienen para compatibilidad con validadores instalados; NO habilitan FIFO de PR, trabajo en branches ni PR nuevos. La regla ejecutiva efectiva es `WORK_BRANCH_CREATION_ALLOWED=FALSE` y publicación serial a `dev`.
- `FORMAL_APPROVAL_GOVERNANCE_REQUIRED=FALSE` se limita al workflow directo de dev. Cualquier protección que subsista en `qa` o `main` mantiene sus obligaciones nativas.
- En el protocolo A0–A5, A0 calidad estática es obligatoria en VALIDANDO antes de A1–A3; A4 verifica el commit publicado; LISTO_DEV requiere A0–A4 PASS y A5_PENDIENTE registrado; revisión A5 de otro TASK_ID obligatoria antes de QA/PROD, sin bloquear la cola de dev. No certificar seis PASS si A5 aún no se ejecutó. Los seis títulos Javier son secuenciales por hora de ejecución; IDs y RRULE intactos.
- Las seis Tasks Javier ejecutan V13 directa en dev con el orden: al seleccionar PENDIENTE, pasar inmediatamente a EN_PROGRESO con TASK_ID; tras acabar implementación candidata, pasar a VALIDANDO **antes** de A1 funcional, A2 seguridad y A3 integración; mismo Scope audita, corrige, publica commit fast-forward dev, hace A4 posintegración y sólo entonces marca LISTO con evidencia. Otros Scopes toman IDs PENDIENTE libres en su disparo, sin esperar cierres ajenos. Meta mínima 1 LISTO, objetivo 3 por corrida; si se corta una tarea en VALIDANDO, mantener responsable/huella para continuación, no fingir LISTO. Las dos Tasks Alex siguen bajo su cuenta propietaria.
- La selección y semántica de etapas se definen exclusivamente en `docs/AUTOMATION_V13.md`. No existe un contrato activo de PR FIFO/revisión formal para DEV.
- **SIN BLOQUEO EN SHEETS:** evitar reservas artificiales; preservar validación de autoría, concurrencia, ID y fórmulas de PLAN/COLA. BITACORA: 26 campos A:Z, EVENT_ID e IDEMPOTENCY_KEY únicos, append contiguo. Auditoría 2026-11-13: los prompts Javier y docs/AUTOMATION_V13.md corrigen la mención histórica de 21 campos; no reescribir eventos previos.

## Tasks registradas
| Minuto | Cuenta | Función | TASK_ID |
|---|---|---|---|
| :00 | JAVIER_CHATGPT | PRIMARY DEV 01 | `6adbb9fff1548191a1bd5b30d9e9984a` |
| :10 | JAVIER_CHATGPT | PRIMARY DEV 02 | `6af16bb9492c8191b8e8c7d606f11507` |
| :20 | JAVIER_CHATGPT | PRIMARY DEV 03 | `6adc2e4bd8a48191ac7d8641ed95a1a3` |
| :30 | JAVIER_CHATGPT | PRIMARY DEV 04 | `6aeacb7820ac8191b7d768a6f33dfa09` |
| :40 | JAVIER_CHATGPT | PRIMARY DEV 05 | `6adc2e55f47c81919f6374e561687076` |
| :50 | JAVIER_CHATGPT | PRIMARY DEV 06 | `6af16bbad26881918c283abe06e1f32a` |
| :05 | ALEX_CHATGPT | PRIMARY DEV 07 | `6adbbc9ad9e88191bea8eb69104afb25` |
| :35 | ALEX_CHATGPT | PRIMARY DEV 08 | `6adcc98a80a48191b71a5156624c21df` |

## Alex: incorporación 6→8 notificada como completada

Según certificación comunicada por Alex el 12-nov-2026, ambas Tasks fueron leídas directamente desde SU cuenta y tienen is_enabled=true, V13 A0–A5, vigilancia mutua, producción sin techo artificial y preferencia máxima. Sus últimas corridas constan completed y requires_user_action=false. Esta información es evidencia transmitida por el propietario; NO es una lectura independiente desde Javier. El modelo/razonamiento real y un rescate mutuo durante una pausa aún no están comprobados.

- PRIMARY DEV 07 — ALEX, TASK_ID 6adbbc9ad9e88191bea8eb69104afb25, FREQ=HOURLY;BYMINUTE=5;BYSECOND=0, America/Tegucigalpa
- PRIMARY DEV 08 — ALEX, TASK_ID 6adcc98a80a48191b71a5156624c21df, FREQ=HOURLY;BYMINUTE=35;BYSECOND=0, America/Tegucigalpa

No editar Tasks físicas desde la cuenta de Javier; las verificaciones futuras del estado de Alex pertenecen a Alex. Mantener A0–A5 y las restricciones de qa/main/PROD.

## Fuentes autorizadas
- Plan Drive: https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit
- Sheet operativo: https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit
- `docs/AUTOMATION_AUTHORITY.md` y `AGENTS.md`: interpretan este runtime; ninguna evidencia previa altera esta configuración.


## Orden posterior expresa de arranque V5 — 2026-11-12 (prevalece sobre la pausa del 20:56)

- El propietario AUTORIZA la reanudación de las seis Tasks JAVIER_CHATGPT únicamente después de certificar todos los checks requeridos en SUCCESS en el SHA exacto del nuevo commit base de cierre. Hasta el readback de reanudación, los seis `is_enabled=false` continúan describiendo el estado físico real; la pausa anterior es HISTÓRICA y deja de ordenar la ejecución una vez cumplida esta condición.
- Nueva fuente de trabajo: Google Doc canónico `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, V5 FINAL; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. V5 contiene 2.340 microtareas `C00-01.M01.01` a `C15-05.M06.06`, 16 bloques, 65 fases y 390 microfases. El antiguo Plan Comercial V3/V13 y sus IDs históricos no son fuente de nuevos claims; V13 A0–A5 conserva sólo el protocolo de calidad y ejecución.
- `PLAN_MAESTRO!H` es estado único editable con evidencia y responsable; `COLA!A:O` son fórmulas y `COLA!P:Q` calculan dependencias explícitas por fase sin tope de seis. Candidata real: PENDIENTE, sin responsable, `COLA!G=SI` y `COLA!O=SI`, permisos reales, evaluación de factibilidad y exclusión por TASK_ID/ID. No reclamar automáticamente todo lo que esté en SI ni crear duplicados; múltiples IDs independientes pueden avanzar en paralelo.
- Mismo contrato V13: tareas individuales de tamaño ejecutable; PENDIENTE → EN_PROGRESO → VALIDANDO → LISTO únicamente con A0–A4 PASS, commit fast-forward en dev, CI causal SHA exacto y readback PLAN/COLA/BITACORA; A5 por otro TASK_ID para promoción QA/main/PROD, nunca autopromoción. Meta 1 LISTO, hito 3 no techo; continuar mientras haya tiempo/proveedor, dependencias y seguridad. No crear ramas/PR, no modificar QA/main/PROD ni datos existentes de usuarios/empresas.
- RESTAURAR vigilancia y recuperación cruzada exclusivamente entre seis Tasks Javier, IDs y horarios originales :00/:10/:20/:30/:40/:50. Una Task habilitada comprueba los seis IDs y puede intentar `is_enabled=true` UNA vez por compañera pausada si tiene permisos, no existe nueva orden de pausa ni acción humana obligatoria sin resolver; releer estado y registrar evidencia o error. Nunca suplantar, aprobar permisos ni reactivar Tasks de Alex desde Javier. Conservar sus dos IDs :05/:35 para gestión autónoma de su propietario y recuperación mutua sólo en esa cuenta.
- La eliminación física de filas/columnas auxiliares está verificada: DASHBOARD 40×10; CONFIG 150×3; LEYENDA 80×3; TAREAS_PROGRAMADAS 20×26; AUTOMATIZACIONES 20×18; PLAN_MAESTRO 2341×15; COLA 2341×17. BITACORA no tiene límite de crecimiento por esta decisión. Ningún bloqueo estructural impide crecimiento legítimo de BITACORA.
- C10-03 no requiere migrar imágenes inexistentes: inventario real cero → no hay exportaciones ni checksums históricos. Mantener capacidad de archivos futuros segura. La tarea `C15-05.M06.06` es terminal y sólo se habilita con evidencia de cierre de las 2.339 anteriores.
- Jira `KAN-4` es tablero informativo, NO fuente de backlog duplicado ni gate para LISTO. Los respaldos de la estructura y de los seis prompts antes de reactivación quedan preservados; no cambiar títulos, RRULE, zonas, roles, IDs, parámetros de razonamiento ni vigilancia existente fuera del ajuste temporal de pausa.
- Validación de arranque: CI exact-HEAD requerida = SUCCESS, Scope Lock SUCCESS, fuente Google Sheet íntegra, conectores autorizados, y readback de cada Task tras reanudar. Un check SKIPPED puramente auxiliar se identifica y no se falsea como PASS. Un fallo crítico o acciones humanas pendientes bloquean sólo la operación correspondiente.
