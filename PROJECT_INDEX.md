# PROJECT_INDEX — Solqaryn

Índice operativo del repositorio. Su objetivo es llevar al equipo directamente al área correcta sin reindexar todo el proyecto.

## Lectura inicial mínima

Regla: navegar por estado y arquitectura vigentes. No abrir planes, fases o documentos no vigentes como requisito de trabajo salvo que el MAESTRO actual los cite expresamente.


1. `AGENTS.md` — reglas obligatorias y gate de identidad.
2. `PROJECT_CONTEXT.md` — contexto técnico base e identidad `SOLQARYN`.
3. `TASKS.md` — trabajo pendiente/vigente.
4. última entrada relevante de `CHANGELOG_AI.md` — continuidad entre agentes.
5. `PROJECT_INDEX.md` — localizar archivos.
6. `ARCHITECTURE.md` — solo cuando la tarea implique arquitectura, dependencias transversales o diseño.

No leer todos los documentos administrativos en cada tarea. Consultarlos solo cuando sean relevantes.

## Raíz

- `AGENTS.md`: reglas obligatorias para cualquier agente.
- `PROJECT_CONTEXT.md`: memoria técnica canónica e identidad inequívoca del proyecto.
- `PROJECT_INDEX.md`: este índice.
- `ARCHITECTURE.md`: arquitectura y patrones.
- `ARCHITECTURE_CHANGELOG.md`: registro breve de cambios que alteran el mapa técnico.
- `TASKS.md`: pendientes operativos.
- `CHANGELOG_AI.md`: bitácora/evidencia de cada changeset.
- `README.md`: introducción, stack y operación básica.
- `CONTRIBUTING.md`: flujo Git y criterios de contribución.
- `render.yaml`: configuración versionada relacionada con Render; tratar con cautela por separación de entornos.
- `docs/ENTORNOS_DEV_PROD.md`: fuente canónica para GitHub Environments y topología Aiven DEV/PROD (`DEV`, `PROD`, `solqaryn-mysql`, bases/usuarios aislados).

## Backend

`backend/InventoryApp.sln` — solución .NET.

### `backend/src/Domain`

Entidades, enums y elementos puros de dominio. No debe depender de Infrastructure ni API.

### `backend/src/Application`

Casos de uso, DTO, interfaces, servicios, validadores y contratos. Puede depender de Domain, no de detalles concretos de infraestructura.

### `backend/src/Infrastructure`

EF Core, `AppDbContext`, repositorios, configuraciones, migraciones, Cloudinary, SMTP, QuestPDF y demás adaptadores concretos.

### `backend/src/API`

Controladores, middleware, filtros, configuración HTTP, autenticación/autorización, DI y arranque. `Program.cs` es el composition root.

Performance DEV: `Observability/RequestPerformanceContext.cs`, `Observability/DbQueryTimingInterceptor.cs` y `Middleware/RequestObservabilityMiddleware.cs`; contrato y procedimiento en `docs/PERFORMANCE_BASELINE_DEV.md`.

Storefront de productos: `TiendaController.cs` expone `bootstrap`, listado/detalle/contexto. La carga inicial vive en `Application/Services/TiendaBootstrapService.cs` + `DTOs/TiendaBootstrapDto.cs`; listados/destacados usan `TiendaProductoResumenDto` + `ProductoCatalogoResumenReadModel.cs`, el detalle/contexto conservan `ProductoCatalogoPublicoDto`; la resolución pública de WhatsApp vive en `Infrastructure/Services/WhatsAppPublicoService.cs`; las proyecciones de catálogo EF viven en `Infrastructure/Repositories/ProductoCatalogoPublicoRepository.cs`.

### `backend/tests`

Pruebas backend. Ejecutar pruebas dirigidas para cambios localizados y suite completa en cierres/cambios transversales.

## Frontend

### `frontend/src/app/core`

Autenticación, guards, interceptores, modelos y utilidades transversales. El baseline browser DEV vive en `core/performance/performance-baseline.service.ts`.

### `frontend/src/app/features`

Pantallas/módulos funcionales: productos, variantes, catálogos, compras, ventas, facturas, inventario, finanzas, usuarios, roles, permisos, auditoría, etc.

El storefront VariStoreHN usa `varistorehn.service.ts`: `obtenerBootstrap()` comparte/deduplica la carga inicial; el catálogo continúa paginado y `obtenerProductosContexto` rehidrata únicamente referencias del carrito/cuenta, evitando descargas completas.

### `frontend/src/app/services`

Clientes HTTP y servicios compartidos.

### `frontend/src/app/app.routes.ts`

Mapa principal de rutas y permisos. Revisarlo únicamente cuando una tarea afecte navegación, lazy loading o autorización por ruta.

### `frontend/e2e`

Pruebas Playwright para flujos críticos.

## Documentación

### `docs/`

Documentación funcional, técnica y guías complementarias de estado vigente.

Administración colaborativa central:

- `docs/COLABORACION_IA.md`
- `docs/COLABORATIVO.md`
- `docs/ENTORNOS_DEV_PROD.md`

No cargar toda la carpeta `docs` por defecto. Abrir únicamente el documento asociado al punto en ejecución.

## Automatización local de colaboración

- `scripts/iniciar-sesion-ia.ps1`: gate read-only de identidad, rama, HEAD, divergencia y estado del checkout. Ejecutarlo al inicio de cada conversación/sesión local.
- `scripts/configurar-colaboracion.ps1`: configuración inicial/sincronización del flujo local y hooks.
- `.githooks/pre-commit`: bloquea commits locales si repo/rama son incorrectos o falta `CHANGELOG_AI.md` en el changeset.
- `.githooks/post-commit`: publica commits de `dev` solo si `origin` pertenece realmente a Solqaryn.

## CI

- `.github/workflows/`: CI y automatizaciones versionadas.
- Para cambios puramente administrativos/locales puede utilizarse `[skip ci]` bajo las condiciones estrictas de `AGENTS.md`.
- Los workflows y triggers solo deben modificarse con inspección dirigida; no desactivar validaciones funcionales para ahorrar tiempo.

## Regla de navegación dirigida

Para una tarea normal:

1. confirmar PROJECT_ID/repo/rama;
2. identificar módulo;
3. abrir archivo de entrada;
4. seguir únicamente imports/interfaces/repositorios/DTO directamente relacionados;
5. buscar símbolos concretos;
6. detener expansión cuando exista evidencia suficiente.

No listar recursivamente todo `backend`, `frontend` o `docs` salvo cambio estructural justificado.

## Índice de decisión para cambios frecuentes

| Petición | Abrir primero | Seguir solo si aplica |
| --- | --- | --- |
| Endpoint o contrato HTTP | `backend/src/API/Controllers/<Area>Controller.cs` | DTO, interfaz y servicio homónimos en `Application`; cliente en `frontend/src/app/services` |
| Regla de negocio | `backend/src/Application/Services/<Area>Service.cs` | interfaz/DTO/validator; entidad o enum en `Domain` |
| Tabla, relación o índice | entidad en `backend/src/Domain/Entities` + `backend/src/Infrastructure/Persistence/AppDbContext.cs` | `Persistence/Configurations`, `Migrations` y scripts SQL de `backend/scripts` |
| Consulta o persistencia | `backend/src/Infrastructure/Repositories/<Area>Repository.cs` | interfaz en `Application/Interfaces` y configuración EF correspondiente |
| Pantalla o formulario | `frontend/src/app/features/<area>` | servicio HTTP en `frontend/src/app/services` y modelos compartidos |
| Ruta, menú o permiso visual | `frontend/src/app/app.routes.ts` o `features/**/**.routes.ts` | `core/guards`, `core/navigation` y permiso del endpoint backend |
| Login, JWT o permisos | `AuthController.cs`, `Program.cs` y servicios Auth/RBAC | `core/auth`, `core/guards`, interceptor y pruebas de acceso |
| Imagen, PDF, correo o exportación | interfaz en `Application/Interfaces` | implementación en `Infrastructure/Services` y registro DI en `Program.cs` |
| Variable o entorno | `backend/src/API/appsettings*.json`, `frontend/src/environments`, `frontend/vercel.json`, `render.yaml` | `docs/ENTORNOS_DEV_PROD.md`; nunca copiar secretos |
| Prueba localizada | `backend/tests/InventoryApp.Tests` o `frontend/e2e` | workflow específico en `.github/workflows` |

## Puntos de entrada, API y datos

- Backend: `backend/src/API/Program.cs`; controladores bajo `backend/src/API/Controllers`. La mayoría declara una base con `[Route("...")]`; salud se expone directamente como `/health` y `/health/ready`.
- Frontend: `frontend/src/main.ts` -> `frontend/src/app/app.config.ts` -> `frontend/src/app/app.routes.ts`. Algunas áreas agregan rutas en archivos `*.routes.ts` dentro de su feature.
- Datos: `backend/src/Infrastructure/Persistence/AppDbContext.cs` y `Persistence/Configurations`. Existen migraciones en `backend/src/Infrastructure/Migrations` y `backend/src/Infrastructure/Persistence/Migrations`; inspeccionar ambas ubicaciones y no moverlas ni consolidarlas desde un cambio local.
- Dependencias: proyectos `backend/src/*/*.csproj`, solución `backend/InventoryApp.sln`, `frontend/package.json` y `frontend/angular.json`.

## Mapa operativo por capas

```text
Angular route/component
  -> frontend/src/app/services o core/services
  -> API Controller ([Route]/[Http*])
  -> Application Service + DTO/validator/interface
  -> Domain entity/enum/invariant
  -> Infrastructure Repository/Service
  -> AppDbContext + Configuration + MySQL
```

- Angular: `frontend/src/app/features`, `core`, `services` y `app.routes.ts`.
- API: `backend/src/API/Controllers`; composición, middleware y DI en `backend/src/API/Program.cs`.
- Application: `backend/src/Application/{Services,Interfaces,DTOs,Validators}`.
- Domain: `backend/src/Domain/{Entities,Enums,Common}`.
- Infrastructure/DB: `backend/src/Infrastructure/{Repositories,Services,Persistence}` y las dos carpetas de migraciones indicadas arriba.
- Integraciones: contratos en `Application/Interfaces`, adaptadores concretos en `Infrastructure/Services` y registro DI en `Program.cs` (Cloudinary, QuestPDF, SMTP y exportaciones).

## Mapa por dominio

Las rutas mostradas son bases de navegación/API. Para una operación concreta, abrir el archivo citado y localizar su `[Http*]`, DTO o método; no cargar el dominio completo.

| Dominio | Angular / ruta | API y Application | Domain, Infrastructure y migraciones ancla |
| --- | --- | --- | --- |
| Autenticación | `features/login`; `/login`; `core/auth`, `core/guards`, `core/interceptors` | `AuthController` (`auth`); `AuthService`; `IJwtService` | `Usuario`; `UsuarioRepository`; `Infrastructure/Services/JwtService.cs`; `20260720101639_Fases18SesionConfigPermisosCalculos` |
| Usuarios | `features/usuarios`; `/usuarios` | `UsuariosController` (`usuarios`); `UsuarioService` | `Usuario`; `UsuarioRepository`; migraciones `Fase1UsuariosCategoriasImagenes` y `Fases11_17TemaFacturasUsuarios` |
| Roles y permisos | `features/roles`, `features/permisos`; `/roles`, `/permisos` | `RolesController`, `PermisosController`; `RolService`, `PermisoService`, `PermisoCatalogoService` | `Rol`, `Permiso`, `RolPermiso`; repositorios homónimos; `AddRolPermisos`, `Fases1_10RolesDescuentosImpuestos`, `N0_4_ConsolidarRbacRelacional` |
| Productos, categorías e imágenes | `features/productos`, `categorias`, `catalogos-producto`; `/productos`, `/categorias`, `/marcas`, `/modelos`, `/tallas`, `/colores` | `ProductosController`, `ProductoVariantesController`, `CategoriasController` y controladores de catálogo; servicios `Producto*`, `CategoriaService`, `CatalogoProductoService` | `Producto`, `ProductoVariante`, `ProductoImagen`, `Categoria`; repositorios homónimos; adaptadores Cloudinary; migraciones `CatalogosProductoYCategoriaSoftDelete`, `M1NormalizarMaestrosProducto`, `M2VariantesMultidimensionales`, `M2ImagenesPorVariante`, `N0_2_*`, `N0_3_*` |
| Proveedores | `features/proveedores`; `/proveedores` | `ProveedoresController` (`proveedores`); `ProveedorService` | `Proveedor`; `ProveedorRepository`; `20260711022435_AddProveedores` y migraciones N2 de evaluación/documentos cuando aplique |
| Clientes | `features/clientes`, `tipo-clientes`; `/clientes`, `/tipo-clientes` | `ClientesController`, `TipoClientesController`; `ClienteService`, `TipoClienteService` | `Cliente`, `TipoCliente`; repositorios homónimos; `AddClientes`, `AddTipoCliente`, `AddTipoClientePredeterminadoUnico` |
| Compras | `features/compras`, `solicitudes-compra`, `ordenes-compra`, `recepciones-compra`; rutas `/compras`, `/solicitudes-compra`, `/ordenes-compra`, `/recepciones-compra` | controladores `Compras`, `SolicitudesCompra`, `OrdenesCompra`, `RecepcionesCompra`, `FacturasProveedor`, `DevolucionesProveedor`, `NotasCreditoProveedor`; servicios homónimos y `ThreeWayMatchService` | entidades/repositorios `Compra*`, `SolicitudCompra*`, `OrdenCompra*`, `RecepcionCompra*`, `FacturaProveedor*`; migraciones `Fase2MovimientosCompras`, `N2_1_*` a `N2_9_*` repartidas entre ambas carpetas históricas |
| Ventas y facturas | `features/ventas`, `facturas`, `cotizaciones`, `pedidos-venta`; `/ventas`, `/facturas/:id`, rutas feature de cotizaciones/pedidos | `VentasController` (`ventas`), `FacturasController` (`facturas`), `CotizacionesController`, `PedidosVentaController`; `VentaService`, `FacturaService`, `CotizacionService`, `PedidoVentaService` | entidades/repositorios homónimos; `QuestPdfFacturaService`; migraciones `Fase3Fase4VentasFacturacionFinanzas`, `Fase8FacturacionPagosCostosEnvioVariantes`, `N3_1_CotizacionPersistencia`, `N3_2_PedidoVentaPersistencia`, `N3_3_C_PedidoVentaReservaInventario` |
| Inventario | `features/inventario`, `almacenes`, `sucursales`, `ubicaciones-almacen`; `/inventario/*` y rutas feature | controladores `AjustesInventario`, `MovimientosInventario`, `ExistenciasVariante`, `ReservasInventario`, `TransferenciasInventario`, `ConteosInventario`, `CosteoInventario`, `Almacenes`, `Sucursales`, `UbicacionesAlmacen`; servicios homónimos | entidades/repositorios de existencia, movimiento, ajuste, reserva, transferencia, conteo, costo y ubicación; migraciones `N0_6_*`, `N0_7_*`, `N1_1_*` a `N1_10_*` |
| Finanzas | `features/finanzas`; `/finanzas` y ruta feature `/cuentas-por-pagar` | `FinanzasController` (`finanzas`), `CuentasPorPagarController` (`cuentas-por-pagar`); `FinanzasService`, `CuentaPorPagarService` | `MovimientoFinanciero`, `RevisionFinanciera`, `CuentaPorPagar`; repositorios correspondientes; `Fase3Fase4VentasFacturacionFinanzas`, `N2_8_CuentasPorPagarPersistencia` |
| Descuentos e impuestos | `features/descuentos`, `impuestos`; `/descuentos`, `/impuestos` | `DescuentosController`, `ImpuestosController`; `DescuentoService`, `ImpuestoService` | `Descuento`, `Impuesto`; repositorios homónimos; `Fases1_10RolesDescuentosImpuestos` |
| Auditoría | `features/auditoria`; `/auditoria` | `AuditoriaController` (`auditoria`); `AuditoriaService` | `RegistroAuditoria`; `AuditoriaRepository`; `Fase9AuditoriaCentralizada` |
| Configuración | `features/configuracion`; `/configuracion` | `EmpresaConfiguracionController` (`empresa-configuracion`); `EmpresaConfiguracionService` | `EmpresaConfiguracion`; repositorio homónimo; migraciones transversales `Fases18SesionConfigPermisosCalculos` y `M12AutomatizacionTransversal` |
| Tema visual | configuración y servicios `tema-visual.service.ts`, `theme-applier.service.ts`; entrada desde `/configuracion` | `TemaVisualController` (`tema-visual`); `TemaVisualService` | `TemaVisual`; `TemaVisualRepository`; `Fases11_17TemaFacturasUsuarios` |
| Perfil | `features/perfil`; `/perfil` | `PerfilController` (`perfil`); `PerfilService` | usa `Usuario` y almacenamiento de imagen mediante `IPerfilImagenStorageService`/`CloudinaryPerfilImagenStorageService`; `Fase6SeguridadFacturacionPerfil` |

Los nombres de migración son anclas de búsqueda, no una lista exhaustiva ni autorización para aplicarlas. Confirmar siempre `AppDbContextModelSnapshot` y referencias al símbolo afectado.

## Flujos transversales

- Sesión y permiso: route guard/interceptor -> `AuthController` o controlador funcional -> autorización/servicio -> auditoría.
- Escritura transaccional: componente -> servicio HTTP -> controlador -> servicio Application -> repositorio/`IUnitOfWork` -> `AppDbContext`.
- Inventario comercial: compra/recepción o venta/pedido -> servicio funcional -> existencia/reserva/kardex -> movimiento y trazabilidad; revisar ambos dominios solo si el cambio cruza esa frontera.
- Facturación: venta/documento -> `FacturaService` -> repositorios -> QuestPDF; correo y enlaces públicos pasan por sus interfaces/adaptadores.
- Multimedia: UI multipart -> controlador/servicio -> interfaz de almacenamiento -> Cloudinary; las reglas de entorno permanecen en `docs/ENTORNOS_DEV_PROD.md`.
- Errores y observabilidad: `Program.cs` -> `CorrelationIdMiddleware` -> `ExceptionHandlingMiddleware`; filtros y health checks viven en API.

## Alcance: qué no volver a inspeccionar

Ante un cambio local no volver a leer toda la solución, todos los controladores, todas las migraciones, todo `docs` ni todos los tests. Abrir solo la fila del dominio, el punto de entrada y sus dependencias directas. Ampliar el alcance únicamente si:

- cambia un contrato compartido entre Angular y API;
- cambia entidad, relación, transacción o migración;
- afecta autenticación, permisos, auditoría, configuración o integración transversal;
- el símbolo aparece en varios dominios o la documentación contradice el código;
- el cambio es arquitectónico según `ARCHITECTURE.md`.

Para CSS, texto, validación de formulario o CRUD localizado, no inspeccionar migraciones ni dominios vecinos sin evidencia de dependencia. Para un endpoint localizado, no recorrer todos los controladores: seguir Controller -> DTO/Service -> Interface/Repository -> Entity/Configuration -> prueba dirigida.

## Comandos verificados

Desde `backend`:

```powershell
dotnet restore InventoryApp.sln
dotnet build InventoryApp.sln --configuration Release
dotnet test InventoryApp.sln --configuration Release
dotnet run --project src/API/InventoryApp.API.csproj
```

Desde `frontend`:

```powershell
npm ci
npm start
npm run lint
npm run build:prod
npm test
npx playwright test
```

Los scripts `start`, `lint`, `build:prod` y `test` existen en `frontend/package.json`; Playwright está declarado y `frontend/playwright.config.ts` existe.

## Convención de mantenimiento

Cuando cambie una capa, módulo, integración, ruta/API, modelo de datos o comando:

1. actualizar la fila o sección afectada de este mapa;
2. actualizar `PROJECT_CONTEXT.md` o `ARCHITECTURE.md` solo si cambia la realidad transversal;
3. agregar una entrada fechada a `ARCHITECTURE_CHANGELOG.md` con alcance, rutas y verificación;
4. registrar el changeset normal en `CHANGELOG_AI.md`.

No registrar en el changelog arquitectónico correcciones internas que no cambien este mapa.
