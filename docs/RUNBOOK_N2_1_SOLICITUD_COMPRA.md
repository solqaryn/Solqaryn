# Runbook ERP-N2.1 — SolicitudCompra

## Objetivo

Operar y recuperar la superficie `SolicitudCompra` preservando trazabilidad, sin convertir una solicitud en movimiento de inventario o financiero.

## Chequeos previos

1. Confirmar que API y base de datos están saludables mediante los health/readiness existentes.
2. Confirmar migraciones actuales en el ambiente autorizado.
3. Confirmar permisos relacionales del actor para la operación solicitada.
4. Correlacionar incidentes mediante `X-Correlation-ID`/TraceIdentifier y auditoría.
5. Nunca corregir estados productivos con UPDATE manual sin procedimiento formal autorizado.

## Lifecycle esperado

- `Borrador`: editable; permite preparar proveedor opcional y detalles.
- `Solicitada`: no editable como borrador; espera decisión.
- `Aprobada`: terminal para N2.1.
- `Rechazada`: terminal para N2.1; conserva motivo.

## Incidentes comunes

### Error al enviar

Verificar que exista al menos un detalle válido, cantidades positivas y contrato producto/variante válido. Una validación fallida debe dejar el documento en Borrador sin mutación parcial.

### Error al aprobar/rechazar

Verificar que el documento siga en `Solicitada`. Si dos actores deciden simultáneamente, el lock pesimista y la revalidación de estado deben permitir una sola transición válida; el segundo intento debe fallar cerrado.

### Auditoría o persistencia falla

No registrar la transición como exitosa si la unidad transaccional no confirma. Correlacionar logs y auditoría por identificador de request. Reintentar solo después de comprobar el estado persistido; no asumir que una respuesta interrumpida implica rollback.

### UI desactualizada

Recargar detalle/listado desde API. No forzar una transición desde el cliente si el estado del servidor cambió.

## Recuperación

- No reabrir `Aprobada` o `Rechazada` mediante manipulación directa: N2.1 no define reapertura.
- Para rollback de código, usar revert/commit forward sobre `Desarrollo`; nunca force-push.
- Para migraciones, preferir corrección forward. Un `Down` destructivo requiere backup/restauración validada y autorización del ambiente.
- No ejecutar procedimientos de recuperación en Producción desde este runbook sin autorización explícita.

## Smoke técnico

Validar como mínimo:

1. listar/filtrar solicitudes;
2. crear borrador con detalle válido;
3. editar borrador;
4. enviar y comprobar estado `Solicitada`;
5. aprobar una solicitud y comprobar terminalidad;
6. rechazar otra con motivo y comprobar terminalidad;
7. comprobar denegación por permiso ausente;
8. comprobar auditoría/correlation-id;
9. confirmar que ninguna de esas operaciones crea efectos de stock, Kardex, costeo o finanzas.

## Evidencia certificada

Baseline funcional `a1a6f699cbad0186d0e0d7d7ac7f366c51009f7c`; CI `32172981351` SUCCESS, incluyendo backend Release, 994/994 pruebas, frontend y MySQL/migraciones.
