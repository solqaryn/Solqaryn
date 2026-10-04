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

MAPA_ARQUITECTURA: SIN_CAMBIO.
