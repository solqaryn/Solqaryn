# Punto 25 — Rollback del provider candidato a Pomelo

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Prueba reproducible

El workflow Oracle baseline crea dos bases aisladas desde la historia Pomelo de 107 migraciones. Sobre una aplica el marcador de adopción Oracle sin DDL y reabre **esa misma base** con `AppDbContextFactory`, el proyecto API, CLI y provider Pomelo productivos. Antes y después compara SHA-256 de todo el dump DDL y de los datos (excluyendo sólo `__EFMigrationsHistory`), y el hash de la fila sembrada `Usuarios(Id=1, NombreUsuario='admin')`.

En el run `37325231937`, job `111814159705`, la secuencia completa terminó `success` con estos resultados observados en los logs:

- `Provider name: Pomelo.EntityFrameworkCore.MySql` al abrir `phase6_oracle_adopt`.
- `database update` respondió “No migrations were applied. The database is already up to date”; `has-pending-model-changes` pasó y la historia conocida de Pomelo quedó en 107 migraciones.
- Hash de DDL, hash de datos de aplicación y hash de la fila `admin` antes/después idénticos.
- El marcador `OracleBaseline` permanece en `__EFMigrationsHistory`; Pomelo lo ignora como migración desconocida y conserva las 107 migraciones propias.
- Resultado de CI: `ORACLE_TO_POMELO_PROVIDER_ROLLBACK=PASS schema_unchanged=true application_data_unchanged=true seeded_user_preserved=true pomelo_history=107 oracle_baseline_marker_retained=true`.
- El mismo workflow certificó `ORACLE_BASELINE_ADOPTION_NO_DDL=PASS` y equivalencia física EF/SQL-package `SCHEMA_EQUIVALENT=true` antes de la prueba de regreso.

La evidencia se produjo en el commit de código `e48c463be6796ce54ee820db88c1c11317f955d7`; el HEAD actual `061f6eb1e46b0df19aa1ec2a9ec55b0be8097783` sólo agrega el expediente del punto 24 después de esa prueba, sin cambiar el workflow, el script, el modelo ni el código usado por el rollback.

## Límites operativos

El rollback probado consiste en volver al código/configuración Pomelo sobre el esquema canónico intacto; **no** se ejecuta `Down()` de Oracle. El baseline Oracle lo rechaza deliberadamente para impedir una reversión destructiva. Si una adopción futura alterara esquema o datos, la recuperación sería restaurar el backup Pomelo certificado antes de redirigir la aplicación; esta prueba no afirma rollback de cambios arbitrarios.

**Punto 25: CERRADO** para la adopción baseline ensayada en DEV efímero. No se modificaron datos reales, TFM productivo, QA, `main` ni PROD; Fase 7 no se inició.

MAPA_ARQUITECTURA: SIN_CAMBIO.
