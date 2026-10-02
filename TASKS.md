# TASKS — SOLQARYN

> Superficie operativa de **estado actual**. Este archivo no conserva rollups históricos de fases, parents, gates ni planes retirados. El historial anterior permanece auditable en Git.

```text
PROJECT_ID=SOLQARYN
PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
CONTEXT_MODE=CURRENT_STATE_ONLY
```

## Autoridad vigente

- Roadmap y arquitectura objetivo: único Google Doc **PLAN MAESTRO SOLQARYN**, ID `1YdQlNJ312HuziyKb9E-GEt55dcgSmuFGgfsHxyzPUaw`.
- Ejecución VAEP: `docs/VAEP_AUTHORITY.md`.
- Identidad/estado técnico: `PROJECT_CONTEXT.md` y HEAD vivo de `dev`.
- Pendientes deliberadamente aplazados: `docs/DETALLES_PENDIENTES.md`.

## Estado operativo

- DEV, QA y PROD están reconciliados sobre la infraestructura corporativa vigente.
- Los rollups históricos numerados y las secuencias de planes anteriores fueron retirados de esta superficie viva.
- Las diez automatizaciones VAEP permanecen pausadas hasta autorización explícita del propietario.
- No existe una cola, parent, gate o fase histórica con autoridad ejecutable desde este archivo.
- El trabajo futuro se selecciona únicamente desde el Plan Maestro vigente + estado vivo + dependencias técnicas reales.

## Regla de mantenimiento

No volver a agregar aquí snapshots de planes retirados, filas históricas, gates antiguos, receipts, handoffs o cierres de fases. Cuando sea necesario conservar evidencia, Git/CI/CHANGELOG y los artefactos técnicos son la fuente de auditoría; no convierten esa evidencia en planificación vigente.
