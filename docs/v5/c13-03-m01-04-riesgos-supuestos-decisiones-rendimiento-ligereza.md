# SOLQARYN V5 FINAL — C13-03.M01.04: riesgos, supuestos y decisiones de rendimiento

**ID:** C13-03.M01.04; **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-14. **Repositorio/rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** contrato documental de preparación M01, sin benchmarks ni cambios de runtime.

**Autoridad:** [STANDARD DE IMPLEMENTACIÓN V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). PLAN fila 1949, DAG fila 1910 F=VALIDADO/G=SI al inicio de la reclamación. Este gate autoriza **esta preparación**, no presupuestos nuevos, CDN, API, cache, migraciones ni QA/main/PROD.

## 1. Fuentes, trazabilidad y alcance comprobable

El contrato M01.01 (blob `93f4bc4b260fbbf9d93dccdf8fa4b128e2922e80`) define 14 invariantes, 4 P/12 N FUTUROS_NO_EJECUTADOS y baseline comparable. M01.02 (blob `2228ee9f70edf057b4ec52210f7e5be17db699da`) clasifica 12 actores, 12 entradas, 10 salidas y 8 fronteras. M01.03 estaba EN_PROGRESO bajo otra Task Javier al iniciar esta unidad y **no se considera fuente terminada ni gate satisfecho por inferencia**. Este documento es una preparación independiente validada en DAG; cualquier nuevo hallazgo de M01.03 se incorporará mediante reconciliación posterior y sin sobrescribir su autoría.

Las lecturas de `frontend/angular.json` (blob `8b00f8fee80fdfafe1864e233be7d5f20ad444e3`), `frontend/package.json` (`e6d030f643421cb343cc12617e742469e88223c4`), `frontend/src/app/app.routes.ts` (`2f560c14693455b79ac4546e9333fb74586ef234`) y `frontend/playwright.config.ts` (`87946080e7fe87f5fa9e9c8d4c8da8a36f49d5da`) demuestran sólo configuración/código fuente. El budget inicial Angular warning 650 kB/error 750 kB y anyComponentStyle 16/32 kB **no son bytes emitidos ni evidencia de performance alcanzada**.

Los riesgos R01–R14 son posibilidades por comprobar, no defectos probados; los supuestos S01–S10 NO_VERIFICADOS y las decisiones D01–D10 ABIERTAS no crean autorizaciones ni obligan a cambiar stack. No se ejecutó un browser, medición LCP/INP/CLS, profiling .NET/MySQL ni prueba de red/tenant en esta tarea. Los P/N de este documento son FUTUROS_NO_EJECUTADOS.

## 2. Registro de riesgos potenciales R01–R14

| Riesgo | Posible impacto en negocio/UX/seguridad | Mitigación requerida y evidencia futura |
| --- | --- | --- |
| R01 Baseline inválido | Medir distinto dispositivo/red/datos produce falsa mejora | Captura SHA, ruta, browser, red, CPU, caché y serie repetida |
| R02 Budget maquillado | Aumentar límite de 650/750kB oculta regresión JS | Comparar bytes emitidos contra umbral sin modificarlo por conveniencia |
| R03 Preload excesivo | Chunks grandes bloquean carga o exponen vistas indebidas | Waterfall por ruta y guard backend de cada recurso |
| R04 Llamadas duplicadas | Rerender, navegación y filtros repiten API y mutaciones | Conteo requests, cancelaciones, dedupe, idempotencia adecuada |
| R05 Cache sin tenant | Respuesta A podría servirse a B | Claves/segmentación tenant, actor/rol/recurso y pruebas A/B |
| R06 JWT/RBAC omitido | Optimización frontend intenta saltarse permiso | API verifica JWT, membresía y pertenencia; pruebas denegatorias |
| R07 Virtualización incorrecta | Paginación pierde filas, edición o selección | Fixture sintético con totales y aserción de integridad |
| R08 Imágenes mal dimensionadas | CLS o carga excesiva, URL sensible expuesta | Width/height, formatos, fallback, políticas de URL |
| R09 Estados ocultos | Loading/empty/error/403 invisibles simulan rapidez | Mensaje textual, retry, estado accesible y prueba negativa |
| R10 Reflow/foco degradado | Interacción móvil y teclado se rompe por optimización | Matriz 320/390/768/1280, zoom y flujo foco |
| R11 Reduced-motion ignorado | Skeleton/transición causan barrera accesibilidad | Prueba preferencias y legibilidad sin efectos |
| R12 Telemetría excesiva | Traces/RUM pueden recoger JWT, correo o PII de B | Sanitizar y limitar captura, retención, acceso y exportación |
| R13 Pérdida de causalidad | Un run caliente o cancelado se reporta éxito | Ejecutar series exact-SHA, versión runner y checks requeridos SUCCESS |
| R14 Despliegue por documento | M01 se interpreta como certificación runtime o cambio proveedor | A0–A5, Scope Lock, autorización específica por entorno; fail-closed |

La priorización atiende **seguridad > exactitud de datos > accesibilidad > velocidad percibida** cuando hay conflicto; un throughput mejor no justifica exposición de tenant o silenciar errores.

## 3. Supuestos NO_VERIFICADOS S01–S10 y decisiones ABIERTAS D01–D10

| Par | Supuesto a verificar | Decisión futura con condición y responsable |
| --- | --- | --- |
| S01 / D01 | Disponibilidad de rutas representativas | Elegir journeys anónimo/storefront/backoffice y responsables de la muestra |
| S02 / D02 | Entornos de prueba estables y equivalentes | Fijar red, CPU, device, browser, warm/cold y número de repeticiones |
| S03 / D03 | Métricas aplicables a cada tipo de ruta | Seleccionar LCP, INP, CLS/percentiles por caso sin inventar objetivo |
| S04 / D04 | Fixtures sintéticos reproducen tablas y filtros | Definir tamaño y complejidad de dataset, paginación y aserciones |
| S05 / D05 | Lazy routes mantienen límites de autorización | Aprobar política de preloading/chunk y pruebas negative route guard/API |
| S06 / D06 | Recursos media admiten dimensión y fallback | Elegir estrategia de imágenes y versión de proveedor sin cutover |
| S07 / D07 | Cache está estrictamente particionada por empresa/rol | Diseñar claves, TTL, invalidación y tests tenant A/B antes de activar |
| S08 / D08 | Observabilidad puede evitar secretos y PII | Aprobar esquema redactado de trace, retención, acceso y samples |
| S09 / D09 | UX accesible se conserva tras optimización | Elegir suite keyboard/zoom/aria/reduced-motion y criterios de fracaso |
| S10 / D10 | CI/rollback estarán disponibles para cambios de runtime | Definir workflows exact-SHA, baseline posterior y rollback probado |

Toda decisión futura necesita propietario autorizado, hipótesis, evidencia, SHA, fecha, criterios de aceptación/rechazo y costo/riesgo. Ninguna decisión anterior tiene implementación concedida por esta microtarea.

## 4. Oráculos P/N — FUTUROS_NO_EJECUTADOS

| Caso | Positivo verificable futuro | Negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | Misma ruta y condiciones muestran serie antes/después comparable | Mezclar red/browser/cache y declarar éxito |
| P02/N02 | Build real respeta budgets sin ocultar bytes | Aumentar budget arbitrariamente |
| P03/N03 | Lazy loading conserva navegación y permisos | Prefetch de ruta privada filtra datos |
| P04/N04 | Tabla filtra/pagina sin cambiar totales o perder datos | Virtualización descarta filas/ediciones |
| P05/N05 | Cache propia del tenant A nunca sirve a B | Reusar respuesta A con JWT/tenant B |
| P06/N06 | API verifica JWT/RBAC/membresía en toda ruta | Optimización omite autorización |
| P07/N07 | Error/403 y foco accesibles bajo carga | Ocultar error o spinner para medir menos |
| P08/N08 | Traces reproducibles sin PII y rollback causal | Filtrar token/PII o contar CI SKIPPED como PASS |

Todos estos casos están pendientes; un PASS necesitará commit y workflow, browser/OS/red/device, fixtures A/B, captura/trace sanitizada, resultados positivos y negativos, varias muestras y rollback. El check de Scope Lock en un documento no certifica rendimiento.

## 5. Impacto, validación y reversibilidad

- **Frontend:** Angular 22.2.1, Signals, Material/CDK, Zone.js, lazy routes; no se modifican `angular.json`, `package.json`, lazy routes, SCSS, tests ni budgets.
- **Backend y datos:** net10.0, JWT/RBAC, Domain/Application/Infrastructure, EF Core/MySQL; no DTO, endpoint, migración, query, transacción, registros ni caché nueva.
- **Seguridad:** mantener aislamiento tenant A/B, auditoría, permisos y PII. Optimización no concede permiso ni permite cookies/tokens en logs.
- **Integraciones:** no CDN/R2, Cloudflare, worker, webhook, instrumentación, compra, secretos, cambios DNS ni despliegues.
- **A0:** diff de documento y sección autocontenida CHANGELOG, R01–R14/S01–S10/D01–D10/P01–N08, sin `##` vacíos ni secretos.
- **A1 documental:** matriz rastreable a NR01–NR14 del M01.01 y actores/entradas/salidas del M01.02, sin atribuir pruebas runtime ejecutadas.
- **A2 documental:** barreras JWT/RBAC/tenant A/B, PII, cache y negativos; sin cambiar seguridad real.
- **A3 documental:** comparar configuración Angular, rutas y Playwright; la M01.03 en curso no se declara terminada, ni build/NET10/MySQL PASS.
- **A4:** publicar fast-forward CAS en `dev`, comprobar diff y `verify-solqaryn-project-scope` + `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success sobre SHA exacto; readback Git/PLAN/COLA/BITACORA.
- **A5:** independiente por otra Task Javier (no autorrevisión), pendiente antes de promoción; no habilita QA/main/PROD.
- **Rollback:** git revert normal sobre HEAD revalidado y evento compensatorio BITACORA append-only. Sin cambios runtime o datos que revertir.

**Resultado atómico:** identificación documentada y falsable de riesgos/supuestos/decisiones para rendimiento, sin benchmarks inventados ni alteración de la plataforma.
