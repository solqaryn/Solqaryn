# TASKS — SOLQARYN

### Actualización P0 #3 — PR #3599 (13 octubre 2026; candidato, NO fusionado)

Se añadieron unicidades comerciales por EmpresaId e índices compuestos de cinco raíces. Variantes, imágenes, detalles de compra y venta y facturas reciben EmpresaId nullable derivado/validado, además de doce claves foráneas compuestas en MySQL. Se incorporó migración Oracle EF10 y contratos físicos de bootstrap. Esto es un tramo de la solución, NO su certificación: quedan entidades comerciales descendientes por clasificar/proteger, filtros de lectura y validación MySQL exact-head. No levantar cuarentena de segunda empresa, no tocar QA ni PROD y no reasignar los 19 registros descartables.


> Superficie operativa de **estado actual**. Este archivo no conserva rollups históricos de fases, parents, gates ni planes retirados. El historial anterior permanece auditable en Git.

```text
PROJECT_ID=SOLQARYN
PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
CONTEXT_MODE=CURRENT_STATE_ONLY
```

## Autoridad vigente

- Fuente de ejecución única: **SOLQARYN PLAN COMERCIAL INICIAL UNIFICADO** (Drive `1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`), 492 fichas V3.
- Cola de 1123 microtareas trazables: [Google Sheets operativo](https://docs.google.com/spreadsheets/d/1gcVyCoyhLU0jFMwRtf0s5_x8FSnfBs38ojml1QF7Xwk/edit), pestaña `PLAN_COMERCIAL`; fuentes `_COMERCIAL_SOURCE`, `_RUNTIME_SOURCE`, `_AUTOMATION_SOURCE`, `_EVENTS_SOURCE`.
- Proceso VAEP y seguridad: `docs/VAEP_AUTHORITY.md`, `docs/PROJECT_SCOPE_LOCK.md` y `AGENTS.md`; HEAD vivo de `dev`.
- Estado de admisión verificado: **NO_GO**, 0/10 ejecutores habilitados. Cinco primarias preparadas y pausadas; cinco supervisoras de cuenta externa pendiente de actualización con permisos propios. Ninguna primaria actúa sin verificación independiente de su pareja.
- Las 1123 tareas están cargadas como PENDIENTE; `LISTO` exige evidencia exact-head, tests positivos y negativos, relectura de dependencias, CI, REVIEW_FIRST, tenancy, P0/P1=0 y readback. No asumir funciones ya certificadas como pendientes repetibles.
- El Plan Maestro de horizonte futuro y las notas/colas/objetivos anteriores NO son ejecutables en esta etapa; los respaldos y pruebas históricas permanecen exclusivamente en archivo controlado.
- No ejecutar en QA, `main` o PROD ni modificar datos/secretos/dominios productivos sin autorización nueva y expresa. No alterar funcionamiento comercial sólo por migrar el control-plane.

## Regla de mantenimiento

No volver a agregar aquí snapshots de planes retirados, filas históricas, gates antiguos, receipts, handoffs o cierres de fases. Cuando sea necesario conservar evidencia, Git/CI/CHANGELOG y los artefactos técnicos son la fuente de auditoría; no convierten esa evidencia en planificación vigente.
