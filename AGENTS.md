# AGENTS — SOLQARYN (instrucciones operativas vigentes)

> **ESTADO OPERATIVO V5 FINAL (readback Javier 2026-11-13).** Seis Tasks Javier originales habilitadas (is_enabled=true, 6/6); pausa del 12-nov HISTORICA REVOCADA por orden posterior. Alex solo desde cuenta propietaria. Fuente ejecutable unica: Google Doc V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, 2.340 IDs C00-01.M01.01 a C15-05.M06.06. V13 mantiene A0–A5 y seguridad, no el backlog antiguo. El commit base `f4b8528f57e82625d343dd1590076df44cacaee2` es antecesor del HEAD `4d582aa48dcdf8e9efb8d4c15ec229cbc0d603a5` (13 commits de avance); la autoridad de codigo/CI es siempre HEAD vivo y SHA exacto del candidato, no un SHA congelado. Validar con GitHub Actions (dotnet/Angular/Node/Docker/MySQL cuando los jobs pertinentes realmente corren); sin PASS si el job es SKIPPED o el SHA es anterior. Preservar horarios, IDs y recuperacion cruzada; no fingir nuevas microtareas LISTO.


PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=ACTIVE_DEV
TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET
TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY
CURRENT_REGISTERED_TASKS=8
CAPACITY_POLICY_VERSION=V13_1000_FIFO_BATCH
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
WORK_BRANCH_CREATION_ALLOWED=FALSE
GOAL_MIN_LISTO_PER_RUN=1
GOAL_TARGET_LISTO_PER_RUN=3
CONTINUE_WHILE_ELIGIBLE=TRUE

- Productividad: tres LISTO son un hito, nunca un máximo. Después de cada tarea LISTO_DEV, continuar otra PENDIENTE elegible en la misma corrida si quedan recursos reales, repitiendo A0–A4 y registrando A5_PENDIENTE. No autopausar. Si el proveedor exige intervención, reportar TASK_ID y run_id, sin bypass.
- Ante una compañera Javier pausada: comprobar titularidad, TASK_ID y permisos; un intento de update is_enabled=true, readback y aviso del resultado. La orden de mantener las seis Javier activas está vigente; no cambiar otras opciones ni tocar Tasks Alex. Si un requisito humano persiste no eludirlo ni falsificar reactivación. Usar la preferencia de razonamiento máximo, pero un prompt no cambia ni verifica el modelo real.
- Identidad Javier: `is_self_owned_automation=false` visto desde otra conversación y `Automations.get` 404 de acción no prueban falta de permiso; validar positivamente ID físico del prompt/allowlist/peek, cuenta Javier real, accesos de GitHub/Sheet, Skill y SHA exacto. No confundir indicador neutral con autorización, ni aceptar identidad basada solamente en prompt. Seleccionar por escaneo completo de PENDIENTE y `DESARROLLO_HABILITADO=SI`, dependencias, propietario, riesgos y herramientas requeridas; un ID que exija .NET/Angular/entorno live no bloquea otras tareas factibles. COLA!E debe ser espejo por fórmula de PLAN_MAESTRO!H; siete valores manuales antiguos fueron reparados con nota. Prohibido simular auditorías, suplantar cuentas o alterar QA/PROD.
## REGISTRO HISTÓRICO — suspensión del 2026-11-12 20:56 (NO VIGENTE, revocada por reanudación V5)

ORDEN POSTERIOR EXPRESA QUE PREVALECE SOBRE V13 ANTERIOR. Las seis Tasks físicas JAVIER_CHATGPT quedan is_enabled=false por cambio pendiente del plan de implementación. El readback del conector confirmó 6/6 pausadas y sus prompts físicos contienen OWNER_PAUSE_HOLD. Ninguna Task puede reclamar una nueva microtarea, iniciar A5 independiente, entrar a PENDIENTE→EN_PROGRESO, publicar cambios nuevos no iniciados ni reactivar a otra Task. La regla de recuperación cruzada entre Tasks queda SUSPENDIDA, aunque el catálogo de ocho IDs autorizados permanezca. No reactivar ninguna Javier hasta nueva instrucción expresa del propietario; no confundir esta pausa autorizada con un error del proveedor.

CIERRE SEGURO: al hacer la pausa, DEV 03/04/05/06 se veían in_progress; DEV 01/02 completed. Desactivar is_enabled impide disparos programados nuevos, pero NO garantiza que un run ya iniciado haya recibido el cambio de prompt ni que se haya cancelado. Si una ejecución recibe la orden, sólo terminará la microtarea reclamada antes de la pausa, con las auditorías pertinentes y readback, y terminará sin reclamar otra. No inventar cierres ni cambiar tareas ya reclamadas arbitrariamente.

ALEX: la cuenta propietaria de las Tasks 07 (:05) y 08 (:35) es distinta y NO está administrable desde esta sesión. El propietario solicitó su suspensión; el estado físico de Alex no está confirmado como PAUSADO. Requiere ejecución desde cuenta Alex. No usar conexiones cruzadas ni declarar 8/8 pausadas.

CONSERVAR: ocho IDs, RRULE, zonas, historial, fuente de plan, seis controles A0–A5, arquitectura y límites QA/main/PROD. Nueva activación sólo con orden posterior del propietario y revalidación del plan y los permisos.


## Autoridad exacta
- Leer siempre Skill y su copia exacta en HEAD dev, docs/PROJECT_SCOPE_LOCK.md, docs/AUTOMATION_STATUS.md, docs/AUTOMATION_AUTHORITY.md y **docs/AUTOMATION_V13.md**. La única fuente de microtareas nuevas es el plan V5 FINAL identificado por EXECUTION_PLAN_DRIVE_FILE_ID; V13 conserva el protocolo A0–A5 y la seguridad. Ningún histórico de otra versión impone condiciones.
- SIN BLOQUEO EN SHEETS: no usar locks artificiales; sí respetar ID, TASK_ID, edición concurrente y fórmulas. Las seis Tasks Javier están HABILITADAS según readback nativo del 12-nov-2026; Alex bajo su propia cuenta. No crear ni modificar Tasks salvo reactivación autorizada de las seis Javier con verificación de identidad y readback; no tocar Alex.
- DEV: commits directos fast-forward, serializados, sin PR/branch de trabajo ni espera de GitHub APPPROVED/required checks de un ruleset retirado. GitHub Actions siguen aportando evidencia; resolver defectos de seguridad/funcionalidad reales. No tocar main/qa/prod sin permiso.
- Control adicional obligatorio: A0 estática antes de publicar y LISTO; A5 revisión independiente posdev de distinto TASK_ID antes de cualquier promoción a QA/PROD. En cierre dev registrar A0–A4 PASS + A5_PENDIENTE; ningún A5 ficticio, sin bloquear productividad de dev por revisión ajena. El protocolo completo está en docs/AUTOMATION_V13.md.
- En cada corrida habilitada intentar cerrar 1 microtarea LISTO, meta 3. Estados exactos y visibles: PENDIENTE→EN_PROGRESO en claim; EN_PROGRESO→VALIDANDO tras implementar y ANTES de auditorías A1/A2/A3; el mismo Scope permanece responsable durante esas auditorías, corrige defectos e integra commit directo dev; A4 posintegración en VALIDANDO; finalmente LISTO con cuatro PASS, SHA dev y readback PLAN/COLA/BITACORA. Otros Scopes pueden empezar otras PENDIENTE elegibles desde su disparo, sin esperar a que termine un Scope ajeno. Si se agota tiempo, conservar VALIDANDO/EN_PROGRESO con responsable, evidencia y siguiente acción; jamás fingir LISTO.
- Conservar JWT, BCrypt, RBAC, EmpresaId A/B, MySQL, secretos protegidos, seguridad y pruebas proporcionadas. Revalidar HEAD, permisos y dependencias; nada de force-push, reset destructivo o suplantación de identidades.
- Alex reportó readback nativo 2/2 enabled y prompts V13 A0–A5 desde su propia cuenta (12-nov-2026): PRIMARY DEV 07 :05 (ID 6adbbc9ad9e88191bea8eb69104afb25) y PRIMARY DEV 08 :35 (ID 6adcc98a80a48191b71a5156624c21df). Fuente: certificación del propietario comunicada, no acceso independiente desde Javier. No administrar Tasks de Alex con credenciales Javier; no afirmar máximo razonamiento nativo.
- Ejecución exclusiva en SOLQARYN; no utilizar otros proyectos ni referencias externas fuera de la allowlist técnica.


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
