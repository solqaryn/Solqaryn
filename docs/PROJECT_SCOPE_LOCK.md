# PROJECT_SCOPE_LOCK — SOLQARYN

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
PILOT_TASK_LIMIT=2

## Alcance actual

La fuente comercial aprobada es `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` (ID de Drive citado arriba). Los planes antiguos, manifestaciones, hojas, Tasks y calendarios ya retirados no tienen autoridad ejecutiva, y no se reutilizan automáticamente. El estado de automatización se documenta solamente en `docs/AUTOMATION_STATUS.md`.

## Autoridad vigente del piloto DEV (23-10-2026)

`docs/AUTOMATION_AUTHORITY.md` regula el piloto `ACTIVE_DEV` autorizado para la Task física `PRIMARY_01` de Javier como único escritor automatizado de ramas y PRs hacia `dev`. `SECONDARY_02` de Alex permanece revisor `READ_ONLY`. `PROPIETARIA_INSTITUCIONAL` = SOLQARYN Platform; los ejecutores humanos conservan paridad, sin delegación de credenciales ni de sesión. La fuente única para el contador y modo de ejecución es `docs/AUTOMATION_STATUS.md` (`SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1`); este documento no activa Tasks adicionales, autoriza merges automáticos ni habilita QA, main o PROD. El VAEP retirado y Jules siguen prohibidos.

## Identidad, código y seguridad

Se permite una única Skill local: `solqaryn-project-governance`. Las referencias externas se utilizan solo bajo el registro y allowlist actual del proyecto, con pin y fuente aprobados. No recurrir a otras identidades, proyectos o repositorios como fallback.

El trabajo ordinario se hace en `dev` por PR protegido. Nunca usar force-push, alterar `main` o producción sin autorización específica. Conservar aislamiento empresarial, JWT, RBAC, auditoría, migraciones MySQL, respaldo, pruebas de código actuales y secretos fuera del repositorio.

Las restricciones de seguridad del producto no son hitos de un roadmap antiguo: permanecen mientras la funcionalidad protegida exista. Las validaciones históricas que exigen certificaciones de fases anteriores deben ejecutarse manualmente o sustituirse por regresiones actuales relevantes, no bloquear cada nuevo cambio sin causa.

### Enmienda 1.2: Tasks físicas existentes y límites del piloto

`PRIMARY_01` pertenece a la cuenta de Javier; `SECONDARY_02` a la de Alex. Su instalación es independiente del permiso de escritura: solo una Task está autorizada a producir ramas y PRs de microtareas DEV mediante el piloto `ACTIVE_DEV` y controles nativos GitHub. El límite de dos impide crear duplicados. El detalle de roles está en `docs/AUTOMATION_AUTHORITY.md`; `PROJECT_SCOPE_LOCK=STRICT` conserva la propiedad institucional y el aislamiento de datos. El antiguo Maestro (incluidos identificadores `SQ-000` a `SQ-353` y el Word histórico) es referencia futura/no ejecutable, no fuente de microtareas, despachos, dependencias ni autorizaciones.

## Control de cambios

Los cambios deben analizar consumidores directos, rollback, tests y documentación. No crear nuevas Tasks o automatizaciones sin autorización expresa. El histórico queda en Git; no se reescribe ni borra la genealogía de commits.
