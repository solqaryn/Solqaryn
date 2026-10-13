## 2026-10-13 — P0 #3: expansión de recursos empresariales independientes (candidato)

- 41 recursos comerciales adicionales de catálogo, almacén, caja, contabilidad, finanzas y cargas reciben `EmpresaId` nullable para preservar pruebas legacy; nuevos registros firmados sólo mediante tenant RBAC verificado.
- 33 unicidades antes globales se convierten en índices únicos por `EmpresaId` conservando índices no únicos de soporte de FKs, y el token de enlace público retiene unicidad global por seguridad. Se agregan 83 FKs compuestas de A/B, 76 índices hijos y 3 únicos de padres.
- Quedan 123/132 entidades EF con `EmpresaId`; las nueve excepciones restantes son plataforma, identidad, autorización y catálogos estáticos globales. Mantener la cuarentena de segunda empresa, sin QA/PROD ni atribución de datos históricos.
- Este commit no equivale a certificación; exige MySQL efímero exact-head, pruebas y merge en `dev`.

# SOLQARYN — Inventario técnico de dependencias para aislamiento comercial P0

### Actualización P0 #3 — PR #3599 (13 octubre 2026; candidato, NO fusionado)

Se añadieron unicidades comerciales por EmpresaId e índices compuestos de cinco raíces. Variantes, imágenes, detalles de compra y venta y facturas reciben EmpresaId nullable derivado/validado, además de doce claves foráneas compuestas en MySQL. Se incorporó migración Oracle EF10 y contratos físicos de bootstrap. Esto es un tramo de la solución, NO su certificación: quedan entidades comerciales descendientes por clasificar/proteger, filtros de lectura y validación MySQL exact-head. No levantar cuarentena de segunda empresa, no tocar QA ni PROD y no reasignar los 19 registros descartables.


**Fecha:** 2026-10-13. **Fuente:** modelo Oracle EF Core 10 de `dev`, SHA `ba2ccbd01939326cbdcef5817e5e0089cca25b59`, archivo `backend/src/Infrastructure.Migrations/Migrations/OracleAppDbContextModelSnapshot.cs`. **Estado:** diagnóstico de código, no certificación multiempresa ni clasificación definitiva de propiedad.

## Resumen comprobable

- Entidades EF únicas en el snapshot: **132**.
- Entidades con propiedad física de `EmpresaId`: **20** (tipos `int` o `int?`).
- Raíces comerciales analizadas: **Producto, Cliente, Proveedor, Compra, Venta** (cinco con `EmpresaId?` de transición).
- Entidades adicionales conectadas por relaciones EF a alguna de estas raíces: **66** (unión sin duplicados, incluyendo descendientes con owner propio).
- El grafo sigue enlaces de FK desde descendiente hacia principal y marca la profundidad de dependencia; **no demuestra que la FK lleve ownership correcto ni que las lecturas respeten el tenant**.

## Matriz de entidades y tablas

**REVISAR DERIVACIÓN** indica una dependencia a una raíz comercial y exige decidir entre ownership directo, derivado y consistencia FK/clave compuesta. **OWNER DIRECTO (OTRO SCOPE)** requiere revisar si la tabla también necesita referencias comerciales consistentes. **CLASIFICACIÓN PENDIENTE** no significa global ni exenta: requiere revisión humana del contrato de datos.

| Tabla EF | Entidad | EmpresaId | Dependencia raíz:profundidad | Clasificación preliminar |
|---|---|---|---|---|
| AjusteInventarioDetalles | AjusteInventarioDetalle | — | Producto:1 | REVISAR DERIVACIÓN |
| AjustesInventario | AjusteInventario | — | — | CLASIFICACIÓN PENDIENTE |
| Almacenes | Almacen | — | — | CLASIFICACIÓN PENDIENTE |
| AplicacionesCuentaPorPagar | AplicacionCuentaPorPagar | — | Proveedor:2 | REVISAR DERIVACIÓN |
| AsientoDetalles | Contabilidad.AsientoDetalle | — | — | CLASIFICACIÓN PENDIENTE |
| AsientosContables | Contabilidad.AsientoContable | — | — | CLASIFICACIÓN PENDIENTE |
| AsignacionesCostoMovimientoInventario | AsignacionCostoMovimientoInventario | — | Producto:2, Cliente:3, Proveedor:3, Compra:2, Venta:2 | REVISAR DERIVACIÓN |
| Bancos | Catalogos.Banco | — | — | CLASIFICACIÓN PENDIENTE |
| CajaMovimientos | Cajas.CajaMovimiento | — | — | CLASIFICACIÓN PENDIENTE |
| Cajas | Cajas.Caja | — | — | CLASIFICACIÓN PENDIENTE |
| CajaSesiones | Cajas.CajaSesion | — | — | CLASIFICACIÓN PENDIENTE |
| CapasCostoInventario | CapaCostoInventario | — | Producto:2, Cliente:3, Proveedor:3, Compra:2, Venta:2 | REVISAR DERIVACIÓN |
| CargaMasivaErrores | CargaMasivaError | — | — | CLASIFICACIÓN PENDIENTE |
| CargasMasivas | CargaMasiva | — | — | CLASIFICACIÓN PENDIENTE |
| Categorias | Categoria | — | — | CLASIFICACIÓN PENDIENTE |
| CentrosCosto | Contabilidad.CentroCosto | — | — | CLASIFICACIÓN PENDIENTE |
| Clientes | Cliente | \`int?\` | — | RAÍZ COMERCIAL |
| Colores | Color | — | — | CLASIFICACIÓN PENDIENTE |
| CompraDetalles | CompraDetalle | — | Producto:1, Proveedor:2, Compra:1 | REVISAR DERIVACIÓN |
| CompraDocumentos | CompraDocumento | — | Proveedor:2, Compra:1 | REVISAR DERIVACIÓN |
| CompraImpuestos | CompraImpuesto | — | Proveedor:2, Compra:1 | REVISAR DERIVACIÓN |
| Compras | Compra | \`int?\` | Proveedor:1 | RAÍZ COMERCIAL |
| ConciliacionesBancarias | Bancos.ConciliacionBancaria | — | — | CLASIFICACIÓN PENDIENTE |
| ConfigEmpresas | ConfigEmpresa | \`int\` | — | OWNER DIRECTO (OTRO SCOPE) |
| ConfiguracionesContables | Contabilidad.ConfiguracionContable | — | — | CLASIFICACIÓN PENDIENTE |
| ConfiguracionesWhatsAppEmpresa | ConfiguracionWhatsAppEmpresa | \`int\` | — | OWNER DIRECTO (OTRO SCOPE) |
| ConsumoInsumoDetalles | ConsumoInsumoDetalle | — | Producto:1 | REVISAR DERIVACIÓN |
| ConsumosInsumos | ConsumoInsumo | — | — | CLASIFICACIÓN PENDIENTE |
| ConteoInventarioDetalles | ConteoInventarioDetalle | — | Producto:2 | REVISAR DERIVACIÓN |
| ConteosInventario | ConteoInventario | — | — | CLASIFICACIÓN PENDIENTE |
| CostosEnvio | CostoEnvio | — | — | CLASIFICACIÓN PENDIENTE |
| CostosEstandarInventario | CostoEstandarInventario | — | Producto:2 | REVISAR DERIVACIÓN |
| CotizacionDetalles | CotizacionDetalle | — | Producto:1, Cliente:2 | REVISAR DERIVACIÓN |
| Cotizaciones | Cotizacion | — | Cliente:1 | REVISAR DERIVACIÓN |
| CreditosCliente | CreditoCliente | — | Cliente:1 | REVISAR DERIVACIÓN |
| CuentasBancarias | Bancos.CuentaBancaria | — | — | CLASIFICACIÓN PENDIENTE |
| CuentasContables | CuentaContable | — | — | CLASIFICACIÓN PENDIENTE |
| CuentasPorPagar | CuentaPorPagar | — | Proveedor:1 | REVISAR DERIVACIÓN |
| DashboardKpiConfiguraciones | DashboardKpiConfiguracion | — | — | CLASIFICACIÓN PENDIENTE |
| DescuentoCategorias | DescuentoCategoria | — | — | CLASIFICACIÓN PENDIENTE |
| DescuentoClientes | DescuentoCliente | — | Cliente:1 | REVISAR DERIVACIÓN |
| DescuentoProductos | DescuentoProducto | — | Producto:1 | REVISAR DERIVACIÓN |
| DescuentoRoles | DescuentoRol | — | — | CLASIFICACIÓN PENDIENTE |
| Descuentos | Descuento | — | — | CLASIFICACIÓN PENDIENTE |
| DevolucionClienteDetalles | DevolucionClienteDetalle | — | Producto:2, Cliente:3, Venta:2 | REVISAR DERIVACIÓN |
| DevolucionesCliente | DevolucionCliente | — | Cliente:2, Venta:1 | REVISAR DERIVACIÓN |
| DevolucionesProveedor | DevolucionProveedor | — | Proveedor:1 | REVISAR DERIVACIÓN |
| DevolucionProveedorDetalles | DevolucionProveedorDetalle | — | Producto:1, Proveedor:2 | REVISAR DERIVACIÓN |
| DocumentosFiscalesEmision | DocumentoFiscalEmision | \`int\` | Cliente:3, Venta:2 | REVISAR DERIVACIÓN |
| EmailsEmpresariales | EmailEmpresarial | \`int\` | — | OWNER DIRECTO (OTRO SCOPE) |
| EmpresaConfiguraciones | EmpresaConfiguracion | — | — | CLASIFICACIÓN PENDIENTE |
| Empresas | Empresa | — | — | CLASIFICACIÓN PENDIENTE |
| EnlacesPublicosFactura | EnlacePublicoFactura | — | — | CLASIFICACIÓN PENDIENTE |
| EvaluacionesProveedor | EvaluacionProveedor | — | Proveedor:1 | REVISAR DERIVACIÓN |
| ExistenciasVariante | ExistenciaVariante | — | Producto:2 | REVISAR DERIVACIÓN |
| FacturaDetalles | FacturaDetalle | — | Cliente:3, Venta:2 | REVISAR DERIVACIÓN |
| FacturaPagos | FacturaPago | — | Cliente:3, Venta:2 | REVISAR DERIVACIÓN |
| FacturaProveedorDetalles | FacturaProveedorDetalle | — | Producto:1, Proveedor:2 | REVISAR DERIVACIÓN |
| Facturas | Factura | — | Cliente:2, Venta:1 | REVISAR DERIVACIÓN |
| FacturasProveedor | FacturaProveedor | — | Proveedor:1 | REVISAR DERIVACIÓN |
| HistorialAplicacionImpuestos | HistorialAplicacionImpuesto | — | — | CLASIFICACIÓN PENDIENTE |
| HistorialEnviosFactura | HistorialEnvioFactura | — | — | CLASIFICACIÓN PENDIENTE |
| HistorialUsoDescuentos | HistorialUsoDescuento | — | Cliente:1, Venta:1 | REVISAR DERIVACIÓN |
| ImpuestoCategorias | ImpuestoCategoria | — | — | CLASIFICACIÓN PENDIENTE |
| ImpuestoClientes | ImpuestoCliente | — | Cliente:1 | REVISAR DERIVACIÓN |
| ImpuestoOperaciones | ImpuestoOperacion | — | — | CLASIFICACIÓN PENDIENTE |
| ImpuestoProductos | ImpuestoProducto | — | Producto:1 | REVISAR DERIVACIÓN |
| ImpuestoProveedores | ImpuestoProveedor | — | Proveedor:1 | REVISAR DERIVACIÓN |
| Impuestos | Impuesto | — | — | CLASIFICACIÓN PENDIENTE |
| LotesInventario | LoteInventario | — | Producto:2 | REVISAR DERIVACIÓN |
| Marcas | Marca | — | — | CLASIFICACIÓN PENDIENTE |
| MatchesConciliacion | Bancos.MatchConciliacion | — | Cliente:3, Proveedor:3, Compra:2, Venta:2 | REVISAR DERIVACIÓN |
| MensajesOutbox | MensajeOutbox | \`int\` | — | OWNER DIRECTO (OTRO SCOPE) |
| MetodosPago | Catalogos.MetodoPago | — | — | CLASIFICACIÓN PENDIENTE |
| Modelos | Modelo | — | — | CLASIFICACIÓN PENDIENTE |
| MovimientosEstadoCuenta | Bancos.MovimientoEstadoCuenta | — | — | CLASIFICACIÓN PENDIENTE |
| MovimientosFinancieros | MovimientoFinanciero | — | Cliente:2, Proveedor:2, Compra:1, Venta:1 | REVISAR DERIVACIÓN |
| MovimientosInventario | MovimientoInventario | — | Producto:1, Cliente:2, Proveedor:2, Compra:1, Venta:1 | REVISAR DERIVACIÓN |
| NotasCreditoCliente | NotaCreditoCliente | — | Cliente:3, Venta:2 | REVISAR DERIVACIÓN |
| NotasCreditoProveedor | NotaCreditoProveedor | — | Proveedor:1 | REVISAR DERIVACIÓN |
| OrdenCompraDetalles | OrdenCompraDetalle | — | Producto:1, Proveedor:2 | REVISAR DERIVACIÓN |
| OrdenesCompra | OrdenCompra | — | Proveedor:1 | REVISAR DERIVACIÓN |
| PagosOnline | PagoOnline | \`int\` | Cliente:3, Venta:2 | REVISAR DERIVACIÓN |
| PedidosVenta | PedidoVenta | — | Cliente:1 | REVISAR DERIVACIÓN |
| PedidoVentaDetalles | PedidoVentaDetalle | — | Producto:1, Cliente:2 | REVISAR DERIVACIÓN |
| PeriodosContables | Contabilidad.PeriodoContable | — | — | CLASIFICACIÓN PENDIENTE |
| Permisos | Permiso | — | — | CLASIFICACIÓN PENDIENTE |
| PlanLimites | Plan | — | — | CLASIFICACIÓN PENDIENTE |
| PlanModulos | PlanModulo | — | — | CLASIFICACIÓN PENDIENTE |
| PlantillasCorreoEmpresa | PlantillaCorreoEmpresa | \`int\` | — | OWNER DIRECTO (OTRO SCOPE) |
| PlantillasEmailEmpresarial | PlantillaEmailEmpresarial | \`int\` | — | OWNER DIRECTO (OTRO SCOPE) |
| PoliticasCosteoInventario | PoliticaCosteoInventario | — | — | CLASIFICACIÓN PENDIENTE |
| PreparacionesPedidoVenta | PreparacionPedidoVenta | — | Cliente:2, Venta:2 | REVISAR DERIVACIÓN |
| PreparacionPedidoVentaDetalles | PreparacionPedidoVentaDetalle | — | Producto:2, Cliente:3, Venta:3 | REVISAR DERIVACIÓN |
| ProductoImagenes | ProductoImagen | — | Producto:1 | REVISAR DERIVACIÓN |
| Productos | Producto | \`int?\` | — | RAÍZ COMERCIAL |
| ProductoVariantes | ProductoVariante | — | Producto:1 | REVISAR DERIVACIÓN |
| Proveedores | Proveedor | \`int?\` | — | RAÍZ COMERCIAL |
| RecepcionCompraDetalles | RecepcionCompraDetalle | — | Producto:1, Proveedor:3 | REVISAR DERIVACIÓN |
| RecepcionesCompra | RecepcionCompra | — | Proveedor:2 | REVISAR DERIVACIÓN |
| RegistrosAuditoria | RegistroAuditoria | \`int?\` | — | OWNER DIRECTO (OTRO SCOPE) |
| ReservaInventarioDetalles | ReservaInventarioDetalle | — | Producto:2, Cliente:3, Venta:2 | REVISAR DERIVACIÓN |
| ReservasInventario | ReservaInventario | — | Cliente:2, Venta:1 | REVISAR DERIVACIÓN |
| RevisionesFinancieras | RevisionFinanciera | — | — | CLASIFICACIÓN PENDIENTE |
| Roles | Rol | \`int?\` | — | OWNER DIRECTO (OTRO SCOPE) |
| RolPermisos | RolPermiso | — | — | CLASIFICACIÓN PENDIENTE |
| SecuenciasDocumento | SecuenciaDocumento | \`int\` | — | OWNER DIRECTO (OTRO SCOPE) |
| SeriesInventario | SerieInventario | — | Producto:2 | REVISAR DERIVACIÓN |
| SolicitudCompraDetalles | SolicitudCompraDetalle | — | Producto:1, Proveedor:2 | REVISAR DERIVACIÓN |
| SolicitudesCompra | SolicitudCompra | — | Proveedor:1 | REVISAR DERIVACIÓN |
| Sucursales | Sucursal | \`int?\` | — | OWNER DIRECTO (OTRO SCOPE) |
| Suscripciones | Suscripcion | \`int\` | — | OWNER DIRECTO (OTRO SCOPE) |
| Tallas | Talla | — | — | CLASIFICACIÓN PENDIENTE |
| TemaVisual | TemaVisual | — | — | CLASIFICACIÓN PENDIENTE |
| ThreeWayMatchDiscrepancias | ThreeWayMatchResult | — | Proveedor:2 | REVISAR DERIVACIÓN |
| TiendaCuentasCliente | TiendaCuentaCliente | — | Cliente:1 | REVISAR DERIVACIÓN |
| TiendaDireccionesCliente | TiendaDireccionCliente | — | Cliente:2 | REVISAR DERIVACIÓN |
| TiendaFavoritosCliente | TiendaFavoritoCliente | — | Producto:1, Cliente:2 | REVISAR DERIVACIÓN |
| TiendaSesionesCliente | TiendaSesionCliente | — | Cliente:2 | REVISAR DERIVACIÓN |
| TipoClientes | TipoCliente | — | — | CLASIFICACIÓN PENDIENTE |
| TransferenciaInventarioDetalles | TransferenciaInventarioDetalle | — | Producto:2 | REVISAR DERIVACIÓN |
| TransferenciasInventario | TransferenciaInventario | — | — | CLASIFICACIÓN PENDIENTE |
| UbicacionesAlmacen | UbicacionAlmacen | — | — | CLASIFICACIÓN PENDIENTE |
| UsuarioEmpresas | UsuarioEmpresa | \`int\` | — | OWNER DIRECTO (OTRO SCOPE) |
| UsuarioRolesPlataforma | UsuarioRolPlataforma | — | — | CLASIFICACIÓN PENDIENTE |
| Usuarios | Usuario | — | — | CLASIFICACIÓN PENDIENTE |
| VariacionesCostoEstandarInventario | VariacionCostoEstandarInventario | — | Producto:2, Cliente:3, Proveedor:3, Compra:2, Venta:2 | REVISAR DERIVACIÓN |
| VentaDescuentos | VentaDescuento | — | Cliente:2, Venta:1 | REVISAR DERIVACIÓN |
| VentaDetalles | VentaDetalle | — | Producto:1, Cliente:2, Venta:1 | REVISAR DERIVACIÓN |
| VentaImpuestos | VentaImpuesto | — | Cliente:2, Venta:1 | REVISAR DERIVACIÓN |
| Ventas | Venta | \`int?\` | Cliente:1 | RAÍZ COMERCIAL |
| WebhooksEntrantes | WebhookEntrante | \`int\` | — | OWNER DIRECTO (OTRO SCOPE) |

## Bloqueos obligatorios antes de declarar LISTO

1. Verificar las tablas físicas `information_schema` de MySQL DEV frente a esta matriz, incluyendo tablas no administradas por EF, vistas, procedimientos, SQL directo, jobs y colas. No interpretar un snapshot como inventario de runtime completo.
2. Clasificar explícitamente cada tabla como **global verificable**, **propiedad directa**, **descendiente obligado al mismo owner** o **ambigua en cuarentena**. Las pertenencias derivadas deben tener ruta inequívoca, FKs/índices y validación transaccional.
3. Auditar consultas y cambios que salten filtros: `FromSql`, `IgnoreQueryFilters`, `ExecuteUpdate`, `ExecuteDelete`, `SqlQuery`, jobs, exports, públicos y caché. El JWT/header sin membresía validada no asigna dueño.
4. No atribuir automáticamente los 19 registros históricos de Aiven DEV. El informe de procedencia #38409321128 identifica autores y membresías **actuales**, no una prueba histórica completa. Conservar NULL y cuarentena hasta tener procedencia documentada.
5. No retirar `UnscopedCommercialTenantGuardMiddleware`, no habilitar segunda empresa ni promover QA/PROD hasta CI exact-head, MySQL real/efímero, pruebas A/B y humo DEV satisfactorios.

**Nota:** la tabla es un artefacto derivado del modelo EF en Git, no contiene registros ni PII y no ejecuta DML/DDL sobre Aiven.
