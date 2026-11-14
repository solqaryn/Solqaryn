# N5.3.A.4 — Preflight de seguridad y RBAC para reportes de ventas

Autoridad operativa: `docs/VAEP_AUTHORITY.md`.

## REVIEW_FIRST del R2

J4 entregó un R2 terminal `READY_FOR_VAEP` en ATTEMPT2/2. El análisis identifica correctamente los pilares de seguridad existentes, pero REVIEW_FIRST corrige prescripciones que no están demostradas como contrato de reportes de ventas: no se fija todavía un módulo/permiso nuevo, no se asume que los filtros de periodo deban responder 403, no se exige llamar literalmente a un método privado de repository y no se traslada automáticamente la política de censura de inventario a ventas. Esas decisiones corresponden al contrato de N5.3.B+.

## Autoridad RBAC viva relevante

- `VentasController` está protegido con `[Authorize]`; la lectura de ventas usa `[RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]` y las operaciones transaccionales usan acciones específicas de `AccionPermiso`.
- `ModuloSistema` contiene hoy `Ventas`, `Facturacion`, `Finanzas`, `ReportesAdministrativos`, `Sucursales`, `Almacenes` y otros módulos; **no existe un `ReportesVentas` dedicado** en la autoridad viva.
- `AccionPermiso` contiene `Ver` y `Exportar`, entre otras acciones. La existencia de `Exportar` no decide por sí sola qué módulo debe proteger una futura exportación de ventas.
- `VentaRepository` aplica un alcance fail-closed derivado de `IUsuarioScopeService`: un no-administrador queda limitado a `CreadoPorUsuarioId == alcance.UsuarioId`; un administrador puede aplicar el `UsuarioIdScope` solicitado. Un nuevo query de reporting debe preservar una semántica de alcance igual o más restrictiva, sin confiar en filtros HTTP como autorización.

## Campos financieros sensibles

Existe un precedente concreto en `ReportesInventarioValorizacionController`: el endpoint requiere `Inventario:Ver` y, si falta `Finanzas:Ver`, registra auditoría y devuelve un DTO financiero vacío/censurado. **Ese comportamiento es precedente, no contrato automático para N5.3.**

N5.3.B debe decidir explícitamente para ventas:

- qué métricas (costo, utilidad, margen u otras) son financieras/sensibles;
- qué permiso autoriza esas métricas;
- si la respuesta censura campos, omite campos, separa endpoints o deniega la operación;
- cómo se prueba negativamente esa política.

Hasta que ese contrato exista, N5.3.A no debe afirmar que valores de ventas necesariamente se devuelven en cero/nulo ni que `Finanzas:Ver` es la única política correcta.

## Sucursal, almacén y row-scope

La evidencia de datos aceptada para este parent ubica `AlmacenId` en `VentaDetalle` y relaciona almacén con sucursal. Por ello:

- un filtro de sucursal es una dimensión de datos, **no una autorización por sí mismo**;
- el backend debe intersectar cualquier filtro solicitado con el scope autorizado;
- no se debe asumir una única `SucursalId` de cabecera para toda venta;
- si un usuario no autorizado intenta ampliar su scope, la implementación debe fallar cerrada mediante la política RBAC/row-scope aprobada; el código HTTP exacto y la validación de filtros se fijarán en el contrato, no en este preflight.

## Exportación y auditoría

`ReportesAdministrativosController` y la valorización de inventario proporcionan precedentes de auditoría para operaciones de reporte. Para una futura exportación de ventas, N5.3.B/D debe definir y luego probar:

1. permiso relacional aplicable (`Ver`, `Exportar` o combinación aprobada);
2. alcance de filas idéntico al reporte consultado;
3. auditoría sin exponer secretos ni valores sensibles innecesarios;
4. rechazo/censura proporcional cuando falte autorización financiera.

No se fija aquí un controller, módulo o atributo nuevo inexistente.

## Security gates para N5.3.B+

- **P0 — Row-scope fail-closed:** ninguna query agregada/export puede consultar ventas fuera del alcance autorizado.
- **P0 — Authorization before disclosure:** los filtros de usuario/sucursal/cliente/producto no sustituyen permisos.
- **P0 — Financial disclosure:** costo/utilidad/margen requieren política explícita y pruebas negativas antes de exposición.
- **P1 — Aggregation leakage:** agrupaciones pequeñas, exports o mensajes de error no deben revelar información fuera de scope.
- **P1 — Auditability:** consultas/exportaciones sensibles deben dejar evidencia compatible con la infraestructura de auditoría viva.
- **P1 — UI/API consistency:** la UI no debe mostrar acciones que el API rechazará, pero el API sigue siendo la autoridad de enforcement.

## Validación proporcional

El R2 reportó build exitoso y un `dotnet test --no-build` afectado por conexión MySQL del entorno. Como el delta aceptado de este facet es sólo documental, REVIEW_FIRST no convierte ese fallo ambiental en un PASS global ni lo usa para afirmar que los gates de implementación futura están probados. La evidencia requerida aquí es inspección de contratos RBAC vivos y delimitación de gates.

## Dictamen del facet

`N5.3.A.4.SECURITY_RBAC_PREFLIGHT` queda materialmente aceptable con estas correcciones: autoridad RBAC identificada, row-scope fail-closed preservado como invariante, precedente financiero distinguido de un contrato futuro y gates P0/P1 explícitos. No se implementa N5.3.B+, no se crea R3 y este facet aislado no declara el parent `LISTO_REAL`.
