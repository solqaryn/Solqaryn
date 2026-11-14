# ERP-N0.3 — Consolidación de ProductoVariante

## Estado

**APROBADO / CERRADO**

- Fecha de cierre local: **2026-08-10 (America/Tegucigalpa)**.
- Evidencia CI ejecutada en UTC: **2026-08-11**.
- SHA funcional certificado previo al documento de cierre: `653a2c37300fcbbbd680b2a0aca787c399a26a1a`.
- Rama trabajada: `Desarrollo`.
- `main`: no modificada.
- Producción: no tocada.
- Merge / auto-merge: no autorizados.

## 1. Objetivo

Consolidar `ProductoVariante` como **única fuente de verdad operativa** para los datos que identifican, valorizan y controlan una unidad exacta inventariable/vendible:

- SKU;
- código de barras;
- costo;
- precio;
- existencia;
- Marca / Modelo / Color / Talla de la variante;
- imágenes específicas de variante;
- umbral de stock.

La fase también debía endurecer integridad e impedir que combinaciones inválidas o referencias inconsistentes pudieran persistirse aun si una capa de aplicación fallara.

## 2. Alcance y no alcance

### Incluido

- lecturas operativas desde `ProductoVariante`;
- escrituras operativas hacia `ProductoVariante`;
- compatibilidad controlada para productos simples mediante variante técnica;
- normalización de SKU y barcode;
- backfill de variantes técnicas faltantes;
- constraints de dominio e integridad referencial;
- relación compuesta Modelo–Marca;
- integridad ProductoImagen–ProductoVariante–Producto;
- filtros, búsquedas y agregados de inventario desde variantes;
- compras, ventas, escáner, ajustes/concurrencia y cargas masivas en los puntos afectados;
- preflight, postdeploy, guardas estáticas, migración, pruebas y gate CI dedicado.

### Fuera de alcance

- eliminar físicamente en esta fase las columnas legacy todavía existentes en `Producto`;
- inventario multialmacén/multisucursal, que pertenece a ERP-N1;
- lotes, series o vencimientos;
- cierre de RBAC legacy, que corresponde a ERP-N0.4;
- cambios en `main` o despliegue productivo.

## 3. Regla de autoridad después de N0.3

### `Producto`

Continúa representando la **familia comercial** y sus datos comunes: nombre, descripción, categoría, tipo/estado y metadatos compartidos.

Por compatibilidad histórica todavía existen campos como `Cantidad`, `Costo`, `Precio`, `UmbralStockBajo`, `MarcaId`, `ModeloId`, `ColorId`, `TallaId`, `Marca` y `Modelo` en la entidad/tabla `Producto`. A partir de N0.3 estos valores **no son autoridad operativa**.

Cuando sea necesario conservarlos para contratos o regresión histórica, se mantienen únicamente como **proyección derivada de `ProductoVariante`**. No deben utilizarse para decidir stock, costo, precio o dimensiones de una operación nueva.

### `ProductoVariante`

Queda como autoridad operativa para:

| Dato | Fuente de verdad |
|---|---|
| SKU | `ProductoVariante.Sku` |
| Barcode | `ProductoVariante.CodigoBarras` |
| Costo | `ProductoVariante.Costo` |
| Precio | `ProductoVariante.Precio` |
| Existencia | `ProductoVariante.Cantidad` |
| Umbral | `ProductoVariante.UmbralStockBajo` |
| Marca | `ProductoVariante.MarcaId` |
| Modelo | `ProductoVariante.ModeloId` |
| Color | `ProductoVariante.ColorId` |
| Talla | `ProductoVariante.TallaId` |
| Imagen específica | `ProductoImagen.ProductoVarianteId` con integridad al mismo `ProductoId` |

## 4. Variante técnica

La variante técnica queda formalizada como la unidad exacta administrada por el sistema para un producto simple que no necesita una matriz de variantes comerciales.

Reglas relevantes:

- puede portar Marca, Modelo, Color y Talla normalizados cuando esos atributos describen al producto simple;
- utiliza SKU técnico interno;
- no acepta barcode comercial manual;
- conserva stock, costo, precio y umbral como cualquier otra variante;
- clientes antiguos que no envían `ProductoVarianteId` pueden resolverse a la variante técnica cuando existe una única unidad simple válida;
- si existen variantes comerciales, la operación debe identificar la variante exacta;
- una técnica existente no vuelve a copiar valores desde `Producto`; solo una creación inicial de compatibilidad pre-N0.3 puede sembrarse una vez antes de quedar bajo autoridad de variante.

## 5. Cambios de runtime

### Productos / mapper

`ProductoMapper` deriva inventario, costo, precio, umbral y dimensiones operativas desde variantes. Se mantiene un fallback muy acotado solo para una variante técnica histórica todavía no backfilleada durante la transición.

### Repositorio de productos

Filtros, búsquedas, ordenamientos y agregados relacionados con stock/economía se calculan desde `ProductoVariantes`, evitando que `Producto.Cantidad`, `Producto.Costo` o `Producto.Precio` funcionen como autoridad.

### Servicio de variantes

`ProductoVarianteService` concentra la sincronización de variantes técnicas y comerciales y mantiene, cuando hace falta, el espejo de compatibilidad de `Producto` como una proyección derivada.

### ProductosController

Se retiró la escritura denominada `AplicarProyeccionLegacy(dto)`. Los campos compatibles de clientes antiguos se traducen a una variante exacta; no se utilizan para establecer una segunda autoridad en `Producto`.

### Compras y ventas

- producto simple: puede resolver su variante técnica cuando un contrato anterior omite `ProductoVarianteId`;
- producto con variantes comerciales: exige variante exacta;
- snapshots históricos se preservan;
- la operación moderna utiliza la variante exacta como fuente de costo/precio/existencia.

### Escáner

Se eliminaron fallbacks operativos de costo/precio hacia `Producto`.

### Inventario / concurrencia

Las demandas modernas de inventario trabajan con variante exacta. El uso de `Producto.Cantidad` queda limitado a compatibilidad histórica/pre-backfill cuando una demanda antigua carece de variante identificable.

### Cargas masivas

Las variantes son la autoridad. Los valores que todavía deban reflejarse en `Producto` se recalculan como proyección derivada y no como escritura independiente.

## 6. Integridad y constraints

La unicidad existente de SKU y barcode fue preservada y endurecida mediante normalización/preflight. N0.3 agregó constraints adicionales para impedir estados inválidos en base de datos.

### CHECK constraints

- `CK_ProductoVariantes_N03_Sku`: SKU obligatorio/no vacío.
- `CK_ProductoVariantes_N03_Barcode`: barcode `NULL` o no vacío.
- `CK_ProductoVariantes_N03_Stock`: cantidad y umbral no negativos.
- `CK_ProductoVariantes_N03_Importes`: costo/precio, cuando existan, no negativos.
- `CK_ProductoVariantes_N03_ModeloMarca`: un Modelo requiere Marca.
- `CK_ProductoVariantes_N03_TecnicaBarcode`: una variante técnica no porta barcode comercial.

### Integridad Modelo–Marca

Se añadió una clave/indexación compatible con la relación compuesta y el FK:

`(ProductoVariante.ModeloId, ProductoVariante.MarcaId) -> Modelo(Id, MarcaId)`

Con esto no basta con que ambos IDs existan: el Modelo seleccionado debe pertenecer realmente a la Marca seleccionada.

### Integridad de imágenes específicas

Se añadió integridad compuesta:

`(ProductoImagen.ProductoVarianteId, ProductoImagen.ProductoId) -> ProductoVariante(Id, ProductoId)`

Una imagen específica no puede apuntar a una variante perteneciente a otro producto.

### Combinación de atributos

Se conserva el mecanismo de identidad activa de variante que impide combinaciones activas duplicadas, complementado por las nuevas reglas de Modelo–Marca y coexistencia técnica/comercial verificadas por preflight/runtime.

## 7. Migración y datos

Migración:

`20260811032000_N0_3_ConsolidarProductoVariante`

### Preflight fail-closed

Antes de modificar estructura se comprueba:

- cantidades/umbrales/importes inválidos;
- Modelo sin Marca;
- Modelo perteneciente a Marca distinta;
- imágenes de variante asociadas a Producto incorrecto;
- SKU duplicado después de normalizar;
- barcode duplicado después de normalizar;
- coexistencia indebida de variante técnica y comercial viva;
- inconsistencias legacy Marca–Modelo que impedirían backfill seguro;
- colisiones de SKU técnico a generar.

Script:

`backend/scripts/preflight-erp-n0-3-producto-variante.sql`

### Backfill / normalización

- SKU: `TRIM` + mayúsculas;
- barcode: `TRIM`; vacío pasa a `NULL`;
- se crean variantes técnicas para productos históricos que carecen de variante viva;
- la técnica recibe dimensiones/economía legacy únicamente como migración de transición cuando le faltan;
- después del backfill la variante queda como autoridad.

### Postdeploy

Script:

`backend/scripts/postdeploy-erp-n0-3-producto-variante.sql`

Verifica:

- todos los productos vivos tienen variante viva;
- no existen SKU/barcode inválidos o duplicados;
- no existen valores negativos inválidos;
- Modelo y Marca son coherentes;
- imágenes específicas pertenecen al mismo producto;
- los seis CHECK de N0.3 existen;
- los dos FKs compuestos críticos existen.

El postcheck fue además corregido para que el conteo de constraints sea determinista y no produzca falsos negativos cuando el conjunto correcto existe.

## 8. Rollback

La migración es **forward-only**. `Down()` no intenta reconstruir de forma automática una autoridad legacy, porque hacerlo podría reintroducir datos ambiguos o perder trazabilidad.

Rollback operativo:

1. detener aplicación del cambio;
2. utilizar respaldo/preflight anterior a N0.3;
3. restaurar DB y versión de aplicación compatibles;
4. verificar reconciliación antes de reabrir operaciones.

No se autoriza una reversión destructiva improvisada.

## 9. QA y regresiones encontradas durante el cierre

Durante la construcción se detectó que un archivo de pruebas existente había sido sobrescrito por un transformador temporal, reduciendo accidentalmente el conteo de pruebas de 272 a 270.

La regresión de QA fue corregida antes del cierre:

- se restauraron las tres pruebas existentes;
- se conservó la nueva prueba de autoridad de `ProductoVariante`;
- el total final quedó en **273/273 pruebas backend aprobadas**.

Los scripts/workflows temporales usados para aplicar el cambio fueron retirados antes de la certificación final; solo quedan código definitivo, migración, pruebas, scripts de auditoría y gate permanente.

## 10. Evidencia de certificación del SHA funcional

SHA funcional certificado:

`653a2c37300fcbbbd680b2a0aca787c399a26a1a`

### Gate ERP-N0.3

- Workflow: `ERP-N0.3 - Certificación ProductoVariante autoridad única`.
- Run: **31458013180**.
- Resultado: **SUCCESS**.
- Build: **0 warnings / 0 errors**.
- Backend: **273 passed / 0 failed / 0 skipped**.
- MySQL: **8.4**.
- Preflight: **0 bloqueos**.
- Migración N0.3: aplicada correctamente.
- Postcheck: **0 errores**.
- EF: sin cambios de modelo pendientes.
- Escritura deliberada con SKU vacío: rechazada por constraint.
- Escritura deliberada con Modelo sin Marca: rechazada por constraint.

### Regresión transversal

Sobre el mismo SHA:

- `Desarrollo - Compilación y pruebas`: run **31458013226** — **SUCCESS**.
- `Fase 8 - Validación completa automatizada`: run **31458013170** — **SUCCESS**.
- `Desarrollo - aceptación funcional integral`: run **31458013177** — **SUCCESS**.
- `ERP-N0.2 - Certificación CatalogoProducto legacy`: run **31458013158** — **SUCCESS**.
- `M13 - Auditoría integral y certificación final`: run **31458013174** — **SUCCESS**.
- M13 Playwright integral: **107/107 passed**, 0 fallos.
- Runtime M13: aprobado.

Además permanecieron verdes los gates M9, M10, M11, M12, recuperación de migraciones y controles auxiliares ejecutados por el SHA.

## 11. Seguridad y estado del repositorio

- P0 abiertos atribuibles a N0.3: **0**.
- P1 abiertos atribuibles a N0.3: **0**.
- `main`: no modificada.
- Producción: no tocada.
- PR #2: debe permanecer abierto, Draft, sin merge y sin auto-merge.
- N0.3 no autoriza despliegue ni liberación productiva.

## 12. Definition of Done N0.3

| Criterio | Estado |
|---|---|
| SKU bajo autoridad de variante | ✅ |
| Barcode bajo autoridad de variante | ✅ |
| Costo bajo autoridad de variante | ✅ |
| Precio bajo autoridad de variante | ✅ |
| Existencia bajo autoridad de variante | ✅ |
| Marca/Modelo/Color/Talla bajo autoridad de variante | ✅ |
| Imágenes específicas con integridad variante/producto | ✅ |
| Umbral bajo autoridad de variante | ✅ |
| SKU único / normalizado | ✅ |
| Barcode único cuando existe | ✅ |
| Modelo pertenece a Marca | ✅ |
| Combinaciones activas protegidas | ✅ |
| Integridad referencial endurecida | ✅ |
| Backfill/preflight/postcheck | ✅ |
| Build backend | ✅ |
| 273/273 backend tests | ✅ |
| 107/107 Playwright M13 | ✅ |
| P0/P1 abiertos | 0 |
| Producción tocada | No |

## 13. Dictamen

**ERP-N0.3 queda APROBADO y CERRADO.**

`ProductoVariante` es la única autoridad operativa de la unidad exacta inventariable/vendible. Los campos duplicados que todavía existen físicamente en `Producto` quedan restringidos a compatibilidad/proyección derivada hasta su retiro físico seguro dentro del cierre global de legacy; no vuelven a constituir una segunda fuente de verdad.

El siguiente punto oficial del Plan Maestro ERP V5 es:

**ERP-N0.4 — Cierre de RBAC legacy**

Este documento no autoriza iniciar producción, fusionar PR #2 ni modificar `main`.
