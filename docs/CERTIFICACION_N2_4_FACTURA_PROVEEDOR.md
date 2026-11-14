# CERTIFICACIÓN ERP-N2.4 — Factura de Proveedor

## 1. Identificación
- **Módulo:** N2.4 Factura de Proveedor.
- **Objetivo:** documentar la factura de proveedor independiente del incremento de stock de N2.3.
- **Estado de este documento:** `READINESS / EVIDENCE`. La promoción de H.1/H/N2.4 corresponde exclusivamente a ChatGPT/VAEP.

## 2. Evidencia canónica verificada

### 2.1 Dominio
- `FacturaProveedor` se relaciona directamente con `ProveedorId` y `OrdenCompraId`.
- `FacturaProveedorDetalle` referencia `OrdenCompraDetalleId`; no existe FK directa `FacturaProveedor -> RecepcionCompra`.
- `NumeroFactura` tiene longitud máxima 80 y la combinación `ProveedorId + NumeroFactura` es única.

### 2.2 Ciclo de vida
- `Borrador -> Registrada`.
- `Registrada -> Anulada`.
- `Borrador -> Anulada` no está permitido.
- La anulación requiere motivo y es documental: no revierte stock, Kardex, costeo ni cantidades.

### 2.3 API y RBAC
Controlador real: `FacturasProveedorController`, ruta base `[Route("facturas-proveedor")]`.
- `GET /facturas-proveedor` — `Compras:Ver`.
- `GET /facturas-proveedor/{id:int}` — `Compras:Ver`.
- `POST /facturas-proveedor` — `Compras:Crear`, body `CreateFacturaProveedorDto`.
- `PUT /facturas-proveedor/{id:int}` — `Compras:Editar`, body `UpdateFacturaProveedorDto`.
- `POST /facturas-proveedor/{id:int}/registrar` — `Compras:Confirmar`, sin body.
- `POST /facturas-proveedor/{id:int}/anular` — `Compras:Anular`, body `AnularFacturaProveedorDto`.
- No existe endpoint `DELETE`.
- Las operaciones exitosas devuelven los envelopes implementados con `FacturaProveedorDto`; el listado usa `PagedResult<FacturaProveedorDto>`.

### 2.4 Persistencia
- Migración N2.4: `20260820082500_N2_4_FacturaProveedorPersistencia`.
- Predecesora inmediata: `20260819143000_N2_3_RecepcionCompraOrigenKardex`.
- `Down()` elimina `FacturaProveedorDetalles` y `FacturasProveedor`; por tanto existe riesgo real de pérdida de datos N2.4.

## 3. Regla de evidencia de pruebas
- Registrar únicamente resultados vinculados a runs/jobs/logs verificables.
- Si aparece `MySqlException`, timeout, `Access denied` o `Unable to connect`, registrar el mensaje exacto.
- Sin evidencia causal suficiente, usar: **`causa no determinada; recolectar logs`**.
- No atribuir la causa a BD ausente, red, locks, pool, credenciales, configuración o código sin evidencia directa.

## 4. Disposición
Este documento no se autocertifica como cierre del parent. ChatGPT/VAEP realiza el review final, rollup y validación de gates antes de promover `N2.4.H.1`, `N2.4.H` o `N2.4`.
