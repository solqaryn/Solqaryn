# C15-02.M05.06 - regresion, rendimiento y accesibilidad de arquitectura (V22)

## Contrato atomico del PLAN D
Ejecutar regresion, rendimiento y accesibilidad proporcionales de la superficie de revision de arquitectura de plataforma en dev. Esta prueba no cierra todas las exigencias acumuladas M05 ni autoriza Hostinger, Cloudflare, R2, MySQL productivo, DNS o pagos.

## Impacto y riesgo
R2 por prueba de superficie privada/guard de plataforma y configuracion CI. Cambios: un spec Playwright en frontend/e2e, un workflow limitado a dev y esta evidencia. Sin cambios a runtime Angular, API, DTO, permisos, base de datos, migraciones, secretos o proveedores. Rollback: git revert normal del commit, preservando historial, hojas y BITACORA. Pruebas en runner GitHub-hosted ubuntu-latest y Angular local loopback sin servicios de terceros.

## Matriz A0-A4 por requisito real
- A0: diff test-only, sin cambios de seguridad ni infraestructura; 5 tarjetas locales siguen marcadas NO_VERIFICADO.
- A1: Chromium real cubre 320/375/768/1280 sin overflow horizontal ni POST/PUT/PATCH/DELETE, con recarga, navegación teclado Tab/Enter y 403 sin fuga de diagnosticos.
- A2: guard de UI cubierto por respuesta 403 sintética. Esto NO es autenticacion JWT firmada ni aislamiento backend; pruebas reales JWT/MySQL pertenecen a microtareas previas y no se reutilizan como propia evidencia nueva.
- A3: DOM, focus, rendimiento navigation DCL y FCP de laboratorio desde Chromium real. LCP/CLS/INP de usuarios reales = NO_MEDIDO; WCAG integral/contraste automatizado y dispositivos fisicos no certificados.
- A4: exigir Scope Lock, V5/procedencia, frontend tipado y build y este workflow completados SUCCESS en SHA causal exacto, readback PLAN/COLA/BITACORA. Los checks SKIPPED/CANCELLED/FAIL/IN_PROGRESS requeridos no acreditan PASS.
- A5: revision independiente por una Task Javier distinta del autor y del reparador, posterior al LISTO A0-A4.

Ningun estado se certifica mientras falte evidencia causal. No se ejecuta QA, main, PROD ni secretos externos. La pantalla es inventario local, no verificacion de proveedores productivos.
