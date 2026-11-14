# Baseline de rendimiento DEV — SOLQARYN

## Punto 10 — Render Free y cold start (2026-09-29)

Readback vivo: tanto `solqaryn-api-dev` como `solqaryn-api-prod` continúan en plan `free`. No existe keep-alive artificial en el repositorio y se añadió `scripts/validate-render-free-policy.mjs` al Scope Lock para rechazar cron/pings a `*.onrender.com`.

Evidencia DEV separa claramente instancia fría de aplicación caliente: bootstrap **3371.5 ms / 3 queries / 130.6 ms DB** y **2608.6 ms / 4 queries / 89.1 ms DB**, seguidos por **13.5 ms / 0 queries** y **1.7 ms / 0 queries**. Esto no respalda sobredimensionar CPU/RAM; respalda tratar Render Free como entorno con cold start.

DEV puede seguir Free aceptando warm-up antes de benchmarks. PROD permanece Free por la regla de cero compras y, por tanto, no se declara backend comercial always-on. Cualquier transición futura requiere autorización explícita de gasto y dimensionamiento basado en métricas.

Evidencia: `docs/evidencias/DEV_ANALISIS_PUNTO_10_RENDER_FREE_ALWAYS_ON_2026-09-29.md`.

## Punto 9 — Aiven y topología después de reducir queries (2026-09-29)

La decisión de topología ya se toma después de reducir round trips: el listado público pasó de 7 a **5 queries por miss**, mientras los hits de cache pública operan con **0 queries**. Las pasadas calientes DEV del read-model ligero quedaron en **216.5–254.1 ms** totales con **107.5–112.3 ms DB**.

Readback vivo Render: workspace único `SOLQARYN`; DEV `oregon/free/dev`, PROD `virginia/free/main`. Aiven permanece en `do-sfo`. Los logs DEV recientes reconfirman listados calientes de 5 queries en 207.4–257.0 ms con 104.1–113.3 ms DB y hits de cache con 0 queries. Render no devolvió series `http_latency`/`http_request_count` en la ventana consultada, por lo que la penalización PROD Virginia ↔ San Francisco queda como riesgo no cuantificado, no como defecto demostrado. DEV se mantiene en Oregon y no se mueve Aiven; cualquier candidato PROD oeste futuro deberá ser blue/green, medido y autorizado expresamente.

Evidencia: `docs/evidencias/DEV_ANALISIS_PUNTO_9_AIVEN_TOPOLOGIA_2026-09-29.md`.

## Punto 8 — Angular medido y adelgazado (2026-09-29/30)

La medición canónica usa `ng build --configuration production --stats-json` y conserva tanto el baseline raw/gzip/Brotli como el desglose del grafo inicial.

| Bundle inicial Angular | Antes | Después | Cambio |
| --- | ---: | ---: | ---: |
| Raw | 730.17 kB | 580.96 kB | -20.4% |
| Transfer estimado Angular CLI | 171.55 kB | 137.46 kB | -19.9% |
| `main` raw | 126.32 kB | 66.94 kB | -47.0% |
| `styles` raw | 123.42 kB | 123.44 kB | estable |

Cambios causales: shell raíz sin Material Button/Icon eager, animaciones legacy async, `@defer (on idle)` bajo el fold del home y preload selectivo post-estabilidad sólo para rutas probables del storefront. `PreloadAllModules` permanece prohibido. El budget `initial` queda en 650 kB warning / 750 kB error.

Evidencia completa: `docs/evidencias/DEV_CERTIFICACION_PUNTO_8_ANGULAR_BUNDLE_2026-09-29.md`.

## Objetivo

Medir antes de optimizar. Este baseline no usa ni compra servicios de observabilidad externos. Toda la instrumentación vive en SOLQARYN y se activa únicamente en DEV/local.

## Métricas capturadas

### Backend ASP.NET Core

Cada request DEV registra una línea estructurada `PerformanceBaseline ApiRequest` con duración total API, cantidad de comandos SQL, tiempo acumulado de DB, porcentaje aproximado consumido por DB, bytes disponibles por Content-Length y target aplicable.

El interceptor no registra SQL, parámetros, bodies, tokens, PII ni secretos. La respuesta DEV añade `Server-Timing` con duración DB y cantidad de queries. PROD mantiene el baseline desactivado por defecto.

### Frontend Angular

`PerformanceBaselineService` se habilita automáticamente en `solqaryn-dev.vercel.app`, localhost o mediante `?perf=1`.

Captura TTFB, tiempo de carga inicial, requests totales/API por pantalla, bytes transferidos, TTFB y duración API, LCP, INP y CLS.

Las muestras quedan en:

```js
window.__SOLQARYN_PERF_BASELINE__
```

y se imprimen como `[SOLQARYN_PERF_BASELINE]`. No se envía telemetría a terceros.

### Bundles Angular

Desde `frontend`:

```bash
npm run perf:bundle-baseline
```

Genera `dist/Solqaryn-frontend/performance-bundle-baseline.json` con tamaño raw, gzip y Brotli del bundle inicial y de los chunks JS/CSS.

## Targets iniciales

| Métrica | Target inicial |
| --- | ---: |
| API pública caliente sencilla | <= 500 ms |
| Identidad pública / categorías calientes | <= 300 ms |
| LCP | <= 2500 ms |
| INP | <= 200 ms |
| CLS | <= 0.10 |
| Bundle inicial raw | por debajo del warning vigente de 1 MiB mientras se obtiene el baseline |

Cold starts de Render Free se miden por separado y no se mezclan con el target de API caliente.

## Procedimiento de captura DEV

1. Dejar que Render DEV esté despierto y ejecutar una pasada caliente.
2. Abrir `https://solqaryn-dev.vercel.app/SOLQARYN?perf=1`.
3. Navegar por portada, productos, categoría, producto, carrito y checkout.
4. Realizar al menos una interacción real por pantalla para obtener INP.
5. Esperar al menos 10 s por pantalla y usar preferentemente la muestra cuyo `reason` termina en `-settled`.
6. Copiar `window.__SOLQARYN_PERF_BASELINE__` desde DevTools o conservar las líneas `[SOLQARYN_PERF_BASELINE]`.
7. Correlacionar con logs Render filtrando `PerformanceBaseline`.
8. Ejecutar `npm run perf:bundle-baseline` para el baseline de bundles.

## Baseline real observado — 2026-09-29

Captura sobre DEV desplegado en Render commit `02ec7994430812543e43371387318cfa9822e7dd`:

| Endpoint / métrica | Total API | Queries DB | Tiempo DB | Resultado |
| --- | ---: | ---: | ---: | --- |
| Identidad pública — primera pasada post-deploy | 1606.1 ms | 1 | 20.3 ms | warm-up / fuera de target |
| Identidad pública — caliente | 182.9 ms | 1 | 20.1 ms | PASS <= 300 ms |
| Identidad pública — caliente repetida | 120.0 ms | 1 | 20.4 ms | PASS <= 300 ms |
| Categorías — primera pasada post-deploy | 1652.7 ms | 1 | 80.3 ms | warm-up / fuera de target |
| Categorías — caliente | 44.7 ms | 1 | 20.0 ms | PASS <= 300 ms |
| Categorías — caliente repetida | 184.9 ms | 1 | 78.6 ms | PASS <= 300 ms |
| Productos públicos pageSize 24 | 2912.0 ms | 7 | 210.4 ms | FAIL > 500 ms |

El listado de productos dedicó sólo ~7.2% del tiempo total medido a comandos DB, por lo que su deuda principal no se explica únicamente por latencia MySQL. El siguiente análisis debe revisar shape del read model, includes/materialización, mapeo y llamadas por producto/variante antes de considerar infraestructura pagada.

### Comparación posterior al read-model ligero — 2026-09-29

El read-model público ligero fue desplegado en DEV a partir de `75c4014a28a25726dae3798a280eec476c257dbe` y quedó incorporado en el HEAD funcional `ff8743aa14ef018450ebacafb74272076e6ab269`.

Pasadas calientes observadas posteriormente para `GET /tienda/productos`: **302.4, 497.2, 326.7, 269.8, 327.9, 287.2, 309.0 y 264.9 ms**, con 6 queries por request. Frente al baseline previo de **2912.0 ms / 7 queries**, la reducción de latencia observada es aproximadamente de **83% a 91%** en esas pasadas calientes y la mayoría cumple el target inicial de <=500 ms.

Los cold starts de Render Free permanecen separados del criterio de API caliente. La certificación completa del Punto 2 vive en `docs/evidencias/DEV_CERTIFICACION_PUNTO_2_CATALOGO_ACOTADO_2026-09-29.md`.

### Punto 3 — resumen público vs detalle rico

Functional HEAD: `3d2af21c83403fd7f4fd4f3039a26058be64bf54`.

La separación final reserva galerías y detalle completo para `GET /tienda/productos/{slug}`. El listado usa `TiendaProductoResumenDto` y su proyección EF específica; conserva una imagen principal y variantes mínimas sin galerías.

Pasadas calientes internas de `GET /tienda/productos` posteriores al deploy exact-head:

- **254.1 ms / 5 queries / 109.2 ms DB**;
- **216.5 ms / 5 queries / 107.5 ms DB**;
- **225.4 ms / 5 queries / 111.9 ms DB**;
- **224.3 ms / 5 queries / 112.3 ms DB**.

Frente al read path previo de 6 queries, el resumen dedicado elimina una query adicional. Todas estas pasadas calientes cumplen el target inicial de <=500 ms. Las primeras pasadas posteriores al deploy se mantienen separadas como warm-up/concurrencia de Render Free.

La evidencia completa está en `docs/evidencias/DEV_CERTIFICACION_PUNTO_3_READ_MODELS_PUBLICOS_LIGEROS_2026-09-29.md`.

### Punto 4 — bootstrap único del storefront

Functional HEAD: `3ad6450e037466e78c25aa07f37ff4e77e5ec109`.

La portada sustituye las lecturas iniciales independientes de identidad, WhatsApp, tema, categorías y destacados por `GET /tienda/bootstrap`. El navegador comparte la respuesta entre consumidores con `shareReplay`.

La regresión Playwright causal exige en camino feliz:

- `/tienda/bootstrap`: **1 request**;
- identidad pública separada: **0**;
- WhatsApp público separado: **0**;
- tema visual separado: **0**;
- categorías separadas: **0**;
- destacados separados: **0**.

El bootstrap ejecuta **5 queries internas** para componer el paquete completo.

Cold/warm-up posterior al deploy: **2403.1 ms / 5 queries**.

Muestra posterior:

- **559.3 ms / 5 queries / 110.7 ms DB**;
- **227.6 ms / 5 queries / 105.7 ms DB**;
- **836.1 ms / 5 queries / 376.5 ms DB**;
- **282.7 ms / 5 queries / 163.5 ms DB**;
- **218.8 ms / 5 queries / 106.0 ms DB**;
- **215.0 ms / 5 queries / 106.9 ms DB**;
- **543.8 ms / 5 queries / 319.0 ms DB**;
- **332.0 ms / 5 queries / 107.0 ms DB**.

Las pasadas calientes estables de **215.0–332.0 ms** cumplen el target inicial de <=500 ms. Los picos por encima de 500 ms coinciden con incrementos de tiempo DB en la infraestructura gratuita y se conservan en la evidencia en lugar de descartarlos.

La evidencia completa vive en `docs/evidencias/DEV_CERTIFICACION_PUNTO_4_BOOTSTRAP_STOREFRONT_2026-09-29.md`.

### Punto 5 — cache público tenant-aware en dos niveles

Functional HEAD backend: `10f77f08226bd97d966f20fb53ed8b56022d2fb6`.

La cache pública usa `IMemoryCache` in-process con partición tenant-aware, lock por key, TTL diferenciados e invalidación generacional. El frontend comparte identidad en vuelo y categorías entre rutas.

#### Bootstrap

Miss observado:

- **2848.1 ms / 7 queries / 191.8 ms DB**.

Hits posteriores sobre la misma entrada:

- **4.5 ms / 0 queries**;
- **82.5 ms / 0 queries**;
- **1.1 ms / 0 queries**;
- **1.2 ms / 0 queries**.

Otra ventana confirmó:

- miss: **1838.4 ms / 7 queries / 146.0 ms DB**;
- hit: **1.0 ms / 0 queries**.

#### Categorías compartidas

Después de que bootstrap calentó la lista:

- `GET /tienda/categorias`: **0.8–6.4 ms / 0 queries**.

#### Listado de productos

`pageSize=24`:

- miss: **1675.8 ms / 5 queries / 104.9 ms DB**;
- hit: **2.3 ms / 0 queries**.

Otra ventana:

- miss: **538.8 ms / 5 queries / 250.3 ms DB**;
- hit: **1.6 ms / 0 queries**.

#### Separación por parámetros

Con `pageSize=24` ya calentado, `pageSize=12` produjo un miss independiente:

- primer `pageSize=12`: **245.6 ms / 5 queries / 107.2 ms DB**;
- repetición: **1.3 ms / 0 queries**.

Esto confirma que la key incorpora parámetros y que una variante de listado no reutiliza indebidamente otra.

La evidencia completa vive en `docs/evidencias/DEV_CERTIFICACION_PUNTO_5_CACHE_DOS_NIVELES_2026-09-29.md`.

### Punto 6 — cache HTTP, ETag y compresión

Functional HEAD: `f3d1119133c1991b742575f8a675c9f012b9b42e`.

La certificación runtime DEV se cerró con el workflow canónico `36638217740`, que pasó:

- ETag + `304 Not Modified` para bootstrap;
- ETag y política pública corta para productos;
- compresión Brotli;
- compresión Gzip;
- `private, no-store, max-age=0` para ruta no allowlisted;
- `private, no-store, max-age=0` cuando existe `Authorization`.

Render DEV:

- deploy `dep-dau37vvlot8c7399s6g`;
- estado **live**;
- runtime equivalente al merge funcional del Punto 6.

Vercel DEV:

- deployment `dpl_E1h8BhRAVcE97JcnKJjMfcozWjB3`;
- estado **READY**;
- alias `solqaryn-dev.vercel.app`.

Headers observados en el alias canónico:

- `styles-JXKYC424.css`: `Cache-Control: public, max-age=31536000, immutable` + Brotli;
- `main-CO5LMMRB.js`: `Cache-Control: public, max-age=31536000, immutable` + Brotli;
- `/api/tienda/bootstrap`: `public, max-age=15, s-maxage=30, stale-while-revalidate=60`, ETag débil, Brotli y `Vary: Accept-Encoding`.

Las lecturas observadas del rewrite API mostraron `x-vercel-cache: MISS`; esto se conserva como dato de observación y no se interpreta como fallo, porque el backend sigue siendo la autoridad de reusabilidad y el CDN recibe correctamente la política pública upstream.

La evidencia completa vive en `docs/evidencias/DEV_CERTIFICACION_PUNTO_6_HTTP_CACHE_ETAG_COMPRESION_2026-09-29.md`.

### Bundle Angular observado

Build productivo exact-head:

- initial raw: **724.32 kB**;
- estimated transfer: **169.88 kB**;
- main: **126.05 kB raw / 28.17 kB transfer**;
- styles: **123.42 kB raw / 11.58 kB transfer**;
- mayor chunk inicial: **219.32 kB raw / 63.13 kB transfer**;
- el bundle inicial cumple el warning vigente de 1 MiB.

### Pendiente de captura humana

LCP, INP, CLS, requests y bytes **por pantalla real** requieren una sesión navegador DEV con interacción humana. La instrumentación ya está desplegada; esa captura no requiere instalar, contratar ni comprar ningún servicio.

## Interpretación

- DB alta + muchas queries: optimizar read model/query antes que infraestructura.
- API alta + DB baja: revisar cold start, serialización, middleware, integraciones o CPU.
- Requests altos por pantalla: deduplicar/cachar/crear bootstrap.
- Transfer bytes altos: reducir DTO, imágenes/chunks.
- LCP alto con API rápida: priorizar imágenes, critical rendering path y bundle.
- TTFB alto sólo después de inactividad: separar explícitamente cold start de Render Free.

## Alcance

Este baseline no cambia lógica de negocio, RBAC, tenancy, persistencia, datos, PROD, `main`, dominios ni secretos.
