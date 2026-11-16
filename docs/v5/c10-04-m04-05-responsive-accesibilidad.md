# SOLQARYN V17 — C10-04.M04.05: responsive, teclado, foco, etiquetas y contraste en continuidad privada

**Task autora:** PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`). **Riesgo:** R2 por superficie de continuidad restringida, aunque las pruebas solo usan sesiones sintéticas. **Fuente única:** STANDARD V5 FINAL, PLAN fila 1572, COLA/DAG dinámicos. **Repo:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Lock V17:** `2000001572`.

## A0: alcance y límites

La pantalla existente `continuidad-operativa-v5.component.ts` ya tiene una ruta `plataformaGuard` y estados fail-closed. Este alcance agrega el etiquetado explícito de la lista de cuatro controles mediante `aria-labelledby`, protege párrafos en 320 px con `overflow-wrap:anywhere` y refuerza la visibilidad del contorno al solicitar alto contraste. No cambia guard, permisos, app.routes, servicios, APIs, datos, colores empresariales, sesión ni RBAC; tampoco incorpora CSS externo.

La prueba nueva `c10-04-m04-05-continuidad-a11y.spec.ts` usa Chromium real con token y respuestas de sesión SINTÉTICAS. Comprueba anónimo, 403, 320/768/1280, labels, ausencia de overflow horizontal, foco y contorno visible, media-query de contraste fuerte, controles sin formularios o mutaciones, y 503 sin filtración de payload sensible. La prueba de CSS de foco no es una auditoría automática integral de WCAG ni de contraste de todas las combinaciones.

## A1/A2: rechazo y regresiones

Cinco escenarios reproducibles positivos, negativos, frontera de tamaños y error. La guía solo debe renderizar cuatro controles cuando el guard de plataforma acepta la sesión; ante 401/403/503 no se muestran controles ni secretos. Se preservan las pruebas existentes de M04.01–04 (revocación y single-flight). No inferir aislamiento tenant A/B, JWT real, restore, rate limiting, aprobación o acceso de plataforma desde mock de navegador.

## A3: integración real proporcional

GitHub Actions instala Node 24/npm 11 y dependencias reproducibles; ejecuta Angular lint/build y Playwright/Chromium real en localhost con mocks. Los checks son obligatorios del SHA publicado, incluyendo Scope Lock, V5/procedencia, frontend y el workflow dirigido. MySQL 8.4, EF Core, API autenticada, proveedores, infraestructura y datos reales: NO_APLICA porque no se tocaron ni se prueban.

## A4/A5: evidencia y rollback

Commit aditivo fast-forward esperado, cinco archivos: componente, E2E, workflow, documento y CHANGELOG autocontenido. Confirmar `completed/success` de checks requeridos exact-SHA antes de LISTO_DEV; en queued/in_progress mantener VALIDANDO con owner y RUN. Registrar BITACORA única, readback PLAN/COLA/DAG y liberar lock V17 tras transición. La revisión A5 la realiza otra Task Javier diferente, con posibilidad A5_FAIL. Revertir por `git revert` ordinario sin force-push; QA/main/PROD no autorizados.
