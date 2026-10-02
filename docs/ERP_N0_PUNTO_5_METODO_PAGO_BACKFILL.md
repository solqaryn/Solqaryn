# ERP-N0 — Punto 5: Migración, preflight y backfill histórico de `MetodoPago`

**Estado:** ✅ CERRADO / IMPLEMENTADO Y CERTIFICADO  
**Rama:** `Desarrollo`  
**Fecha:** 2026-08-11  
**Baseline técnico preservado antes de N0.5:** `215d5feed3cdd4725b7c89a48bf8bad55874c6aa`  
**Implementación N0.5:** `4a7ef8d7d1cf741373b37bdeca6c969abea5e569`  
**Corrección de compatibilidad MySQL del postcheck:** `0b0d18b6fe5cee2380175b0d6175b87274ad157e`

## 1. Objetivo

Migrar de forma segura los métodos de pago históricos hacia la autoridad relacional creada en el Punto 4, asignando `MetodoPagoId` sin perder, normalizar silenciosamente ni reinterpretar los valores legacy existentes.

El alcance cubre:

- seed histórico equivalente;
- preflight fail-closed;
- backfill de `Venta`;
- backfill de `FacturaPago`;
- backfill de `MovimientoFinanciero`;
- auditoría de compatibilidad legacy en `Compra`;
- postcheck de integridad;
- prueba negativa de bloqueo;
- prueba positiva 1:1 en MySQL 8.4;
- consistencia EF/snapshot.

## 2. Equivalencia histórica certificada

El enum legacy vigente define:

| Valor legacy | Código funcional estable |
|---:|---|
| `1` | `Efectivo` |
| `2` | `Transferencia` |
| `3` | `Tarjeta` |
| `4` | `Otro` |

La migración **no supone** que `MetodosPago.Id` sea igual a `1..4`.

La autoridad de equivalencia es `MetodosPago.Codigo`.

Para `FacturaPago`, que persistía el enum como entero, el mapeo se realiza mediante un `CASE` explícito:

- `1 -> Efectivo`;
- `2 -> Transferencia`;
- `3 -> Tarjeta`;
- `4 -> Otro`.

Para `Venta` y `MovimientoFinanciero`, que persistían texto, el mapeo se realiza por igualdad binaria exacta contra `Codigo`.

## 3. Migración de datos

Se agregó:

- `backend/src/Infrastructure/Migrations/20260812023600_N0_5_BackfillMetodoPagoHistorico.cs`

La migración ejecuta una transición forward-only y fail-closed:

1. valida el contrato legacy antes de escribir;
2. toma snapshots temporales de identidad y valor legacy;
3. genera de forma idempotente los cuatro métodos históricos si no existen;
4. asigna `Venta.MetodoPagoId`;
5. asigna `FacturaPago.MetodoPagoId` mediante conversión explícita `1..4 -> Codigo`;
6. asigna `MovimientoFinanciero.MetodoPagoId` cuando el método legacy no es `NULL`;
7. conserva `NULL -> NULL` en movimientos sin método;
8. verifica dentro de la misma migración que cardinalidad, identidad y valores legacy no hayan cambiado;
9. aborta si cualquier postcondición no se cumple.

`Down()` no borra datos ni intenta reconstruir automáticamente el estado anterior. La migración es deliberadamente **forward-only**; una reversión operacional exige restauración desde respaldo/preflight, evitando una falsa reversibilidad destructiva.

## 4. Seed histórico idempotente

Se generan los códigos:

- `Efectivo`;
- `Transferencia`;
- `Tarjeta`;
- `Otro`.

Cada registro incorpora metadata de trazabilidad ERP-N0.5 con el número legacy correspondiente, pero el número legacy se conserva únicamente como metadata de migración y **no como FK relacional**.

El seed busca por `Codigo` exacto y no duplica registros ya compatibles.

## 5. Preflight N0.5

Se agregó:

- `backend/scripts/preflight-erp-n0-5-metodo-pago.sql`

Es de solo lectura y devuelve `BloqueosN05`.

Debe ser `0` antes del backfill.

Bloquea, entre otros:

- base relacional N0.5 ausente;
- backfill ya aplicado;
- valores desconocidos en `Ventas.MetodoPago`;
- valores distintos de `1..4` en `FacturaPagos.MetodoPago`;
- valores desconocidos no nulos en `MovimientosFinancieros.MetodoPago`;
- valores fuera del contrato histórico en `Compras.MetodoPago`;
- `MetodoPagoId` preexistentes antes del backfill;
- registros de catálogo incompatibles para los cuatro códigos reservados.

La comparación de texto es exacta/binaria para impedir que una collation case-insensitive convierta silenciosamente un valor mal formado en uno válido.

## 6. Postcheck N0.5

Se agregó:

- `backend/scripts/postdeploy-erp-n0-5-metodo-pago.sql`

Debe devolver `BloqueosN05 = 0` después de la migración.

Certifica:

- presencia exacta de los cuatro métodos históricos;
- estado activo/no eliminado;
- `Venta.MetodoPagoId -> Codigo` correcto;
- `FacturaPago.MetodoPagoId -> Codigo` correcto para cada entero `1..4`;
- `MovimientoFinanciero.MetodoPagoId -> Codigo` correcto;
- preservación de `NULL` en movimientos sin método;
- permanencia del contrato legacy mientras continúe la transición;
- existencia de las tres FKs relacionales del Punto 4.

## 7. Postcheck interno y preservación de datos

La propia migración crea snapshots temporales de:

- `Ventas(Id, MetodoPago)`;
- `FacturaPagos(Id, MetodoPago)`;
- `MovimientosFinancieros(Id, MetodoPago)`;
- `Compras(Id, MetodoPago)`.

Después del backfill ejecuta guards independientes que verifican:

- mismo número de filas antes/después;
- mismos IDs;
- mismos valores legacy byte a byte;
- ninguna fila obligatoria sin `MetodoPagoId`;
- ningún `MetodoPagoId` apuntando a un código distinto del legado;
- `MovimientoFinanciero` con método nulo conserva FK nula;
- `Compra` no fue modificada por N0.5.

La separación en guards independientes es necesaria por una limitación de MySQL 8.4: una misma `TEMPORARY TABLE` no puede reabrirse varias veces dentro de un único `SELECT` compuesto. Esta corrección no alteró el algoritmo de mapeo ni los datos migrados.

## 8. Prueba explícita de independencia entre enum e ID relacional

El workflow dedicado crea primero un método ajeno al enum histórico:

- `ChequeExterno` recibe `Id = 1` en la base de certificación.

Después ejecuta el seed de los cuatro métodos históricos.

Esto demuestra de forma ejecutable que:

- `Efectivo = 1` en el enum **no significa** `MetodoPagoId = 1`;
- `Transferencia = 2` **no significa** `MetodoPagoId = 2`;
- `Tarjeta = 3` **no significa** `MetodoPagoId = 3`;
- `Otro = 4` **no significa** `MetodoPagoId = 4`.

Los documentos quedan relacionados por el ID real recuperado a partir del `Codigo` estable.

## 9. Certificación fail-closed

El workflow dedicado inserta intencionalmente:

- `MovimientoFinanciero.MetodoPago = 'Cheque'`.

Se certificó que:

1. el preflight reporta bloqueo;
2. la migración N0.5 falla como corresponde;
3. N0.5 no queda registrada en `__EFMigrationsHistory`;
4. no se generan los cuatro registros históricos;
5. no se escriben `MetodoPagoId` parciales.

Después se elimina exclusivamente el dato inválido de prueba, el preflight vuelve a `0` y se ejecuta el camino válido.

## 10. Certificación positiva 1:1

El workflow dedicado prueba los cuatro métodos en:

- `Venta`;
- `FacturaPago`;
- `MovimientoFinanciero`.

También prueba un movimiento con método nulo.

Resultado certificado:

- `Efectivo -> Codigo Efectivo`;
- `Transferencia -> Codigo Transferencia`;
- `Tarjeta -> Codigo Tarjeta`;
- `Otro -> Codigo Otro`;
- `NULL -> NULL` donde el dominio permite ausencia de método;
- columnas legacy preservadas sin cambios;
- IDs/cardinalidad preservados.

## 11. CI dedicado N0.5

Se agregó:

- `.github/workflows/erp-n0-5-ci.yml`

Workflow:

- `ERP-N0.5 - Certificación MetodoPago histórico`;
- MySQL 8.4;
- restore/build backend con warnings como error;
- unit tests no integración;
- aplicación exacta de la base relacional;
- prueba negativa fail-closed;
- preflight válido;
- aplicación exacta del backfill;
- postcheck;
- preservación 1:1;
- `dotnet ef migrations has-pending-model-changes`.

Evidencia definitiva:

- Run `31558300465`;
- HEAD certificado: `0b0d18b6fe5cee2380175b0d6175b87274ad157e`;
- conclusión: **success**;
- todos los pasos, incluido `Snapshot EF consistente`, finalizaron correctamente.

## 12. CI general de Desarrollo

El workflow general `Desarrollo - Compilación y pruebas`, run `31558300370`, certificó sobre el mismo HEAD:

- backend Release y pruebas: ✅;
- frontend lint/build producción: ✅;
- higiene del repositorio: ✅;
- Docker/aislamiento: ✅;
- aplicación de migraciones actuales sobre MySQL 8.4: ✅;
- pruebas de integración MySQL: ✅;
- verificación de variante legado, cargas y snapshot: ✅;
- generación de SQL forward: ✅.

## 13. Decisiones de transición que permanecen vigentes

Este punto **no elimina** todavía:

- `Solqaryn.Domain.Enums.MetodoPago`;
- las columnas legacy `MetodoPago`;
- la nulabilidad transicional de `MetodoPagoId`.

Tampoco agrega una FK de método de pago a `Compra`, porque el modelo estructural aprobado en el Punto 4 relacionó `Venta`, `FacturaPago` y `MovimientoFinanciero`. `Compra.MetodoPago` se audita en preflight/postcheck para evitar un quinto significado incompatible mientras continúe el contrato legacy.

La eliminación del enum, el endurecimiento `NOT NULL` y la retirada de columnas legacy solo pueden realizarse después de migrar servicios, DTO/API, frontend, PDF y demás consumidores documentados en el Punto 3.

## 14. Resultado

**Punto 5 — Migración + preflight + backfill histórico: ✅ REALIZADO / CERRADO FORMALMENTE.**

Existe evidencia ejecutable de que los cuatro métodos actuales fueron creados y relacionados por código estable, los `MetodoPagoId` históricos se asignan correctamente, un valor desconocido bloquea la migración antes de escribir y los valores/filas legacy se preservan sin pérdida ni reinterpretación.