# SOLQARYN — Autoridad actual de automatizaciones (V13 Direct DEV)

**Único contrato de ejecución:** `docs/AUTOMATION_V13.md`. Sin gates PR de GitHub en dev; cuatro auditorías reales por trabajo y commit directo a dev. Nunca confundir aprobación de repositorio con autorización sobre permisos reales ni cambiar QA/main/PROD.

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

## Propiedad e identidad
- Cuenta titular JAVIER_CHATGPT: seis Tasks :00, :10, :20, :30, :40 y :50. Cuenta ALEX_CHATGPT: dos Tasks :10 y :50. Estas ocho identidades conservan su ID físico, horarios y titularidad; sólo el propietario de cada cuenta cambia sus Tasks. Javier tiene seis habilitadas en la última lectura. No afirmar estado vivo de Alex sin verificar su propia cuenta.
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
- Auditar funcionalidad, seguridad, integración y posintegración sobre el SHA exacto; crear registro verificable. Meta mínima 1 cierre/turno y objetivo 3, no garantía ni permiso de declarar PASS ficticio.

## Registros vigentes
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

## Protección técnica y rollback
- Arquitectura Angular 22 / ASP.NET 10 / MySQL EF Core 10; auth JWT, RBAC, BCrypt, aislamiento A/B, auditoría, CORS y seguridad de datos sin rebajas. Tests de CI pueden continuar sin convertirse en barreras obligatorias de escritura en dev.
- Cambios correctivos con nuevo commit fast-forward sobre dev y verificación del SHA; no force-push. Ante conflicto de concurrencia fallido, relectura antes de reintentar. QA/main/PROD requieren aprobación nueva expresa cuando se actúe allí.
