# Punto 11 — Forward migrations y coherencia del modelo EF

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**PASS — el historial y snapshot vigentes de Pomelo se aplican sin drift; la ruta Oracle EF10 genera/aplica su migración de baseline/adopción y `has-pending-model-changes` queda en cero.** El historial histórico de 107 migraciones no se reescribe ni se intenta ejecutar directamente con Oracle.

## Evidencia Pomelo (autoridad actual en net8/EF8)

- El gate de Fase 6 instala `dotnet-ef 8.0.8`, aplica toda la historia Pomelo y ejecuta `dotnet ef migrations has-pending-model-changes`.
- Exact-head actual `37237921281`, commit `915c4853d0c2bd695adc79bb5386a3fe64032be8`: el job `Pomelo actual - migraciones SQL modelo integración y contratos MySQL` terminó `success`, incluyendo los pasos de migración y pending-model.
- Inventario previamente verificado: 107 archivos e IDs únicos, sin reescritura de la historia.

## Evidencia Oracle EF10 (ruta objetivo)

- La lane de baseline crea, desde el modelo vigente, un assembly EF Core 10.0.12 y una migración temporal `OracleBaseline`; genera/aplica su SQL a una base vacía restaurada desde el esquema canónico y también prueba adopción del esquema existente sin DDL.
- Antes y después de la adopción, `dotnet-ef 10.0.12 migrations has-pending-model-changes` debe terminar correctamente; el job también verifica la fila de historia, migración forward y cero migraciones pendientes.
- Run `37232938573` (`success`) completó todos los pasos de generación, aplicación, comparación física y adopción sin DDL sobre el commit `aeee293abb4beb757ff4196ebd52308bd54f2320`.
- Comprobación de continuidad: entre ese run y el HEAD actual no se modificaron `AppDbContext`, el snapshot/migraciones, configuraciones ni entidades/enums del modelo. Los cambios posteriores de esta lista cubrieron contratos de excepción, pruebas de provider y documentación.
- Evidencia de bootstrap: [Punto 6 — Oracle clean bootstrap](docs/evidencias/modernizacion/PUNTO_6_ORACLE_CLEAN_BOOTSTRAP_2026-10-04.md).

## Dictamen exact-head actual

El gate de Fase 6 `37237921281` sobre `915c4853d0c2bd695adc79bb5386a3fe64032be8` terminó 5/5 jobs `success`, `P0=0`, `P1=0`. La lane Oracle conserva `PHASE7_EXECUTED=false`.

## Alcance

Oracle no se presenta como capaz de reproducir las 107 migraciones Pomelo; la autoridad histórica se preserva y la adopción usa baseline físico verificable. No se modifica producción ni se ejecuta Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
