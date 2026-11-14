# Runbook ERP-N2.9 — Evaluación de proveedores

## Objetivo

Operar, diagnosticar y revertir de forma segura la capacidad `EvaluacionProveedor` sin introducir scoring o criterios no implementados.

## Precondiciones

1. Trabajar exclusivamente en `Desarrollo`.
2. Confirmar que las migraciones N2.9 están presentes y que el snapshot EF no tiene drift.
3. Confirmar que la recepción origen está `Recibida` y tiene `FechaRecepcionUtc`.
4. Confirmar que la orden asociada existe, tiene `FechaEsperadaUtc` y proveedor válido.
5. Usar permisos `Compras/Ver` para lectura y `Compras/Crear` para generación.

## Operación funcional

### Consultar

- Listado: `GET /evaluaciones-proveedor` con filtros/paginación del contrato vigente.
- Detalle: `GET /evaluaciones-proveedor/{id}`.

### Generar o refrescar

`POST /evaluaciones-proveedor/recepciones/{recepcionCompraId}/generar`.

El servicio obtiene la recepción bajo lock, valida materialización, carga la orden y crea o actualiza la evaluación de esa recepción. No aceptar proveedor, fechas o cantidades arbitrarias desde el cliente para reemplazar la autoridad persistida.

## Diagnóstico

Si la generación falla:

- 404: revisar recepción u orden inexistente.
- Regla de negocio: revisar estado de recepción, `FechaRecepcionUtc`, `FechaEsperadaUtc` y proveedor.
- Error de persistencia: verificar migración/snapshot y FKs de `EvaluacionesProveedor`.
- Error de autorización: verificar grants relacionales `Compras/Ver` o `Compras/Crear`; no introducir bypass administrativo.
- Auditoría: la mutación debe conservar `RegistrarEstrictoAsync`; no degradar a auditoría best-effort para hacer pasar una operación.

## Verificación de datos

La tabla `EvaluacionesProveedor` debe conservar:

- FK a `Proveedores`, `OrdenesCompra`, `RecepcionesCompra` con `Restrict`.
- índices por recepción, orden y proveedor+fecha.
- cantidades no negativas `decimal(18,4)`.
- una evaluación reconciliable por recepción según el repositorio/servicio.

Ejecutar los scripts N2.9 preflight/postcheck y las suites causales antes de promover un cambio.

## Rollback

La migración N2.9 posee `DownGuard`: si `EvaluacionesProveedor` contiene filas, el rollback debe fallar cerrado. No borrar evaluaciones para forzar un downgrade. Ante datos reales, usar corrección forward o restauración desde respaldo compatible según el runbook de recuperación global.

## Gate de cierre

Un cierre válido requiere: código y documentación reconciliados, Development/Acceptance/Fase8/M13 aplicables en SUCCESS, evidencia de persistencia/rollback, P0/P1=0 y actualización de COLA/CONFIG/TASKS/CHANGELOG. `COMPLETED` de Jules no sustituye ese gate.