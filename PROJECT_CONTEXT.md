PROJECT_SCOPE_LOCK=STRICT
# SOLQARYN — Contexto vigente del proyecto

**Estado operativo único desde 2026-11-12.** Este documento describe el sistema actual, no una cronología de candidatos de PR. Las versiones anteriores permanecen únicamente en la historia Git del repositorio, no como autoridad de ejecución.

## Identidad, arquitectura y fuentes
- Repositorio: solqaryn/Solqaryn. Rama DEV, rama QA=qa, rama producción existente=main. No crear ramas adicionales ni modificar QA/PROD por inferencia.
- Frontend: Angular 22.2.1 standalone, Material/CDK 22.2.1, Zone.js 0.16.3. Backend: ASP.NET Core 10, capas Domain→Application→Infrastructure, API como borde. Datos: MySQL, EF Core 10.0.12, proveedor MySql.EntityFrameworkCore 10.0.9, migraciones auditables.
- Seguridad: JWT, BCrypt, RBAC relacional, empresa/tenant A/B aislados, auditoría, CORS explícito, rate limiting, cabeceras; secretos fuera de Git. Integraciones: Cloudinary, QuestPDF, SMTP. E2E: Playwright/Chromium.
- Contrato de código: ARCHITECTURE.md y .agents/skills/solqaryn-project-governance/references/architecture.md; el código actual de dev prevalece sobre narraciones históricas.
- Plan único: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive ID 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. Tablero: STANDARD DE AUTOMATIZACIONES EN SOLQARYN, Sheet ID 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU.

- Operación continua durante cada corrida: tras los primeros tres cierres legítimos, continuar cuarto y siguientes hasta límite real de recursos, elegibilidad o permisos; no autopausar ni afirmar disponibilidad garantizada ante controles del proveedor. A0–A5 conservadas.
- Salud del runtime: seis Tasks Javier actualmente habilitadas; autorización de vigilancia cruzada y reanudación de compañeras con un intento de API nativa y readback, sin eludir aprobaciones. Alex continúa en su cuenta. Selección máxima de razonamiento solicitada, pero no configurable ni verificable con el conector presente.
- Incidencia corregida del contrato V13 2026-11-12: falsos NO_GO de identidad DEV 01/05 por `self_owned=false` y `get=404`; exige corroboración positiva de TASK_ID, dueño y accesos sin rebajar permisos. Selección recorre todas las tareas elegibles y deriva requerimientos reales de A0–A3 por ID, saltando sin reclamar los candidatos inviables y siguiendo otros factibles. COLA!E 108/110/118/120/127/128/249 ya restauradas a fórmula desde PLAN_MAESTRO!H; notas de reconciliación preservan historial.
## SUSPENSION EJECUTIVA DEL PROPIETARIO — 2026-11-12 20:56 America/Tegucigalpa (PRIORIDAD MAXIMA)

ORDEN POSTERIOR EXPRESA QUE PREVALECE SOBRE V13 ANTERIOR. Las seis Tasks físicas JAVIER_CHATGPT quedan is_enabled=false por cambio pendiente del plan de implementación. El readback del conector confirmó 6/6 pausadas y sus prompts físicos contienen OWNER_PAUSE_HOLD. Ninguna Task puede reclamar una nueva microtarea, iniciar A5 independiente, entrar a PENDIENTE→EN_PROGRESO, publicar cambios nuevos no iniciados ni reactivar a otra Task. La regla de recuperación cruzada entre Tasks queda SUSPENDIDA, aunque el catálogo de ocho IDs autorizados permanezca. No reactivar ninguna Javier hasta nueva instrucción expresa del propietario; no confundir esta pausa autorizada con un error del proveedor.

CIERRE SEGURO: al hacer la pausa, DEV 03/04/05/06 se veían in_progress; DEV 01/02 completed. Desactivar is_enabled impide disparos programados nuevos, pero NO garantiza que un run ya iniciado haya recibido el cambio de prompt ni que se haya cancelado. Si una ejecución recibe la orden, sólo terminará la microtarea reclamada antes de la pausa, con las auditorías pertinentes y readback, y terminará sin reclamar otra. No inventar cierres ni cambiar tareas ya reclamadas arbitrariamente.

ALEX: la cuenta propietaria de las Tasks 07 (:05) y 08 (:35) es distinta y NO está administrable desde esta sesión. El propietario solicitó su suspensión; el estado físico de Alex no está confirmado como PAUSADO. Requiere ejecución desde cuenta Alex. No usar conexiones cruzadas ni declarar 8/8 pausadas.

CONSERVAR: ocho IDs, RRULE, zonas, historial, fuente de plan, seis controles A0–A5, arquitectura y límites QA/main/PROD. Nueva activación sólo con orden posterior del propietario y revalidación del plan y los permisos.


## Trabajo de automatizaciones
- Contrato V13: docs/AUTOMATION_V13.md; inventario físico y permisos: docs/AUTOMATION_STATUS.md, docs/AUTOMATION_AUTHORITY.md. Seis Tasks Javier habilitadas según readback nativo; dos Alex sólo gestionables por su titular.
- Cero rulesets obligatorios en dev al último readback; main y qa protegidas. Scopes paralelos por IDs diferentes, commits directos serializados sin PR/branch nueva. Flujo por Scope: PENDIENTE→EN_PROGRESO al claim; EN_PROGRESO→VALIDANDO al completar candidato y ANTES de A1/A2/A3; corregir y publicar commit en dev; A4 posintegración y VALIDANDO→LISTO con evidencia PLAN/COLA/BITACORA. Mínimo objetivo 1 LISTO y meta 3 por corrida habilitada.
- Conflictos y errores: corregir en ejecución cuando sea verificable, evitar pérdida de trabajo ajeno, no marcar PASS falso. No recuperar PR cerrados, antiguos pipelines de aprobación ni permisos por inferencia.
- Calidad extendida: A0 estática + A1–A4 para LISTO_DEV y A5 independiente sobre SHA publicado para habilitar promoción a QA/PROD. No cambiar entorno QA/PROD ni reducir seguridad; sin espera A5 como gate de desarrollo. Títulos Javier secuenciales por minuto sin cambiar TASK_ID y RRULE.
- Incorporación 6→8 reportada completada por Alex, con readback nativo desde SU cuenta: PRIMARY DEV 07 :05 y PRIMARY DEV 08 :35, IDs originales y V13 A0–A5. Evidencia recibida del titular, no leída independientemente desde Javier. Las seis Tasks Javier estaban habilitadas en la última lectura propia.

## Límite documental
- Este contexto no prescribe gates ni tareas adicionales. La información de roadmap, candidatos no integrados y estados históricos no se utiliza para decidir el trabajo actual. Las decisiones de implementación deben comprobarse contra el código presente, el plan único y evidencia real.
