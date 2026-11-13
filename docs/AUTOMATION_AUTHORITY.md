# SOLQARYN — Autoridad actual de automatizaciones (V13 Direct DEV)

**Único contrato de ejecución:** `docs/AUTOMATION_V13.md`. Sin gates PR de GitHub en dev; A0–A4 obligatorias para cerrar LISTO y A5 independiente obligatoria antes de QA/PROD y commit directo a dev. Nunca confundir aprobación de repositorio con autorización sobre permisos reales ni cambiar QA/main/PROD.

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=ACTIVE_DEV
TASK_DISPATCH_STATE=PAUSED_BY_OWNER_2026_11_12
JAVIER_PHYSICAL_TASKS_PAUSED=6
JAVIER_PEER_AUTORESUME_EFFECTIVE=DISABLED_BY_OWNER
ALEX_PHYSICAL_PAUSE_STATUS=UNVERIFIED_OWNER_ACCOUNT
PHYSICAL_JAVIER_TASKS_ENABLED=0
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

- Continuidad: primer LISTO objetivo mínimo, 3 hito no máximo; completar todas las microtareas reales factibles en la misma corrida, sin perder A0–A5. Una suspensión del proveedor requiere aviso con run_id y causa si existe; no se soluciona por mero prompt ni se auto reactiva sin resolver permisos.
- El propietario autorizó que cualquiera de las seis Tasks Javier habilitadas intente reanudar otra Task Javier pausada usando solo su TASK_ID y un update is_enabled=true de la misma cuenta, con lectura confirmatoria, un intento por corrida y registro del run_id. Nunca modificar Alex ni concederse permisos. La elección de razonamiento máximo es del selector nativo del propietario; el conector de Tasks no dispone de parámetro para imponerla.
- Contrato de identidad y selección actual: no usar `is_self_owned_automation=false` ni `Automations.get 404` como único motivo de NO_GO; son diagnósticos del agente/herramienta consultante, no revocaciones. Probar Task ID del prompt y allowlist, propietario, `peek` + enabled/horario, conectores GitHub/Drive del mismo titular y Skill/Scope Lock exact-HEAD antes de atribuir operación. Si hay denegación real, bloquear sólo la operación sin prueba, nunca suplantar. Leer todas las PENDIENTE elegibles, seleccionar una cuya A0–A4 pueda acreditarse, y continuar otras si falta .NET/Angular/proveedor para la primera. Los siete estados manuales divergentes en COLA se reconciliaron a fórmula `PLAN_MAESTRO!H`, con notas de auditoría.
## SUSPENSION EJECUTIVA DEL PROPIETARIO — 2026-11-12 20:56 America/Tegucigalpa (PRIORIDAD MAXIMA)

ORDEN POSTERIOR EXPRESA QUE PREVALECE SOBRE V13 ANTERIOR. Las seis Tasks físicas JAVIER_CHATGPT quedan is_enabled=false por cambio pendiente del plan de implementación. El readback del conector confirmó 6/6 pausadas y sus prompts físicos contienen OWNER_PAUSE_HOLD. Ninguna Task puede reclamar una nueva microtarea, iniciar A5 independiente, entrar a PENDIENTE→EN_PROGRESO, publicar cambios nuevos no iniciados ni reactivar a otra Task. La regla de recuperación cruzada entre Tasks queda SUSPENDIDA, aunque el catálogo de ocho IDs autorizados permanezca. No reactivar ninguna Javier hasta nueva instrucción expresa del propietario; no confundir esta pausa autorizada con un error del proveedor.

CIERRE SEGURO: al hacer la pausa, DEV 03/04/05/06 se veían in_progress; DEV 01/02 completed. Desactivar is_enabled impide disparos programados nuevos, pero NO garantiza que un run ya iniciado haya recibido el cambio de prompt ni que se haya cancelado. Si una ejecución recibe la orden, sólo terminará la microtarea reclamada antes de la pausa, con las auditorías pertinentes y readback, y terminará sin reclamar otra. No inventar cierres ni cambiar tareas ya reclamadas arbitrariamente.

ALEX: la cuenta propietaria de las Tasks 07 (:05) y 08 (:35) es distinta y NO está administrable desde esta sesión. El propietario solicitó su suspensión; el estado físico de Alex no está confirmado como PAUSADO. Requiere ejecución desde cuenta Alex. No usar conexiones cruzadas ni declarar 8/8 pausadas.

CONSERVAR: ocho IDs, RRULE, zonas, historial, fuente de plan, seis controles A0–A5, arquitectura y límites QA/main/PROD. Nueva activación sólo con orden posterior del propietario y revalidación del plan y los permisos.


## Propiedad e identidad
- Cuenta titular JAVIER_CHATGPT: seis Tasks :00, :10, :20, :30, :40 y :50. Cuenta ALEX_CHATGPT: dos Tasks :05 y :35, según reporte de Alex. Estas ocho identidades conservan su ID físico, horarios y titularidad; sólo el propietario de cada cuenta cambia sus Tasks. Javier tiene seis habilitadas en la última lectura. Alex reportó lectura nativa y 2/2 habilitadas el 12-nov-2026; no es comprobación independiente desde Javier.
- PROPIETARIA_INSTITUCIONAL: SOLQARYN Platform. EJECUTOR_PRINCIPAL_01: Javier. EJECUTOR_PRINCIPAL_02: Alex. Ninguna identidad tiene idéntica autoridad de credenciales; los permisos efectivos prevalecen sobre cualquier descripción.
- ACTOR_HUMANO firma órdenes del propietario; CREADOR_TASK y ACTOR_AUTOMATIZADO usan TASK_ID válido. El RUN_ID/evento de la plataforma no se inventa. En cada tarea, EVENT_ID e IDEMPOTENCY_KEY evitan duplicaciones; REVIEWER/REVISOR debe ser independiente de la autoría al desempeñar una auditoría externa.
- Separación de deberes: un ejecutor no puede autoaprobarlo como otro usuario, ni usar tokens de Alex/Javier/SOLQARYN intercambiablemente. Revisión cruzada es recomendable cuando tiene lugar, pero la Task puede realizar sus cuatro auditorías técnicas con pruebas sin esperar aprobación GitHub cuando DEV carece de ruleset.

## Ciclo y decisiones
- Modos de las Tasks: RETIRED, PREPARING, READ_ONLY, ACTIVE_DEV, PAUSED, BLOCKED, DISABLED. La habilitación real viene de la plataforma; un valor documental no activa Tasks.
- Capacidades lógicas: AUTHORITY_GUARD, PLAN_SELECTOR, DEV_OPERATOR, REVIEW_GATE, AUDIT_REPORTER. No generan roles GitHub ficticios. SIN BLOQUEO EN SHEETS: claim idempotente con lectura posterior y protección contra editores simultáneos; No se importa un reservador de Sheets.
- Plan: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES; Sheet TAREAS_PROGRAMADAS y AUTOMATIZACIONES son referencias de registro, no habilitación por sí solas. La cola se lee de PLAN_MAESTRO / COLA. Estados PENDIENTE, EN_PROGRESO, VALIDANDO, LISTO, BLOQUEADO, CANCELADO.
- Transiciones operativas con readback: PENDIENTE→EN_PROGRESO en el claim y antes de desarrollar; EN_PROGRESO→VALIDANDO cuando el código candidato esté completo, **antes de A1/A2/A3** y manteniendo mismo TASK_ID responsable; auditar A1 funcional, A2 seguridad y A3 integración dentro de VALIDANDO; publicar commit directo a dev; A4 posintegración en VALIDANDO; pasar VALIDANDO→LISTO sólo con las cuatro auditorías PASS y lectura confirmada. Todo en la misma corrida cuando sea posible. Reconciliación segura si hay conflicto. El trabajo debe satisfacer dependencias existentes y satisfechas. Los accesos GitHub y Google Sheets deben confirmar permisos efectivos, autorización por empresa y ausencia de bypass PROD.
- La fuente comercial única está identificada por Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. No usar otros planes o snapshots. Un permiso NO_VERIFICADO no se convierte en permitido.
- No activar Tasks, conceder permisos o subir a producción por editar este archivo. Una auditoría fallida bloquea únicamente la tarea afectada. Nunca crear PR/branch o reconstruir fases retiradas. No hacer fuerza de escritura sobre dev ni borrar commits ajenos.
- Validar A0 estática antes del commit, A1 funcional, A2 seguridad, A3 integración y A4 posintegración para LISTO; A5 debe registrarse como PENDIENTE y obtener PASS de otro TASK_ID antes de QA/PROD. Auditar funcionalidad, seguridad, integración y posintegración sobre el SHA exacto; crear registro verificable. Meta mínima 1 cierre/turno y objetivo 3, no garantía ni permiso de declarar PASS ficticio.

## Registros vigentes
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

## Protección técnica y rollback
- Arquitectura Angular 22 / ASP.NET 10 / MySQL EF Core 10; auth JWT, RBAC, BCrypt, aislamiento A/B, auditoría, CORS y seguridad de datos sin rebajas. Tests de CI pueden continuar sin convertirse en barreras obligatorias de escritura en dev.
- Cambios correctivos con nuevo commit fast-forward sobre dev y verificación del SHA; no force-push. Ante conflicto de concurrencia fallido, relectura antes de reintentar. QA/main/PROD requieren aprobación nueva expresa cuando se actúe allí.
