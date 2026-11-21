# C06-01.M06.06 — Entrada pública y cierre condicionado del lote SaaS (V23)

Contrato: PLAN_MAESTRO!D901 «Cerrar estado, liberar el siguiente lote y actualizar el mapa de dependencias». PLAN!E901 es meta acumulativa de fase; no acredita por sí sola pricing, onboarding, legal, SEO, contratación, QA ni producción. Único repositorio solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. Riesgo R1 funcional Angular con verificación A2 del guard privado. Fecha: 21-11-2026.

## Defecto causal y corrección

AUDIT_UI_BRECHAS UI-03 (P1/PARCIAL) registró que la URL raíz / enviaba al formulario /login y ocultaba la presentación institucional que ya estaba implementada en /conoce-solqaryn. El cambio mínimo reemplaza exclusivamente el redirect público exact-match de / hacia /conoce-solqaryn; /login sigue explícito y /dashboard conserva authGuard + permisoGuard. No se alteran las 99 API, JWT, roles, pertenencias tenant, datos, pagos, tiendas ni seguridad del servidor. No se reabre ningún ID LISTO.

El expediente nuevo incluye Playwright/Chromium real GitHub-hosted para / sin sesión, CTA a /login, denegación de /dashboard sin sesión, ausencia de llamadas privadas en la landing, captura en 320/390/768/1280/1440 px, sin overflow horizontal, 1 main/h1, Tab/Enter y reduced motion. Capturas posteriores del root bajo el nuevo SHA; la prueba visual ANTES del root no está capturada: la evidencia previa es el diff de app.routes.ts en el SHA padre, que apuntaba a login. No afirmar captura «antes» inexistente ni comparación pixel a pixel.

## Puertas y decisiones de liberación

- A0: inspeccionar diff exacto, PLAN H/I/P:S, DAG F=VALIDADO/G=SI, COLA G/O=SI; dependencia M05.01-06 + M06.01-05 A5 independientes como readback; rollback git revert normal, sin reset/force push.
- A1: tres tests reales: página pública, guard anónimo, cinco viewports/foco, con errores que rompen asserts. Cualquier fallo conserva VALIDANDO.
- A2: enlace a /login y authGuard/permisoGuard sin cambios, /dashboard anónimo debe caer en /login. JWT/RBAC tenant A/B real NO_APLICA al cambio de redirect (no toca runtime de servidor), y queda sin certificar para el producto completo.
- A3: Angular 22, Playwright/Chromium real en CI. MySQL/HTTP privados y dispositivos físicos NO_APLICA para este redirect. No presentar fixture /health ni mocks como integración MySQL.
- A4: requeridos verify-solqaryn-project-scope, V5 CHANGELOG, Angular build, nuevo workflow Chromium, seguridad/regresión dev causal en SHA exacto completed/success; no convertir in_progress/cancelled/skipped en PASS. READBACK PLAN/COLA/BITACORA tras transición bajo V17.
- A5: tercera Task JAVIER autorizada y distinta del autor DEV03 para revisión genuina del diff, tests, P1 UI-03, CI y riesgos. LISTO por A0–A4 no es A5_PASS ni permiso QA/main/PROD.

UI-03 conserva etiqueta P1/PARCIAL para pricing, registro/onboarding, host y publicación comercial real todavía no demostrados. La mejora raíz aborda una subcausa, no certificar UI-03 COMPLETA ni toda fase E901 hasta evidencia separada. Las fórmulas de DAG/COLA se leen después de cierres verdaderos, nunca se fuerzan. Si una P1 causal impide el cierre de D901, mantener VALIDANDO y reparar antes de liberar sucesoras.

Routing V23 según diff: SKREF-03 Impeccable original pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199; SKREF-05 Taste original Leonxlnx/taste-skill@ccbc15639c97057cbfcf32ecebc38ef716e4bb37; SKREF-10 Angular Developer original angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9; SKREF-06 Humanizer original blader/humanizer@9862685f575c65a8247f90369951df1b3416e3d6 para documentación. SKREF-04 (no modifica motion), SKREF-11 (sin threat model), SKREF-12 (sin profiling .NET): NO_APLICA. Ninguna referencia instala paquetes, copia scripts o reemplaza Angular Material existente.

## Reparación causal de CI compartida (V23, source SHA eb02b1ba76b62a19e15b74a245ab5c1bbd04bcb3)

GitHub Actions check C15-01.M05.04 `source audit and adversarial cases` falló en job 137022389758 por `route hash drift frontend/src/app/app.routes.ts` después de la corrección autorizada de la raíz. La foto histórica de PLAN/COLA/DAG C15-01.M05.04 permanece NON_AUTHORITATIVE, con captura UTC original intacta; el manifiesto `docs/v5/evidence/c15-01-m05-04-snapshot.json` revalida por separado el hash git blob real `3181fab58be23bd22eab741a0edb73baa01e6a92` y 120 declaraciones actuales (116 en el corte histórico), mientras exige fingerprints idénticos para las otras tres rutas. Se conserva la detección fail-closed ante cualquier nueva modificación no revisada. No se altera H/I de C15-01.M05.04 ni se borra fallo previo; reparacion CI para el mismo scope C06-01.M06.06, con nuevos checks exact-SHA obligatorios.

## Ajuste de prueba de semántica heredada (evidencia exacta)

Otro check transversal del SHA `eb02b1ba` falló: C15-01.M04.01, job 137022389857, 14/15 pruebas de Chromium superadas; una aserción heredada buscaba `main[aria-labelledby="coverage-title"]` cuando el componente existente representa esa región con `section.coverage-shell[aria-labelledby="coverage-title"]`. Se corrigió el selector de una prueba, preservando el mismo count=1, el aria-labelledby, los demás 14 casos y la prohibición de revelar datos privados; no se cambia runtime, permisos ni ninguna ruta protegida. La CI dirigida de esa suite debe volver a ejecutar exactamente los 15 casos en GitHub-hosted. No se presume PASS de test pendiente.
