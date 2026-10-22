# SOLQARYN — Reglas actuales de trabajo

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXECUTION_PLAN_DRIVE_FILE_ID=1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=RETIRED

## Autoridades vigentes

1. `docs/PROJECT_SCOPE_LOCK.md` regula la identidad de SOLQARYN y el acceso a sus fuentes.
2. `docs/AUTOMATION_STATUS.md` declara si existe un motor programado autorizado.
3. El documento `SOLQARYN PLAN COMERCIAL INICIAL UNIFICADO` es la fuente actual del alcance comercial. Ninguna hoja, manifiesto, bitácora o plan histórico es ejecutable por inferencia.
4. La única Skill local permitida sigue siendo `solqaryn-project-governance`; las referencias externas son opcionales y se consultan únicamente para una necesidad técnica real.

## Estado de automatización (22-10-2026)

El propietario informó que eliminó las diez Tasks de ChatGPT y la hoja operativa antigua. **No hay tareas de ChatGPT activas autorizadas, workers Jules, coordinadores ni cola VAEP en ejecución por este contrato.** No crear o reconstruir Tasks, activar cron, restaurar Sheets, reclamar leases, realizar despachos comerciales ni interpretar evidencias históricas como instrucciones vigentes. Todo motor nuevo necesita diseño y autorización explícitos; esta regla no impide el trabajo manual solicitado.

## Trabajo de desarrollo

- Trabajar en `solqaryn/Solqaryn`, rama `dev`, por PR y fusión protegida. Consultar el HEAD actual y no sobrescribir trabajo concurrente.
- Cambios de código tienen pruebas proporcionales, revisión causal, diffs auditables y rollback. Un plan histórico no puede imponer hitos ni gates no relacionados.
- Conservar JWT, BCrypt, RBAC, aislamiento A/B por empresa, auditoría, seguridad multiempresa, integridad de MySQL, respaldos, controles de secretos y prevención de fugas; no rebajarlos para ahorrar tiempo.
- Frontend Angular 22, backend .NET 10, MySQL EF Core, Cloudinary, SMTP y QuestPDF permanecen bajo la arquitectura vigente.
- Las pruebas de producto de CI son distintas de las automatizaciones que coordinaban Tasks. No usar un job llamado `automatizacion` como criterio único para borrarlo: verificar sus consumidores.
- No promover a QA/main/PROD, alterar datos productivos, secretos, dominios ni pagar servicios sin autorización nueva y explícita.
- `docs/VAEP_AUTHORITY.md` es un archivo de compatibilidad histórica sin mandato operacional; no aplicar sus antiguas reglas.

## Cierre

Mantener `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, `ARCHITECTURE.md`, `ARCHITECTURE_CHANGELOG.md` y `CHANGELOG_AI.md` cuando el cambio lo amerite. Documentar cualquier acción humana pendiente con enlace e instrucciones concretas.
