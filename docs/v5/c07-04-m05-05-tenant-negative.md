# C07-04.M05.05 — pruebas negativas de tenant y permisos de pedidos (DEV01)

**Riesgo R2.** Objetivo: probar que el filtro de autorización existente de `pedidos-venta` no ejecuta la acción comercial antes de validar el selector de empresa y la denegación explícita de la matriz de permisos. Cambios limitados a test xUnit, CI dirigida, documento y CHANGELOG. Ningún cambio de código runtime, endpoints, JWT, persistencia, inventario, transacciones, pagos o producción.

## A0: límite y contratos reales
La unidad C07-04.M05.05 corresponde a caso negativo de seguridad, aislamiento o abuso sobre pedidos y pagos. El test ejecuta `RequierePermisoAttribute.OnActionExecutionAsync`, que invoca `TenantPermissionContext.RequireEmpresaIdAsync` y `IPermisoService.VerificarPermisoAsync`. Dependencias: `PedidosVentaController` con atributos `Authorize` y `RequierePermiso(Ventas, Crear)` ya cubiertos por M05.02, sin modificar sus contratos. Rollback mediante git revert fast-forward, sin force-push.

## A1/A2: aserciones que detectan regresiones
Se comprueban cinco selectores inválidos (cero, negativo, texto, múltiples en un solo valor, vacío), dos cabeceras `X-Empresa-Id` simultáneas y la ausencia de membresía tenant única. Todos deben lanzar `ForbiddenAccessException` antes de consultar permisos o invocar `next()`. Un selector de empresa diferente con grant denegado debe impedir la acción. Control positivo: el mock del permiso efectivo autoriza exactamente un tenant 71 y permite una única invocación de `next()`. Si se relaja la validación del filtro, los mocks strict o los contadores adversariales deben fallar.

**Limitación material:** es una prueba in-process del filtro real. No se emite JWT firmado ni se ejecutan middleware HTTP, políticas de autorización completas, membresía real desde EF/MySQL, revocación efectiva, BOLA/IDOR sobre registros comerciales ni conciliación financiera. El mock de `IPermisoService` no acredita grants reales.

## A3/A4/A5 y estado
GitHub-hosted .NET 10 ejecuta Release xUnit dirigido y Scope Lock exact-SHA, junto con checks institucionales pertinentes. Verificar `completed/success` y logs antes de declarar PASS; `queued`, `in_progress`, `failure`, `cancelled` y `skipped` nunca son PASS de pruebas requeridas. Mantener en VALIDANDO hasta alcanzar A0–A4 del alcance completo (incluida autorización HTTP JWT de tenant A/B, flujo de pedidos, rollback/idempotencia y evidencia fiscal pertinente). A5 corresponde a otra Task Javier distinta de DEV01, con lectura independiente. Mantener PROJECT_SCOPE_LOCK=STRICT, dev únicamente; sin QA/main/PROD, secretos ni datos reales.
