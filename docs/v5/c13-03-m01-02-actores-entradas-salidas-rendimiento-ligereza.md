# SOLQARYN V5 FINAL — C13-03.M01.02: actores, entradas, salidas y fronteras de rendimiento

**Microtarea:** C13-03.M01.02 — identificar actores, entradas, salidas y superficies afectadas. **Autor:** PRIMARY DEV 05 JAVIER; TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-14. **Entorno:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** análisis documental de M01; no implementación, benchmark ni certificación.

**Fuente institucional:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). PLAN fila 1947, DAG fila 1908 F=VALIDADO/G=SI al preflight; gate sólo de preparación documental y no autorización para caché compartida, backend, cambios de proveedor, QA/main/PROD.

## 1. Alcance, evidencia leída y límites

La microtarea anterior C13-03.M01.01 (documento blob `93f4bc4b260fbbf9d93dccdf8fa4b128e2922e80`) establece NR01–NR14, P01–P04, N01–N12 FUTUROS_NO_EJECUTADOS. Los contratos C05-04 y C12-02 de UX ligera/SPA no representan un benchmark. Se inspeccionaron las fuentes reales `frontend/angular.json` (blob `8b00f8fee80fdfafe1864e233be7d5f20ad444e3`), `frontend/src/app/app.routes.ts` (`2f560c14693455b79ac4546e9333fb74586ef234`), `frontend/package.json` (`e6d030f643421cb343cc12617e742469e88223c4`) y `frontend/playwright.config.ts` (`87946080e7fe87f5fa9e9c8d4c8da8a36f49d5da`). La configuración de Angular establece presupuesto inicial de advertencia 650 kB/error 750 kB y por estilo 16/32 kB: **son umbrales definidos, NO tamaños medidos ni pruebas PASS**.

La observación es estática. Se identifican superficies para futuras mediciones sin afirmar p95, LCP/INP/CLS, métricas de RUM, accesibilidad certificada, performance real de MySQL/Render/Vercel, pérdidas de tenancy ni resultados de browser. No se modificó ningún código Angular/NET10, scripts, migraciones, JWT/RBAC, índices, redes, secrets, providers o datos.

## 2. Actores A01–A12 y su interés observable

| Actor | Uso o interés en el rendimiento | Límite de su autoridad |
| --- | --- | --- |
| A01 Visitante anónimo | Navegación pública de catálogo y carga inicial | Ningún acceso a recursos privados |
| A02 Cliente autenticado de tienda | Carrito, cuenta, pedidos y transiciones | Sólo datos de su cuenta y tienda autorizada |
| A03 Operador de empresa | Dashboard y flujos de backoffice | JWT/RBAC y pertenencia verificados en backend |
| A04 Encargado de inventario | Tablas grandes, filtros y transacciones | Integridad de stock prevalece sobre rapidez |
| A05 Gestor financiero | Reportes, listados y exportaciones | Prohibido cachear datos cruzando empresas |
| A06 Administrador de sucursal | Cambios de contexto y permisos por sucursal | La selección UI no concede autorización |
| A07 Administrador de plataforma | Cobertura y rutas de configuración | Privilegios adicionales comprobados en API |
| A08 Usuario móvil con red limitada | Carga, reflow, navegación y errores recuperables | No eliminar controles ni contenido esencial |
| A09 Usuario sólo teclado/lector | Foco, semántica y estados accesibles | No sacrificar a11y por menor tiempo de carga |
| A10 QA ejecutor autorizado | Medir escenarios reproducibles con fixtures | Artefactos sin PII y sin QA/main/PROD automáticos |
| A11 Ingeniero frontend/API | Identificar bundles, consultas, caché y hot paths | Cambios de runtime sólo bajo scope y CI causal |
| A12 Operaciones/observabilidad | Diagnóstico de regresiones por versión y tenant | No recopilar tokens, secretos ni emails en trazas |

Un actor puede ocupar varios roles, pero **la autorización de backend no deriva del tipo de actor, de un route guard o de esta matriz**.

## 3. Entradas I01–I12 y superficies afectadas

| Entrada | Superficie concreta y datos necesarios para medir |
| --- | --- |
| I01 URL/ruta y cold start | Login, catálogo público, dashboard; URL normalizada, estado de caché |
| I02 Chunk lazy y preload | Árbol `app.routes.ts`, secuencia de imports y waterfall JS |
| I03 Viewport/zoom/OS | 320/390/768/1280, DPR, browser, teclado y reduced-motion |
| I04 Perfil de red/dispositivo | CPU, ancho de banda, RTT, memoria y condiciones de prueba |
| I05 Tenant y membership | Token sintético, rol, sucursal y contexto A/B antes de peticiones |
| I06 Consulta HTTP | Endpoint/shape autorizado, número de requests, status, latencia |
| I07 Filtro/paginación/tabla | Número de filas, tamaño de dataset, página, orden y selección |
| I08 Imágenes/medios | Tamaño, formato, dimensiones, fuente, compresión y placeholder |
| I09 Estado UI | Loading, vacío, error, 403, reintento, foco y lectura accesible |
| I10 Estado de cache | Frío/caliente, TTL, invalidez, clave tenant/rol/recurso/versión |
| I11 Transición de navegación | Disparador del usuario, prefetch y cancelación de request |
| I12 Observación reproducible | SHA, workflow, suite, trace sanitizada y repeticiones |

**Riesgo de medición:** mezclar caché fría/caliente, datos distintos, red diferente o tenant diferente invalida una comparación; registrar el contexto antes de calcular mejora.

## 4. Salidas O01–O10 exigibles para implementación futura

| Salida | Evidencia requerida, no ejecutada aquí |
| --- | --- |
| O01 Tiempo de carga por ruta | Serie de muestras y distribución/percentiles con SHA y condiciones |
| O02 Core Web Vitals aplicables | LCP, INP, CLS cuando procedan, con instrumento y referencia |
| O03 Presupuesto de bundle | Bytes efectivos emitidos y comparación contra budget declarado |
| O04 Navegación inter-rutas | Timeline, requests duplicados, chunks y rollback a la ruta previa |
| O05 Integridad tabla/form | Recuento total, selección, ediciones y ausencia de pérdida |
| O06 Recursos/medios | Transfer size, dimensiones reservadas, fallbacks y URL autorizada |
| O07 Seguridad A/B y caché | Negativo de acceso cruzado, cache aislada y logs sin JWT/PII |
| O08 Accesibilidad/percepción | Foco, reflow, contenido útil y estados narrados, no ocultos |
| O09 Informe de regresión | Antes/después con variación, método, prioridad y falsaciones |
| O10 Evidencia de rollback | Reversión identificable por SHA y prueba de comportamiento |

Ninguna salida se reporta como resultado conseguido; constituyen contratos para M03–M05 o fases posteriores autorizadas.

## 5. Fronteras F01–F08 y controles fail-closed

| Frontera | Riesgo de confusión | Evidencia/control requerido en la fase ejecutable |
| --- | --- | --- |
| F01 Browser ↔ Angular router | Lazy preload revela rutas no autorizadas | Guard UI sólo UX, autorización server-side obligatoria |
| F02 Angular ↔ API .NET 10 | Duplicidad, retry o cache de respuesta ajena | Contexto de tenant/JWT/RBAC backend y error denegado A/B |
| F03 API ↔ Application/Domain | Optimización elimina validación de invariantes | Tests de negocio positivos/negativos y propiedad |
| F04 Infrastructure ↔ MySQL | Query rápida pero estado transaccional incorrecto | Plan de consultas, exactitud, aislamiento y rollback |
| F05 Tenant A ↔ Tenant B | Caché compartida filtra pedidos/reportes | Key por tenant/actor/rol según caso e invalidación autorizada |
| F06 App ↔ Cloudinary/media | Imagen sin reserva causa CLS o URL sensible | FallBack, dimensiones, URL permitida y tokens fuera de trazas |
| F07 UI ↔ servicios de medición | RUM/logs exportan PII, URL sensible o token | Redacción, mínima retención, permisos y aprobación de proveedor |
| F08 Desarrollo ↔ QA/main/PROD | Un benchmark local se usa para desplegar | CI exact SHA + permisos diferenciados, ningún gate inventado |

## 6. Oráculos P/N futuros para validar las interfaces (NO EJECUTADOS)

| Caso | Positivo futuro | Negativo futuro que debe fallar |
| --- | --- | --- |
| P01/N01 | Carga de catálogo frío/cálido comparable por SHA/red | Comparar redes distintas y declarar mejora |
| P02/N02 | Ruta lazy carga sólo lo necesario y conserva guards | Preload indiscriminado expone datos o bloquea navegación |
| P03/N03 | Tabla grande filtra/pagina preservando todas las filas | Virtualización pierde selección o ediciones |
| P04/N04 | Imagen progresiva reserva espacio y usa fallback | CLS/imagen rota o URL privada filtrada |
| P05/N05 | Tenant A nunca obtiene respuesta/cache de B | Una clave de caché omite tenant y filtra B |
| P06/N06 | HTTP error/403/load se muestra con texto accesible | Spinner/error oculto para fingir ligereza |
| P07/N07 | Trace sanitizada y repetible conserva método | Se almacena JWT/PII o se elimina contexto causal |
| P08/N08 | Budget Angular se cumple con build real | Ampliar budget sin justificación para esconder bytes |

**Todos FUTUROS_NO_EJECUTADOS.** Un PASS exigirá datos sintéticos, commit SHA, ejecución CI, browser/versión, device/red/cache, tiempos, artefactos, repetición, negativos y rollback. Las pruebas de UI no reemplazan pruebas HTTP/seguridad.

## 7. Impacto, A0–A5, decisiones y reversibilidad

- **Capa principal:** documentación de rendimiento percibido/frontend. **Consumidores:** equipos de Angular, API, pruebas, seguridad, accesibilidad y operación; no se cambia ninguna interfaz en código.
- **Arquitectura:** Angular 22.2.1 standalone/Signals, Zone.js vigente, ASP.NET Core net10.0, Domain/Application/Infrastructure, EF Core/MySQL, JWT/RBAC y tenancy sin cambios.
- **Datos/integración:** no hay migraciones, tablas, endpoints, CDN, cache nueva, scripts, instrumentación, worker, eventos ni secretos; los futuros cambios requieren su propio alcance y permisos.
- **Decisiones ABIERTAS:** rutas representativas, matriz red/device, tamaño de dataset sintético, percentiles/umbrales, política de cache por tenant y tratamiento de archivos/medios. No fijar SLA o percentil sin baseline.
- **A0:** validar diff dos Markdown, A12/I12/O10/F08/P8-N8, encabezados y viñetas CHANGELOG contiguos, ausencia de secretos/HTML peligroso.
- **A1 documental:** matrices de entradas/salidas coherentes con NR01–14 M01.01 y 8 P/N FUTUROS_NO_EJECUTADOS; no simular benchmark.
- **A2 documental:** respetar JWT/RBAC/tenant A/B, PII, orden de autorización y sanitización; tests ejecutables NO_APLICA al diff Markdown, no PASS.
- **A3 documental:** cotejar `angular.json`, rutas, scripts, Playwright y documento de alcance; no atribuir build/Playwright/MySQL al documento.
- **A4:** commit en dev fast-forward/CAS, Scope Lock y V5 provenance completed/success en SHA exacto, readback PLAN/COLA/BITACORA.
- **A5:** PENDIENTE de otra Task Javier distinta de autora; no certificar KPI, UI/HTTP ni QA/main/PROD.
- **Rollback:** git revert aditivo del changeset tras releer HEAD; registrar evento compensatorio append-only, conservar historial. No revertir datos porque no se modifican.

**Resultado:** interfaces y actores de rendimiento identificados con límites auditable, no aumento demostrado de velocidad ni nueva autorización.
