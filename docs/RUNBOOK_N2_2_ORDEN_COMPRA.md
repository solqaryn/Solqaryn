# Runbook ERP-N2.2 — OrdenCompra

## Objetivo

Operar, diagnosticar y recuperar la superficie `OrdenCompra` preservando su naturaleza documental. Una orden aprobada representa un compromiso comercial; no constituye recepción física, Kardex, costo ni movimiento financiero.

## Chequeos previos

1. Confirmar `Desarrollo` y HEAD vigente antes de diagnosticar.
2. Confirmar health/readiness de API y estado de migraciones en el ambiente autorizado.
3. Confirmar que `20260818204700_N2_2_OrdenCompraPersistencia` esté aplicada cuando corresponda.
4. Confirmar permisos relacionales del actor para la operación solicitada.
5. Correlacionar incidentes mediante `TraceIdentifier`/CorrelationId y auditoría.
6. No corregir estados con SQL manual salvo procedimiento formal y ambiente autorizado.
7. No asumir que aprobar una orden debe cambiar stock: el incremento físico pertenece a `RecepcionCompra` en N2.3.

## Lifecycle esperado

- `Borrador`: editable.
- `PendienteAprobacion`: enviado; ya no debe editarse como borrador.
- `Aprobada`: compromiso comercial aprobado.
- `Cancelada`: terminal para N2.2.

## Incidentes comunes

### Creación rechazada por Idempotency-Key

La creación exige `Idempotency-Key`.

- Si falta o es whitespace: corregir el cliente; el servicio no debe ser invocado.
- Si la misma clave reaparece con el mismo fingerprint: tratar como replay idempotente.
- Si la misma clave reaparece con payload diferente: debe fallar cerrado; no generar una segunda orden.
- No reutilizar una clave para una operación empresarial diferente.

### No se puede enviar a aprobación

Verificar:

- estado `Borrador`;
- proveedor válido;
- snapshot del proveedor;
- moneda ISO de tres caracteres;
- al menos una línea;
- cantidad positiva y precios/descuentos/impuestos consistentes;
- `SolicitudCompraId` válido cuando exista.

Una validación fallida no debe dejar el documento parcialmente mutado.

### No se puede aprobar

Verificar que el estado persistido siga en `PendienteAprobacion`. Las decisiones concurrentes deben serializarse/revalidarse; sólo una transición válida puede ganar.

Aprobar **no** debe:

- crear `Compra`;
- crear `RecepcionCompra`;
- aumentar `ExistenciaVariante`;
- escribir Kardex;
- materializar cuentas por pagar o movimientos financieros.

### Cancelación rechazada

El motivo es obligatorio y debe conservar actor/fecha. Si ya está cancelada, no forzar una segunda transición mediante SQL.

### UI muestra datos stale

Recargar desde API. El listado debe limpiar datos previamente mostrados si una consulta posterior falla; no mantener filas stale bajo un estado de error. Tras recuperación, usar la acción de reintento o refrescar explícitamente.

### Error de migración

La migración N2.2.C es aditiva y usa guards fail-closed. Si el preguard/postguard aborta:

1. no eliminar manualmente el guard para “hacer pasar” el deploy;
2. comparar esquema real con dependencias `Proveedores`, `SolicitudesCompra`, `Productos` y `ProductoVariantes`;
3. revisar colisiones de tablas/índices/FKs;
4. resolver por corrección forward;
5. repetir en ambiente controlado.

## Rollback

### Código

Revertir mediante commit forward/revert sobre `Desarrollo`. Nunca force-push ni reescribir historia compartida.

### Esquema

El `Down` de N2.2.C sólo debe avanzar si `OrdenesCompra` y `OrdenCompraDetalles` están vacías. Si existe cualquier fila, el guard debe abortar.

Con datos reales:

- preferir migración correctiva forward;
- si se requiere recuperación destructiva, restaurar desde backup certificado en ambiente autorizado;
- no ejecutar `Down` como procedimiento cotidiano.

### Datos

No “reparar” lifecycle cambiando `Estado` directamente. Preservar historia y aplicar el flujo empresarial autorizado.

## Smoke técnico mínimo

1. listar órdenes con paginación/filtros;
2. obtener detalle por ID y comprobar 404 ProblemDetails para inexistente;
3. crear borrador con `Idempotency-Key` válido;
4. repetir misma creación con misma clave/payload y comprobar replay idempotente;
5. reutilizar la clave con payload distinto y comprobar rechazo fail-closed;
6. editar un borrador;
7. enviar a aprobación con `Compras:Confirmar`;
8. aprobar con `Compras:Aprobar`;
9. cancelar otro documento con `Compras:Anular` y motivo;
10. comprobar 401/403 según autenticación/permisos;
11. comprobar auditoría/correlation-id;
12. comprobar que aprobar/cancelar no modifica stock/Kardex/costeo/finanzas;
13. confirmar que MySQL 8.4 aplica migraciones y ejecuta integration tests.

## Evidencia certificada

Baseline funcional final: `b4d477e2de25077c459d02b479968c93c93bc910`.

- Development `#32218997006` — SUCCESS.
- Acceptance `#32218996971` — SUCCESS.
- Fase 8 `#32218996994` — SUCCESS.
- M10 `#32218996973` — SUCCESS.
- M13 `#32218996978` — SUCCESS.
- Migración N2.2.C: M12 `#32184108722` — SUCCESS en MySQL 8.4.

El commit documental de N2.2.H debe conservar estas evidencias o superarlas antes del cierre formal.