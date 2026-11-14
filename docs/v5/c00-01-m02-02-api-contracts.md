# C00-01.M02.02 — Contratos API, DTO, errores, validaciones y compatibilidad

**Proyecto:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Plan V5 FINAL:** Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, microtarea `C00-01.M02.02`. **Autor:** Javier PRIMARY DEV 05, TASK_ID `6adc2e55f47c81919f6374e561687076`. Fuente: archivos reales del HEAD de lectura de dev `c099819831c1332492544064e2eacc4b62e08a5d`.

## Límite de esta unidad

Es una definición documental de contratos ya presentes en API/Application; **no agrega endpoint, DTO, validator, servicio, proveedor, tabla, migración, regla RBAC ni permiso**. Los nombres y hashes de blob permiten contraste posterior con GitHub. El frontend no decide autoridad tenant: todo tenant A/B se valida en backend, y un `EmpresaId` enviado por cliente no concede acceso.

## Matriz del contrato observable

| Superficie | Fuente original (Git blob) | Comportamiento y compatibilidad documentados | Caso de denegación, error o límite |
| --- | --- | --- | --- |
| Empresas API | `backend/src/API/Controllers/EmpresasController.cs` (`cfedc20236e71db079529442ebf45b0431d339e2`) | `[ApiController]`, `[Authorize]`, ruta `empresas`, GET listado y detalle, POST, PUT, PATCH activar/desactivar; salida `ApiResponse<EmpresaDto>` | `ExigirEmpresaAutorizada` rechaza ID fuera de `TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext)`; GET detalle no encontrado retorna 404; rol mediante `RequierePermiso` |
| Empresa DTO | `backend/src/Application/DTOs/EmpresaDto.cs` (`bbca706d08d5a9738798af14ca3bb99185b18a40`) | `EmpresaDto`: Id, Nombre, Activa, fechas; `CreateEmpresaDto` y `UpdateEmpresaDto` contienen Nombre | DTO no prueba por sí solo autorización; no interpretar `CreateEmpresaDto.Nombre` como token ni identificador tenant autenticado |
| Tenant selector | `backend/src/API/Controllers/TenantContextController.cs` (`2407c2eb6779ed724e841c688ee14dabbf163171`) | `GET tenant-context/empresas` lista membresías permitidas; `GET tenant-context/{empresaId:int}` consulta el servicio de ámbito | EmpresaId <= 0 => 400; sin membresía activa => `Forbid()` sin filtrar existencia cross-tenant; cliente no suministra autoridad |
| Producto API | `backend/src/API/Controllers/ProductosController.cs` (`8b9e1f0600dd7ffe255552f237688ff5a8367fad`) | `[Authorize]`, ruta `productos`, acciones decoradas `RequierePermiso`, creación/actualización con `ExecuteInTransactionAsync` para producto + variantes; respuestas `ApiResponse` | Producto inexistente => 404; no saltar `RequierePermiso`, transacciones, validaciones ni filtros por tenant |
| Producto DTO | `backend/src/Application/DTOs/CreateProductoDto.cs` (`9a3059da790647d81f89d9ff8169ac30da068210`) | `CreateProductoDto` conserva Marca/Modelo legacy opcionales; `Variantes` es la autoridad de SKU/barcode/stock/precio; lista `Imagenes` opcional | Campos legacy de cantidad/precio no reintroducen autoridad sobre variantes; fotos limitadas por validator |
| Producto validator | `backend/src/Application/Validators/CreateProductoValidator.cs` (`4453155f8ac7a840d55a331c0989c637b8cbf5da`) | FluentValidation: Nombre obligatorio <=150; campos legacy si no hay variantes; cantidades no negativas; precio positivo, SKU/barcode longitudes | Cuando hay variantes, validar cada variante; imágenes solo formatos JPG/JPEG/PNG/WEBP y máximo 10 MB; no aceptar HTML/JS arbitrario |
| Login validator | `backend/src/Application/Validators/LoginRequestValidator.cs` (`253fbd19c7543d5c8c3f0c851e0a5a97a4c81bdd`) | `LoginRequestDto` exige NombreUsuario y Password no vacíos | No registrar Password ni PasswordHash; el validator no reemplaza JWT/BCrypt ni comprobación del backend |
| Errores centralizados | `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs` (`44595eb1e8505446b0c6c34286577f8b9da99323`) | `ValidationException` => 400; `ForbiddenAccessException` => 403; `ResourceNotFoundException` => 404; `ConflictException` y error concurrencia => 409; respuestas `ApiResponse`/ProblemDetails | No exponer detalles de infraestructura ni datos ajenos; 409 no equivale a autorización; verificar middleware ejecutado por pruebas de integración reales cuando se cambie |

## Pruebas de aceptación y compatibilidad

1. **Positivo documental:** las ocho superficies y sus hashes de blob verificables están en GitHub; nombres de controladores, tipos, filtros y DTO corresponden al código de lectura.
2. **Negativo de tenancy:** tenant A no obtiene recurso tenant B por `empresaId`, header, slug, host ni `EmpresaId` de cliente; esta unidad documenta el rechazo, no lo prueba por HTTP.
3. **Negativo de validación:** valores inválidos, cantidades negativas, precio <= 0, contraseña vacía y archivos no permitidos no deben aceptarse. No inferir desde documento que todas las rutas están efectivamente protegidas.
4. **Negativo de errores:** no devolver secretos, PII ni stacktrace; preservar códigos 400/403/404/409 de las excepciones citadas. No omitir el caso `Forbid()`.
5. **Compatibilidad:** DTO legacy de Producto no concede autoridad operativa ni permite mezclar variantes; cualquier cambio futuro necesita análisis de consumidores Angular, DTO, API, servicio y regresión real.

## Auditoría, rollback y A5

A0–A3 de esta unidad cubren únicamente documento y prueba Node 22 dirigida: calidad/secretos, aceptación y negativos de contrato documental, integración proporcional doc/test. **No prueban .NET HTTP, JwtBearer real, BCrypt, MySQL, EF, Angular/Playwright ni aislamiento tenant A/B en ejecución.** Para modificar ejecución hay que implementar y probar tales superficies en una microtarea distinta. `PLAN_MAESTRO!H` es autoridad de estados; `COLA!E/G/O` son fórmulas; `BITACORA` apendicular. Tras VALIDANDO y PASS de A0–A3, commit directo fast-forward `dev`; A4 verifica SHA exacto, CI, diff, hashes, pruebas postdev y readback. Registrar `A5_PENDIENTE` para otro TASK_ID; `A5_PASS` independiente es condición técnica previa a QA/main/PROD, nunca autopromoción. Rollback `git revert` normal aditivo después de revalidar HEAD, sin force-push ni borrar historia. Imágenes históricas cero: migración pasada `NO_APLICA`; archivos futuros requieren validación separada.
