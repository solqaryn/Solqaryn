# C15-04.M05.04 — A0.88: sesion 401/429 sin fuga privada (DEV, 2026-11-21)

**Exclusividad:** ID C15-04.M05.04, Task DEV02 `6af16bb9492c8191b8e8c7d606f11507`, EN_PROGRESO; candado V17 `SQV17_C15_04_M05_04` / `2000002297` comprobado antes de esta edicion. Un solo WIP, sin nuevo CLAIM.

**Hipotesis adversarial R2:** un HTTP 401 o rate-limit HTTP 429 de sesion no autoriza a mostrar `ux-surface`, filtrar `PRIVATE_ERROR_DO_NOT_RENDER` ni iniciar escrituras. El caso de Playwright examina cuatro navegaciones Chromium (status 401/429 por 375/1280 px), DOM, requests y overflow; son aserciones negativas capaces de fallar.

**Limitacion:** helper `auth()` usa `page.route` y token sintetico. El browser es real, pero el rechazo de sesion es simulado. Este cambio test-only NO certifica JWT firmado, RBAC backend, MySQL, HTTP real, WCAG global, 160 rutas/roles ni CWV de campo. A3 HTTP/JWT/MySQL es NO_APLICA a este cambio, no a toda la fase; las pruebas reales previas son evidencia independiente.

**A0–A4:** diff solo test+doc; A1/A2 negativos 401/429 y fronteras responsive; A3 GitHub-hosted Chromium pendiente de ejecutar, no PASS previo; A4 comprobar `verify-solqaryn-project-scope`, V5, Angular y CI dirigida SUCCESS en SHA exacto. Nunca contar SKIPPED/CANCELLED/IN_PROGRESS como PASS requerido. A5 por otra Task Javier distinta del autor y del reparador, solo tras completar toda la microtarea.

**Estado y rollback:** mantener H=EN_PROGRESO, I=DEV02 y candado propio. Revert nuevo commit fast-forward bajo CAS si hay regresion. No tocar DAG/COLA, QA/main/PROD, secretos, permisos ni servicios reales.
