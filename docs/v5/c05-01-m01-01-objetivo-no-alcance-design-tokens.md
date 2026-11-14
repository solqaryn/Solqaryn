# SOLQARYN V5 — C05-01.M01.01: objetivo, no-alcance y no-regresión del sistema de diseño

**Autor:** PRIMARY DEV 06 — JAVIER; TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO fila 722. **Repositorio:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK:** STRICT. **Fecha:** 2026-11-14. Esta unidad M01.01 establece exclusivamente el perímetro; no implementa nuevos componentes ni certifica lotes posteriores.

## Objetivo acotado y medible

Definir el contrato de no-regresión del lote **C05-01 — Design tokens y componentes** para que tipografía, paleta semántica, espaciado, componentes, estados, tablas, modales, formularios y navegación mantengan consistencia visual, accesibilidad y comportamiento responsive entre pantallas públicas y privadas. La evidencia exigida al final del lote será un catálogo trazable de componentes, contraste y respuesta a diferentes tamaños; **M01.01 solo fija objetivo, exclusiones y criterios**, sin afirmar que ese catálogo completo ya existe ni que fue probado E2E.

Al completar la fase futura deberán identificarse el token canónico, el consumidor, las variantes/estados, la prueba funcional y la evidencia de accesibilidad por componente. Un token de marca o preferencia visual nunca debe convertirse en autoridad de autenticación, tenant o permisos. Los cambios visuales reales requieren sus microtareas M02–M05, diffs controlados y CI causal.

## Línea base inspeccionada en dev (no equivale a certificación de comportamiento)

| Fuente del repositorio | Blob observado | Hecho delimitado |
| --- | --- | --- |
| `frontend/src/styles.scss` | `2eb48562fd77c758662af6b9683649b4f9698a64` | Importa design system y accesibilidad; declara `--color-primary`, superficies, texto, feedback, `--focus-ring`, reglas `min-width:0` y foco visible |
| `frontend/src/styles/_enterprise-design-system.scss` | `6a188590af9a958bff2e3e0375c0c46cdb2b14e6` | Declara tipografía, `--space-1` a `--space-12`, radios, sombras, motion y `--target-min:44px`, breakpoints y escala de contenido |
| `frontend/src/styles/_m10-accessibility.scss` | `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f` | Añade skip-link, foco al main, geometría de layouts, headers y controles accesibles |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Declara rutas Angular y guards; inventariar navegación sin sustituir decisiones del servidor |

Estos blobs identifican **archivos existentes**, no garantizan contraste suficiente en todas las combinaciones, accesibilidad certificada, ausencia de regresiones ni cobertura total del builder. La arquitectura actual declarada es Angular 22.2.1 standalone + Material/CDK, .NET 10, EF/MySQL, JWT/RBAC; se preserva Zone.js. No se añade framework ni dependencia.

## Dentro del perímetro futuro y fuera de esta microtarea

**Dentro del lote C05-01, pendiente de unidades específicas:** mapa token→consumidor para color, tipografía, espaciado, foco y motion; estados `default/hover/focus/disabled/error/loading`; contraste texto/fondo y feedback no dependiente solo del color; objetivos táctiles mínimos; tablas que desplazan contenido dentro del contenedor; jerarquía de encabezados; navegación por teclado, skip links y recuperación de foco; diálogos con foco contenido y restaurado; responsive mínimo desde 320px; preferencias visuales por tenant limitadas a allowlists y saneadas.

**Fuera de C05-01.M01.01:** cambiar SCSS/TS, componentes, rutas, guards, estilos activos, brandings reales, JWT, RBAC, tenant, datos, API, BD/migraciones, trabajadores, eventos, webhooks, despliegues, fuentes externas, pagos, secretos, Tasks, horarios, main/QA/PROD, DNS o servicios productivos. No implementar un nuevo design-system ni habilitar flags comerciales desde documentación. No ejecutar ni atribuir Playwright o navegador sin ejecución real.

## Criterios de no-regresión que gobiernan implementaciones futuras

| ID | Invariante | Indicador de NO_GO |
| --- | --- | --- |
| NR01 | Un componente reutiliza el token canónico; excepciones requieren motivo trazable | Duplicación arbitraria que diverge semántica entre vistas |
| NR02 | El texto y los controles conservan foco visible por teclado | Foco suprimido u oculto tras overlay, sticky header o navegación |
| NR03 | Estados de error, éxito y advertencia ofrecen texto o iconografía contextual | Estado comunicado solo con color |
| NR04 | Controles interactivos accesibles conservan objetivo táctil suficiente | Reducción no justificada por debajo del objetivo definido (`--target-min`) |
| NR05 | Diseño se ajusta desde 320px sin scroll horizontal del documento | Contenido impone ancho fijo fuera de tabla con scroll interno |
| NR06 | Diálogos y overlays mantienen orden de foco y escape seguro | Foco perdido o interacción con fondo modal activo |
| NR07 | Tablas y formularios admiten etiquetas, errores y estados de carga legibles | Header o label inaccesible, validación silenciosa |
| NR08 | Motion conserva propósito, no bloquea uso y respeta reducción de movimiento | Animación obligatoria que esconde acciones, contenido o foco |
| NR09 | Preferencia de tema o branding solo cambia presentación allowlisted | CSS/HTML/JS arbitrario, valor de tenant usado como rol/permiso |
| NR10 | La autorización sigue siendo server-side: ruta/guard no concede privilegios | Acceso a datos B desde A por tema, ruta, host o cliente |
| NR11 | Contraste de texto/foco se verifica por combinaciones y estados, no por intuición | Contraste insuficiente o foco invisible en temas previstos |
| NR12 | No se rompen componentes existentes ni compatibilidad Angular/Material | Cambio global sin pruebas de pantallas consumidoras y rollback |

## Oráculos futuros (especificaciones, NO resultados PASS de runtime)

**Positivos:** P01: focus-visible navegando con Tab; P02: layout de 320px sin scroll horizontal del documento; P03: formulario con label, error y descripción accesible; P04: tabla amplia con scroll únicamente local; P05: cambio de tema allowlisted que mantiene roles/permisos invariantes.

**Negativos:** N01: color como único indicador; N02: foco ausente; N03: contraste insuficiente; N04: modal con foco fuera; N05: botón inalcanzable por teclado; N06: tabla que amplía viewport; N07: input sin label; N08: HTML/CSS arbitrario en branding; N09: rol inferido de tema; N10: token no previsto por allowlist; N11: animación sin alternativa reducida; N12: acceso cruzado tenant A/B tras cambiar marca.

Los 5 positivos y 12 negativos son pruebas de aceptación **a implementar en fases posteriores**; no se ejecutaron aquí ni se interpretan como pruebas Angular/Playwright. Para esta unidad A1–A3 se validan documentalmente la existencia, coherencia y no contradicción de estos contratos.

## Predecesores, cierres y reversibilidad

`DAG_TECNICO!A683:G683` referencia `C00-03.M01.01–05`, `C01-01..04.M01.01–05` y `C02-01..04.M01.01–05` como preparación documental. La habilitación puntual exige `PLAN_MAESTRO!H722=PENDIENTE/I722` vacío al claim, `COLA!G722/O722=SI`, `DAG_TECNICO!F683=VALIDADO/G683=SI` y propietario exclusivo; no equivale a liberación de fase ni M01.06. Los predecesores son referencias, no permisos para modificar IAM/DB.

**A0:** comprobar fuentes, diff, no secretos y límites; **A1:** verificar objetivos, cinco positivos y doce negativos como oráculos, no como ejecución; **A2:** preservar seguridad JWT/RBAC/tenant A/B, temas saneados y denegación backend; **A3:** referencias del source plan, SCSS y ruta; **A4:** commit fast-forward a dev, SHA exacto, Scope Lock y control V5 CHANGELOG en `completed/success`, PLAN/COLA/BITACORA readback. Tests backend/frontend ajenos a Markdown: `NO_APLICA` causal, no PASS. **A5:** revisión independiente obligatoria por otro TASK_ID Javier; nunca por el autor. Revertir este contrato documental con `git revert` regular (sin force-push); cualquier reversión UI futura exige plan separado. Nunca promover QA/main/PROD automáticamente.
