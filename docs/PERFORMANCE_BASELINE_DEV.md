# Baseline de rendimiento DEV — SOLQARYN

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

Genera `dist/inventoryapp-frontend/performance-bundle-baseline.json` con tamaño raw, gzip y Brotli del bundle inicial y de los chunks JS/CSS.

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
2. Abrir `https://solqaryn-dev.vercel.app/varistorehn?perf=1`.
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
