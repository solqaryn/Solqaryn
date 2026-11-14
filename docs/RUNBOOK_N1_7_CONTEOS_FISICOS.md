# Runbook — ERP-N1.7 Conteos físicos

## Objetivo

Operar y diagnosticar conteos físicos empresariales sin romper la autoridad de `ExistenciaVariante`, la privacidad de los conteos ciegos ni el lifecycle formal de `AjusteInventario`.

## Autoridad y reglas no negociables

- `ExistenciaVariante.StockFisico` es la única autoridad física de stock.
- `ConteoInventario` conserva snapshots, capturas y diferencias; no escribe stock directamente.
- Las diferencias aprobadas se materializan mediante un `AjusteInventario` formal.
- La identidad física de una línea es `ProductoVarianteId + AlmacenId + UbicacionAlmacenId`.
- No corregir stock mediante SQL manual ni edición directa de snapshots.

## Lifecycle operativo

1. `Borrador` — definición del alcance y edición permitida.
2. `EnProceso` — universo materializado y captura habilitada.
3. `Cerrado` — captura cerrada y conciliación disponible.
4. `Aprobado` — resultado aceptado; puede generar ajuste si existen diferencias.
5. `Cancelado` — cierre controlado cuando la transición es válida.

No forzar estados ni timestamps mediante DML directo.

## Antes de iniciar

Verificar:

- conteo en `Borrador`;
- almacén activo y alcance coherente;
- para `PorUbicacion`, ubicación válida perteneciente al almacén;
- para `PorCategoria`, categoría válida;
- variantes resolubles y sin duplicados físicos dentro del conteo;
- usuario con permiso `MovimientosInventario/CambiarEstado`;
- ausencia de otro cambio concurrente que invalide la definición del universo.

La materialización del universo debe capturar el snapshot esperado de cada existencia sin convertirlo en stock vivo.

## Captura individual y por lote

- aceptar cantidad física `0` como observación válida;
- rechazar cantidades negativas;
- validar que cada `DetalleId` pertenezca al conteo actual;
- en captura por lote, prevalidar todas las líneas antes de mutar el agregado;
- si una línea del lote falla, ninguna captura del lote debe persistirse parcialmente;
- evitar reenvíos innecesarios desde frontend usando captura dirty-only.

## Conteos ciegos

Mientras un conteo ciego no haya alcanzado un cierre válido, el API debe impedir que el capturista conozca o reconstruya el stock esperado.

Ocultar o neutralizar:

- `StockEsperado`;
- `Diferencia`;
- `CantidadConDiferencia`;
- `DiferenciaNeta`.

La cantidad realmente capturada puede mostrarse. La protección aplica a detalle y listado paginado, incluso si el conteo se cancela antes de cerrar.

No confiar únicamente en ocultamiento visual del frontend. Si `CantidadContada` y `Diferencia` se exponen simultáneamente, puede inferirse `StockEsperado = CantidadContada - Diferencia`.

Después de un cierre válido, usuarios autorizados pueden ver snapshot y diferencias para conciliación.

## Cierre y aprobación

Antes de cerrar:

- todas las líneas requeridas deben tener captura válida;
- no debe existir detalle pendiente;
- el conteo debe estar `EnProceso`.

Antes de aprobar:

- el conteo debe estar `Cerrado`;
- el usuario debe tener `MovimientosInventario/Aprobar`;
- la operación debe ser fail-closed y no dejar actor/timestamp parciales ante error.

Cerrar o aprobar no modifica `StockFisico`.

## Generación del ajuste

Sólo generar `AjusteInventario` cuando:

- el conteo esté `Aprobado`;
- existan diferencias reales;
- no exista un vínculo parcial o inconsistente con ajustes previos;
- no se esté intentando crear un segundo ajuste para las mismas diferencias.

La generación debe ser idempotente: si ya existe un vínculo completo y coherente, reutilizar el ajuste canónico.

La modificación física ocurre únicamente cuando el lifecycle del ajuste confirma el movimiento bajo sus locks y reglas de concurrencia.

## Cancelación

La cancelación es documental y auditable. Si el conteo ciego se cancela antes de cerrar, la privacidad del snapshot sigue vigente.

Registrar motivo, actor y timestamp. No revelar diferencias como efecto lateral de cancelar.

## Diagnóstico de diferencias

1. identificar `ConteoInventarioId`;
2. revisar tipo, alcance, almacén y filtros;
3. comparar líneas por `ProductoVariante + Almacen + Ubicacion`;
4. revisar snapshot esperado y cantidad contada sólo si el estado permite conciliación;
5. verificar auditoría de inicio, captura, cierre y aprobación;
6. si existe ajuste generado, revisar `AjusteInventarioId`, lifecycle y Kardex asociado;
7. comprobar que el stock vigente provenga de `ExistenciaVariante` y no de snapshots históricos;
8. usar una transición compensatoria soportada en lugar de DML manual.

## Reintentos e idempotencia

- antes de repetir `iniciar`, `cerrar`, `aprobar` o `generar-ajuste`, consultar el estado persistido;
- no duplicar ajustes por timeout del cliente;
- una captura por lote reintentada debe preservar atomicidad;
- ante respuesta incierta, reconciliar estado documental y auditoría antes de reejecutar.

## Seguridad y observabilidad

- controller protegido por autenticación y permisos relacionales de `MovimientosInventario`;
- no introducir `[AllowAnonymous]` en endpoints de conteos;
- usar el `TraceIdentifier` saneado del runtime para correlación de auditoría;
- no confiar directamente en `X-Correlation-ID` proporcionado por el cliente;
- una falla del logging no debe revertir una operación de negocio ya confirmada;
- los conteos ciegos se consideran una frontera de confidencialidad de negocio, no un detalle de presentación.

## Rollback

En `Desarrollo`, revertir mediante commits explícitos y volver a ejecutar los gates causales. Nunca force-push.

Para datos operativos, conservar conteos históricos y corregir stock mediante `AjusteInventario` o mecanismos compensatorios soportados. No ejecutar cambios destructivos en Producción desde este runbook.

## Gates de certificación

Baseline QA de N1.7.G:

- Desarrollo `#31995868136` — `SUCCESS`;
- aceptación integral `#31995868251` — `SUCCESS`;
- Fase 8 `#31995868120` — `SUCCESS`;
- M13 `#31995868144` — `SUCCESS`;
- M10 `#31995868110` — `SUCCESS`.

Los commits documentales de N1.7.H deben completar sus gates causales antes de cerrar formalmente el punto y habilitar el siguiente punto de ERP-N1.