# Certificación DEV — Punto 6: cache HTTP + ETag + compresión

Fecha: 2026-09-29  
Proyecto: SOLQARYN  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Estado

**LISTO y certificado en DEV.**

No se compró ni se requiere ningún servicio.

## Functional HEAD

Merge funcional en `dev`:

`f3d1119133c1991b742575f8a675c9f012b9b42e`

PR:

`#3481 — perf(http): cache etag compression for public storefront`

HEAD exacto probado antes del merge:

`6b364c7c3e1fd24374fbc02cface54ad3f977bee`

Los commits posteriores en `dev` que participaron en el cierre del Punto 6 modifican únicamente workflow/checkpoint de certificación y documentación; el runtime funcional permanece equivalente al merge anterior.

## Backend HTTP

### Compresión

ASP.NET Core registra y ejecuta Response Compression sobre HTTPS con:

- Brotli;
- Gzip;
- nivel `Fastest`;
- JSON/text y `application/problem+json`.

### Política segura por defecto

Toda respuesta API que no esté explícitamente allowlisted conserva:

`Cache-Control: private, no-store, max-age=0`

Esto cubre por defecto:

- autenticación y sesiones;
- administración;
- endpoints autenticados;
- datos sensibles;
- contexto de carrito;
- checkout;
- documentos privados;
- errores y rutas no clasificadas.

Incluso un GET público marcado como cacheable cae a `no-store` si la petición trae `Authorization` o un usuario autenticado.

### Allowlist pública

Sólo GET públicos explícitos usan `PublicHttpCacheAttribute`.

Identidad/tema/WhatsApp/categorías:

`public, max-age=120, s-maxage=300, stale-while-revalidate=600`

Bootstrap:

`public, max-age=15, s-maxage=30, stale-while-revalidate=60`

Productos/listados/detalle:

`public, max-age=5, s-maxage=15, must-revalidate`

### ETag / 304

Las respuestas allowlisted:

- serializan el payload con las mismas opciones JSON de MVC;
- calculan SHA-256;
- emiten ETag débil;
- añaden `Vary: Accept-Encoding`;
- resuelven `If-None-Match` coincidente como `304 Not Modified`.

Los errores 4xx/5xx no quedan en cache público.

## Vercel / CDN

`frontend/vercel.json` define:

- bundles Angular hashados JS/CSS: `Cache-Control: public, max-age=31536000, immutable`;
- `x-vercel-enable-rewrite-caching: 1` para `/api/:path*`.

El rewrite no convierte respuestas privadas en públicas: Vercel conserva la política emitida por el backend. Como el backend es `no-store` por defecto, sesiones, checkout y administración permanecen no cacheables.

## QA exact-head

Sobre `6b364c7c3e1fd24374fbc02cface54ad3f977bee`:

- SOLQARYN Project Scope Lock: **SUCCESS**;
- SOLQARYN Fase 1: **SUCCESS**;
- SOLQARYN Fase 2: **SUCCESS**;
- SOLQARYN Fase 3: **SUCCESS**;
- SOLQARYN Fase 4: **SUCCESS**;
- SOLQARYN Fase 5: **SUCCESS**;
- SOLQARYN Fase 6: **SUCCESS**;
- SOLQARYN Fase 7: **SUCCESS**;
- backend Release build: **SUCCESS**;
- pruebas focales storefront/cache/HTTP: **SUCCESS**;
- frontend lint + guardas: **SUCCESS**;
- frontend production build: **SUCCESS**;
- Playwright acumulado: **SUCCESS**.

Durante QA se detectó una falla del harness de prueba por `RouteData` nulo. El código productivo ya compilaba; se corrigió el setup MVC del test y el rerun exact-head terminó verde.

## Certificación runtime DEV

### Render

Servicio:

`solqaryn-api-dev`

Deploy causal:

`dep-dau37vvlot8c739g9s6g`

Commit desplegado:

`d550dc0668fe1eeaa45348d2bc2cd46e7638d4d9`

Estado:

**live**

Ese commit contiene el mismo runtime funcional del Punto 6; las diferencias respecto a `f3d1119...` son únicamente de documentación/QA.

### Workflow canónico DEV

Run:

`36638217740`

Job:

`Certificar DEV canónico extremo a extremo`

Estado:

**SUCCESS**

Probes runtime confirmados:

- HTTP cache y ETag bootstrap: **SUCCESS**;
- HTTP cache y ETag productos: **SUCCESS**;
- Brotli: **SUCCESS**;
- Gzip: **SUCCESS**;
- no-store ruta no allowlisted: **SUCCESS**;
- no-store request con Authorization: **SUCCESS**;
- Render health: **SUCCESS**;
- rutas/branding/API/legacy: **SUCCESS**.

### ETag real

Bootstrap DEV emitió:

`W/"aaa17650683729d118bad4281da17980810f3b0941150da00cb4d7e26fca455d"`

La repetición con `If-None-Match` coincidente devolvió:

**304 Not Modified**

Productos públicos también emitieron ETag y superaron el probe 304/cache del workflow canónico.

## Vercel DEV canónico

Deployment promovido:

`dpl_E1h8BhRAVcE97JcnKJjMfcozWjB3`

Commit de origen:

`02eb746117e98d903e9d984111aee00d8ce5c80a`

Estado:

**READY**

Target:

**production del proyecto `solqaryn-dev`**

Alias:

- `solqaryn-dev.vercel.app`;
- `solqaryn-dev-solqaryn.vercel.app`;
- `solqaryn-dev-git-cert-http-transport-runtime-20260929-solqaryn.vercel.app`.

Vercel reconstruyó internamente el artefacto al ejecutar `Promote to Production`; no hubo compra, upgrade ni cambio de plan.

La comparación entre `f3d1119...` y `02eb746...` modifica únicamente workflow/documentación de certificación, por lo que el runtime Angular es equivalente al Punto 6 funcional.

### Headers reales del CDN

`https://solqaryn-dev.vercel.app/styles-JXKYC424.css`:

- HTTP 200;
- `Cache-Control: public, max-age=31536000, immutable`;
- `Content-Encoding: br`.

`https://solqaryn-dev.vercel.app/main-CO5LMMRB.js`:

- HTTP 200;
- `Cache-Control: public, max-age=31536000, immutable`;
- `Content-Encoding: br`.

`https://solqaryn-dev.vercel.app/api/tienda/bootstrap`:

- HTTP 200;
- `Cache-Control: public, max-age=15, s-maxage=30, stale-while-revalidate=60`;
- ETag débil correcto;
- `Content-Encoding: br`;
- `Vary: Accept-Encoding`.

En las lecturas observadas `x-vercel-cache` fue `MISS`; esto no altera la política: el rewrite preserva la elegibilidad y el backend continúa siendo la autoridad que decide si una respuesta es pública o `no-store`.

## Datos / seguridad / coste

- sin migraciones;
- sin tablas nuevas;
- sin escrituras de datos de negocio;
- sin cambios de RBAC;
- sin secretos nuevos;
- sin Redis;
- sin CDN adicional;
- sin observabilidad pagada;
- sin upgrade Vercel;
- sin cambio de plan Render;
- `main` y `solqaryn-prod` no fueron modificados.

## Cierre

El Punto 6 queda **LISTO y certificado en DEV**:

- cache HTTP público allowlisted;
- ETag + 304;
- Brotli/Gzip;
- no-store por defecto y para requests autenticados;
- bundles Angular hashados immutable;
- alias canónico `solqaryn-dev.vercel.app` sobre deployment READY del Punto 6.

No requiere acción manual adicional ni compra de servicios.
