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
FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=FALSE
FORMAL_APPROVAL_GOVERNANCE_REQUIRED=FALSE
WORK_BRANCH_CREATION_ALLOWED=FALSE
GOAL_MIN_LISTO_PER_RUN=1
GOAL_TARGET_LISTO_PER_RUN=3

## Autoridad exacta
- Leer siempre Skill y su copia exacta en HEAD dev, docs/PROJECT_SCOPE_LOCK.md, docs/AUTOMATION_STATUS.md, docs/AUTOMATION_AUTHORITY.md y **docs/AUTOMATION_V13.md**. La única fuente de alcance es el Plan Comercial Inicial. Ningún histórico de otra versión impone condiciones.
- SIN BLOQUEO EN SHEETS: no usar locks artificiales; sí respetar ID, TASK_ID, edición concurrente y fórmulas. Las seis Tasks Javier están HABILITADAS según readback nativo del 12-nov-2026; Alex bajo su propia cuenta. No crear, habilitar o tocar Tasks sin orden.
- DEV: commits directos fast-forward, serializados, sin PR/branch de trabajo ni espera de GitHub APPPROVED/required checks de un ruleset retirado. GitHub Actions siguen aportando evidencia; resolver defectos de seguridad/funcionalidad reales. No tocar main/qa/prod sin permiso.
- En cada corrida habilitada intentar cerrar 1 microtarea LISTO, meta 3. Estados exactos y visibles: PENDIENTE→EN_PROGRESO en claim; EN_PROGRESO→VALIDANDO tras implementar y ANTES de auditorías A1/A2/A3; el mismo Scope permanece responsable durante esas auditorías, corrige defectos e integra commit directo dev; A4 posintegración en VALIDANDO; finalmente LISTO con cuatro PASS, SHA dev y readback PLAN/COLA/BITACORA. Otros Scopes pueden empezar otras PENDIENTE elegibles desde su disparo, sin esperar a que termine un Scope ajeno. Si se agota tiempo, conservar VALIDANDO/EN_PROGRESO con responsable, evidencia y siguiente acción; jamás fingir LISTO.
- Conservar JWT, BCrypt, RBAC, EmpresaId A/B, MySQL, secretos protegidos, seguridad y pruebas proporcionadas. Revalidar HEAD, permisos y dependencias; nada de force-push, reset destructivo o suplantación de identidades.
- Ejecución exclusiva en SOLQARYN; no utilizar otros proyectos ni referencias externas fuera de la allowlist técnica.
