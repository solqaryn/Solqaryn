# SOLQARYN V5 — C05-04.M01.04: riesgos, supuestos y decisiones pendientes de ligereza

**Autor:** SOLQARYN PRIMARY DEV 01 / JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-14. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 833. **Rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## Alcance y separación entre realidad y propuesta

M01.04 **documenta** riesgos potenciales, supuestos falsables y decisiones a resolver antes de medir u optimizar carga inicial, llamadas repetidas, densidad visual y Core Web Vitals. Se apoya en `C05-04.M01.01` (NR01–NR12), `M01.02` (actores y contratos) y `M01.03` (inventario de código y brechas), sin convertir ninguna hipótesis en defecto comprobado. No cambia aplicación, diseño activo, base de datos, negocio, seguridad, hosts ni workers. M01.05 y M01.06 mantienen sus propias puertas.

### Evidencia real empleada y límites

| Evidencia | SHA/blob de fuente | Delimitación |
| --- | --- | --- |
| M01.01 — objetivo y no-regresión | `a9b7219c7094b8b162f53117a2c67d2c86dd7c21` | NR01–NR12, P06/N12 son expectativas |
| M01.02 — actores e interfaces | `93efea304f25ea098f73f78b2502fe62be054a8c` | 12 actores/12 interfaces, no trazas |
| M01.03 — inventario | `a20753ad76388ec48fe766af54bf07a2456edc8e` | Fuentes Angular/SCSS; no benchmark |
| `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | Budgets 650/750 kB son configuración |
| `frontend/src/app/app.config.ts` | `cd77384e713014015d83b348955b3d83573348b3` | Zone.js y preload selectivo declarados |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Lazy rutas y guards, no consumo medido |
| `frontend/src/app/core/performance/after-render-selective-preloading.strategy.ts` | `a5083eac7fd882430c9550c037c17f89cb946149` | Lógica tras estabilidad/250 ms en /tienda |
| `frontend/src/app/shared/feedback-state/feedback-state.component.ts` | `7deb40475f0c492d09e649e7351b2915bd53d356` | Feedback ARIA disponible, cobertura no probada |

Los SHA son evidencia de lectura estática de árboles ya publicados, no de ejecución en navegador. Ningún riesgo R implica una vulnerabilidad explotable ni una degradación estadísticamente medida por sí mismo.

## Registro de doce riesgos hipotéticos y mitigaciones verificables

| ID | Riesgo sujeto a evidencia | Causa/impacto posible | Control y evidencia requerida |
| --- | --- | --- | --- |
| R01 | Optimización aparente sin baseline | Métrica sin dispositivo/red/caché no comparable | Build SHA y mediciones repetidas por ruta |
| R02 | Reducción artificial de bundle | Excluir funciones o cambiar budget para ocultar regresión | Comparar chunks entregados, funcionalidad y rollback |
| R03 | Precarga prematura | Chunks costosos compiten con contenido útil | Trazar orden de preload con estado /tienda |
| R04 | HTTP repetido | Navegación/reintentos generan tráfico o estados obsoletos | Contar requests por escenario, abort/cancel y perfil |
| R05 | Caché privada entre tenants | A observa datos de B por reutilización global | Pruebas positivas/negativas A→B, cache key/invalidación |
| R06 | Desactivar verificaciones | Reducir latencia eludiendo JWT/RBAC/tenant | Control servidor A/B sigue siendo obligatorio |
| R07 | Ocultar errores por limpieza visual | 403/500 pasa por vacío o loading eterno | Casos ARIA/error/empty, códigos y estados fieles |
| R08 | Romper foco/teclado | Modales o densidad interrumpen navegación | Playwright teclado/zoom/focus/restore |
| R09 | Degradar móvil 320 px | Tablas/grids generan overflow/cortes | Capturas y assertions responsive por ruta |
| R10 | Ignorar reduced-motion | Animación perjudica experiencia | Inspección prefers-reduced-motion y feedback |
| R11 | Marca/estado anterior persiste | Cambio tenant deja flash de tokens o datos | Conmutación A→B comprobada sin PII en trazas |
| R12 | Rollback no reversible | Refactor distribuido sin límites ni fallback | Cambios por pantalla/feature, revert y regresiones |

**Estado:** R01–R12 = `RIESGO_HIPOTETICO_SIN_CAUSA_VERIFICADA`. Ningún riesgo autoriza una mitigación de código automática ni relaja controles de alcance, y los de seguridad se tratan fail-closed.

## Ocho supuestos falsables (S01–S08)

| ID | Supuesto, NO verificado | Experimento para confirmarlo/refutarlo |
| --- | --- | --- |
| S01 | Ruta dashboard está entre las cargas críticas | Baseline de navegación y frecuencia de uso con datos autorizados |
| S02 | Bundle inicial es una limitante perceptible | Medir bytes/chunks contra LCP bajo red reproducible |
| S03 | Preload condicional actual ahorra transferencia | Comparar con/contra preload en laboratorio autorizado |
| S04 | Existen llamadas duplicadas significativas | Traza HTTP por flujo sin capturar tokens/PII |
| S05 | Densidad mayor afecta productividad móvil | Prueba de tareas y accesibilidad a 320px |
| S06 | Estados ARIA están cubiertos en rutas críticas | Inventario por componente, teclado y tests |
| S07 | Cache/estado se invalida correctamente en A→B | Escenarios cross-tenant positivos/negativos con API real |
| S08 | Un rollback por feature restaura la experiencia | Ensayo aislado en CI/código versionado antes de promover |

Sin estas mediciones no asignar PASS ni cifras de bundle o Core Web Vitals. El alcance M01.04 no ejecutó los ocho experimentos.

## Ocho decisiones abiertas (D01–D08)

| ID | Decisión pendiente | Autoridad y puerta exigida |
| --- | --- | --- |
| D01 | Rutas prioritarias y escenarios base | Producto/UX con datos permitidos, no datos de cliente expuestos |
| D02 | Objetivos de LCP/INP/CLS por perfil | Equipo técnico y producto; baseline reproducible antes de fijar umbrales |
| D03 | Política de bundle/chunks y budget | Arquitectura frontend, CI dirigida y revisión de regresión |
| D04 | Dedupe/cancel/cache HTTP con tenant | Seguridad/backend/Angular, test A/B y JWT/RBAC |
| D05 | Estrategia de densidad y contenido | UX/accesibilidad, teclado/320px/zoom |
| D06 | Condiciones de preload por ruta | Arquitectura Angular, traza de red, sin rutas privadas indebidamente cargadas |
| D07 | Telemetría RUM y retención permitida | Privacidad/seguridad, minimización de PII, control de acceso |
| D08 | Unidad de rollback/flags autorizados | Ingeniería/QA, exact-SHA, reversibilidad sin alterar PROD |

Todas quedan en **PENDIENTE_DECISION**; esta microtarea no aprueba un nuevo presupuesto, plan de compra, proveedor, permisos, token, bandera operativa ni migración. Una Task no obtiene autoridad comercial por documentar la posibilidad.

## Casos documentales de revisión FUTURA (no tests ejecutados)

**Positivos P01–P06:** (1) baseline reproducible con SHA, entorno y métricas; (2) carga diferida que conserva disponibilidad; (3) A→B sin mezcla de cache/estado; (4) móvil 320 px con foco/ARIA correctos; (5) 403/500 informativos y seguros; (6) revert de optimización sin deshabilitar auth.

**Negativos N01–N12:** cambios de budget sin comparar artefactos; 403 convertido en vacío; fuga A→B por caché; precarga privada; fallback que omite JWT/RBAC; HTTP duplicado no detectado; foco inaccesible; overflow 320 px; reduced-motion ignorado; Core Web Vitals sin trazas; RUM con JWT/PII; auditoría A5 realizada por su autor. Son **oráculos NO_EJECUTADOS**, no pruebas concluyentes.

## Impacto, DAG, auditorías y rollback

**Impacto limitado:** nuevo Markdown y sección autocontenida en `CHANGELOG_AI.md`. No se modifican Angular, SCSS, API, Domain/Application/Infrastructure, MySQL, entornos, datos/secretos, permisos, routers, workers ni dispatcher. Consumidores futuros: UX Angular, backend auth/tenant, pipeline CI, QA de accesibilidad, observabilidad con privacidad.

**Preflight:** PLAN_MAESTRO!H833=PENDIENTE/I833 vacío; COLA!G833/O833=SI; DAG_TECNICO!A794=C05-04.M01.04/F794=VALIDADO/G794=SI; CAS único, readback y evento BITACORA. Dependencias preparatorias C01/C02/C05 no liberan M01.06, M02, JWT/RBAC ni QA/main/PROD.

**A0:** diff Markdown, ocho fuentes y ausencia de credenciales. **A1:** inventario de 12 riesgos, ocho supuestos, ocho decisiones y oráculos P06/N12 futuros. **A2:** JWT/RBAC/tenant A/B, ARIA, privacidad RUM y seguridad sin cambio ejecutable. **A3:** coherencia con M01.01–03 y fuentes reales, no benchmark. **A4:** Git fast-forward/CAS serializado y exact-SHA checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` con resultado `completed/success`; readback PLAN/COLA/BITACORA. Builds Angular, backend, MySQL y Playwright **NO_APLICA** al diff solo Markdown, nunca PASS inferido. **A5:** pendiente de Task Javier distinta, anterior a cualquier promoción. **Rollback:** `git revert` ordinario, sin force-push, sin reescritura histórica y sin QA/main/PROD.
