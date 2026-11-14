# OpenAPI — N2.5 Three-Way Match

## Endpoint
`GET /conciliacion/ordenes-compra/{ordenCompraId}/three-way-match`

Evalúa una Orden de Compra frente a recepciones N2.3 y facturas N2.4 vigentes.

### Seguridad
- `[Authorize]` a nivel de controller.
- `[RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]` en la acción.

### Path
- `ordenCompraId`: integer, obligatorio, `> 0` por contrato de dominio/recurso.

### 200 OK
Retorna `ApiResponse<ThreeWayMatchResultDto>` con:
- `ordenCompraId`.
- `estado`: 0 Pendiente, 1 Aprobado, 2 Discrepancia.
- `discrepancias[]` con `ordenCompraDetalleId`, `tipo`, valores numéricos, mensaje y campos de texto cuando aplica.

Tipos de discrepancia:
1. Cantidad.
2. Precio.
3. Descuento.
4. Impuesto.
5. Moneda.

`OrdenCompraDetalleId = 0` identifica discrepancias de cabecera, por ejemplo moneda.

### Errores
- Orden inexistente: `ResourceNotFoundException`, gestionada por el pipeline global de errores.
- Evidencia inestable durante lectura paginada: `BusinessRuleException`; el servicio falla cerrado en vez de devolver una evaluación incompleta.

### Reglas de contrato
- Solo `RecepcionCompra.Recibida` y `FacturaProveedor.Registrada` participan.
- Comparación exacta; no tolerancias ni FX implícitos.
- Endpoint de lectura: N2.5 no introduce por esta acción confirmación, pago, CxP ni mutación transaccional adicional.

## Ejemplo conceptual
```json
{
  "success": true,
  "data": {
    "ordenCompraId": 123,
    "estado": 2,
    "discrepancias": [
      {
        "ordenCompraDetalleId": 0,
        "tipo": 5,
        "mensaje": "Discrepancia de moneda: orden USD / factura HNL.",
        "esperadoTexto": "USD",
        "valorFacturadoTexto": "HNL"
      }
    ]
  }
}
```
