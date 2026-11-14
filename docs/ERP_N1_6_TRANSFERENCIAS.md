# ERP-N1.6 — Transferencias empresariales

## 1. Alcance y objetivo

ERP-N1.6 incorpora transferencias internas de inventario con trazabilidad física completa entre almacenes y ubicaciones. El flujo canónico es `Borrador -> Solicitada -> Aprobada -> EnTransito -> Recibida`, con cancelación controlada y soporte explícito para recepción parcial, faltantes, sobrantes y daños.

La autoridad de stock vivo permanece en `ExistenciaVariante`. `ProductoVariante.Cantidad` no se utiliza para decidir transferencias ni para resolver concurrencia física.

## 2. Modelo e invariantes de dominio

`TransferenciaInventario` y `TransferenciaInventarioDetalle` preservan:

- almacén origen y almacén destino;
- ubicaciones físicas de origen y destino cuando aplican;
- `ProductoVarianteId` por línea;
- cantidades solicitada, aprobada, despachada y recibida;
- faltantes, sobrantes y dañados como discrepancias explícitas;
- actores y timestamps de solicitud, aprobación, despacho, recepción y cancelación;
- transiciones fail-closed que validan antes de mutar el agregado.

Las operaciones inválidas no deben dejar mutaciones parciales de estado ni de auditoría. N1.6.G añadió regresiones explícitas para aprobación sin cantidades aprobadas, despacho sin cantidades despachadas, recepción con detalles pendientes y cancelación de transferencias ya recibidas.

## 3. Persistencia y migración

N1.6.C consolidó cabecera y detalle de transferencias, FKs, índices, snapshot EF y origen tipado de Kardex. La FK canónica de `TransferenciaInventarioId` quedó unificada en la migración `20260816042500_N1_6_TransferenciaOrigenMovimientoInventario`; la migración posterior duplicada `20260816051000_N1_6_TransferenciaKardexOrigen` fue retirada para evitar DDL duplicado.

La persistencia fue certificada en MySQL con migraciones actuales, integración y recuperación controlada.

## 4. Lifecycle físico y concurrencia

El lifecycle físico usa locks sobre la clave autoritativa `ProductoVariante + Almacen + Ubicacion` mediante `ExistenciaVariante`:

- **Despachar:** descuenta `StockFisico` del origen y materializa tránsito hacia destino.
- **Recibir:** consume el tránsito y materializa únicamente la cantidad efectivamente recibida en destino.
- **Discrepancias:** faltantes y dañados no se convierten silenciosamente en stock disponible; sobrantes permanecen explícitos para conciliación.
- **Cancelar en tránsito:** revierte de forma transaccional el stock físico del origen, elimina el tránsito asociado y registra Kardex de reversión.

Las operaciones físicas se ejecutan dentro de la misma unidad transaccional que el cambio de estado y la evidencia de Kardex, evitando estados documentales desconectados del stock.

## 5. Kardex y correlación

Los movimientos de transferencias usan origen tipado `TransferenciaInventario` y exponen `TransferenciaInventarioId` en persistencia, DTOs y consultas.

La correlación es determinística y separada por operación (`despachar`, `recibir`, `cancelar`), de manera que reintentos o análisis posteriores puedan distinguir cada transición sin reutilizar identificadores ambiguos.

El Kardex permite filtrar transferencias por `OrigenTipo=transferencia` / `TransferenciaInventario` y `OrigenId`, manteniendo simetría entre escritura y lectura del origen tipado.

## 6. API y aplicación

La capa de aplicación separa CRUD documental del lifecycle físico. `ITransferenciaInventarioMovimientoService` concentra despacho, recepción y cancelación transaccionales, mientras el controller expone contratos HTTP dirigidos y delega reglas físicas al servicio.

La API mantiene:

- consultas y paginación server-side;
- ProblemDetails/errores fail-closed;
- lifecycle explícito para solicitar, aprobar, despachar, recibir y cancelar;
- reintentos físicos protegidos para no duplicar efectos;
- RBAC runtime basado en `MovimientosInventario`.

## 7. Frontend y UX

N1.6.E implementó frontend empresarial para transferencias con:

- listado, filtros y paginación;
- creación/edición de borradores;
- selectores de almacén, ubicación y variante;
- detalle y lifecycle completo;
- recepción con discrepancias;
- estados loading, vacío y error;
- navegación protegida por permisos;
- comportamiento responsive y cobertura E2E.

El cierre UX quedó certificado por CI Desarrollo, aceptación funcional, M10 UI/UX y Fase 8.

## 8. Seguridad, RBAC, auditoría y observabilidad

N1.6.F consolidó autorización relacional por endpoint y auditoría explícita para `Crear`, `Editar`, `Solicitar`, `Aprobar`, `Despachar`, `Recibir` y `Cancelar`.

La auditoría preserva referencia de transferencia, actor, estado, timestamps y motivo de cancelación. La observabilidad consume el identificador de trazabilidad saneado por runtime; no persiste directamente un `X-Correlation-ID` bruto suministrado por el cliente.

Las pruebas protegen además que permisos específicos no degraden a permisos genéricos y que las transiciones posteriores no sobrescriban actores/timestamps ya consolidados.

## 9. Evidencia A–H

### N1.6.A — Auditoría y preflight

Preflight publicado en `docs/ERP_N1_6_TRANSFERENCIAS_PREFLIGHT.md`, con autoridad física, riesgos, rollback, RBAC, API/UX y matriz de pruebas.

### N1.6.B — Dominio y contratos

Cierre funcional en `57dac3f026e3303a1ec7176828416f7c56f780d2`. CI Desarrollo `#31924945075` `SUCCESS` completo.

### N1.6.C — Persistencia, migración y datos

Cierre `1697c1bdb7d909b995100679bbb8c60d441a7644`.

- Desarrollo `#31934821227` — `SUCCESS`;
- recuperación MySQL `#31934823434` — `SUCCESS`.

Quedaron certificadas migraciones actuales, integración MySQL, FK canónica y eliminación del DDL duplicado.

### N1.6.D — Aplicación, servicios y API

Cierre `53b5111e7c6d0ea5d2cfd173a57eafcdf63c54b1`. CI Desarrollo `#31942933659` terminó `SUCCESS`, incluyendo backend/unitarias, frontend, Docker, higiene y MySQL/migraciones/integración.

### N1.6.E — Frontend y UX

Cierre `b4c221018d951253c4868b49b9534e8e1a5e02e6`.

- Desarrollo `#31944213065` — `SUCCESS`;
- aceptación funcional `#31944213057` — `SUCCESS`;
- M10 UI/UX `#31944213043` — `SUCCESS`;
- Fase 8 `#31944213099` — `SUCCESS`.

### N1.6.F — RBAC, auditoría, seguridad y observabilidad

Cierre `85d672f6f4f7e9753b36bd509532d8e24b318fa5`.

- Desarrollo `#31949179909` — `SUCCESS`;
- Fase 8 `#31949179872` — `SUCCESS`.

### N1.6.G — QA, regresión y CI

Cierre sobre `fc9729bad8d8dfd6f9c402c8e0ff2ca66de3bf9f`, tree `0903fa3c9a0b50ee6b8431285c59e2055c41e781`.

Gates causales finales:

- Desarrollo `#31954861360` — `SUCCESS`;
- aceptación funcional integral `#31954861314` — `SUCCESS`;
- Fase 8 `#31954861485` — `SUCCESS`;
- M13 `#31954861328` — `SUCCESS`.

La regresión final blindó fail-closed y ausencia de mutación parcial en aprobación, despacho, recepción pendiente y cancelación inválida.

### N1.6.H — Documentación y certificación

Este documento consolida el cierre técnico de ERP-N1.6. El commit documental y sus gates causales deben quedar verdes antes de marcar N1.6.H como `LISTO`.

## 10. Rollback y operación

Rollback de código:

1. revertir commits causales de N1.6 en `Desarrollo` mediante commits de reversión explícitos;
2. no usar force-push ni reescribir historia;
3. volver a ejecutar CI y regresión después de cada reversión.

Rollback de datos:

1. no revertir transferencias ya recibidas mediante DDL destructivo;
2. conservar Kardex, correlación y auditoría como evidencia histórica;
3. para transferencias `EnTransito`, utilizar la transición operativa de cancelación/reversión, no manipulación manual de stock;
4. cualquier operación sobre Producción requiere procedimiento y autorización separados.

## 11. Restricciones de despliegue

Esta certificación aplica exclusivamente a `Desarrollo`. No autoriza merge a `main`, auto-merge del PR #2, cambios en Producción, nuevas ramas, force-push, secretos ni infraestructura productiva.

PR #2 debe permanecer abierto y Draft.

## 12. Estado de certificación

ERP-N1.6 A–G está técnicamente cerrado y validado. N1.6.H queda en certificación documental hasta que los gates causales del commit documental finalicen satisfactoriamente.