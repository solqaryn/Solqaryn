# Punto 24 — dotnet-ef alineado con la lane candidata

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
HEAD exacto de la comprobación candidata: `8216027ebaac483f830196da048ff1a964d0ed1c`

## Contrato de versiones

- Runtime/SDK de la lane: .NET SDK `10.0.401`, runtime `10.0.12`.
- `Microsoft.EntityFrameworkCore` y `Microsoft.EntityFrameworkCore.Design` de la copia temporal: `10.0.12`.
- CLI instalada en una ruta efímera aislada: `dotnet-ef 10.0.12`.
- Provider candidato: `MySql.EntityFrameworkCore 10.0.9`.

## Evidencia

El script reproducible `scripts/modernization/oracle10-provider-lane.sh` ahora instala el CLI con versión fija, comprueba su salida de versión y ejecuta `dotnet-ef dbcontext info` contra `AppDbContext` de la copia net10/provider Oracle, usando `AppDbContextFactory` y el proyecto de inicio API. El resultado del run `37262901995`, job `111613645132`, en el HEAD indicado reportó:

```text
Entity Framework Core .NET Command-line Tools
10.0.12
Provider name: MySql.EntityFrameworkCore
ORACLE10_EF_TOOLCHAIN=PASS runtime=10.0.12 design=10.0.12 cli=10.0.12
```

La misma corrida terminó además con **2,336/2,336 pruebas unitarias** y **22/22 integraciones portables** aprobadas. El gate compuesto `37262902089` cerró sus dos jobs (stack actual Pomelo y lane candidata Oracle/net10) con `success`.

El toolchain productivo Pomelo/EF8 permanece intacto durante Fase 6; la igualdad exacta `runtime = Design = CLI = 10.0.12` queda demostrada en la combinación candidata antes de autorizar el retarget productivo. La CLI temporal no se instala globalmente ni cambia archivos del repo.

**Punto 24: CERRADO** para la lane definitiva candidata; CLI y Design resuelven el DbContext correcto bajo el runtime net10. No se modificó el TFM productivo ni se inició Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
