# C15-02.M04.05 — accesibilidad y responsive V20

Proyecto SOLQARYN; rama dev; PROJECT_SCOPE_LOCK=STRICT. Actor DEV01, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a.

## A0 Impacto
Regresión de la vista Angular documental privada C15-02.M04.01 existente, sin cambiar backend, API, DTO, JWT/RBAC, datos, migraciones, proveedores ni secretos. Playwright Chromium en GitHub-hosted con Angular efímero loopback. La UI no concede acceso backend.

## Pruebas y puertas
- A1: H1/H2/H3, lista de cinco tarjetas, ausencia de acciones mutantes; teclado Tab y Enter reales sin focus programático, indicador de foco.
- A2: HTTP 403 sintético no expone inventario o mensaje privado; simular sesión global no certifica JWT firmado, roles backend ni tenant A/B real.
- A3: 320/375/768/1280 píxeles sin desborde; objetivo táctil de al menos 44 px; reduced-motion. No significa auditoría WCAG integral.
- A4: exigir GitHub Actions SHA exacto SUCCESS en lint, build, Vitest, Chromium, Scope Lock y V5; SKIPPED, QUEUED, IN_PROGRESS y FAILED no son PASS. Releer PLAN/COLA/DAG/BITACORA.
- A5: revisión independiente por otra Task Javier, no por el autor ni el reparador.

Rollback: git revert normal del commit con verificación del HEAD y sin force-push. No QA, main o PROD.

## Reparación cruzada V21 — contraste medido y oráculo adversarial
Actor de reparación: DEV03 (`6adc2e4bd8a48191ac7d8641ed95a1a3`), autor/owner histórico DEV01 inalterado. Se agrega una prueba Playwright real en Chromium del contraste WCAG AA (4.5:1) del encabezado, identificador institucional, enlace accesible y primer título de tarjeta, tanto a 320 como a 1280 px. El algoritmo compone opacidad alfa y fondos CSS de ancestros en vez de asumir un fondo local opaco; verifica también el control negativo de color sobre el mismo fondo, que debe fallar 4.5:1. Mantiene los cuatro escenarios anteriores de navegación, semántica, privacidad y responsive, sin modificar la aplicación, API, JWT, MySQL, infraestructura, secretos ni datos.

A0: diff test-only + esta documentación + CHANGELOG_AI; sin nuevas rutas, DTO, persistencia ni autorizaciones. A1: contraste real, casos previos y adversarial; A2: preserva denegación 403 de sesión sintética (no certifica JWT firmado o tenant A/B); A3: ejecución Angular efímera/Chromium por GitHub Actions; A4: exigir job Chromium, frontend y Scope Lock/V5 exact SHA `completed/success`, no certificar en progreso/fallido. La validación WCAG integral, contraste de todas las rutas, consola global y ausencia de P0/P1 siguen pendientes; A5 sólo una tercera Task Javier distinta del autor DEV01 y reparador DEV03. No mover de VALIDANDO hasta A0–A4 completos. Rollback: git revert normal de este único changeset después de verificar HEAD, sin force-push.

## V21 reparación cruzada DEV05 — oráculo negativo causal y cinco tarjetas

Defecto QA P2: el oráculo negativo antiguo era `(backL + 0.05)/(backL + 0.05)`, una constante que nunca ejecutaba la función de contraste; por tanto podía aprobar aunque el cálculo real estuviera defectuoso. Esta reparación comparte exactamente el algoritmo de luminancia sRGB y contraste de casos positivos/negativos, evalúa mismo color de fondo, texto invisible alfa cero y referencia blanco/negro 21:1. Amplía la inspección de primer título a cinco encabezados H3 en 320 y 1280 px. Conserva las otras pruebas Chromium, sin modificar API, datos ni runtime.

A0: cambio test/documental en C15-02.M04.05, sin datos, migraciones, privilegios, JWT/RBAC ni comportamiento productivo. A1/A3: probar la regresión real en Playwright/Chromium GitHub-hosted exact-SHA; sin PASS inventado si queued/in_progress/failed. A2: la denegación 403 actual sigue siendo sintética; no certifica sesión JWT/tenant A/B ni WCAG integral. A4: verificar Scope Lock STRICT y V5 en SHA causal, lint, build y workflow dedicado. A5: revisor genuino distinto de DEV01 autor, DEV03 reparador previo y DEV05 reparador actual. Mantener PLAN VALIDANDO hasta A0–A4 completos y sin P0/P1. Rollback mediante git revert normal.
