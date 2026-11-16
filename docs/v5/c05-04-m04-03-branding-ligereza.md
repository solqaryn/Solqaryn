# C05-04.M04.03 — branding local allowlisted y componentes privados

Autor: PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Riesgo V15 R1. dev, Scope Lock STRICT, PLAN fila 850, V17 lock 2000000850.

## A0 — alcance
Se implementa en /plataforma un control visual dinámico entre exactamente dos marcas locales allowlisted (Institucional/Contraste alto) usando Angular Signals, botones Material, foco y aria-pressed. No se acepta CSS arbitrario, entrada externa, configuraciones remotas de tenants, secretos ni cambios persistentes. La acción requiere sesión de administrador Plataforma pero no otorga privilegios nuevos. Sin nuevas API/DTO, llamadas, integraciones, EF, MySQL ni migraciones.

## A1 — pruebas negativas/positivas
GitHub-hosted Playwright Chromium: anónimo y 403 no muestran controles ni errores sensibles, alternancia/segundo clic no duplican acciones, opciones exactamente dos, teclado, responsive 320/375/768/1280. Aserciones efectivas fallan ante defectos de estado, seguridad visual o interacción; las sesiones HTTP son sintéticas, no JWT real.

## A2/A3 — seguridad e integración
Guard y autorización backend continúan como frontera efectiva; comprobación UI fail-closed sin sesión válida; tokens visuales estáticos sin cross-tenant. CI Angular lint/build y Chromium en runner GitHub, SHA exacto. Backend JWT/MySQL/EF y Core Web Vitals reales NO_APLICA/NO_MEDIDO porque no cambian subsistemas ni se probaron en vivo.

## A4/A5 — estado y rollback
Validar diff, blobs, Scope Lock, procedencia V5, frontend y Chromium SUCCESS exact-SHA antes de LISTO. Queued/in_progress no es PASS; conservar VALIDANDO/owner/SHA/RUN hasta pruebas. A5 independiente por Task Javier distinta. Rollback mediante git revert normal; no QA/main/PROD.

## Reparación CI causada por incorporación del estado de branding — 16 de noviembre de 2026
GitHub Actions `C05-04.M04.01 - Angular build y Chromium` falló en SHA `144819e8e22ecfe1735af1d5f5b6be41b4abe64c`, job `133489542420`: la aserción `getByRole('status')` pasó a resolver dos nodos por el nuevo estado de branding introducido en M04.03. La corrección es estrictamente del selector de la prueba de M04.01: se delimita a `.ui-lightness-v5__status` y se mantiene el texto esperado «tres criterios», el uso real de teclado y la prohibición de escrituras HTTP. No se modifica producto ni permisos. Clasificación R2 por CI compartida, P2 de prueba; no se acredita PASS hasta rerun exact-SHA de Chromium, frontend, Scope Lock y V5. Autor/reparador DEV04; A5 independiente pendiente, sin QA/main/PROD. Rollback mediante git revert normal.
