# C14-01.M04.01 — Superficie de consulta QA global (V19)

## A0 — alcance y criterios
Ruta privada Angular /plataforma/qa-global-v19 con plataformaGuard y comprobacion adicional del grant servidor Plataforma/esAdministrador. Muestra areas de QA global y enlace a ejecuciones GitHub Actions dev, sin resultados en vivo; no endpoint, token GitHub en navegador, aprobacion humana ni escrituras.

## A1 — positivos, negativos, frontera y error
Vitest: ruta unica/lazy con guard, denegacion sin sesion, vista con grant global, cinco areas NO_VERIFICADO sin botones o formularios. Playwright: usuario anonimo y 403 denegados sin divulgar texto, sesion sintetica sin escrituras, anchos 320/375/768/1280 y Tab real con foco. La sesion sintetica NO acredita JWT firmado.

## A2 — seguridad
El frontend solo aplica UX fail-closed; autoridad real en /plataforma/sesion y en cada servicio backend. No exponer secretos, payload crudo, datos tenant A/B ni simular RBAC backend, MySQL o resultados de tests. La ruta por si sola no autoriza.

## A3 — integracion
CI GitHub-hosted real con Angular 22.2.1, Vitest y Playwright/Chromium. El enlace al proveedor abre GitHub Actions sin llamar Checks API desde el navegador. Integrar lectura automatica de check-runs necesita source trust y scope posterior; NO_VERIFICADO hasta SHA y SUCCESS causal.

## A4, A5 y rollback
Revisar commit exacto, build, Chromium, Scope Lock y V5. Mantener VALIDANDO mientras CI QUEUED/IN_PROGRESS; CANCELLED/SKIPPED no son PASS. BITACORA y PLAN/COLA/DAG readback. A5 solo por otra Task Javier. No QA/main/PROD ni forzar dependencias. Rollback con git revert normal en dev, preservando historial.
