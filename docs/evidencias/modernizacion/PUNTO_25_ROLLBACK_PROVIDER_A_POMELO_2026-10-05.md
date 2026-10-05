# Punto 25 — Rollback del provider candidato a Pomelo

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
HEAD exacto certificado: `14e9f056bb24a894eb18e23976b0c1bef2bbb442`

## Prueba reproducible

El job Oracle baseline crea dos bases aisladas desde la historia Pomelo de 107 migraciones, aplica en una de ellas el marcador de adopción Oracle y luego vuelve a abrir esa misma base con el `AppDbContextFactory`, proyecto API, CLI y provider Pomelo vigentes. Antes/después calcula hashes de todo el DDL y del dump de datos (excluyendo únicamente la tabla de historial de migraciones); compara además la fila de usuario determinista sembrada por la historia.

La secuencia del job `111617518111`, run exact-head `37264224218`, pasó con estos resultados:

- Resolución del contexto de regreso: `Provider name: Pomelo.EntityFrameworkCore.MySql`.
- `dotnet-ef` Pomelo ejecutó `database update` y respondió `No migrations were applied. The database is already up to date.`; `has-pending-model-changes` pasó y la historia conocida de Pomelo quedó en **107 migraciones**.
- Los hashes de esquema y datos antes/después fueron iguales; el usuario sembrado `admin` conservó el mismo hash de fila.
- El marcador Oracle de adopción permanece en `__EFMigrationsHistory`; EF/Pomelo lo ignora como una migración desconocida y puede seguir operando con su historia certificada.
- Dictamen emitido por CI: `ORACLE_TO_POMELO_PROVIDER_ROLLBACK=PASS schema_unchanged=true application_data_unchanged=true seeded_user_preserved=true pomelo_history=107 oracle_baseline_marker_retained=true`.

## Límites operativos

El rollback probado es volver al código/configuración Pomelo sobre el esquema canónico intacto; no se intenta ejecutar `Down()` de Oracle. El baseline Oracle lo rechaza deliberadamente para impedir un rollback destructivo. Si una futura adopción alterara el esquema o los datos, la recuperación será restaurar el backup Pomelo certificado y validado antes de volver a dirigir la aplicación; no se afirma rollback de cambios de schema arbitrarios.

**Punto 25: CERRADO** para el flujo de adopción baseline ensayado en DEV efímero. No se modificaron datos reales, TFM productivo, QA, `main` ni PROD; Fase 7 no se inició.

MAPA_ARQUITECTURA: SIN_CAMBIO.
