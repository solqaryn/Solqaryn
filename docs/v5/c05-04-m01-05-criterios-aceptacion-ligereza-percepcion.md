# SOLQARYN V5 — C05-04.M01.05: criterios observables de aceptación de ligereza

**Autor:** PRIMARY DEV 01 / JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-14. **Autoridad única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 834. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## Resultado contractual y límites

La microtarea M01.05 define criterios funcionales y técnicos **observables para etapas futuras** de mejora de carga inicial, duplicación HTTP, densidad y percepción del ERP. Los criterios son verificables únicamente cuando existan baseline, entorno de medición, implementación y CI dirigida. **No se ejecutan pruebas de rendimiento, Lighthouse, Playwright ni HTTP reales por escribirlos.** No fija umbrales de negocio sin aprobación de D01–D08 de M01.04, no modifica configuración Angular ni supera las puertas de M01.06/M02–M06.

El alcance conserva los doce invariantes NR01–NR12 de M01.01, los doce actores/entradas de M01.02, el inventario M01.03 y los 12 riesgos/8 supuestos/8 decisiones PENDIENTE de M01.04. Se declara aceptable solamente una mejora que preserve rendimiento **y** seguridad, accesibilidad, trazabilidad y control de datos multiempresa.

## Catorce criterios positivos de aceptación FUTURA

| ID | Escenario observable | Evidencia mínima para PASS futuro | Tecnología / condición |
| --- | --- | --- | --- |
| AC01 | Baseline de ruta crítica | LCP/INP/CLS, SHA, device, viewport, red, caché, serie de mediciones | Playwright/Lighthouse/RUM aprobado; D01/D02 |
| AC02 | Peso real del bundle inicial | Bytes emitidos por producción, chunks y diff contra baseline | Angular build, `angular.json`; D03 |
| AC03 | Presupuesto de estilos | Reporte `anyComponentStyle` sin exceder límites aprobados | Angular build; no alterar budget para aparentar PASS |
| AC04 | Diferimiento de rutas | Evidencia de chunks lazy y carga bajo navegación autorizada | Router/trace, sin alterar guards |
| AC05 | Preload selectivo | Solo ruta elegible `/tienda` y flag, app estable, disparo condicionado | Traza de chunks, sin preload indiscriminado |
| AC06 | Menos solicitudes duplicadas | Conteo comparativo de URL/método/origen por flujo sin PII | Instrumentación autorizada; D04 |
| AC07 | Navegación sin estado viejo | Refetch/cancel seguros al cambiar pantalla o filtros | Angular/router/HTTP, sin mostrar error falso |
| AC08 | Aislamiento A→B | Sesión A no revela datos/tema/cache de A tras pasar a B | Browser + API autenticada; tenant A/B |
| AC09 | Permisos servidor intactos | API sigue denegando recursos de tenant ajeno y rol insuficiente | .NET JWT/RBAC negativos y positivos |
| AC10 | Feedback fiel | loading, empty, error, warning, success accesibles y sin errores ocultos | aria-live/role + casos 403/500 |
| AC11 | Móvil a 320 px | Tareas clave, forms/dialogs, foco y botones utilizables sin clipping | Playwright 320px/zoom y teclado |
| AC12 | Preferencias inclusivas | reduced-motion, forced-colors y foco visibles sin pérdida semántica | CSS/browser testing, no solo inspección estática |
| AC13 | Privacidad de medición | Trazas sin JWT, cookies, secretos ni datos de otro tenant | Revisión log/telemetría minimizada |
| AC14 | Reversibilidad | Revert o flag autorizado restaura baseline sin pérdida de función, permiso o dato | Ensayo dirigido, rollback versionado |

**Definición de PASS futuro:** cada criterio debe adjuntar caso positivo y negativo del mismo SHA implementado, artefacto reproducible con URL de CI, comparador antes/después y limitaciones. Los thresholds comerciales de LCP/INP/CLS y mejora porcentual siguen `PENDIENTE_DECISION` D02; no usar los budgets declarados 650/750 kB como prueba automática de rendimiento percibido.

## Catorce condiciones negativas de rechazo FUTURO

| ID | Intento o regresión que debe ser detectada | Resultado exigido |
| --- | --- | --- |
| N01 | Reemplazar medición por cifra estimada | NO_PASS sin evidencia con SHA, red y dispositivo |
| N02 | Aumentar budget para ocultar un bundle inflado | Rechazo sin decisión D03 y comparación de bytes |
| N03 | Precargar chunk no autorizado | Rechazo o rollback, rastreo de política |
| N04 | Fuga de caché de empresa A hacia B | API/UI niegan y purgan estado |
| N05 | Derivar tenant de query, slug o header cliente | Autorización backend rechaza |
| N06 | Quitar guard o permiso servidor por performance | Test de seguridad falla y bloquea publicación |
| N07 | Duplicar HTTP/reintentar sin control | Diagnóstico de conteo y aceptación denegada |
| N08 | Convertir 403/500 en `empty` | Error semántico visible y test negativo |
| N09 | Ocultar loading/error/aria-live al simplificar | Accesibilidad no aprueba |
| N10 | Vista 320 px con overflow de controles o textos | Test responsive falla |
| N11 | Foco perdido al cerrar modal o cambiar ruta | Prueba teclado falla |
| N12 | Animación obligatoria con reduced-motion | Prueba de preferencia falla |
| N13 | RUM/log contiene JWT, cookie, PII o URL firmada | Rechazo y corrección de exposición |
| N14 | Rollback daña autorización, tenant o datos | NO_GO; revert sin force-push, sin promoción |

Estos negativos **no se ejecutaron** al escribir este contrato. Sus resultados deberán provenir de evidencias técnicas en M02–M05 y A0–A4; la aprobación A5 independiente tampoco reemplaza un test omitido.

## Paquetes de prueba, baseline y puertas futuras

| Paquete | Evidencia exigida cuando se implemente | No confundir con |
| --- | --- | --- |
| BUILD | `npm run build:prod`, tamaños emitidos, budgets y SHA exacto | Presupuestos escritos en `angular.json` |
| NAV | Playwright navegación, cold/warm cache, preload y rutas lazy | Presencia de `loadComponent` |
| PERF | Repeticiones de Lighthouse/CWV con entorno y política D02 | Valores sin fuente o una sola captura |
| HTTP | Conteo e identificación de solicitudes duplicadas, cancelación y retries | Interceptor instalado |
| TENANT | Dos empresas A/B, rol permitido/prohibido, switch, API y cache | Guard Angular o localStorage únicamente |
| A11Y | 320 px, zoom, teclado, foco, reduced-motion, estado ARIA | SCSS con reglas declaradas |
| SAFE | Auditoría de secretos, no PII en métricas, rollback y degradaciones | Declarar seguridad por falta de cambios visibles |

Las pruebas de API/.NET corresponden solo si una implementación modifica autorización, servicio o datos. Si el diff es exclusivamente documental, esas pruebas son **NO_APLICA motivado**, no PASS. Cualquier decisión de activar RUM, cambiar permisos o desplegar cambios requiere autorización ulterior específica.

## Fuentes verificadas y dependencias

| Fuente de `dev` | Blob Git | Hecho estático |
| --- | --- | --- |
| `docs/v5/c05-04-m01-01-objetivo-no-alcance-ligereza-percepcion.md` | `a9b7219c7094b8b162f53117a2c67d2c86dd7c21` | NR01–NR12 y oráculos previos |
| `docs/v5/c05-04-m01-02-actores-interfaces-ligereza-percepcion.md` | `93efea304f25ea098f73f78b2502fe62be054a8c` | Actores/interfaces futuros |
| `docs/v5/c05-04-m01-03-inventario-codigo-evidencia-ligereza.md` | `a20753ad76388ec48fe766af54bf07a2456edc8e` | Inventario Angular/SCSS y brechas |
| `docs/v5/c05-04-m01-04-riesgos-supuestos-decisiones-ligereza.md` | `8554efc8ccf82ca4c48282eb5f4ab61288a2e4b1` | R12/S08/D08 pendientes |
| `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | Budgets declarados |
| `frontend/src/app/app.config.ts` | `cd77384e713014015d83b348955b3d83573348b3` | Router, Zone.js, preload |
| `frontend/src/app/core/performance/after-render-selective-preloading.strategy.ts` | `a5083eac7fd882430c9550c037c17f89cb946149` | Preload condicional |
| `frontend/src/app/shared/feedback-state/feedback-state.component.ts` | `7deb40475f0c492d09e649e7351b2915bd53d356` | Feedback ARIA |

**Preflight:** PLAN_MAESTRO!H834=PENDIENTE/I834 vacío; COLA!G834/O834=SI; DAG_TECNICO A795=C05-04.M01.05/F795=VALIDADO/G795=SI; CAS único y evento BITACORA. La puerta es de preparación M01.05, no libera M01.06/M02 ni autoriza cambios productivos.

## Impacto, A0–A5 y reversión

**Impacto:** solo este archivo `docs/v5` y encabezado/viñetas nuevos en `CHANGELOG_AI.md`. Cero cambios Angular, API/.NET, DTO, MySQL, auth JWT/RBAC, tenant, bundles, UI, observabilidad real, secret, worker, servicio de evento, compras, Task física o entornos. Consumidores futuros: frontend/router, backend auth, QA/performance, accesibilidad y seguridad.

**A0:** ocho fuentes, diff documental, sin secretos. **A1:** AC01–AC14/N01–N14 son oráculos, no tests ejecutados. **A2:** fail-closed tenant A/B y JWT/RBAC, protección de métricas/PII. **A3:** trazabilidad desde M01.01–04, decisiones D01–D08 siguen pendientes. **A4:** publicación fast-forward/CAS en dev, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA publicado, lectura posterior PLAN/COLA/BITACORA. Build/Playwright/MySQL/.NET runtime **NO_APLICA** al cambio solo Markdown. **A5:** revisión independiente de otra Task Javier antes de QA/main/PROD; no autoaprobación.

**Rollback:** `git revert` normal del commit documental, sin force-push, sin modificar history/secretos ni inventar una liberación. M01.06 requiere sus gates y A5 previos; no se declara habilitado aquí.
