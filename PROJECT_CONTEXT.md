PROJECT_SCOPE_LOCK=STRICT
# SOLQARYN — Contexto vigente del proyecto

**Estado operativo único desde 2026-11-12.** Este documento describe el sistema actual, no una cronología de candidatos de PR. Las versiones anteriores permanecen únicamente en la historia Git del repositorio, no como autoridad de ejecución.

## Identidad, arquitectura y fuentes
- Repositorio: solqaryn/Solqaryn. Rama DEV, rama QA=qa, rama producción existente=main. No crear ramas adicionales ni modificar QA/PROD por inferencia.
- Frontend: Angular 22.2.1 standalone, Material/CDK 22.2.1, Zone.js 0.16.3. Backend: ASP.NET Core 10, capas Domain→Application→Infrastructure, API como borde. Datos: MySQL, EF Core 10.0.12, proveedor MySql.EntityFrameworkCore 10.0.9, migraciones auditables.
- Seguridad: JWT, BCrypt, RBAC relacional, empresa/tenant A/B aislados, auditoría, CORS explícito, rate limiting, cabeceras; secretos fuera de Git. Integraciones: Cloudinary, QuestPDF, SMTP. E2E: Playwright/Chromium.
- Contrato de código: ARCHITECTURE.md y .agents/skills/solqaryn-project-governance/references/architecture.md; el código actual de dev prevalece sobre narraciones históricas.
- Plan único: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive ID 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. Tablero: STANDARD DE AUTOMATIZACIONES EN SOLQARYN, Sheet ID 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU.

## Trabajo de automatizaciones
- Contrato V13: docs/AUTOMATION_V13.md; inventario físico y permisos: docs/AUTOMATION_STATUS.md, docs/AUTOMATION_AUTHORITY.md. Seis Tasks Javier habilitadas según readback nativo; dos Alex sólo gestionables por su titular.
- Cero rulesets obligatorios en dev al último readback; main y qa protegidas. Scopes paralelos por IDs diferentes, commits directos serializados sin PR/branch nueva. Flujo por Scope: PENDIENTE→EN_PROGRESO al claim; EN_PROGRESO→VALIDANDO al completar candidato y ANTES de A1/A2/A3; corregir y publicar commit en dev; A4 posintegración y VALIDANDO→LISTO con evidencia PLAN/COLA/BITACORA. Mínimo objetivo 1 LISTO y meta 3 por corrida habilitada.
- Conflictos y errores: corregir en ejecución cuando sea verificable, evitar pérdida de trabajo ajeno, no marcar PASS falso. No recuperar PR cerrados, antiguos pipelines de aprobación ni permisos por inferencia.
- Preparación documental 6→8: dos Alex ya registrados, IDs físicos y RRULE :10/:50 conservados. Alias propuestos PRIMARY DEV 07 :05 y 08 :35 aún NO aplicados, V13 físico y activación pendientes exclusivamente desde Alex. Auditorías A1–A4 sin cambios; ver docs/AUTOMATION_V13.md.

## Límite documental
- Este contexto no prescribe gates ni tareas adicionales. La información de roadmap, candidatos no integrados y estados históricos no se utiliza para decidir el trabajo actual. Las decisiones de implementación deben comprobarse contra el código presente, el plan único y evidencia real.
