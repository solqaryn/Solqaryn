# ERP-N0.1 — Producto legacy / autoridad de ProductoVariante

Estado: **cerrado en código; eliminación física condicionada a seguridad histórica**.

## Objetivo

`Producto` representa la familia comercial. `ProductoVariante` es la autoridad operacional para dimensiones, SKU/código de barras, stock, costo, precio y umbral por variante. Las columnas duplicadas que permanecen en `Productos` son únicamente proyecciones/snapshots de compatibilidad mientras existan contratos e históricos que las consuman.

## 1. Inventario completo de columnas legacy

| Columna `Productos` | Duplicada en variante | Estado N0.1 | Motivo de permanencia física |
|---|---:|---|---|
| `Marca` | Sí, vía `ProductoVariante.MarcaId -> Marcas` | Snapshot legacy, sin autoridad | Compatibilidad API/UI e histórico; puede quedar vacía en productos multidimensionales |
| `Modelo` | Sí, vía `ProductoVariante.ModeloId -> Modelos` | Snapshot legacy, sin autoridad | Compatibilidad API/UI e histórico |
| `ColorId` | Sí | Legacy, sin autoridad | Apunta a `CatalogosProducto`, mientras variante apunta a `Colores`; no son FKs intercambiables |
| `TallaId` | Sí | Legacy, sin autoridad | Misma incompatibilidad de catálogo |
| `MarcaId` | Sí | Legacy, sin autoridad | `CatalogosProducto` vs `Marcas` |
| `ModeloId` | Sí | Legacy, sin autoridad | `CatalogosProducto` vs `Modelos` |
| `Cantidad` | Sí | **Proyección materializada** | `AppDbContext` conserva snapshots agregados de compras/anulaciones y validaciones de stock total |
| `Costo` | Sí | **Proyección materializada** | Snapshots/reversión de valorización de compras |
| `Precio` | Sí | **Proyección materializada** | Compatibilidad de contratos y resumen de familia |
| `UmbralStockBajo` | Sí | **Proyección materializada** | Compatibilidad de resumen familiar; el umbral operativo vive en variante |

No se consideran legacy `Nombre`, `Descripcion`, `TipoInventario`, `CategoriaId`, `Activo` ni `Eliminado`: son responsabilidades de la familia. Las imágenes generales (`ProductoVarianteId = NULL`) también siguen siendo responsabilidad de `Producto`.

## 2. Consumidores backend

- `ProductosController`: entrada principal de altas/ediciones. N0.1 sanea cualquier resumen legacy recibido y lo recalcula desde `Variantes[]` antes de persistirlo. Una fila única sin dimensiones/SKU/código se interpreta como producto simple y se materializa/sincroniza como variante técnica.
- `ProductoService`: conserva asignaciones legacy internas por compatibilidad, pero desde N0.1 el controlador no le entrega valores independientes cuando existe `Variantes[]`; recibe una proyección derivada.
- `ProductoVarianteService`: autoridad de CRUD de variantes y responsable de recalcular `Producto.Cantidad/Costo/Precio` como proyección.
- `ProductoMapper`: desde N0.1 lee stock, costo, precios, umbral y dimensiones desde `ProductoVariante`; solo usa `Producto` como fallback para registros todavía no backfilleados.
- `ProductoRepository`: filtros y agregados de inventario ya operan mayoritariamente sobre variantes. Las referencias restantes a `Producto.Cantidad/Costo/Precio` se consideran acceso a proyección materializada, no autoridad.
- `InventarioConcurrencyService`: el ajuste legacy a nivel producto ahora redirige a la variante técnica cuando el producto es simple. Para variantes comerciales exige `ProductoVarianteId`.
- `AppDbContext`: captura `Producto.Cantidad/Costo` y snapshots de `ProductoVariante` al confirmar/anular compras. Esta dependencia bloquea el DROP físico de esas columnas en N0.1.
- Compras/ventas/inventario: las operaciones modernas transportan `ProductoVarianteId`; los caminos legacy continúan protegidos por la proyección agregada durante la transición.

## 3. Consumidores frontend

- `frontend/src/app/features/productos/producto-form.component.ts`: administra `variantes[]` como filas físicas y calcula un resumen legacy para compatibilidad del contrato multipart.
- `frontend/src/app/services/producto.service.ts`: envía tanto los aliases legacy como `Variantes[index].*`. N0.1 considera autoritativo únicamente `Variantes[]`; el backend recalcula los aliases.
- `frontend/src/app/core/models/producto.model.ts`: mantiene campos de resumen (`cantidad`, `costo`, `precio`, `marca`, etc.) para listas/detalle y `ProductoVariante[]` para operación. Los campos de resumen pasan a ser read models.
- `producto-detail`, listados y componentes de inventario pueden seguir mostrando los campos resumen porque `ProductoMapper` los deriva de variantes.

No se rompe el contrato HTTP en N0.1. Retirar propiedades legacy del modelo TypeScript/DTO es una ruptura de API y se hará después de que telemetría/consumidores confirmen que ya no son necesarias.

## 4. Reportes dependientes

- Los totales de unidades y valorización del repositorio de productos ya consultan `ProductoVariantes`.
- Los reportes/escáneres que exponen variante usan SKU, dimensiones, cantidad, costo y precio de `ProductoVariante`.
- Los campos agregados de `Producto` que aún aparezcan en ordenamiento/resumen se consideran proyección materializada y deben coincidir con el script de validación.
- `ReporteAdministrativoService` no depende de atributos operacionales de producto.

Regla de salida: ningún reporte nuevo puede introducir lectura operacional directa de `Producto.Cantidad`, `Producto.Costo`, `Producto.Precio` o IDs legacy.

## 5. Migraciones históricas

La rama conserva infraestructura de variantes multidimensionales y de variante técnica, además de los preflight históricos:

- `backend/scripts/preflight-m2-variantes-multidimensionales.sql`
- `backend/scripts/preflight-variante-tecnica.sql`
- configuración `ProductoVarianteConfiguration` con unicidad de identidad activa y una única variante técnica por producto.
- migraciones EF actualmente rastreadas en `backend/src/Infrastructure/Persistence/Migrations` incluyen las migraciones M7; el historial anterior fue consolidado en la evolución del repositorio y no debe inferirse únicamente desde esa carpeta.

N0.1 agrega scripts explícitos e idempotentes de preflight/backfill/validación sin asumir equivalencia entre IDs de `CatalogosProducto` y tablas normalizadas.

## 6. Comparación Producto vs ProductoVariante

### Autoridad `Producto`

- identidad/nombre de familia
- descripción
- tipo de inventario
- categoría
- estado/eliminación de familia
- galería general

### Autoridad `ProductoVariante`

- Marca/Modelo/Color/Talla operacionales
- SKU
- código de barras
- cantidad/stock
- umbral de stock bajo
- costo
- precio
- estado de variante
- galería específica de variante

### Proyección temporal en `Producto`

`Cantidad`, `Costo`, `Precio`, `UmbralStockBajo`, `Marca`, `Modelo` y los cuatro IDs legacy. No deben tomarse como fuente primaria para una operación nueva.

## 7. Script de backfill

Archivo: `backend/scripts/backfill-erp-n0-1-producto-variante.sql`.

Acciones:

1. completa `Costo/Precio` nulos en variantes existentes usando la última proyección legacy;
2. crea una variante técnica `TEC-{ProductoId}` para cada producto vigente sin variante;
3. no copia IDs legacy de dimensiones porque pertenecen a tablas distintas;
4. recalcula `Productos.Cantidad/Costo/Precio/UmbralStockBajo` desde variantes;
5. es idempotente respecto de productos que ya poseen variantes.

Debe ejecutarse únicamente con preflight en cero.

## 8. Validación de datos

Archivo: `backend/scripts/preflight-erp-n0-1-producto-variante.sql`.

Comprueba, entre otros:

- stock negativo;
- colisión de SKU técnico;
- producto sin variante;
- costo/precio nulo en variante;
- stock agregado desalineado;
- costo/precio agregados desalineados;
- coexistencia de variante técnica y comercial;
- más de una técnica por producto;
- variante técnica con dimensiones comerciales.

Criterio de aceptación: `Bloqueos = 0` antes del backfill y `ErroresAutoridad = 0` después.

## 9. Cambio gradual de lecturas

Implementado en `ProductoMapper`: cuando existen variantes, el DTO familiar deriva:

- `Cantidad`: suma de variantes vigentes;
- `Costo`: promedio ponderado por existencia, o promedio simple si stock total es cero;
- `Precio/PrecioMinimo/PrecioMaximo`: variantes operativas;
- `UmbralStockBajo`: suma de umbrales de variantes;
- estado de inventario: variantes activas;
- IDs/nombres de dimensión: solo se publican como valor común cuando todas las variantes comparten el mismo valor.

El fallback a `Producto` existe únicamente para datos previos al backfill.

## 10. Cambio gradual de escrituras

`ProductosController` recalcula el resumen legacy desde `Variantes[]` antes de delegar a `ProductoService`. Un cliente no puede enviar `Cantidad/Costo/Precio/Marca/Modelo/IDs` contradictorios y convertirlos en autoridad si también envía variantes.

Productos simples: una única fila sin dimensiones/SKU/código se trata como variante técnica y se sincroniza con el resumen derivado.

## 11. Desactivar escritura legacy

Estado N0.1:

- **desactivada como autoridad externa** cuando `Variantes[]` está presente;
- **compatibilidad temporal** cuando un cliente antiguo no envía `Variantes[]`;
- toda operación nueva debe enviar/usar variante;
- ajustes de inventario a nivel producto son redirigidos a la variante técnica cuando existe.

La compatibilidad sin `Variantes[]` es deliberada para evitar una ruptura abrupta de clientes desplegados; no otorga autoridad preferente frente a variantes persistidas.

## 12. Eliminar dependencias

Eliminadas o neutralizadas en N0.1:

- DTO de salida ya no toma dimensiones/precios/stock de `Producto` cuando existen variantes;
- resumen de alta/edición no puede contradecir `Variantes[]`;
- ajuste de stock simple deja de escribir únicamente `Producto.Cantidad` una vez existe variante técnica;
- nuevos productos sin variantes comerciales quedan representados por variante técnica.

Dependencias que permanecen intencionalmente:

- snapshots/reversión de compra en `AppDbContext`;
- contrato HTTP legacy sin `Variantes[]`;
- columnas FK legacy hacia `CatalogosProducto`, pendientes de reconciliación de catálogos N0.2.

## 13. Eliminar columnas cuando sea seguro

**No se ejecuta DROP físico en N0.1.** Es una decisión de seguridad, no trabajo pendiente oculto.

Bloqueos demostrados:

1. `Cantidad/Costo` participan en snapshots y restauración de compras confirmadas/anuladas.
2. `Marca/Modelo/ColorId/TallaId/MarcaId/ModeloId` sostienen compatibilidad e histórico y pertenecen al catálogo legacy, cuya reconciliación con las tablas normalizadas es N0.2.
3. `Precio/UmbralStockBajo` siguen expuestos por el contrato familiar de compatibilidad.

### Gate para el DROP futuro

Una columna solo puede eliminarse cuando:

- búsqueda estática = 0 consumidores operacionales;
- preflight de datos = 0 diferencias;
- frontend desplegado ya no la envía;
- no participa en snapshot/reversión histórica;
- existe migración `Up/Down` segura y backup/restore probado;
- CI backend/frontend e integración pasan.

Hasta entonces su presencia física no implica autoridad. La autoridad operacional queda en `ProductoVariante` desde este punto.
