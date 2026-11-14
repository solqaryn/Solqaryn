# Informe técnico — Catálogos de producto y estabilización integral

## 1. Resumen ejecutivo

La rama `Desarrollo` incorpora mantenimientos reutilizables de Colores, Tallas, Marcas y Modelos, normalización de Marca–Modelo, integración con Productos, Compras y Ventas, eliminación lógica correcta de Categorías, sesión basada en inactividad continua, estado visible `Agotado`, filtros avanzados de inventario, mejoras de contraste/iconos y nuevos indicadores financieros.

La solución mantiene compatibilidad con productos legados, conserva el historial de operaciones y no modifica ni despliega producción. La aceptación automatizada utiliza MySQL 8.4, API ASP.NET Core, Angular y Chromium efímeros.

El PR `Desarrollo -> main` debe permanecer en borrador. La aprobación aislada no reemplaza las pruebas externas con Aiven, Render, Cloudinary, Vercel, Gmail, WhatsApp y dispositivos reales.

## 2. Módulos modificados

- Productos e inventario.
- Categorías.
- Colores.
- Tallas o tamaños.
- Marcas.
- Modelos.
- Compras.
- Ventas.
- Dashboard.
- Finanzas.
- Autenticación y sesión.
- Roles, permisos y auditoría.
- Tema visual, diálogo de confirmación, iconos y layout.
- Migraciones y persistencia EF Core.
- CI, calidad estática y aceptación E2E.
- Documentación de ambientes y cierre.

## 3. Archivos principales creados

### Backend

- `backend/src/Domain/Entities/CatalogoProducto.cs`
- `backend/src/Domain/Enums/TipoCatalogoProducto.cs`
- `backend/src/Application/DTOs/CatalogoProductoDtos.cs`
- `backend/src/Application/Interfaces/ICatalogoProductoRepository.cs`
- `backend/src/Application/Interfaces/ICatalogoProductoService.cs`
- `backend/src/Application/Services/CatalogoProductoService.cs`
- `backend/src/Infrastructure/Repositories/CatalogoProductoRepository.cs`
- `backend/src/API/Controllers/CatalogoProductoControllerBase.cs`
- `backend/src/API/Controllers/ColoresController.cs`
- `backend/src/API/Controllers/TallasController.cs`
- `backend/src/API/Controllers/MarcasController.cs`
- `backend/src/API/Controllers/ModelosController.cs`
- `backend/src/Application/Common/ProductoPagedRequest.cs`
- migración `CatalogosProductoYCategoriaSoftDelete` y su Designer.
- pruebas unitarias de catálogos, categorías y sesión/JWT.

### Frontend

- `frontend/src/app/core/models/catalogo-producto.model.ts`
- `frontend/src/app/services/catalogo-producto.service.ts`
- `frontend/src/app/features/catalogos-producto/catalogo-producto-list.component.ts`
- `frontend/src/app/features/catalogos-producto/catalogo-producto-list.component.html`
- `frontend/src/app/features/catalogos-producto/catalogo-producto-list.component.scss`
- `frontend/src/app/core/auth/session-activity.service.ts`
- `frontend/scripts/lint-quality.mjs`
- `frontend/e2e/catalogos-mantenimientos.spec.ts`
- `frontend/e2e/productos-filtros.spec.ts`
- `frontend/e2e/sesion-inactividad.spec.ts`
- `frontend/e2e/matriz-modulos-visual.spec.ts`

### CI y documentación

- `.github/workflows/catalogos-aceptacion.yml`
- `docs/RESPONSABILIDADES_CIERRE_DESARROLLO.md`
- `docs/INFORME_TECNICO_CATALOGOS_ESTABILIDAD.md`

## 4. Archivos principales modificados

- `backend/src/Infrastructure/Persistence/AppDbContext.cs`
- `backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- `backend/src/Domain/Entities/Producto.cs`
- `backend/src/Domain/Entities/Categoria.cs`
- DTOs, mapeo, validadores, servicio y repositorio de Producto.
- servicio y repositorio de Categoría.
- `backend/src/API/Controllers/ProductosController.cs`
- controladores y configuración de autenticación.
- permisos, seeding y registro de dependencias.
- DTOs y servicios de Dashboard y Finanzas.
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/app.component.*`
- `frontend/src/styles.scss`
- formulario, listado y detalle de Productos.
- formularios y detalles de Compras y Ventas.
- listado de Categorías.
- Dashboard y Finanzas.
- login e interceptor/autenticación.
- `frontend/package.json`
- `.github/workflows/desarrollo-ci.yml`
- `docs/PLAN_CIERRE_SOLQARYN.md`

## 5. Cambios en base de datos

### Tabla `CatalogosProducto`

Almacena todos los catálogos reutilizables con:

- `Tipo`: Color, Talla, Marca o Modelo.
- `Nombre`, descripción, código visual y orden.
- estado activo y eliminación lógica.
- auditoría de creación, actualización y eliminación.
- `CatalogoPadreId` para relacionar Modelo con Marca.

### Tabla `Productos`

Nuevas relaciones opcionales:

- `ColorId`.
- `TallaId`.
- `MarcaId`.
- `ModeloId`.

Las claves foráneas usan `SetNull` para proteger el historial si una referencia deja de estar disponible. Los textos legados `Marca` y `Modelo` se conservan para compatibilidad y la migración crea/relaciona catálogos a partir de sus valores existentes.

### Tabla `Categorias`

Se agregaron:

- `Eliminada`.
- `FechaEliminacion`.
- `EliminadaPorUsuarioId`.

Las consultas excluyen categorías eliminadas, pero permiten mantener categorías inactivas visibles cuando corresponde.

## 6. Endpoints nuevos

Cada catálogo expone el mismo contrato:

- `GET /colores`, `/tallas`, `/marcas`, `/modelos`
- `GET /{catalogo}/activos` o `/marcas/activas`
- `GET /{catalogo}/{id}`
- `POST /{catalogo}`
- `PUT /{catalogo}/{id}`
- `PATCH /{catalogo}/{id}/activar`
- `PATCH /{catalogo}/{id}/desactivar`
- `DELETE /{catalogo}/{id}`

Modelos admite `marcaId` para listar o seleccionar los modelos de una Marca.

`GET /productos` admite adicionalmente:

- `categoriaId`
- `colorId`
- `tallaId`
- `marcaId`
- `modeloId`
- `activo`
- `agotado`

Los filtros se combinan con búsqueda, ordenamiento y paginación.

## 7. Componentes y servicios nuevos

- Componente administrativo genérico para los cuatro catálogos.
- Servicio Angular único para todos los catálogos.
- Servicio backend único con reglas específicas por tipo.
- Controlador base reutilizable.
- Servicio de sesión por actividad.
- Panel de filtros dependientes de Productos.
- Validación estática reproducible sin dependencias adicionales.

## 8. Reglas de negocio implementadas

- Un Modelo siempre pertenece a una Marca.
- No puede seleccionarse un Modelo de una Marca diferente.
- No se ofrece un catálogo inactivo para nuevos Productos.
- No puede activarse un Modelo cuya Marca esté inactiva.
- No puede desactivarse una Marca mientras tenga Modelos activos.
- No puede eliminarse una Marca mientras tenga Modelos asociados.
- El código visual de Color usa formato `#RRGGBB`.
- Eliminar un catálogo o una Categoría es una eliminación lógica.
- Los registros históricos conservan sus textos y relaciones.
- `Agotado` significa existencia menor o igual a cero.
- El token no caduca por un reloj fijo de treinta minutos mientras haya actividad; la sesión se cierra tras treinta minutos continuos sin actividad.

## 9. Calidad del código

- Lógica común centralizada para evitar cuatro CRUD duplicados.
- DTOs y servicios tipados.
- Validaciones de negocio en la capa Application.
- Persistencia y consultas en Infrastructure.
- Endpoints protegidos por permiso exacto.
- Componentes standalone y servicios reutilizables.
- Lint con TypeScript `--noEmit` y revisión de trazas temporales, debugger, conflictos y URLs inseguras.
- CI bloquea temporales, errores de compilación y desalineación de migraciones.

## 10. Validaciones automatizadas

### Backend y base de datos

- restauración y compilación Release;
- pruebas unitarias;
- migración completa sobre MySQL 8.4 descartable;
- conversión de Marca/Modelo legados;
- verificación de `__EFMigrationsHistory`;
- modelo y snapshot sin cambios pendientes;
- SQL forward sin `DROP TABLE`, `TRUNCATE` ni eliminación masiva;
- construcción Docker real.

### Frontend

- validación TypeScript;
- calidad estática;
- build de producción;
- navegación de módulos administrativos;
- captura de errores de consola y `pageerror`;
- ausencia de desbordamiento horizontal representativo;
- iconos visibles;
- contraste representativo en paletas clara y oscura.

### Funcionales E2E

- CRUD, búsqueda y estados de los cuatro catálogos;
- Marca–Modelo y restricciones de integridad;
- Producto con Marca, Modelo, Color y Talla;
- filtros por catálogos y agotado;
- estado `Agotado`;
- eliminación y recarga de Categorías;
- botón destructivo visible y contrastado;
- sesión activa e inactiva;
- actividad compartida entre pestañas;
- renovación de token conservando una venta en curso;
- falla temporal de red durante renovación;
- Administrador, Vendedor y rol personalizado;
- permisos y aislamiento de datos;
- perfil, contraseña, venta, factura, PDF y enlaces públicos.

## 11. Riesgos identificados

- La migración debe probarse primero en Aiven Desarrollo; no debe aplicarse directamente a producción.
- Un fork con datos reales exige control de acceso y anonimización.
- Cloudinary necesita separación real o el prefijo `desarrollo/` correctamente configurado.
- Gmail, WhatsApp y PDF requieren pruebas reales externas.
- Los temas configurables pueden recibir combinaciones extremas; la interfaz incluye defensas, pero se requiere revisión manual del tema elegido por el negocio.
- Una certificación aislada no prueba latencia, límites o indisponibilidad de proveedores externos.
- El cambio acumulado respecto de `main` es amplio y exige plan de rollback antes de producción.

## 12. Pendientes externos

Requieren acceso del propietario o un operador autorizado:

1. Crear Aiven Desarrollo.
2. Configurar Cloudinary Desarrollo.
3. Crear Render Desarrollo y cargar secretos no productivos.
4. Aplicar migraciones únicamente en Aiven Desarrollo.
5. Crear Vercel Desarrollo.
6. Probar Gmail/SMTP real.
7. Probar WhatsApp desde teléfono real.
8. Revisar PDF, impresión y descarga reales.
9. Revisar teléfonos y tabletas físicos.
10. Preparar respaldo, ventana y rollback productivos.

El procedimiento exacto y la evidencia exigida están en `docs/RESPONSABILIDADES_CIERRE_DESARROLLO.md`.

## 13. Posibles mejoras futuras

- Catálogos adicionales configurables usando el mismo patrón.
- Variantes/SKU independientes cuando un mismo producto necesite múltiples combinaciones de color y talla con stock separado.
- Exportación específica de inventario filtrado.
- Auditoría automática WCAG con una herramienta especializada en CI.
- Pruebas de carga y resiliencia contra servicios de Desarrollo.
- Datos sintéticos anonimizados para demostraciones y aceptación.

## 14. Criterio de entrega

El código se considera técnicamente listo para validación externa cuando los workflows `Desarrollo - Compilación y pruebas` y `Desarrollo - aceptación funcional integral` terminan en verde sobre el mismo estado de `Desarrollo`.

No se considera listo para producción hasta completar los pendientes externos, cerrar defectos críticos/altos, preparar rollback y obtener autorización expresa de Javier Mejía.
