# C14-04.M05.06 — Regresión, teclado, movimiento reducido y presupuesto de humo de readiness DEV

**Corte:** 2026-11-18 · **Proyecto:** SOLQARYN dev, Scope Lock STRICT · **Riesgo del contexto:** R2, aceptación y potencial release, pero **diff sólo de pruebas y documentación**, sin ejecución de despliegue. **Owner DEV03**.

## A0 — Alcance real

Se incorpora un archivo Playwright Chromium con cuatro casos y workflow dirigido GitHub-hosted (Node 24/npm 11, Angular build, Chromium real), reutilizando la vista ya existente `/plataforma/flujo-v5`. Ninguna implementación productiva, autenticación, EF, SQL, JWT/RBAC, API, migración, DNS, Render, Vercel, Cloudflare, Aiven, secretos, flags o datos ha sido editada.

El helper de autenticación usa sesión **interceptada sintética**, claramente **NO JWT firmado**. El objetivo es que la UI no afirme que un release o gate de tenant pasó pruebas que no existen. Las puertas de readiness siguen `3 SIN_VERIFICAR + 1 NO_MEDIDO`, no PASS ni acciones de autorización.

## A1/A2 — Asserts falsables de seguridad visual y accesibilidad

1. Anchuras 320, 768 y 1280 px: título y lista semántica, cuatro puertas, 3 SIN_VERIFICAR, 1 NO_MEDIDO, cero PASS/controles de mutación dentro del panel y desbordamiento horizontal no mayor a 2px.
2. Teclado auténtico mediante **Tab desde el estado inicial** sin llamar `.focus()`: botón de revalidación alcanzable; Enter produce feedback seguro, conserva estado de evidencia vacía y no concede PASS.
3. Con `prefers-reduced-motion: reduce` se verifica que el panel permanece legible y estable con cuatro puertas. Esto **no mide** el cumplimiento WCAG completo ni demuestra una duración CSS específica.
4. Coarse DOMContentLoaded smoke de navegador local (menos de 30 segundos) y panel visible en un máximo de 12 segundos. **No es p95/p99, RPO/RTO, tasa de errores productivos o SLO real**, que siguen NO_MEDIDO.

Los falsos PASS, foco inaccesible, rotura responsive, ausencia de elementos o tardanza fuera del budget hacen fallar los asserts. No se ocultan resultados FAIL ni se ajustan gates con textos.

## A3 — Integración realmente aplicable

Angular se compila y sirve localmente en GitHub-hosted y Playwright ejecuta Chromium real. La sesión HTTP se intercepta, por lo que **API firmada JWT, RBAC real, MySQL/EF tenant A/B, respaldo, soporte y observabilidad productiva son NO_APLICA al diff de tests pero NO_VERIFICADOS para go-live**. No se envían requests al backend productivo ni se toman datos de clientes. No confundir la vista readiness con un smoke real de toda la plataforma.

## A4/A5 y rollback

CI precisa un job dirigido `C14-04.M05.06 - Chromium regression, keyboard and smoke` completed/success sobre SHA exacto y las verificaciones `verify-solqaryn-project-scope`, `V5 - Sin encabezados vacios ni atribucion cruzada`, frontend/seguridad pertinentes. `SKIPPED/CANCELLED/QUEUED/IN_PROGRESS` nunca son PASS. Al publicar se registra VALIDANDO y se libera candado V17, luego LISTO sólo tras A0–A4 acreditados con CAS/readback. A5 revisada independientemente por otra Task Javier distinta de DEV03. Un PASS DEV nunca autoriza QA/main/PROD, release ni infraestructura R3.

Rollback: `git revert` normal del commit de pruebas tras revisar HEAD; BITACORA no se borra y no existe operación de restauración sobre datos vivos.
