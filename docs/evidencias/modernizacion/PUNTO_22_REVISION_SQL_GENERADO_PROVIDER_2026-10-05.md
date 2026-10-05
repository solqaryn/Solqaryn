# Punto 22 — Revisión del SQL generado

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
HEAD de las pruebas de provider y regresión: `46e52ea9887db783183c2bfe529819b487547685`

## Evidencia revisada

- Gate exact-head de Fase 6, run `37261240261`: Pomelo genera el script forward de migraciones con `dotnet ef migrations script`; verifica script no vacío, ejecuta la historia vigente en MySQL 8.4.11 y certifica tipos, índices, FK, CHECK, defaults y collations del esquema resultante.
- La lane Oracle EF10 genera el SQL de `OracleBaseline` temporal con EF Core 10.0.12 y Connector/NET 10.0.9, lo aplica en base MySQL vacía restaurada desde el baseline canónico y comprueba su resultado contra el esquema de referencia.
- El comparador del workflow `modernization-oracle-baseline-probe.yml` hace diffs por catálogo de tablas/engine/opciones/collation, columnas/orden/tipo/nullability/default/generated/collation, índices/columnas/orden/expresión, constraints, FK y reglas, CHECK, vistas/definiciones, triggers/cuerpo, rutinas y eventos. Un delta falla el gate. El resultado fue `ORACLE_EF10_SCRIPT_BOOTSTRAP_SCHEMA_EQUIVALENT=true`; el artefacto `oracle-baseline-candidate-37232938573` conserva scripts, fuentes y archivos de diff.
- La referencia de bootstrap `37232938573` corresponde a HEAD `0e4b552ed058b538f9b7bbe38d642a28191e7720`. Entre ese HEAD y el HEAD de regresión `46e52ea` no cambiaron `DbContext`, entidades, configuraciones, snapshots ni migraciones; el único cambio bajo `backend/src/Infrastructure` fue lógica de `SecuenciaDocumentoService`, así que el modelo y el SQL DDL comparados permanecieron iguales.
- En el HEAD de regresión, la lane Oracle EF10/net10 pasó 2,336 unit tests y 22 integraciones; el workflow registra 34 probes LINQ de repositorios. También pasaron los contratos de DML para transacción/rollback, duplicate key 1062, retry, JSON, decimal, `datetime(6)` y collation. La suite vigente Pomelo pasó 28/28 integraciones en el gate exact-head.

## Dictamen

La revisión del SQL generado no encontró diferencias físicas en el bootstrap DDL del provider candidato frente al esquema canónico. La historia de 107 migraciones sigue siendo autoridad Pomelo y no se reescribe; Oracle usa un baseline de adopción separado. El DML/consultas del candidate lane ejecuta probes contra MySQL, incluyendo repositorios y contratos provider.

**Punto 22: CERRADO** con comparación física del DDL y ejecución de contratos SQL/DML en la lane candidata. No se alteró el esquema productivo, el TargetFramework productivo ni se inició Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
