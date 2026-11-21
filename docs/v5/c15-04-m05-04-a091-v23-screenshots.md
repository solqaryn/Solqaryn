# C15-04.M05.04 - A0.91: capturas reproducibles del storefront publico
Fecha: 2026-11-21. SOLQARYN dev. PROJECT_SCOPE_LOCK=STRICT.
TASK_ID DEV02: 6af16bb9492c8191b8e8c7d606f11507. WIP existente H2297=EN_PROGRESO, I2297 historico DEV02; candado propio V17 SQV17_C15_04_M05_04 (namedRangeId 2000002297). No nuevo CLAIM.

## A0 - impacto y precondicion
Incremento R1 test-only dentro de auditoria global R2: tres rutas publicas /tienda, /tienda/productos, /tienda/categorias; cinco anchos 320, 390, 768, 1280 y 1440. Se ejecuta navegador Angular real con Tab y aserciones h1, destino de skip-link, foco y cero body overflow; ahora guarda 15 capturas PNG fullPage tras las aserciones. Cada captura exige bytes no triviales; fallo en prueba o falta de evidencia impide certificar el incremento. No modifica Angular runtime, componentes, roles, MySQL, API o datos.

## A1-A4 - verificacion causal
El job .github/workflows/v19-c15-04-m05-04-global-ux.yml debe ejecutar 26 pruebas Playwright previas + nuevos PNG; nombre de job sin conteo fijo. Job GitHub-hosted de Chromium, Angular lint/build, Scope Lock y V5/procedencia del SHA nuevo deben estar COMPLETED SUCCESS; consultar logs y artefacto c1504-m0504-v23-a091-screenshots, exigir 15 PNG reales, antes de declarar PASS. Evidencia previa A0.90: job 136969815254 del SHA 358bd22dab5b19d1028d1d81bd1a212f81909767 completo SUCCESS, 26/26 Playwright y check Scope Lock/V5; esta evidencia NO certifica el SHA nuevo. Fotos nuevas solo DESPUES: no existe baseline antes con igual artefacto; no afirmar comparacion visual antes/despues.

## Alcance excluido
Capturas son PUBLICAS y sin credenciales; 200% zoom real, auditoria visual completa, 160 rutas y combinaciones rol/tenant, CWV de campo y WCAG global NO_MEDIDO, no PASS. A2 JWT/RBAC y MySQL no aplican a este diff test-only, pero siguen necesarios para el scope global C15-04.M05.04. A5 exclusivamente otra Task Javier distinta del autor tras cerrar A0-A4 completos. No cambiar H/I, no reclamar nuevo ID con WIP, no QA/main/PROD.

## Skills y rollback
Aplicadas solo referencias originales ACTIVE + PIN: SKREF-03 Impeccable pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199; SKREF-05 Taste Leonxlnx/taste-skill@ccbc15639c97057cbfcf32ecebc38ef716e4bb37; SKREF-10 Angular angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9. SKREF-04/06/11/12 NO_APLICA al diff, sin motion/copy/threat model/perf backend. Sin copiar skills, sin dependencias adicionales. Rollback git revert no destructivo del commit si defecto, conservar evidencia histórica.
