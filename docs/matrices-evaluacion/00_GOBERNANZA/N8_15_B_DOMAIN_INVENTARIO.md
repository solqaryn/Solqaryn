# N8.15.B — DOMAIN inventario

Estado: `LISTO_REAL`

Baseline de entrada: `7f6da862a8ae3173b93fb48f8faeeee7f2223d8a` (`N8.15.A` ya certificado).

## Fuentes físicas contrastadas

- `backend/src` contiene exactamente las cuatro capas raíz actuales: `API`, `Application`, `Domain`, `Infrastructure`.
- `frontend/src/app/features` fue enumerado desde GitHub sobre `Desarrollo`; no se tomó el índice documental como sustituto del código.
- `backend/src/API/Controllers` fue enumerado desde GitHub y contrastado con `PROJECT_INDEX.md` y `ARCHITECTURE.md`.
- `Domain/Entities` se inspeccionó físicamente; además de entidades planas existen bounded folders como `Bancos/`, `Cajas/` y `Catalogos/`, por lo que el inventario no asume un único namespace plano.

## Bounded areas actuales

| Parent area | Capacidades / hijos observables | Anclas actuales | Dependencias materiales actuales | Clasificación baseline |
| --- | --- | --- | --- | --- |
| Identity & Access | login, usuarios, roles, permisos, perfil | `features/login`, `usuarios`, `roles`, `permisos`, `perfil`; `AuthController`, `UsuariosController`, `RolesController`, `PermisosController`, `PerfilController` | JWT, RBAC, auditoría, tenant/empresa | KEEP |
| Catálogo producto | productos, variantes, categorías, marcas, modelos, tallas, colores, imágenes | `features/productos`, `catalogos-producto`, `categorias`; controllers producto/catálogos; entidades producto/catálogo | ventas, compras, inventario | KEEP; límites internos candidatos a CONSOLIDATE documental |
| Clientes & comercial | clientes, tipos de cliente, cotizaciones, pedidos, ventas, facturas | `features/clientes`, `tipo-clientes`, `cotizaciones`, `pedidos-venta`, `ventas`, `facturas`; controllers homónimos | catálogo, inventario, impuestos/descuentos, finanzas, documentos | KEEP |
| Compras & proveedores | proveedores, solicitudes, órdenes, recepciones, factura proveedor, devoluciones/notas crédito | `features/proveedores`, `compras`, `solicitudes-compra`, `ordenes-compra`, `recepciones-compra`; controllers/services homónimos | inventario, CxP/finanzas, catálogos | KEEP |
| Inventario & logística | almacenes, sucursales, ubicaciones, existencias, ajustes, movimientos, reservas, transferencias, conteos, costeo | `features/inventario`, `almacenes`, `sucursales`, `ubicaciones-almacen`; controllers inventario | compras, ventas, catálogo, contabilidad/costos | KEEP |
| Caja & bancos | caja física/lógica, cuentas bancarias, conciliación/tesorería | `features/caja`, `cuentas-bancarias`; bounded folders Domain `Cajas/`, `Bancos/` | ventas, pagos, finanzas, contabilidad | KEEP |
| Finanzas & contabilidad | movimientos financieros, CxP/CxC, asientos, centros de costo | `features/finanzas`, `asientos-contables`, `centros-costo`; `FinanzasController`, `CuentasPorPagarController`, `AsientosContablesController`, `CentrosCostoController` | ventas, compras, bancos/caja, auditoría | KEEP |
| BI / reporting | centro de reportes, exportaciones/consultas | `features/centro-reportes`; servicios/endpoints de reporting según área | lectura transversal de dominios | KEEP |
| Governance / configuración | empresa config, tema visual, cargas masivas, automatizaciones, auditoría | `features/configuracion`, `cargas-masivas`, `auditoria`; `EmpresaConfiguracionController`, `TemaVisualController`, `CargasMasivasController`, `AutomatizacionesController`, `AuditoriaController` | transversal | KEEP |
| Integraciones / documentos | Cloudinary, SMTP, QuestPDF y adaptadores externos | contratos Application + implementaciones Infrastructure + registro DI | ventas/facturas/perfil/reportes | KEEP |

## Parent→child y permisos

El flujo autoritativo actual queda modelado como:

`bounded area -> feature/use case -> Angular route/component -> backend controller endpoint -> Application service/DTO/validator -> Domain entity/invariant -> Infrastructure repository/service -> AppDbContext/MySQL`.

Las rutas/ocultación UI no son autoridad de negocio. Los permisos visuales y guards son hijos UX del permiso real; la autorización de endpoint/backend debe permanecer como frontera autoritativa.

## Aliases, duplicidad y coupling detectados

1. **Catálogo producto**: `catalogos-producto` y features específicas como `categorias` representan límites de UI distintos sobre un mismo bounded area. Se clasifica `CONSOLIDATE` sólo a nivel de taxonomía/ownership documental; no se autoriza mover ni borrar código.
2. **Finanzas / Caja / Bancos / Contabilidad**: existe coupling intencional por flujos de pago, conciliación y asiento. Ownership de reglas contables debe quedar en backend/servicios del dominio financiero; UI de caja/banco no debe convertirse en segunda autoridad. Clasificación `KEEP` con dependency map explícito.
3. **Ventas → Inventario → Finanzas** y **Compras → Inventario → CxP/Finanzas** son dependencias cross-domain materiales y esperadas; requieren transacción/idempotencia/auditoría en las etapas posteriores, no duplicación de cálculo.
4. **Migraciones históricas en dos ubicaciones** (`Infrastructure/Migrations` y `Infrastructure/Persistence/Migrations`) son un alias físico conocido del historial de persistencia; no se clasifican removibles en DOMAIN.
5. No se certifica ningún asset como `REMOVE_SAFE` en B. Cualquier aparente duplicado sin referencias completas permanece `UNKNOWN` hasta C/G/H.

## Casos de uso representativos inventariados

- autenticación/sesión y autorización RBAC;
- mantenimiento de maestros de producto;
- ciclo solicitud→orden→recepción→factura proveedor→CxP;
- ciclo cotización→pedido→venta/factura→pago;
- reserva/transferencia/conteo/ajuste/costeo de inventario;
- caja/banco/conciliación y generación de asientos;
- reportes/BI;
- configuración multiempresa, auditoría, carga masiva y automatización;
- generación/almacenamiento/envío de documentos.

## REVIEW_FIRST

P0=0, P1=0. No se introdujo nueva frontera de dominio ni segunda autoridad. Los hallazgos de alias/coupling quedan como clasificación de inventario, no como refactor prematuro.

## Resultado

`N8.15.B = LISTO_REAL`.

Siguiente dependency-valid: `N8.15.C — DB_MIG`.
