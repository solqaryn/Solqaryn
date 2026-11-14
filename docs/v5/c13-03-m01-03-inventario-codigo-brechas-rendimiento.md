# SOLQARYN V5 FINAL — C13-03.M01.03: inventario de código y brechas de rendimiento

**Fecha:** 2026-11-14. **Task autora:** SOLQARYN PRIMARY DEV 04 — JAVIER, `6aeacb7820ac8191b7d768a6f33dfa09`. **Repositorio:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Tipo:** auditoría documental M01, sin mediciones runtime ni cambios ejecutables.

**Autoridades únicas:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). La microtarea C13-03.M01.03 exige **leer contratos, código y evidencia disponible sin asumir certificación** para carga inicial, navegación, imágenes, tablas, datos y móviles. La elegibilidad DAG VALIDADO/SI habilita esta preparación documental, no M03, cambios runtime, QA, main ni producción. Los contratos de C13-03.M01.01 (NR01–NR14 y P01–P04/N01–N12 futuros) y M01.02 anteceden al inventario; **no se ejecutó benchmark, prueba de dispositivo o test de navegador por redactarlo**.

## 1. Inventario verificable del árbol de dev: archivos y significado limitado

Los identificadores son SHA de **blob de Git**, no SHA de compilación, CI o ejecución. Ninguno prueba por sí solo tamaños emitidos, LCP, INP, CLS, latencias reales, compatibilidad móvil o éxito de una prueba.

| Ref | Ruta y blob inspeccionado | Hecho que sí respalda / límite |
| --- | --- | --- |
| F01 | `frontend/angular.json` — `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | Budget production initial `maximumWarning=650kb`, `maximumError=750kb`; no son bytes emitidos ni rendimiento medido |
| F02 | `frontend/package.json` — `e6d030f643421cb343cc12617e742469e88223c4` | Scripts build, lint, `perf:bundle-baseline`, `build:stats`, responsive y unit test; script disponible no equivale a ejecutado |
| F03 | `frontend/src/app/app.routes.ts` — `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas lazy y guards; no demuestra carga eficiente en red ni acceso servidor |
| F04 | `frontend/scripts/validate-angular-bundle-policy.mjs` — `8081db397b5cdc5d3799f858faaa2ed7d52761f0` | Valida budget 650/750, preloading selectivo y rechaza PreloadAllModules; verificación estática, no build |
| F05 | `frontend/scripts/performance-bundle-baseline.mjs` — `e233339922e471076f08ffe3c785d9cd2b9238fa` | Lee el build producido, calcula bytes raw/gzip/brotli y escribe JSON; requiere `npm run build:prod` previo y evidencia del artefacto |
| F06 | `frontend/src/app/core/performance/performance-baseline.service.ts` — `8855bf98bed1faf0a2e91cc7756d90d01495b48a` | Define captura opcional de TTFB, LCP, INP, CLS, navegación, solicitudes y transferencia; servicio o interfaz no prueba datos reales |
| F07 | `frontend/src/app/core/performance/after-render-selective-preloading.strategy.ts` — `a5083eac7fd882430c9550c037c17f89cb946149` | Preloading sólo si ruta marca flag y URL empieza `/tienda`, tras estabilidad y temporizador; no implica chunk realmente descargado |
| F08 | `frontend/e2e/responsive-global.spec.ts` — `aef296d8d2766f652b1a9f47800e19411867fb07` | Especifica casos de clipping/scroll/controles y viewports; ejecución, artefactos y cobertura siguen NO_VERIFICADOS aquí |
| F09 | `frontend/scripts/validate-http-cache-contract.mjs` — `ddf0beb1a306f25e347a01d55d6b379186cf6b3a` | Contrato de ETag, HTTP 304, TTL de caché pública y exclusión de requests autenticados; no prueba CDN/HTTP live |
| F10 | `frontend/scripts/validate-cloudinary-responsive.mjs` — `7fcea8ed83e6da21fb6fc88b277ddbbbae252e56` | Contrato para `srcset`, `sizes`, carga lazy/async y prioridad de imagen principal; no demuestra CLS o LCP visual |
| F11 | `frontend/playwright.config.ts` — `87946080e7fe87f5fa9e9c8d4c8da8a36f49d5da` | Configuración browser y fixture tenant; ninguna medida E2E atribuible sin un run del SHA |
| F12 | `docs/v5/c13-03-m01-01-alcance-no-regresion-rendimiento-ligereza.md` — `93f4bc4b260fbbf9d93dccdf8fa4b128e2922e80` | Fuente NR01–NR14 y oráculos futuros; no constituye benchmark |
| F13 | `docs/v5/c05-04-m01-05-criterios-aceptacion-ligereza-percepcion.md` — `6b9293d1435ab148a2afdf6b0b2a63455e92b05e` | Criterios sobre percepción, carga y A/B; deben convertirse en tests más adelante |
| F14 | `docs/v5/c12-02-m01-05-criterios-aceptacion-frontend-separado.md` — `0758f037ae4910c73d6617f63d5bafc753acfb8d` | Contrato sobre SPA/proxy/cache, no migración ni despliegue autorizado |

**Discrepancia concreta de código a conciliar en otra microtarea autorizada:** F01/F04 exigen warning/error **650/750 kB** para el budget production initial, mientras F05 incorpora un objeto `existingInitialBudget` con **1 MiB/2 MiB**. No se concluye que el build incumpla 650/750 o que el JSON haya sido generado en esta ejecución. La discrepancia puede inducir una interpretación errónea del reporte histórico; se debe identificar la fuente de verdad y probar la corrección del script en un cambio de código específico, con CI, sin aflojar el presupuesto de Angular ni tratar M01 documental como permiso para editarlo.

## 2. Brechas de evidencia B01–B14

| Brecha | Falta para afirmar un resultado | Riesgo de una conclusión prematura |
| --- | --- | --- |
| B01 — baseline inicial | Artefacto de `npm run build:prod`, SHA, hash de archivos y conteo raw/gzip/brotli | Confundir budget declarativo con bytes reales |
| B02 — reporte de budget | Conciliar 650/750 kB de F01/F04 con 1MiB/2MiB escrito en F05 | Reporte de límites contradictorio |
| B03 — navegaciones | Traces de rutas lazy, chunks, preload y cronología de carga por rol | Confundir configuración con descarga efectiva |
| B04 — Core Web Vitals | Series LCP/INP/CLS, browser/OS, caché/red y ruta, muestras repetidas | Inventar percentiles o una mejora sin baseline |
| B05 — percepción/estados | Observar loading, error, vacío, 401/403 y feedback accesible | Acelerar ocultando fallos |
| B06 — imágenes | Medir CLS, LCP, tamaños, fallback y descargas en viewport real | `srcset` presente sin impacto contrastado |
| B07 — tablas y datasets | Dataset controlado, paginación, filtros, integridad y conteo de llamadas | Tablas rápidas con pérdida de filas |
| B08 — requests HTTP | Capturar duplicados, cancelación/abort, ETag/304, cache-hit y TTL | Side effects y estimaciones ficticias |
| B09 — backend | Latencia end-to-end de API/DB y métricas de errores con fixtures aislados | Atribuir costo del backend al frontend |
| B10 — aislamiento | Dos JWT/roles/empresas sintéticas, requests negativos A/B y caché segregada | Filtración de datos intertenant por optimización |
| B11 — accesibilidad | Teclado/foco, responsive, zoom, motion y lector cuando aplique | Ganar velocidad a costa de usabilidad |
| B12 — reproducibilidad | Mismo SHA, escenario, dispositivo, red, caché, dataset y múltiples rondas | Comparación caliente/fría sesgada |
| B13 — privacidad de trazas | Sanitización y ausencia de tokens, PII, cookies, URLs sensibles | Telemetría con exposición de secretos |
| B14 — CI/certificación | Links al run/job, head_sha, estado, resultado y logs/artefactos pertinentes | SKIPPED/CANCELLED/IN_PROGRESS mal acreditados como PASS |

Estas brechas son **faltantes de evidencia**, no afirmaciones de que el producto falla. F05 detecta una inconsistencia textual comprobada, distinta de una medición fallida o un incumplimiento en producción. Para cerrar cualquier brecha se deberán identificar responsable, alcance, hipótesis, prueba negativa, rollback y evidencia causal.

## 3. Ocho pares de casos positivos y negativos: TODOS FUTUROS_NO_EJECUTADOS

| Par | Futuro caso positivo | Futuro caso negativo que debe reprobar |
| --- | --- | --- |
| P01/N01 | Build inicial medido con stats, SHA y budget 650/750 kB | Reporte utiliza 1MiB/2MiB como si fuera el budget vigente sin reconciliación |
| P02/N02 | Navegación /tienda precarga sólo chunks permitidos tras estabilidad | Preload indiscriminado satura red o carga ruta privada |
| P03/N03 | Rutas lazy mantienen guards, contenido y foco al navegar | Mejora salta guard, pierde estado o falla navegación |
| P04/N04 | Imágenes reservan espacio, dimensiones/srcset y fallback seguro | Imagen sin tamaño causa CLS o URL indebida |
| P05/N05 | Tabla/paginación conserva filas, selección y accesibilidad en móvil | Virtualización pierde registros o control se recorta |
| P06/N06 | ETag/304 y caché pública siguen política con sesión correcta | Cache privada de tenant A se sirve a B |
| P07/N07 | Core Web Vitals comparables por serie, mismo entorno y no una sola muestra | Se declara mejoría comparando redes/dispositivos distintos |
| P08/N08 | Telemetría con datos sintéticos, errores 403 explícitos y artefactos anonimizados | Se exporta JWT/PII o se oculta un 403 para bajar métricas |

No se ejecutaron estos tests, Playwright, Chromium, benchmarks, series LCP/INP/CLS, HTTP real, MySQL ni `npm build` en esta microtarea. Las CI de Scope Lock y CHANGELOG para la publicación de Markdown validan **gobernanza documental**, no rendimiento de la aplicación.

## 4. Decisiones, límites, auditorías y rollback

**Decisiones pendientes:** seleccionar rutas y perfiles de red, fijar dispositivos/viewport y condiciones de cache, número de rondas, percentiles y umbrales con baseline, fixture A/B sintética, artefactos sanitizados, y designar responsable de reconciliar F05 con F01/F04. No alterar presupuesto para fabricar PASS. El documento M01.04 analizará riesgos/decisiones; M01.05 fijará criterios de aceptación; M01.06/M02–M06 sólo siguen sus propios gates.

**Impacto:** sólo esta documentación y sección autonómica de `CHANGELOG_AI.md`. No modifica Angular 22.2.1/Zone.js, ASP.NET Core 10, API/DTO, JWT/RBAC, MySQL, tenant, Cloudinary, R2, workers, cache live, CI, instrumentos, secretos, datos, infraestructura, QA, main ni PROD. Los consumidores existentes permanecen inalterados.

- **A0:** comprobación de Markdown, 14 blobs, B01–B14, ausencia de secretos, 2 archivos modificados y encabezado/viñetas juntos sin procedencia cruzada.
- **A1:** revisión de consistencia documental con M01.01 y contratos C05-04/C12-02; 8 pares P/N definidos y FUTUROS_NO_EJECUTADOS, no resultados runtime.
- **A2:** JWT/RBAC/tenant A/B, no cache cross-tenant, sin PII o tokens en artefactos futuros; no se alteró autorización backend.
- **A3:** inventario Git contrastado, discrepancia F05/F01 sin afirmar fallo de build; `npm`, Playwright, `.NET`, Docker/MySQL son **NO_APLICA por diff Markdown**, no PASS de runtime.
- **A4:** commit fast-forward CAS en `dev`, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA exacto, readback Git/PLAN/COLA/BITACORA.
- **A5:** siempre **PENDIENTE** de otra Task Javier, no autoaprobar ni promover a QA/main/PROD.
- **Rollback:** `git revert` como nuevo commit aditivo tras comprobar HEAD, registrar evento compensatorio; no reescribir Git ni BITACORA.

**Estado del inventario:** fuentes y discrepancia documentadas; rendimiento, mejoras y regresiones runtime **NO_VERIFICADOS**.
