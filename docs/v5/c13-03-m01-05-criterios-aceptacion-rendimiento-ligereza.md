# SOLQARYN V5 FINAL — C13-03.M01.05: criterios de aceptación de rendimiento y ligereza

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 04 JAVIER, Task `6aeacb7820ac8191b7d768a6f33dfa09`. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Clasificación:** contrato técnico M01, no optimización implementada ni benchmark ejecutado.

**Autoridad única:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El requisito de `C13-03.M01.05` es definir aceptación funcional y técnica **observable** para carga inicial, navegación, imágenes, tablas, datos y móviles. **No** acredita mejora, p95, LCP/INP/CLS, certificado de accesibilidad ni resultado de red/DB. La elegibilidad `DAG F=VALIDADO/G=SI` sólo habilita preparación M01 y A0–A4 correspondientes.

## 1. Fuentes y relaciones

- **C13-03.M01.01**, blob `93f4bc4b260fbbf9d93dccdf8fa4b128e2922e80`: NR01–NR14 y escenarios P01–04/N01–12 **FUTUROS_NO_EJECUTADOS**. Ningún umbral de Core Web Vitals se inventa.
- **C13-03.M01.02**, blob `2228ee9f70edf057b4ec52210f7e5be17db699da`: 12 actores (A), 12 entradas (I), 10 salidas (O), ocho fronteras F01–F08 de datos/seguridad.
- **C13-03.M01.03**, blob `18e3546462ab0e1b83d6f10f22e21066cb4e687f`: 14 blobs de fuente, brechas B01–B14 y ocho pares P/N. Identifica inconsistencia verificable entre budget production initial `650kb/750kb` de `frontend/angular.json` + validador y objeto `existingInitialBudget=1MiB/2MiB` en `frontend/scripts/performance-bundle-baseline.mjs`. Corregir herramienta requiere **microtarea de código autorizada con pruebas**, no modificarla en esta M01.
- **C13-03.M01.04**, blob `d278023ea2194ec00358563535dd7b5b3f53ad91`: R01–R14 riesgos **potenciales**, S01–S10 **NO_VERIFICADOS** y D01–D10 **ABIERTAS**; ningún riesgo supuesto equivale a incidencia runtime comprobada.
- **Código actual:** `frontend/angular.json` blob `8b00f8fee80fdfafe1864e233be7d5f20ad444e3`, `frontend/package.json` blob `e6d030f643421cb343cc12617e742469e88223c4`, `frontend/scripts/validate-angular-bundle-policy.mjs` blob `8081db397b5cdc5d3799f858faaa2ed7d52761f0`, `frontend/e2e/responsive-global.spec.ts` blob `aef296d8d2766f652b1a9f47800e19411867fb07`, `frontend/scripts/validate-http-cache-contract.mjs` blob `ddf0beb1a306f25e347a01d55d6b379186cf6b3a`.

Los blobs se leen como **fuentes estáticas**, no se confunden con una compilación, un reporte web-vitals, CI emitida o pruebas ejecutadas.

## 2. Requisitos generales de un PASS futuro

Una comparación reproducible debe fijar antes del cambio la ruta funcional, identidad sintética/tenant, dataset, navegador/versión, viewport, dispositivo/CPU, red/RTT, estado cold/warm de caché, método de medición, número de muestras y SHA de código. Repetir luego en condiciones equivalentes y publicar mediana/percentiles y dispersión por ruta. Los umbrales cuantitativos de latencia, LCP/INP/CLS, error rate, bundle y disponibilidad se acuerdan con baselines reproducibles y responsables D01–D10; se rechaza inventarlos a partir del plan.

Los **budgets declarados** 650kb warning / 750kb error inicial, 16/32kb de estilos, son configuración de Angular, **no mediciones del bundle**. La discrepancia F05 detectada en M01.03 impide usar el campo `existingInitialBudget` de dicho script como umbral autoritativo hasta reconciliación causal. No aumentar presupuestos para ocultar regresiones. Un check documental de Scope Lock NO valida runtime.

## 3. Matriz de aceptación positiva y negativa P01–P16/N01–N16 (TODOS FUTUROS_NO_EJECUTADOS)

| Caso | Positivo futuro verificable | Negativo que debe fallar | Artefacto de demostración esperado |
| --- | --- | --- | --- |
| P01/N01 | Build productivo emite bundles medidos con `stats` y cumple presupuesto inicial aprobado | Se usa 1MiB/2MiB como si fuera 650/750kb o se eleva el budget sin autorización | Reporte bytes raw/gzip/brotli, SHA, config, CI build |
| P02/N02 | Ruta pública inicial carga contenido principal sin JS de módulos privados | Se precarga módulo pesado/privado al cold start | Waterfall navegador y manifiesto chunks |
| P03/N03 | Navegación lazy conserva guardas y operación tras volver | Prefetch indiscriminado rompe ruta o acceso | Trace router, guard/API negatives y chunks |
| P04/N04 | Estrategia `preloadAfterRender` en `/tienda` actúa después de estabilidad | Preload ocurre antes de estabilidad o en ruta no permitida | Traza de network, app stable y URL |
| P05/N05 | Solicitud HTTP repetida por ruta/filtro conserva semántica sin duplicaciones accidentales | Rerender activa doble POST o refetch redundante | Conteo request y acciones side-effect con dataset sintético |
| P06/N06 | ETag/If-None-Match resuelve 304 en contenido público elegible | Respuesta autenticada privada ingresa en caché pública | HTTP headers, fixture A/B y caché |
| P07/N07 | Cambio tenant/rol limpia caché y recarga sólo recursos autorizados | Usuario B recibe respuesta/cache de empresa A | Test HTTP autenticado tenant A/B y permisos |
| P08/N08 | Tabla grande pagina/filtra sin perder total, selección o filas | Virtualización omite registros o ediciones | Fixture reproducible, totales antes/después |
| P09/N09 | Imágenes responsivas aplican `srcset/sizes`, reservan espacio y fallback | Imagen causa CLS alto o revela URL privada | Trace recurso/captura y métricas de layout |
| P10/N10 | Loading/vacío/error/403/500 son visibles, accesibles y recuperables | Se oculta error para aparentar mejor velocidad | Test UI+API, foco, ARIA y estado |
| P11/N11 | Scroll, zoom y teclado funcionan a 320/390/768/1280px representativos | Mobile oculta control crítico o genera focus trap | Capturas, árbol a11y, tabulación, zoom |
| P12/N12 | `prefers-reduced-motion` elimina transiciones no esenciales sin quitar significado | Optimización introduce animación imprescindible para comprender | Estado OS/browser, prueba a11y |
| P13/N13 | LCP/INP/CLS se miden en series reproducibles antes/después | Se comparan una muestra caliente con una fría o redes distintas | Series, distribución y condiciones por SHA |
| P14/N14 | Endpoint API o consulta MySQL optimizados preservan exactitud e integridad | Query más rápida ignora tenant, permisos o transacción | Tests API/.NET con MySQL efímero y casos A/B cuando aplique |
| P15/N15 | Observabilidad evita PII, JWT, cookies, rutas sensibles y secretos | Trace reporta token o datos de otro tenant | Escáner/redacción de artefactos |
| P16/N16 | Un cierre presenta CI causal exact-SHA y rollback probado | Job SKIPPED/CANCELLED/QUEUED/IN_PROGRESS se marca PASS | URL de GitHub Actions, status, conclusion, head_sha, rollback |

**Estado:** los 32 casos anteriores son **oráculos FUTUROS_NO_EJECUTADOS**, no tests ejecutados en esta microtarea. Los tests funcionales/seguridad sólo se exigen al code scope relevante; la mera presencia de un `.spec.ts` o script no demuestra PASS. Requieren positivo, negativo, datos sintéticos, evidencia causal y rechazo determinista del incumplimiento.

## 4. Criterios no funcionales NF01–NF12

| Requisito | Evidencia requerida antes de aceptar implementación futura |
| --- | --- |
| NF01 — comparabilidad | Dispositivo/red/cache/rol/dataset idénticos y múltiples muestras |
| NF02 — budget | Build real, budget de Angular y tamaño inicial/chunks con procedencia |
| NF03 — interacción | Navegación, foco y contenido útil sin guard omitido |
| NF04 — integridad | Tablas, filtros, paginación y transacciones sin pérdida de datos |
| NF05 — tenancy | A/B fail-closed en .NET con JWT/RBAC/propiedad/sucursal |
| NF06 — privacidad | Logs/profiling sin JWT/PII/tokens ni URL privadas |
| NF07 — medios | Tamaño/CLS, bytes transferidos y fallback por dispositivo |
| NF08 — caché | ETag, TTL, variación por usuario/tenant cuando aplique, invalidación |
| NF09 — a11y | Teclado, zoom, reflow, foco, contraste/motion según superficie |
| NF10 — observabilidad | Run ID, SHA, herramienta, series sanitizadas y errores reales visibles |
| NF11 — rollback | Desactivar/revertir sin modificar datos productivos ni silenciar fallos |
| NF12 — autorización | Ningún M01 ni A5 documental libera QA/main/PROD, providers, DNS o migraciones |

## 5. Plan de pruebas, impacto, A0–A5 y rollback

**Siguiente fase ejecutable futura:** seleccionar journeys representativos (landing/tienda/login/backoffice/tablas), construir baseline con entorno y dataset autorizados, ejecutar mínimo positivos+negativos, documentar desviaciones y retestar después del cambio de código real. Para API/DB/Migrations deben utilizarse .NET 10, MySQL/EF y CI efímera cuando el scope toque esas capas; para Angular, build/npm/Playwright. Seguridad exige HTTP A/B auténtico dentro del entorno de pruebas. Un scanner o un mock no concede permisos.

**Impacto ahora:** sólo nuevo Markdown `docs/v5` y sección autocontenida de `CHANGELOG_AI.md`. No cambian frontend Angular 22.2.1/Zone.js 0.16.3, backend net10.0, DTO, ORM/MySQL, Cloudinary, workers, R2, APIs, JS, CSS, configuración, CI, JWT/RBAC, tenancy, secretos, QA, main o PROD.

- **A0:** revisar diff de dos Markdown, 16 P/N, 12 NF, encabezados con viñetas contiguas y sin secretos.
- **A1:** cotejar criterios con NR01–NR14, A/I/O/F de M01.02, B01–B14 de M01.03 y R/S/D de M01.04. Es aceptación documental, NO ejecución.
- **A2:** nunca omitir JWT/RBAC/tenant A/B ni sanitización de PII por optimizar, negar caché cross-tenant.
- **A3:** verificar contrato con blobs de budget/script/caché/responsive. `dotnet`, `npm build`, Playwright, MySQL y profiling son **NO_APLICA al diff sólo Markdown**, no PASS de código.
- **A4:** publicar por CAS fast-forward en `dev`, exigir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` sobre SHA exacto, readback PLAN/COLA y BITACORA A:Z.
- **A5:** PENDIENTE de revisión independiente por otra Task Javier distinta de autora, sin autoaprobación. No habilita QA, main, PROD ni cambios de infraestructura.
- **Rollback:** `git revert` no destructivo del commit Markdown sobre HEAD revalidado, evento nuevo de compensación en BITACORA, sin borrar historia ni datos.

**Resultado M01.05:** criterios trazables y falsables; mejora de rendimiento, mediciones, conformidad a11y, despliegues y runtime **NO_VERIFICADOS**.
