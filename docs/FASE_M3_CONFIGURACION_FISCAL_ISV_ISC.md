# FASE M3 — Configuración fiscal ISV/ISC

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**

Fecha: 2026-08-09  
Rama: `Desarrollo`  
PR oficial: `#2 Desarrollo -> main`  
Producción: **sin cambios**

> Este documento describe código, esquema versionado y pruebas en entornos descartables de CI. No afirma inspección ni modificación directa de Producción ni de una instancia externa concreta de Aiven.

---

## 1. Objetivo

Certificar que la configuración fiscal ISV/ISC sea persistente, administrable e históricamente estable, sin reconstruir el módulo de Impuestos que ya existe.

M3 exige que:

- los impuestos sean registros reales de base de datos;
- los defaults sean idempotentes;
- un reinicio nunca sobrescriba una decisión administrativa;
- un borrado lógico no provoque recreación/reactivación;
- la identidad fiscal sea estable;
- los documentos conserven snapshots de la aplicación fiscal realizada;
- permisos y auditoría sigan siendo obligatorios;
- no se recalculen históricos con la configuración fiscal vigente del momento de consulta.

---

## 2. Configuración fiscal persistida

El módulo utiliza la entidad `Impuesto` y persistencia EF/MySQL. `Codigo` tiene índice único y `Tasa` utiliza precisión `decimal(18,4)`.

Los defaults definidos por el propio repositorio son:

| Código | Nombre inicial | Tasa inicial | Activo inicial | Incluido en precio | Operación inicial |
|---|---|---:|---|---|---|
| `ISV15` | ISV 15% | 15% | Sí | Sí | Venta |
| `ISC5` | ISC 5% | 5% | No | Sí | Compra |

Estos valores son defaults de bootstrap del sistema. Después de creados, la base de datos es la fuente persistente y la administración puede modificar nombre, tasa, vigencia, estado, alcance y demás propiedades permitidas sin que el siguiente arranque restaure los defaults.

---

## 3. Seed fiscal idempotente y fail-safe

Se auditó `SeedFiscalService` y se reforzó su comportamiento.

### 3.1 Regla final

Antes de crear `ISV15` o `ISC5`, el seed consulta `Impuestos` con `IgnoreQueryFilters()`.

Esto es necesario porque un impuesto eliminado lógicamente debe seguir contando como una identidad existente. De lo contrario, el query filter lo ocultaría y un reinicio intentaría recrear el mismo código, pudiendo:

- revertir una decisión administrativa;
- reintroducir un impuesto retirado;
- provocar una colisión con el índice único de `Codigo`.

La misma protección se aplicó al descuento inicial que comparte este bootstrap para no dejar un comportamiento asimétrico.

### 3.2 Garantías automatizadas

`SeedFiscalServiceTests` verifica:

- ejecución repetida sin duplicados;
- `ISV15` e `ISC5` creados una sola vez;
- preservación de tasa, nombre, estado e `IncluidoEnPrecio` administrados;
- persistencia a través de un contexto nuevo que simula reinicio;
- no reactivación ni recreación de un impuesto eliminado lógicamente.

`SeedFiscalMySqlIntegrationTests` repite los escenarios críticos contra MySQL 8.4 descartable para validar el comportamiento real de query filters, índice único y persistencia entre contextos.

---

## 4. Identidad fiscal estable

`Codigo` pasa a tratarse explícitamente como identidad técnica estable del impuesto.

### Backend

`ImpuestoService.UpdateAsync` rechaza cualquier intento de modificar el código de un impuesto existente.

Esto evita que, por ejemplo, renombrar `ISV15` provoque que el siguiente arranque interprete que el default desapareció y cree otro impuesto.

### Frontend

En edición, el control `codigo` queda deshabilitado. `getRawValue()` conserva el código original al enviar el formulario, mientras tasa, nombre, vigencia, estado, alcance y demás propiedades administrables continúan editables.

`ImpuestoCodigoEstableTests` certifica que el backend rechaza el renombrado y no ejecuta `Update`/`SaveChanges` ante ese intento.

---

## 5. Snapshot histórico fiscal

El motor de cálculo produce `ImpuestoAplicadoDto` con:

- `ImpuestoId`;
- nombre;
- código;
- tasa;
- base imponible;
- monto;
- `IncluidoEnPrecio`.

### Hallazgo corregido en Venta

`VentaService` ya persistía nombre, código, tasa, base y monto, pero no copiaba `IncluidoEnPrecio` hacia `VentaImpuesto.IncluidoEnPrecioSnapshot`.

El valor podía quedar `false` por defecto aunque el motor hubiera aplicado el impuesto como incluido en precio.

Se corrigió en dos puntos:

1. `CalcularTotalesAsync` ahora persiste `IncluidoEnPrecioSnapshot = i.IncluidoEnPrecio`.
2. `ToDto` devuelve `IncluidoEnPrecio = i.IncluidoEnPrecioSnapshot`.

`VentaFiscalSnapshotTests` protege ambos comportamientos.

### Compras

`CompraService` ya persistía y devolvía correctamente `CompraImpuesto.IncluidoEnPrecioSnapshot`; no requirió corrección.

### Facturación

`FacturaService` obtiene el detalle fiscal desde `Venta.ImpuestosAplicados` y sus snapshots históricos. No consulta la tasa actual del maestro al reconstruir una factura.

El fallback de compatibilidad existente permanece únicamente para documentos históricos generados antes de corregir el flag de Venta.

---

## 6. Administración, permisos y auditoría

Los endpoints de Impuestos permanecen protegidos por autenticación y `RequierePermiso(ModuloSistema.Impuestos, ...)` para acciones como:

- ver;
- crear;
- editar;
- activar;
- desactivar;
- eliminar lógico;
- eliminar permanente cuando la integridad histórica lo permita.

`ImpuestoService` mantiene auditoría para creación, edición, activación, desactivación y eliminación.

Un impuesto con aplicaciones históricas no puede eliminarse de forma que rompa documentos; debe conservarse o desactivarse según corresponda.

---

## 7. Alcances fiscales existentes preservados

M3 no reconstruye la arquitectura ya funcional de Impuestos. Se mantienen las relaciones normalizadas reforzadas en M1:

- Impuesto -> Producto;
- Impuesto -> Categoría;
- Impuesto -> Cliente exento;
- Impuesto -> Proveedor exento;
- Impuesto -> Operación Venta/Compra.

El cálculo utiliza registros activos/vigentes persistidos y sus alcances, no constantes hardcodeadas de ISV/ISC.

---

## 8. Esquema y migraciones

M3 no requiere una migración adicional de esquema para los hallazgos corregidos:

- las columnas fiscales y snapshots necesarios ya existían;
- los FKs, índices y precisiones relevantes fueron reforzados previamente;
- los cambios M3 son de comportamiento de seed, identidad estable, snapshot y cobertura de pruebas.

El gate `has-pending-model-changes` debe permanecer en verde antes del cierre.

---

## 9. Evidencia focalizada previa al gate final

- `31330080768` — corrección de snapshot fiscal de Venta: **success**; compilación Release + pruebas M3 focalizadas; workflow temporal eliminado en el mismo commit.
- `31330264576` — protección de código fiscal estable: **success**; compilación Release + pruebas M3 focalizadas; workflow temporal eliminado en el mismo commit.

No se contabilizan como certificación final por sí solas. El cierre de M3 depende de los workflows oficiales sobre un HEAD normal de `Desarrollo`.

---

## 10. Gate final cumplido

El HEAD funcional certificado `9ea747acd110914d6445f687caabf4cf42a1fefe` demostró:

- Desarrollo - Compilación y pruebas: success;
- migraciones MySQL 8.4: success;
- integración MySQL, incluidos los tests fiscales M3: success;
- snapshot EF coherente: success;
- frontend lint/build producción: success;
- Desarrollo - aceptación funcional integral: success;
- Fase 2 - Auditoría de configuración y dependencias: success;
- Bloque 2C.1 - Variante técnica y migración: success;
- Fase 8 - Validación completa automatizada: success;
- ningún P0/P1 introducido por M3.

`Solqaryn CI` se registró como `SKIPPED` y no se contabiliza como workflow verde.

Evidencia oficial del HEAD funcional certificado `9ea747acd110914d6445f687caabf4cf42a1fefe`:

- `31330348378` — Desarrollo - Compilación y pruebas — **success**;
- `31330348374` — Desarrollo - aceptación funcional integral — **success**;
- `31330348396` — Fase 2 - Auditoría de configuración y dependencias — **success**;
- `31330348421` — Bloque 2C.1 - Variante técnica y migración — **success**;
- `31330348369` — Fase 8 - Validación completa automatizada — **success**;
- `31330348386` — Solqaryn CI — **skipped**, no contabilizado como verde.

Dentro de `31330348378` quedaron en success backend Release, pruebas no integración, frontend lint/build, Docker/higiene, migraciones MySQL 8.4, pruebas de integración MySQL (incluidos los escenarios fiscales M3), verificación de snapshot EF y SQL forward.

---

## 11. Validaciones externas separadas

No se atribuyen como ejecutadas:

- cambios directos sobre Producción;
- inspección manual de una instancia externa concreta de Aiven;
- cobro fiscal real ante una pasarela o ente externo.

M3 se certifica sobre código versionado, persistencia y CI descartable. Producción permanece congelada.

---

## 12. Dictamen final

**M3 — Configuración fiscal ISV/ISC: COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE.**

La siguiente fase del Plan Maestro será M4 — Estado persistente de filtros y navegación, únicamente después del cierre verde de M3.
