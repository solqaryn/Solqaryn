# Punto 11 — Forward migrations y coherencia del modelo EF

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**PASS — el historial y snapshot vigentes de Pomelo se aplican sin drift; la ruta Oracle EF10 genera/aplica su migración de baseline/adopción y `has-pending-model-changes` queda en cero.** El historial histórico de 107 migraciones no se reescribe ni se intenta ejecutar directamente con Oracle.

## Evidencia Pomelo (autoridad actual en net8/EF8)

- El gate de Fase 6 instala `dotnet-ef 8.0.8`, aplica toda la historia Pomelo y ejecuta `dotnet ef migrations has-pending-model-changes`.
- Exact-head vigente `37298140358`, HEAD `a02a6049bed532ae0d26ffcc49c4ea3151985932`: el job `Pomelo actual - migraciones SQL modelo integración y contratos MySQL` terminó `success`, aplicó la historia completa y `dotnet-ef migrations has-pending-model-changes` informó `No changes have been made to the model since the last migration`.
- Inventario previamente verificado: 107 archivos e IDs únicos, sin reescritura de la historia.

## Evidencia Oracle EF10 (ruta objetivo)

- La lane de baseline crea, desde el modelo vigente, un assembly EF Core 10.0.12 y una migración temporal `OracleBaseline`; genera/aplica su SQL a una base vacía restaurada desde el esquema canónico y también prueba adopción del esquema existente sin DDL.
- Antes y después de la adopción, `dotnet-ef 10.0.12 migrations has-pending-model-changes` debe terminar correctamente; el job también verifica la fila de historia, migración forward y cero migraciones pendientes.
- En el run exact-head `37298140358`, la lane Oracle EF10/net10 terminó `success`: recompiló la copia efímera, aplicó la historia Pomelo sólo para obtener el esquema físico canónico y completó la generación/aplicación del baseline Oracle y su ruta de adopción.
- Comprobación de continuidad: entre ese run y el HEAD actual no se modificaron `AppDbContext`, el snapshot/migraciones, configuraciones ni entidades/enums del modelo. Los cambios posteriores de esta lista cubrieron contratos de excepción, pruebas de provider y documentación.
- Evidencia de bootstrap: [Punto 6 — Oracle clean bootstrap](docs/evidencias/modernizacion/PUNTO_6_ORACLE_CLEAN_BOOTSTRAP_2026-10-04.md).

## Dictamen exact-head actual

El gate de Fase 6 `37298140358` sobre `a02a6049bed532ae0d26ffcc49c4ea3151985932` terminó con todos los jobs `success`, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`; `PHASE7_EXECUTED=false`. [Dictamen exact-head de Fase 6](https://github.com/solqaryn/Solqaryn/actions/runs/37298140358).

## Alcance

Oracle no se presenta como capaz de reproducir las 107 migraciones Pomelo; la autoridad histórica se preserva y la adopción usa baseline físico verificable. No se modifica producción ni se ejecuta Fase 7.

**Punto 11: CERRADO.** La historia Pomelo se aplica sin drift y la estrategia Oracle baseline/adoption pasa el gate exact-head; no se reescribe ni se ejecuta la historia histórica con Oracle.

MAPA_ARQUITECTURA: SIN_CAMBIO.
