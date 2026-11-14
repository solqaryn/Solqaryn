# OpenAPI / Swagger Specification N2.4

```yaml
openapi: 3.0.1
info:
  title: Solqaryn ERP API - Facturas de Proveedor
  version: v1
paths:
  /facturas-proveedor:
    get:
      summary: Lista facturas de proveedor con paginación y filtros.
      tags: [FacturasProveedor]
      security: [{ bearerAuth: [] }]
      responses:
        '200': { description: Operación exitosa }
    post:
      summary: Crea un borrador de factura de proveedor.
      tags: [FacturasProveedor]
      security: [{ bearerAuth: [] }]
      responses:
        '201': { description: Factura creada exitosamente }
  /facturas-proveedor/{id}:
    get:
      summary: Obtiene una factura de proveedor por ID.
      tags: [FacturasProveedor]
      parameters:
        - { name: id, in: path, required: true, schema: { type: integer } }
      security: [{ bearerAuth: [] }]
      responses:
        '200': { description: Factura encontrada }
        '404': { description: Factura no encontrada }
    put:
      summary: Actualiza un borrador de factura de proveedor.
      tags: [FacturasProveedor]
      parameters:
        - { name: id, in: path, required: true, schema: { type: integer } }
      security: [{ bearerAuth: [] }]
      responses:
        '200': { description: Factura actualizada }
  /facturas-proveedor/{id}/registrar:
    post:
      summary: Registra una factura de proveedor en borrador.
      tags: [FacturasProveedor]
      parameters:
        - { name: id, in: path, required: true, schema: { type: integer } }
      security: [{ bearerAuth: [] }]
      responses:
        '200': { description: Factura registrada }
  /facturas-proveedor/{id}/anular:
    post:
      summary: Anula una factura de proveedor previamente registrada.
      tags: [FacturasProveedor]
      parameters:
        - { name: id, in: path, required: true, schema: { type: integer } }
      security: [{ bearerAuth: [] }]
      responses:
        '200': { description: Factura anulada }
```

## Contrato verificado contra `FacturasProveedorController`
- Ruta base real: `facturas-proveedor` (sin prefijo `/api` declarado por el controller).
- `GET /facturas-proveedor` devuelve `ApiResponse<PagedResult<FacturaProveedorDto>>`.
- `GET /{id}`, `POST`, `PUT`, `POST /registrar` y `POST /anular` devuelven `ApiResponse<FacturaProveedorDto>` en éxito.
- DTOs de entrada verificados: `FacturaProveedorFiltroDto`, `CreateFacturaProveedorDto`, `UpdateFacturaProveedorDto`, `AnularFacturaProveedorDto`; `registrar` no recibe body.
- Permisos: `Compras:Ver`, `Compras:Crear`, `Compras:Editar`, `Compras:Confirmar`, `Compras:Anular` según operación.
- No existe endpoint `DELETE`.
