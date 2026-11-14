# ERD — ERP-N1.8 Reservas de inventario

## Objetivo

Representar las relaciones persistentes y la relación lógica de autoridad física usadas por Reservas.

```mermaid
erDiagram
    VENTA o|--o{ RESERVA_INVENTARIO : "pedido/venta opcional"
    RESERVA_INVENTARIO ||--|{ RESERVA_INVENTARIO_DETALLE : contiene
    PRODUCTO_VARIANTE ||--o{ RESERVA_INVENTARIO_DETALLE : reserva
    ALMACEN ||--o{ RESERVA_INVENTARIO_DETALLE : abastece
    UBICACION_ALMACEN o|--o{ RESERVA_INVENTARIO_DETALLE : localiza

    PRODUCTO_VARIANTE ||--o{ EXISTENCIA_VARIANTE : posee
    ALMACEN ||--o{ EXISTENCIA_VARIANTE : mantiene
    UBICACION_ALMACEN o|--o{ EXISTENCIA_VARIANTE : localiza

    VENTA {
        int Id PK
    }

    RESERVA_INVENTARIO {
        int Id PK
        string Numero UK
        int VentaId FK_NULL
        string Estado
        datetime FechaExpiracion NULL
        datetime FechaCreacion
        datetime FechaActivacion NULL
        datetime FechaConsumo NULL
        datetime FechaLiberacion NULL
        datetime FechaExpiracionAplicada NULL
        datetime FechaCancelacion NULL
        int CreadoPorUsuarioId
        int ActualizadoPorUsuarioId
    }

    RESERVA_INVENTARIO_DETALLE {
        int Id PK
        int ReservaInventarioId FK
        int ProductoVarianteId FK
        int AlmacenId FK
        int UbicacionAlmacenId FK_NULL
        int CantidadReservada
        int CantidadConsumida
        string ProductoSkuSnapshot
        string ProductoMarcaSnapshot NULL
        string ProductoModeloSnapshot NULL
        string ProductoColorSnapshot NULL
        string ProductoTallaSnapshot NULL
    }

    EXISTENCIA_VARIANTE {
        int Id PK
        int ProductoVarianteId FK
        int AlmacenId FK
        int UbicacionAlmacenId FK_NULL
        int StockFisico
        int StockReservado
        int StockTransito
        int StockMinimo
        int StockMaximo NULL
    }

    PRODUCTO_VARIANTE {
        int Id PK
        int ProductoId FK
        string Sku
    }

    ALMACEN {
        int Id PK
        int SucursalId FK
    }

    UBICACION_ALMACEN {
        int Id PK
        int AlmacenId FK
        int PadreId FK_NULL
    }
```

## Clave física

Tanto el detalle de Reserva como la existencia se interpretan con la misma clave operacional:

```text
ProductoVarianteId + AlmacenId + UbicacionAlmacenId
```

No es obligatorio que exista una FK directa `ReservaInventarioDetalle -> ExistenciaVariante`: la relación operativa se resuelve por la clave física. Esto evita convertir el ID técnico de una fila de existencia en una identidad de negocio distinta a Variante/Almacén/Ubicación.

## Autoridad

`RESERVA_INVENTARIO_DETALLE.CantidadReservada` explica el compromiso del documento.

`EXISTENCIA_VARIANTE.StockReservado` es el saldo autoritativo para decisiones de disponibilidad y concurrencia.

Por tanto:

```text
Reserva explica ≠ Reserva manda el saldo
ExistenciaVariante manda el saldo
```

## Integridad esperada

- número de reserva único;
- al menos un detalle por documento válido;
- cantidades reservadas positivas;
- consumido no negativo y no superior al reservado;
- no repetir la misma clave física dentro del documento;
- Variante/Almacén/Ubicación deben existir;
- si Ubicación no es nula, debe pertenecer al Almacén indicado;
- cambios de `StockReservado` sólo mediante servicios autoritativos con concurrencia/transacción.

## Lifecycle y auditoría

Los timestamps/actores de lifecycle están en la cabecera; la evidencia transversal se registra además en el subsistema de Auditoría. Desde N1.8.F la auditoría crítica se persiste dentro de la misma unidad de trabajo de la mutación, por lo que un fallo de auditoría debe impedir el commit de la transición.

## Fuera de alcance

Lotes, series, IMEI y vencimientos por lote no forman parte de este ERD N1.8.
