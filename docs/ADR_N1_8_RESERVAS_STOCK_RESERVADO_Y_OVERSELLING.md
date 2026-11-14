# ADR — ERP-N1.8 Reservas: StockReservado, identidad física y overselling

- **Estado:** Aceptado
- **Fase:** ERP-N1.8
- **Baseline funcional:** `95baf2763b912e1015a3bdd25a37aca649e34c37`

## Contexto

Solqaryn necesita reservar inventario para pedidos/ventas sin crear una segunda fuente de verdad, sin perder la ubicación física y sin permitir que dos solicitudes concurrentes comprometan la misma disponibilidad.

Antes de N1.8, `ExistenciaVariante` ya era la autoridad de stock por Variante + Almacén + Ubicación. La reserva debía integrarse a ese modelo, no competir con él.

## Decisiones

### D1. `ExistenciaVariante` sigue siendo la única autoridad cuantitativa

`ReservaInventario` no guarda un saldo agregado alternativo. Cada detalle explica un compromiso y la suma efectiva se refleja en `ExistenciaVariante.StockReservado`.

`StockDisponible` se deriva de la autoridad física; una consulta de reservas nunca sustituye ese valor para decidir disponibilidad.

### D2. La identidad de reserva es física completa

Toda línea identifica:

```text
ProductoVarianteId + AlmacenId + UbicacionAlmacenId
```

Se rechaza una identidad basada sólo en Variante porque mezclaría existencias de almacenes/ubicaciones distintas.

### D3. Activar usa lock pesimista y transacción

La activación bloquea las existencias requeridas, valida disponibilidad y ajusta `StockReservado` dentro del mismo flujo transaccional. Esto elimina la ventana de carrera de un patrón read-then-write desprotegido.

### D4. Las salidas terminales retiran el compromiso exactamente una vez

Consumir, liberar, expirar o cancelar una reserva activa retira la cantidad que esa reserva mantiene comprometida. Un reintento sobre un estado ya materializado no duplica el decremento.

### D5. Reservar no mueve `StockFisico`

`StockFisico` sólo cambia por operaciones físicas autorizadas de inventario/venta/transferencia/ajuste. Reservar o liberar únicamente modifica `StockReservado`.

### D6. Auditoría crítica dentro de la misma unidad de trabajo

Las mutaciones de Reserva usan auditoría estricta. La evidencia se persiste dentro de la misma `IUnitOfWork` de la mutación de estado/stock reservado. Si la auditoría falla, la operación falla cerrado y no debe confirmarse la transacción de negocio.

Esto evita el estado inválido “operación exitosa sin auditoría”.

### D7. RBAC relacional y correlación saneada

Cada endpoint exige autenticación y permiso exacto `MovimientosInventario:*`. No se permite `AllowAnonymous`. La correlación de auditoría utiliza el identificador saneado por middleware (`TraceIdentifier`), no datos de correlación del cliente sin validar.

### D8. El frontend no es autoridad

El frontend muestra físico/reservado/disponible, restringe acciones y valida expiración para UX, pero el backend vuelve a comprobar permisos, estado, fecha, existencia y disponibilidad.

## Alternativas rechazadas

### A. Mantener una tabla/saldo paralelo de stock reservado

Rechazada porque introduce doble autoridad, drift y reconciliación permanente entre Reserva y ExistenciaVariante.

### B. Reservar por Variante sin Almacén/Ubicación

Rechazada porque rompe la trazabilidad física y puede comprometer stock de un lugar distinto al que realmente abastece el pedido.

### C. Consultar disponibilidad y actualizar después sin lock

Rechazada por race condition y overselling bajo concurrencia.

### D. Auditar después del commit y tolerar el fallo

Rechazada en N1.8.F porque una reserva/activación/liberación puede ser financieramente y operativamente sensible. La evidencia crítica forma parte del éxito de la transacción.

### E. Confiar en validación de Angular

Rechazada porque clientes alternos o peticiones concurrentes pueden omitirla.

## Consecuencias

### Positivas

- una sola fuente de verdad para stock;
- explicación trazable del reservado;
- overselling prevenido en la capa autoritativa;
- soporte natural de múltiples almacenes/ubicaciones;
- reintentos de lifecycle sin doble mutación;
- auditoría fuerte y correlacionable.

### Costes

- las transiciones requieren locks y orden determinista de claves físicas;
- un fallo del subsistema de auditoría bloquea mutaciones críticas, por diseño;
- diagnósticos deben revisar documento y existencia física conjuntamente.

## Verificación

El baseline `95baf276...` fue validado por Development `#32035509947`, Acceptance `#32035509805`, Fase8 `#32035509973`, M10 `#32035509930` y M13 `#32035509964`, todos `SUCCESS`.

## Regla de evolución

Cualquier futura optimización que elimine locks, cambie la autoridad de `StockReservado`, reduzca la clave física o vuelva tolerante la auditoría de mutaciones críticas requiere un ADR sustituto y nueva certificación integral.
