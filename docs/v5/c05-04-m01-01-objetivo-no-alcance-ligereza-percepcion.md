# SOLQARYN V5 — C05-04.M01.01: objetivo, no-alcance y no-regresión de ligereza del ERP

**Autor:** PRIMARY DEV 06 JAVIER / TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 830. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## Resultado de esta microtarea documental

Fijar un contrato de mejora futura de percepción, carga inicial, densidad visual, llamadas duplicadas y ruido del ERP, **sin afirmar que se hayan obtenido mejoras de rendimiento o reducciones de bundle**. La unidad M01.01 define objetivos y límites, no ejecuta refactor, medición, cambio de dependencias, publicación de CSS, migración, API ni incremento de cuota. Sus pruebas deben diseñarse y ejecutarse en M02–M05 según la cola. M01.02–05 y puerta M01.06 permanecen separadas; no adelantar liberaciones.

**Objetivo falsable posterior:** una pantalla debe proporcionar contenido útil y controles navegables con menos espera y menor carga de trabajo percibida, sin suprimir advertencias, accesibilidad, permisos, tenant o trazabilidad. Se medirá antes/después por rutas críticas, dispositivo móvil representativo y red reproducible, usando bundle emitido por build, trazas HTTP y Core Web Vitals. Nunca cambiar límites o recortar contenido para mejorar números aislados sin criterio de negocio.

## Línea base leída del código (no benchmark ejecutado)

| Fuente de dev | Blob Git leído | Evidencia delimitada |
| --- | --- | --- |
| `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | Build Angular application tiene budget inicial de warning 650 kB y error 750 kB, styles por componente 16/32 kB; es **configuración declarada**, no resultado de build ni objetivo final aprobado |
| `frontend/src/app/app.config.ts` | `cd77384e713014015d83b348955b3d83573348b3` | `withPreloading(AfterRenderSelectivePreloadingStrategy)`, `provideZoneChangeDetection` y router; no significa que todas las rutas se pre-carguen ni que la app sea zoneless |
| `frontend/src/app/core/performance/after-render-selective-preloading.strategy.ts` | `a5083eac7fd882430c9550c037c17f89cb946149` | Preload solo si `preloadAfterRender===true`, ruta `/tienda`, app estable y temporizador 250 ms; lectura estática, no certifica métricas |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Lazy `loadComponent`, rutas y guards existentes; no implica que permisos y datos estén autorizados por frontend |
| `frontend/src/styles/_enterprise-design-system.scss` | `6a188590af9a958bff2e3e0375c0c46cdb2b14e6` | Tokens semánticos, spacing, motion, breakpoints y `--target-min:44px` declarados; no equivalen a todas las rutas auditadas |
| `frontend/src/styles/_m10-accessibility.scss` | `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f` | Clases de foco, anchura flexible, scroll margin y formularios accesibles; no prueba resultado en navegador |
| `frontend/src/app/shared/feedback-state/feedback-state.component.ts` | `7deb40475f0c492d09e649e7351b2915bd53d356` | Estados loading/empty/error/success/warning con aria-live; no eliminar estados útiles por reducir densidad |
| `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Define scripts lint/build/test y Playwright; presencia de script **no** es evidencia de ejecución ni PASS |

Los ocho blobs se comprobaron en el árbol `dev` antes de la reclamación. Una modificación posterior exige revalidar hashes y HEAD. Este inventario es base limitada a las rutas aquí listadas, no auditoría global de todas las vistas ni demostración de defectos.

## Doce invariantes de no-regresión NR01–NR12

| ID | Condición exigida a cambios futuros | Bloqueo |
| --- | --- | --- |
| NR01 | No ampliar bundle inicial sin evidencia de beneficio y presupuesto aprobado | NO_GO si se degrada sin evidencia causal y rollback |
| NR02 | Cargar rutas/código no críticos después del primer render, sin alterar rutas ni guards | NO_GO si se degrada sin evidencia causal y rollback |
| NR03 | Evitar duplicación de HTTP y llamadas de inicialización; preservar deduplicación/cancelación segura tenant A/B | NO_GO si se degrada sin evidencia causal y rollback |
| NR04 | No ocultar el error al reducir el ruido: loading, vacío, error y feedback deben seguir disponibles y accesibles | NO_GO si se degrada sin evidencia causal y rollback |
| NR05 | No eliminar o debilitar JWT/RBAC, permisos backend o verificaciones tenant por optimizar navegación | NO_GO si se degrada sin evidencia causal y rollback |
| NR06 | Mantener lectura clara y densidad adaptable en móvil 320px, teclados, lectores de pantalla y zoom | NO_GO si se degrada sin evidencia causal y rollback |
| NR07 | Mantener foco visible, jerarquía semántica, contrastes y reduced-motion | NO_GO si se degrada sin evidencia causal y rollback |
| NR08 | Preservar tokens de branding y evitar flash de tema/datos de empresa anterior | NO_GO si se degrada sin evidencia causal y rollback |
| NR09 | Medir Core Web Vitals por ruta/escenario y comparar baseline antes de declarar mejora | NO_GO si se degrada sin evidencia causal y rollback |
| NR10 | No usar caché o preload que mezcle datos privados de empresas o usuarios | NO_GO si se degrada sin evidencia causal y rollback |
| NR11 | Reversión por commit/flag autorizado sin cambios productivos ni migraciones improvisadas | NO_GO si se degrada sin evidencia causal y rollback |
| NR12 | No atribuir pruebas CI, Playwright, Lighthouse o métricas que no se ejecutaron | NO_GO si se degrada sin evidencia causal y rollback |

## Alcance futuro permitido sólo por fases elegibles

**Dentro del lote futuro, condicionado a autorización y CI:** medir y reducir duplicación HTTP verificada, carga inicial, contenido excesivo por pantalla, complejidad de layouts, imágenes pesadas, preloading innecesario, duplicación de CSS, secuencias de carga y estados visuales confusos; conservar rutas lazy y priorizar telemetría sin PII. Registrar métricas antes/después por ruta y condiciones de prueba, presupuestos propuestos para bundle y Core Web Vitals, con trazas y Playwright móvil, además de accesibilidad/tenant A/B.

**Fuera de esta M01.01:** cambiar Angular 22.2.1, sustituir Zone.js, activar zoneless, actualizar librerías, optimizar consultas MySQL sin profiling, eliminar controles auth/tenant/RBAC, modificar HTTP API, introducir nuevos proveedores, ejecutar cambios productivos, activar workers, eventos, Tasks, PR, ramas, compras, QA/main/PROD o editar datos reales. Una optimización transversal necesitará análisis de consumidores, costes y fallback.

## Oráculos de aceptación futuros; no pruebas ejecutadas

**Positivos P01–P06**: P01 el primer contenido útil de la ruta crítica aparece dentro del objetivo de rendimiento aprobado; P02 lazy chunk no crítico permanece diferido hasta uso/preload pertinente; P03 cambio A→B invalida cargas privadas de A y muestra B sin mezcla; P04 navegador 320px conserva contenido sin scroll horizontal roto, foco/teclado; P05 loading/empty/error siguen disponibles con etiquetas accesibles; P06 medición reproducible registra baseline, versión, red/dispositivo, condiciones y rollback.

**Negativos N01–N12**: N01 duplicar HTTP al cambiar de ruta repetidamente; N02 precargar módulos costosos fuera del criterio explícito; N03 presentar datos A durante cambio a B; N04 ocultar 403 o error real como resultado vacío; N05 suprimir spinner/aria-live/foco; N06 sobrepasar budgets aprobados sin evidencia; N07 introducir layout roto a 320px; N08 animación forzada bajo reduced-motion; N09 cargar un bundle extra por cada tarjeta sin necesidad; N10 cachear recurso privado sin segmentación tenant; N11 modificar JWT/RBAC/guards para mejorar métricas; N12 declarar CWV PASS por un informe no ejecutado. Todos son criterios FUTUROS, no incidencias verificadas en esta unidad.

## Dependencias y controles A0–A5

En el momento de reclamar: `PLAN_MAESTRO!H830=PENDIENTE/I830` vacío; `COLA!G830/O830=SI`; `DAG_TECNICO!A791=C05-04.M01.01/F791=VALIDADO/G791=SI`. Son gates de **preparación documental**, no certificación de fase C05 ni autorización a código runtime.

**A0:** diff solo dos Markdown, ocho blobs y exclusión de secretos; **A1:** objetivo, alcance, 12 NR, P06/N12 documentales, no ejecutar suites; **A2:** no alterar tenant/JWT/RBAC ni secretos; **A3:** fuente estándar y lectura causal de Angular/SCSS, sin inventar benchmark; **A4:** fast-forward serializado en dev, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` SUCCESS en SHA exacto, PLAN/COLA/BITACORA readback. Builds, xUnit y Playwright de runtime son **NO_APLICA al diff exclusivamente Markdown**, no PASS. **A5:** evaluación de otra Task Javier, distinta del autor, antes de cualquier QA/main/PROD.

**Rollback:** `git revert` ordinario del commit documental; no force-push ni reescritura de historia.
