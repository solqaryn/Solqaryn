# P0-007.A — Inventario reproducible de componentes Angular (DEV)

**Proyecto:** SOLQARYN | **Scope:** `PROJECT_SCOPE_LOCK=STRICT` | **Fuente:** Plan Comercial Inicial Unificado V3 P0-007.A (L15/UI) | **SHA base:** `bafcde53ce2562f70d5b4115184df51e5c48483d` | **Task:** `SECONDARY_REVIEW_01` / `ALEX_CHATGPT` / `6adbbc9ad9e88191bea8eb69104afb25` | **GitHub:** `morales35alex`.

## Método y alcance

Lectura GitHub nativa de `git/trees/bafcde53ce2562f70d5b4115184df51e5c48483d?recursive=1`, con `truncated=false`. Se incluyen blobs bajo `frontend/src/app/features/` o `frontend/src/app/shared/` terminados exactamente en `.component.ts`. Lista ordenada y sin duplicados; es inventario de **archivos fuente**, NO de pantallas navegables ni componentes renderizados.

**Conteo:** 137 archivos (129 features y 8 shared) en 47 áreas feature. Hay 2 `.component.ts` adicionales bajo `app` fuera de features/shared; se detallan sin contarlos. No incluir `.spec.ts`, templates HTML, estilos, servicios ni directivas.

## Pruebas negativas y límites

- **N1 (UX):** que exista `.component.ts` no acredita funcionamiento, navegación, responsividad, accesibilidad, pruebas E2E ni despliegue. Ninguna pantalla se declara certificada por este inventario.
- **N2 (alcance):** `app.component.ts`, `core/**`, `*.component.spec.ts` y `*.component.html` quedan fuera. La extensión requerida es `.component.ts` y las raíces son features/shared.
- **N3 (regresión):** ejecutar `node scripts/verify-p0-007-a-component-inventory.mjs --self-test` en checkout de la rama. El comparador detecta rutas añadidas o eliminadas y mutaciones sintéticas (sin alterar archivos).
- **N4 (seguridad):** una lista de archivos no sustituye JWT, RBAC backend, `authGuard`, `permisoGuard` ni aislamiento tenant. No inferir autorización desde la UI.

El PR modifica únicamente documentación, validador de solo lectura y changelog; sin APIs, datos, migraciones, secrets, despliegues o QA/main/PROD. Rollback: revert protegido del changeset. `LISTO` requiere CI 3/3 exact-HEAD, revisión GitHub independiente, merge dev y readback PLAN_MAESTRO/COLA/BITACORA.

## Resumen por carpeta

| Carpeta | Cantidad |
| --- | ---: |
| `app/features/almacenes` | 2 |
| `app/features/asientos-contables` | 2 |
| `app/features/auditoria` | 1 |
| `app/features/caja` | 1 |
| `app/features/cargas-masivas` | 1 |
| `app/features/catalogos-producto` | 1 |
| `app/features/categorias` | 2 |
| `app/features/centro-reportes` | 1 |
| `app/features/centros-costo` | 1 |
| `app/features/clientes` | 2 |
| `app/features/compras` | 5 |
| `app/features/configuracion` | 9 |
| `app/features/costos-envio` | 1 |
| `app/features/cuentas-bancarias` | 1 |
| `app/features/cuentas-por-cobrar` | 1 |
| `app/features/dashboard` | 1 |
| `app/features/descuentos` | 2 |
| `app/features/estados-financieros` | 1 |
| `app/features/facturas` | 3 |
| `app/features/finanzas` | 3 |
| `app/features/impuestos` | 2 |
| `app/features/inventario` | 17 |
| `app/features/login` | 1 |
| `app/features/metodos-pago` | 1 |
| `app/features/ordenes-compra` | 2 |
| `app/features/perfil` | 1 |
| `app/features/periodos-contables` | 1 |
| `app/features/permisos` | 1 |
| `app/features/plan-cuentas` | 1 |
| `app/features/plataforma` | 2 |
| `app/features/preparaciones-pedido-venta` | 1 |
| `app/features/productos` | 7 |
| `app/features/proveedores` | 2 |
| `app/features/recepciones-compra` | 3 |
| `app/features/rentabilidad` | 5 |
| `app/features/reportes-administrativos` | 1 |
| `app/features/reportes-compras` | 5 |
| `app/features/reportes-inventario` | 5 |
| `app/features/reportes-ventas` | 4 |
| `app/features/roles` | 2 |
| `app/features/solicitudes-compra` | 1 |
| `app/features/storefront` | 11 |
| `app/features/sucursales` | 2 |
| `app/features/tipo-clientes` | 2 |
| `app/features/ubicaciones-almacen` | 2 |
| `app/features/usuarios` | 3 |
| `app/features/ventas` | 3 |
| `app/shared` | 8 |
| **TOTAL** | **137** |

## Manifiesto de rutas (137 archivos)

Cada entrada es una ruta exacta del árbol GitHub; el validador usa estas líneas como manifiesto.

### `app/features/almacenes` (2)

- `frontend/src/app/features/almacenes/almacen-form.component.ts`
- `frontend/src/app/features/almacenes/almacenes-list.component.ts`

### `app/features/asientos-contables` (2)

- `frontend/src/app/features/asientos-contables/asientos-contables.component.ts`
- `frontend/src/app/features/asientos-contables/evento-contable-panel.component.ts`

### `app/features/auditoria` (1)

- `frontend/src/app/features/auditoria/auditoria-list.component.ts`

### `app/features/caja` (1)

- `frontend/src/app/features/caja/caja-flujo-shell.component.ts`

### `app/features/cargas-masivas` (1)

- `frontend/src/app/features/cargas-masivas/cargas-masivas.component.ts`

### `app/features/catalogos-producto` (1)

- `frontend/src/app/features/catalogos-producto/catalogo-producto-list.component.ts`

### `app/features/categorias` (2)

- `frontend/src/app/features/categorias/categoria-form.component.ts`
- `frontend/src/app/features/categorias/categorias-list.component.ts`

### `app/features/centro-reportes` (1)

- `frontend/src/app/features/centro-reportes/centro-reportes.component.ts`

### `app/features/centros-costo` (1)

- `frontend/src/app/features/centros-costo/centros-costo.component.ts`

### `app/features/clientes` (2)

- `frontend/src/app/features/clientes/cliente-form.component.ts`
- `frontend/src/app/features/clientes/clientes-list.component.ts`

### `app/features/compras` (5)

- `frontend/src/app/features/compras/compra-detail.component.ts`
- `frontend/src/app/features/compras/compra-form.component.ts`
- `frontend/src/app/features/compras/compras-list.component.ts`
- `frontend/src/app/features/compras/evaluaciones-proveedor/evaluaciones-proveedor.component.ts`
- `frontend/src/app/features/compras/three-way-match/three-way-match.component.ts`

### `app/features/configuracion` (9)

- `frontend/src/app/features/configuracion/automatizacion-configuracion-card.component.ts`
- `frontend/src/app/features/configuracion/configuracion.component.ts`
- `frontend/src/app/features/configuracion/email-empresarial-card.component.ts`
- `frontend/src/app/features/configuracion/empresa-administracion-card.component.ts`
- `frontend/src/app/features/configuracion/empresa-configuracion-plantillas-tenant-card.component.ts`
- `frontend/src/app/features/configuracion/empresa-configuracion-tenant-card.component.ts`
- `frontend/src/app/features/configuracion/secuencia-documento-card.component.ts`
- `frontend/src/app/features/configuracion/suscripcion-saas-card.component.ts`
- `frontend/src/app/features/configuracion/whatsapp-business-card.component.ts`

### `app/features/costos-envio` (1)

- `frontend/src/app/features/costos-envio/costos-envio.component.ts`

### `app/features/cuentas-bancarias` (1)

- `frontend/src/app/features/cuentas-bancarias/cuentas-bancarias.component.ts`

### `app/features/cuentas-por-cobrar` (1)

- `frontend/src/app/features/cuentas-por-cobrar/cuentas-por-cobrar.component.ts`

### `app/features/dashboard` (1)

- `frontend/src/app/features/dashboard/dashboard.component.ts`

### `app/features/descuentos` (2)

- `frontend/src/app/features/descuentos/descuento-form.component.ts`
- `frontend/src/app/features/descuentos/descuentos-list.component.ts`

### `app/features/estados-financieros` (1)

- `frontend/src/app/features/estados-financieros/estados-financieros.component.ts`

### `app/features/facturas` (3)

- `frontend/src/app/features/facturas/factura-fiscal-emision.component.ts`
- `frontend/src/app/features/facturas/factura-pagos.component.ts`
- `frontend/src/app/features/facturas/factura-view.component.ts`

### `app/features/finanzas` (3)

- `frontend/src/app/features/finanzas/cuentas-por-cobrar/cuentas-por-cobrar.component.ts`
- `frontend/src/app/features/finanzas/cuentas-por-pagar/cuentas-por-pagar.component.ts`
- `frontend/src/app/features/finanzas/finanzas.component.ts`

### `app/features/impuestos` (2)

- `frontend/src/app/features/impuestos/impuesto-form.component.ts`
- `frontend/src/app/features/impuestos/impuestos-list.component.ts`

### `app/features/inventario` (17)

- `frontend/src/app/features/inventario/ajuste-detail.component.ts`
- `frontend/src/app/features/inventario/ajuste-form.component.ts`
- `frontend/src/app/features/inventario/ajustes-list.component.ts`
- `frontend/src/app/features/inventario/conteo-inventario-detail.component.ts`
- `frontend/src/app/features/inventario/conteo-inventario-form.component.ts`
- `frontend/src/app/features/inventario/conteos-inventario-list.component.ts`
- `frontend/src/app/features/inventario/costeo-inventario.component.ts`
- `frontend/src/app/features/inventario/existencia-variante-form.component.ts`
- `frontend/src/app/features/inventario/existencias-variante-list.component.ts`
- `frontend/src/app/features/inventario/movimientos-list.component.ts`
- `frontend/src/app/features/inventario/reserva-inventario-detail.component.ts`
- `frontend/src/app/features/inventario/reserva-inventario-form.component.ts`
- `frontend/src/app/features/inventario/reservas-inventario-list.component.ts`
- `frontend/src/app/features/inventario/transferencia-detail.component.ts`
- `frontend/src/app/features/inventario/transferencia-form.component.ts`
- `frontend/src/app/features/inventario/transferencias-list.component.ts`
- `frontend/src/app/features/inventario/trazabilidad-variante-page.component.ts`

### `app/features/login` (1)

- `frontend/src/app/features/login/login.component.ts`

### `app/features/metodos-pago` (1)

- `frontend/src/app/features/metodos-pago/metodos-pago.component.ts`

### `app/features/ordenes-compra` (2)

- `frontend/src/app/features/ordenes-compra/orden-compra-form.component.ts`
- `frontend/src/app/features/ordenes-compra/ordenes-compra-shell.component.ts`

### `app/features/perfil` (1)

- `frontend/src/app/features/perfil/perfil.component.ts`

### `app/features/periodos-contables` (1)

- `frontend/src/app/features/periodos-contables/periodos-contables.component.ts`

### `app/features/permisos` (1)

- `frontend/src/app/features/permisos/permisos-matrix.component.ts`

### `app/features/plan-cuentas` (1)

- `frontend/src/app/features/plan-cuentas/plan-cuentas.component.ts`

### `app/features/plataforma` (2)

- `frontend/src/app/features/plataforma/plataforma-gestion-mvp.component.ts`
- `frontend/src/app/features/plataforma/plataforma.component.ts`

### `app/features/preparaciones-pedido-venta` (1)

- `frontend/src/app/features/preparaciones-pedido-venta/preparaciones-pedido-venta.component.ts`

### `app/features/productos` (7)

- `frontend/src/app/features/productos/producto-combination-generator.component.ts`
- `frontend/src/app/features/productos/producto-detail.component.ts`
- `frontend/src/app/features/productos/producto-form.component.ts`
- `frontend/src/app/features/productos/producto-variante-trazabilidad.component.ts`
- `frontend/src/app/features/productos/producto-variantes.component.ts`
- `frontend/src/app/features/productos/productos-list.component.ts`
- `frontend/src/app/features/productos/trazabilidad-variante-panel.component.ts`

### `app/features/proveedores` (2)

- `frontend/src/app/features/proveedores/proveedor-form.component.ts`
- `frontend/src/app/features/proveedores/proveedores-list.component.ts`

### `app/features/recepciones-compra` (3)

- `frontend/src/app/features/recepciones-compra/recepcion-compra-detail.component.ts`
- `frontend/src/app/features/recepciones-compra/recepcion-compra-form.component.ts`
- `frontend/src/app/features/recepciones-compra/recepciones-compra-shell.component.ts`

### `app/features/rentabilidad` (5)

- `frontend/src/app/features/rentabilidad/agrupacion/rentabilidad-agrupacion.component.ts`
- `frontend/src/app/features/rentabilidad/disclosure/rentabilidad-disclosure.component.ts`
- `frontend/src/app/features/rentabilidad/filtros/rentabilidad-filtros.component.ts`
- `frontend/src/app/features/rentabilidad/rentabilidad.component.ts`
- `frontend/src/app/features/rentabilidad/tabla/rentabilidad-resultados-tabla/rentabilidad-resultados-tabla.component.ts`

### `app/features/reportes-administrativos` (1)

- `frontend/src/app/features/reportes-administrativos/reportes-administrativos.component.ts`

### `app/features/reportes-compras` (5)

- `frontend/src/app/features/reportes-compras/disclosure/purchase-report-disclosure.component.ts`
- `frontend/src/app/features/reportes-compras/estados/estado-selector.component.ts`
- `frontend/src/app/features/reportes-compras/filtros/reporte-compras-filtros.component.ts`
- `frontend/src/app/features/reportes-compras/reportes-compras.component.ts`
- `frontend/src/app/features/reportes-compras/tabla/reportes-compras-tabla.component.ts`

### `app/features/reportes-inventario` (5)

- `frontend/src/app/features/reportes-inventario/kardex/kardex.component.ts`
- `frontend/src/app/features/reportes-inventario/reconciliacion/reconciliacion.component.ts`
- `frontend/src/app/features/reportes-inventario/shared/components/reporte-inventario-filtro-base.component.ts`
- `frontend/src/app/features/reportes-inventario/stock-health/stock-health.component.ts`
- `frontend/src/app/features/reportes-inventario/valorizacion/valorizacion.component.ts`

### `app/features/reportes-ventas` (4)

- `frontend/src/app/features/reportes-ventas/detalle/reporte-ventas-detalle.component.ts`
- `frontend/src/app/features/reportes-ventas/filtros/reporte-ventas-filtros.component.ts`
- `frontend/src/app/features/reportes-ventas/reportes-ventas.component.ts`
- `frontend/src/app/features/reportes-ventas/resumen/reporte-ventas-resumen.component.ts`

### `app/features/roles` (2)

- `frontend/src/app/features/roles/rol-form.component.ts`
- `frontend/src/app/features/roles/roles-list.component.ts`

### `app/features/solicitudes-compra` (1)

- `frontend/src/app/features/solicitudes-compra/solicitudes-compra-shell.component.ts`

### `app/features/storefront` (11)

- `frontend/src/app/features/storefront/storefront-carrito.component.ts`
- `frontend/src/app/features/storefront/storefront-categoria.component.ts`
- `frontend/src/app/features/storefront/storefront-categorias.component.ts`
- `frontend/src/app/features/storefront/storefront-checkout.component.ts`
- `frontend/src/app/features/storefront/storefront-cuenta.component.ts`
- `frontend/src/app/features/storefront/storefront-header.component.ts`
- `frontend/src/app/features/storefront/storefront-pedido.component.ts`
- `frontend/src/app/features/storefront/storefront-producto.component.ts`
- `frontend/src/app/features/storefront/storefront-productos.component.ts`
- `frontend/src/app/features/storefront/storefront.component.ts`
- `frontend/src/app/features/storefront/tienda-vinculacion.component.ts`

### `app/features/sucursales` (2)

- `frontend/src/app/features/sucursales/sucursal-form.component.ts`
- `frontend/src/app/features/sucursales/sucursales-list.component.ts`

### `app/features/tipo-clientes` (2)

- `frontend/src/app/features/tipo-clientes/tipo-cliente-form.component.ts`
- `frontend/src/app/features/tipo-clientes/tipo-clientes-list.component.ts`

### `app/features/ubicaciones-almacen` (2)

- `frontend/src/app/features/ubicaciones-almacen/ubicacion-almacen-form.component.ts`
- `frontend/src/app/features/ubicaciones-almacen/ubicaciones-almacen-list.component.ts`

### `app/features/usuarios` (3)

- `frontend/src/app/features/usuarios/usuario-detail.component.ts`
- `frontend/src/app/features/usuarios/usuario-form.component.ts`
- `frontend/src/app/features/usuarios/usuarios.component.ts`

### `app/features/ventas` (3)

- `frontend/src/app/features/ventas/venta-detail.component.ts`
- `frontend/src/app/features/ventas/venta-form.component.ts`
- `frontend/src/app/features/ventas/ventas-list.component.ts`

### `app/shared` (8)

- `frontend/src/app/shared/anular-dialog.component.ts`
- `frontend/src/app/shared/codigo-scanner-dialog/codigo-scanner-dialog.component.ts`
- `frontend/src/app/shared/codigo-scanner-input/codigo-scanner-input.component.ts`
- `frontend/src/app/shared/confirm-dialog.component.ts`
- `frontend/src/app/shared/feedback-state/feedback-state.component.ts`
- `frontend/src/app/shared/metodo-pago-select/metodo-pago-select.component.ts`
- `frontend/src/app/shared/navigation/app-navigation-menu.component.ts`
- `frontend/src/app/shared/producto-imagen/producto-imagen.component.ts`

## Otros archivos `.component.ts` fuera del alcance

- `frontend/src/app/app.component.ts`
- `frontend/src/app/core/auth/tenant-selector.component.ts`

## Evidencia y estado

- Árbol GitHub exacto: https://github.com/solqaryn/Solqaryn/tree/bafcde53ce2562f70d5b4115184df51e5c48483d/frontend/src/app
- Fuentes de la enumeración: blobs del árbol Git, `truncated=false`. No se leyó el decorador `@Component` de cada archivo; la clasificación es por ruta/extensión.
- El validador no escribe archivos y la ejecución nativa en checkout requiere verificación independiente antes del cierre.
- P0-007.B debe ejecutar su certificación negativa UX por separado; P0-007.A no concede tal certificación.
