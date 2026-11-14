# SOLQARYN V5 — C07-01.M01.03 — Inventario contrastado de código, contratos y evidencias de catálogo universal

**Fecha de revisión:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, C07-01.M01.03 (M01, preparación documental). **Naturaleza:** lectura selectiva de Git y delimitación de huecos, sin cambios runtime. La existencia de un archivo, método o atributo no certifica funcionalidad multitenant completa.

## 1. Criterio y límites del inventario

Esta entrega identifica componentes actuales que ya participan en producto, variantes, impuestos, categoría, publicación y ERP, compara contratos y deja pruebas faltantes explícitas. M01.01 fija alcance y no-regresión; M01.02 (actores/fronteras) pertenece a otra Task y no es apropiada ni se atribuye a este documento. El artefacto NO modifica endpoints, DB, EF, Angular, archivos binarios, variables de entorno ni esquema de autorización. No habilita aún C07-01.M01.06 ni M03/M06.

**Método:** lectura directa de blobs actuales de `solqaryn/Solqaryn@dev`; los SHA siguientes identifican contenido concreto, no el HEAD de rama. La evidencia de CI exact-SHA de esta publicación se comprueba separadamente. La revisión es **selectiva**, no un recorrido exhaustivo de cada controlador y consulta ni una prueba HTTP/SQL.

## 2. Matriz de evidencia por capa

| Capa y fuente real | Git blob SHA | Dato observable; prueba adicional necesaria |
| --- | --- | --- |
| Domain `Entities/Producto.cs` | `19632ce1aebe6a9cbc4379523367531852ee3e7e` | `EmpresaId?`, precio/costo, categoría, estado, imágenes, variantes y stock; NULL legado requiere tratamiento fail-closed |
| Domain `Entities/ProductoVariante.cs` | `de0dce026a147bb52ad46c952a2e9231015c195f` | `EmpresaId?`, producto padre, dimensión, SKU, stock y controles de trazabilidad; validar ownership padre-hijo |
| Domain `Entities/Categoria.cs` | `45e02b38473c89b944aeb02b5affcf65beb51f51` | No declara `EmpresaId` en esta clase: comprobar alcance real en repositorio/servicio, no asignar propiedad implícita |
| Domain `Entities/Impuesto.cs` | `f55d1e68b951ddb8a3daa3b9482f76b433d2470d` | Reglas de tasa y snapshots; validar vigencia, cálculo, relaciones y aislamiento de la aplicación, sin inferir de la entidad |
| Domain `Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | Raíz de tenant, estado y datos comerciales; no prueba por sí sola autorización de catálogo |
| Domain `Entities/PublicStoreBinding.cs` | `dae03bcd963281d74e1d02e460934ab0783d39d5` | Host y StoreKey ligados a EmpresaId para publicación; analizar resolución, caché y revocación |
| Application `Services/ProductoService.cs` | `045230de73cd1b4785b62a1b8c811c87b3d7fbd0` | Servicio consulta/creación/paginación y `VerifiedCommercialTenant.Resolve` al crear; exigir trazado de lecturas y writes auxiliares |
| Application `Services/ProductoVarianteService.cs` | `b2eb9fd253a980eea625970a1b20627c89969b6b` | CRUD variantes, servicios de catálogo, UnitOfWork y auditoría; revisar verificaciones de producto padre |
| Application `Services/CatalogoPublicoService.cs` | `3d1ba38e0638ad75ad23e92c98120cc1b99660e4` | Búsqueda y detalle público con cache/tenantKey opcionales; inspeccionar fallback y aislamiento por host |
| Infrastructure `Repositories/ProductoRepository.cs` | `8c3e4c3312d8dafcc2654f4012de89b3451893db` | `GetById`, paginación y lock transaccional SQL con `CommercialTenantSqlLockScope`; verificar paths y ausencia de filtración en lecturas |
| Infrastructure `Repositories/ProductoVarianteRepository.cs` | `001172f1f84828aa53d70a7dcec9c23cc30729a0` | Query de variantes y locks con scope; revisar integridad producto/variante/empresa |
| Infrastructure `Repositories/ImpuestoRepository.cs` | `701e87829d407a21d02b9ecb51d02e8335d0fa7e` | Relaciones, vigencia y registro de aplicaciones; no concluir política tenant sin leer AppDbContext y servicio |
| API `Controllers/ProductosController.cs` | `8b9e1f0600dd7ffe255552f237688ff5a8367fad` | `[Authorize]`, `[RequierePermiso]`, rutas `/productos`, CRUD transaccional con variantes; validar denegaciones reales |
| API `Controllers/ProductoVariantesController.cs` | `cd3c110394f3ce6a4d8c2920bfb04049e484280a` | `/productos/{productoId}/variantes`, permisos y galerías; probar parent cross-tenant y ownership de imagen |
| API `Controllers/TiendaController.cs` | `39a23ddb94b24077bbce79de1b965781b70ca37a` | `[AllowAnonymous]`, límites/rate limit y `ICatalogoPublicoService`; confirmar binding y DTO público por tenant |
| Angular `services/producto.service.ts` | `b51b4fb4a6595d6af83d1eb67da6e7321eed1db6` | `HttpClient` contra `/productos` y rutas variantes; permisos frontend no reemplazan autorización de API |
| Angular `features/productos/productos-list.component.ts` | `7022fb627469f3f304b7b93ac6708baf544fd87e` | Listado, filtros, diálogos y `PermisosRuntimeService`; validar paginación/teclado/accesibilidad aparte |

**Cuidado especial:** `ProductoRepository.GetByIdAsync` consulta por ID con `ConIncludes()` y el alcance efectivo depende de filtros de `AppDbContext`/tenant. La presencia de un lock transaccional con `({tenant} IS NULL OR ...)` no prueba aislamiento sin revisar el contrato de `RequireAsync`, configuración de consultas y todas las rutas. Se registra como cuestión a probar, NO como vulnerabilidad confirmada.

## 3. Superficies de datos y consumidores a revisar en fases siguientes

| Flujo | Entrada y consumidor conocido | Dependencias y límites |
| --- | --- | --- |
| Gestión ERP de producto | Angular `ProductoService` → API `ProductosController` → Application → Repository | `ProductoDto`, variantes, imágenes, transacciones y permisos; conservar errores y uso de stock |
| Gestión de variantes | Angular rutas producto/variantes → API `ProductoVariantesController` → UnitOfWork/Repositorio | Validar variante subordinada a producto del mismo tenant, SKU, lote/serie y media |
| Catálogo público | Host/binding → API `TiendaController` → `CatalogoPublicoService` → repo | `AllowAnonymous` solo para proyección pública, no para inventario privado |
| Categorías y dimensiones | Servicios/controladores de categoría y catálogo de producto | Revalidar ownership/compartición de categoría, marcas, modelos, color y talla |
| Tributos y precio | `ImpuestoService`, `ImpuestoRepository`, venta/factura histórica | Mantener snapshots monetarios y periodos, probar cálculo y límites tenant |
| Backoffice/seguridad | JWT, membresías, `RequierePermiso`, auditoría, registros ERP | Probar que cliente A no obtiene datos B incluso con ID numérico válido |
| Media/SEO/store | Gallery de producto/variante, `PublicStoreBinding`, cache público | Host A no usa imagen ajena, token, precio costo, ni proyecciones privadas B |

## 4. Brechas y ensayos causales pendientes

- G01: comprobar todos los filtros EF `Producto`, `ProductoVariante`, categoría e impuesto y sus bypass explícitos (si los hay) con empresa A/B; no extrapolar desde una clase.
- G02: validar integridad cross-tenant en variantes, imágenes, dimensiones y relaciones mediante servicios, repositorios y constraints reales.
- G03: comprobar `EmpresaId=NULL` en registros heredados en lectura, actualización y publicación; no tratarlo como público.
- G04: levantar contrato de `VerifiedCommercialTenant` y `CommercialTenantSqlLockScope`, incluido comportamiento sin contexto.
- G05: verificar denegación de JWT/membresía/rol revocado, incluso en operaciones de imagen y stock.
- G06: contrastar `Impuesto` y snapshots al actualizar tasas o asignaciones, sin modificar facturas históricas.
- G07: comprobar que cache/host público no cruza empresas ni se puede enumerar; analizar eventual invalidación.
- G08: validar paginación, costo de consultas y accesibilidad UI con pruebas reales; no inferir métricas del inventario.

### Futuros oráculos (todos NO_EJECUTADOS)

**Positivos P01–P06:** lista ERP filtrada por A; CRUD producto-variante A consistente; stock correcto; impuesto activo calculado; tienda host A ve solo proyección A; factura histórica conserva snapshot.

**Negativos N01–N12:** lectura producto B desde A; variante de B bajo parent A; imagen ajena; SKU de B; categoría de B sin permiso; tasa/relación ajena; `EmpresaId=NULL` expuesto; host inválido; cache de A sirviendo B; pérdida de rol/membresía; manipulación de precio de factura pasada; endpoint público devolviendo costo/PII. Son pruebas por implementar y ejecutar en fases aprobadas.

## 5. Dependencias y cierre de esta unidad

`PLAN_MAESTRO!A1012:O1012`: microtarea `C07-01.M01.03`; `COLA!G/O=SI`; `DAG_TECNICO!A973:J973` F=VALIDADO, G=SI. Las doce familias C01-01–04, C02-01–04 y C05-01–04, cada una M01.01–05, fueron verificadas **60/60 LISTO**. Son prerrequisitos de **preparación**, no una certificación de catálogo universal.

**A0:** análisis de archivos, blobs y autoridades; **A1:** cotejo positivo documental de flujos y criterios; **A2:** escenarios de seguridad A/B, RBAC, privacidad y negativos inventariados, NO_EJECUTADOS; **A3:** NO_APLICA pruebas runtime de .NET, MySQL, Playwright y HTTP para el diff sólo Markdown; **A4:** Scope Lock y procedencia V5 exact-SHA más diff y readback. **A5:** revisión por otra Task Javier; no autoaprobación.

Rollback documental por `git revert` normal y evento compensatorio nuevo en BITACORA, sin borrar historia ni intervenir QA/main/PROD. Se mantienen pendientes G01–G08 y cualquier implementación o certificación que requiera decisiones y pruebas futuras.
