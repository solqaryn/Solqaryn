# N8.15.D — BACKEND_API inventario

Estado: `LISTO_REAL`

Baseline de entrada: `69f6e76fc407a08fed0f3441d9100e493fbcc206`.

## Roots y composition

- HTTP composition root: `backend/src/API/Program.cs`.
- API física: `Controllers/`, `Filters/`, `Middleware/`, `Observability/`, `appsettings.json`, `appsettings.Development.json`.
- Application física: `Bancos/`, `Common/`, `DTOs/`, `Exceptions/`, `Interfaces/`, `Mappings/`, `Models/`, `Services/`, `Validators/`.
- Infrastructure física: `Repositories/`, `Services/`, `Persistence/`.
- Persistencia: `AppDbContext` + configuraciones EF + MySQL.

## Inventario por familias de endpoint/caso de uso

El directorio físico `API/Controllers` fue enumerado. Contiene familias para inventario, almacenes, contabilidad, auditoría, auth, automatizaciones, cargas masivas, catálogos/productos, centros de costo, clientes, compras y el resto de bounded areas reflejadas en el mapa de N8.15.B. El inventario no asume que el nombre del controller sea suficiente: el trace se completa contra Application/Infrastructure.

Trace base actual:

`Controller/[Http*] -> DTO/validator -> Application Service/interface -> Repository/Infrastructure Service -> AppDbContext/adapter -> permission/auth policy`

| Familia | Controller/API | Caso de uso Application | Persistencia/adaptador | Permission boundary |
| --- | --- | --- | --- | --- |
| Identity/RBAC | `Auth`, `Usuarios`, `Roles`, `Permisos`, `Perfil` | Auth/usuario/rol/permiso/perfil services + DTO/validators | usuario/rol/permiso repositories + JWT/image adapter | backend authorization/JWT/RBAC |
| Catálogo | producto/variante/categorías/catálogos | `Producto*`, `Categoria*`, catálogo services | repositories catálogo/producto + Cloudinary cuando aplica | backend permission + tenant/empresa |
| Compras | compras/solicitudes/órdenes/recepciones/facturas/devoluciones | services homónimos + three-way match | compra/orden/recepción/factura repositories + DbContext | permission + state/invariant validation |
| Inventario | ajustes/movimientos/existencias/reservas/transferencias/conteos/costeo/almacenes | services homónimos | repositories inventario + DbContext | permission + tenant/location invariants |
| Ventas/facturación | ventas/facturas/cotizaciones/pedidos | services homónimos | repositories venta/factura/pedido + PDF/mail adapters | permission + commercial isolation + document state |
| Caja/bancos/finanzas | caja, bancos, conciliación, finanzas, CxP/CxC, centros de costo | services financieros/bancarios | repositories caja/banco/finanzas + DbContext | permission + accounting/state invariants |
| Contabilidad | asientos/periodos | services contables | repositories/asiento persistence | permission + accounting closure rules |
| Governance | auditoría/configuración/cargas/automatizaciones | services homónimos | audit/config/load repositories/services | admin/explicit permission |
| Integraciones | endpoints que generan/envían/exportan | Application interfaces | Cloudinary, MailKit/SMTP, QuestPDF, ClosedXML, ImageSharp | caller permission + backend validation |

## Jobs / middleware / config

- `API/Middleware/**` y `API/Filters/**` forman parte del inventario transversal y no se clasifican como decoración.
- `API/Observability/**` se mantiene separado como capacidad operativa.
- `Program.cs` es autoridad de registro DI, authn/authz, middleware, health y wiring de adapters.
- `appsettings*.json` son configuración versionada; secretos reales deben permanecer fuera del repo.
- Background/outbox/automation behavior se inventaría como job/capacidad sólo donde exista implementación/referencia; no se infiere un scheduler inexistente por nombre.

## Duplicados / ownership

- `CatalogoProductoControllerBase` representa reutilización/base de controllers de catálogo, no un endpoint duplicado removible.
- Application tiene un bounded folder `Bancos/` además de las carpetas transversales (`Services`, `DTOs`, etc.); esto es ownership mixto de layout, candidato a `CONSOLIDATE` arquitectónico en N8.16/N8.18, no a mover en baseline.
- Repositories por aggregate y services concretos se clasifican `KEEP` salvo evidencia posterior de implementación huérfana.
- Ningún controller/repository/service se clasifica `REMOVE_SAFE` sólo por similitud nominal.

## REVIEW_FIRST

P0=0, P1=0. El inventario preserva backend como autoridad; no se creó segunda vía de autorización/persistencia, no se tocaron endpoints y no se alteró config/runtime.

## Resultado

`N8.15.D = LISTO_REAL`.

Siguiente dependency-valid: `N8.15.E — FRONTEND_UX`.
