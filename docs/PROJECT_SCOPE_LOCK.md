# PROJECT_SCOPE_LOCK — SOLQARYN (estado vigente)

PROJECT_SCOPE_LOCK=STRICT
PLATFORM=SOLQARYN
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
LOCAL_SKILL_COUNT=1
LOCAL_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
CONTEXT_MODE=CURRENT_STATE_ONLY
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY
LEGACY_MASTER_EXECUTABLE=FALSE
TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY
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
WORK_BRANCH_CREATION_ALLOWED=FALSE

- Continuidad: GOAL_MAX_LISTO_PER_RUN=NONE_WITHIN_PROVIDER_RUN; se permite superar el hito 3 mientras haya elegibilidad, recursos y pruebas, pero nunca omitir auditorías ni superar límites o suspensiones del proveedor.
- La autorización vigente permite exclusivamente recuperación física entre seis Javier por la misma cuenta: un intento de is_enabled=true de una hermana pausada, readback y registro; nunca autoaprobar permisos ni habilitar Alex. El razonamiento máximo es una preferencia de operación, no un ajuste verificable desde el conector.
- Evidencia de identidad permitida: TASK_ID/propietario y prompt de la Task, consulta nativa `peek` real, conexiones autenticadas Javier GitHub/Drive y Skill/Scope Lock de SHA exacto. Un `is_self_owned_automation=false` relativo al agente consultante o un `get 404 Action not found` NO son denegación de cuenta ni permiso positivo; denegación efectiva sigue siendo NO_GO. Candidatas PENDIENTE deben evaluarse todas por factibilidad y seguridad sin bloquear cola por una herramienta faltante. Siete estados antiguos de COLA fueron sincronizados al PLAN mediante fórmulas, conservando evidencia.
## SUSPENSION EJECUTIVA DEL PROPIETARIO — 2026-11-12 20:56 America/Tegucigalpa (PRIORIDAD MAXIMA)

ORDEN POSTERIOR EXPRESA QUE PREVALECE SOBRE V13 ANTERIOR. Las seis Tasks físicas JAVIER_CHATGPT quedan is_enabled=false por cambio pendiente del plan de implementación. El readback del conector confirmó 6/6 pausadas y sus prompts físicos contienen OWNER_PAUSE_HOLD. Ninguna Task puede reclamar una nueva microtarea, iniciar A5 independiente, entrar a PENDIENTE→EN_PROGRESO, publicar cambios nuevos no iniciados ni reactivar a otra Task. La regla de recuperación cruzada entre Tasks queda SUSPENDIDA, aunque el catálogo de ocho IDs autorizados permanezca. No reactivar ninguna Javier hasta nueva instrucción expresa del propietario; no confundir esta pausa autorizada con un error del proveedor.

CIERRE SEGURO: al hacer la pausa, DEV 03/04/05/06 se veían in_progress; DEV 01/02 completed. Desactivar is_enabled impide disparos programados nuevos, pero NO garantiza que un run ya iniciado haya recibido el cambio de prompt ni que se haya cancelado. Si una ejecución recibe la orden, sólo terminará la microtarea reclamada antes de la pausa, con las auditorías pertinentes y readback, y terminará sin reclamar otra. No inventar cierres ni cambiar tareas ya reclamadas arbitrariamente.

ALEX: la cuenta propietaria de las Tasks 07 (:05) y 08 (:35) es distinta y NO está administrable desde esta sesión. El propietario solicitó su suspensión; el estado físico de Alex no está confirmado como PAUSADO. Requiere ejecución desde cuenta Alex. No usar conexiones cruzadas ni declarar 8/8 pausadas.

CONSERVAR: ocho IDs, RRULE, zonas, historial, fuente de plan, seis controles A0–A5, arquitectura y límites QA/main/PROD. Nueva activación sólo con orden posterior del propietario y revalidación del plan y los permisos.


## Límite de proyecto y seguridad
- Este Scope Lock identifica exclusivamente al proyecto SOLQARYN, no a un ruleset GitHub. La rama dev carece del antiguo ruleset de PR/CI; main y qa conservan protección independiente.
- Escritura directa y serializada a dev, HEAD verificado, sin force-push, sin crear PR o ramas auxiliares. SIN BLOQUEO EN SHEETS: no aplicar reservas artificiales, pero sí impedir que dos Tasks sobrescriban el mismo ID.
- Plan Comercial Inicial único; límites de arquitectura Angular 22.2.1, .NET 10, MySQL EF Core/Oracle, JWT, RBAC, BCrypt y aislamiento A/B. Cuatro auditorías con evidencia antes de LISTO, prueba de SHA publicado y estado PLAN/COLA/BITACORA. Las cuatro auditorías no excusan un fallo real.
- Tasks físicas conservadas y pausadas según titular; sin crear nuevas ni extender poderes. No consultar otras cuentas/proyectos por fallback. Nada de acceso a QA/main/PROD, secrets o datos productivos sin autorización específica.
- Calidad V13: A0 análisis estático y A1–A4 son gate de LISTO_DEV; A5 requiere segundo TASK_ID y PASS para promover a qa/main/PROD, nunca autorización implícita. Reanudar solo las Tasks Javier dentro del permiso explícito del propietario y las facultades reales del proveedor, con readback; nunca aprobar o saltarse acciones pendientes de terceros.
- No reactivar modelos retirados ni planes previos. El contrato operativo único es docs/AUTOMATION_V13.md; el estado actual y censo, docs/AUTOMATION_STATUS.md; separación de cuentas, docs/GITHUB_ACCOUNT_ROLES.md. Incorporación 6→8 reportada como completada por Alex desde su cuenta, con IDs originales y horarios :05/:35. La constancia no confiere acceso a su cuenta ni autoriza QA/main/PROD.


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
