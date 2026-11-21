# C15-04.M05.04 — A0.87: errores de sesion 500/503 (DEV, 2026-11-21)

**Microtarea existente:** C15-04.M05.04; WIP unico DEV02, owner historico 6af16bb9492c8191b8e8c7d606f11507; candado V17 2000002297 previamente observado. **Riesgo R2 (sesion privada), cambio exclusivamente en prueba Playwright y evidencia; no modifica runtime ni configuracion productiva.**

**Fallo que vigilar:** La ruta privada `/plataforma/auditoria-ux-v5` no debe mostrar las cinco superficies de auditoria ni reenviar el cuerpo `PRIVATE_ERROR_DO_NOT_RENDER` cuando `/plataforma/sesion` devuelve HTTP 500 o 503. La prueba comprueba cuatro navegaciones Chromium (375 y 1280 px), cero requests HTTP mutantes, y ausencia de overflow horizontal. Son casos negativos y frontera frente a fallo de backend; no se altera la seguridad ni se convierte un 500/503 en acceso autorizado.

**Limitacion:** La respuesta de sesion es simulada con Playwright `page.route` y token local sintetico, como en el helper existente. Se trata de regresion UI/guard y **NO** certifica JWT firmado, RBAC real, MySQL, HTTP de produccion, WCAG global, 160 rutas/roles, ni Core Web Vitals de campo. A3 HTTP/JWT/MySQL: **NO_APLICA a este cambio de prueba**, no PASS del resto de la fase. Las integraciones reales previas tienen evidencia separada.

**A0:** cambio de un spec del WIP existente, sin interfaz ni migracion. **A1/A2:** aserciones negativas de fuga de datos, acceso privado, request mutante y responsive. **A3:** Chromium real exigido en GitHub-hosted; cuenta solo cuando el job ejecute 24 casos sin fallos. **A4:** verificar checks obligatorios Scope Lock, V5, Angular y CI causal en SHA exacto, sin aceptar in_progress/skipped/failed. **A5:** otra Task Javier distinta de DEV02 y de reparador cuando el ID alcance LISTO.

**Rollback:** nuevo commit de revert bajo CAS si se detecta una regresion, nunca force-push. **No declarar LISTO**: mantener H2297 EN_PROGRESO, I=DEV02 y candado propio. No tocar QA/main/PROD, secretos, permisos ni DAG.