# ERP-N1.9 — Series, lotes y vencimientos — Auditoría y preflight

## Estado

**N1.9.A — PREFLIGHT / DISEÑO DE TRANSICIÓN**

Baseline inspeccionado:

```text
c7c4dbf316a3913bd019a754d631a5f64c8a9fd2
```

Este documento es exclusivamente de auditoría y diseño. No crea tablas, no modifica migraciones ni cambia el comportamiento runtime. Su objetivo es congelar la autoridad actual y definir una transición segura antes de implementar N1.9.B–H.

## 1. Objetivo empresarial

ERP-N1.9 debe permitir trazabilidad opcional de inventario por:

- lote;
- número de serie;
- fecha de vencimiento cuando aplique;
- identidad física a través de recepción, reserva, transferencia, venta, conteo, ajuste/reversión y Kardex.

La implementación debe soportar productos sin trazabilidad, productos controlados por lote, productos serializados y productos con lote + vencimiento, sin volver obligatorios esos conceptos para todo el catálogo.

## 2. Hallazgo principal: hoy no existe autoridad de lote/serie/vencimiento

La inspección dirigida del baseline no encontró contratos de dominio ni persistencia para `Lote`, `NumeroSerie` o `FechaVencimiento`.

### 2.1 Producto y variante

`Producto` no contiene política de control de lote/serie/vencimiento. `ProductoVariante` identifica comercialmente una variante por Producto + Marca + Modelo + Color + Talla y mantiene SKU/código de barras, precios, costo y cantidades legacy/compatibilidad. Su configuración EF conserva una identidad activa única basada en esas dimensiones; lote y serie no forman parte de la identidad comercial de una variante.

Conclusión: **un lote o un serial no es una nueva ProductoVariante**. Crear variantes por cada lote/serial fragmentaría el catálogo, rompería SKU/atributos y convertiría metadata logística en identidad comercial.

### 2.2 Autoridad cuantitativa actual

`ExistenciaVariante` es la autoridad viva de stock por:

```text
ProductoVarianteId + AlmacenId + UbicacionAlmacenId
```

La configuración EF aplica unicidad NULL-safe a esa clave y deriva físicamente:

```text
StockDisponible = StockFisico - StockReservado
```

También mantiene `StockTransito` y protege la ubicación para que pertenezca al mismo Almacén.

**Decisión innegociable para N1.9:** `ExistenciaVariante` continúa como autoridad cuantitativa agregada. La trazabilidad por lote/serie será un subledger de identidad que explique/complemente ese stock, no una segunda fuente independiente de cantidad.

## 3. Superficies auditadas

### 3.1 Compras

`CompraDetalle` conoce Producto/Variante, Almacén/Ubicación, cantidad, precio/costo y snapshots comerciales, pero no lote/serial/vencimiento.

`CompraService.ConfirmarAsync` ya opera dentro de `IUnitOfWork`, toma locks de inventario, incrementa cantidades y escribe Kardex con origen tipado Compra. La recepción de identidades N1.9 debe ocurrir dentro de esa misma transacción: no se permite confirmar stock físico y registrar lotes/seriales después en una operación separada.

### 3.2 Ventas

`VentaDetalle` no contiene identidad trazable. `VentaService.ConfirmarAsync` bloquea inventario, deduce cantidad y escribe movimiento de salida dentro de una transacción.

Para variantes con trazabilidad habilitada, la salida debe consumir identidades exactas en la misma transacción que deduce stock. Un serial vendido debe ser inequívoco; para lote/vencimiento debe existir selección explícita o asignación determinística según política.

### 3.3 Reservas

`ReservaInventarioDetalle` reserva cantidad por Variante + Almacén + Ubicación. `ReservaInventarioService` incrementa/decrementa `StockReservado` bajo lock pesimista y audita estrictamente dentro de la misma transacción.

N1.9 debe impedir que el mismo serial o cantidad de un lote quede asignado simultáneamente a reservas incompatibles. Para inventario trazable, la reserva debe poder materializar asignaciones de identidad; el momento exacto de asignación se define en N1.9.B, pero no puede existir una reserva de identidad desconectada de `StockReservado`.

### 3.4 Transferencias

`TransferenciaInventarioDetalle` conoce Variante, ubicaciones origen/destino y cantidades. `TransferenciaInventarioExistenciaService` bloquea el conjunto físico, mueve `StockFisico`/`StockTransito` y revierte tránsito de forma ordenada.

Un lote o serial transferido debe conservar la **misma identidad** durante origen → tránsito → destino. No se debe cerrar una identidad en origen y crear otra equivalente en destino.

### 3.5 Conteos físicos

`ConteoInventarioDetalle` compara stock esperado/contado por clave física y puede generar `AjusteInventario`, pero no captura identidades.

Una variante serializada no puede considerarse reconciliada sólo porque el total coincide: dos seriales diferentes con cantidad total 2 no son equivalentes a otros dos seriales. N1.9 deberá extender el conteo de inventario trazable a identidad, preservando el conteo agregado para variantes sin trazabilidad.

### 3.6 Kardex

`MovimientoInventario` y `KardexMovimientoWriter` registran Producto/Variante/Almacén/Ubicación, cantidad, costos, stock, correlación y orígenes tipados, pero no lote/serial/vencimiento.

N1.9 debe incorporar una relación durable entre movimiento e identidades afectadas. Agregar únicamente strings `Lote`/`Serie` al movimiento sería insuficiente para transferencias, reservas, reversión exacta, múltiples lotes por movimiento y varias series por cantidad.

## 4. Modelo conceptual recomendado para N1.9.B

N1.9.B debe materializar el contrato exacto, pero el preflight fija las responsabilidades para evitar doble autoridad.

### 4.1 Política de trazabilidad en Variante

La política debe ser opcional y preferentemente residir en `ProductoVariante`, porque distintas variantes de un mismo producto pueden requerir controles logísticos diferentes.

Conceptualmente:

```text
ControlLote: bool
ControlSerie: bool
ControlVencimiento: bool
```

Reglas mínimas:

- `ControlVencimiento` requiere identidad apta para vencimiento; normalmente lote.
- `ControlSerie` implica unidades individualizadas 1:1.
- los flags no convierten lote/serie en dimensiones de ProductoVariante.
- cambiar una política con stock/historial existente debe ser fail-closed o exigir reconciliación/cutover explícito.

### 4.2 Identidad de lote

Entidad conceptual `LoteInventario`:

- `ProductoVarianteId`;
- código de lote normalizado;
- fecha fabricación opcional;
- fecha vencimiento opcional según política;
- estado trazable;
- timestamps/auditoría.

La identidad del lote es estable para la variante; su cantidad física no debe vivir como un total libre en esta entidad.

### 4.3 Posición física de lote

Entidad conceptual de posición/asignación física ligada a:

```text
LoteInventario + ExistenciaVariante
```

Mantiene cantidad atribuida a ese lote en la clave física. La suma de posiciones trazadas jamás puede exceder `ExistenciaVariante.StockFisico`.

Cuando una variante haya completado un cutover de trazabilidad total, N1.9 podrá exigir igualdad entre stock físico y cantidad trazada. Esa igualdad **no debe imponerse retroactivamente** a stock histórico no trazado sin reconciliación.

### 4.4 Identidad serial

Entidad conceptual `SerieInventario`:

- `ProductoVarianteId`;
- número de serie normalizado;
- lote opcional si ambos controles aplican;
- estado/lifecycle;
- clave física actual o referencia a posición física;
- vínculo histórico de entrada/salida.

Cada serial representa una unidad. Debe existir unicidad persistente y protegida por MySQL, no sólo validación previa en aplicación.

### 4.5 Relación movimiento-identidad

Se requiere un detalle/subledger entre `MovimientoInventario` e identidad trazable para representar:

- una salida con varios lotes;
- una entrada con múltiples series;
- transferencia de las mismas identidades;
- reversión exacta;
- trazabilidad end-to-end de un serial o lote.

No se permite depender únicamente de texto snapshot para la autoridad de identidad.

## 5. Política de asignación y vencimiento

Para variantes con `ControlVencimiento`, la salida automática debe priorizar FEFO (**First Expired, First Out**) entre identidades elegibles, salvo selección explícita autorizada por el caso de uso.

Reglas:

- FEFO sólo aplica cuando la variante controla vencimiento.
- lotes vencidos deben quedar no elegibles para nuevas salidas normales; cualquier excepción futura debe ser permiso/flujo explícito, no fallback silencioso.
- la fecha de vencimiento es metadata de lote/identidad de recepción, no una propiedad estática del producto.
- no inventar fecha de vencimiento para histórico.

## 6. Transacciones, locks y concurrencia

La trazabilidad N1.9 debe incorporarse a los locks y transacciones ya existentes:

- **Compra:** stock + lote/series + movimiento de entrada, atómicos.
- **Venta:** stock + consumo de identidad + Kardex de salida, atómicos.
- **Reserva:** `StockReservado` + asignación/liberación/consumo de identidad, atómicos.
- **Transferencia:** físico/tránsito + identidad origen/destino + Kardex, atómicos.
- **Conteo/Ajuste:** reconciliación agregada e identidad, atómicas cuando la variante sea trazable.

Los locks deben adquirirse en orden estable para evitar deadlocks. La unicidad de serial debe resolverse también en base de datos para proteger carreras concurrentes.

## 7. Compatibilidad histórica y estrategia de cutover

Existe stock histórico sin identidad trazable. N1.9 **no debe fabricar lotes o seriales ficticios** sólo para conseguir igualdad matemática.

Transición segura:

1. desplegar estructura N1.9 de forma aditiva;
2. mantener variantes actuales sin política de trazabilidad obligatoria;
3. permitir alta de política sólo cuando no exista stock conflictivo o mediante procedimiento de reconciliación explícito;
4. registrar nuevas entradas trazables desde el momento de activación;
5. distinguir cantidad histórica no trazada mientras dure un cutover controlado;
6. una vez reconciliada una variante, aplicar fail-closed: no permitir entradas/salidas que rompan cobertura de identidad requerida.

No se autoriza un backfill heurístico basado en fechas de movimiento, descripciones, SKU u otros snapshots para inventar identidades históricas.

## 8. Rollback / forward-only

La persistencia de identidad es histórica. Después de que un lote/serial sea referenciado por movimientos, reservas, transferencias o conteos:

- no debe eliminarse físicamente;
- rollback seguro debe ser forward-fix o restauración completa compatible;
- desactivar política de trazabilidad requiere que no existan asignaciones abiertas y que la reconciliación preserve históricos;
- una migración `Down` destructiva no es mecanismo operativo aceptable para producción.

N1.9.C deberá documentar preflight y postcheck SQL antes de cualquier migración de datos.

## 9. Impacto previsto por fase

### N1.9.B — Dominio y contratos

Definir flags/política exacta, entidades/value objects, lifecycle de serial, identidad de lote, DTOs y invariantes. Sin DDL improvisado.

### N1.9.C — Persistencia/migración/datos

EF configurations, tablas/FKs/uniqueness/checks/índices, snapshot, preflight/postcheck, cutover histórico y rollback forward-only.

### N1.9.D — Aplicación/servicios/API

Recepción, consulta, asignación, salida, transferencia, reserva, reversión, FEFO y trazabilidad end-to-end bajo locks/transacciones.

### N1.9.E — Frontend/UX

Configuración opcional por variante; captura de lotes/series/vencimiento en entradas; selección/visualización en salidas, reservas, transferencias, conteos y trazabilidad.

### N1.9.F — RBAC/auditoría/seguridad/observabilidad

Auditoría crítica de cambios de identidad/política, permisos, correlación y métricas sin exponer PII/secrets.

### N1.9.G — QA/regresión/CI

Pruebas de concurrencia, MySQL real, lifecycle e integración transversal.

### N1.9.H — Documentación/certificación

Documento canónico, ADR de autoridad/cutover, runbook y ERD finales; checkpoint de gates antes de cierre.

## 10. Matriz mínima de pruebas obligatorias

N1.9.B–G deben congelar como mínimo:

1. variante sin trazabilidad continúa operando sin lote/serie;
2. variante con lote rechaza nueva entrada sin lote;
3. variante serializada exige exactamente una identidad por unidad;
4. serial duplicado concurrente falla cerrado por constraint MySQL;
5. un mismo lote puede existir en varias ubicaciones sin duplicar identidad de lote;
6. suma trazada nunca supera `StockFisico` de su existencia;
7. reserva no puede asignar el mismo serial o cantidad de lote dos veces;
8. consumo/liberación/cancelación de reserva revierte la asignación exacta;
9. transferencia conserva los mismos lotes/seriales en tránsito y destino;
10. cancelación de transferencia restaura identidades exactas;
11. venta consume identidad exacta y Kardex permite rastrearla;
12. FEFO selecciona el vencimiento más próximo sólo cuando aplica;
13. lote vencido es inelegible para salida normal;
14. reversión de venta/compra restaura o retira la identidad exacta según reglas históricas;
15. conteo de variante serializada detecta sustitución de serial aunque la cantidad total coincida;
16. activación de trazabilidad con stock histórico no reconciliado falla cerrado o exige cutover explícito;
17. no se crean identidades ficticias durante migración;
18. idempotencia y reintentos no duplican lote/serial/movimiento;
19. locks concurrentes no permiten stock agregado correcto con identidad inconsistente;
20. historial y rollback forward-only preservan referencias existentes.

## 11. Riesgos P0/P1 del diseño

- **P0 — Doble autoridad de stock:** introducir cantidades independientes de lote que puedan divergir de `ExistenciaVariante`.
- **P0 — Serial duplicado:** confiar sólo en validación de aplicación sin constraint persistente.
- **P0 — Operación no atómica:** actualizar stock y confirmar identidad en transacciones separadas.
- **P0 — Backfill inventado:** fabricar lotes/series para histórico sin evidencia.
- **P1 — Transferencia recrea identidad:** perder continuidad serial/lote entre almacenes.
- **P1 — Reserva sólo cuantitativa para serializados:** permitir doble asignación de la misma unidad.
- **P1 — Kardex sin identidad durable:** imposibilitar trazabilidad y reversión exacta.
- **P1 — FEFO global:** aplicar vencimiento a variantes que no lo controlan.
- **P1 — Cambio de política permisivo:** activar/desactivar trazabilidad sobre stock abierto sin reconciliación.

## 12. Decisiones del preflight

Quedan fijadas para N1.9.B–H:

1. `ExistenciaVariante` sigue siendo autoridad cuantitativa agregada.
2. Lote y serie son identidad logística, no nuevas variantes.
3. Vencimiento pertenece a la identidad/lote, no al Producto estático.
4. La política de trazabilidad es opcional y debe poder diferir por Variante.
5. El subledger debe relacionarse con la clave física y con `MovimientoInventario`.
6. Stock e identidad se mutan en la misma transacción y bajo locks compatibles.
7. Transferencias preservan identidad; no la recrean.
8. Reservas de inventario trazable deben impedir doble asignación de identidad.
9. El histórico sin trazabilidad no se backfillea con datos ficticios.
10. El cutover a trazabilidad estricta es explícito, auditable y fail-closed.
11. MySQL debe proteger unicidad de serial e integridad relacional.
12. N1.9.B debe mantener el changeset pequeño: dominio/contratos primero; persistencia queda en C.

## 13. DoD de N1.9.A

N1.9.A puede marcarse `LISTO` porque:

- se auditó la ausencia actual de lote/serie/vencimiento;
- se confirmó `ExistenciaVariante` como autoridad de stock;
- se revisaron las superficies Producto/Variante, Compras, Ventas, Reservas, Transferencias, Conteos y Kardex;
- se identificaron los límites transaccionales que N1.9 debe reutilizar;
- se definieron transición histórica, riesgos, rollback y matriz mínima de pruebas;
- no se realizó ningún cambio funcional, DDL, migración, infraestructura o producción.

Siguiente paso FINISH_FIRST: **N1.9.B — Dominio y contratos**.
