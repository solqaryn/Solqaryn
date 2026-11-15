# SOLQARYN V16 — C07-03.M02.03: persistencia, índices, migración reversible y fixtures de inventario/compras

**ID:** C07-03.M02.03 (PLAN_MAESTRO fila 1090). **Autor:** PRIMARY DEV 02 JAVIER, `6af16bb9492c8191b8e8c7d606f11507`. **Riesgo:** R0, especificación documental no ejecutable. **Rama:** `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT.

**Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN/COLA/DAG. Resultado: **definir** persistencia, índices, migraciones futuras y datos de prueba, preservando almacenes, compras, costeo, reservas y conciliación. **No se crea ni ejecuta migración, SQL, DbSet, índice, transacción, servicio, endpoint o modificación de existencias.** M02 es contrato; cualquier implementación EF/MySQL será una microtarea R2 posterior con CI real.

## 1. Estado base, autoridad y dependencias

- Preflight: PLAN H1090=PENDIENTE, I1090 vacío; COLA G1090/O1090=SI; DAG F1051=VALIDADO/G1051=SI. Claim V16 exclusivo por Task DEV02 en EN_PROGRESO con readback y evento BITACORA.
- `C07-03.M01.06` contiene BL01–BL16 de bloqueo, RB01–RB10 de reversión y SAL01–SAL12 de salida; `M01.04` conserva D01–D08 pendientes y supuestos sin certificar. `M02.01` define E01–E12, INV01–INV16, REL01–REL10 y T01–T12 **futuros**. `M02.02` define OP01–OP12, DTO01–12, ERR01–12 y VAL01–16 sin endpoints nuevos.
- Código inspeccionado: `backend/src/Infrastructure/Persistence/AppDbContext.cs` declara `DbSet` de `ProductoVariante`, `MovimientoInventario`, `ExistenciaVariante`, `LoteInventario`, `SerieInventario`, `ReservaInventario`, `ReservaInventarioDetalle`, `Compra`, `CompraDetalle` y `AjusteInventario`. `backend/src/Domain/Entities/Almacen.cs` obtiene tenant mediante `Sucursal.ObtenerEmpresaIdTenant()` y falla si no está cargada. La carpeta de migraciones EF está en `backend/src/Infrastructure/Persistence/Migrations/`. Esta inspección **no certifica** constraints/indexes existentes ni migración SQL histórica.
- Domain <- Application <- Infrastructure, API como composición; EF Core 10.0.12, proveedor Oracle `MySql.EntityFrameworkCore` 10.0.9, MySQL 8.4 según arquitectura. No inventar una tabla multitenant paralela ni agregar `EmpresaId` al almacén sin estudio: su jerarquía actual pasa por Sucursal.
- **Bloqueos:** `NULL` heredado no concede propiedad global; costo histórico no se recalcula por defecto; transferencias, lotes/series, reservas y costeo requieren transacciones reales. Ninguna especificación presume que estos controles ya están implementados.

## 2. Contrato conceptual de persistencia (P01–P12)

| ID | Agregado/ámbito propuesto | Llave, cardinalidad y regla a verificar antes de EF |
| --- | --- | --- |
| P01 | Sucursal/Almacén | FK almacén->sucursal válida; Empresa propietaria derivada desde sucursal autenticada; `NULL` falla cerrado |
| P02 | Ubicación/Almacén | ubicación nunca visible fuera del almacén autorizado; no asumir tabla existente sin inspección |
| P03 | Variante/Existencia | unicidad lógica por variante, ubicación/almacén y alcance efectivo según esquema real |
| P04 | Reserva/Detalle | reserva con versión y estado; saldo reservado no puede duplicar disponibilidad |
| P05 | Movimiento/Kardex | operación, referencia de origen y correlación estables; histórico no borrado en cascada |
| P06 | Orden de compra/Detalle | proveedor y sucursal del mismo tenant; línea cantidad/costo con precisión explícita |
| P07 | Recepción/Detalle | relación a orden, saldo pendiente y clave de replay; no duplicar recepción |
| P08 | Transferencia/Detalle | origen/destino distintos y autorizados antes de comprometer movimientos |
| P09 | Conteo/Ajuste borrador | conteo no confirma ajuste sin revisión RBAC y versión |
| P10 | Lote/Serie | lote o serie única bajo el ámbito que defina dominio real, sin mezclar tenants |
| P11 | Costeo/Histórico | snapshot inmutable de método, moneda, redondeo y origen; cambios requieren autorización |
| P12 | Conciliación | vista/proyección por tenant; costos/PII ocultos sin autorización y sin registrar secretos |

**Nota de esquema:** P01–P12 son responsabilidades, NO nombres de tablas ni alteraciones propuestas automáticas. Antes de escribir migración, inspeccionar modelos EF, `OnModelCreating`, migrations snapshot y SQL generado; registrar discrepancias actuales de PK/FK/unique/soft-delete, comparar ambientes únicamente de desarrollo autorizado.

## 3. Índices y constraints candidatos (IDX01–IDX12, sin DDL)

| ID | Consulta o constraint que debe estudiarse | Propósito / condición adversarial |
| --- | --- | --- |
| IDX01 | Sucursal(empresa,estado,identificador) | localizar jerarquía; el `EmpresaId` real debe validarse |
| IDX02 | Almacén(sucursal,código,activo,eliminado) | evitar duplicidad de código en sucursal activa; soft-delete explícito |
| IDX03 | Ubicación(almacén,código) | no resolver ubicación tenant B |
| IDX04 | Existencia(variante,almacén,ubicación) | unicidad funcional; evitar doble saldo por carrera |
| IDX05 | Reserva(almacén,estado,expiración) | disponibilidad y liberación idempotente |
| IDX06 | ReservaDetalle(reserva,variante) | detalle de reserva sin duplicación |
| IDX07 | OrdenCompra(proveedor,estado,fecha) | consultas acotadas por sucursal/tenant |
| IDX08 | Recepción(orden,clave-idempotencia) | replay sin nuevo kardex, nulidad revisada |
| IDX09 | Movimiento(almacén,fecha,identificador) | paginación estable y auditoría |
| IDX10 | Transferencia(origen,destino,estado) | reconciliar ambos extremos autorizados |
| IDX11 | Lote/Serie(variante,identificador) | unicidad conforme a reglas de ámbito reales |
| IDX12 | Reconciliación(empresa,corte,correlación) | reporte/exportación nunca atraviesa empresa |

No asumir que existan esas columnas o índices. Para MySQL 8.4, estudiar selectividad, tamaño de clave, collation, índice compuesto/orden, locks, costo de escritura y semántica de `UNIQUE` con NULL; índices redundantes deben descartarse después de `EXPLAIN` sobre fixtures no productivos. **No crear constraints que hagan fallar datos legados antes de una conciliación auditada.**

## 4. Plan de migración futura, transaccional y reversible (MIG01–MIG08)

| Paso | Gate de ejecución R2 que deberá acreditarse |
| --- | --- |
| MIG01 | Baseline: snapshot de EF, tablas, FKs, indexes y filas legacy no propietarias; backup verificado aislado |
| MIG02 | Perfil de inconsistencias: `NULL`, duplicados, stock negativo, reserva doble, costeo histórico sin origen |
| MIG03 | Diseño aditivo: mapping, columnas/versionado e índices sólo donde el esquema lo necesite, sin eliminar histórico |
| MIG04 | Estrategia de datos heredados con cuarentena: ninguna asignación cross-tenant por inferencia |
| MIG05 | Transacciones de reserva/recepción/transferencia con lock optimista/pesimista probado en MySQL 8.4 |
| MIG06 | Pruebas de rollback y replay: falla a mitad de escritura revierte saldo, detalle, kardex y costo |
| MIG07 | Ensayo en MySQL 8.4 efímero, ejecutar realmente `Category=Integration`, migrar arriba/abajo y demostrar 0 FAIL |
| MIG08 | Reconciliación antes/después por empresa, redacción de costos, monitoreo, plan de `git revert` normal; aprobación separada |

No afirmar que el rollback de una migración destructiva sea automático: la reversión de datos y las decisiones D01–D08 requieren plan probado y nueva autorización. Este documento **no efectúa** MIG01–MIG08.

## 5. Fixtures futuros y oráculos positivos/negativos F01–F12 (NO_EJECUTADOS)

| ID | Positivo verificable futuro | Negativo / frontera que debe fallar cerrado |
| --- | --- | --- |
| F01 | almacén empresa A y sucursal A visibles a rol correcto | token B no lee ni muta A; legado NULL no habilita |
| F02 | variante A en almacén A cuenta una sola vez | duplicado de llave o ubicación B no agrega existencia |
| F03 | reserva con cantidad disponible actualiza ambos saldos | dos reservas en carrera no sobrerreservan |
| F04 | expiración libera una reserva exactamente una vez | doble liberación/replay no duplica stock |
| F05 | orden válida asocia proveedor y sucursal A | proveedor de B o rol revocado da cero escritura |
| F06 | recepción parcial reduce pendiente correctamente | recepción repetida no duplica kardex |
| F07 | transferencia A entre almacenes distintos conserva suma | origen=destino o destino B se rechaza |
| F08 | conteo autorizado deja borrador sin ajustar | confirmación no autorizada no altera stock |
| F09 | lote/serie válida bajo ámbito correcto | serial duplicado/trans-tenant falla |
| F10 | costo histórico estable con política decimal aprobada | overflow/redondeo corrupto no se confirma |
| F11 | informe de conciliación A y rol Finanzas ve su costo | rol sin Finanzas y tenant B ven contenido censurado |
| F12 | commit de movimientos y auditoría con correlación | fallo después de débito antes de crédito revierte todo |

**Evidencia actual:** 12 pares especificados, 0 ejecutados, resultado runtime `NO_MEDIDO` (no se expresan como PASS). Al tocar EF, los tests mock/in-memory **no sustituyen** MySQL 8.4 efímero ni evidencia de la transacción. Registrar denominador, proveedor, SHA, filtro y logs para futura ejecución.

## 6. A0–A5 y recuperación

- **A0:** documento y sección aditiva `CHANGELOG_AI.md`; ID, DAG, fuente comercial, jerarquía, archivos afectados, consumidores, P01–P12, IDX01–IDX12 y MIG01–MIG08 contrastados; rollback documentado.
- **A1:** revisión de doce fixtures F01–F12 con caso positivo/negativo/fallo/fuera de tenant **propuestos**, sin asegurar que se ejecutaron.
- **A2:** aislamiento A/B, RBAC, revocación, idempotencia, costo y auditoría declarados; ningún cambio de runtime.
- **A3:** cotejo de AppDbContext/Almacen y precedentes M01/M02; ejecución .NET/MySQL/Angular/HTTP/Playwright **NO_APLICA para dos Markdown**, pruebas de integración futuras R2 son obligatorias al implementar.
- **A4:** commit único fast-forward/CAS sobre HEAD dev, Scope Lock/V5 `completed/success` SHA exacto; registrar `VALIDANDO` antes de cierre A0–A4, evento BITACORA y readback PLAN/COLA/DAG. `CANCELLED/SKIPPED/IN_PROGRESS` nunca son PASS obligatorio.
- **A5:** revisión real por una Task Javier distinta de DEV02, puede rechazar; `A5_PENDIENTE` no libera M02.06/M03 ni QA/main/PROD.

**Rollback real de este cambio R0:** `git revert` normal del commit (con HEAD revalidado), conservar eventos de BITACORA, sin force-push, sin cambios a datos/productores. BL01–BL16, D01–D08 y necesidad de análisis de índices / seguridad permanecen.
