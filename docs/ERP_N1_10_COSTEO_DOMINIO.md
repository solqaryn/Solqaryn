# ERP-N1.10.B — Costeo empresarial — Dominio y contratos

## Estado

Documento de contrato del punto B. No contiene DDL, mapping EF, migraciones, DI runtime ni cambios de workflow documental; esos elementos pertenecen a N1.10.C/D.

Baseline funcional candidato: `b9e68ccb847f8aa94ffe6d7796a22026fb3e54b7`.

## Autoridades congeladas

- `ExistenciaVariante` continúa como autoridad exclusiva de cantidad física.
- `ProductoVariante` es la unidad mínima de valoración.
- `Producto.Costo` se conserva como proyección consolidada/compatibilidad.
- El costo histórico de una salida se representa mediante asignaciones persistibles ligadas a un `MovimientoInventario`; no se recalcula desde el costo corriente al consultar.
- La política de costeo se modela como una versión temporal para el `EmpresaConfiguracion` activo del contexto single-company. ERP-N6 podrá tenantizar el ámbito sin reinterpretar historia.

## Política

`MetodoCosteoInventario` congela tres métodos estables:

1. `PromedioPonderado` — default de compatibilidad.
2. `FIFO` — requiere capas contables durables.
3. `Estandar` — requiere costo estándar temporal y registro de variaciones.

`PoliticaCosteoInventario` representa el intervalo de vigencia y exige UTC, motivo y configuración empresarial válida. Cerrar una versión es irreversible a nivel de la entidad: un cambio posterior crea una nueva versión.

## Promedio Ponderado

`CosteoPromedioPonderado` extrae como regla pura el algoritmo histórico de `CompraService`:

`((costoAnterior * stockAnterior) + valorEntrada) / (stockAnterior + cantidadEntrada)`

con redondeo a dos decimales `MidpointRounding.AwayFromZero`. Cantidades/costos inválidos fallan cerrado.

La extracción permite que N1.10.D mueva la autoridad fuera de `CompraService`/`AppDbContext` sin cambiar el resultado contable certificado.

## FIFO

`CapaCostoInventario` es deliberadamente independiente de `LoteInventario`: lote/serie son identidad logística; una capa es identidad contable de valoración.

Una capa posee:

- Variante.
- Almacén y Ubicación opcional.
- cantidad original y restante.
- costo unitario.
- fecha de origen UTC.
- correlation ID.
- movimiento origen cuando la capa nace de una operación real.
- capa predecesora opcional para conservar linaje de transferencias.

### Cutover

El stock preexistente puede materializarse mediante `CrearApertura`, con motivo explícito y **sin inventar un `MovimientoInventario` histórico**. Esa capa queda distinguida por `EsApertura` y `MotivoApertura`.

### Consumo/reversión

`Consumir` no permite sobreconsumo. `Restaurar` no puede exceder la cantidad original. N1.10.C deberá reforzar las mismas invariantes en MySQL y N1.10.D deberá ejecutarlas bajo lock transaccional.

## Costo estándar

`CostoEstandarInventario` es una versión temporal por Variante. Conserva el estándar vigente y calcula la variación como:

`(CostoRealUnitario - CostoEstandarUnitario) * Cantidad`

El costo real de adquisición nunca se descarta. `VariacionCostoEstandarInventario` congela movimiento, variante, versión estándar, costos real/estándar, cantidad, variación firmada y correlation.

## Asignaciones históricas

`AsignacionCostoMovimientoInventario` congela el costo de un movimiento confirmado:

- movimiento.
- variante.
- método.
- cantidad.
- costo unitario.
- capa opcional.
- correlation.

FIFO exige `CapaCostoInventarioId`; Promedio/Estándar pueden persistir una asignación sin capa. El costo total se deriva de cantidad × costo unitario.

`ResultadoCosteoInventario` valida que la suma de cantidades de sus asignaciones coincida exactamente con la cantidad valorada. FIFO falla cerrado si una asignación carece de capa.

## Boundary de aplicación

`ICosteoInventarioService` congela las operaciones:

- consultar método activo;
- registrar entrada;
- valorar salida;
- revertir usando referencia al movimiento original.

Las requests de entrada/salida/reversión contienen identidad física, cantidad, movimiento, fecha UTC y correlation según corresponda. La reversión no recibe un costo nuevo: debe reconstruirse desde evidencia original.

`ICosteoInventarioRepository` congela el contrato transaccional:

- política vigente con lectura opcional `FOR UPDATE`;
- costo estándar vigente con lock;
- capas FIFO disponibles `FOR UPDATE`;
- capa individual `FOR UPDATE`;
- asignaciones por movimiento;
- persistencia de política, capas, estándar, asignaciones y variaciones.

## Invariantes que N1.10.C debe materializar

- una política vigente por ámbito empresarial.
- un costo estándar vigente por Variante cuando aplique.
- cantidad restante de capa entre 0 y cantidad original.
- ubicación perteneciente al mismo Almacén.
- capa predecesora y asignación FIFO pertenecientes a la misma Variante.
- asignación FIFO exige capa.
- costos no negativos.
- correlation no vacío.
- FKs a Movimiento/Variante/Almacén/Ubicación/EmpresaConfiguracion según contrato.

## Integración posterior

N1.10.D deberá:

- sustituir la fórmula embebida de Compra por el boundary de costeo sin cambiar Promedio actual.
- preservar la reversión certificada existente y sacar la política contable del DbContext.
- congelar COGS de Venta al confirmar, no al editar el borrador.
- trasladar valor/capas en Transferencias sin utilidad.
- costear Ajustes y diferencias de Conteo según política.
- dejar Reservas sin reconocimiento de costo hasta salida física.

## Criterio de cierre de B

B queda certificable cuando el baseline funcional compila y pasa pruebas causales, y ninguna de las entidades/contratos anteriores requiere DDL para existir en el ensamblado de dominio. Sólo después debe abrirse N1.10.C.