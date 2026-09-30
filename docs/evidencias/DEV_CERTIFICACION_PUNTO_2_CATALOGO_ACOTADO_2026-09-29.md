# Certificación DEV — Punto 2: eliminación de descarga completa del catálogo

Fecha: 2026-09-29  
Proyecto: SOLQARYN  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Resultado

**Estado certificado: LISTO en DEV.**

El storefront público ya no depende de descargar todas las páginas del catálogo para catálogo, categoría, detalle, carrito, checkout ni cuenta.

## Evidencia funcional

- `VaristorehnService` ya no expone `obtenerCatalogo()`.
- Listado: `GET /tienda/productos` recibe paginación y filtros server-side.
- Categoría: solicita únicamente la categoría y una página pequeña filtrada por `categoriaId`.
- Detalle: `GET /tienda/productos/{slug}` carga un solo producto.
- Relacionados: consulta una página pequeña por categoría.
- Carrito: `POST /tienda/productos/contexto` rehidrata únicamente IDs persistidos.
- Checkout: rehidrata únicamente los IDs del carrito y envía las líneas a `POST /tienda/checkout/validar`; no descarga el catálogo completo.
- Cuenta/favoritos/recompra: usa contexto por IDs referenciados.
- Gates estáticos del storefront prohíben reintroducir `obtenerCatalogo()`.

## Arquitectura

El read path público está separado del repositorio administrativo:

`TiendaController -> ICatalogoPublicoService -> IProductoCatalogoPublicoRepository -> proyecciones EF/MySQL`

No se añadió persistencia, migración, fuente de verdad paralela ni servicio externo.

## Evidencia Git y despliegue

- Baseline previo registrado sobre `02ec7994430812543e43371387318cfa9822e7dd`.
- Read-model ligero introducido en `75c4014a28a25726dae3798a280eec476c257dbe`.
- HEAD funcional certificado: `ff8743aa14ef018450ebacafb74272076e6ab269`.
- Render DEV `solqaryn-api-dev`: deploy de `ff8743aa14ef018450ebacafb74272076e6ab269` en estado `live`.
- Vercel DEV `solqaryn-dev`: deployment del mismo commit en estado `READY`.
- `main` y PROD no fueron modificados por este cierre.

## Comparación de rendimiento

Antes del read-model ligero:

- `GET /tienda/productos`: **2912.0 ms**, 7 queries, 210.4 ms DB.

Después del read-model ligero, en pasadas calientes observadas en Render DEV:

- 302.4 ms, 6 queries;
- 497.2 ms, 6 queries;
- 326.7 ms, 6 queries;
- 269.8 ms, 6 queries;
- 327.9 ms, 6 queries;
- 287.2 ms, 6 queries;
- 309.0 ms, 6 queries;
- 264.9 ms, 6 queries.

La reducción observada frente al baseline de 2912.0 ms es aproximadamente de **83% a 91%** en las pasadas calientes listadas, con la mayoría dentro del target inicial de 500 ms.

Los cold starts del plan Free permanecen separados de este criterio, conforme al baseline canónico.

## Seguridad e impacto

- Sin cambios de RBAC.
- Sin cambios de tenancy.
- Sin secretos nuevos.
- Sin migraciones.
- Sin escrituras de datos.
- Sin contratación de servicios.
- Sin cambios en PROD o `main`.

## Cierre

El objetivo del Punto 2 queda cumplido y certificado en DEV: el patrón de “descargar todo el catálogo” fue retirado y sustituido por lecturas acotadas y consultas específicas.

No queda acción manual requerida al propietario para este punto.
