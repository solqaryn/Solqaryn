# Runbook N2.6 — Devoluciones a proveedor

## Propósito

Este runbook describe la validación operativa de la persistencia N2.6 y, especialmente, la frontera de rollback. No autoriza despliegues a Producción ni reemplaza los controles de release del repositorio.

## Migración canónica

`20260821173500_N2_6_DevolucionProveedorPersistencia`

La migración crea:

- `DevolucionesProveedor`.
- `DevolucionProveedorDetalles`.

## Precondiciones antes de aplicar

1. Ejecutar únicamente sobre un entorno autorizado para la validación correspondiente.
2. Verificar que existan las tablas de dependencia requeridas por el guard de la migración: proveedores, órdenes, recepciones, facturas, detalles, productos, variantes, almacenes y ubicaciones.
3. Confirmar que `DevolucionesProveedor` y `DevolucionProveedorDetalles` no existan previamente salvo que el historial de migraciones demuestre que N2.6 ya fue aplicado.
4. No desactivar los guards para hacer pasar un baseline inconsistente.
5. Mantener una única historia EF coherente con el snapshot vigente.

La propia migración utiliza una tabla temporal de guard y falla cerrado si las precondiciones físicas esperadas no se cumplen.

## Postcondiciones de aplicación

Después de aplicar N2.6 se debe comprobar como mínimo:

- existencia de ambas tablas N2.6;
- PKs presentes;
- `UX_DevolucionesProveedor_NumeroDevolucion` único;
- `UX_DevolucionesProveedor_IdempotencyKey` único;
- `UX_DevolucionProveedorDetalles_Devolucion_RecepcionDetalle` único;
- FKs externas con comportamiento restrictivo;
- cascade solamente cabecera → detalles;
- checks de IDs/estado/moneda/idempotencia/cantidades/costos;
- precisión `decimal(18,4)` en cantidad, costo e impuesto;
- `__EFMigrationsHistory` coherente con la migración aplicada;
- ausencia de pending model changes en el gate EF aplicable.

El postguard embebido en `Up` comprueba la presencia de las tablas y de índices únicos críticos; no sustituye los gates de integración MySQL ni las pruebas de aplicación.

## Validación funcional después de migrar

La validación de aplicación debe comprobar:

1. creación con `Idempotency-Key` válido;
2. replay de creación sin duplicar documento;
3. edición sólo en Borrador;
4. confirmación con líneas válidas;
5. actualización física bajo locks de `ExistenciaVariante`;
6. Kardex registrado en la misma operación;
7. repetición de Confirmar sin persistencia/auditoría duplicada;
8. anulación desde Confirmada con motivo;
9. reversión física/Kardex controlada;
10. repetición de Anular sin efectos duplicados;
11. denegación 401/403 conforme autenticación/RBAC;
12. paginación, filtros, ProblemDetails y UX sin datos stale.

## Rollback: realidad del código

El método `Down()` de la migración N2.6 ejecuta únicamente:

- `DropTable("DevolucionProveedorDetalles")`;
- `DropTable("DevolucionesProveedor")`.

**No existe un `DownGuard` en esta migración y el `Down()` es destructivo para los datos N2.6.**

Por tanto, un rollback con filas N2.6 no debe tratarse como una operación reversible por sí sola.

## Requisitos operativos antes de un rollback destructivo

Antes de ejecutar `Down()` en un entorno que contenga datos relevantes se debe exigir, fuera de la migración:

- quiescencia de escrituras N2.6;
- evidencia verificable de preservación/export de las filas y relaciones necesarias;
- mecanismo de restore probado para ese entorno;
- identificación del punto de retorno y del historial EF esperado;
- criterio de aborto si la evidencia de preservación o restore no es suficiente.

El repositorio no demuestra aquí un mecanismo universal de backup/restore para todos los entornos. La herramienta concreta y el procedimiento de infraestructura son **DECISION_PENDING por entorno** y no deben inventarse en este documento.

## Secuencia de rollback controlado

1. Detener nuevas mutaciones de devoluciones en el entorno objetivo.
2. Confirmar la evidencia de preservación y restore definida para ese entorno.
3. Registrar el HEAD, versión/migración y estado de `__EFMigrationsHistory`.
4. Ejecutar el rollback únicamente bajo el procedimiento de release autorizado.
5. Verificar que las tablas N2.6 fueron eliminadas y que el resto de las dependencias permanecen intactas.
6. Si se requiere recuperar datos N2.6, ejecutar el restore probado después de volver a una versión compatible con su esquema; no intentar insertar datos contra un esquema inexistente.
7. Ejecutar smoke tests del baseline restaurado y verificar que no existan migraciones/modelos divergentes.

## Abort criteria

Abortar el rollback si ocurre cualquiera de estos casos:

- no existe evidencia verificable para restaurar datos que deban conservarse;
- el historial EF no corresponde al commit objetivo;
- existen escrituras concurrentes N2.6 que no pueden detenerse;
- el target requiere conservar datos N2.6 pero no hay un esquema compatible preparado;
- una dependencia externa sería eliminada o alterada fuera del scope N2.6.

## No autorizado por este runbook

Este documento no autoriza:

- tocar Producción;
- modificar secretos;
- ejecutar deploy;
- hacer merge del PR #2;
- force-push;
- eliminar datos para forzar un CI verde;
- simular que `Down()` contiene protección que no existe.