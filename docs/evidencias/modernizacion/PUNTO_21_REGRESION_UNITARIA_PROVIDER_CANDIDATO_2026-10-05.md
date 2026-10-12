# Punto 21 — Regresión unitaria con el provider candidato

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
HEAD exacto de código certificado: `5e85582fa555b4cd374cc5b33de4b68d3136059c`

## Criterio

La suite unitaria y la regresión del backend deben pasar con la combinación candidata .NET 10 + EF Core 10 + Oracle Connector/NET, incluyendo expresiones, LINQ, converters, interceptors, repositorios y UnitOfWork. La ejecución candidata usa copias temporales; no cambia el TargetFramework ni las dependencias productivas del repositorio.

## Evidencia de CI

- Gate Fase 6 exact-head run `37316643791`, HEAD `5e85582fa555b4cd374cc5b33de4b68d3136059c`: dictamen `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- La copia diagnóstica EF8/Pomelo retargeteada a net10 pasó ahora **2388/2388 unitarias** y **28/28 integraciones**, 0 fallos y 0 omitidas. Esto cierra los ocho errores de `ReporteComprasServiceTests` que previamente reproducían el fallo de evaluación `ReadOnlySpan<int>` al usar arreglos capturados por `Contains`; el cambio usa listas y conserva el resultado de las consultas.
- La lane Oracle EF10/net10 del mismo gate pasó **2336/2336 unitarias** y **22/22 integraciones portables** con EF Core `10.0.12` + `MySql.EntityFrameworkCore 10.0.9`, MySQL `8.4.11`.
- Pomelo/MySQL 8.4.11 pasó **28/28 integraciones** en la lane productiva actual.
- [Gate Fase 6 exact-head](https://github.com/solqaryn/Solqaryn/actions/runs/37316643791).

- Lane Oracle EF10/net10 del run `37261240274`, job `111608733243`: **2,336/2,336 unit tests aprobados** y **22/22 integraciones portables aprobadas**. Ejecutó EF Core `10.0.12` con `MySql.EntityFrameworkCore 10.0.9` contra MySQL `8.4.11`.
- Gate compuesto de Fase 6, run `37261240261`: job Oracle EF10/net10 `111608839362` **success**; suite Pomelo sobre MySQL real `111608839218` **28/28 integraciones aprobadas, 0 fallos, 0 omitidas**; dictamen `111610719663` **success**.
- Los contratos complementarios de la lane Oracle reportaron PASS para concurrencia, 34 probes LINQ de repositorios, transacciones/rollback, retry, collation, JSON, decimal, `datetime(6)` y aislamiento tenant.
- El candidato incluyó 438 archivos fuente unitarios. Excluyó explícitamente 20 clases acopladas al historial/migraciones exclusivas de Pomelo, porque la lane candidata no importa las 107 migraciones históricas Pomelo; esas clases siguen ejercitándose en el stack productivo Pomelo. No se contabilizan como pruebas candidatas aprobadas.
- Las cuatro integraciones históricas/provider-specific se excluyeron explícitamente de la lane Oracle por el mismo límite de migraciones/bridge; sí forman parte de las 28/28 integraciones de Pomelo.
- El retarget EF8/net10 sigue siendo sólo diagnóstico y no autoriza cambiar el TFM productivo sin alinear el provider. Sus suites ahora pasan 2388/2388 y 28/28, pero el gate continúa requiriendo provider certificado.

## Alcance del cierre

El test concurrente de tipo de cliente ahora verifica que la base persista exactamente un registro predeterminado. No exige que una petición concurrente necesariamente arroje una excepción si el planificador serializa ambas operaciones; si una operación sí pierde por conflicto, su excepción y mensaje se siguen validando. Así se conserva la invariante de datos y se elimina una expectativa temporal no determinista.

**Punto 21: CERRADO.** Oracle EF10/net10 aprueba la suite candidata; además se corrigieron y pasan las 2388 unitarias y 28 integraciones del ensayo EF8/Pomelo bajo net10. Las pruebas históricas excluidas de Oracle continúan cubiertas por el stack actual Pomelo/MySQL. **Fase 6: PASS** en el HEAD indicado. No se modificó el TFM productivo ni se inició Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
