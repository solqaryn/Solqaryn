# C02-04.M04.03 — marca dinámica local allowlisted del control plane (DEV)

Autor: PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Riesgo V15 R2 por frontera del control plane; PROJECT_SCOPE_LOCK=STRICT, solqaryn/Solqaryn@dev, PLAN fila 418, candado V17 2000000418.

## A0 — requisito y alcance
Añadir branding dinámico, tipado y LOCAL entre dos presentaciones institucional y contraste alto sólo en la consola privada global existente. El cambio comprende HTML/CSS/component Angular, pruebas Chromium, workflow dirigido, documento y CHANGELOG. Prohibido obtener CSS remoto, publicar nombres de tenant, usar esta preferencia como autorización, guardar datos o añadir permisos. No nuevos DTO, API, HTTP de escritura, grants, migraciones, MySQL ni proveedores. El guard de ruta y autorización del backend no cambian. El estado se mantiene en un Angular Signal local, con lista fija y botones accesibles.

## A1 — pruebas verdaderas y límites
Playwright GitHub-hosted con fixture HTTP sintética: anónimo y sesión 403 denegados; sesión permitida presenta exactamente dos botones; alternar dos veces sin HTTP adicional; teclado Enter; responsive 320/375/768/1280, denegación al revocar tras revalidar y ausencia de mensajes privados del servidor. Verificar asserts capaces de fallar ante defecto y resultados de CI por SHA exacto; no atribuirles PASS antes del job.

## A2 — frontera seguridad / tenant
SeleccionarMarca sólo permite IDs del allowlist, accesoVerificado y estadoVisible permitido. Sesión denegada/cargando/error oculta el panel. No se afecta JWT real, MFA, RBAC backend, cross-tenant ni secretos. Tests son de UI con respuestas sintéticas: no certifican autorización HTTP real, MySQL ni integración de proveedores.

## A3/A4 — integración y revisión
npm lint/build Angular 22.2.1, suite Vitest existente y Chromium Playwright en GitHub Actions. Scope Lock y procedencia V5 exact-SHA. API/EF/MySQL real NO_APLICA porque no se modifican, Core Web Vitals NO_MEDIDO. Mantener VALIDANDO con SHA/RUN, propietario e historial cuando CI externa QUEUED/IN_PROGRESS; sólo concluir LISTO tras A0-A4 causal con readback y lock V17. A5 pendiente de otra Task Javier independiente. Revert normal para rollback; ningún cambio QA/main/PROD.
