# ERP-N1.8 — Reservas de inventario

## Estado

**CERRADO FUNCIONALMENTE / DOCUMENTACIÓN CANÓNICA N1.8.H**

Baseline funcional certificado previo al cierre documental:

```text
95baf2763b912e1015a3bdd25a37aca649e34c37
```

Este documento consolida el resultado real de ERP-N1.8. El preflight histórico permanece en `docs/ERP_N1_8_RESERVAS_PREFLIGHT.md` y el procedimiento específico de migración en `docs/RUNBOOK_N1_8_RESERVAS_MIGRACION.md`.

## 1. Objetivo empresarial

Reservas permite comprometer inventario físico para pedidos/ventas sin convertir la reserva en una segunda autoridad de stock. El sistema diferencia explícitamente:

- **Stock físico:** cantidad material registrada en `ExistenciaVariante`.
- **Stock reservado:** compromiso activo acumulado en `ExistenciaVariante.StockReservado`.
- **Stock disponible:** capacidad vendible derivada de físico menos reservado.

La reserva explica **por qué** existe stock reservado; `ExistenciaVariante` continúa siendo la única autoridad cuantitativa.

## 2. Autoridad e identidad física

La clave física empresarial es:

```text
ProductoVarianteId + AlmacenId + UbicacionAlmacenId
```

`UbicacionAlmacenId` puede ser nulo cuando la existencia pertenece al almacén sin ubicación interna específica. Una reserva nunca se reduce a producto/variante global: almacén y ubicación forman parte de la identidad operativa.

Invariantes obligatorias:

1. `ExistenciaVariante` es la única autoridad de `StockFisico`, `StockReservado` y disponibilidad.
2. Una misma clave física no puede repetirse dentro de una reserva.
3. La cantidad reservada por detalle debe ser positiva.
4. Activar valida disponibilidad y muta `StockReservado` bajo lock pesimista.
5. Consumir/liberar/expirar/cancelar una reserva activa retira exactamente una vez el compromiso correspondiente.
6. Nunca se modifica `StockFisico` por el mero acto de reservar o liberar.
7. No se permite overselling por una carrera read-then-write fuera de la transacción autoritativa.

## 3. Modelo de dominio

### 3.1 ReservaInventario

Cabecera documental con, entre otros, los siguientes conceptos:

- número único de reserva;
- `VentaId` opcional para vincular el compromiso a una venta/pedido;
- estado de lifecycle;
- fecha de expiración opcional;
- timestamps de creación, activación, consumo, liberación, expiración aplicada y cancelación;
- motivo de liberación/cancelación cuando corresponde;
- actores de creación/actualización;
- colección de detalles físicos.

### 3.2 ReservaInventarioDetalle

Cada detalle conserva:

- `ProductoVarianteId`;
- `AlmacenId`;
- `UbicacionAlmacenId` opcional;
- `CantidadReservada`;
- `CantidadConsumida`;
- snapshots históricos de SKU, marca, modelo, color y talla.

Los snapshots preservan legibilidad histórica sin reemplazar las FKs ni la autoridad física vigente.

## 4. Lifecycle

```text
Borrador
  ├─> Activa
  │    ├─> Consumida
  │    ├─> Liberada
  │    ├─> Expirada
  │    └─> Cancelada
  └─> Cancelada
```

Reglas principales:

| Operación | Estado de origen | Efecto sobre StockReservado | Estado destino |
|---|---|---:|---|
| Crear | — | 0 | Borrador |
| Editar | Borrador | 0 | Borrador |
| Activar | Borrador | +cantidad | Activa |
| Consumir | Activa | -cantidad pendiente | Consumida |
| Liberar | Activa | -cantidad | Liberada |
| Expirar | Activa y fecha alcanzada | -cantidad | Expirada |
| Cancelar | Borrador | 0 | Cancelada |
| Cancelar | Activa | -cantidad | Cancelada |

Las transiciones terminales son idempotentes en el sentido de que no deben duplicar la mutación de stock reservado si se repite una petición ya materializada.

## 5. Prevención de overselling y concurrencia

La activación no se implementa como “leer disponible → calcular → guardar” fuera de una sección crítica. Se utiliza el servicio de concurrencia de `ExistenciaVariante` para:

1. ordenar/bloquear las claves físicas requeridas;
2. validar existencia y disponibilidad real;
3. incrementar `StockReservado` con valores esperados;
4. confirmar el documento dentro de la misma transacción.

Al retirar una reserva activa se bloquea nuevamente la misma clave física y se valida que el stock reservado autoritativo sea suficiente antes de decrementar. Una divergencia hace fallar cerrado; no se corrige silenciosamente.

## 6. Aplicación y API

Superficie HTTP de `ReservasInventarioController`:

| Método | Ruta | Propósito | Permiso |
|---|---|---|---|
| GET | `/reservas-inventario` | búsqueda paginada/filtros | `MovimientosInventario:Ver` |
| GET | `/reservas-inventario/{id}` | detalle | `MovimientosInventario:Ver` |
| POST | `/reservas-inventario` | crear borrador | `MovimientosInventario:Crear` |
| PUT | `/reservas-inventario/{id}` | editar borrador | `MovimientosInventario:Editar` |
| POST | `/reservas-inventario/{id}/activar` | comprometer stock | `MovimientosInventario:Confirmar` |
| POST | `/reservas-inventario/{id}/consumir` | consumir compromiso | `MovimientosInventario:Confirmar` |
| POST | `/reservas-inventario/{id}/liberar` | liberar compromiso | `MovimientosInventario:Anular` |
| POST | `/reservas-inventario/{id}/expirar` | expirar compromiso | `MovimientosInventario:CambiarEstado` |
| POST | `/reservas-inventario/{id}/cancelar` | cancelar | `MovimientosInventario:Anular` |

El controlador requiere autenticación y no expone bypass `AllowAnonymous`. Los errores de dominio se propagan mediante el contrato global de errores/ProblemDetails de Solqaryn.

La especificación Swagger/OpenAPI se deriva de los controladores/DTOs de la API; N1.8 no introduce un contrato manual paralelo.

## 7. Seguridad, auditoría y observabilidad

N1.8.F endureció la auditoría crítica de Reservas:

- `IAuditoriaService` es dependencia obligatoria del servicio de aplicación;
- la auditoría se ejecuta mediante `RegistrarEstrictoAsync`;
- la escritura de auditoría ocurre **dentro del mismo `IUnitOfWork`** que muta estado y `StockReservado`;
- si la auditoría no puede persistirse, la operación propaga el error y la transacción de negocio no debe confirmarse;
- se eliminó la auditoría tolerante post-commit del controlador;
- el `CorrelationId` de auditoría procede del `TraceIdentifier` ya saneado por middleware, no del header bruto del cliente.

El payload de auditoría conserva número, venta, estado, fechas relevantes, actor y detalle físico/cantidades suficiente para reconstruir la intención sin convertir logs en una autoridad alternativa de inventario.

## 8. Frontend y UX

N1.8.E materializó la experiencia de Reservas con:

- lista y detalle de reservas;
- creación/edición de borradores;
- selector basado en existencia física real;
- visualización diferenciada de físico/reservado/disponible;
- acciones de lifecycle según estado y permiso;
- validación de fecha de expiración futura;
- bloqueo de expiración prematura;
- filtros, loading, vacío y errores;
- responsive y navegación por teclado/accesibilidad;
- rutas y menú protegidos por permisos relacionales.

La validación cliente mejora UX pero no sustituye las invariantes del backend.

## 9. Persistencia y migración

N1.8.C incorporó persistencia de cabecera/detalle y relaciones con Venta, Variante, Almacén y Ubicación. La migración no realiza movimientos de stock ni inventa reservas históricas. El procedimiento de preflight/postcheck/rollback está documentado en:

- `docs/RUNBOOK_N1_8_RESERVAS_MIGRACION.md`

Principios de datos:

- no backfill especulativo de reservas legacy;
- FKs y checks protegen identidad y cantidades;
- la ubicación, cuando existe, debe corresponder al almacén;
- el número de reserva es único;
- el rollback destructivo no es seguro si ya existen reservas reales: ante ese escenario se requiere corrección forward o restauración controlada compatible.

## 10. QA y evidencia

Baseline funcional final previo a documentación: `95baf2763b912e1015a3bdd25a37aca649e34c37`.

Gates de N1.8.G sobre ese mismo SHA:

```text
Development  #32035509947  SUCCESS (5/5)
Acceptance   #32035509805  SUCCESS
Fase 8       #32035509973  SUCCESS
M10          #32035509930  SUCCESS
M13          #32035509964  SUCCESS
```

M13 confirmó frontend, backend, MySQL/migraciones/upgrade, Docker/backup, secretos/higiene/dependencias, seguridad HTTP, Runtime, Playwright integral, SMTP/PDF/logs y `Dictamen automatizado M13`.

Regresiones N18 cubren, entre otras, identidad física, `StockReservado`, idempotencia de lifecycle, fecha de expiración, RBAC exacto y auditoría transaccional fail-closed.

## 11. Operación y rollback

El runbook operativo permanente es `docs/RUNBOOK_N1_8_RESERVAS.md`. Principios:

- nunca reparar `StockReservado` manualmente en Producción desde este flujo;
- diagnosticar primero documento, clave física, auditoría y correlación;
- ante divergencia, detener la transición y corregir forward con evidencia;
- no ejecutar rollback de esquema si existen datos reales sin estrategia de preservación;
- no usar force-push, merge a `main` ni cambios productivos para “cerrar” N1.8.

## 12. Fuera de alcance

N1.8 no implementa lotes, series, IMEI, fechas de vencimiento por lote ni trazabilidad serializada. Esos conceptos pertenecen a fases posteriores y no deben mezclarse con la reserva física actual.

## 13. Dictamen

ERP-N1.8 queda funcionalmente completo cuando este paquete documental y su checkpoint CI estén verdes. La arquitectura final mantiene una sola autoridad cuantitativa (`ExistenciaVariante`), una explicación documental del compromiso (`ReservaInventario`), concurrencia pesimista contra overselling y auditoría crítica transaccional fail-closed.
