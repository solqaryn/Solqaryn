# RUNBOOK N2.4 — Factura de Proveedor

## 1. Dependencias y pre-requisitos
- Predecesor de migración: `20260819143000_N2_3_RecepcionCompraOrigenKardex`.
- `OrdenCompra` (N2.2) es una dependencia funcional de `FacturaProveedor`.

## 2. Comprobaciones de salud
- **API:** verificar con autenticación válida los endpoints reales bajo `facturas-proveedor`; no asumir prefijos adicionales.
- **Base de datos:**
  - verificar existencia de `FacturasProveedor` y `FacturaProveedorDetalles`;
  - verificar el índice único `UX_FacturasProveedor_Proveedor_NumeroFactura`;
  - verificar que la migración aplicada corresponda a `20260820082500_N2_4_FacturaProveedorPersistencia`.

## 3. Tratamiento de fallos de prueba o conectividad
- Registrar el **mensaje exacto**, run/job y log asociado.
- Un `MySqlException`, timeout, `Access denied` o `Unable to connect` **no demuestra por sí solo una causa raíz**.
- Si no existe evidencia causal suficiente, registrar: **`causa no determinada; recolectar logs`**.
- No atribuir el fallo a ausencia de BD, red, locks, pool, credenciales, configuración ni código de aplicación sin evidencia directa.

## 4. Troubleshooting funcional
**Error de unicidad al registrar una factura**
- Verificar si ya existe la combinación `ProveedorId + NumeroFactura` (`varchar(80)`). El índice `UX_FacturasProveedor_Proveedor_NumeroFactura` sigue aplicando aunque la factura existente esté `Anulada`.

**Anulación rechazada**
- Confirmar que la factura esté en estado `Registrada`. `Borrador` no puede anularse.
- Enviar un `MotivoAnulacion` no vacío.
- La anulación es documental: no revierte stock, Kardex, costeo ni cantidades.

## 5. Escalamiento
- Si el problema no se explica con evidencia directa del dominio, controller, migración o logs causales, detener la clasificación y recopilar evidencia adicional antes de afirmar una causa.
