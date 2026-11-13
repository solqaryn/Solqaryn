# AGENTS — SOLQARYN (instrucciones operativas vigentes)

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
## SUSPENSION EJECUTIVA DEL PROPIETARIO — 2026-11-12 20:56 America/Tegucigalpa (PRIORIDAD MAXIMA)

ORDEN POSTERIOR EXPRESA QUE PREVALECE SOBRE V13 ANTERIOR. Las seis Tasks físicas JAVIER_CHATGPT quedan is_enabled=false por cambio pendiente del plan de implementación. El readback del conector confirmó 6/6 pausadas y sus prompts físicos contienen OWNER_PAUSE_HOLD. Ninguna Task puede reclamar una nueva microtarea, iniciar A5 independiente, entrar a PENDIENTE→EN_PROGRESO, publicar cambios nuevos no iniciados ni reactivar a otra Task. La regla de recuperación cruzada entre Tasks queda SUSPENDIDA, aunque el catálogo de ocho IDs autorizados permanezca. No reactivar ninguna Javier hasta nueva instrucción expresa del propietario; no confundir esta pausa autorizada con un error del proveedor.

CIERRE SEGURO: al hacer la pausa, DEV 03/04/05/06 se veían in_progress; DEV 01/02 completed. Desactivar is_enabled impide disparos programados nuevos, pero NO garantiza que un run ya iniciado haya recibido el cambio de prompt ni que se haya cancelado. Si una ejecución recibe la orden, sólo terminará la microtarea reclamada antes de la pausa, con las auditorías pertinentes y readback, y terminará sin reclamar otra. No inventar cierres ni cambiar tareas ya reclamadas arbitrariamente.

ALEX: la cuenta propietaria de las Tasks 07 (:05) y 08 (:35) es distinta y NO está administrable desde esta sesión. El propietario solicitó su suspensión; el estado físico de Alex no está confirmado como PAUSADO. Requiere ejecución desde cuenta Alex. No usar conexiones cruzadas ni declarar 8/8 pausadas.

CONSERVAR: ocho IDs, RRULE, zonas, historial, fuente de plan, seis controles A0–A5, arquitectura y límites QA/main/PROD. Nueva activación sólo con orden posterior del propietario y revalidación del plan y los permisos.


## Autoridad exacta
- Leer siempre Skill y su copia exacta en HEAD dev, docs/PROJECT_SCOPE_LOCK.md, docs/AUTOMATION_STATUS.md, docs/AUTOMATION_AUTHORITY.md y **docs/AUTOMATION_V13.md**. La única fuente de alcance es el Plan Comercial Inicial. Ningún histórico de otra versión impone condiciones.
- SIN BLOQUEO EN SHEETS: no usar locks artificiales; sí respetar ID, TASK_ID, edición concurrente y fórmulas. Las seis Tasks Javier están HABILITADAS según readback nativo del 12-nov-2026; Alex bajo su propia cuenta. No crear ni modificar Tasks salvo reactivación autorizada de las seis Javier con verificación de identidad y readback; no tocar Alex.
- DEV: commits directos fast-forward, serializados, sin PR/branch de trabajo ni espera de GitHub APPPROVED/required checks de un ruleset retirado. GitHub Actions siguen aportando evidencia; resolver defectos de seguridad/funcionalidad reales. No tocar main/qa/prod sin permiso.
- Control adicional obligatorio: A0 estática antes de publicar y LISTO; A5 revisión independiente posdev de distinto TASK_ID antes de cualquier promoción a QA/PROD. En cierre dev registrar A0–A4 PASS + A5_PENDIENTE; ningún A5 ficticio, sin bloquear productividad de dev por revisión ajena. El protocolo completo está en docs/AUTOMATION_V13.md.
- En cada corrida habilitada intentar cerrar 1 microtarea LISTO, meta 3. Estados exactos y visibles: PENDIENTE→EN_PROGRESO en claim; EN_PROGRESO→VALIDANDO tras implementar y ANTES de auditorías A1/A2/A3; el mismo Scope permanece responsable durante esas auditorías, corrige defectos e integra commit directo dev; A4 posintegración en VALIDANDO; finalmente LISTO con cuatro PASS, SHA dev y readback PLAN/COLA/BITACORA. Otros Scopes pueden empezar otras PENDIENTE elegibles desde su disparo, sin esperar a que termine un Scope ajeno. Si se agota tiempo, conservar VALIDANDO/EN_PROGRESO con responsable, evidencia y siguiente acción; jamás fingir LISTO.
- Conservar JWT, BCrypt, RBAC, EmpresaId A/B, MySQL, secretos protegidos, seguridad y pruebas proporcionadas. Revalidar HEAD, permisos y dependencias; nada de force-push, reset destructivo o suplantación de identidades.
- Alex reportó readback nativo 2/2 enabled y prompts V13 A0–A5 desde su propia cuenta (12-nov-2026): PRIMARY DEV 07 :05 (ID 6adbbc9ad9e88191bea8eb69104afb25) y PRIMARY DEV 08 :35 (ID 6adcc98a80a48191b71a5156624c21df). Fuente: certificación del propietario comunicada, no acceso independiente desde Javier. No administrar Tasks de Alex con credenciales Javier; no afirmar máximo razonamiento nativo.
- Ejecución exclusiva en SOLQARYN; no utilizar otros proyectos ni referencias externas fuera de la allowlist técnica.
