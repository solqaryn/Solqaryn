# FASE M2 — Motor de Variantes Multidimensionales

Estado documental: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**

Fecha de cierre técnico: 2026-08-09
Rama: `Desarrollo`
PR oficial: `#2 Desarrollo -> main`
Producción: **sin cambios**

> Este informe describe el estado versionado de `Desarrollo` y las validaciones realizadas en entornos descartables de CI. No afirma inspección ni migración directa de una instancia externa concreta de Aiven/Producción.

---

## 1. Objetivo

Convertir `ProductoVariante` en la unidad física exacta de inventario, eliminando la antigua equivalencia operativa “variante = color” y permitiendo que cada combinación aplicable de:

`Producto + Marca + Modelo + Color + Talla`

tenga inventario, identidad técnica, costo, precio, imágenes, auditoría y trazabilidad independientes.

Las dimensiones son opcionales cuando no aplican. No se introducen registros ficticios “N/A”, “Sin talla” o equivalentes para forzar combinaciones.

---

## 2. Arquitectura resultante

### 2.1 Producto

`Producto` representa la familia comercial. Mantiene datos comunes y campos legacy únicamente por compatibilidad progresiva e históricos. Su cantidad consolidada se deriva del inventario de variantes vigentes; no debe sumarse como inventario físico adicional.

### 2.2 ProductoVariante

La variante exacta contiene, según corresponda:

- `ProductoId`;
- `MarcaId`;
- `ModeloId`;
- `ColorId`;
- `TallaId`;
- `Sku`;
- `CodigoBarras`;
- `Cantidad`;
- `Costo`;
- `Precio`;
- `UmbralStockBajo`;
- estado;
- soft delete;
- variante técnica para producto simple;
- auditoría.

La etiqueta canónica utilizada por servicios y UI muestra únicamente dimensiones presentes:

`Marca · Modelo · Color · Talla · SKU`

### 2.3 Maestros normalizados

Marca, Modelo, Color y Talla se mantienen en tablas normalizadas independientes creadas en M1. `Modelo.MarcaId` expresa de forma explícita la pertenencia de Modelo a Marca.

El repositorio de mantenimientos opera sobre las tablas normalizadas; `CatalogosProducto` permanece únicamente como espejo/compatibilidad transitoria para consumidores legacy que todavía deban retirarse de forma progresiva. No se eliminó destructivamente la tabla legacy durante M2.

---

## 3. Integridad MySQL

### 3.1 Identidad comercial exacta

`ProductoVarianteConfiguration` utiliza una columna generada `IdentidadActivaUnica` y un índice único para representar de forma determinista la combinación:

`ProductoId : MarcaId|0 : ModeloId|0 : ColorId|0 : TallaId|0`

solo cuando la variante no está eliminada. Esto evita el comportamiento insuficiente de un `UNIQUE` convencional con columnas nullable en MySQL.

### 3.2 Identidades técnicas

- SKU: único global.
- Código de barras: único cuando existe; múltiples `NULL` continúan permitidos por MySQL.
- Variante técnica: única por Producto mediante columna generada `ProductoTecnicoUnico`.
- FKs de Variante hacia Producto/Marca/Modelo/Color/Talla con `Restrict`.
- Coherencia Modelo -> Marca validada por dominio/aplicación y por la relación normalizada del maestro.

### 3.3 Migraciones M2

- `20260809121915_M2VariantesMultidimensionales`
- `20260809123201_M2SnapshotsVariantesExactas`
- `20260809144945_M2ImagenesPorVariante`

La última migración fue generada con EF Core oficial y luego corregida quirúrgicamente para respetar la dependencia del índice de la FK `ProductoImagenes -> Productos`: primero crea el índice compuesto con prefijo `ProductoId` y solo después retira el índice simple previo.

---

## 4. Backfill y compatibilidad

El backfill preserva IDs existentes de `ProductoVariante`, Color, SKU, código de barras, cantidad, costo y precio, e incorpora Marca/Modelo/Talla heredables desde Producto cuando corresponde.

Se conserva la compatibilidad histórica y no se reescriben documentos confirmados. La evolución sigue el patrón:

`expandir -> backfill -> validar -> cambiar fuente operativa -> retirar legacy solo cuando ya no tenga consumidores`

---

## 5. Productos y administrador de variantes

El formulario de Producto trabaja con “Variantes y existencias”. Cada fila soporta:

- Marca;
- Modelo filtrado por Marca;
- Color;
- Talla/Tamaño;
- Cantidad;
- SKU opcional;
- código de barras;
- costo;
- precio;
- umbral;
- estado.

Reglas relevantes:

- Modelo requiere Marca y debe pertenecer a ella.
- Una variante comercial debe definir al menos una dimensión.
- No se permite duplicar la misma combinación dentro del formulario.
- SKU vacío es válido en UI; el backend genera uno único.
- El stock de una variante existente no se edita desde metadatos; se usa Ajustar inventario para mantener trazabilidad.
- Soft delete exige stock cero.
- Activación/desactivación es independiente de la cantidad histórica.

El administrador dedicado de variantes permite alta, edición de dimensiones/metadatos, ajuste de stock, cambio de estado, eliminación lógica y gestión de imágenes.

---

## 6. Generador de combinaciones

Se incorporó `ProductoCombinationGeneratorComponent`.

Comportamiento:

1. permite seleccionar Marca base;
2. filtra Modelos por Marca;
3. permite selección múltiple de Modelos, Colores y Tallas;
4. aplica cantidad/costo/precio/umbral inicial a la generación;
5. calcula el producto cartesiano;
6. limita cada operación a **100 combinaciones** para evitar explosión accidental;
7. descarta combinaciones ya presentes;
8. presenta vista previa;
9. no agrega ni guarda nada hasta confirmación explícita;
10. deja SKU vacío por defecto para que la autoridad de generación/unicidad sea el backend.

El componente fue integrado en el formulario de alta de Producto y validado mediante `npm run lint` y `npm run build:prod` antes de publicarse.

---

## 7. Imágenes por variante

`ProductoImagen` admite ahora `ProductoVarianteId` nullable:

- `null` = imagen general del Producto;
- valor = imagen específica de una variante exacta.

### 7.1 Principal por ámbito

`PrincipalAmbitoKey` es una columna generada almacenada que produce una clave solo si `EsPrincipal = 1`:

`ProductoId : ProductoVarianteId|0`

Un índice único garantiza en MySQL como máximo:

- una principal general por Producto;
- una principal por cada variante.

Se agregó una prueba de integración MySQL que certifica que una principal general y principales de variantes distintas pueden coexistir, mientras una segunda principal dentro de la misma variante es rechazada por la base.

### 7.2 Fallback

`ProductoVarianteImagenService.GetAsync` devuelve:

- galería propia cuando la variante tiene imágenes específicas;
- únicamente galería general del Producto cuando la variante no posee imágenes propias.

El DTO conserva `ProductoVarianteId = null` en el fallback para que la UI pueda distinguirlo visualmente.

### 7.3 Aislamiento de la portada general

`Producto.ImagenPrincipal`, `ProductoMapper` y `ProductoService` filtran estrictamente `ProductoVarianteId == null`. Una imagen principal de una variante nunca puede convertirse por accidente en portada general del Producto.

Se agregaron pruebas unitarias específicas para este comportamiento.

### 7.4 Frontend

El administrador de variantes incorpora:

- galería específica;
- indicador de fallback general;
- upload múltiple;
- máximo de 5 específicas por variante;
- selección de principal;
- eliminación;
- responsive;
- navegación por teclado en el control de carga.

La seguridad de almacenamiento/validación de imágenes existente se reutiliza; no se creó un segundo storage paralelo.

---

## 8. Compras

Compras operan sobre `ProductoVarianteId` exacto. El proceso:

- selecciona variante concreta;
- incrementa únicamente esa variante;
- actualiza el resumen de Producto;
- conserva costo de variante;
- guarda snapshots de Marca/Modelo/Color/Talla/SKU;
- crea movimiento de inventario sobre la variante exacta;
- la anulación revierte la misma variante;
- usa locks/concurrencia de inventario existentes.

---

## 9. Ventas

Ventas:

- resuelven variante exacta;
- bloquean la fila de variante;
- validan stock de esa combinación;
- descuentan solo esa variante;
- toman costo/precio/utilidad de la variante;
- persisten snapshots Marca/Modelo/Color/Talla/SKU;
- impiden sobreventa concurrente con la infraestructura de concurrencia existente;
- la anulación devuelve unidades a la misma variante.

---

## 10. Facturación, PDF y POS

`FacturaDetalle` conserva snapshots históricos de:

- Marca;
- Modelo;
- Color;
- Talla;
- SKU.

Se corrigió un hueco de presentación donde Talla no llegaba al DTO y los perfiles PDF/POS no mostraban toda la identidad de variante.

Resultado:

- `FacturaDetalleDto.VarianteTalla` expuesto;
- frontend de factura muestra Color/Talla/SKU;
- PDF de papel incorpora Color/Talla/SKU;
- formato compacto/POS incorpora Marca/Modelo/Color/Talla/SKU;
- la información se toma de snapshots del documento, no de maestros actuales, preservando inmutabilidad histórica.

Correo/WhatsApp que comparten el PDF heredan la identidad completa del documento generado.

---

## 11. Inventario y movimientos

`MovimientoInventario` mantiene `ProductoVarianteId` y snapshots de:

- Marca;
- Modelo;
- Color;
- Talla;
- SKU.

Las operaciones de compra, venta, anulación, consumo, ajuste y carga masiva reutilizan el movimiento exacto correspondiente.

`Producto.Cantidad` permanece como resumen derivado por compatibilidad; la valoración dimensional nueva toma **solo** `ProductoVariante`, evitando sumar Producto + Variante y duplicar existencias.

---

## 12. Escáner, autocomplete y búsqueda

Se reutilizó y extendió la infraestructura ya existente en lugar de reconstruirla:

- resolución exacta por SKU;
- resolución exacta por código de barras;
- búsqueda por Producto;
- Marca;
- Modelo;
- Color;
- Talla;
- etiqueta canónica;
- filtros por tipo de inventario y stock cuando aplica.

Los repositorios cargan las cuatro navegaciones normalizadas y las pruebas existentes de escáner/autocomplete permanecen dentro de la regresión automatizada.

---

## 13. Carga masiva de variantes

`VariantesInventario` fue migrado desde identidad `Producto + Color` a identidad multidimensional.

Columnas funcionales:

- Producto;
- Marca;
- Modelo;
- Color;
- Talla;
- SKU;
- Código de barras;
- Cantidad;
- Costo;
- Precio;
- Umbral;
- Activo.

Validaciones:

- Producto existente;
- Maestro activo;
- Modelo pertenece a Marca;
- al menos una dimensión;
- combinación duplicada;
- SKU duplicado global/archivo;
- código de barras duplicado global/archivo;
- snapshots de producto/variante/cantidad al validar;
- confirmación transaccional;
- lock `FOR UPDATE`;
- rechazo fail-closed si inventario o identidad cambió después del preview.

Se amplió `CargaMasivaConcurrencyTests` para trabajar con Marca + Modelo + Color + Talla completos.

---

## 14. Reportes y Dashboard

Nuevo endpoint:

`GET /dashboard/inventario/variantes`

Filtros:

- `productoId`;
- `marcaId`;
- `modeloId`;
- `colorId`;
- `tallaId`;
- `incluirInactivas`.

Devuelve:

- total de variantes;
- total de unidades;
- valoración de costo;
- valoración potencial de venta;
- agrupación por Producto;
- agrupación por Marca;
- agrupación por Modelo;
- agrupación por Color;
- agrupación por Talla;
- filas de combinación exacta.

La fuente física exclusiva del reporte es `ProductoVariante`, por lo que no existe doble conteo con `Producto.Cantidad`.

La política de visibilidad financiera existente se conserva: un usuario no administrador no recibe costo ni valoración de costo en este reporte.

---

## 15. Permisos y auditoría

Se reutilizan los permisos de Producto/Inventario existentes:

- ver;
- crear;
- editar;
- activar/desactivar;
- eliminar lógico;
- ajustar inventario;
- exportar cuando corresponde.

Se auditan operaciones de variante y también cambios de imágenes específicas. Los cambios de stock siguen el flujo de ajuste/movimiento, no una mutación silenciosa de metadatos.

---

## 16. Regresión automatizada cubierta

La matriz de M2 queda cubierta por:

- producto simple / variante técnica;
- ciclo de vida de variante técnica;
- combinación comercial multidimensional;
- unicidad de identidad/SKU/barcode;
- compra y anulación exacta;
- venta y anulación exacta;
- concurrencia y sobreventa;
- snapshots históricos;
- carga masiva con concurrencia;
- escáner;
- autocomplete;
- imagen principal general aislada de imágenes específicas;
- principal única por ámbito de imagen en MySQL;
- migraciones desde base vacía y escenarios legacy de CI;
- backend Release y unitarias;
- frontend lint/build;
- aislamiento Docker/higiene.

Evidencia final del HEAD funcional certificado `98603c21fa4b05b6e6c72565b579ab247b902b19`:

- `31325383762` — Desarrollo - Compilación y pruebas — **success**;
- `31325383745` — Desarrollo - aceptación funcional integral — **success**;
- `31325383744` — Fase 2 - Auditoría de configuración y dependencias — **success**;
- `31325383748` — Bloque 2C.1 - Variante técnica y migración — **success**;
- `31325383772` — Fase 8 - Validación completa automatizada — **success**;
- `31325383746` — Solqaryn CI — **skipped** (registrado como tal, no contabilizado como verde).

Dentro de `31325383762` quedaron en **success** backend Release, pruebas unitarias, frontend lint/build de producción, Docker/higiene, historial de migraciones, migraciones actuales, integración MySQL 8.4, verificación de variante legacy/cargas/snapshot y SQL forward.

---

## 17. Validaciones físicas/externas separadas

No se declaran como ejecutadas en esta fase las siguientes pruebas que requieren hardware, servicios o infraestructura externos reales:

- lector físico de código de barras;
- cámara Android/iPhone real;
- impresión física A4/Carta/POS-58/POS-80;
- recepción real de correo en Gmail/otro proveedor externo;
- entrega real de enlace/mensaje por WhatsApp;
- Cloudinary externo real si la validación implica credenciales/recursos no descartables;
- instancia externa concreta de Desarrollo/Aiven cuando no haya sido conectada explícitamente durante la certificación.

Estas validaciones no sustituyen ni invalidan las pruebas automatizadas, pero deben mantenerse distinguidas para no atribuir evidencia no ejecutada.

---

## 18. Commits técnicos principales

Entre los cambios de cierre de M2 destacan:

- `efbcc1c4186cc43639983aaa9140640c6b0ac8a4` — importación multidimensional normalizada;
- `9d13c985d5e8576d87ec7a8000f2118f3d8a4905` — concurrencia de carga con dimensiones completas;
- `c92c8369896824d2989366a9fbd9891f9fec1177` — migración EF de imágenes por variante;
- `86e475ce37c1fc8cc20e93667392688990fec1c9` — orden correcto de índices/FK MySQL;
- `9811be4331a500051e74fee3b1bc852044e767da` — conexión backend de imágenes por variante;
- `41b34249b142b572de563d97adc69ca448de606e` — snapshots visibles completos en factura/POS;
- `15a5e3aa6035222044fe58a82262d388a7bd29c1` — DTO de analítica dimensional;
- `2670d41efb09672163361a614f0464620ea10f41` — consulta exacta de variantes para reporte;
- `4bc2b73fe2909c754890a6ba73890f3469b8e0b7` — analítica sin doble conteo;
- `272953b19ff26b7674e207133c1d5a004bcf9395` — endpoint dimensional;
- `c253d87bd3f1a701d0f1df9ffd5e631f2b2ccf55` — aislamiento de portada general probado;
- `3b352dbab2e143f322428ffb88313afab4c8610d` — prueba MySQL de principal única por ámbito.

---

## 19. Gate de salida

Para cambiar este estado a **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**, el HEAD final debe demostrar:

- compilación backend Release: success;
- pruebas unitarias backend: success;
- migraciones MySQL descartables: success;
- pruebas de integración MySQL: success;
- snapshot EF coherente: success;
- frontend lint: success;
- frontend build producción: success;
- aceptación funcional integral: success;
- auditoría de configuración/dependencias: success;
- variante técnica/migración: success;
- Fase 8 completa: success;
- ningún P0/P1 introducido por M2.

`Solqaryn CI` se reportó como `SKIPPED`, tal como ocurrió en el HEAD certificado, y no se contabiliza como workflow verde.

---

## 20. Dictamen final M2

**M2 — Motor de Variantes Multidimensionales: COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE.**

Criterios de salida verificados:

- no existen fallos P0/P1 introducidos por M2 en los gates automatizados ejecutados;
- backend Release y pruebas: success;
- MySQL 8.4 descartable, migraciones e integración: success;
- snapshot EF: coherente;
- frontend lint/build: success;
- aceptación funcional integral: success;
- auditoría de dependencias/configuración: success;
- variante técnica/migración: success;
- Fase 8 completa: success;
- `main` y Producción no forman parte de este cierre.

Las validaciones físicas/externas enumeradas en la sección 17 permanecen separadas y no se presentan como ejecutadas.

**Fase siguiente: M3 — Configuración fiscal ISV/ISC.**
