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
5. Copiar `window.__SOLQARYN_PERF_BASELINE__` desde DevTools o conservar las líneas `[SOLQARYN_PERF_BASELINE]`.
6. Correlacionar con logs Render filtrando `PerformanceBaseline`.
7. Ejecutar `npm run perf:bundle-baseline` para el baseline de bundles.

## Interpretación

- DB alta + muchas queries: optimizar read model/query antes que infraestructura.
- API alta + DB baja: revisar cold start, serialización, middleware, integraciones o CPU.
- Requests altos por pantalla: deduplicar/cachar/crear bootstrap.
- Transfer bytes altos: reducir DTO, imágenes/chunks.
- LCP alto con API rápida: priorizar imágenes, critical rendering path y bundle.
- TTFB alto sólo después de inactividad: separar explícitamente cold start de Render Free.

## Alcance

Este baseline no cambia lógica de negocio, RBAC, tenancy, persistencia, datos, PROD, `main`, dominios ni secretos.
