# N5.3.A.3 — Preflight QA de UX/Frontend de Reportes de Ventas

## Alcance
Inspección documental (read-only) de la superficie del frontend actual relacionada con ventas y el centro de reportes, para identificar componentes reutilizables, comportamientos de UX (estados de carga, error, vacío, exportación, A11Y) y las necesidades para implementar los reportes de ventas (fase N5.3).

## Superficie observable

### 1. Centro de Reportes
- El componente `CentroReportesComponent` y su plantilla HTML existen como un shell (tabs/navegación) para alojar reportes.
- **Rutas actuales:** `/centro-reportes` cuenta con sub-rutas para `financieros`, `administrativos`, `inventario/valorizacion`, `inventario/kardex`, `inventario/stock-health` e `inventario/reconciliacion`.
- **Falta ruta de ventas:** Aún no existe la ruta ni la navegación para reportes de ventas (ej. `/centro-reportes/ventas`).
- La seguridad en las rutas del centro se basa en `authGuard` y `permisoGuard`.

### 2. Componentes y UX de Ventas y Reportes
- **Ventas List:** Existe `ventas-list.component.ts/html` (`/ventas`), el cual muestra cómo se consultan y despliegan las ventas.
  - Cuenta con un `mat-form-field` para búsqueda por número o cliente.
  - Tiene una tabla para desktop (`table-desktop`) y tarjetas para mobile (`cards-mobile`).
  - Utiliza `mat-paginator` para la paginación con tamaño de página configurable (10, 25, 50).
  - Incluye capacidades de ordenamiento (ej. `ordenarPor('Fecha')`, `ClienteNombre`, `Total`).
- **Estados de UX detectados (basados en kardex, reportes administrativos y ventas):**
  - *Loading:* Se utiliza `mat-spinner` mientras se cargan los datos.
  - *Empty:* Mensajes claros (ej. `<td colspan="9" class="empty">No hay ventas registradas.</td>` o advertencias de filtros sin resultados).
  - *Errores:* Se capturan en la suscripción a los observables y se muestran como mensajes de alerta (ej. banners rojos).
  - *Exportación:* El componente `reportes-administrativos.component.ts` posee métodos para exportar a CSV o XLSX invocando `service.exportar()`. `ventas-list` actualmente no posee exportación.

### 3. Filtros y Dimensiones para N5.3
- El listado de ventas actual posee filtros simples (búsqueda general).
- **Gaps para los reportes N5.3:** Un reporte de ventas analítico requerirá múltiples dimensiones (rango de fechas desde/hasta, usuario/vendedor, sucursal, cliente, categorías y productos específicos). La interfaz actual de `ventas-list` no cuenta con filtros avanzados para todas estas dimensiones (aunque la vista del Kardex sí demuestra el uso de un `<form>` con controles como `productoId`, `desde`, `hasta`, etc. usando Angular Reactive Forms).
- Será necesario implementar un componente de reporte específico de ventas (ej. `VentasReporteComponent`) dentro de `/centro-reportes` aprovechando el diseño del Kardex o Reporte Administrativo (Reactive forms para filtros, paginación, tabla y manejo de estados).

## Riesgos y Consideraciones
- **Accesibilidad (A11Y):** El centro de reportes y las listas utilizan etiquetas semánticas (`<header>`, `<section>`, `aria-labelledby`, `aria-label`). El nuevo componente de reporte de ventas deberá mantener este nivel de accesibilidad.
- **Rendimiento:** Las listas actuales de ventas usan paginación del lado del servidor. El reporte de ventas debe seguir esta convención si se muestran detalles o manejar adecuadamente la carga si se trata de un resumen agregado con muchos datos.
- **Aislamiento:** La UI de operaciones de venta (ej. crear/editar venta) está separada del área analítica. Los reportes deben alojarse en `/centro-reportes` para evitar que las consultas analíticas pesadas o masivas bloqueen el flujo de venta transaccional.

## Dictamen VAEP Preflight
- TESTS_EXECUTED: Documentación completada. Inspección de los archivos de Angular (rutas, componentes de reportes y ventas) ejecutada sin modificar código.
- SELF_REVIEW_PASS_1: Análisis completado cumpliendo la restricción de read-only.
- SELF_REVIEW_PASS_2: Los gaps están documentados explícitamente y se han evaluado elementos de UX como loading, error, empty states y exportación basándose en el estado actual de `Desarrollo`.
