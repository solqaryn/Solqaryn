# Punto 6 — Bootstrap limpio con la ruta Oracle EF10

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**PASS — existe un paquete reproducible y probado para crear una instalación limpia con el esquema canónico y adoptarlo con Oracle EF10.** El flujo no necesita ejecutar las 107 migraciones históricas con Oracle: importa el baseline físico derivado de ellas y aplica el baseline Oracle EF10.

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

## Alcance

- El paquete de bootstrap es una salida reproducible del gate DEV; el artefacto tiene retención de 90 días y puede regenerarse desde el HEAD vigente.
- No se despliega, no se conecta a entornos reales, no altera proyectos productivos y no ejecuta Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
