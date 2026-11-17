# P0-007.A — Inventario reproducible de componentes Angular (DEV)

**Proyecto:** SOLQARYN | **Scope:** `PROJECT_SCOPE_LOCK=STRICT` | **Fuente:** Plan Comercial Inicial Unificado V3 P0-007.A (L15/UI) | **SHA base:** `bafcde53ce2562f70d5b4115184df51e5c48483d` | **Task:** `SECONDARY_REVIEW_01` / `ALEX_CHATGPT` / `6adbbc9ad9e88191bea8eb69104afb25` | **GitHub:** `morales35alex`.

## Método y alcance

Lectura GitHub nativa de `git/trees/bafcde53ce2562f70d5b4115184df51e5c48483d?recursive=1`, con `truncated=false`. Se incluyen blobs bajo `frontend/src/app/features/` o `frontend/src/app/shared/` terminados exactamente en `.component.ts`. Lista ordenada y sin duplicados; es inventario de **archivos fuente**, NO de pantallas navegables ni componentes renderizados.

**Conteo:** 137 archivos (129 features y 8 shared) en 47 áreas feature. Hay 2 `.component.ts` adicionales bajo `app` fuera de features/shared; se detallan sin contarlos. No incluir `.spec.ts`, templates HTML, estilos, servicios ni directivas.

## Pruebas negativas y límites

- **N1 (UX):** que exista `.component.ts` no acredita funcionamiento, navegación, responsividad, accesibilidad, pruebas E2E ni despliegue. Ninguna pantalla se declara certificada por este inventario.
- **N2 (alcance):** `app.component.ts`, `core/**`, `*.component.spec.ts` y `*.component.html` quedan fuera. La extensión requerida es `.component.ts` y las raíces son features/shared.
- **N3 (regresión):** ejecutar `node scripts/verify-p0-007-a-component-catalog.mjs --self-test` en checkout de la rama. El comparador detecta rutas añadidas o eliminadas y mutaciones sintéticas (sin alterar archivos).
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

## Actualización aditiva V5 FINAL — 13 de noviembre de 2026
El inventario histórico P0-007.A de 137 rutas permanece como evidencia del SHA base. El commit comercial C00-01.M04.01 incorpora una ruta pública adicional con componente standalone en `app/features/shared`, por lo que el catálogo **vigente** de archivos `.component.ts` en `features/shared` pasa a **138 (130 features y 8 shared)**; hay **48 carpetas feature**. Esto es mantenimiento de alcance del catálogo existente, no ejecución de una microtarea V3, ni certificación UX/RBAC. La revisión debe detectar tanto omisiones como archivos inexistentes.

- `frontend/src/app/features/shared/not-found.component.ts`

Validación: `node scripts/verify-p0-007-a-component-catalog.mjs --self-test` (casos negativos de entradas faltantes y no documentadas) y verificación Scope Lock sobre el SHA exacto. Las evidencias UI/Playwright pertenecen exclusivamente a V5 M04.01; rollback por revert no destructivo.

## Actualización aditiva V5 C01-01.M04.01 — 15 de noviembre de 2026

La nueva superficie informativa de contexto confirmado en la vista privada /usuarios añade un componente independiente. El catálogo vigente pasa a **139 archivos**: **131 en features** y **8 en shared**. Los recuentos históricos de los SHA anteriores permanecen como evidencia y no se reescriben. El detector sigue comparando por ruta/extensión y falla ante archivos nuevos no inventariados o archivos borrados.

- `frontend/src/app/features/usuarios/identidad-sesion-verificada.component.ts`

Esta entrada no concede permisos ni certifica UX/tenant, HTTP, accesibilidad E2E o despliegue. La revisión funcional/negativa pertenece a C01-01.M04.01; rollback normal de Git.

## Actualización aditiva V5 C01-01.M04.02 — 15 de noviembre de 2026

La nueva superficie de estados accesibles de usuarios agrega una ruta al inventario reproducible: **140 archivos** (132 feature y 8 shared). Las cifras de los cortes previos se preservan como evidencia histórica. El validador mantiene fallos positivos ante rutas faltantes o nuevas sin registro; no se relaja Scope Lock, JWT, RBAC ni tenancy.

- `frontend/src/app/features/usuarios/usuarios-estado-listado.component.ts`

Este registro no equivale a certificar interacción real, accesibilidad, E2E ni despliegue; la prueba UI corresponde a C01-01.M04.02 y exige GitHub-hosted CI exact SHA y revisión A5 independiente.

## Actualización aditiva V5 C00-04.M04.01 — 15 de noviembre de 2026

La nueva superficie protegida de consulta para gobierno de cambios agrega un archivo `.component.ts` real de Angular al inventario vigente: **141 archivos (133 features y 8 shared)**. El inventario histórico de 137, y los cortes aditivos de 138/139/140, permanecen intactos como evidencia de sus SHAs originales.

- `frontend/src/app/features/plataforma/gobierno-cambios-v5.component.ts`

Se actualizan únicamente los recuentos exactos del verificador P0-007.A y se conserva la detección fail-closed de altas no inventariadas y archivos desaparecidos. El guard servidor sigue siendo `plataformaGuard`; registrar un componente no concede JWT, RBAC, acceso tenant, autorización de cambios, QA/main/PROD ni certifica UX o Playwright. Evidencia de CI dirigida para C00-04.M04.01 y reversión mediante `git revert` normal, sin reescribir la lista histórica.

## Actualización aditiva V17 C15-05.M04.01 — 15 de noviembre de 2026

La pantalla privada de auditoría operativa añade un componente real a la carpeta plataforma. Total verificable actual: **142 archivos (134 features y 8 shared)**. Los recuentos de SHAs históricos permanecen documentados sin reescritura.

- `frontend/src/app/features/plataforma/auditoria-operativa-v5.component.ts`

El verificador conserva comparaciones estrictas de paths reales, duplicados, faltantes y no catalogados. El alta NO certifica JWT, consultas institucionales, datos vivos, acciones de CLAIM/A5, backend ni UX/Playwright; esas pruebas se acreditan por jobs propios. Rollback: revert normal de este ajuste junto al cambio que introdujo el componente.

## Actualización aditiva V17 C10-04.M04.01 — 15 de noviembre de 2026

La nueva superficie privada e informativa de continuidad operativa agrega un componente Angular a features/plataforma. El inventario reproducible vigente es **143 archivos (135 features y 8 shared)**. Los recuentos originales 137 y cortes 138–142 se preservan como evidencia histórica de sus SHAs.

- `frontend/src/app/features/plataforma/continuidad-operativa-v5.component.ts`

El detector fail-closed mantiene la comparación exacta con rutas reales y escenarios negativos de archivos faltantes y no inventariados; ningún componente otorga restauración, OAuth/JWT, RBAC, tenant, datos vivos, QA, main ni PROD. CI dirigida y readback pertenecen a C10-04.M04.01. Rollback: revert normal.

## Actualización aditiva V17: auditoría tenancy y placement privado — 15 de noviembre de 2026

El inventario reproducible de componentes Angular en `dev` incorpora dos superficies `plataforma` autenticadas adicionales, con **145 archivos (137 features y 8 shared)**. Los cortes de 137–143 documentados arriba se preservan como evidencia histórica de los SHA correspondientes; no se reescriben ni se declaran operativos actuales.

- `frontend/src/app/features/plataforma/auditoria-tenancy-v5.component.ts`
- `frontend/src/app/features/plataforma/placement-review-v5.component.ts`

La verificación P0-007.A compara estrictamente los archivos reales y la lista exacta; conserva pruebas negativas de archivos omitidos y ficticios. La auditoría ERP admite exclusivamente las rutas privadas exactas `plataforma/auditoria-tenancy-v5` y `plataforma/placement-v5` bajo `canActivate: [plataformaGuard]` y la comprobación de sesión administrativa respaldada por servidor. No se amplían comodines, prefijos genéricos ni acceso público. Esta reparación de baseline CI no certifica identidad JWT en producción, consultas reales a tenants, DB, pagos, pentest, QA/main/PROD ni pruebas pendientes.

## Actualización aditiva V17 — tres superficies privadas verificadas el 15 de noviembre de 2026

El catálogo reproducible en `dev` contiene **148 archivos `.component.ts`**: **140 en features y 8 en shared**. Se conservan íntegros los cortes históricos anteriores y sus SHAs. Estas tres superficies privadas ya existen en el árbol y solo se incorporan al manifiesto:

- `frontend/src/app/features/plataforma/observabilidad-privada-v5.component.ts`
- `frontend/src/app/features/plataforma/respuesta-incidentes-v5.component.ts`
- `frontend/src/app/features/plataforma/revision-amenazas-v5.component.ts`

La validación mantiene rechazos estrictos de rutas omitidas, entradas inexistentes y duplicados. Registrar un archivo no certifica accesibilidad, integración HTTP/MySQL, JWT/RBAC/tenant, restauraciones ni autorización de QA/main/PROD. Es una reparación del baseline CI, no un nuevo CLAIM ni A5 del código de otra Task. Reversión mediante `git revert` normal.

## Actualización aditiva V17 C05-01.M04.01 — 16 de noviembre de 2026

La nueva biblioteca visual privada de plataforma incorpora un componente Angular standalone real al catálogo reproducible: **149 archivos `.component.ts` (141 features y 8 shared)**. Se preservan íntegramente los cortes históricos y SHAs anteriores.

- `frontend/src/app/features/plataforma/design-system-v5.component.ts`

El validador mantiene comparaciones estrictas de archivos presentes y documentados, duplicados, ausentes y altas no inventariadas, además de negativos del self-test. El check P0-007.A falló en el SHA `0509d36948f22ce8cee548554acd08fb4efbcda4` por esta única ruta nueva; corregir el baseline no concede JWT/RBAC, permisos, tenant, DB, QA, main ni PROD, ni constituye certificación de UI. Se requiere CI exact-SHA y A5 independiente. Rollback mediante `git revert` normal.


## Actualización aditiva V17 C02-04.M04.01 — 16 de noviembre de 2026

El registro reproducible incorpora la nueva consola global privada y conserva los 149 archivos históricos ya documentados. El árbol actual contiene **150 archivos `.component.ts` (142 features y 8 shared)**, todos con ruta exacta y sin duplicados:

- `frontend/src/app/features/plataforma/consola-global-auditada-v5.component.ts`

La detección negativa de rutas faltantes, nuevas sin inventariar y duplicadas permanece exigible; registrar esta pantalla no aprueba seguridad de backend, grants, JWT HTTP, MFA, tenant A/B ni producción. El único acceso permitido conserva el mismo `plataformaGuard` con validación servidor; revisar CI exact SHA, pruebas Angular y navegador sintético. Rollback mediante `git revert` normal.

## Actualización aditiva V17 C15-01.M04.01 — 16 de noviembre de 2026

Nueva superficie privada de trazabilidad registrada en el catálogo exacto: **151 archivos `.component.ts` (143 features y 8 shared)**. Se conservan sin modificar los conteos históricos anteriores y el control negativo de rutas omitidas, ficticias o duplicadas.

- `frontend/src/app/features/plataforma/cobertura-trazabilidad-v5.component.ts`

La ruta `plataforma/trazabilidad-cobertura-v5` se incorpora explícitamente al allowlist cerrado de rutas del control-plane y mantiene `canActivate: [plataformaGuard]` con sesión verificada por servidor. No se añaden rutas públicas, comodines ni bypass del validador de seguridad. Inventariar este componente no acredita comparación global del plan, JWT real, tenancy HTTP, QA/main/PROD ni permisos de despliegue. Cambio causal para reparar P0-007.A y calidad ERP del SHA 07169caa. Rollback normal, CI exact-SHA y A5 independiente.

## Reparación causal V17 — branding privado de pedidos y preparación DEV (16 de noviembre de 2026)

El check exact-SHA P0-007.A del commit `022c5aa7c2217d51c63945aa47bc7d6c0341f718`
falló con `unlisted=frontend/src/app/features/pedidos-venta/c07-04-m04-03-branding.component.ts`.
La siguiente entrega `f4b9a2bf30a1cc90ee19fc9a0677d98841dcf803` agregó
una segunda ruta física que requiere la misma actualización del inventario.
Ahora se documentan **153 archivos** (145 features y 8 shared), al agregar
exactamente los dos archivos existentes de la rama DEV:

- `frontend/src/app/features/pedidos-venta/c07-04-m04-03-branding.component.ts`
- `frontend/src/app/features/plataforma/c14-04-m04-03-release-branding.component.ts`

Se preserva el comparador por conjuntos sin filtros ni excepciones; los tests negativos
para rutas ausentes y nuevas no registradas, duplicados y scope siguen obligatorios.
Este parche de baseline no prueba Vitest, Chromium, JWT/RBAC, tenancy HTTP o
release real, no altera autoridades ni aprobaciones, y exige CI exact-SHA y A5
independiente de las microtareas correspondientes. Rollback `git revert` normal.

## Actualización causal V18 C08-04.M04.01 — 16 de noviembre de 2026

El control de inventario P0-007.A detectó en el SHA `69c136cbe36af14466f0dec00604d038f4bd5336` un nuevo componente que no estaba documentado. Se incorpora **exactamente una ruta existente** al inventario, ahora **154 archivos `.component.ts` (146 features y 8 shared)**, sin alterar las entradas y recuentos de cortes históricos:

- `frontend/src/app/features/extensiones-sectoriales/sector-modules-readonly.component.ts`

Se conservan comparaciones estrictas de conjuntos, conteo exacto y negativos de ausencia, duplicados y rutas fuera del alcance; no hay exclusiones ni bypass. La nueva superficie es Angular privada, estática y no operativa. El inventario no certifica JWT, RBAC, tenant, API, MySQL ni aceptación de navegador. Exigir `node scripts/verify-p0-007-a-component-catalog.mjs --self-test` real en CI del nuevo SHA y A0–A4 aplicables; A5 requiere revisor Javier distinto del autor. Sin QA/main/PROD; rollback Git revert normal.

## Actualización aditiva V18 C13-04.M04.03 — 16 de noviembre de 2026

La nueva referencia visual privada fue publicada en dev y requiere inventario. Se incorpora exactamente un archivo fuente real, total actual **155 archivos .component.ts (147 features y 8 shared)**, preservando todos los inventarios y SHAs históricos anteriores:

- `frontend/src/app/features/plataforma/c13-04-presentacion-v5.component.ts`

El self-test estricto de P0-007.A mantiene comparaciones exactas de rutas presentes, omitidas y duplicadas, y rechaza altas no inventariadas. Registrar el archivo NO certifica accesibilidad, JWT/RBAC/tenant real, HTTP, MySQL ni QA/main/PROD. Esta reparación no cambia el conjunto de rutas protegidas ni excluye ninguna verificación. Revalidar CI exact-SHA. Rollback normal git revert, sin force-push.

## Actualización causal V19 C15-04.M04.01 — 16 de noviembre de 2026

El nuevo componente de auditoría UX real provoca un fallo de inventario P0-007.A en SHA 0f2affce; se incorpora exactamente un archivo a la lista cerrada, pasando a **156 archivos .component.ts (148 features y 8 shared)** sin suprimir ningún origen anterior:

- `frontend/src/app/features/plataforma/c15-04-auditoria-ux-v19.component.ts`

La comprobación exige igualdad estricta por conjunto de todos los archivos reales y del inventario, cinco escenarios negativos del contrato existente y no certifica UX real, WCAG, JWT/RBAC/tenant HTTP, métricas, ni QA/PROD. La ruta dispone de plataformaGuard y de la allowlist exacta. Revalidar CI de SHA reparado. A5 distinta Task. Reversión git revert normal.

## Actualización causal V19 C12-04.M04.01 — 17 de noviembre de 2026

La implementación de la superficie privada de preparación MySQL añadió exactamente un componente Angular real. La comprobación P0-007.A del SHA `24a4ecc4a108aa4f425d04c41cd06616eb806e43` detectó correctamente su ausencia del inventario. Se incorpora la entrada sin retirar ni excluir las 156 rutas anteriores: **157 archivos `.component.ts` (149 features y 8 shared)**.

- `frontend/src/app/features/plataforma/c12-04-m04-01-cutover-readonly.component.ts`

La puerta de seguridad ERP también detectó el nuevo path privado antes de su incorporación a la allowlist exacta de `plataformaGuard`; se añade solo esa ruta, preservando la comprobación de sesión backend y los negativos de rutas no autorizadas. Esta reparación no autoriza cutover, restore, DNS, MySQL ni PROD, ni elimina auditorías. Revalidar CI exact-SHA, A0–A4 y A5 de Task distinta. Rollback con `git revert` normal.

## Actualización causal V19 C13-04.M04.01 — 17 de noviembre de 2026

El inventario estricto detectó correctamente el nuevo componente autónomo del catálogo privado en `dev`. Se incorpora **una única** entrada verificable, pasando de 157 a **158 componentes** (150 en features y 8 en shared) sin borrar ni reinterpretar los cortes históricos ni relajar la comparación de rutas reales.

- `frontend/src/app/features/plataforma/c13-04-m04-01-catalogo-rutas.component.ts`

El control P0-007.A mantiene negativos de archivo omitido, ficticio, duplicado y exclusión por carpeta. Esta entrada no acredita 16 módulos implementados, JWT/RBAC real, datos, HTTP, MySQL, métricas ni QA/main/PROD. La ruta tiene `plataformaGuard` y se revisa con el auditor exacto de control plane. Exigir GitHub Actions SUCCESS SHA nuevo, rollback git revert y A5 independiente.

## Actualización aditiva V19 C09-04.M04.01 — 17 de noviembre de 2026

Se incorpora exactamente una superficie privada real de mensajería multicanal declarativa. El manifiesto cerrado aumenta de 158 a **159 archivos .component.ts (151 features y 8 shared)**, preservando íntegros todos los cortes anteriores:

- `frontend/src/app/features/plataforma/c09-04-m04-01-mensajeria-privada.component.ts`

Su acceso exige plataformaGuard exacto y comprobación de sesión administrativa global en servidor; la pantalla local no lee datos tenant ni envía correo o WhatsApp. Registrar un componente no acredita proveedores, JWT/RBAC de integración, HTTP, MySQL, accesibilidad global ni despliegue. Mantener comparación por conjunto y pruebas negativas de P0-007.A; revalidar CI exact-SHA, A0–A4 y A5 por otra Task Javier. Rollback git revert.

## Actualización causal V19 C09-04.M04.02 — 17 de noviembre de 2026

El P0-007.A del SHA `83e8323a584a08305fe3f08335fc1f1ded4833de` detectó la ruta nueva que no estaba en el catálogo. Se incorpora exactamente una entrada adicional, ahora **160 componentes**; el histórico queda intacto.

- `frontend/src/app/features/plataforma/c09-04-m04-02-estados.component.ts`

Este catálogo es solo de archivos, no certificación Angular E2E, permisos JWT/tenant, API, MySQL, SMTP/WhatsApp ni QA/PROD. Revalidar verificador inalterado salvo conteos exactos, Scope Lock y procedencia V5; A5 independiente.
