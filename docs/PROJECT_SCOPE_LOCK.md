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
WORK_BRANCH_CREATION_ALLOWED=FALSE

## Límite de proyecto y seguridad
- Este Scope Lock identifica exclusivamente al proyecto SOLQARYN, no a un ruleset GitHub. La rama dev carece del antiguo ruleset de PR/CI; main y qa conservan protección independiente.
- Escritura directa y serializada a dev, HEAD verificado, sin force-push, sin crear PR o ramas auxiliares. SIN BLOQUEO EN SHEETS: no aplicar reservas artificiales, pero sí impedir que dos Tasks sobrescriban el mismo ID.
- Plan Comercial Inicial único; límites de arquitectura Angular 22.2.1, .NET 10, MySQL EF Core/Oracle, JWT, RBAC, BCrypt y aislamiento A/B. Cuatro auditorías con evidencia antes de LISTO, prueba de SHA publicado y estado PLAN/COLA/BITACORA. Las cuatro auditorías no excusan un fallo real.
- Tasks físicas conservadas y pausadas según titular; sin crear nuevas ni extender poderes. No consultar otras cuentas/proyectos por fallback. Nada de acceso a QA/main/PROD, secrets o datos productivos sin autorización específica.
- No reactivar modelos retirados ni planes previos. El contrato operativo único es docs/AUTOMATION_V13.md; el estado actual y censo, docs/AUTOMATION_STATUS.md; separación de cuentas, docs/GITHUB_ACCOUNT_ROLES.md. Preparación 6→8 de las dos Tasks Alex previamente autorizadas es documental y no confiere acceso a su cuenta ni autoriza activación o auditorías nuevas.
