# N5.3.A.6 — Preflight de integración, rollback y observabilidad de reportes de ventas

Autoridad operativa: `docs/VAEP_AUTHORITY.md`.

## REVIEW_FIRST del R2

J6 agotó su presupuesto Jules en ATTEMPT2/2 y entregó un patch documental terminal `READY_FOR_VAEP`. REVIEW_FIRST conserva el material causal y corrige afirmaciones no demostradas por el repo vivo: este preflight **no presupone** read replicas, feature toggles, `TenantId`, un límite temporal específico ni controllers/índices futuros. Esos elementos sólo podrán adoptarse si un parent posterior los define y demuestra.

## Dependencias cross-layer ya verificadas en N5.3.A

La integración posterior debe respetar simultáneamente las decisiones de los facets aceptados del mismo parent:

- **Backend/Data:** `Venta` es cabecera; las dimensiones de producto/variante y ubicación física están en `VentaDetalle`; la sucursal se relaciona por `VentaDetalle.AlmacenId -> Almacen.SucursalId`; los snapshots del detalle importan para historia y `ProductoVarianteId` puede ser nulo en legado.
- **API/Contracts:** el listado operativo de ventas usa `PagedRequest`/`GetPagedAsync`; un contrato multidimensional de reporting debe ser dedicado y no ampliar silenciosamente el API operacional. Los filtros HTTP nunca reemplazan autorización.
- **Frontend/UX:** el Centro de Reportes ya posee rutas protegidas y patrones de reportes; todavía no existe una superficie de reportes de ventas aceptada como autoridad. La UI futura debe ser aditiva al flujo transaccional de `/ventas`.
- **Seguridad, CI e integración:** los facets J4/J5/J6 deben quedar REVIEW_FIRST-aceptados antes de declarar el parent completo `LISTO_REAL`.

## Fallos de integración que N5.3.B+ debe prevenir

1. **Fuga de alcance/RBAC.** Una query agregada no puede debilitar el row-scope fail-closed actual de ventas ni permitir que un filtro de usuario/sucursal expanda el alcance autorizado.
2. **Historia reescrita.** Etiquetas y dimensiones de maestro actuales no deben reemplazar snapshots históricos sin contrato explícito.
3. **Cardinalidad/duplicación.** Joins a detalles, variantes, almacenes y sucursales deben demostrar que no multiplican venta/importe/utilidad al agregar.
4. **Semántica de sucursal.** Una venta puede tener evidencia de almacén por detalle; no se debe asumir una única sucursal de cabecera sin definición causal.
5. **Estados documentales.** Confirmadas, anuladas, borradores y borrado lógico deben tener una política explícita antes de certificar cifras.
6. **Carga analítica.** Rangos, paginación, agregaciones y exportación deben tener límites/plan de consulta proporcionales y pruebas de performance; este preflight no inventa valores máximos.
7. **Compatibilidad de UI/API.** La ruta y los DTOs futuros deben versionarse/adicionarse de forma que el flujo operativo actual de ventas no dependa del módulo analítico.

## Estrategia de rollback aceptable

Para una implementación futura en `Desarrollo`/entornos autorizados:

- Mantener cambios de reportes de ventas **aditivos y separables** del flujo transaccional de crear/confirmar/anular ventas.
- Si un cambio de código de N5.3 introduce una regresión, el rollback primario debe ser la reversión del delta causal de N5.3; no requiere ni autoriza modificar datos productivos durante este preflight.
- Si en N5.3.C/D se introduce una migración o índice material, debe existir antes de aceptar esa entrega una prueba de forward/rollback compatible con la política de migraciones vigente. N5.3.A no autoriza DDL.
- Un mecanismo de deshabilitación por configuración/feature flag sólo puede formar parte del rollback si realmente se implementa y valida posteriormente; **no se afirma que exista hoy**.
- Cualquier resultado tardío de una sesión superseded o un R3 debe permanecer fuera de integración automática conforme al MASTER.

## Observabilidad que debe probarse antes del cierre de implementación

La futura superficie analítica debe permitir atribuir y diagnosticar, sin secretos ni PII innecesaria:

- duración de consultas y exportaciones;
- volumen de filas/resultado o tamaño lógico de respuesta suficiente para detectar degradación;
- endpoint/facet y resultado técnico;
- identidad autorizada sólo en la medida ya permitida por la política de logging vigente.

Los nombres exactos de eventos, umbrales, sinks y campos no se fijan aquí: deben derivarse de la infraestructura de observabilidad viva al implementar y cubrirse con tests/validación proporcional.

## Parent-level DoD aportado por este facet

Este facet queda materialmente completo cuando:

- reconcilia backend/data, API/contracts y frontend/UX aceptados del mismo parent;
- identifica riesgos cross-layer y rollback sin inventar infraestructura presente;
- deja explícitos los gates de RBAC, historia, cardinalidad, estados, performance y migraciones para N5.3.B+;
- no implementa código, esquema, Producción ni secretos;
- el REVIEW_FIRST lo acepta con P0/P1 abiertos introducidos por el facet = 0.

Este documento **no certifica por sí solo N5.3.A** y no promueve N5.3.B. El cierre del parent exige reconciliar todos los terminales J1–J6 y los gates aplicables.
