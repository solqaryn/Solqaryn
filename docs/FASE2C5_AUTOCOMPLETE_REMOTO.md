# Fase 2C.5 — Autocomplete remoto de productos

## Objetivo

Eliminar la carga inicial masiva de productos en los formularios de Ventas y Compras y sustituirla por una búsqueda operativa remota, limitada, cancelable y compatible con el backend de escáner desarrollado en la Fase 2C.3 y con el frontend del escáner de la Fase 2C.4.

## Gobernanza

- Rama exclusiva: `Desarrollo`.
- `main`: congelada.
- PR #2: debe permanecer abierto y en borrador.
- Producción: no se modifica.
- No se introducen migraciones de base de datos en esta fase.

## Backend

### Endpoints

#### Ventas

```http
GET /ventas/productos/buscar?termino={texto}&limite=30
```

Permisos: `Ventas/Crear` **o** `Ventas/Editar`.

Características:

- mínimo 2 caracteres;
- máximo 100 caracteres;
- máximo 30 resultados;
- solo productos y variantes activos y no eliminados;
- excluye variantes con stock `<= 0`;
- busca por nombre, marca, modelo, color, SKU y código de barras;
- proyecta una fila operativa por variante;
- no expone costo en `ProductoEscaneadoVentaDto`.

#### Compras

```http
GET /compras/productos/buscar?termino={texto}&limite=30
```

Permisos: `Compras/Crear` **o** `Compras/Editar`.

Características:

- mismas reglas de longitud y límite;
- productos y variantes activos y no eliminados;
- permite variantes con stock cero;
- devuelve costo y precio en `ProductoEscaneadoCompraDto`.

### Repositorio

`IProductoVarianteRepository` incorpora `BuscarPorTerminoAsync`.

La implementación utiliza:

- `AsNoTracking()`;
- proyección operativa a partir de variantes;
- filtros de estado;
- filtro opcional de stock;
- ordenamiento determinista;
- `Take()` con límite máximo 30;
- `CancellationToken`.

### Servicio

`ProductoEscanerService` es la fuente compartida para:

- resolución exacta por SKU/código de barras;
- autocomplete remoto de Ventas;
- autocomplete remoto de Compras.

De esta forma se evita duplicar reglas de proyección entre escáner y búsqueda.

## Frontend

### Ventas

Se elimina la carga inicial:

```text
GET /productos?page=1&pageSize=200
```

El formulario utiliza un `FormControl` con:

- `debounceTime(300)`;
- `distinctUntilChanged()`;
- `switchMap()` para cancelar búsquedas anteriores;
- consulta únicamente desde 2 caracteres;
- visualización de máximo 30 variantes.

Al seleccionar un resultado:

1. se incorpora únicamente el producto/variante seleccionado al caché local del formulario;
2. se utiliza la misma lógica canónica que el escáner;
3. si la variante ya existe en el documento, se incrementa la cantidad;
4. se bloquea el incremento cuando supera el stock disponible;
5. nunca se recibe ni almacena costo desde el endpoint de Ventas.

### Compras

Aplica el mismo patrón remoto, con estas diferencias:

- permite seleccionar variantes con stock cero;
- utiliza el costo retornado por el backend;
- una selección repetida incrementa la cantidad sin límite de stock previo porque representa una entrada de inventario.

### Edición de borradores

La edición no vuelve a cargar el catálogo completo.

Los productos ya referenciados por el borrador se hidratan individualmente mediante `ProductoService.getById`, manteniendo funcionales los selectores existentes y evitando traer productos no utilizados.

## Relación con el escáner

2C.4 y 2C.5 utilizan el mismo modelo operativo:

- `ProductoId`;
- `ProductoVarianteId`;
- SKU;
- código de barras;
- stock;
- precio;
- costo únicamente para compras.

La búsqueda textual y el escáner convergen en una sola función de incorporación/consolidación por formulario, evitando diferencias de comportamiento entre ambos métodos de selección.

## Pruebas

### Unitarias

`ProductoAutocompleteServiceTests.cs` cubre:

- mínimo de 2 caracteres;
- máximo de 100 caracteres;
- normalización del término;
- límite máximo de 30;
- ventas con filtro de stock;
- ausencia del costo en DTO de venta;
- compras con stock cero;
- costo disponible en compras.

### Integración MySQL 8.4

`ProductoAutocompleteIntegrationTests.cs` valida la consulta real contra MySQL 8.4:

- búsqueda por nombre;
- búsqueda por marca;
- búsqueda por SKU;
- búsqueda por código con ceros iniciales;
- exclusión de stock cero en ventas;
- inclusión de stock cero en compras.

### Playwright

`frontend/e2e/fase2c5-autocomplete-remoto.spec.ts` valida:

- ausencia de carga inicial masiva de `/productos`;
- ausencia de petición con un solo carácter;
- consulta remota con 2 o más caracteres;
- máximo 30 resultados;
- ausencia de `costo` en la respuesta de Ventas;
- selección e incorporación de variante en Venta;
- exclusión de agotados en Venta;
- selección de stock cero en Compra;
- costo retornado en Compra;
- consolidación de selecciones repetidas.

Los archivos `fase2c4-escaner-frontend.spec.ts` y `fase2c5-autocomplete-remoto.spec.ts` quedaron incorporados expresamente al workflow `Desarrollo - aceptación funcional integral`.

## Limitación deliberada

En el estado actual de la rama, la entidad `Producto` todavía no contiene `TipoInventario`. Por ello esta fase no introduce de forma anticipada el filtro `MercaderiaVenta` / `InsumoAdministrativo` ni una migración ajena a su alcance. Ese filtro deberá conectarse cuando el bloque de Insumos Administrativos incorpore formalmente `TipoInventario` al dominio.

## Criterio de cierre

La fase solo puede considerarse completada cuando sobre el SHA final queden aprobados:

- compilación backend;
- pruebas unitarias;
- integración MySQL 8.4;
- lint frontend;
- build Angular de producción;
- Playwright integral, incluyendo 2C.4 y 2C.5;
- controles de seguridad y dependencias obligatorios del repositorio.
