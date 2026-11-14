# Runbook — ERP-N1.6 Transferencias empresariales

## Objetivo

Operar y diagnosticar transferencias internas sin romper la autoridad física de `ExistenciaVariante`, el lifecycle documental ni la trazabilidad de Kardex.

## Estados y transiciones válidas

1. `Borrador` — editable.
2. `Solicitada` — pendiente de aprobación.
3. `Aprobada` — cantidades autorizadas.
4. `EnTransito` — stock descontado del origen y representado en tránsito.
5. `Recibida` — tránsito cerrado y stock recibido materializado en destino.
6. `Cancelada` — cierre controlado; si estaba en tránsito, exige reversión física y Kardex.

No forzar estados mediante SQL ni editar timestamps/actores manualmente.

## Antes de despachar

Verificar:

- transferencia en estado aprobado;
- cantidades despachables > 0;
- almacén/ubicación de origen válidos;
- existencia física resoluble por `ProductoVariante + Almacen + Ubicacion`;
- stock físico suficiente;
- usuario con permiso `MovimientosInventario/Confirmar` o permiso específico configurado por el endpoint vigente.

Si una validación falla, la operación debe terminar sin mutaciones parciales.

## Antes de recibir

Verificar:

- transferencia `EnTransito`;
- detalle de recepción completo según contrato;
- cantidades recibidas/faltantes/sobrantes/dañadas coherentes;
- destino físico resoluble;
- ausencia de reintento ya materializado para la misma correlación.

La recepción sólo materializa cantidad efectivamente recibida. Faltantes y dañados permanecen discrepancias; sobrantes deben mantenerse visibles para conciliación.

## Cancelación

### Antes del despacho

La cancelación es documental y auditable. No debe modificar stock físico.

### En tránsito

Debe ejecutarse por el workflow soportado:

1. bloquear las existencias físicas afectadas;
2. devolver al origen el stock previamente despachado;
3. eliminar/compensar el tránsito del destino;
4. registrar Kardex de reversión con `TransferenciaInventarioId`;
5. persistir motivo, actor y timestamp;
6. confirmar transacción completa.

Si cualquier paso falla, no dejar el agregado como `Cancelada` con stock sin revertir.

## Diagnóstico de discrepancias

Cuando una transferencia no concilie:

1. identificar `TransferenciaInventarioId`;
2. consultar Kardex por `OrigenTipo=TransferenciaInventario` y `OrigenId`;
3. agrupar por `CorrelationId` de `despachar`, `recibir` o `cancelar`;
4. contrastar `ExistenciaVariante.StockFisico` origen/destino;
5. revisar detalle solicitado/aprobado/despachado/recibido/faltante/sobrante/dañado;
6. revisar auditoría de lifecycle y actor/timestamps;
7. no corregir cantidades mediante SQL manual mientras exista una transición compensatoria soportada.

## Reintentos e idempotencia

- no reutilizar una correlación de despacho para recepción o cancelación;
- un reintento no debe duplicar salida, tránsito, entrada ni reversión;
- ante timeout, comprobar primero estado documental + Kardex antes de repetir la orden;
- si la operación quedó confirmada, responder/continuar desde el estado persistido en vez de volver a aplicar el movimiento.

## Seguridad y observabilidad

- usar el Correlation ID saneado por runtime;
- no confiar ni persistir directamente headers de correlación sin validación;
- revisar ProblemDetails y logs usando el identificador de trazabilidad;
- la ausencia de permisos debe fallar cerrado;
- no usar credenciales, secretos ni cambios de infraestructura para resolver fallos funcionales.

## Rollback

En `Desarrollo`, revertir mediante commits explícitos y volver a ejecutar CI.

Para datos operativos, usar transiciones compensatorias soportadas. No ejecutar DDL/DML destructivo ni cambios directos en Producción desde este runbook.

## Gates de certificación

Baseline QA N1.6.G:

- Desarrollo `#31954861360` — `SUCCESS`;
- aceptación integral `#31954861314` — `SUCCESS`;
- Fase 8 `#31954861485` — `SUCCESS`;
- M13 `#31954861328` — `SUCCESS`.

Los commits documentales N1.6.H deben completar sus propios gates antes del cierre formal del punto.