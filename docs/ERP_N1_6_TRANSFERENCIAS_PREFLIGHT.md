# ERP-N1.6 — Transferencias de inventario

## N1.6.A — Auditoría y preflight

Fecha: 2026-08-16
Rama autorizada: `Desarrollo`
Estado: preflight técnico dirigido

## 1. Objetivo

Preparar la implementación de transferencias internas de inventario entre almacenes sin alterar todavía el modelo físico existente. El flujo objetivo del punto N1.6 es:

`Borrador -> Solicitada -> Aprobada -> EnTransito -> Recibida`

Debe soportar recepción parcial, faltantes, sobrantes, daños y cancelaciones con trazabilidad completa.

## 2. Baseline confirmado

No existe actualmente una entidad o implementación llamada `TransferenciaInventario` en el código indexado/inspeccionado de `Desarrollo`; por tanto N1.6 parte de una capacidad nueva y no de un CRUD legacy que deba preservarse.

La autoridad de stock vivo ya está normalizada en `ExistenciaVariante`, cuya clave física es:

- `ProductoVarianteId`
- `AlmacenId`
- `UbicacionAlmacenId` opcional

`ExistenciaVariante` mantiene `StockFisico`, `StockReservado`, `StockDisponible`, `StockTransito`, `StockMinimo` y `StockMaximo`, e impide estados inválidos como stock negativo o reservado superior al físico.

`Almacen` ya pertenece a `Sucursal`, de modo que una transferencia debe operar entre almacenes existentes y no introducir una segunda autoridad para sucursal/empresa.

`MovimientoInventario` ya conserva contexto físico (`AlmacenId`, `UbicacionAlmacenId`), `CorrelationId`, snapshots de variante y origen relacional tipado para Compra/Venta/Consumo/Ajuste. N1.6 deberá extender esta trazabilidad para Transferencia sin volver a depender de `ReferenciaTipo/ReferenciaId` como autoridad.

## 3. Alcance funcional propuesto

### Cabecera

`TransferenciaInventario` debe contener como mínimo:

- `Id`
- `Numero` o correlativo empresarial
- `AlmacenOrigenId`
- `AlmacenDestinoId`
- `Estado`
- `SolicitadaPorUsuarioId` / `FechaSolicitud`
- `AprobadaPorUsuarioId` / `FechaAprobacion`
- `DespachadaPorUsuarioId` / `FechaDespacho`
- `RecibidaPorUsuarioId` / `FechaRecepcion`
- motivo y auditoría de cancelación
- observaciones
- campos auditables estándar

### Detalle

Cada detalle debe conservar al menos:

- `ProductoVarianteId`
- `UbicacionOrigenId` opcional
- `UbicacionDestinoId` opcional
- `CantidadSolicitada`
- `CantidadAprobada`
- `CantidadDespachada`
- `CantidadRecibida`
- `CantidadFaltante`
- `CantidadSobrante`
- `CantidadDanada`
- snapshots de SKU/marca/modelo/color/talla cuando correspondan

Las cantidades derivadas no deben convertirse en entradas independientes si pueden calcularse de forma determinística.

## 4. Invariantes de dominio obligatorias

1. Almacén origen y destino deben ser distintos.
2. Ambos almacenes deben existir, estar activos y no eliminados.
3. Una ubicación origen debe pertenecer al almacén origen; una ubicación destino debe pertenecer al almacén destino.
4. No se permiten líneas con cantidad solicitada <= 0.
5. No se permite despachar más de lo aprobado sin una operación empresarial explícita que lo autorice.
6. No se permite recibir más de lo despachado salvo que el sobrante quede registrado explícitamente como discrepancia y pase por una regla de aceptación separada.
7. La suma `recibida + faltante + dañada` no puede superar lo despachado; cualquier sobrante debe representarse fuera de esa suma como discrepancia positiva.
8. `Borrador` es editable; después de `Solicitada` la edición estructural queda bloqueada salvo transición/reversión formal.
9. `Aprobada` no debe mover stock todavía.
10. `EnTransito` debe representar el despacho físico: decremento controlado del stock físico de origen y aumento de `StockTransito` en la dimensión destino/transferencia según el diseño final de N1.6.B/C.
11. `Recibida` debe materializar el ingreso real en destino y reducir el tránsito correspondiente.
12. Cancelar no puede dejar stock físico o tránsito desbalanceado; la reversión depende del estado alcanzado y debe ser idempotente.
13. Toda transición debe fallar cerrado si no puede tomar locks sobre las existencias físicas afectadas.

## 5. Integración con inventario físico

La transferencia no debe modificar directamente cantidades legacy. La autoridad seguirá siendo `ExistenciaVariante`.

La implementación deberá reutilizar el patrón de concurrencia pesimista ya aplicado en N1.4:

- bloquear por clave física de origen antes de despachar;
- validar disponibilidad real contra `StockFisico/StockReservado`;
- aplicar el delta de origen de forma atómica;
- representar tránsito sin perder correlación de la transferencia;
- bloquear la clave física de destino antes de recibir;
- materializar el stock recibido en destino de forma atómica;
- mantener cualquier bridge legacy únicamente si todavía existe durante el cutover, nunca como autoridad.

## 6. Kardex y trazabilidad

Cada transición física debe emitir movimientos correlacionados mediante el writer canónico de Kardex introducido en N1.5.

Se requieren al menos eventos diferenciables para:

- despacho de transferencia;
- recepción de transferencia;
- reversión/cancelación cuando ya hubo afectación física;
- ajustes por faltante/daño/sobrante cuando correspondan.

El `CorrelationId` debe ser determinístico por transferencia + operación, evitando duplicados ante reintentos.

N1.6 deberá añadir `TransferenciaInventarioId` como origen relacional tipado de `MovimientoInventario`/Kardex o un mecanismo relacional equivalente. No se considera suficiente reutilizar únicamente `ReferenciaTipo/ReferenciaId`.

## 7. API y aplicación esperadas

La fase de aplicación deberá separar comandos y consultas de forma coherente con los módulos ERP actuales. Endpoints mínimos esperados:

- listado paginado con filtros por estado, origen, destino, fecha y correlativo;
- detalle por id;
- crear borrador;
- editar borrador;
- solicitar;
- aprobar/rechazar;
- despachar;
- registrar recepción parcial/final con discrepancias;
- cancelar cuando la transición lo permita.

Cada endpoint de transición debe aplicar RBAC específico y devolver errores fail-closed/ProblemDetails consistentes.

## 8. Frontend/UX esperado

Pantallas mínimas:

- bandeja de transferencias con filtros y estados;
- creación/edición de borrador;
- detalle con timeline de estados;
- aprobación;
- despacho;
- recepción con captura de recibidos/faltantes/dañados/sobrantes;
- visualización de auditoría y correlación Kardex.

La UI no debe permitir seleccionar la misma bodega como origen/destino ni ubicaciones fuera del almacén seleccionado.

## 9. Persistencia y migración

N1.6.C deberá crear tablas normalizadas de cabecera/detalle y constraints suficientes para impedir estados imposibles a nivel de datos cuando sea viable. La migración debe ser reversible o acompañarse de rollback explícito.

No existe backfill obligatorio de transferencias históricas porque no se detectó una entidad legacy equivalente. Si durante N1.6.B/C aparece una fuente histórica real, deberá documentarse antes de migrarla; no se inventarán registros sintéticos.

## 10. Seguridad y auditoría

Permisos propuestos, sujetos al catálogo vigente:

- `TransferenciasInventario:Ver`
- `TransferenciasInventario:Crear`
- `TransferenciasInventario:Editar`
- `TransferenciasInventario:Solicitar`
- `TransferenciasInventario:Aprobar`
- `TransferenciasInventario:Despachar`
- `TransferenciasInventario:Recibir`
- `TransferenciasInventario:Cancelar`

Cada transición de estado debe registrar usuario, fecha, estado anterior/nuevo y motivo cuando aplique dentro de la misma unidad transaccional de la mutación de negocio.

## 11. Riesgos identificados

- doble despacho o doble recepción por reintentos/concurrencia;
- stock negativo en origen si se valida fuera del lock físico;
- tránsito huérfano si despacho y Kardex no son atómicos;
- recepción en ubicación que no pertenece al almacén destino;
- cierre prematuro de una transferencia con recepción parcial;
- discrepancias no balanceadas entre despachado y recibido;
- pérdida de trazabilidad si no se añade origen relacional de transferencia al Kardex;
- deadlocks por adquirir múltiples existencias en orden no determinístico.

Mitigación obligatoria: adquirir locks físicos en orden determinístico de clave, transacciones atómicas, idempotencia por operación y regresiones de concurrencia.

## 12. Estrategia de rollback

- Antes de despacho: cancelación lógica sin tocar stock.
- Después de despacho y antes de recepción: reversión transaccional de stock/tránsito mediante la misma clave física y correlación.
- Tras recepción parcial: cualquier cancelación debe revertir únicamente cantidades físicamente afectadas y conservar evidencia de discrepancias; no borrar movimientos.
- Migraciones: `Down()` coherente o script de rollback probado en entorno controlado antes del cierre de N1.6.C.

## 13. Pruebas obligatorias

Dominio:

- lifecycle válido e inválido;
- origen != destino;
- cantidades y discrepancias;
- cancelaciones por estado.

Persistencia:

- FKs, índices y constraints;
- ubicaciones pertenecen al almacén correcto;
- snapshot EF sin pending model changes.

Aplicación/concurrencia:

- doble despacho;
- doble recepción;
- stock insuficiente;
- recepción parcial y cierre final;
- faltante/daño/sobrante;
- reversión por cancelación;
- locks ordenados en transferencias con múltiples variantes.

API/RBAC:

- permiso por operación;
- usuario sin permiso falla cerrado;
- ProblemDetails y conflictos 409 cuando corresponda.

Frontend/E2E:

- crear -> solicitar -> aprobar -> despachar -> recibir;
- recepción parcial;
- cancelación válida/inválida;
- controles de almacén/ubicación;
- usuario sin permiso.

## 14. Criterio de cierre de N1.6.A

N1.6.A queda técnicamente listo cuando este preflight está publicado y reconciliado con COLA, las dependencias N1.4.H/N1.5.H están cerradas y N1.6.B puede iniciar sin reabrir auditoría global.

Siguiente acción: `N1.6.B — Dominio y contratos`, implementando exclusivamente el agregado, estados, invariantes y DTOs/contratos necesarios antes de persistencia o API.
