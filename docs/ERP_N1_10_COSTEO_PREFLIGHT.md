# ERP-N1.10 — Costeo empresarial — Auditoría y preflight

## 1. Propósito

Definir el punto de partida real de ERP-N1.10 antes de introducir persistencia o integración runtime. El objetivo rector es disponer de **una política de costeo única, coherente, reversible y auditable para la empresa**, contemplando Promedio Ponderado Móvil, FIFO y Costo Estándar cuando corresponda, sin romper históricos ni crear una segunda autoridad de stock.

Este documento es PRE: no introduce migraciones, endpoints ni UI funcionales de N1.10.C–H.

## 2. Dependencias y límites

- Dependencia VAEP directa: `N1.5.H` — LISTO.
- `N1.9.H` está cerrado y no se reabre.
- `ExistenciaVariante` continúa como única autoridad cuantitativa del stock vivo por Variante + Almacén + Ubicación.
- ERP-N1.10 define **valoración**, no una segunda cantidad.
- ERP-N6 sigue siendo responsable de multiempresa/SaaS. N1.10 no inventa una entidad tenant ni una FK `Empresa` inexistente.
- `EmpresaConfiguracion` activa única representa el ámbito empresarial efectivo del sistema actual single-company; N6 podrá tenantizar esta política sin reinterpretar históricos.

## 3. Estado real encontrado

### 3.1 Autoridades actuales de costo

Existen dos proyecciones persistidas de costo corriente:

- `Producto.Costo` — costo consolidado de compatibilidad a nivel producto.
- `ProductoVariante.Costo` — costo operativo a nivel variante.

La confirmación de compra aplica hoy **Promedio Ponderado Móvil** dentro de `CompraService`:

- Variante: `(valor anterior + valor entrada) / stock nuevo`.
- Producto consolidado: `(costo anterior * stock anterior + valor entrada) / stock nuevo`.
- Redondeo: 2 decimales con `MidpointRounding.AwayFromZero`.

Por tanto, Promedio Ponderado ya es la política de facto, pero no existe todavía una política empresarial explícita ni un boundary único de costeo.

### 3.2 Cantidad y valoración están correctamente separadas en N1.4

`ExistenciaVariante` contiene `StockFisico`, `StockReservado`, `StockDisponible`, `StockTransito`, mínimos y máximos, pero no costo. Esta separación debe preservarse.

`MovimientoInventario` conserva `CostoUnitario` nullable como snapshot del evento, además de cantidad, stock anterior/nuevo, origen tipado y `CorrelationId`. Esto es base de auditoría/Kardex, pero no constituye por sí mismo un motor FIFO o Estándar.

### 3.3 Compras: cálculo en servicio, snapshots/reversión en persistencia

`CompraDetalle.CostoUnitario` conserva el costo real de entrada y posee snapshots de valoración anterior/nueva para Producto y Variante.

La revisión completa del flujo confirmó que esos snapshots **sí se materializan**. `AppDbContext.SaveChangesAsync` llama a `PrepararValorizacionComprasAsync`; ante transición `Borrador → Confirmada`, `CapturarSnapshotsValorizacion` lee `OriginalValues` y valores nuevos de Producto/Variante y los persiste en los detalles.

Ante transición `Confirmada → Anulada`, `RestaurarValorizacionAsync` valida que los snapshots estén completos, restaura costo/stock de Variante y recompone el agregado de Producto. La operación falla cerrada si el estado físico no coincide con el esperado.

Esto corrige una conclusión preliminar de la primera lectura de `CompraService`: **la reversión de valoración sí existe**. El problema real es arquitectónico: la política está fragmentada entre `CompraService` y `AppDbContext`, mezclando reglas contables con el boundary de persistencia.

### 3.4 Ventas: COGS congelado demasiado pronto

`VentaDetalle` persiste `CostoUnitarioSnapshot` y `UtilidadBruta`.

`VentaService.ArmarDetallesAsync` toma hoy `ProductoVariante.Costo` al crear/editar el borrador. La confirmación física posterior reutiliza ese snapshot. Si el costo cambia mientras la venta permanece en borrador, COGS y utilidad pueden quedar obsoletos.

N1.10 debe congelar el costo contable de salida **en la confirmación autoritativa**, bajo la misma transacción/lock del inventario, y persistirlo como historia inmutable.

### 3.5 Ámbito empresarial actual

No existe `Domain/Entities/Empresa.cs`. `Sucursal.EmpresaId` está expresamente reservado para ERP-N6 y no es todavía una relación tenant-aware.

Sí existe `EmpresaConfiguracion` activa única. N1.10 puede vincular inicialmente su política a esa configuración y dejar la tenantización para N6.

## 4. Riesgos confirmados

### R1 — Política fragmentada entre Application y Persistence

El cálculo promedio está en `CompraService`, mientras captura y reversión están en `AppDbContext.SaveChangesAsync`. Un método nuevo obligaría a extender dos boundaries que no deberían poseer política contable de forma independiente.

**Dirección:** extraer la autoridad a `ICosteoInventarioService`/estrategias transaccionales y dejar al DbContext como persistencia/invariantes estructurales, no como motor contable.

### R2 — Doble proyección Producto/Variante

`Producto.Costo` y `ProductoVariante.Costo` pueden divergir si un flujo nuevo actualiza uno sin reconciliar el otro.

**Dirección:** `ProductoVariante` es la unidad mínima valorable; `Producto.Costo` queda como proyección consolidada/compatibilidad.

### R3 — Costo de venta congelado en borrador

Una venta puede confirmar COGS/utilidad de un costo viejo aunque el stock físico se bloquee correctamente al confirmar.

### R4 — FIFO no tiene capas contables

No existe una entidad de capas de costo ni asignaciones de consumo. `LoteInventario` N1.9 representa identidad logística, no autoridad contable; no debe reutilizarse como capa FIFO.

### R5 — Costo estándar no tiene vigencia ni variación

No existe costo estándar temporal por Variante ni evidencia de variación real vs estándar.

### R6 — Transferencias y ajustes

- Transferir inventario no debe crear utilidad ni modificar el valor empresarial total; FIFO debe preservar procedencia de capas.
- Ajuste negativo consume costo según política.
- Ajuste positivo exige fuente explícita de valoración.
- Conteos físicos sólo afectan costo cuando materializan un Ajuste.

### R7 — Históricos

No se puede reinterpretar COGS/utilidad cerrados al cambiar política. El cutover debe ser forward-only, temporalmente versionado y auditable.

## 5. Decisiones de arquitectura para N1.10.B–H

### 5.1 Principio de autoridad

- **Cantidad:** `ExistenciaVariante`.
- **Unidad mínima de valoración:** `ProductoVariante`.
- **Producto.Costo:** proyección consolidada de compatibilidad.
- **Costo histórico de salida:** snapshot/asignación persistida en confirmación, nunca recalculado al consultar.
- **Política:** versión temporal única para el ámbito empresarial actual.
- **Reversión:** usa evidencia histórica original, nunca la política/costo corriente del momento de anular.

### 5.2 Métodos objetivo

#### Promedio Ponderado Móvil

Default de compatibilidad. Debe preservar el algoritmo actual y su reversibilidad, pero mover la lógica desde `CompraService`/`AppDbContext` hacia el boundary de costeo.

#### FIFO

Requiere `CapaCostoInventario` independiente de lotes logísticos:

- Variante.
- Almacén/Ubicación cuando aplique.
- cantidad original/restante.
- costo unitario.
- fecha/orden durable.
- movimiento origen/correlation.

Cada salida debe persistir una o varias asignaciones a capas, de modo que COGS sea inmutable y auditable.

#### Costo Estándar

Requiere valor estándar versionado por Variante y registro de variación. El costo real de compra se conserva siempre.

### 5.3 Política empresarial sin adelantar N6

- `MetodoCosteoInventario`: `PromedioPonderado`, `FIFO`, `Estandar`.
- Política temporal vinculada a `EmpresaConfiguracion` activa única.
- Default/backfill: `PromedioPonderado` para preservar semántica actual.
- Una sola política vigente en el ámbito actual.
- Un cambio de política crea un nuevo corte temporal; no reescribe históricos.

## 6. Flujos que N1.10 debe cubrir

1. Compra confirmada: entrada valorada según política y evidencia histórica.
2. Compra anulada: reversión basada en evidencia original; preservar protección actual fail-closed.
3. Venta confirmada: congelar COGS en confirmación, no en borrador.
4. Venta anulada: restaurar valoración compatible con el costo histórico de la salida original.
5. Transferencia: trasladar valor sin ganancia/pérdida; FIFO preserva capas/procedencia.
6. Ajuste negativo: consumo según política.
7. Ajuste positivo: valoración explícita/justificada.
8. Conteo físico: diferencias materializadas vía Ajuste obedecen la misma política.
9. Kardex/reportes: reproducir costo histórico sin consultar costo corriente.
10. Reservas: no reconocen COGS ni consumen capas mientras no exista salida física.
11. Lotes/series: identidad logística correlacionable, nunca sustituto de capas contables.

## 7. Persistencia/cutover esperado para N1.10.C

- migraciones aditivas y forward-only;
- política inicial Promedio Ponderado;
- conservar columnas/snapshots existentes durante transición;
- no inventar backfill FIFO sobre históricos sin evidencia;
- si se activa FIFO con stock preexistente, capa de apertura explícita usando costo corriente certificado y marca de cutover;
- Estándar requiere costo vigente explícito antes de activarse con stock;
- unicidad de política vigente;
- capas sin cantidad negativa ni sobreconsumo;
- asignaciones históricas ligadas a movimientos;
- variaciones estándar persistidas;
- snapshot EF sin drift;
- preflight/postcheck y rollback documentados.

## 8. API/UX/RBAC esperados

N1.10.D–F debe separar:

- consulta de política y estado de costeo;
- cambio de política con preflight/cutover;
- mantenimiento versionado de costo estándar;
- consulta auditable de capas/asignaciones/variaciones;
- reportes de costo/COGS/utilidad.

Cambiar política o costo estándar es operación sensible: RBAC explícito, auditoría estricta y correlation obligatorios.

## 9. Matriz mínima de pruebas

### Dominio

- políticas válidas e intervalos temporales;
- promedio exacto/redondeo;
- FIFO parcial y multicapa;
- estándar y variación;
- cantidades/costos inválidos fail-closed.

### Integración/MySQL

- unicidad de política vigente;
- capas FIFO sin saldo negativo;
- asignaciones suman exactamente la salida;
- transferencias preservan valor;
- migración desde baseline real y snapshot EF sin drift.

### Servicios

- compra promedio y reversión preservan el comportamiento certificado actual;
- DbContext deja de poseer la política una vez migrada la autoridad;
- venta en borrador no congela COGS definitivo;
- confirmación congela costo bajo lock;
- FIFO asigna/persiste capas;
- estándar persiste variación;
- reservas no alteran costo.

### E2E/contrato

- consulta/cambio autorizado de política;
- bloqueo de cambio incompatible;
- utilidad consistente con COGS confirmado;
- Kardex/reportes reproducen costo histórico.

## 10. Rollback

Antes de N1.10.C, los cambios A/B son aditivos de documentación/contrato y no alteran runtime.

En implementación posterior:

- no eliminar `Producto.Costo`, `ProductoVariante.Costo`, snapshots de `CompraDetalle`, `VentaDetalle.CostoUnitarioSnapshot` ni `MovimientoInventario.CostoUnitario` durante cutover;
- mantener compatibilidad hasta certificar la nueva autoridad;
- cambio de política con preflight y condición de retorno explícita;
- nunca reinterpretar movimientos históricos al volver/cambiar método.

## 11. Criterio de cierre de N1.10.A

N1.10.A queda cerrado cuando:

- algoritmo implícito y autoridad actual están identificados;
- captura/restauración existente en DbContext está documentada correctamente;
- riesgo de COGS de venta en borrador está identificado;
- Promedio/FIFO/Estándar están separados de cantidad y de N6;
- consumidores, transición, rollback y pruebas están definidos;
- N1.10.B puede continuar sin reescaneo global.
