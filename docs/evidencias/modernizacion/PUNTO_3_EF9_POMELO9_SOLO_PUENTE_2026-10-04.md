# Punto 3 — EF9 + Pomelo 9 es sólo un puente

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**PASS — el candidato EF Core 9 + Pomelo 9 se limita a una sonda transitoria de CI; no es la arquitectura final ni se adoptó en los proyectos productivos.** El provider objetivo certificado para el cierre de Fase 6 es Oracle `MySql.EntityFrameworkCore 10.0.9` con EF Core `10.0.12` en una copia aislada `net10.0`.

## Evidencia del puente

- El workflow [Modernización - Probe EF9 Pomelo9 DEV](../../../.github/workflows/modernization-ef9-pomelo9-probe.yml) prueba EF Core `9.0.20` + Pomelo `9.0.0` + MySqlConnector `2.6.2` en `net8.0` y luego en una copia temporal `net10.0`.
- El run final `37216002726`, sobre `005d516a74d3ebc52a5799b43ba8a3b062ba45e0`, terminó `success`; su job `EF9 Pomelo9 net8 y net10` y los pasos de ambas lanes, migraciones, `has-pending-model-changes`, suites unitarias/integración, audit tenant y comparación de columnas/índices terminaron correctamente. [Run del probe](https://github.com/solqaryn/Solqaryn/actions/runs/37216002726).
- Hubo dos intentos anteriores fallidos en la etapa de certificación `net8` (`37215447812`, `37215532912`); no se toman como evidencia de aprobación. La conclusión se basa en el run final exitoso.
- Comparación del HEAD vigente `9d2dad3da651d604bd392aef49e2580dbd3d30fa` contra el run exitoso: no hay cambios en el workflow del probe ni en los `.csproj` de Infrastructure, API o tests que éste usa. Por ello, la prueba sigue correspondiendo a los mismos inputs de código relevantes.

## No adopción / arquitectura vigente

- `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` permanece en `net8.0`, EF Core `8.0.2`, Pomelo `8.0.2` y MySqlConnector `2.3.7`.
- `backend/src/API/Solqaryn.API.csproj` permanece en `net8.0`; no hay retarget productivo a `net10.0`.
- El gate definitivo de Fase 6 en el HEAD vigente terminó `success` en el run `37228972369`; selecciona Oracle EF10, no EF9/Pomelo9. [Run exact-head de Fase 6](https://github.com/solqaryn/Solqaryn/actions/runs/37228972369).
- Las modificaciones EF9/Pomelo9 y el retarget a net10 viven sólo dentro del runner/copia efímera del probe; no se guardan en los proyectos productivos.
- `PHASE7_EXECUTED=false`.

## Criterio de cierre

El punto se cierra porque el puente se probó, su carácter efímero está explícito y la ruta final certificada es EF10 con Oracle Connector/NET. No se requiere convertir EF9/Pomelo9 en dependencia ni cambiar el código productivo.

MAPA_ARQUITECTURA: SIN_CAMBIO.
