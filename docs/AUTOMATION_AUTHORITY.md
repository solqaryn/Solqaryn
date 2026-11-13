# SOLQARYN — Autoridad actual de automatizaciones (V13 Direct DEV)

> **ESTADO OPERATIVO V5 FINAL (readback Javier 2026-11-13).** Seis Tasks Javier originales habilitadas (is_enabled=true, 6/6); pausa del 12-nov HISTORICA REVOCADA por orden posterior. Alex solo desde cuenta propietaria. Fuente ejecutable unica: Google Doc V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, 2.340 IDs C00-01.M01.01 a C15-05.M06.06. V13 mantiene A0–A5 y seguridad, no el backlog antiguo. El commit base `f4b8528f57e82625d343dd1590076df44cacaee2` es antecesor del HEAD `4d582aa48dcdf8e9efb8d4c15ec229cbc0d603a5` (13 commits de avance); la autoridad de codigo/CI es siempre HEAD vivo y SHA exacto del candidato, no un SHA congelado. Validar con GitHub Actions (dotnet/Angular/Node/Docker/MySQL cuando los jobs pertinentes realmente corren); sin PASS si el job es SKIPPED o el SHA es anterior. Preservar horarios, IDs y recuperacion cruzada; no fingir nuevas microtareas LISTO.


**Único contrato de ejecución:** `docs/AUTOMATION_V13.md`. Sin gates PR de GitHub en dev; A0–A4 obligatorias para cerrar LISTO y A5 independiente obligatoria antes de QA/PROD y commit directo a dev. Nunca confundir aprobación de repositorio con autorización sobre permisos reales ni cambiar QA/main/PROD.

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

- Continuidad: primer LISTO objetivo mínimo, 3 hito no máximo; completar todas las microtareas reales factibles en la misma corrida, sin perder A0–A5. Una suspensión del proveedor requiere aviso con run_id y causa si existe; no se soluciona por mero prompt ni se auto reactiva sin resolver permisos.
- El propietario autorizó que cualquiera de las seis Tasks Javier habilitadas intente reanudar otra Task Javier pausada usando solo su TASK_ID y un update is_enabled=true de la misma cuenta, con lectura confirmatoria, un intento por corrida y registro del run_id. Nunca modificar Alex ni concederse permisos. La elección de razonamiento máximo es del selector nativo del propietario; el conector de Tasks no dispone de parámetro para imponerla.
- Contrato de identidad y selección actual: no usar `is_self_owned_automation=false` ni `Automations.get 404` como único motivo de NO_GO; son diagnósticos del agente/herramienta consultante, no revocaciones. Probar Task ID del prompt y allowlist, propietario, `peek` + enabled/horario, conectores GitHub/Drive del mismo titular y Skill/Scope Lock exact-HEAD antes de atribuir operación. Si hay denegación real, bloquear sólo la operación sin prueba, nunca suplantar. Leer todas las PENDIENTE elegibles, seleccionar una cuya A0–A4 pueda acreditarse, y continuar otras si falta .NET/Angular/proveedor para la primera. Los siete estados manuales divergentes en COLA se reconciliaron a fórmula `PLAN_MAESTRO!H`, con notas de auditoría.
## REGISTRO HISTÓRICO — suspensión del 2026-11-12 20:56 (NO VIGENTE, revocada por reanudación V5)

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
