# Punto 6 — Bootstrap limpio con la ruta Oracle EF10

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**PASS — revalidado exact-head.** Existe un paquete reproducible y probado para crear una instalación limpia con el esquema canónico y adoptarlo con Oracle EF10. El flujo no necesita ejecutar las 107 migraciones históricas con Oracle: importa el baseline físico derivado de ellas y aplica el baseline Oracle EF10.

## Procedimiento certificado

Para una base MySQL 8.4 recién creada, el paquete de GitHub Actions contiene:

1. `solqaryn-mysql-canonical-baseline.sql`: esquema físico y semillas que crea la historia vigente, generados desde una base CI vacía con Pomelo.
2. `oracle-baseline.sql`: migración SQL generada con EF Core 10.0.12 / Connector/NET 10.0.9 que registra la adopción y valida fail-closed que existan las tablas base.
3. `oracle-baseline-id.txt`, hash SHA-256 y fuentes/proyecto de la migración Oracle temporal.

El workflow crea una tercera base vacía, importa ambos SQL en orden, confirma la fila de historia Oracle, más de 100 tablas y cero cambios pendientes del modelo con dotnet-ef 10.0.12. Luego compara el resultado del bootstrap SQL y de la adopción EF con el esquema Pomelo de referencia.

La equivalencia coteja tablas, engine, collation de tabla y create options; columnas, tipos, defaults y collations; índices; constraints; reglas y columnas de FK; CHECK; vistas y definiciones; triggers y cuerpos; rutinas y eventos. También comprueba el conteo de tablas. Se valida por separado la adopción de Oracle sobre un esquema Pomelo existente sin DDL.

## Evidencia exact-head

- Run `37232587801`, HEAD `0e4b552ed058b538f9b7bbe38d642a28191e7720`: 21/21 pasos `success`, incluyendo aplicación real del paquete SQL a una base vacía, `has-pending-model-changes`, equivalencia física completa y adopción sin DDL. [Ejecución del bootstrap Oracle EF10](https://github.com/solqaryn/Solqaryn/actions/runs/37232587801).
- Artefacto descargable `oracle-baseline-candidate-37232587801`, 227,854 bytes, con expiración 2027-01-02. El workflow lo regenera al cambiar cualquiera de los dos árboles de migraciones, los proyectos/providers del scope o las evidencias de estos puntos.
- Scope gate y rama `dev` pasaron; el baseline proviene sólo de bases vacías efímeras. No contiene backup ni datos de Aiven/QA/PROD.

## Revalidación del HEAD vigente — 2026-10-05

- Run [37280729749](https://github.com/solqaryn/Solqaryn/actions/runs/37280729749), HEAD `cf0607908701e5a9e3015dcc463f2a6beb87b2cf`: el único job del probe terminó `success`.
- `ORACLE_EF10_SQL_PACKAGE_FRESH_BOOTSTRAP=PASS tables=136`; `ORACLE_EF10_SCRIPT_BOOTSTRAP_SCHEMA_EQUIVALENT=true`; `ORACLE_BASELINE_SCHEMA_EQUIVALENT=true`; `ORACLE_BASELINE_ADOPTION_NO_DDL=PASS`; `has-pending-model-changes` completó correctamente.
- Rollback confirmado: `ORACLE_TO_POMELO_PROVIDER_ROLLBACK=PASS`, esquema y datos sin cambio, usuario semilla preservado, 107 migraciones Pomelo y marcador Oracle retenidos.
- Artefacto de este HEAD: `oracle-baseline-candidate-37280729749`, 237,107 bytes, digest `sha256:309175859be6cc938e2a4edfdc3a162055a79c20fce31cb1828b79ccbde626a8`, expira `2027-01-03T07:58:03Z`.

**Punto 6: CERRADO.** Bootstrap reproducible validado en MySQL descartable; no se usaron servicios persistentes ni se inició Fase 7.

## Alcance

- El paquete de bootstrap es una salida reproducible del gate DEV; el artefacto tiene retención de 90 días y puede regenerarse desde el HEAD vigente.
- No se despliega, no se conecta a entornos reales, no altera proyectos productivos y no ejecuta Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.


## Revalidación exact-head DEV — 2026-10-06

**Estado vigente: PASS** sobre `dev` SHA `27d84f8112a68b62dc473a17083e299bdf215e9e`; probe [run 37419454048](https://github.com/solqaryn/Solqaryn/actions/runs/37419454048) terminó `success` en ese SHA.

- `ORACLE_EF10_SQL_PACKAGE_FRESH_BOOTSTRAP=PASS tables=136` en una base MySQL vacía.
- `ORACLE_EF10_SCRIPT_BOOTSTRAP_SCHEMA_EQUIVALENT=true` y `ORACLE_BASELINE_SCHEMA_EQUIVALENT=true`.
- `ORACLE_BASELINE_ADOPTION_NO_DDL=PASS`; `has-pending-model-changes` post-bootstrap completó.
- Rollback `ORACLE_TO_POMELO_PROVIDER_ROLLBACK=PASS`: esquema/datos sin cambio, usuario semilla preservado, `pomelo_history=107` y marcador Oracle retenido.
- Artefacto exact-head [oracle-baseline-candidate-37419454048](https://github.com/solqaryn/Solqaryn/actions/runs/37419454048/artifacts/11392766256): 237,110 bytes, digest `sha256:e2db159851f66ee430404358e641d89ea09775f24ea3021d023a653dc767eb2a`, expira `2027-01-04T05:37:26Z`.
- Gate Fase 6 exact-head [run 37419454153](https://github.com/solqaryn/Solqaryn/actions/runs/37419454153) terminó `success`.

Esta PR sólo documenta evidencia. El workflow regenera el artefacto al cambiar esta acta; su nuevo HEAD volverá a ejecutar el probe antes de confirmar el cierre vigente. Fase 7 sigue sin ejecutarse.
