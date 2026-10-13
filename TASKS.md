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
- **P0 abierto — segregación de datos comerciales multiempresa:** Aiven DEV ya tiene `EmpresaId?` en las cinco raíces comerciales (5 FKs e índices) y los 19 registros históricos siguen con `EmpresaId=NULL`; la auditoría de procedencia read-only pasó, pero no demuestra atribución histórica. Permanecen pendientes filtros SQL, integridad del grafo, clasificación de 132 entidades EF (66 descendientes comerciales), escrituras con propietario verificado, dominios públicos/cachés/jobs y pruebas A/B exact-head. La cuarentena de 2+ empresas continúa obligatoria; ver `docs/runbooks/TENANCY_DATOS_COMERCIALES_P0.md` y `docs/tenancy/INVENTARIO_GRAFO_COMERCIAL_P0_DEV.md`.

## Regla de mantenimiento

No volver a agregar aquí snapshots de planes retirados, filas históricas, gates antiguos, receipts, handoffs o cierres de fases. Cuando sea necesario conservar evidencia, Git/CI/CHANGELOG y los artefactos técnicos son la fuente de auditoría; no convierten esa evidencia en planificación vigente.
