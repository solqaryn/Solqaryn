# SOLQARYN — Índice vigente (sin historiales de ejecución)

## Autoridad operativa
- `.agents/skills/solqaryn-project-governance/SKILL.md`: identidad y límites.
- `docs/PROJECT_SCOPE_LOCK.md`: plataforma, rama, alcance y seguridad.
- `AGENTS.md`: reglas condensadas que debe seguir cualquier agente.
- `docs/AUTOMATION_V13.md`: único flujo ejecutable desde PENDIENTE hasta LISTO, A0 estática + A1–A4 para LISTO dev y A5 independiente posdev previa a promoción, con commits directos.
- `docs/AUTOMATION_STATUS.md`: censo efectivo, cuotas 1/3 y estado nativo conocido.
- `docs/AUTOMATION_AUTHORITY.md`: identidades, permisos, competencia y protección de cuentas.
- `docs/GITHUB_ACCOUNT_ROLES.md`: credenciales y roles sin aprobaciones GitHub rutinarias DEV.
- Ampliación de 6 a 8 Tasks: el handoff para las dos identidades originales de Alex y los títulos/horarios futuros propuestos se encuentra exclusivamente en `docs/AUTOMATION_V13.md` (*Preparación de Alex*). Aún no se editaron las Tasks físicas ni se ampliaron auditorías.

## Desarrollo y arquitectura
- `ARCHITECTURE.md` y `.agents/skills/solqaryn-project-governance/references/architecture.md`: límites frontend/backend/database de sistema vivo, no fuente de microtareas.
- `PROJECT_CONTEXT.md`: síntesis vigente de componentes y operación.
- `CHANGELOG_AI.md`: cambios vigentes a partir de limpieza V13, sin usar el registro como plan ni prueba de aceptación.
- Frontend `frontend/` Angular 22.2.1; backend `backend/` ASP.NET Core 10 y EF Core/MySQL; pruebas `backend/tests/`, frontend, Playwright, workflows GitHub Actions.

## Autoridades externas
- Plan comercial: https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit
- Sheet operativo: https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit
- GitHub DEV: https://github.com/solqaryn/Solqaryn/tree/dev
- Main/QA no se operan por inferencia; no activar Tasks pausadas.

## Exclusión
Esta página no contiene propuestas antiguas, PR descartados, gates históricos ni instrucciones de microfases anteriores. Los commits viejos permanecen sólo en la historia Git para trazabilidad.
