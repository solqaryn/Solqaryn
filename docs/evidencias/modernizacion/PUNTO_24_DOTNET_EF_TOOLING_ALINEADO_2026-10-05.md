# Punto 24 — SDK y dotnet-ef alineados con la lane candidata

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Hallazgo y corrección

La evidencia anterior demostraba CLI EF `10.0.12`, pero los workflows pedían `10.0.x`, por lo que el SDK podía variar sin cambiar el repositorio. Se fijó SDK `10.0.401` en las cuatro lanes Oracle EF10/baseline relevantes y la lane candidata ahora falla explícitamente si `dotnet --version` no coincide. La documentación oficial confirma que SDK `10.0.401` incluye runtime y ASP.NET Core `10.0.12` ([descargas .NET 10 de Microsoft](https://dotnet.microsoft.com/es-es/download/dotnet/10.0)).

El script `scripts/modernization/oracle10-provider-lane.sh` además instala `dotnet-ef` en ruta temporal como `10.0.12`, verifica su versión y ejecuta `dbcontext info` para `AppDbContext` desde el startup API de la copia EF10. Las referencias EF Core y Design se retargetean en esa copia a `10.0.12`; los proyectos productivos permanecen en EF8/Pomelo durante Fase 6.

Las invocaciones `dotnet-ef 8.0.8` en pasos de baseline son intencionales: aplican la historia heredada Pomelo antes de ejecutar la lane EF10 aislada; no son el CLI del provider candidato.

## Certificación

- Commit de la corrección: `e48c463be6796ce54ee820db88c1c11317f955d7`.
- Gate principal exact-head de Fase 6: run `37325231888`, `success`; baseline Oracle independiente: `37325231937`, `success`; gate final: `37325231804`, `success`; scope lock: `37325231976`, `success`.
- Log de Oracle EF10/net10: `ORACLE10_SDK=PASS version=10.0.401`, `ORACLE10_EF_TOOLCHAIN=PASS runtime=10.0.12 design=10.0.12 cli=10.0.12`, `ORACLE_EF10_NET10_PROVIDER_LANE=PASS`; 2,336 unitarias, 22 integraciones y 34 probes LINQ pasaron.
- Pomelo actual y la copia temporal net10/EF8-Pomelo pasaron sus gates respectivos; el dictamen Fase 6 terminó `PASS`, `P0=0`, `P1=0`.

**Punto 24: CERRADO.** SDK, runtime/design EF y CLI quedaron exactamente fijados y comprobados en el carril candidato. No se retargetearon los proyectos productivos ni se inició Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
