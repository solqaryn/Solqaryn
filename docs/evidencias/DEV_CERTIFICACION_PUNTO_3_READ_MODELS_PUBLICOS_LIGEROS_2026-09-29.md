# Certificación DEV — Punto 3: read models públicos ligeros

Fecha: 2026-09-29  
Proyecto: SOLQARYN  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Resultado

**Estado certificado: LISTO en DEV.**

El storefront público separa los contratos de listado/resumen del contrato rico de detalle. Los listados ya no reutilizan el grafo administrativo ni materializan galerías completas por producto/variante.

## Implementación certificada

- `GET /tienda/productos` y `GET /tienda/productos/destacados` usan `TiendaProductoResumenDto`.
- EF usa `ProductoCatalogoResumenReadModel` y `ProductoVarianteCatalogoResumenReadModel`.
- El listado conserva: ID/slug, nombre, descripción acotada, categoría, precio/oferta, disponibilidad, imagen principal y variantes mínimas necesarias para selección/precio/stock.
- El listado no devuelve `imagenes[]` de producto ni `imagenes[]` por variante.
- `GET /tienda/productos/{slug}` conserva `ProductoCatalogoPublicoDto` y carga la galería/detalle rico únicamente para el producto solicitado.
- `POST /tienda/productos/contexto` conserva el contrato rico únicamente para los IDs acotados de carrito/cuenta.
- `TiendaController` exige `ICatalogoPublicoService`; se retiró el fallback al camino administrativo y el mapper público duplicado.
- El repositorio público no usa `ProductoRepository.ConIncludes()`.

## Evidencia Git / CI

- PR: #3478 — `perf(storefront): separar resumen público de detalle rico`.
- Functional HEAD integrado a `dev`: `3d2af21c83403fd7f4fd4f3039a26058be64bf54`.
- Project Scope Lock: PASS.
- Build backend exact-head de la rama funcional: PASS.
- Test focal `TiendaControllerDestacadosTests`: PASS.
- Guardas/lint frontend: PASS.
- Build productivo frontend: PASS.
- Preview Vercel del runtime Angular equivalente: READY.
- Los builds Vercel posteriores fueron bloqueados por el límite de builds del plan vigente; no es un fallo de código y no se compró ni cambió ningún plan.

## Evidencia runtime DEV

Render DEV `solqaryn-api-dev` desplegó exactamente `3d2af21c83403fd7f4fd4f3039a26058be64bf54`:

- deploy `dep-dau0lb8jo6nc73cofhng`;
- estado final: `live`;
- `/health`: `{"status":"ok","service":"InventoryApp API"}`.

Contrato observado en vivo:

### Listado

`GET /tienda/productos?page=1&pageSize=24`

- devuelve `descripcionResumen`;
- devuelve `imagenPrincipalUrl`;
- conserva `modelos` con identidad/precio/stock/oferta mínimos;
- no devuelve galería `imagenes[]` del producto;
- no devuelve galería `imagenes[]` en variantes.

### Detalle

`GET /tienda/productos/cargador-2`

- devuelve `descripcion` completa;
- devuelve galería `imagenes[]` del producto;
- devuelve galerías de variantes;
- confirma que el costo rico quedó reservado a la lectura individual.

## Rendimiento interno observado

Baseline anterior al Punto 2:

- `GET /tienda/productos`: **2912.0 ms / 7 queries**.

Después del Punto 2, antes de esta separación final:

- pasadas calientes típicas: **264.9–497.2 ms / 6 queries**.

Después del Punto 3, exact-head DEV:

- **254.1 ms / 5 queries**;
- **216.5 ms / 5 queries**;
- **225.4 ms / 5 queries**;
- **224.3 ms / 5 queries**.

Las pasadas calientes anteriores de 621–699.8 ms se observaron durante warm-up/concurrencia inmediatamente posterior al deploy y no se usan como referencia caliente estable.

Resultado: el listado reduce una query adicional frente al read path previo y las pasadas calientes certificadas quedan dentro del target inicial de 500 ms.

## Seguridad e impacto

- Sin migraciones.
- Sin escrituras de datos.
- Sin cambios de RBAC.
- Sin cambios de tenancy.
- Sin secretos nuevos.
- Sin nueva fuente de verdad.
- Sin servicios externos nuevos.
- Sin compra ni upgrade de servicios.
- `main` y PROD no fueron modificados.

## Cierre

El Punto 3 queda **LISTO y certificado en DEV**. No requiere acción manual ni compra de servicios por parte del propietario.
