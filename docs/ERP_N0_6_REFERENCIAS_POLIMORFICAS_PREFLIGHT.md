# ERP-N0.6 — Preflight de referencias polimórficas críticas

**Proyecto:** SOLQARYN  
**Repositorio/rama:** `jmejia31/Solqaryn` / `Desarrollo`  
**Microtarea:** `N0.6.A` — Auditoría y preflight  
**Baseline inspeccionado:** `11c958ead2a7a8cc5a3b1db4b502cbe63e8efba7`

## 1. Objetivo

Delimitar con evidencia el retiro de referencias polimórficas críticas antes de tocar dominio o persistencia. Esta microtarea no implementa la migración: identifica autoridades actuales, productores/consumidores, riesgos, dependencias, estrategia de transición, rollback y validaciones para `N0.6.B` en adelante.

## 2. Estado real confirmado

### Inventario — deuda principal

`MovimientoInventario` mantiene `ReferenciaTipo` + `ReferenciaId` como referencia de origen sin FK tipada. La configuración EF exige `ReferenciaTipo`, indexa el par y no impone integridad referencial sobre el documento de origen.

Productores confirmados:

- `CompraService.ConfirmarAsync`: `Compra` + `Compra.Id`.
- `CompraService.AnularAsync`: `CompraAnulada` + `Compra.Id`.
- `VentaService.ConfirmarAsync`: `Venta` + `Venta.Id`.
- `VentaService.AnularAsync`: `VentaAnulada` + `Venta.Id`.
- `ConsumoInsumoService.ConfirmarAsync`: `ConsumoInsumo` + `ConsumoInsumo.Id`.
- `ConsumoInsumoService.AnularAsync`: vuelve a usar `ConsumoInsumo` + `ConsumoInsumo.Id`; el carácter de reversión se expresa además mediante `Tipo`/`Causa`.

El repositorio de inventario usa directamente `ReferenciaTipo == "Compra"` y `ReferenciaId` para localizar los movimientos originales y validar si existen movimientos posteriores antes de permitir una anulación. Por tanto, la migración debe preservar esa garantía transaccional y no puede sustituir el par con un cambio cosmético.

El contrato de salida también expone ambos campos mediante `MovimientoInventarioDto`; `GET /inventario/movimientos` los devuelve actualmente a consumidores autenticados con permiso de lectura.

### Finanzas — migración parcial ya existente

`MovimientoFinanciero` conserva `ModuloOrigen` + `ReferenciaId`, pero ya dispone de FKs tipadas `CompraId`, `VentaId` y `FacturaId`. La configuración EF declara explícitamente que las FKs tipadas son la autoridad relacional y que `ModuloOrigen/ReferenciaId` se conservan como snapshot de auditoría/correlación.

Conclusión: en finanzas N0.6 debe **retirar autoridad polimórfica**, no deshacer el trabajo relacional ya hecho. La eliminación física de snapshots no debe adelantarse mientras existan requisitos de auditoría, compatibilidad o históricos pendientes de certificar.

## 3. Alcance de implementación propuesto

### N0.6.B — dominio y contratos

Diseñar orígenes tipados para `MovimientoInventario`, inicialmente cubriendo como mínimo:

- `CompraId`
- `VentaId`
- `ConsumoInsumoId`

La operación concreta (original, anulación, reversión) debe expresarse por semántica de dominio (`Tipo`/`Causa` u otra propiedad tipada), no codificando estados en strings como `CompraAnulada` o `VentaAnulada`.

Definir una invariante explícita: para movimientos originados por documento debe existir exactamente un origen tipado válido. Los movimientos que en el futuro procedan de ajustes formales deberán enlazar al documento empresarial correspondiente, no reutilizar una pareja string/id genérica.

En finanzas, conservar `CompraId`/`VentaId`/`FacturaId` como autoridad y formalizar el papel no autoritativo de `ModuloOrigen/ReferenciaId`. Los casos `Manual` y `Reversion` deben tratarse como semántica de operación, no como sustituto de una FK cuando existe documento origen.

### N0.6.C — persistencia/migración

1. Añadir FKs nullable e índices de transición en inventario.
2. Crear preflight SQL que agrupe todos los valores históricos de `ReferenciaTipo` y falle cerrado ante valores desconocidos/inconsistentes.
3. Backfill determinista:
   - `Compra` / `CompraAnulada` → `CompraId`.
   - `Venta` / `VentaAnulada` → `VentaId`.
   - `ConsumoInsumo` → `ConsumoInsumoId`.
4. Verificar que cada fila histórica mapeable conserva exactamente su documento origen.
5. Aplicar constraints/invariantes compatibles con datos certificados.
6. Mantener temporalmente columnas legacy durante la transición; la limpieza física corresponde a una fase posterior, particularmente N0.8, después de validar históricos.

## 4. Consumidores que deben migrarse de forma dirigida

- `MovimientoInventario`.
- `MovimientoInventarioConfiguration`.
- `IMovimientoInventarioRepository` / `MovimientoInventarioRepository`.
- `CompraService`.
- `VentaService`.
- `ConsumoInsumoService`.
- `MovimientoInventarioService`.
- `MovimientoInventarioDto`.
- Pruebas de compra/venta/consumo y pruebas de integración MySQL que cubran movimientos/anulaciones.
- Cualquier UI que renderice `ReferenciaTipo/ReferenciaId` solo si la inspección del contrato frontend demuestra consumo efectivo durante la etapa correspondiente.

En finanzas, revisar exclusivamente los consumidores que todavía toman decisiones a partir de `ModuloOrigen/ReferenciaId`; no reescribir consumidores que ya usan las FKs tipadas.

## 5. Riesgos

1. **Colisión semántica de IDs:** `ReferenciaId` por sí solo no identifica tabla/origen.
2. **Valores históricos no catalogados:** un string inesperado impediría un backfill seguro y debe provocar fail-closed.
3. **Anulaciones:** `CompraAnulada` y `VentaAnulada` codifican operación y origen en un mismo string; perder esa distinción sin reemplazo tipado afectaría trazabilidad.
4. **Integridad de anulación de compra:** la lógica actual de “movimientos posteriores” depende del origen legacy; debe reescribirse y probarse antes de retirar autoridad.
5. **Soft delete/históricos:** una FK `Restrict` debe permitir conservar documentos históricos aunque el documento origen quede lógicamente eliminado.
6. **Compatibilidad API:** retirar de inmediato `ReferenciaTipo/ReferenciaId` rompería contratos existentes; la deprecación debe ser explícita y gradual si hay consumidores.
7. **Concurrencia:** cambios de migración y repositorios deben conservar bloqueos/transacciones existentes.
8. **Finanzas:** eliminar snapshots prematuramente degradaría auditoría sin aportar integridad adicional, porque la autoridad tipada ya existe.

## 6. Rollback y seguridad de datos

- No ejecutar ninguna migración en Producción.
- La migración debe ser forward-only y verificable en Desarrollo.
- Antes de retirar columnas legacy, conservar una ventana de compatibilidad con ambos datos y postcheck 1:1.
- El rollback operativo de cada etapa consiste en volver a la lectura legacy mientras las columnas se conserven; una eliminación física solo procede después de respaldo lógico documentado y certificación de históricos.
- Ante cualquier fila no mapeable, detener la migración antes de modificar datos.

## 7. Validaciones requeridas para las siguientes etapas

- Unit tests de invariantes de origen tipado.
- Tests de repositorio para compra, venta y consumo.
- Caso crítico: confirmar compra → movimientos → intentar anulación con/sin movimientos posteriores.
- Casos de venta original/anulada y consumo/reversión.
- Integración MySQL para FK, índices, backfill, preflight fail-closed y postcheck.
- Verificación de que un documento lógicamente eliminado sigue siendo trazable históricamente.
- Contrato/API: compatibilidad durante transición y posterior retiro documentado de campos legacy.
- Regresión de `MovimientoFinanciero` para confirmar que sus FKs tipadas continúan siendo autoridad.

## 8. Criterio de cierre de N0.6.A

Cumplido cuando la deuda real, productores/consumidores, riesgos, estrategia de datos, rollback y pruebas quedan identificados sin introducir cambios funcionales. La siguiente microtarea elegible de este punto es `N0.6.B` — dominio y contratos.

## 9. Archivos inspeccionados

- `backend/src/Domain/Entities/MovimientoInventario.cs`
- `backend/src/Infrastructure/Persistence/Configurations/MovimientoInventarioConfiguration.cs`
- `backend/src/Infrastructure/Repositories/MovimientoInventarioRepository.cs`
- `backend/src/Application/Interfaces/IMovimientoInventarioRepository.cs`
- `backend/src/Application/Services/CompraService.cs`
- `backend/src/Application/Services/VentaService.cs`
- `backend/src/Application/Services/ConsumoInsumoService.cs`
- `backend/src/Application/Services/MovimientoInventarioService.cs`
- `backend/src/Application/DTOs/MovimientoInventarioDto.cs`
- `backend/src/API/Controllers/MovimientosInventarioController.cs`
- `backend/src/Domain/Entities/MovimientoFinanciero.cs`
- `backend/src/Infrastructure/Persistence/Configurations/MovimientoFinancieroConfiguration.cs`

No se realizó reescaneo funcional global ni se modificó código de aplicación.
