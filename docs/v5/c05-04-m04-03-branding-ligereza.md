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
