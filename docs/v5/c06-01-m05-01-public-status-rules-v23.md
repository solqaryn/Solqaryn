# C06-01.M05.01 — Pruebas de reglas de estado público reproducibles V23

SOLQARYN / solqaryn/Solqaryn / dev, PROJECT_SCOPE_LOCK=STRICT. PLAN_MAESTRO D890 exige ejecutar una prueba unitaria **o de regla** con resultado reproducible. E890 define la meta acumulativa M05, no la certificación de toda esa fase. Entrega atómica R1 test-only: suite de reglas Playwright en Angular 22 real sobre /conoce-solqaryn, workflow dirigido GitHub-hosted, configuración aislada y este documento. No se edita runtime ni datos.

## Alcance y criterios comprobables

La suite debe validar al abrir la ruta ausencia de GET automático, y al solicitar expresamente el estado público un único GET de solo lectura sin Authorization/cookie/EmpresaId, en seis escenarios HTTP reales de fixture localhost: 200 listo; 204 vacío; 403 denegado; 404 inexistente; 429 límite; 500 error con mensaje público seguro. Debe rechazar un escenario de fixture inválido (HTTP 400), conservar mensajes sin texto del servidor y recuperar estado al siguiente 200. Debe acreditar con retardo de 450 ms estado loading y botón deshabilitado (anti-doble petición). Son asserts positivos, negativos, de frontera y error capaces de fallar si se rompe la regla.

Dado que el scope afecta superficie pública Angular, registrar diez capturas reales antes/después para 320/390/768/1280/1440, cero scroll horizontal del documento, una etiqueta main/h1, CTA de login preservado, Tab+Enter, contraste alto, reduced motion y zoom Chromium CDP 200%. Sidebar, tablas y modales no aparecen en esta ruta y son NO_APLICA, no se inventan componentes. No se afirma mejora cuantificada de Core Web Vitals, LCP, SEO ni performance.

## Frontera y seguridad

La respuesta /health proviene de fixture HTTP local en GitHub-hosted: comprueba contrato visible, estados y navegación en Chromium real, **no** prueba API productiva, JWT/RBAC de usuarios A/B, MySQL, sesión empresarial, checkout ni aislamiento multitenant en vivo. A2/A3 privado NO_APLICA a este diff test-only y a la acción D890, no a la fase completa. No consultar endpoints ni secretos reales. Rollback por git revert normal, sin force push. No QA/main/PROD.

## Referencias ACTIVE originales y aplicabilidad

- SKREF-03: pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199, .agents/skills/impeccable/SKILL.md — foco, ergonomía y estados.
- SKREF-05: Leonxlnx/taste-skill@ccbc15639c97057cbfcf32ecebc38ef716e4bb37, skills/taste-skill/SKILL.md — experiencia pública coherente sin alterar diseño.
- SKREF-06: blader/humanizer@9862685f575c65a8247f90369951df1b3416e3d6, SKILL.md — mensajes verificables sin promesas excesivas.
- SKREF-10: angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9, skills/dev-skills/angular-developer/SKILL.md — Angular standalone y tests.
- SKREF-04 NO_APLICA: no se implementa motion; SKREF-11 NO_APLICA: no nuevo threat model; SKREF-12 NO_APLICA: no se perfila .NET. No se instalan/copian fuentes externas ni se incorpora otro stack.

## Control A0–A5

A0: PLAN D890/P:S, Git diff exclusivo cuatro archivos nuevos y lock V17 H:I 890, clasificado R1.
A1: cinco pruebas adversariales con estados 200/204/403/404/429/500, fallo 400, anti-duplicado, foco y contención visual.
A2: contrato público sin headers de sesión privada; JWT/RBAC/tenant/MySQL no modificados ni certificados.
A3: Angular + Chromium reales GitHub-hosted con fixture localhost, JUnit y diez PNG.
A4: solo PASS al observar exact-SHA, GitHub Actions dirigido completed/success, Scope Lock V5 exact-SHA, build Angular y readback PLAN/COLA/BITACORA.
A5: revisión independiente por otra Task Javier distinta de DEV04 autora/reparadora, con análisis genuino. A5 pendiente hasta tal revisión. Un CI QUEUED/IN_PROGRESS/SKIPPED/FAIL no es PASS. Toda dificultad se registra causalmente; no reabrir LISTO previas.
