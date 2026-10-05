# Punto 12 — Equivalencia física de esquema

Fecha: 2026-10-04
Repositorio: `solqaryn/Solqaryn`
Rama: `dev`

## Hallazgo y corrección

La lane de baseline ya comparaba tablas, columnas, índices, constraints, FK, CHECK, vistas, triggers, rutinas y eventos. La revisión detectó que faltaban algunos metadatos físicos: formato/comentario de tabla, charset explícito, expresión/comentario/SRS de columnas, propiedades de índices funcionales/visibilidad/prefijo, particiones, parámetros de rutinas y defaults de charset/collation de la base. Se amplió el comparator para incluirlos además de los contratos previamente cubiertos.

La captura ahora compara, entre la referencia creada aplicando las migraciones Pomelo, la restauración SQL canónica y el bootstrap Oracle EF10:

- Tablas: engine, row format, collation, create options y comentario.
- Columnas: tipo completo, nullabilidad, default, extra, charset/collation, expresión generada, comentario y SRS.
- Índices: unicidad, orden, columna/expresión, longitud de prefijo, dirección, tipo, visibilidad y comentario.
- Particiones/subparticiones, método, expresiones, límites, nodegroup, tablespace y comentario.
- Constraints, estado de enforcement de CHECK, FK y columnas/orden/reglas referenciales.
- Vistas, triggers, rutinas, parámetros de rutinas y eventos.
- Charset y collation por defecto de cada schema.

Cada exportación se ordena de forma determinista y se compara con `diff`; cualquier diferencia falla el job. Se excluye sólo `__EFMigrationsHistory` del cotejo del schema de aplicación, pues los proveedores mantienen historial distinto.

## Evidencia

- Run previo `37232938573` sobre `aeee293abb4beb757ff4196ebd52308bd54f2320`: comparator anterior success, 136 tablas en el paquete SQL limpio y adopción sin DDL. No basta por sí solo para certificar los nuevos campos.
- Primera ejecución ampliada `37241337349` falló porque `INFORMATION_SCHEMA.PARTITIONS` no contiene `ENGINE` ni `CREATE_OPTIONS`; se quitaron esos campos.
- Segunda ejecución `37241647354` llegó al comparator, pero detectó que `CHECK_CONSTRAINTS` no contiene `ENFORCED`; ese atributo sí se compara desde `TABLE_CONSTRAINTS.ENFORCED`, por lo que se quitó la referencia duplicada inválida. No se declaró PASS.
- El comparator ampliado se volvió a ejecutar en `37280729749`, HEAD `cf0607908701e5a9e3015dcc463f2a6beb87b2cf`: job completo `success`; `ORACLE_EF10_SCRIPT_BOOTSTRAP_SCHEMA_EQUIVALENT=true`; `ORACLE_BASELINE_SCHEMA_EQUIVALENT=true`; `ORACLE_EF10_SQL_PACKAGE_FRESH_BOOTSTRAP=PASS tables=136`; `ORACLE_BASELINE_ADOPTION_NO_DDL=PASS`.
- El gate integral más reciente `37298140358`, HEAD `a02a6049bed532ae0d26ffcc49c4ea3151985932`, terminó `success` en provider/net10 y dictamen: `P0=0`, `P1=0`, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `PHASE7_EXECUTED=false`.
- Continuidad comprobada hasta ese HEAD: no hubo cambios en `.github/workflows/modernization-oracle-baseline-probe.yml`, `backend/src/Infrastructure/Persistence`, `backend/src/Infrastructure/Migrations` ni `Solqaryn.Infrastructure.csproj` desde el run comparator `37280729749`. Por tanto, sus entradas de esquema/modelo/historia siguen siendo las mismas; los cambios posteriores relevantes de provider se volvieron a probar en el gate integral.

## Alcance

La prueba compara el contrato físico producido por las rutas CI efímeras en MySQL 8.4. No modifica esquemas persistentes ni datos reales y no ejecuta Fase 7. El gate integral pasó contra el HEAD vigente al cerrar este punto.

**Punto 12: CERRADO.** Comparador físico ampliado, bootstrap limpio de 136 tablas y adopción sin DDL pasan; sus entradas permanecen sin cambios desde la corrida comparator.

MAPA_ARQUITECTURA: SIN_CAMBIO.
