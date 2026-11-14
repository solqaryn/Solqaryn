# SOLQARYN V5 FINAL — C13-03.M01.01: alcance y no-regresión de rendimiento y ligereza

**ID:** C13-03.M01.01. **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14. **Naturaleza:** contrato documental M01 de objetivos y límites; no implementación ni benchmark.

**Autoridades únicas:** [STANDARD DE IMPLEMENTACIÓN V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). PLAN fila 1946, DAG fila 1907: gate VALIDADO/SI para preparación documental. **No certifica** mejora real, bundle emitido, Core Web Vitals ni desempeño de proveedores live.

## 1. Objetivo falsable y resultado exigible

Diseñar una vía verificable para mejorar carga inicial, navegación, hidratación/interactividad, imágenes, listas/tablas y lectura de datos en escritorio y móvil. Para cada superficie representativa se deberá fijar baseline reproducible antes de optimizar y comparar después con el **mismo SHA de producto, datos autorizados, dispositivo, red, caché, perfil de acceso y escenario**. La mejora de percepción no puede depender de ocultar errores, permisos, progreso o contenido esencial.

**Resultado de esta M01.01:** objetivo, no-alcance, límites de confianza, requisitos de observabilidad y matriz de no-regresión con oráculos futuros. Las fases M01.02–05 registrarán actores, inventario, riesgos, decisiones y aceptación; el gate M01.06 y cualquier trabajo ejecutable M02–M06 requieren dependencia, claim CAS, CI dirigida y A0–A5 aplicables.

**Fuera de alcance ahora:** cambiar `angular.json`, budgets, lazy routes, Angular components, SCSS, HTTP, backend .NET, MySQL, queries, DTO, provider, CDN, caching, prerender, worker, pipeline, JWT/RBAC/tenant, CI, QA/main/PROD o datos reales. Tampoco se establecen valores objetivo de LCP, INP, CLS o percentiles de API sin baseline y decisión documentada.

## 2. Código y contratos rastreados (lectura estática, NO benchmark)

| Fuente real | Blob Git | Acredita / no acredita |
| --- | --- | --- |
| `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | Configuración budget inicial warning 650kB/error 750kB; no tamaño de build real |
| `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Scripts build, lint, responsive; no resultados de ejecución |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Lazy `loadComponent` y rutas, no timing ni cobertura universal |
| `frontend/scripts/validate-angular-bundle-policy.mjs` | `8081db397b5cdc5d3799f858faaa2ed7d52761f0` | Valida política declarada de budgets/preload; no sustituye build medido |
| `frontend/e2e/responsive-global.spec.ts` | `aef296d8d2766f652b1a9f47800e19411867fb07` | Casos de regresión responsive versionados, no su ejecución aquí |
| `docs/v5/c05-04-m01-01-objetivo-no-alcance-ligereza-percepcion.md` | `a9b7219c7094b8b162f53117a2c67d2c86dd7c21` | NR01–NR12 para ligereza percibida, documento no benchmark |
| `docs/v5/c05-04-m01-05-criterios-aceptacion-ligereza-percepcion.md` | `6b9293d1435ab148a2afdf6b0b2a63455e92b05e` | AC01–14 y N01–14 futuros, no PASS de rendimiento |
| `docs/v5/c12-02-m01-05-criterios-aceptacion-frontend-separado.md` | `0758f037ae4910c73d6617f63d5bafc753acfb8d` | Contratos SPA, performance, frontend separado futuro; no cutover autorizado |

El presupuesto inicial de 650/750 kB es una **restricción de configuración observada**, nunca una medición real ni autorización para aflojarlo. Cualquier discrepancia entre el blob y un HEAD futuro obliga a revalidar. No se midieron LCP, INP, CLS, API, base de datos, tráfico ni dispositivo real durante esta tarea.

## 3. Invariantes de no-regresión NR01–NR14

| ID | Contrato a preservar | Bloqueo ante incumplimiento |
| --- | --- | --- |
| NR01 | Baseline real identificado por ruta, SHA, navegador, dispositivo, red y caché | Métrica sin procedencia no es evidencia |
| NR02 | Mantener budgets de Angular hasta cambio autorizado con comparación de bytes | Subir umbral para esconder degradación |
| NR03 | Diferir código no crítico sin retrasar contenido principal, foco o interacción | Lazy/preload mal diseñado rompe flujo |
| NR04 | No duplicar consultas HTTP al navegar, filtrar o refrescar | Requests redundantes o race sin cancelación |
| NR05 | Cache incluye tenant, actor, rol y versión cuando proceda | Respuesta A aparece a B |
| NR06 | API .NET valida JWT, membresía, RBAC y propiedad aunque la UI cachee | Guard frontend concede recursos ajenos |
| NR07 | Paginación, virtualización y filtros no eliminan filas o cambios del usuario | Tabla rápida pero inconsistente |
| NR08 | Imágenes y medios se cargan con tamaño/caché explícitos y fallback seguro | Broken images, layout shift o URL indebida |
| NR09 | Loading, vacío, error y 403 siguen visibles y accesibles | Optimización oculta estado de error |
| NR10 | Responsive 320px, zoom, teclado y foco permanecen utilizables | Clipping, scroll doble o focus trap |
| NR11 | Accesibilidad y motion preferences no se sacrifican por velocidad | Animaciones o skeleton inaccesibles |
| NR12 | Traces, profiling y RUM no capturan JWT, cookies, secretos ni PII intertenant | Telemetría filtra datos privados |
| NR13 | Medición de mejora compara mismo flujo antes/después y múltiples muestras | Un único run caliente cuenta como éxito |
| NR14 | M01 no activa CDN, proveedor, migración, worker, evento, QA/main/PROD ni certifica runtime | Documento interpretado como autorización |

En caso de conflicto entre ahorro de bytes y seguridad o funcionalidad, prevalecen control de acceso, exactitud de datos y accesibilidad. Las variaciones por proveedor externo se registrarán, no se ocultarán.

## 4. Oráculos de prueba para fases posteriores — TODOS FUTUROS_NO_EJECUTADOS

| Caso | Estímulo verificable futuro | Resultado y evidencia exigidos |
| --- | --- | --- |
| P01 | Medir carga fría y caliente en ruta pública/móvil | LCP/INP/CLS por serie, SHA, trazas y perfil de red |
| P02 | Navegar entre rutas lazy autenticadas | Chunks a demanda, sin guard omitido ni request duplicado |
| P03 | Cargar tabla con paginación/filtro con dataset controlado | Filas/total consistentes y tiempos comparables |
| P04 | Abrir vista con imágenes progresivas | Reserva de espacio, fallback y ausencia de fuga A/B |
| N01 | Cambiar budget a umbral más alto sin justificación | Rechazar regresión engañosa |
| N02 | Comparar distinto dispositivo/red/caché sin declarar | NO_PASS por baseline no comparable |
| N03 | Llamar a API dos veces por render incidental | Detectar duplicación/side effects |
| N04 | Mostrar dato cacheado de tenant A a B | Rechazo backend y purga de estado |
| N05 | Optimizar saltándose JWT/RBAC/permisos | Denegación server-side exigida |
| N06 | Ocultar spinner/error 403/500 para mejorar percepción | Rechazar falta de feedback |
| N07 | Virtualizar y perder filas/selecciones/editados | Caso de regresión de integridad |
| N08 | Imagen causa CLS o filtra URL privada | Rechazo de la variante |
| N09 | Ruta privada precargada sin autorización | Bloquear acceso y no precargar secretos |
| N10 | Responsive a 320px recorta controles y foco | Regresión funcional reproducible |
| N11 | Trace incluye token, email o información de B | Sanitización obligatoria y fallo |
| N12 | CI cancelada/omitada/en curso se reporta PASS | NO_GO a certificación causal |

**No se ejecutó ninguno de estos 16 casos.** El PASS futuro debe aportar antes/después, series repetidas, versión de herramientas, condiciones, URLs de CI, artefactos sanitizados, percentiles por escenario, comparación de regresión y rollback verificado. Valores numéricos objetivo requieren decisión expresa, no inventar thresholds de negocio.

## 5. Riesgos, integración y auditoría

**Impacto:** contrato solo `docs/v5` y `CHANGELOG_AI.md`. No cambia API/DTO, componente Angular, librerías, esquemas, migraciones, índices, credenciales, jobs ni observabilidad activa. La arquitectura Angular 22.2.1, .NET 10, EF Core/MySQL, JWT/RBAC/tenant se conserva. Datos A/B y permisos son oráculos negativos obligatorios de implementación, no pruebas pasadas.

**A0:** diff solo dos Markdown, ausencia de secretos y encabezados huérfanos, procedencia de viñetas. **A1:** matrices NR01–14/P01–04/N01–12 documentales coherentes. **A2:** seguridad tenant/JWT/RBAC y sanitización de métricas. **A3:** lectura de blobs reales y compatibilidad C05-04/C12-02; runtime tests Angular/.NET/Playwright/MySQL = NO_APLICA para este diff documental, no PASS. **A4:** commit fast-forward CAS sobre HEAD dev, Scope Lock y check V5 `completed/success` SHA exacto, PLAN/COLA/BITACORA readback. **A5:** revisión por otra Task Javier distinta de la autora; ninguna promoción QA/main/PROD.

**Rollback:** git revert aditivo del commit Markdown sobre HEAD actual, event compensatorio en BITACORA, sin force-push ni alterar base de datos. Las decisiones de métricas/umbrales, rutas críticas y proveedores permanecen ABIERTAS.
