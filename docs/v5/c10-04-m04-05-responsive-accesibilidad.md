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

## Reparación A5_FAIL P2 — foco y alto contraste (2026-11-15)

El revisor DEV02 detectó dos huecos de evidencia causal en el SHA 36c0210d: el test llamaba a button.focus() después de Tab y podía aprobar un control no alcanzable con teclado; la aserción de 3px no detectaba la ausencia de la regla de contraste alto de 4px. No se observó una vulnerabilidad de autorización real. La corrección comprueba hasta 35 pulsaciones Tab, foco DOM verdadero sin método focus(), ancho de contorno mínimo 4px y contraste del contorno sobre fondo >=3:1 en Chromium con prefers-contrast:more. Se emplean Canvas y CanvasText únicamente al enfocar para contraste alto, sin cambiar rutas, guard, sesión, APIs ni permisos.

Riesgo R2 UI; SKREF-10 angular/angular pin 6808966993bf2b2d455d9b6245e960a59a01b9a9 y referencia e2e-testing.md/component-styling.md en pin oficial. Pruebas sintéticas no certifican JWT, DB, HTTP productivo, tenant A/B real ni proveedores. La CI de GitHub-hosted deberá ejecutar Angular lint/build y Chromium causal, Scope Lock y V5 sobre el SHA publicado. Este documento NO acredita PASS antes del resultado de CI. Conservar QA_DEFECT_FOUND y A5_FAIL históricos hasta nueva revisión por otra Task Javier; revertir con git revert normal, nunca force-push, sin QA/main/PROD.

## Diagnóstico de CI de la primera reparación

El SHA 0d021aa8 tuvo 4/5 Chromium PASS, 1 FAIL real en test de alto contraste: el ancho computado fue 3px donde se exigía 4px (job 133076771626). No se convirtió ese resultado a PASS. La corrección circunscrita aplica `!important` únicamente al outline de 4px de los controles enfocables cuando `prefers-contrast: more` está activo, para priorizar la preferencia de accesibilidad frente a reglas generales de foco. La prueba añade aserción verificable de `matchMedia('(prefers-contrast: more)')`; si el navegador no emula esa preferencia, falla explícitamente. Sigue pendiente CI nueva y revisión A5 distinta. No cambia arquitectura, JWT/RBAC, datos ni autorización; rollback git revert normal.
