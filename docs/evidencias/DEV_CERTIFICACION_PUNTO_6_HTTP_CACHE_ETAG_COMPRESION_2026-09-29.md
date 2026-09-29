# Certificación DEV — Punto 6: cache HTTP + ETag + compresión

Fecha: 2026-09-29  
Proyecto: SOLQARYN  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Estado

**Implementación y CI: CERTIFICADOS.**  
**Runtime DEV: pendiente de despliegue causal por bloqueo externo de cuota gratuita Vercel.**

No se compra ni se requiere ningún servicio.

## Functional HEAD

Merge funcional en `dev`:

`f3d1119133c1991b742575f8a675c9f012b9b42e`

PR:

`#3481 — perf(http): cache etag compression for public storefront`

HEAD exacto probado antes del merge:

`6b364c7c3e1fd24374fbc02cface54ad3f977bee`

## Backend HTTP

### Compresión

ASP.NET Core registra y ejecuta Response Compression sobre HTTPS con:

- Brotli;
- Gzip;
- nivel `Fastest` para minimizar CPU;
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

El bootstrap mantiene TTL corto porque incluye productos destacados.

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

## Vercel/CDN

`frontend/vercel.json` define:

- bundles Angular hashados JS/CSS: `Cache-Control: public, max-age=31536000, immutable`;
- `x-vercel-enable-rewrite-caching: 1` para `/api/:path*`.

El segundo punto no convierte respuestas privadas en públicas: únicamente permite que Vercel respete el `Cache-Control` upstream. Como el backend es `no-store` por defecto, sesiones, checkout y administración permanecen no cacheables.

## Guardas

`frontend/scripts/validate-http-cache-contract.mjs` falla si se elimina:

- Brotli/Gzip;
- `UseResponseCompression`;
- no-store por defecto;
- ETag/If-None-Match/304;
- `Vary: Accept-Encoding`;
- la exclusión de peticiones autenticadas;
- la exclusión de checkout/contexto;
- los TTL públicos definidos;
- immutable de bundles;
- rewrite caching de Vercel.

## QA exact-head

Sobre `6b364c7c3e1fd24374fbc02cface54ad3f977bee`:

- SOLQARYN Project Scope Lock: **SUCCESS**;
- VariStoreHn Fase 1: **SUCCESS**;
- VariStoreHn Fase 2: **SUCCESS**;
- VariStoreHn Fase 3: **SUCCESS**;
- VariStoreHn Fase 4: **SUCCESS**;
- VariStoreHn Fase 5: **SUCCESS**;
- VariStoreHn Fase 6: **SUCCESS**;
- VariStoreHn Fase 7: **SUCCESS**;
- backend Release build: **SUCCESS**;
- pruebas focales storefront/cache/HTTP: **SUCCESS**;
- frontend lint + guardas: **SUCCESS**;
- frontend production build: **SUCCESS**;
- Playwright acumulado: **SUCCESS**.

Durante QA se detectó una falla del harness de prueba por `RouteData` nulo. El código productivo ya compilaba; se corrigió el setup MVC del test y el rerun exact-head terminó verde.

## Datos / seguridad / coste

- sin migraciones;
- sin tablas nuevas;
- sin escrituras de datos de negocio;
- sin cambios de RBAC;
- sin secretos;
- sin Redis;
- sin CDN adicional;
- sin observabilidad pagada;
- sin upgrade Vercel;
- sin cambio de plan Render;
- `main` y PROD no fueron modificados.

## Estado proveedor

Vercel devuelve actualmente para el merge funcional:

`api-deployments-free-per-day`

El servicio Render DEV usa `autoDeployTrigger=checksPass`; por tanto no se fuerza manualmente un deploy mientras el check Vercel del commit esté fallando.

Este bloqueo es de cuota gratuita del proveedor, no un defecto funcional del changeset.

## Criterio de cierre runtime

Para convertir esta certificación en **LISTO runtime DEV** faltan únicamente evidencias causales del mismo árbol funcional:

1. Render DEV `live`;
2. GET público con `Cache-Control` esperado + ETag;
3. segundo GET con `If-None-Match` -> 304;
4. petición con compresión -> Brotli o Gzip;
5. endpoint privado/no clasificado -> `private, no-store`;
6. Vercel DEV READY con bundles hashados immutable y rewrite caching operativo.

No requiere acción manual del propietario ni compra de servicios.
