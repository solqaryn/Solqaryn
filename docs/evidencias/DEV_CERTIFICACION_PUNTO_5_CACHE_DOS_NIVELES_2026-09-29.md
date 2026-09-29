# Certificación DEV — Punto 5: cache público tenant-aware en dos niveles

Fecha: 2026-09-29  
Proyecto: SOLQARYN  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Resultado

**Estado técnico certificado: LISTO en DEV.**

El storefront público implementa cache en dos niveles sin servicios externos ni pagos:

1. deduplicación/compartición de observables en Angular;
2. cache in-process tenant-aware en ASP.NET Core mediante `IMemoryCache`.

## Functional HEAD

Merge funcional en `dev`:

`10f77f08226bd97d966f20fb53ed8b56022d2fb6`

PR funcional:

`#3480 — perf(storefront): cache publico tenant-aware en dos niveles`

HEAD final probado antes del merge:

`eb1971d538f5c0c576ae54674d1c1c5d3de4bdff`

## Nivel 1 — Angular

### Identidad/bootstrap

`VaristorehnIdentidadService` conserva explícitamente `cargaEnVuelo$`.

Reglas:

- si la identidad ya está cargada y no hay `force`, devuelve el valor ya hidratado;
- si existe una carga en vuelo y no hay `force`, devuelve exactamente ese observable;
- la carga usa `shareReplay({ bufferSize: 1, refCount: false })`;
- `finalize` limpia la referencia en vuelo cuando concluye;
- el bootstrap compartido conserva recovery legacy únicamente si la llamada bootstrap falla.

Esto evita que consumidores concurrentes creen solicitudes HTTP duplicadas.

### Categorías

`VaristorehnService.obtenerCategorias(force = false)` conserva `categorias$` compartido:

- múltiples rutas reutilizan la misma respuesta;
- usa `shareReplay({ bufferSize: 1, refCount: false })`;
- si la petición falla, la referencia se limpia para permitir un retry válido.

## Nivel 2 — ASP.NET Core

### Implementación

La cache vive en:

- `IPublicStoreCache`;
- `PublicStoreMemoryCache`;
- `IPublicStoreTenantKeyProvider`;
- `PublicStoreTenantKeyProvider`.

No se añadió Redis, servicio administrado, storage externo ni dependencia de pago.

### Claves

Cada key incorpora:

- tenant;
- segmento;
- generación global;
- generación tenant;
- hash de parámetros.

Formato conceptual:

`tenant + segmento + generación + hash(parámetros)`.

### Partición tenant

El tenant público se resuelve como:

- `empresa:{EmpresaId}` cuando la identidad pública coincide inequívocamente con una empresa;
- `public-config:{ConfiguracionId}` como fallback fail-safe cuando no puede desambiguarse una empresa.

La propia resolución tenant usa el segmento de identidad de la cache, de forma que invalidar identidad fuerza una nueva resolución.

### TTL

- identidad: **5 minutos**;
- tema: **5 minutos**;
- categorías: **5 minutos**;
- destacados: **30 segundos**;
- listados de productos: **15 segundos**.

Se mantienen **sin cache**:

- detalle rico de producto;
- contexto de carrito/cuenta por IDs;
- checkout y revalidación de compra.

### Anti-stampede

`PublicStoreMemoryCache` usa `SemaphoreSlim` por cache key. Consumidores concurrentes del mismo tenant/segmento/parámetros comparten la primera creación de la entrada.

### Invalidación

`AppDbContext.SaveChangesAsync` detecta cambios relevantes antes de persistir y, tras un guardado exitoso, incrementa generaciones de cache.

Cambios cubiertos:

- `Empresa`;
- `EmpresaConfiguracion`;
- `ConfiguracionWhatsAppEmpresa`;
- `TemaVisual`;
- `Categoria`;
- `Producto`;
- `ProductoVariante`;
- `ProductoImagen`;
- `ExistenciaVariante`;
- `Marca`;
- `Modelo`;
- `Descuento`;
- `DescuentoProducto`;
- `DescuentoCategoria`.

La invalidación generacional evita enumerar/borrar manualmente todas las combinaciones de parámetros.

## QA exact-head

Sobre el HEAD final de la PR:

- SOLQARYN Project Scope Lock: **SUCCESS**;
- VariStoreHn Fase 1: **SUCCESS**;
- VariStoreHn Fase 2: **SUCCESS**;
- VariStoreHn Fase 3: **SUCCESS**;
- VariStoreHn Fase 4: **SUCCESS**;
- VariStoreHn Fase 5: **SUCCESS**;
- VariStoreHn Fase 6: **SUCCESS**;
- VariStoreHn Fase 7: **SUCCESS**;
- backend Release build: **SUCCESS**;
- pruebas focales de destacados/bootstrap/cache: **SUCCESS**;
- frontend lint/guardas: **SUCCESS**;
- frontend production build: **SUCCESS**;
- Playwright Fase 7: **SUCCESS**.

Pruebas específicas verifican:

- diez consumidores concurrentes ejecutan una sola factory de cache;
- tenant y parámetros producen particiones distintas;
- invalidación tenant afecta únicamente ese tenant;
- invalidación global cambia la generación;
- resolución tenant se reutiliza y se vuelve a resolver tras invalidación de identidad;
- un listado idéntico reutiliza cache;
- `SaveChangesAsync` invalida los segmentos públicos correspondientes.

## Runtime DEV — Render

Servicio:

`solqaryn-api-dev`

Deploy:

`dep-dau26a5g1s2s73b5urfg`

Commit:

`10f77f08226bd97d966f20fb53ed8b56022d2fb6`

Estado:

**live**

### Bootstrap

Primer miss observado tras deploy:

- **2848.1 ms / 7 queries**.

Hits inmediatamente posteriores:

- **4.5 ms / 0 queries**;
- **82.5 ms / 0 queries**;
- **1.1 ms / 0 queries**;
- **1.2 ms / 0 queries**.

Otra ventana posterior confirmó:

- miss: **1838.4 ms / 7 queries**;
- hit: **1.0 ms / 0 queries**.

### Categorías compartidas entre rutas

Después de que el bootstrap calentó la lista de categorías:

- `GET /tienda/categorias`: **0.8–6.4 ms / 0 queries**.

Esto demuestra que bootstrap y la ruta de categorías comparten el mismo cache servidor.

### Listado de productos

`pageSize=24`:

- miss: **1675.8 ms / 5 queries**;
- hit: **2.3 ms / 0 queries**.

Otra ventana:

- miss: **538.8 ms / 5 queries**;
- hit: **1.6 ms / 0 queries**.

### Separación por parámetros

Con `pageSize=24` ya calentado, se consultó `pageSize=12`.

Resultado:

- primer `pageSize=12`: **245.6 ms / 5 queries**;
- repetición `pageSize=12`: **1.3 ms / 0 queries**.

Esto confirma que un conjunto de parámetros no reutiliza indebidamente la entrada de otro.

## Frontend Vercel

El preview del proyecto `solqaryn-dev`:

`dpl_DoZuHfXdPM91YEbyFaVLJopSeqns`

está **READY** y responde HTTP 200 en `/varistorehn`.

Commit del preview:

`87f86b3f7dc871f742ad4a1f3c347e0f896f7340`.

La comparación `87f86b3... -> eb1971d...` cambia únicamente tests/guardas; no cambia archivos Angular de runtime. Por tanto, ese preview es runtime-equivalente al frontend funcional del merge.

El alias canónico del proyecto DEV no se forzó a un rebuild adicional porque Vercel alcanzó el límite gratuito diario de deployments. No se compró ni se cambió de plan.

## Topología futura

La implementación actual es deliberadamente in-process y es correcta para la topología vigente de una instancia API.

Si SOLQARYN escala horizontalmente a múltiples instancias simultáneas, `IPublicStoreCache` debe sustituirse por una implementación distribuida con invalidación compartida antes de asumir coherencia cross-instance. El contrato ya deja esa migración encapsulada.

## Seguridad e impacto

- sin migraciones;
- sin nuevas tablas;
- sin escrituras de datos de negocio durante la certificación runtime;
- sin cambios de RBAC;
- sin cambios de tenancy de datos;
- sin secretos nuevos;
- sin Redis ni servicio de cache externo;
- sin compra ni upgrade de servicios;
- `main` y PROD no fueron modificados.

## Cierre

El Punto 5 queda implementado, integrado y certificado técnicamente en `dev`.

La única limitación operativa observada es el límite gratuito diario de builds de Vercel para volver a apuntar el alias canónico a un artefacto nuevo; el frontend funcional quedó validado en preview READY y es runtime-equivalente al merge.

No requiere compra de servicios.
