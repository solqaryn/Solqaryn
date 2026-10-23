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
PILOT_TASK_LIMIT=2

## Alcance actual

La fuente comercial aprobada es `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` (ID de Drive citado arriba). Los planes antiguos, manifestaciones, hojas, Tasks y calendarios ya retirados no tienen autoridad ejecutiva, y no se reutilizan automáticamente. El estado de automatización se documenta solamente en `docs/AUTOMATION_STATUS.md`.

## Preparación de nueva autoridad de automatizaciones

`docs/AUTOMATION_AUTHORITY.md` regula únicamente el contrato nuevo en preparación: `PROPIETARIA_INSTITUCIONAL` = SOLQARYN Platform; `EJECUTOR_PRINCIPAL_01` = Javier y `EJECUTOR_PRINCIPAL_02` = Alex con derechos humanos paritarios, sin jerarquía ni herencia de sesiones o permisos de Tasks. El estado de ejecución y contador autorizados siguen únicamente en `docs/AUTOMATION_STATUS.md`; al introducir la referencia no se activa ninguna Task.

## Identidad, código y seguridad

Se permite una única Skill local: `solqaryn-project-governance`. Las referencias externas se utilizan solo bajo el registro y allowlist actual del proyecto, con pin y fuente aprobados. No recurrir a otras identidades, proyectos o repositorios como fallback.

El trabajo ordinario se hace en `dev` por PR protegido. Nunca usar force-push, alterar `main` o producción sin autorización específica. Conservar aislamiento empresarial, JWT, RBAC, auditoría, migraciones MySQL, respaldo, pruebas de código actuales y secretos fuera del repositorio.

Las restricciones de seguridad del producto no son hitos de un roadmap antiguo: permanecen mientras la funcionalidad protegida exista. Las validaciones históricas que exigen certificaciones de fases anteriores deben ejecutarse manualmente o sustituirse por regresiones actuales relevantes, no bloquear cada nuevo cambio sin causa.

### Enmienda 1.2: dos Tasks futuras, sin jerarquia humana

`PRIMARY_01` pertenece a la cuenta de Javier; `SECONDARY_02` a la cuenta
de Alex. El limite maximo de dos Tasks no implica que existan ni permite
escrituras o ejecucion en `RETIRED`. El detalle unico de roles y permisos
reside en `docs/AUTOMATION_AUTHORITY.md`; este Scope Lock mantiene
`PROJECT_SCOPE_LOCK=STRICT` y protege la propiedad institucional.

## Control de cambios

Los cambios deben analizar consumidores directos, rollback, tests y documentación. No crear nuevas Tasks o automatizaciones sin autorización expresa. El histórico queda en Git; no se reescribe ni borra la genealogía de commits.
