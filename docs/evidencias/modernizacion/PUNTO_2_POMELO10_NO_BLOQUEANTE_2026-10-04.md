# Punto 2 — Pomelo 10 no es dependencia bloqueante

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Problema original

Pomelo 10 estable no estaba disponible. Mientras la modernización dependía de esa familia como única salida, la ausencia impedía cerrar una ruta EF Core 10.

## Verificación quirúrgica

1. El gate intenta resolver exactamente `Pomelo.EntityFrameworkCore.MySql 10.0.0` desde el feed estable.
2. La resolución actual devuelve no disponible.
3. El upstream público de Pomelo mantiene 9.0.0 como último release estable y el soporte EF Core 10 continúa sin release estable.
4. No se permite usar nightly, preview o RC como sustituto de un provider final certificado.
5. La ruta Oracle certificada del Punto 1 se vuelve a ejecutar en el mismo gate.
6. La condición de PASS de Fase 6 depende de Oracle EF10, no del booleano de disponibilidad de Pomelo 10.
7. Si Pomelo 10 aparece en el futuro, se clasifica como alternativa no seleccionada y exige certificación independiente antes de cualquier cambio.

## Contrato permanente

- `POMELO10_REQUIRED_FOR_TARGET_ROUTE=false`
- `POMELO10_ROUTE_SELECTED=false`
- `POMELO10_BLOCKS_MODERNIZATION=false`
- ausencia actual: `UNAVAILABLE_NON_BLOCKING`
- futura disponibilidad: `AVAILABLE_NOT_SELECTED_PENDING_SEPARATE_CERTIFICATION`
- provider objetivo vigente: `ORACLE_MYSQL_EFCORE_10_0_9`
- EF objetivo vigente: `10.0.12`
- TFM objetivo vigente: `net10.0`
- `PHASE7_EXECUTED=false`

## Criterio de cierre

El Punto 2 queda certificado sólo si, sobre el HEAD exacto de `dev`:

- el probe confirma el estado actual de Pomelo 10;
- la lane Oracle EF10 termina SUCCESS;
- el dictamen general termina SUCCESS aun con `POMELO_EF10_STABLE_AVAILABLE=false`;
- se emite `POINT_2_POMELO10_DEPENDENCY=PASS`;
- P0=0 y P1=0 para este scope;
- no se modifica ningún `.csproj` productivo ni se ejecuta Fase 7.

## Revalidación exact-head — 2026-10-04

- El índice oficial de NuGet consultado en esta fecha lista `9.0.0` como última versión de `Pomelo.EntityFrameworkCore.MySql` y no contiene ninguna versión estable `10.x`: [índice oficial del paquete](https://api.nuget.org/v3-flatcontainer/pomelo.entityframeworkcore.mysql/index.json).
- El workflow permanente intentó resolver exactamente `10.0.0`; su sonda clasifica la ausencia como informativa y el dictamen final la normaliza a `UNAVAILABLE_NON_BLOCKING`.
- El run `37228315030`, sobre HEAD `04b71f798680a64d300084f26135d3d16ebe6030`, terminó `success`: los cinco jobs, incluida `Oracle EF10 net10 - lane final certificada` y `Dictamen Fase 6`, fueron exitosos. [Ejecución de Fase 6 en GitHub](https://github.com/solqaryn/Solqaryn/actions/runs/37228315030).
- El resultado de esa lane confirma `POINT_2_POMELO10_DEPENDENCY=PASS`, con Pomelo 10 fuera de la ruta seleccionada y sin convertir la falta de release en bloqueo.
- Esta revalidación no cambia paquetes, `.csproj` productivos, TargetFramework ni despliega Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.


## Revalidación exact-head DEV — 2026-10-06

- HEAD de `dev`: `46bb3e41ebb01a81ea5801d18c11472ea5dbdd9a`.
- Gate de Fase 6 [run 37411140780](https://github.com/solqaryn/Solqaryn/actions/runs/37411140780) terminó `success` sobre ese mismo SHA, con dictamen `FASE_6_MYSQL_EF_PROVIDER=PASS`, `POINT_2_POMELO10_DEPENDENCY=PASS`, `P0=0`, `P1=0` y `PHASE7_EXECUTED=false`.
- El probe del mismo gate resolvió Pomelo `10.0.0`: `POMELO_EF10_STABLE_AVAILABLE=false`; salida `POMELO10_ROUTE_STATUS=UNAVAILABLE_INFORMATIONAL`; `POMELO10_REQUIRED_FOR_TARGET_ROUTE=false`.
- El dictamen final normalizó ese resultado a `UNAVAILABLE_NON_BLOCKING` y mantuvo la ruta Oracle EF10 con baseline/adopción certificada.
- Revisión de fuentes upstream al 2026-10-06: el README y el listado de releases de [Pomelo](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql) siguen mostrando `9.0.0` como release final; NuGet presenta [Pomelo.EntityFrameworkCore.MySql 9.0.0](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql/). No se seleccionó nightly, preview ni RC.

**Revalidación del Punto 2: PASS en `dev` HEAD `46bb3e41ebb01a81ea5801d18c11472ea5dbdd9a`.** La falta de Pomelo EF10 estable es informativa y no bloquea la ruta objetivo ya certificada.
