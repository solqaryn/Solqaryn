# ERP-N2.4 — Diseño Funcional: Factura de Proveedor

## 1. Visión General
El módulo N2.4 proporciona el registro documental de la factura del proveedor mediante la entidad `FacturaProveedor`, independiente de la recepción física de mercadería. La materialización de CxP/pagos no se documenta como parte de N2.4.

## 2. Hechos Canónicos del Diseño
- **Relaciones Clave:** Se vincula a `ProveedorId` y a `OrdenCompraId`.
- **Desvinculación Física:** Los detalles referencian `OrdenCompraDetalleId`. No hay asociación directa entre la Factura y `RecepcionCompraId`. No se asume una relación 1:N ni N:1 con recepciones.
- **Tamaño de Campo:** `NumeroFactura` es `varchar(80)`.
- **Restricciones de Unicidad:** `UX_FacturasProveedor_Proveedor_NumeroFactura`; no se inventan políticas extra de CxP o pagos.

## 3. Máquina de Estados y Ciclo de Vida
1. **Borrador (1):** Estado inicial; se puede editar mientras permanezca en borrador.
2. **Registrada (2):** Transición unidireccional desde `Borrador`.
3. **Anulada (3):** Transición unidireccional **solo** desde `Registrada`.
   - No se puede anular un borrador.
   - Requiere un motivo de anulación obligatorio.
   - **Impacto de Anulación:** documental; no revierte Kardex, stock, costeo ni cantidades.

## 4. Endpoints y Seguridad (RBAC)
Ruta base: `facturas-proveedor`.
- `GET /`, `GET /{id:int}` — `Compras:Ver`.
- `POST /` — `Compras:Crear`.
- `PUT /{id:int}` — `Compras:Editar`.
- `POST /{id:int}/registrar` — `Compras:Confirmar`.
- `POST /{id:int}/anular` — `Compras:Anular`.
- El controlador actual no expone operación HTTP `DELETE`; la salida del ciclo de vida se modela mediante `Anular` desde `Registrada`.
