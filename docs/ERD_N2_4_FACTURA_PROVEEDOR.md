# ERD-N2.4 — Entidades de Factura de Proveedor

## Diagrama Lógico de Relaciones

```mermaid
erDiagram
    FacturaProveedor ||--o{ FacturaProveedorDetalle : "contiene"
    Proveedor ||--o{ FacturaProveedor : "emite"
    OrdenCompra ||--o{ FacturaProveedor : "respalda"
    OrdenCompraDetalle ||--o{ FacturaProveedorDetalle : "referencia"
    Producto ||--o{ FacturaProveedorDetalle : "asocia"
    ProductoVariante ||--o{ FacturaProveedorDetalle : "asocia"

    FacturaProveedor {
        int Id PK
        string NumeroFactura "varchar(80)"
        int ProveedorId FK
        int OrdenCompraId FK
        string Moneda "HNL"
        datetime FechaEmisionUtc
        datetime? FechaVencimientoUtc
        int Estado "Borrador(1), Registrada(2), Anulada(3)"
        datetime FechaRegistroUtc
        datetime FechaAnulacionUtc
        string MotivoAnulacion
    }

    FacturaProveedorDetalle {
        int Id PK
        int FacturaProveedorId FK
        int OrdenCompraDetalleId FK
        int ProductoId FK
        int? ProductoVarianteId FK
        decimal CantidadFacturada
        decimal PrecioUnitarioSnapshot
        decimal DescuentoSnapshot
        decimal ImpuestoSnapshot
    }
```

**Notas de Integridad:**
- Índice único en `FacturasProveedor`: `ProveedorId`, `NumeroFactura` (`UX_FacturasProveedor_Proveedor_NumeroFactura`).
- Índice único en `FacturaProveedorDetalles`: `FacturaProveedorId`, `OrdenCompraDetalleId` (`UX_FacturaProveedorDetalles_Factura_OrdenDetalle`).
- La FK de `RecepcionCompra` **no** existe en este esquema de factura.
