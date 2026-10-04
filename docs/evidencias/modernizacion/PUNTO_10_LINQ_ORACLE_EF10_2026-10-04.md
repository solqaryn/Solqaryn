# Punto 10 — LINQ de SOLQARYN en Oracle EF Core 10

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**PASS — la lane final Oracle EF10/net10 compila el backend y ejecuta una matriz obligatoria de 34 consultas sobre repositorios reales; el gate falla si alguna consulta deja de traducirse o ejecutarse.**

## Matriz de consultas ejecutada

La lane crea `AppDbContext` con Oracle Connector/NET 10 y corre el código real de los repositorios contra MySQL 8.4 efímero con el esquema canónico:

- `ProductoRepository`: lectura con filtered/split includes; filtros combinados y búsqueda relacional; las nueve ramas de sort (`marca`, `modelo`, `color`, `talla`, `cantidad`, `costo`, `precio`, `fechacreacion`, `nombre`); existencia/stock y agregados de unidades, costos y precios.
- `ClienteRepository`: includes de ventas/tipo de cliente, búsqueda textual, normalización de identidad/correo/teléfono, unicidad por nombre e identidad.
- `ExistenciaVarianteRepository`: navegación producto-variante-almacén-ubicación, predicados de IDs/nulos/stock, paginación, operatividad pública y existencia de clave.
- `CompraRepository` y `VentaRepository`: scope de administrador, filtros de búsqueda, conteo y paginación, includes/split queries, periodos y agregaciones monetarias.
- La lane cuenta los probes y exige `repositoryLinqProbes >= 34`; cualquier error de SQL translation, ejecución, lectura o compilación hace fallar el job.

No se limita a `ToQueryString`: son materializaciones y agregaciones ejecutadas en el provider candidato. La API/backend completo también debe compilarse en la copia EF10 Oracle; los proyectos productivos siguen sin retargetearse.

## Certificación exact-head

- Commit: `87cce67c7846e361448c390810366d9126ffb490`.
- Lane Oracle EF10 dedicada `37237322033`: `success`; salida `ORACLE10_PROVIDER_LANE_RUNTIME=PASS attempts=2 repositoryLinqProbes=34` y auditoría tenant PASS.
- Gate exact-head de Fase 6 `37237322283`: los cinco jobs terminaron `success`, incluido Pomelo con suite integral, Oracle Connector/NET, Oracle EF10/net10, copia net10 y dictamen; `P0=0`, `P1=0`.
- Fase 7 permaneció sin ejecutar. [Lane LINQ Oracle EF10](https://github.com/solqaryn/Solqaryn/actions/runs/37237322033) · [Gate exact-head Fase 6](https://github.com/solqaryn/Solqaryn/actions/runs/37237322283).

## Alcance

La matriz se enfoca en rutas de consulta de repositorios reales con filtros, paginación, navegación y agregaciones. No afirma ejecutar cada endpoint ni toda combinación de datos de negocio; la suite completa de integración y regresión continúa siendo un gate separado. Sin cambios a dependencias productivas ni ejecución de Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
