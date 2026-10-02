# ERP-N0.8 — Migraciones y limpieza

## Dictamen

**Estado técnico:** LISTO / CERRADO, sujeto únicamente a la reconciliación documental y de tablero de esta microtarea H.

**Rama:** `Desarrollo`  
**SHA funcional certificado:** `369158761ad05671b9a1859d17796c8ca4a09bf8`  
**Producción:** no tocada  
**`main`:** no modificada por ERP-N0.8  
**PR #2:** debe permanecer OPEN + DRAFT, sin merge ni auto-merge.

ERP-N0.8 consolida el saneamiento de ERP-N0 mediante preflight, persistencia transicional, backfill fail-closed, postchecks, reconciliación de autoridad, compatibilidad histórica controlada y rollback documentado. El punto no elimina físicamente una columna solo por ser legacy: una estructura puede permanecer cuando todavía cumple una función de snapshot, reversión histórica o compatibilidad, siempre que haya dejado de ser autoridad decisoria.

El preflight histórico queda en `docs/ERP_N0_8_MIGRACIONES_LIMPIEZA_PREFLIGHT.md`; este documento es la fuente canónica final del punto.

## 1. Principio de cierre

El criterio aplicado fue:

1. inventariar deuda física y consumidores reales;
2. distinguir autoridad de snapshot/bridge;
3. fallar cerrado ante datos no representables;
4. materializar primero la autoridad relacional;
5. backfillear por códigos/relaciones estables, nunca por supuestos sobre IDs;
6. preservar históricos y reversibilidad operacional;
7. retirar dependencias runtime de la autoridad legacy;
8. mantener compatibilidad transitoria únicamente donde su eliminación física todavía no sea segura;
9. certificar desde cero, upgrade histórico, integración MySQL, frontend y E2E antes del cierre.

No se ejecutó DDL/DML contra Producción.

## 2. Preflight N0.8.A

Scripts y evidencia principales:

- `backend/scripts/preflight-erp-n0-8-migraciones-limpieza.sql`
- `backend/tests/Solqaryn.Tests/N08MigracionesLimpiezaPreflightIntegrationTests.cs`

El preflight es de solo lectura e inventaría:

- tablas y columnas de compatibilidad;
- FKs e índices;
- historial EF;
- triggers y vistas;
- estructuras que N0.2/N0.4 deben mantener físicamente ausentes;
- autoridades relacionales ya existentes;
- deuda pendiente de Compra, Producto y orígenes documentales.

Hallazgos que condicionaron la implementación:

- `Compras.MetodoPago` seguía siendo la única representación persistida de pago de Compra;
- `MovimientosInventario` ya poseía físicamente `CompraId`, `VentaId`, `ConsumoInsumoId` y `AjusteInventarioId`, pero el modelo EF/runtime todavía accedía parcialmente mediante SQL explícito y snapshots legacy;
- `Productos.Cantidad/Costo` continúan interviniendo en snapshots y reversión segura de compras, por lo que un DROP físico sería inseguro;
- columnas RBAC legacy retiradas en N0.4 deben permanecer ausentes; `EsAdministrador` es metadato deliberado, no bypass;
- `ReferenciaTipo/ReferenciaId` y `ModuloOrigen/ReferenciaId` podían conservarse como snapshot/correlación, pero no como autoridad referencial.

Commits relevantes de A:

- `916447b9f9d6ee0fc732ccd688807563962ff9fe`
- `610ebbf9e0d4e65e1861bf5ff7917dd925a8c86d`
- `caea2b660585789fb6ed92faabe2a0509a058d96`
- `c7d39903eb978337d501a37c4d9c32b506c450f3`

## 3. Dominio N0.8.B

Commit principal:

`c20151391d696ebe1d172ae3341e579cc371c35f`

Se formalizaron los contratos que la persistencia debía materializar después:

- Compra puede expresar `MetodoPagoId` + catálogo relacional;
- MovimientoInventario expresa `CompraId`, `VentaId`, `ConsumoInsumoId` y `AjusteInventarioId`;
- `OrigenTipado` exige como máximo un origen documental tipado;
- el snapshot legacy no tiene precedencia sobre la FK tipada.

B se mantuvo sin DDL mediante contrato transitorio y pruebas dirigidas; C materializó la persistencia.

## 4. Persistencia, migración y backfill N0.8.C

### 4.1 Migración

Migración:

`20260814155400_N0_8_PersistenciaLimpiezaTransicional`

Commit funcional:

`b7b1db8746beac2a6e3f25c68afcafd8768383c8`

La migración:

- valida fail-closed que todo `Compras.MetodoPago` histórico sea representable;
- añade `Compras.MetodoPagoId` nullable para la transición;
- backfillea por `MetodosPago.Codigo` normalizado;
- **no** asume que el Id autoincremental del catálogo equivale al antiguo enum;
- ejecuta postguard de correspondencia 1:1;
- crea `IX_Compras_MetodoPagoId`;
- crea `FK_Compras_MetodosPago_MetodoPagoId` con `RESTRICT`;
- no elimina todavía `Compras.MetodoPago`.

### 4.2 MovimientoInventario

N0.8.C reconcilia el modelo EF con las columnas y FKs tipadas ya creadas por N0.6/N0.7, sin duplicar DDL:

- `CompraId`
- `VentaId`
- `ConsumoInsumoId`
- `AjusteInventarioId`

El snapshot EF se actualiza para reflejar la estructura física real y `has-pending-model-changes` queda sin drift.

### 4.3 Postcheck

Script:

`backend/scripts/postdeploy-erp-n0-8-c-persistencia.sql`

Verifica:

- columna, índice y FK de Compra→MetodoPago;
- backfill completo y consistente por código;
- cuatro columnas de origen tipado de inventario;
- exclusividad de origen;
- preservación de columnas legacy transitorias mientras aún sean necesarias.

### 4.4 Rollback de esquema

N0.8.C es **forward-only**. Su `Down` falla cerrado porque después de que nuevas operaciones utilicen un catálogo administrable pueden existir métodos no representables por el enum histórico.

Rollback soportado:

- restaurar un respaldo certificado compatible con el SHA destino; o
- aplicar una corrección forward que preserve datos.

No se debe usar un DROP improvisado de `MetodoPagoId` como rollback operacional.

## 5. Aplicación, servicios y API N0.8.D

Commits principales:

- `9adcde0dc8121319e7e98d50eccc74a5800e862e`
- `8c5da7986d412e9e45cfaa04a5bf9070b1a196cb`
- `23a1ad79b1e08c83ba9b232b2de3e5d3691c633f`
- `380ac794c576424569681a1ce81c0f07b70b9d03`
- `d18d7c4cab70643e8fc613a0015092eb5c744688`
- `633d8fc36e2b825a6362f418c01254c8886f37fe`

### 5.1 Compra y MetodoPago

Las nuevas escrituras de Compra:

- resuelven el método contra el catálogo activo;
- persisten `MetodoPagoId` como autoridad relacional;
- fallan cerrado ante método inexistente/inactivo;
- conservan `MetodoPago` enum únicamente como snapshot/bridge representable;
- para métodos administrables nuevos, la autoridad permanece en `MetodoPagoId` aunque el snapshot enum deba proyectarse a `Otro`;
- propagan `MetodoPagoId` a `MovimientoFinanciero`.

Las lecturas de Compra incluyen `MetodoPagoCatalogo` y el DTO expone el nombre relacional cuando está disponible.

### 5.2 Bridge transitorio de Compra legacy

Una fila legacy válida que llegue a `Confirmar` con `MetodoPagoId = NULL` puede reconciliarse **one-way y bajo el lock de la misma transacción**:

- se resuelve el enum/snapshot contra un método activo del catálogo;
- se asigna la FK;
- la transacción la persiste junto con la operación;
- si no existe una equivalencia válida, CompraService mantiene el rechazo fail-closed y no muta stock.

Este bridge no convierte el enum en autoridad preferente; solo permite converger una fila legacy representable hacia la autoridad relacional.

### 5.3 Origen tipado de MovimientoInventario

`MovimientoInventarioRepository` deja de utilizar raw SQL como vía normal para escribir/leer las FKs tipadas. EF persiste y consulta directamente:

- `CompraId`
- `VentaId`
- `ConsumoInsumoId`
- `AjusteInventarioId`

`ReferenciaTipo/ReferenciaId` continúan como snapshot de compatibilidad/correlación. El fallback que interpreta esos snapshots se conserva únicamente para providers no relacionales de pruebas/compatibilidad, no como autoridad del runtime MySQL.

## 6. Frontend y UX N0.8.E

Commits:

- `d263a10c4f08af3cb599145cffe9148f36009816`
- `4693502282f54e3adfeee97669e0ca7ffa10b3ae`

El formulario de Compras eliminó la lista hardcodeada `Efectivo / Transferencia / Tarjeta / Otro` y reutiliza el catálogo N0.5 existente mediante `MetodoPagoService.getActivos()`.

Contrato UX final:

- el usuario ve `MetodoPago.Nombre`;
- el formulario envía `MetodoPago.Codigo` estable;
- métodos administrables nuevos aparecen sin recompilar frontend;
- si el catálogo está cargando, vacío o falla, Guardar permanece bloqueado;
- existe acción de reintento;
- si un borrador referencia un método que dejó de estar activo, exige seleccionar uno vigente;
- la carrera entre carga del borrador y carga del catálogo se reconcilia sin convertir un valor transitorio en autoridad.

## 7. RBAC, auditoría, seguridad y observabilidad N0.8.F

**N/A verificado para introducir nueva superficie de seguridad.** N0.8 no añadió endpoints nuevos.

Se verificó que:

- Compras conserva permisos granulares `Ver/Crear/Editar/Confirmar/Anular/EliminarLogico`;
- mantenimiento de MétodosPago conserva RBAC relacional;
- `/metodos-pago/activos` permanece autenticado como lookup operativo read-only y no habilita mutaciones;
- el resolver de Compra solo acepta métodos activos/no eliminados;
- el bridge legacy no omite los controles de servicio;
- correlation middleware y auditoría existentes siguen aplicando a las mutaciones afectadas;
- no se introdujeron secretos ni bypasses.

## 8. QA, regresión y CI N0.8.G

SHA funcional final:

`369158761ad05671b9a1859d17796c8ca4a09bf8`

Regresión específica:

`frontend/e2e/n0-8-compras-metodos-pago-regresion.spec.ts`

Cubre:

- método administrable dinámico visible en Nueva Compra;
- ausencia de la lista hardcodeada antigua;
- selección desde catálogo activo;
- catálogo no disponible => UI fail-closed + reintento + Guardar deshabilitado.

### Evidencia final del mismo SHA

- **Desarrollo - Compilación y pruebas** `31821172124` — SUCCESS completo.
- **M10 - UI UX empresarial y accesibilidad** `31821172381` — SUCCESS completo, incluido lint/build y Playwright.
- **Fase 8 - Validación completa automatizada** `31821172230` — SUCCESS completo.
- **Desarrollo - aceptación funcional integral** `31821172223` — SUCCESS completo, incluido Playwright integral, SMTP y PDF.
- **M13 - Auditoría integral y certificación final** `31821172341` — SUCCESS completo:
  - frontend TypeScript/lint/build;
  - backend Release con warnings como error;
  - unitarias/contratos;
  - MySQL estricto;
  - historial completo desde cero;
  - integración sobre esquema actual;
  - SQL forward idempotente;
  - upgrade representativo y preservación histórica;
  - seguridad HTTP/autorización fail-closed;
  - Playwright integral;
  - SMTP/PDF/logs sin secretos;
  - Docker/aislamiento/backup;
  - **Dictamen automatizado M13 = SUCCESS**.

No quedan P0/P1 conocidos atribuibles a ERP-N0.8.

## 9. Estructuras deliberadamente preservadas

ERP-N0.8 no considera “deuda sin resolver” toda columna antigua que permanece físicamente. Se preservan únicamente cuando existe una justificación concreta y han dejado de ser autoridad primaria.

### 9.1 Producto

`Producto.Cantidad`, `Costo` y otras proyecciones familiares continúan porque:

- participan en snapshots/valorización/reversión de compras;
- los históricos necesitan una transición explícita antes de un DROP;
- `ProductoVariante` sigue siendo la autoridad operacional certificada.

Eliminar físicamente esas columnas sin sustituir primero la estrategia de valorización/reversión sería destructivo y queda expresamente prohibido.

### 9.2 Compra.MetodoPago

Se conserva como snapshot/bridge de compatibilidad. Para operaciones nuevas, la autoridad es `MetodoPagoId -> MetodosPago`.

### 9.3 MovimientoInventario.ReferenciaTipo/ReferenciaId

Se conservan como snapshot/correlación. Para MySQL/runtime, la autoridad documental son las FKs tipadas.

### 9.4 MovimientoFinanciero.ModuloOrigen/ReferenciaId

Se conservan como snapshot/correlación. `CompraId`, `VentaId` y `FacturaId` son la autoridad relacional cuando existe documento origen.

## 10. Backups y recuperación

Antes de cualquier futura eliminación física adicional:

1. debe existir backup lógico/restauración certificada compatible;
2. el preflight del consumidor debe demostrar cero uso operacional;
3. históricos y reversión deben estar cubiertos por otra fuente;
4. debe existir postcheck de preservación;
5. CI de migración/upgrades debe quedar verde;
6. Producción requiere autorización formal separada.

No se autoriza inferir que una columna puede borrarse porque su autoridad ya fue retirada.

## 11. Trazabilidad A-H

- **N0.8.A** — auditoría y preflight: LISTO.
- **N0.8.B** — dominio y contratos: LISTO.
- **N0.8.C** — persistencia, migración y datos: LISTO.
- **N0.8.D** — aplicación, servicios y API: LISTO.
- **N0.8.E** — frontend y UX: LISTO.
- **N0.8.F** — RBAC/auditoría/seguridad/observabilidad: LISTO / N.A. verificado para nueva superficie.
- **N0.8.G** — QA, regresión y CI: LISTO.
- **N0.8.H** — documentación y certificación: este cierre.

## 12. Gate ERP-N0

Con N0.8 cerrado, ERP-N0 completa el saneamiento rector de N0.1–N0.8:

- ProductoVariante permanece como autoridad operacional de la unidad inventariable;
- CatalogoProducto persistente legacy permanece retirado;
- RBAC relacional permanece como única autoridad de autorización;
- MetodoPago administrable es autoridad relacional de operaciones nuevas, incluida Compra;
- referencias documentales críticas de inventario están tipadas y encapsulan los snapshots legacy;
- AjusteInventario formal es la autoridad de ajustes de stock;
- migraciones, backfills, postchecks, upgrades y regresión integral están certificados.

El siguiente gate/fase solo puede iniciarse conforme al Plan Maestro y al tablero VAEP; este documento no autoriza merge a `main` ni despliegue a Producción.

## 13. Límites del cierre

ERP-N0.8 no autoriza:

- merge del PR #2;
- auto-merge;
- cambios en `main`;
- despliegue productivo;
- ejecución de migraciones contra Producción;
- modificación de secretos, credenciales, dominios o servicios productivos;
- force-push;
- creación de ramas adicionales;
- DROP físico posterior sin un nuevo preflight histórico verificable.

## 14. Dictamen final

**ERP-N0.8 queda funcionalmente certificado sobre `369158761ad05671b9a1859d17796c8ca4a09bf8`.**

El cierre formal de `N0.8.H` se completa al reconciliar este documento, `TASKS.md`, `CHANGELOG_AI.md`, VAEP/BITACORA y ejecutar el gate final de PR/main/Producción.
