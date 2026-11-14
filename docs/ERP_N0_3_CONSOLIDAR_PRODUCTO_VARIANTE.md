# ERP-N0.3 — Cierre formal y certificación de ProductoVariante

## 0. Estado y trazabilidad

**Estado del expediente:** CIERRE FORMAL DE ERP-N0.3.

- Rama de trabajo: `Desarrollo`.
- SHA funcional certificado: `07432601465189f1d6f4e5b303a7421b7be9f9f6`.
- PR oficial: `#2 — Desarrollo -> main`.
- PR #2: **OPEN + DRAFT** al momento de preparar este cierre.
- `main`: no modificada por ERP-N0.3.
- Producción: no tocada por ERP-N0.3 ni por este cierre documental.
- Despliegue productivo: no autorizado ni ejecutado.
- Aiven Producción: no utilizada para ejecutar la migración N0.3.

Este documento registra el estado comprobado de ERP-N0.3. No reabre la arquitectura ya certificada y no autoriza iniciar ERP-N0.4, fusionar el PR #2 ni desplegar a Producción.

---

## 1. Objetivo de ERP-N0.3

ERP-N0.3 consolida `ProductoVariante` como la **autoridad operativa única de la unidad exacta inventariable, comprable y vendible** dentro del alcance aprobado de saneamiento legacy.

La fase endurece especialmente:

- dimensiones de variante: Marca, Modelo, Color y Talla;
- SKU;
- código de barras;
- stock;
- costo;
- precio;
- umbral de stock bajo;
- integridad Producto–Variante;
- coherencia Marca–Modelo;
- imágenes específicas de variante;
- compras;
- ventas;
- inventario;
- importaciones;
- escáner;
- búsquedas, filtros y agregados relacionados con inventario/economía;
- migración, backfill, preflight, postcheck y guardas de regresión.

ERP-N0.3 no convierte a `Producto` en una segunda autoridad y tampoco exige borrar físicamente en esta fase todos los campos legacy que todavía sean necesarios para compatibilidad histórica.

---

## 2. Autoridad operativa después de N0.3

### 2.1 `ProductoVariante`

`ProductoVariante` es la fuente de verdad operativa para los datos de la unidad exacta:

| Dato | Autoridad |
|---|---|
| SKU | `ProductoVariante.Sku` |
| Código de barras | `ProductoVariante.CodigoBarras` |
| Stock | `ProductoVariante.Cantidad` |
| Costo | `ProductoVariante.Costo` |
| Precio | `ProductoVariante.Precio` |
| Umbral de stock | `ProductoVariante.UmbralStockBajo` |
| Marca | `ProductoVariante.MarcaId` |
| Modelo | `ProductoVariante.ModeloId` |
| Color | `ProductoVariante.ColorId` |
| Talla | `ProductoVariante.TallaId` |
| Imagen específica | `ProductoImagen.ProductoVarianteId`, íntegro respecto del mismo `ProductoId` |

La autorización funcional certificada impide que operaciones nuevas tomen decisiones de stock, costo, precio o dimensiones desde campos equivalentes de `Producto`.

### 2.2 Papel de `Producto`

`Producto` conserva la familia comercial y metadatos comunes. Los campos duplicados que todavía existan físicamente en la entidad/tabla por compatibilidad histórica son **proyecciones derivadas o contratos legacy**, no autoridad operacional.

En consecuencia:

- no gobiernan disponibilidad de stock;
- no gobiernan costo de una operación nueva;
- no gobiernan precio de la variante exacta;
- no gobiernan Marca/Modelo/Color/Talla de una operación nueva;
- no sustituyen `ProductoVarianteId` cuando la operación requiere una variante exacta.

Cuando una proyección legacy se mantiene, debe derivarse desde la autoridad de variantes y nunca competir con ella.

---

## 3. Variante técnica

Para productos simples, N0.3 formaliza la **variante técnica** como la unidad exacta que permite mantener un único modelo operativo basado en `ProductoVariante` aun cuando el producto no necesite una matriz comercial de variantes.

Reglas certificadas:

- porta stock, costo, precio y umbral igual que cualquier variante;
- puede portar Marca, Modelo, Color y Talla normalizados cuando describen al producto simple;
- utiliza un SKU técnico interno;
- no utiliza un barcode comercial manual;
- puede ser resuelta por compatibilidad cuando un contrato histórico omite `ProductoVarianteId` y existe una única unidad simple válida;
- si existen variantes comerciales, la operación debe identificar la variante exacta;
- una vez consolidada la autoridad de variantes, no se permite volver a utilizar `Producto` como fuente operacional alternativa.

---

## 4. Problemas reales encontrados durante la auditoría de cierre

La revisión de N0.3 no partió de la premisa de que la fase estuviera defectuosa. Se inspeccionó el código y la evidencia disponible y se corrigieron únicamente brechas demostradas.

### 4.1 Ventas: dependencia operacional de `Producto`

Se detectó que `VentaService` todavía podía:

- consultar `Producto.Cantidad` antes de resolver la variante exacta;
- utilizar un fallback equivalente a `variante?.Costo ?? producto.Costo`;
- permitir un detalle con `ProductoVarianteId` no resuelto en determinados caminos de compatibilidad.

Esto permitía que el espejo legacy de `Producto` influyera en una operación moderna.

**Corrección aplicada:** ventas quedó **fail-closed**: la variante operativa debe resolverse, el stock se valida sobre `ProductoVariante`, el costo se toma de la variante y el detalle conserva el `ProductoVarianteId` exacto.

### 4.2 Compras: variante no resuelta

Se detectó un camino en el que una compra podía continuar sin una variante operacional válida.

**Corrección aplicada:** compras quedó **fail-closed** y el detalle nuevo debe registrar la variante exacta resuelta. Los snapshots históricos se mantienen, pero la autoridad operacional de la entrada de inventario es `ProductoVariante`.

### 4.3 Carga masiva de Productos: reintroducción de autoridad legacy

La importación de tipo `Productos` todavía podía escribir directamente desde el input operativo valores equivalentes de Marca/Modelo/Talla/costo/precio/umbral sobre `Producto`.

**Corrección aplicada:** la plantilla de Productos conserva compatibilidad de contrato, pero los valores operativos se aplican a la **variante técnica**. Cualquier valor reflejado posteriormente en `Producto` queda como proyección derivada y no como segunda fuente de verdad.

### 4.4 Transición variante técnica → variante comercial

La regresión E2E de M9 demostró un caso real: importar primero un producto simple podía crear su variante técnica y, al importar después una variante comercial, ambas podían quedar activas simultáneamente.

**Corrección aplicada:** la transición técnica → comercial quedó controlada y transaccional.

- Si la variante técnica tiene **stock 0**, puede retirarse de forma segura al materializar la variante comercial.
- Si la variante técnica **conserva stock**, la transición se **bloquea fail-closed**. No se permite ocultar, perder ni reasignar inventario de manera implícita.
- La variante comercial no coexiste silenciosamente con una técnica viva en un estado ambiguo.

### 4.5 Aislamiento del workflow N0.3

Durante la auditoría se comprobó que una versión previa del gate utilizaba un `database update` que podía avanzar hasta migraciones posteriores y, por tanto, contaminar la prueba aislada de N0.3 con N0.4.

**Corrección aplicada:** el workflow permanente fue endurecido para:

1. crear el esquema exactamente hasta N0.2;
2. ejecutar preflight N0.3;
3. aplicar exactamente `20260811032000_N0_3_ConsolidarProductoVariante`;
4. certificar que N0.3 está presente;
5. certificar que N0.4 está ausente;
6. ejecutar postcheck;
7. verificar que el snapshot EF no tenga cambios pendientes.

Esta corrección es de certificación/aislamiento y no reimplementa funcionalidad de N0.3.

---

## 5. Migración N0.3

Migración certificada:

`20260811032000_N0_3_ConsolidarProductoVariante`

La migración es **forward-only**.

### 5.1 Normalización y backfill

La migración:

- normaliza SKU mediante `TRIM` + mayúsculas;
- normaliza barcode mediante `TRIM` y convierte vacío en `NULL`;
- crea una variante técnica para productos legacy vivos que carecen de variante viva;
- utiliza los datos legacy únicamente como fuente de **migración inicial de transición** para poblar la nueva autoridad cuando corresponde;
- deja a `ProductoVariante` como autoridad después del backfill.

### 5.2 Constraints agregados

N0.3 incorpora controles de base de datos para impedir estados inválidos:

- `CK_ProductoVariantes_N03_Sku`;
- `CK_ProductoVariantes_N03_Barcode`;
- `CK_ProductoVariantes_N03_Stock`;
- `CK_ProductoVariantes_N03_Importes`;
- `CK_ProductoVariantes_N03_ModeloMarca`;
- `CK_ProductoVariantes_N03_TecnicaBarcode`.

### 5.3 Integridad Marca–Modelo

Se endurece la relación para que no baste con que `MarcaId` y `ModeloId` existan de forma independiente. El Modelo debe pertenecer realmente a la Marca indicada:

`(ProductoVariante.ModeloId, ProductoVariante.MarcaId) -> Modelo(Id, MarcaId)`

### 5.4 Integridad de imágenes de variante

Una imagen específica de variante debe pertenecer al mismo producto:

`(ProductoImagen.ProductoVarianteId, ProductoImagen.ProductoId) -> ProductoVariante(Id, ProductoId)`

---

## 6. Preflight N0.3

Script:

`backend/scripts/preflight-erp-n0-3-producto-variante.sql`

El preflight es fail-closed y comprueba, entre otras condiciones:

- cantidades, umbrales, costos o precios negativos/inválidos;
- Modelo sin Marca;
- Modelo perteneciente a otra Marca;
- imágenes de variante asociadas al Producto incorrecto;
- colisiones de SKU después de normalizar;
- colisiones de barcode después de normalizar;
- coexistencia indebida de variante técnica y comercial viva;
- inconsistencias Marca–Modelo legacy que impedirían un backfill seguro;
- colisiones del SKU técnico que deba generarse.

La migración replica defensas críticas antes de modificar estructura para impedir avanzar sobre datos no reconciliables.

---

## 7. Postdeploy / postcheck N0.3

Script:

`backend/scripts/postdeploy-erp-n0-3-producto-variante.sql`

El postcheck certifica que la estructura y los datos posteriores a N0.3 cumplen las invariantes requeridas, incluyendo:

- productos vivos con variante viva;
- SKU válido y sin duplicados incompatibles;
- barcode válido y sin duplicados incompatibles;
- cantidades/umbrales/importes válidos;
- coherencia Modelo–Marca;
- integridad imagen–variante–producto;
- presencia de los CHECK de N0.3;
- presencia de los FKs compuestos críticos.

En la certificación aislada el postcheck terminó correctamente.

---

## 8. Guardas runtime y anti-regresión

Script permanente:

`backend/scripts/check-erp-n0-3-runtime.py`

Las guardas verifican explícitamente que el código no vuelva a introducir dependencias legacy críticas. Entre otras reglas:

- ventas no pueden decidir stock desde `Producto.Cantidad`;
- ventas no pueden caer de costo de variante a `Producto.Costo`;
- ventas y compras deben persistir el `ProductoVarianteId` exacto;
- repositorios no pueden usar costo/precio de `Producto` como fallback operativo;
- escáner no puede volver a costo/precio de `Producto`;
- carga masiva no puede volver a escribir costo/precio/umbral operativos directamente desde el input a `Producto`;
- la carga debe usar variante para costo/precio;
- la transición técnica→comercial debe bloquearse si la técnica conserva stock;
- la técnica debe retirarse correctamente cuando la transición es segura.

El Run N0.3 certificado ejecutó estas guardas con resultado **SUCCESS**.

---

## 9. Pruebas de regresión

La auditoría incorporó o ajustó pruebas únicamente para representar el modelo válido de N0.3 y evitar que fixtures antiguos exigieran estados ya inválidos.

Cobertura relevante:

- autoridad operativa de variante;
- venta fail-closed;
- compra fail-closed;
- stock/costo desde variante;
- carga masiva mediante variante técnica;
- transición variante técnica→comercial;
- bloqueo con stock técnico existente;
- filtros y consumidores de inventario basados en variantes;
- constraints e integridad referencial mediante MySQL real efímero;
- regresión E2E de cargas con Playwright.

### Resultado backend certificado

Workflow N0.3, Run `31528522542`:

- **284 passed**;
- **0 failed**;
- **0 skipped**;
- build Release: **0 warnings / 0 errors**.

---

## 10. Certificación aislada N0.3

### SHA funcional certificado

`07432601465189f1d6f4e5b303a7421b7be9f9f6`

### Workflow

`ERP-N0.3 - Certificación ProductoVariante autoridad única`

### Run

`31528522542`

### Resultado

**SUCCESS**

Evidencia comprobada en el job `autoridad-variante`:

- guardas runtime N0.3: SUCCESS;
- restore/build/test backend: SUCCESS;
- 284/284 pruebas backend: SUCCESS;
- Release: 0 warnings / 0 errors;
- MySQL efímero 8.4.x;
- esquema creado hasta N0.2: SUCCESS;
- preflight N0.3: SUCCESS;
- aplicación exacta de `20260811032000_N0_3_ConsolidarProductoVariante`: SUCCESS;
- postcheck y preservación de datos: SUCCESS;
- N0.3 presente en `__EFMigrationsHistory`: confirmado;
- N0.4 ausente en `__EFMigrationsHistory`: confirmado;
- escritura deliberada de SKU vacío: rechazada por `CK_ProductoVariantes_N03_Sku`;
- escritura deliberada de Modelo sin Marca: rechazada por `CK_ProductoVariantes_N03_ModeloMarca`;
- snapshot EF: `No changes have been made to the model since the last migration.`

### Aislamiento N0.3 / N0.4

El gate no utiliza la migración más reciente de forma implícita. Aplica por nombre el target N0.3 y comprueba explícitamente que la migración N0.4 no esté registrada. Por tanto, esta evidencia es válida para N0.3 de manera independiente.

---

## 11. Certificación M9 — cargas masivas y Playwright

Workflow:

`M9 - Cargas masivas profesionales`

- Run: `31528522663`.
- Job: `93902649017`.
- SHA: `07432601465189f1d6f4e5b303a7421b7be9f9f6`.
- Resultado: **SUCCESS**.

Dentro del job quedaron verdes:

- restauración y compilación backend;
- regresión backend M9;
- API contra MySQL descartable;
- frontend lint/build;
- Angular;
- **Playwright M9 y regresión de cargas: SUCCESS**;
- publicación de evidencia M9.

Esta ejecución valida de forma independiente la secuencia de importación que había revelado la coexistencia técnica/comercial y confirma la corrección sobre el SHA funcional certificado.

---

## 12. Resultado transversal M13

Workflow:

`M13 - Auditoría integral y certificación final`

- Run: `31528519432`.
- SHA: `07432601465189f1d6f4e5b303a7421b7be9f9f6`.
- Resultado final: **FAILURE**.

El fallo **no es atribuible a ERP-N0.3**.

### Gates M13 que sí quedaron verdes

- Frontend TypeScript/lint/build producción: SUCCESS;
- Docker, aislamiento y backup: SUCCESS;
- secretos, higiene y dependencias: SUCCESS;
- Backend, MySQL, migraciones, snapshot y upgrade: SUCCESS;
- Backend Release del runtime: SUCCESS;
- API Staging y migraciones: SUCCESS;
- seguridad HTTP y autorización fail-closed: SUCCESS;
- carga del frontend/Angular: SUCCESS.

### Gate que falló

`Runtime, seguridad HTTP y Playwright integral` falló en el paso **Playwright integral**.

Resultado de la suite integral:

- 107 tests descubiertos;
- 82 passed;
- 5 failed;
- 20 no ejecutados debido a fallos previos del run secuencial.

Los cinco fallos observados corresponden a expectativas/permisos transversales, principalmente RBAC:

1. `fase6-reportes-administrativos.spec.ts`: esperaba el modelo anterior de **“acceso total implícito e inmutable”** para administrador; recibió un comportamiento compatible con la transición a grants explícitos.
2. `fase7-validacion-integral.spec.ts`: creación de configuración de envío recibió `403` por falta de permiso `Administrar` en `Facturacion`.
3. `fase8-validacion-completa.spec.ts`: una ruta administrativa no presentó el `h1` esperado durante la navegación integral, en el mismo contexto de permisos/rutas administrativas.
4. `m12-automatizacion-transversal.spec.ts`: `/automatizaciones/sugerencias` recibió `403` por falta de permiso `Ver` en `Dashboard`.
5. `matriz-modulos-visual.spec.ts`: navegación de módulos administrativos no encontró el encabezado esperado, también dentro del contexto de acceso administrativo.

Estas fallas pertenecen al cierre/transición RBAC y a expectativas transversales de autorización, no a la autoridad de `ProductoVariante`.

### Evidencia de no regresión N0.3 dentro del mismo M13

En el propio Playwright integral M13 quedaron verdes pruebas relevantes de N0.3, entre ellas:

- catálogos Marca–Modelo;
- escáner de venta y compra;
- autocomplete remoto con variante exacta;
- regresiones de compatibilidad de variantes;
- Fase 4 de variantes multidimensionales;
- Angular exigiendo variante exacta en compra y venta;
- importación de color, producto y variante con inventario consolidado;
- los casos M9 ejecutados dentro de M13;
- filtros de Productos por relaciones normalizadas e inventario.

Por tanto, M13 queda registrado honestamente como **FAILURE transversal conocido, sin regresión N0.3 pendiente demostrada**. Este expediente no corrige esos fallos porque hacerlo correspondería a RBAC/N0.4 u otros alcances ajenos al cierre formal N0.3.

---

## 13. Estrategia de rollback

La migración N0.3 es **forward-only**. `Down()` no intenta reconstruir automáticamente la autoridad legacy, porque una reversión automática podría:

- reintroducir una segunda fuente de verdad;
- reconstruir datos ambiguos;
- perder trazabilidad de stock/economía por variante;
- degradar integridad Marca–Modelo o imagen–variante–producto.

Rollback operativo autorizado para una contingencia:

1. detener la aplicación del cambio;
2. utilizar el respaldo y la evidencia preflight anterior a N0.3;
3. restaurar base de datos y versión de aplicación compatibles entre sí;
4. ejecutar reconciliación y verificación de datos antes de reabrir operaciones;
5. no ejecutar un `Down()` destructivo improvisado.

ERP-N0.3 no autoriza por sí misma una reversión ni un despliegue productivo.

---

## 14. Seguridad y aislamiento de las validaciones

Las certificaciones N0.3 y M9 utilizaron infraestructura de CI descartable:

- runners de GitHub Actions;
- MySQL efímero/descartable;
- credenciales exclusivamente de prueba definidas para CI;
- API/Angular levantados dentro del runner cuando el workflow lo requiere.

No se ejecutó la migración N0.3 contra Aiven Producción.

No se modificaron:

- credenciales productivas;
- secretos productivos;
- dominios productivos;
- servicios productivos;
- base de datos productiva;
- recursos productivos.

---

## 15. Estado del repositorio y gobierno

Al preparar este expediente:

- única rama utilizada para el cierre: `Desarrollo`;
- `main`: sin modificaciones por este trabajo;
- PR oficial: `#2 — Desarrollo -> main`;
- PR #2: OPEN + DRAFT;
- merge: no autorizado/no ejecutado;
- auto-merge: no autorizado/no activado;
- despliegue: no autorizado/no ejecutado;
- Producción: no tocada.

La existencia de este documento no cambia ninguna de esas restricciones.

---

## 16. Definition of Done formal N0.3

| Criterio | Evidencia / estado |
|---|---|
| `ProductoVariante` autoridad operativa | ✅ |
| `Producto` solo proyección/compatibilidad | ✅ |
| SKU/barcode bajo variante | ✅ |
| Stock/costo/precio/umbral bajo variante | ✅ |
| Marca/Modelo/Color/Talla bajo variante | ✅ |
| Compras fail-closed | ✅ |
| Ventas fail-closed | ✅ |
| Importación por variante técnica | ✅ |
| Transición técnica→comercial controlada | ✅ |
| Técnica con stock bloquea transición | ✅ |
| Preflight | ✅ |
| Migración N0.3 | ✅ |
| Postcheck | ✅ |
| Guardas runtime | ✅ |
| Snapshot EF consistente | ✅ |
| N0.3 aislado de N0.4 | ✅ |
| Backend Release | ✅ 0 warnings / 0 errores |
| Backend tests | ✅ 284/284 |
| Run N0.3 funcional | ✅ `31528522542` |
| M9 funcional | ✅ `31528522663` |
| Playwright M9 | ✅ SUCCESS |
| M13 conocido | ⚠️ FAILURE transversal RBAC/permisos, sin regresión N0.3 demostrada |
| PR #2 | ✅ OPEN + DRAFT |
| `main` tocada | No |
| Producción tocada | No |

---

## 17. Dictamen técnico

El SHA funcional:

`07432601465189f1d6f4e5b303a7421b7be9f9f6`

queda respaldado por dos gates específicos verdes y complementarios:

- ERP-N0.3 aislado: Run `31528522542` — **SUCCESS**;
- M9 cargas masivas: Run `31528522663` — **SUCCESS**, incluido Playwright M9.

El Run M13 `31528519432` terminó **FAILURE**, pero el análisis del gate fallido demuestra que los errores corresponden a expectativas/permisos RBAC y navegación administrativa transversal. No existe en ese run una regresión N0.3 pendiente demostrada; por el contrario, las pruebas de variantes/cargas/filtros relevantes para N0.3 ejecutadas dentro de M13 quedaron verdes.

En consecuencia, el fallo M13 se registra como deuda transversal ajena al alcance de este cierre y **no justifica reabrir ni modificar la arquitectura N0.3 certificada**.

### Dictamen

**ERP-N0.3 se considera funcionalmente certificado sobre `07432601465189f1d6f4e5b303a7421b7be9f9f6`, sujeto únicamente a registrar en este mismo expediente el SHA documental descendiente y la evidencia de rerun final disponible sobre dicho HEAD.**

Una vez comprobados esos runs documentales sin regresión, el estado formal pasa a:

`✅ ERP-N0.3 = 100% CERRADO / CERTIFICADO`

Este dictamen no autoriza comenzar ERP-N0.4, modificar `main`, fusionar PR #2 ni desplegar a Producción.
