# SOLQARYN V5 — C05-01.M01.03: lectura verificable de contratos, código y evidencia

**Microtarea:** `C05-01.M01.03`, preparación documental de design tokens y componentes. **Autor:** PRIMARY DEV 06 JAVIER, `TASK_ID=6af16bbad26881918c283abe06e1f32a`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 724. **Repositorio único:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK=STRICT**. **Fecha:** 2026-11-14.

## Objetivo de esta lectura, no certificación de la plataforma

Registrar el estado comprobable de **contratos y fuentes existentes** que soportan diseño coherente, contraste, responsive, estados y navegación, señalando qué se puede inferir del código y qué **no** está demostrado. Este inventario no modifica UI, no integra un nuevo componente y no certifica WCAG, Playwright, Angular build, backend JWT/RBAC o multiempresa de forma global. Completa solo la lectura M01.03; M01.04 riesgos, M01.05 aceptación y la puerta M01.06 siguen siendo unidades distintas.

## Manifiesto causal de fuentes leídas

| ID | Ruta en repo | Git blob visto | Hecho concreto | Evidencia todavía NO acreditada |
| --- | --- | --- | --- | --- |
| F01 | `frontend/src/styles.scss` | `2eb48562fd77c758662af6b9683649b4f9698a64` | Importa design system y accesibilidad; usa tokens semánticos, foco visible y min-width 0 | Contraste de todos los estados, navegadores y temas |
| F02 | `frontend/src/styles/_enterprise-design-system.scss` | `6a188590af9a958bff2e3e0375c0c46cdb2b14e6` | `--font-size-*`, `--space-*`, radios, `--target-min: 44px`, breakpoints, motion | Inventario componente a componente y cumplimiento de tamaño en todas las rutas |
| F03 | `frontend/src/styles/_m10-accessibility.scss` | `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f` | Skip-link, scroll-margin y geometría de layouts y títulos | Recorrido de lector de pantalla y foco en cada feature |
| F04 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Configura rutas Angular con guards | Guards no sustituyen autorización backend ni prueban tenant A/B |
| F05 | `frontend/src/app/shared/confirm-dialog.component.ts` | `f9b73544cc70beba83a891b2fc89537f0718af8b` | Material dialog con título, mensaje, cancelación y confirmación | Pruebas reales de Escape, teclado, focus trap y restauración de foco |
| F06 | `frontend/src/app/shared/feedback-state/feedback-state.component.ts` | `7deb40475f0c492d09e649e7351b2915bd53d356` | Tipos loading/empty/success/error/warning; `role`, `aria-live`, `aria-busy` y mensaje | Lectura efectiva en tecnologías asistivas y todas las variantes |
| F07 | `frontend/src/app/shared/navigation/app-navigation-menu.component.ts` | `b70ef4ffb252228cf2ab19754a5e30d53f64aca0` | RouterLink, estado active y comprobaciones `PermisosRuntimeService.puede` por menú | Acceso HTTP permitido/denegado server-side y exhaustividad de rutas |
| F08 | `frontend/src/app/services/theme-applier.service.ts` | `cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3` | Valida colores con regex hexadecimal y limpia tokens antes de cargar nuevo tenant; versionado local descarta respuesta tardía | Integración A/B end-to-end y todos los valores recibidos del servidor |
| F09 | `frontend/src/app/services/theme-applier.service.spec.ts` | `99529c4f26a075f58d1b0a08f6817c4d42003814` | Contiene pruebas Angular declaradas para ThemeApplierService | No se ejecutó este archivo en esta unidad; su presencia no equivale a PASS |

Los hashes son identificadores de contenidos inspeccionados en `dev`; **no** se interpretan como resultados de CI. Si un hash o el HEAD cambia, la fase que realice modificación deberá releerlo y ejecutar pruebas de ese SHA. La coincidencia de una ruta tampoco garantiza que todas las páginas usen el patrón. `C05-01.M01.01` fijó objetivo/no-alcance; `C05-01.M01.02` identificó actores y superficies; esta entrega separa **código observado** de **evidencia pendiente**.

## Resultados de la auditoría documental

| Dimensión | Evidencia positiva observada | Brecha verificable / siguiente prueba |
| --- | --- | --- |
| Tipografía y espaciado | Una escala declarada en F02 | Comparar consumidores reales, detectar overrides y duplicados |
| Colores y contraste | Tokens globales F01 y cálculo de foreground en servicio F08 | Matriz AA de contraste por tema A/B, focus y estado, incluyendo preferencias de sistema |
| Responsive | `min-width:0` y breakpoints SCSS F01/F02 | Playwright viewport 320/768/1280 y scroll horizontal real |
| Dialog y formularios | Modal Material F05, estilos de campos F01 | Navegación Tab/Shift+Tab/Escape, anuncio de error y restauración de foco |
| Loading/empty/error | Contrato de componentes F06 con atributos ARIA | Prueba de anuncios semánticos, loaders y retry sin silencio |
| Navegación | RouterLink y visibilidad por permisos runtime F07 | Verificación del backend por tenant/rol; ruta oculta no implica autorización |
| Tema por empresa | Regex HEX y descartado de respuestas obsoletas F08 | Seguridad con tenant A/B, cambio de empresa y fallas de red; no cargar CSS arbitrario |
| Testabilidad | Spec F09 existe en repo | Ejecutar suites dirigidas en microtarea de runtime, registrar SHA y logs; no inventar PASS |

## Matriz de preguntas y oráculos de auditoría futuros

**P01:** se puede rastrear token de tipografía a consumidor y captura. **P02:** menú visible coincide con permiso backend verificado. **P03:** modal se cancela con teclado y devuelve foco. **P04:** cambio A→B limpia marca anterior incluso con respuesta vieja. **P05:** estado de error incluye mensaje, semántica ARIA y recuperación.

**N01:** token duplicado divergente; **N02:** contraste ilegible en tema; **N03:** CSS arbitrario desde tenant; **N04:** fuga de marca A tras cambiar a B; **N05:** menú oculto usado como seguridad; **N06:** ruta directa evade RBAC; **N07:** modal pierde foco; **N08:** feedback solo por color; **N09:** aria-live no anuncia error; **N10:** overflow fuera de tabla a 320px; **N11:** controles inaccesibles por teclado; **N12:** resultado de una spec presente se declara PASS sin ejecución. Estos son criterios de prueba **futuros y no ejecutados** en esta entrega.

## Alcance de validación A0–A5 y control de versión

**Entrada documental:** `PLAN_MAESTRO!H724=PENDIENTE/I724` vacío, `COLA!G724/O724=SI`, `DAG_TECNICO!F685=VALIDADO/G685=SI`, predecesores `C00-03/C01/C02 M01.01–05` como preparación. Esta lectura no libera IAM/JWT/RBAC/tenant, DB, migraciones, M01.06 ni QA/main/PROD.

**A0:** comprobar los nueve paths/blobs, diff y ausencia de secretos. **A1:** validar que los nueve hallazgos distinguen observado de pendiente y que P01–P05/N01–N12 son oráculos documentales. **A2:** preservar autorización backend y aislamiento tenant A/B, no inyectar CSS/JS de terceros. **A3:** verificar coherencia con M01.01/M01.02, Source Doc y arquitectura. **A4:** publicar fast-forward en `dev`, confirmar checks exact SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` con `completed/success`, y PLAN/COLA/BITACORA readback. CI runtime de código sin modificaciones es `NO_APLICA` para este documento, no PASS. **A5:** revisión por TASK_ID Javier distinto de la autoría, nunca autoaprobación.

**Rollback:** revertir exclusivamente el cambio de Markdown con `git revert` normal; no force-push, no modificación de datos, servicios, secretos, permisos, webhooks, Tasks, QA/main/PROD. Cualquier CI fallida obligatoria se investiga causalmente; `SKIPPED/IN_PROGRESS` no es PASS.
