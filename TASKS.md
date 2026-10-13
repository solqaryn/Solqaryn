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
- **P0 multiempresa abierto:** #3596 y #3597 integrados en dev. Los 19 registros comerciales sin EmpresaId son datos de prueba DEV descartables conforme al propietario (2026-10-13). Su atribución histórica/backfill ya NO es requisito. Limpieza de datos comerciales aún sin ejecutar: preservar usuarios, credenciales, roles, permisos, membresías y configuración imprescindible; exigir inventario de dependencias, backup cifrado y restore ensayado. El verdadero P0 pendiente es aislar por EmpresaId todas las operaciones futuras (lecturas SQL, escrituras, FKs, inventarios, documentos, finanzas, medios, tienda pública, jobs y caché), con pruebas A/B MySQL reales. Cuarentena y bloqueo de segunda empresa continúan. Ver docs/tenancy/LIMPIEZA_DATOS_PRUEBA_DEV.md.

## Regla de mantenimiento

No volver a agregar aquí snapshots de planes retirados, filas históricas, gates antiguos, receipts, handoffs o cierres de fases. Cuando sea necesario conservar evidencia, Git/CI/CHANGELOG y los artefactos técnicos son la fuente de auditoría; no convierten esa evidencia en planificación vigente.
