# SOLQARYN — Índice vigente (sin historiales de ejecución)

> **ESTADO OPERATIVO V5 FINAL (readback Javier 2026-11-13).** Seis Tasks Javier originales habilitadas (is_enabled=true, 6/6); pausa del 12-nov HISTORICA REVOCADA por orden posterior. Alex solo desde cuenta propietaria. Fuente ejecutable unica: Google Doc V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, 2.340 IDs C00-01.M01.01 a C15-05.M06.06. V13 mantiene A0–A5 y seguridad, no el backlog antiguo. El commit base `f4b8528f57e82625d343dd1590076df44cacaee2` es antecesor del HEAD `4d582aa48dcdf8e9efb8d4c15ec229cbc0d603a5` (13 commits de avance); la autoridad de codigo/CI es siempre HEAD vivo y SHA exacto del candidato, no un SHA congelado. Validar con GitHub Actions (dotnet/Angular/Node/Docker/MySQL cuando los jobs pertinentes realmente corren); sin PASS si el job es SKIPPED o el SHA es anterior. Preservar horarios, IDs y recuperacion cruzada; no fingir nuevas microtareas LISTO.


- El contrato vigente permite cierres por corrida sin máximo artificial de tres, sujetándose a tiempo y recursos del proveedor y a A0–A5. Vigilancia de pausas documentada en docs/AUTOMATION_V13.md.
- La recuperación cruzada autorizada de seis Javier y la preferencia máxima de razonamiento están en docs/AUTOMATION_V13.md. El conector de Tasks no cambia el modelo. Alex sigue bajo su titularidad.
- Resolución del falso NO_GO de DEV 01/05 y selección por factibilidad: `docs/AUTOMATION_V13.md`, `docs/AUTOMATION_AUTHORITY.md` y `scripts/validate-dual-task-configuration.mjs`. COLA!E refleja PLAN_MAESTRO!H mediante fórmula restaurada en siete filas históricas; no duplicar estados de otro motor.
## REGISTRO HISTÓRICO — suspensión del 2026-11-12 20:56 (NO VIGENTE, revocada por reanudación V5)

ORDEN POSTERIOR EXPRESA QUE PREVALECE SOBRE V13 ANTERIOR. Las seis Tasks físicas JAVIER_CHATGPT quedan is_enabled=false por cambio pendiente del plan de implementación. El readback del conector confirmó 6/6 pausadas y sus prompts físicos contienen OWNER_PAUSE_HOLD. Ninguna Task puede reclamar una nueva microtarea, iniciar A5 independiente, entrar a PENDIENTE→EN_PROGRESO, publicar cambios nuevos no iniciados ni reactivar a otra Task. La regla de recuperación cruzada entre Tasks queda SUSPENDIDA, aunque el catálogo de ocho IDs autorizados permanezca. No reactivar ninguna Javier hasta nueva instrucción expresa del propietario; no confundir esta pausa autorizada con un error del proveedor.

CIERRE SEGURO: al hacer la pausa, DEV 03/04/05/06 se veían in_progress; DEV 01/02 completed. Desactivar is_enabled impide disparos programados nuevos, pero NO garantiza que un run ya iniciado haya recibido el cambio de prompt ni que se haya cancelado. Si una ejecución recibe la orden, sólo terminará la microtarea reclamada antes de la pausa, con las auditorías pertinentes y readback, y terminará sin reclamar otra. No inventar cierres ni cambiar tareas ya reclamadas arbitrariamente.

ALEX: la cuenta propietaria de las Tasks 07 (:05) y 08 (:35) es distinta y NO está administrable desde esta sesión. El propietario solicitó su suspensión; el estado físico de Alex no está confirmado como PAUSADO. Requiere ejecución desde cuenta Alex. No usar conexiones cruzadas ni declarar 8/8 pausadas.

CONSERVAR: ocho IDs, RRULE, zonas, historial, fuente de plan, seis controles A0–A5, arquitectura y límites QA/main/PROD. Nueva activación sólo con orden posterior del propietario y revalidación del plan y los permisos.


## Autoridad operativa
- `.agents/skills/solqaryn-project-governance/SKILL.md`: identidad y límites.
- `docs/PROJECT_SCOPE_LOCK.md`: plataforma, rama, alcance y seguridad.
- `AGENTS.md`: reglas condensadas que debe seguir cualquier agente.
- `docs/AUTOMATION_V13.md`: único flujo ejecutable desde PENDIENTE hasta LISTO, A0 estática + A1–A4 para LISTO dev y A5 independiente posdev previa a promoción, con commits directos.
- `docs/AUTOMATION_STATUS.md`: censo efectivo, cuotas 1/3 y estado nativo conocido.
- `docs/AUTOMATION_AUTHORITY.md`: identidades, permisos, competencia y protección de cuentas.
- `docs/GITHUB_ACCOUNT_ROLES.md`: credenciales y roles sin aprobaciones GitHub rutinarias DEV.
- Incorporación 6→8 comunicada por Alex como completada: PRIMARY DEV 07 :05 y PRIMARY DEV 08 :35, sin edición física desde Javier. Estado y evidencia de propietario en `docs/AUTOMATION_V13.md`.

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
