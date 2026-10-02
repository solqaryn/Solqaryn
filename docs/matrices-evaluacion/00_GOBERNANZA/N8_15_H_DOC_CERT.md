# N8.15.H — DOC_CERT / baseline canónico

Estado: `LISTO_REAL`

## Baseline certificado

- Rama: `Desarrollo` únicamente.
- Autoridad única: `docs/VAEP_AUTHORITY.md`.
- Especificación causal: `docs/matrices-evaluacion/00_GOBERNANZA/ESPECIFICACION_EJECUCION_N8_15_N8_24.md`.
- Baseline funcional previo a la evidencia N8.15: `e03dbfe786bd3ba323414e8af1f3e53f91872a98`.
- Tree SHA del baseline funcional: `b325f5c63776d8396957517bac2b82940ae009ce`.
- Entrada de DOC_CERT: `84285b483ef7a2410b7d9dc86690401bcd0131e2`.
- A–G son cambios de evidencia/documentación; no alteran runtime, esquema, dependencias, rutas ni producto.

## Evidencia canónica A–G

1. `N8_15_A_BASELINE_PRE.md` — baseline, roots y método reproducible.
2. `N8_15_B_DOMAIN_INVENTARIO.md` — dominios, ownership y dependencias.
3. `N8_15_C_DB_MIG_INVENTARIO.md` — DbContext/provider/configuraciones/migraciones.
4. `N8_15_D_BACKEND_API_INVENTARIO.md` — API/Application/Infrastructure y trazas.
5. `N8_15_E_FRONTEND_UX_INVENTARIO.md` — rutas/features/shell/guards/contratos UI.
6. `N8_15_F_SEC_AUDIT_INVENTARIO.md` — authn/authz/RBAC/tenant/audit/PII/logging/health/observability.
7. `N8_15_G_TEST_CI_VALIDATION.md` — referencias estáticas y gates causales.

## Catálogo canónico y conteos exactos

Los conteos siguientes son **conteos exactos de unidades canónicas declaradas**, no estimaciones de líneas de código ni extrapolaciones. Cuando una unidad es un Git tree, todos sus hijos quedan cubiertos transitivamente por el SHA del tree; no se usa una selección visual parcial como sustituto del árbol.

| Clase de unidad canónica | Conteo exacto | Cobertura / autoridad | Clasificación primaria |
| --- | ---: | --- | --- |
| Backend source layers | 4 | `API`, `Application`, `Domain`, `Infrastructure` | KEEP |
| Bounded/domain areas del mapa N8.15.B | 9 | Identity/RBAC; Catálogo; Clientes/comercial; Compras/proveedores; Inventario/logística; Caja/bancos; Finanzas/contabilidad; BI/reporting; Governance/config/integraciones | KEEP |
| Backend API controller files | 90 | `backend/src/API/Controllers/*.cs` enumerados físicamente | KEEP |
| Frontend feature roots | 48 | `frontend/src/app/features/*` enumerados físicamente | KEEP |
| EF migration roots | 2 | `Infrastructure/Migrations` y `Infrastructure/Persistence/Migrations` | KEEP |
| DbContext autoritativo | 1 | `Infrastructure/Persistence/AppDbContext.cs` | KEEP |
| API middleware files | 3 | CorrelationId, RequestObservability, ExceptionHandling | KEEP |
| Angular route table principal | 1 | `frontend/src/app/app.routes.ts` | KEEP |

**Total exacto de registros ancla del catálogo canónico: 158.** Son registros de arquitectura/ownership; no se afirma que 158 sea el número total de archivos del repositorio.

### 90 controllers exactos

AjustesInventario, Almacenes, AsientosContables, Auditoria, Auth, Automatizaciones, CargasMasivas, CatalogoProductoControllerBase, Categorias, CentrosCosto, Clientes, Colores, Compras, ConciliacionBancaria, Conciliacion, Contabilizacion, ConteosInventario, CorreoDiagnostico, CosteoInventario, CostosEnvio, Cotizaciones, CreditosCliente, CuentaBancaria, CuentaContable, CuentasPorCobrar, CuentasPorPagar, Dashboard, DashboardKpiConfiguracion, Descuentos, DevolucionesCliente, DevolucionesProveedor, EmailEmpresarial, EmpresaConfiguracion, Empresas, EstadosFinancieros, EvaluacionesProveedor, ExistenciasVariante, FacturacionFiscal, Facturas, FacturasProveedor, Finanzas, Impuestos, InboundWebhooks, InsumosAdministrativos, InventarioAjustes, Marcas, MensajesOutbox, MetodosPago, Modelos, MovimientosInventario, NotasCreditoCliente, NotasCreditoProveedor, OperacionBancaria, OrdenesCompra, PagosOnline, PedidosVenta, Perfil, PeriodosContables, Permisos, PreparacionesPedidoVenta, ProductoVariantes, Productos, Proveedores, RecepcionesCompra, RentabilidadVentas, ReportesAdministrativos, ReportesCompras, ReportesInventario, ReportesInventarioKardex, ReportesInventarioReconciliacion, ReportesInventarioStockHealth, ReportesInventarioValorizacion, ReportesVentas, ReservasInventario, Roles, SecuenciasDocumento, SolicitudesCompra, Sucursales, SuscripcionesSaaS, Tallas, TemaVisual, TenantContext, Tienda, TipoClientes, TransferenciasInventario, TrazabilidadInventario, UbicacionesAlmacen, Usuarios, Ventas y WhatsApp.

### 48 feature roots exactos

almacenes, asientos-contables, auditoria, caja, cargas-masivas, catalogos-producto, categorias, centro-reportes, centros-costo, clientes, compras, configuracion, costos-envio, cotizaciones, cuentas-bancarias, cuentas-por-cobrar, dashboard, descuentos, estados-financieros, facturas, finanzas, impuestos, inventario, login, metodos-pago, ordenes-compra, pedidos-venta, perfil, periodos-contables, permisos, plan-cuentas, preparaciones-pedido-venta, productos, proveedores, recepciones-compra, rentabilidad, reportes-administrativos, reportes-compras, reportes-inventario, reportes-ventas, roles, solicitudes-compra, sucursales, tipo-clientes, ubicaciones-almacen, usuarios, solqaryn y ventas.

## Dependency map canónico

Flujo base:

`bounded area -> route/screen/interaction -> frontend client -> controller endpoint -> DTO/validator -> Application service/use case -> repository/Infrastructure adapter -> AppDbContext/MySQL`.

Dependencias cross-domain materiales:

- Ventas/facturación -> catálogo + inventario -> caja/bancos/finanzas -> contabilidad/documentos/auditoría.
- Compras/proveedores -> catálogo -> inventario -> CxP/finanzas -> contabilidad/auditoría.
- Inventario/logística -> catálogo + almacenes/sucursales/ubicaciones -> costeo/contabilidad/reporting.
- Identity/RBAC/tenant -> frontera transversal de todas las capacidades ERP autenticadas.
- BI/reporting -> lectura transversal; no se convierte en segunda autoridad de escritura.
- Storefront `solqaryn` -> canal UI público separado que reutiliza catálogo/comercial por APIs; el backend sigue siendo frontera de reglas y datos sensibles.

## Clasificación canónica

### KEEP

Los 158 registros ancla anteriores permanecen `KEEP` en N8.15. Esto incluye explícitamente ambos roots de migraciones: su coexistencia es historia de persistencia y no autoriza eliminar ni mover archivos.

### CONSOLIDATE — 3 candidatos exactos, no destructivos

1. Taxonomía/ownership documental entre `catalogos-producto` y features específicas de catálogo.
2. Layout histórico de los dos roots EF migrations, preservando íntegramente la historia antes de cualquier consolidación.
3. Layout mixto de Application (`Bancos/` junto a carpetas transversales como `Services/`, `DTOs/`, `Interfaces/`) para clarificar bounded ownership.

### DEPRECATE

Conteo exacto certificado: `0`.

### REMOVE_SAFE

Conteo exacto certificado: `0`.

N8.15 no encontró evidencia suficiente para borrar de forma segura un asset. Ninguna similitud nominal, falta de aparición en menú o doble ubicación histórica se acepta como prueba de remoción.

### UNKNOWN

No se promociona ningún asset a remoción desde `UNKNOWN`. Cualquier futuro candidato individual sin trace completo de referencia/runtime/tests comienza en `UNKNOWN` y sólo puede cambiar de clase en una etapa causal posterior con evidencia dirigida.

## Seguridad y autoridad

- Backend continúa como autoridad de authz, tenant, invariantes, estados, totales y persistencia.
- Guards/UI son enforcement de navegación/UX, no sustituyen autorización backend.
- JWT, CORS explícito, rate-limit de login, security headers, audit/correlation/observability y health forman parte del baseline KEEP.
- Secretos reales permanecen fuera del repositorio.

## Certificación de gates

El último gate integral relevante sobre el frontend funcional ejecutó `Desarrollo - Compilación y pruebas` run `35022980732` y cerró SUCCESS en los cinco jobs causales: higiene, Docker/aislamiento, frontend lint/build producción, backend Release/tests y MySQL/EF/migrations. El delta backend funcional posterior `bab40c06bf162718c44444cfc112d7b776b1797b` tiene receipt N8.12.D con REVIEW_FIRST `P0=0/P1=0/P2=0` y gate dirigido SUCCESS para observabilidad. A–G son docs-only y no invalidan esos gates.

## REVIEW_FIRST final

- P0=0.
- P1=0.
- No hay `REMOVE_SAFE` no probado.
- No se ejecutó DDL ni migración sobre datos.
- No se tocó `main`, Producción, deploy, secretos ni PR #2.
- La certificación distingue explícitamente conteos de **registros ancla** de un conteo total de archivos; no fabrica un total repo que no haya sido medido.

## Resultado

`N8.15.H = LISTO_REAL` y, por cierre A→H, **`N8.15 = LISTO_REAL`**.

Siguiente parent dependency-valid dentro del owner gate: `N8.16.A — PRE`.
