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

- Certificación exact-head vigente: gate Fase 6 `37286379704`, HEAD `f5e0c4e3c409e8081850857461554c6c0e705955`; los cuatro jobs de provider y el dictamen terminaron `success`, con `P0=0`, `P1=0` y `PHASE7_EXECUTED=false`. [Gate exact-head Fase 6](https://github.com/solqaryn/Solqaryn/actions/runs/37286379704).
- En la lane Oracle EF10/net10 de ese mismo run: backend compilado, 2336/2336 unitarias, 22/22 integraciones y salida `ORACLE10_PROVIDER_LANE_RUNTIME=PASS attempts=2 repositoryLinqProbes=34`. El job valida una consulta ejecutada/materializada por cada probe y termina en error si la cobertura no llega a 34.
- El run confirma auditoría tenant en la lane y no modifica los `TargetFramework` productivos; el retarget net10 sigue siendo una copia temporal aislada.

## Alcance

La matriz se enfoca en rutas de consulta de repositorios reales con filtros, paginación, navegación y agregaciones. No afirma ejecutar cada endpoint ni toda combinación de datos de negocio; la suite completa de integración y regresión continúa siendo un gate separado. Sin cambios a dependencias productivas ni ejecución de Fase 7.

**Punto 10: CERRADO.** Las 34 consultas obligatorias de repositorios reales se ejecutaron sobre Oracle EF10 y pasaron en el HEAD certificado.

MAPA_ARQUITECTURA: SIN_CAMBIO.


## Revalidación exact-head posterior — 2026-10-06

- HEAD de `dev`: `2ebdb196323d6d15c69ef3d7a3a3127edd448737`. Fase 6 post-merge: [run 37425557471](https://github.com/solqaryn/Solqaryn/actions/runs/37425557471), push sobre ese SHA, conclusión `success`; cinco lanes y dictamen finalizaron correctamente.
- En la lane Oracle EF10/net10, API/backend y pruebas se compilaron como `net10.0`; la suite terminó con 22/22 pruebas de integración.
- La matriz de repositorios reales terminó con `ORACLE10_PROVIDER_LANE_RUNTIME=PASS attempts=2 repositoryLinqProbes=34`. El conteo superó el mínimo de 34 y cualquier excepción de traducción/ejecución falla la lane.
- La matriz y su runner se ejecutan desde copia efímera; los `TargetFramework` productivos no se retargetearon. No hubo cambio de provider productivo ni ejecución de Fase 7.

**Revalidación del punto 10: PASS en dev, HEAD `2ebdb19...`.**
