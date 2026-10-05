# Punto 21 — Regresión unitaria con el provider candidato

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
HEAD exacto certificado: `46e52ea9887db783183c2bfe529819b487547685`

## Criterio

La suite unitaria y la regresión del backend deben pasar con la combinación candidata .NET 10 + EF Core 10 + Oracle Connector/NET, incluyendo expresiones, LINQ, converters, interceptors, repositorios y UnitOfWork. La ejecución candidata usa copias temporales; no cambia el TargetFramework ni las dependencias productivas del repositorio.

## Evidencia de CI

- Lane Oracle EF10/net10 del run `37261240274`, job `111608733243`: **2,336/2,336 unit tests aprobados** y **22/22 integraciones portables aprobadas**. Ejecutó EF Core `10.0.12` con `MySql.EntityFrameworkCore 10.0.9` contra MySQL `8.4.11`.
- Gate compuesto de Fase 6, run `37261240261`: job Oracle EF10/net10 `111608839362` **success**; suite Pomelo sobre MySQL real `111608839218` **28/28 integraciones aprobadas, 0 fallos, 0 omitidas**; dictamen `111610719663` **success**.
- Los contratos complementarios de la lane Oracle reportaron PASS para concurrencia, 34 probes LINQ de repositorios, transacciones/rollback, retry, collation, JSON, decimal, `datetime(6)` y aislamiento tenant.
- El candidato incluyó 438 archivos fuente unitarios. Excluyó explícitamente 20 clases acopladas al historial/migraciones exclusivas de Pomelo, porque la lane candidata no importa las 107 migraciones históricas Pomelo; esas clases siguen ejercitándose en el stack productivo Pomelo. No se contabilizan como pruebas candidatas aprobadas.
- Las cuatro integraciones históricas/provider-specific se excluyeron explícitamente de la lane Oracle por el mismo límite de migraciones/bridge; sí forman parte de las 28/28 integraciones de Pomelo.
- El retarget diagnóstico aislado de .NET 10 manteniendo EF Core 8 no es la combinación candidata ni una suite aprobatoria: falló y su dictamen dice STOP para cambiar únicamente el TFM. No se presenta como regresión del candidato EF10.

## Alcance del cierre

El test concurrente de tipo de cliente ahora verifica que la base persista exactamente un registro predeterminado. No exige que una petición concurrente necesariamente arroje una excepción si el planificador serializa ambas operaciones; si una operación sí pierde por conflicto, su excepción y mensaje se siguen validando. Así se conserva la invariante de datos y se elimina una expectativa temporal no determinista.

**Punto 21: CERRADO** para la combinación candidata aislada Oracle EF10/net10, con suite unitaria y regresión portable aprobadas; las pruebas históricas excluidas permanecen cubiertas por el stack actual Pomelo/MySQL. **Fase 6: PASS** en el dictamen del HEAD indicado. No se modificó el TFM productivo ni se inició Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
