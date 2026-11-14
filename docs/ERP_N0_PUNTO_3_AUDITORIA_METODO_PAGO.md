# ERP-N0 — Punto 3: Auditoría legacy de `MetodoPago`

**Estado:** ✅ CERRADO FORMALMENTE  
**Fecha de cierre:** 2026-08-11  
**Rama auditada:** `Desarrollo`  
**Baseline auditado:** `759d244dd0b27f028d7d6f69d4445a15e834a703`  
**Hallazgo relacionado:** `F-N0-009` de `docs/ERP_N0_LEGACY_AUDIT.md`

---

## 1. Objetivo del cierre

Cerrar la trazabilidad técnica que faltaba para el Punto 3 de ERP-N0 antes de retirar el enum legacy `MetodoPago`.

Este documento no ejecuta todavía la sustitución funcional ni el DDL de `MetodoPago`. Su objetivo es identificar y clasificar los consumidores actuales que deben migrarse de forma coordinada para poder retirar el enum sin romper datos históricos, contratos API, PDFs, frontend ni pruebas.

El cierre de esta auditoría significa que la dirección y el perímetro de migración están definidos y documentados. El enum **permanece vigente de forma intencional** hasta que la implementación del catálogo normalizado y sus migraciones cumplan los gates definidos al final de este documento.

---

## 2. Fuente legacy actual

Archivo:

- `backend/src/Domain/Enums/MetodoPago.cs`

Contrato actual:

```csharp
public enum MetodoPago
{
    Efectivo = 1,
    Transferencia = 2,
    Tarjeta = 3,
    Otro = 4
}
```

Los cuatro valores forman parte de contratos compilados, strings expuestos por API/UI y datos persistidos. No deben eliminarse, renombrarse ni renumerarse antes del backfill y de la compatibilidad correspondiente.

---

## 3. Trazabilidad por capa

### 3.1 Dominio y persistencia

| Archivo | Dependencia | Persistencia/impacto | Acción futura |
|---|---|---|---|
| `backend/src/Domain/Entities/Compra.cs` | `MetodoPago` obligatorio | Documento histórico de compra | migrar a `MetodoPagoId` + código/snapshot histórico |
| `backend/src/Domain/Entities/Venta.cs` | `MetodoPago` obligatorio | Documento histórico de venta | migrar a `MetodoPagoId` + código/snapshot histórico |
| `backend/src/Domain/Entities/FacturaPago.cs` | `MetodoPago` obligatorio | Cada pago registrado de una factura | migrar a `MetodoPagoId` preservando pagos históricos |
| `backend/src/Domain/Entities/MovimientoFinanciero.cs` | `MetodoPago?` nullable | Movimientos manuales/automáticos | migrar a FK/código normalizado nullable según regla de negocio |
| `backend/src/Infrastructure/Persistence/Configurations/CompraConfiguration.cs` | `HasConversion<string>()` | `varchar(20)` | backfill de strings a catálogo |
| `backend/src/Infrastructure/Persistence/Configurations/VentaConfiguration.cs` | `HasConversion<string>()` | `varchar(20)` | backfill de strings a catálogo |
| `backend/src/Infrastructure/Persistence/Configurations/MovimientoFinancieroConfiguration.cs` | `HasConversion<string>()` | `varchar(20)` nullable | backfill de strings a catálogo |
| `backend/src/Infrastructure/Persistence/Configurations/FacturaPagoConfiguration.cs` | no define conversión string | EF conserva representación numérica del enum | migrar enteros históricos 1..4 a IDs/códigos canónicos |
| `backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | refleja las cuatro columnas actuales | esquema mixto: Compra/Venta/Movimiento como string y FacturaPago como `int` | migración debe contemplar ambas representaciones |

### Hallazgo crítico de persistencia

La base actual no representa `MetodoPago` de una sola manera:

- `Compra.MetodoPago`: `varchar(20)` requerido.
- `Venta.MetodoPago`: `varchar(20)` requerido.
- `MovimientoFinanciero.MetodoPago`: `varchar(20)` nullable.
- `FacturaPago.MetodoPago`: `int` requerido.

Por tanto, el reemplazo no puede ser una simple eliminación del enum. Debe existir un backfill dual:

1. strings históricos `Efectivo`, `Transferencia`, `Tarjeta`, `Otro`;
2. enteros históricos `1`, `2`, `3`, `4` de `FacturaPagos`.

---

### 3.2 DTOs y contrato API

Consumidores confirmados:

- `backend/src/Application/DTOs/CompraDto.cs`
- `backend/src/Application/DTOs/VentaDto.cs`
- `backend/src/Application/DTOs/FacturaDto.cs`
- `backend/src/Application/DTOs/FinanzasDto.cs`

Dependencias relevantes:

- Compras y ventas reciben/exponen el método de pago como parte del documento.
- `FacturaPagoDto` expone el método como string.
- `RegistrarFacturaPagoDto` recibe el método como string.
- `MovimientoFinancieroDto` expone `MetodoPago` nullable como string.
- `CreateMovimientoManualDto` recibe `MetodoPago` nullable como string.

Los contratos actuales no pueden cambiarse de golpe a un ID sin una transición explícita. La migración debe mantener compatibilidad por `Codigo` estable o versionar el contrato.

Controladores que transportan estos DTOs y por tanto quedan dentro del perímetro de regresión API:

- `backend/src/API/Controllers/ComprasController.cs`
- `backend/src/API/Controllers/VentasController.cs`
- `backend/src/API/Controllers/FacturasController.cs`
- `backend/src/API/Controllers/FinanzasController.cs`

No se requiere lógica de catálogo hardcodeada en los controladores; el acoplamiento principal está en DTOs/servicios y debe conservarse compatible durante la sustitución.

---

### 3.3 Servicios de aplicación

| Archivo | Comportamiento legacy confirmado | Riesgo al retirar enum |
|---|---|---|
| `backend/src/Application/Services/CompraService.cs` | asigna/mapea `MetodoPago` y lo propaga a operaciones financieras | romper creación/edición/confirmación y movimientos asociados |
| `backend/src/Application/Services/VentaService.cs` | asigna/mapea `MetodoPago` y lo propaga a facturación/movimientos | romper ventas, facturas y movimientos automáticos |
| `backend/src/Application/Services/FacturaService.cs` | `Enum.TryParse<MetodoPago>` al registrar pago; persiste `FacturaPago.MetodoPago`; expone `.ToString()` | rechazar nuevos códigos o perder compatibilidad de pagos |
| `backend/src/Application/Services/FinanzasService.cs` | `Enum.TryParse<MetodoPago>` + `Enum.IsDefined`; expone `.ToString()` | movimientos manuales quedarían validados contra catálogo compilado inexistente |

La sustitución correcta es resolver el método mediante catálogo/repositorio por `Codigo`, no mediante `Enum.TryParse`.

---

### 3.4 Filtros, búsquedas y reportes

Se revisaron los filtros HTTP/API existentes bajo `backend/src/API/Filters`:

- `BusquedaRendimientoMetricas.cs`
- `MedirRendimientoBusquedaFilter.cs`
- `ProductoVarianteOperacionFilter.cs`
- `RequiereAlgunoPermisoAttribute.cs`
- `RequierePermisoAttribute.cs`

**Resultado:** no son consumidores directos de `MetodoPago`; no existe actualmente un filtro HTTP específico de método de pago que requiera migración semántica.

Se revisó también `backend/src/Infrastructure/Services/ReporteAdministrativoService.cs` y el frontend de reportes administrativos.

**Resultado:** los reportes administrativos actuales están centrados en usuarios, roles/permisos y auditoría; no consumen directamente el enum `MetodoPago`.

Sí existe una búsqueda funcional relacionada en:

- `frontend/src/app/features/finanzas/finanzas.component.ts`

La búsqueda local de movimientos incluye `m.metodoPago`, por lo que el texto/nombre presentado por el futuro catálogo debe seguir siendo searchable y estable para la UX.

Conclusión de esta categoría: **reportes administrativos y filtros API revisados, sin dependencia directa; búsqueda financiera sí depende del valor textual expuesto**.

---

### 3.5 PDF y presentación documental

Consumidor confirmado:

- `backend/src/Infrastructure/Services/QuestPdfFacturaPerfilesService.cs`

El generador imprime directamente el valor de `factura.MetodoPago` en perfiles de papel y térmicos.

Riesgo:

- cambiar el contrato a un ID técnico produciría PDFs ilegibles si no se resuelve previamente el nombre/código de presentación;
- renombrar valores históricos cambiaría la representación documental.

Regla de migración:

- el DTO de factura usado por PDF debe continuar entregando un nombre/código de negocio legible;
- los documentos históricos deben seguir renderizando el método registrado en su momento.

---

### 3.6 Frontend — modelos y UI

#### Modelos TypeScript

Consumidores confirmados:

- `frontend/src/app/core/models/compra.model.ts`
- `frontend/src/app/core/models/venta.model.ts`
- `frontend/src/app/core/models/factura.model.ts`
- `frontend/src/app/core/models/finanzas.model.ts`

Hallazgo:

- `Compra.metodoPago` y `Venta.metodoPago` replican literalmente el union type `'Efectivo' | 'Transferencia' | 'Tarjeta' | 'Otro'`.
- Factura y Finanzas usan `string`, creando una inconsistencia de tipado entre módulos.

#### Formularios y componentes

Consumidores hardcodeados confirmados:

- `frontend/src/app/features/compras/compra-form.component.ts`: default `Efectivo`.
- `frontend/src/app/features/compras/compra-form.component.html`: cuatro `<mat-option>` hardcodeados.
- `frontend/src/app/features/ventas/venta-form.component.ts`: default de método de pago ligado al contrato legacy.
- `frontend/src/app/features/ventas/venta-form.component.html`: cuatro `<mat-option>` hardcodeados.
- `frontend/src/app/features/facturas/factura-pagos.component.ts`: `metodosPago = ['Efectivo', 'Transferencia', 'Tarjeta', 'Otro']` y default `Efectivo`.
- `frontend/src/app/features/facturas/factura-pagos.component.html`: consume esa lista para registrar pagos.
- `frontend/src/app/features/facturas/factura-view.component.*`: presenta método de pago proveniente de factura/pagos.
- `frontend/src/app/features/finanzas/finanzas.component.ts`: default `Efectivo` y búsqueda por `m.metodoPago`.
- `frontend/src/app/features/finanzas/finanzas.component.html`: UI de movimiento manual/presentación del método.

Los servicios HTTP frontend de compra, venta, factura y finanzas transportan estos contratos a la API y deberán mantenerse compatibles durante la transición.

#### Sustituto frontend

No duplicar un nuevo catálogo en TypeScript. Consumir un endpoint de métodos de pago activos, ordenados y con metadata de comportamiento, manteniendo `Codigo` estable para interoperabilidad.

---

### 3.7 Pruebas

Pruebas directamente relacionadas revisadas:

- `backend/tests/Solqaryn.Tests/CompraServiceTests.cs`
- `backend/tests/Solqaryn.Tests/VentaServiceTests.cs`
- `backend/tests/Solqaryn.Tests/FacturaServicePagosTests.cs`
- `backend/tests/Solqaryn.Tests/FinanzasServiceTests.cs`
- `backend/tests/Solqaryn.Tests/MovimientoFinancieroRepositoryTests.cs`
- `backend/tests/Solqaryn.Tests/QuestPdfFacturaPerfilesServiceTests.cs`
- `frontend/e2e/fase6-facturacion-impresion.spec.ts`
- `frontend/e2e/fase7-validacion-integral.spec.ts`
- `frontend/e2e/fase8-validacion-completa.spec.ts`

Cobertura actual confirmada:

- `FacturaServicePagosTests` registra pagos con `Transferencia` y `Efectivo` y usa `MetodoPago.Efectivo` en anulación/recalculo.
- `QuestPdfFacturaPerfilesServiceTests` construye factura con `MetodoPago = "Efectivo"`.
- suites E2E recorren rutas de compras, ventas y finanzas y validan facturación/impresión en sus fases correspondientes.

Brecha de cobertura detectada antes de retirar el enum:

- no existe una certificación única que recorra los cuatro códigos legacy de extremo a extremo;
- Compra/Venta/Finanzas no fijan actualmente una matriz completa de compatibilidad del método de pago;
- debe añadirse prueba de rechazo de código inexistente y prueba de compatibilidad durante la migración;
- debe probarse el backfill mixto `varchar` + `int` antes de eliminar columnas/enum legacy;
- E2E debe obtener opciones desde el catálogo y validar que los cuatro códigos históricos siguen operables después del cambio.

Esta brecha **no mantiene abierta la auditoría**: es un requisito de implementación ya identificado y trazado.

---

## 4. Destino técnico aprobado para la implementación

Crear un catálogo normalizado `MetodoPago` con mantenimiento propio y persistencia propia, siguiendo el criterio ya establecido en `F-N0-009`.

Contrato mínimo recomendado:

- `Id`
- `Codigo` único y estable
- `Nombre`
- `Tipo`
- `Activo`
- `RequiereReferencia`
- `RequiereBanco`
- `PermiteCambio`
- `Orden`
- `Metadata`
- campos estándar de auditoría/soft-delete según arquitectura del proyecto

Seed/backfill inicial obligatorio:

| Legacy | Código estable inicial | Valor numérico histórico |
|---|---|---:|
| `Efectivo` | `Efectivo` | 1 |
| `Transferencia` | `Transferencia` | 2 |
| `Tarjeta` | `Tarjeta` | 3 |
| `Otro` | `Otro` | 4 |

Los IDs nuevos del catálogo no deben asumirse iguales a los valores del enum. El mapping debe ser explícito.

---

## 5. Orden seguro de migración

1. Crear tabla/catálogo `MetodoPago` y mantenimiento.
2. Insertar los cuatro códigos históricos mediante seed/migración idempotente.
3. Agregar nuevas FKs/campos de compatibilidad a Compra, Venta, FacturaPago y MovimientoFinanciero sin borrar legacy.
4. Ejecutar preflight de valores distintos/nulls fuera de contrato.
5. Backfill:
   - strings de Compra/Venta/MovimientoFinanciero por `Codigo`;
   - enteros 1..4 de FacturaPago mediante mapping explícito.
6. Hacer dual-read/compatibilidad temporal si el despliegue lo requiere.
7. Migrar servicios de `Enum.TryParse` a resolución por catálogo/código.
8. Migrar DTOs manteniendo contrato compatible o versionándolo de forma controlada.
9. Migrar frontend para cargar catálogo activo desde API y retirar arrays/union types hardcodeados.
10. Migrar PDF para resolver nombre/snapshot de negocio, nunca mostrar un ID.
11. Completar pruebas unitarias, integración/MySQL y Playwright para los cuatro códigos históricos y nuevos métodos configurables.
12. Verificar cero consumidores del enum y cero datos sin mapping.
13. Retirar columnas/compatibilidad legacy y finalmente eliminar `backend/src/Domain/Enums/MetodoPago.cs`.

---

## 6. Gates obligatorios para retirar definitivamente el enum

El enum `MetodoPago` solo puede eliminarse cuando todos estos gates sean ✅:

- [ ] catálogo normalizado y mantenimiento implementados;
- [ ] cuatro códigos históricos sembrados y protegidos por código estable;
- [ ] migración/backfill de Compra completado;
- [ ] migración/backfill de Venta completado;
- [ ] migración/backfill de FacturaPago `int` completado;
- [ ] migración/backfill de MovimientoFinanciero completado;
- [ ] `Enum.TryParse<MetodoPago>` eliminado de servicios;
- [ ] DTOs/API migrados con compatibilidad probada;
- [ ] opciones hardcodeadas retiradas del frontend;
- [ ] union types legacy retirados o reemplazados por contrato de catálogo;
- [ ] PDF conserva representación legible e histórica;
- [ ] filtros/búsquedas/reportes verificados tras migración;
- [ ] pruebas de los cuatro códigos legacy + código inválido en verde;
- [ ] integración MySQL/backfill en verde;
- [ ] Playwright de compra, venta, pago de factura y finanzas en verde;
- [ ] búsqueda final del repositorio sin consumidores productivos de `MetodoPago` enum;
- [ ] migración destructiva separada y reversible/respaldada según política ERP-N0.

---

## 7. Resultado del Punto 3

### ✅ Auditoría legacy — CERRADA

La trazabilidad pendiente quedó formalizada para:

- dominio;
- entidades y persistencia;
- migrations/model snapshot;
- DTOs y contrato API;
- servicios;
- controladores indirectamente afectados;
- filtros y búsquedas;
- reportes;
- PDFs;
- frontend/modelos/UI;
- pruebas unitarias, integración y E2E.

El enum legacy **no se elimina en este commit**, porque hacerlo antes de implementar el catálogo y el backfill violaría los gates definidos arriba y pondría en riesgo datos históricos.

Por tanto, a partir de este documento el estado correcto es:

> **Punto 3 — Auditoría legacy: ✅ CERRADO FORMALMENTE.**  
> **Retiro de `MetodoPago` enum: pendiente de la fase de implementación/migración correspondiente, con perímetro y gates ya definidos.**

---

## 8. Evidencia de alcance

La auditoría se realizó contra el árbol completo de `Desarrollo` del baseline indicado, cuya respuesta de árbol no estaba truncada. Se usaron lecturas específicas por rama para evitar depender del índice de búsqueda de la rama por defecto.

No se modificó `main`, no se creó rama adicional y no se realizó DDL/DML ni cambio de comportamiento productivo en este cierre documental.
