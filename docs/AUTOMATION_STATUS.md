# SOLQARYN — Estado operativo único de automatizaciones

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=ACTIVE_DEV
PHYSICAL_JAVIER_TASKS_ENABLED=6
ALEX_TASKS_STATE=REQUIRES_OWNER_READBACK
SCHEDULED_CHATGPT_TASKS_AUTHORIZED=8
AUTHORIZED_COMMERCIAL_TASK_IDS=6adbb9fff1548191a1bd5b30d9e9984a,6af16bb9492c8191b8e8c7d606f11507,6adc2e4bd8a48191ac7d8641ed95a1a3,6aeacb7820ac8191b7d768a6f33dfa09,6adc2e55f47c81919f6374e561687076,6af16bbad26881918c283abe06e1f32a,6adbbc9ad9e88191bea8eb69104afb25,6adcc98a80a48191b71a5156624c21df
REGISTERED_TASK_IDS=6adbb9fff1548191a1bd5b30d9e9984a,6af16bb9492c8191b8e8c7d606f11507,6adc2e4bd8a48191ac7d8641ed95a1a3,6aeacb7820ac8191b7d768a6f33dfa09,6adc2e55f47c81919f6374e561687076,6af16bbad26881918c283abe06e1f32a,6adbbc9ad9e88191bea8eb69104afb25,6adcc98a80a48191b71a5156624c21df
PHYSICAL_TASKS_REGISTERED=8
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
FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=FALSE
FORMAL_APPROVAL_GOVERNANCE_REQUIRED=FALSE
GOAL_MIN_LISTO_PER_RUN=1
GOAL_TARGET_LISTO_PER_RUN=3
EXECUTION_STYLE=SERIAL_DIRECT_DEV_FAST_FORWARD
STAGES=PENDIENTE,EN_PROGRESO,VALIDANDO,LISTO,BLOQUEADO,CANCELADO
TIMEZONE=America/Tegucigalpa

## Interpretación efectiva
- `AUTOMATION_RUNTIME=ACTIVE_DEV` indica que el contrato contempla DEV; NO afirma que las Tasks se encuentren físicamente habilitadas. En el readback de 2026-11-12 las seis Tasks Javier están habilitadas (`PHYSICAL_JAVIER_TASKS_ENABLED=6`). Para Alex no se presupone su estado. No reactivar por cambiar documentación.
- Las constantes `V13_1000_FIFO_BATCH` y `MAX_OPEN_WORK_BRANCHES=1000` se retienen para compatibilidad con validadores instalados; NO habilitan FIFO de PR, trabajo en branches ni PR nuevos. La regla ejecutiva efectiva es `WORK_BRANCH_CREATION_ALLOWED=FALSE` y publicación serial a `dev`.
- `FORMAL_APPROVAL_GOVERNANCE_REQUIRED=FALSE` se limita al workflow directo de dev. Cualquier protección que subsista en `qa` o `main` mantiene sus obligaciones nativas.
- Las seis Tasks Javier ejecutan V13 directa en dev con el orden: al seleccionar PENDIENTE, pasar inmediatamente a EN_PROGRESO con TASK_ID; tras acabar implementación candidata, pasar a VALIDANDO **antes** de A1 funcional, A2 seguridad y A3 integración; mismo Scope audita, corrige, publica commit fast-forward dev, hace A4 posintegración y sólo entonces marca LISTO con evidencia. Otros Scopes toman IDs PENDIENTE libres en su disparo, sin esperar cierres ajenos. Meta mínima 1 LISTO, objetivo 3 por corrida; si se corta una tarea en VALIDANDO, mantener responsable/huella para continuación, no fingir LISTO. Las dos Tasks Alex siguen bajo su cuenta propietaria.
- La selección y semántica de etapas se definen exclusivamente en `docs/AUTOMATION_V13.md`. No existe un contrato activo de PR FIFO/revisión formal para DEV.
- **SIN BLOQUEO EN SHEETS:** evitar reservas artificiales; preservar validación de autoría, concurrencia, ID y fórmulas de PLAN/COLA. BITACORA: 21 campos, EVENT_ID único, append contiguo.

## Tasks registradas
| Minuto | Cuenta | Función | TASK_ID |
|---|---|---|---|
| :00 | JAVIER_CHATGPT | PRIMARY DEV 01 | `6adbb9fff1548191a1bd5b30d9e9984a` |
| :10 | JAVIER_CHATGPT | PRIMARY DEV 05 | `6af16bb9492c8191b8e8c7d606f11507` |
| :20 | JAVIER_CHATGPT | PRIMARY DEV 02 | `6adc2e4bd8a48191ac7d8641ed95a1a3` |
| :30 | JAVIER_CHATGPT | PRIMARY DEV 04 | `6aeacb7820ac8191b7d768a6f33dfa09` |
| :40 | JAVIER_CHATGPT | PRIMARY DEV 03 | `6adc2e55f47c81919f6374e561687076` |
| :50 | JAVIER_CHATGPT | PRIMARY DEV 06 | `6af16bbad26881918c283abe06e1f32a` |
| :10 | ALEX_CHATGPT | SECONDARY REVIEW 01 | `6adbbc9ad9e88191bea8eb69104afb25` |
| :50 | ALEX_CHATGPT | SECONDARY REVIEW 03 | `6adcc98a80a48191b71a5156624c21df` |

## Fuentes autorizadas
- Plan Drive: https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit
- Sheet operativo: https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit
- `docs/AUTOMATION_AUTHORITY.md` y `AGENTS.md`: interpretan este runtime; ninguna evidencia previa altera esta configuración.
