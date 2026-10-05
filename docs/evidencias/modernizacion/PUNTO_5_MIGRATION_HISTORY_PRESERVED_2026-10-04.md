# Punto 5 — Preservación del historial de migraciones Pomelo

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**PASS — revalidado exact-head; el historial vigente se conserva sin reescribir migraciones.** La migración a Oracle EF10 se resuelve con un baseline físico canónico y una migración Oracle separada de adopción; las migraciones antiguas no se transforman ni se eliminan.

## Inventario verificado

- Hay `107` archivos de migración fuente, `107` identificadores `[Migration]` únicos y cero duplicados.
- `67` de las 107 fuentes contienen anotaciones/estrategias MySQL/Pomelo; no son portables por simple recompilación Oracle. La evidencia por eso no propone editarlas en sitio.
- El árbol de migraciones y el `.csproj` de Infrastructure no tienen diferencias entre el commit del probe verde previo (`dff086d8178489212a795a74c828b89d844435de`) y el HEAD certificado de esta ejecución.
- El gate permanente incluye ambos directorios de migraciones (`Infrastructure/Migrations/**` y `Infrastructure/Persistence/**`) en sus rutas de activación, además del `.csproj` que controla su compilación.

## Evidencia de aplicación sin reescritura

- El probe crea desde cero dos esquemas de referencia aplicando la historia actual con Pomelo/dotnet-ef 8.0.28; la baseline sólo se genera después de esa aplicación completa.
- Exporta/restaura el esquema físico completo para Oracle EF10.12, sin borrar ni modificar fuentes históricas; Oracle registra una migración de baseline aparte.
- El run `37231172751`, sobre HEAD `0b92db7a7829aaf3353635d52d7e7899e7700e9e`, terminó `success` en sus 20 pasos: aplicación de la historia, baseline restaurado, generación/adopción Oracle, `has-pending-model-changes`, comparación física y adopción existente sin DDL. [Run exact-head del baseline](https://github.com/solqaryn/Solqaryn/actions/runs/37231172751).
- El workflow valida rama `dev`, scope y checkout limpio antes de empezar. Las copias Oracle y los artefactos se generan en el runner; los cambios de TFM/dependencias se hacen sólo en una copia efímera.

## Revalidación actual — 2026-10-05

- El inventario local sobre las dos rutas de migraciones confirma 107 fuentes timestamped y 107 atributos `[Migration]` únicos, cero duplicados.
- De `dff086d8178489212a795a74c828b89d844435de` al HEAD `b86bf80b9cd2f3ca223fa3c59b810e123781db7a` no hay diferencias en los árboles `Infrastructure/Migrations/**`, `Infrastructure/Persistence/Migrations/**` ni en `Solqaryn.Infrastructure.csproj`.
- Como reproducción de la métrica de provider-specific files, 67/107 fuentes coinciden con tokens `MySql:`, `MySqlValueGenerationStrategy` o `Pomelo`; no se propone portar ni reescribir esas migraciones.
- El probe ampliado exact-head [37280097642](https://github.com/solqaryn/Solqaryn/actions/runs/37280097642), sobre `b86bf80b9cd2f3ca223fa3c59b810e123781db7a`, terminó `success`: aplicó la historia Pomelo, produjo/restauró la baseline y rollback posterior confirmó `pomelo_history=107`, preservando esquema y datos.

**Punto 5: CERRADO.** Los 107 IDs/fuentes permanecen intactos; Fase 7 no se ejecutó.

## Alcance

- Se preservan los 107 archivos/IDs históricos y su orden. No se reescribe la historia para ocultar incompatibilidades.
- El registro/paquete de baseline operativo para una instalación real se certifica por separado en el Punto 6.
- No cambian proyectos productivos, historial, datos, QA, `main` ni PROD; Fase 7 no se ejecuta.

MAPA_ARQUITECTURA: SIN_CAMBIO.
