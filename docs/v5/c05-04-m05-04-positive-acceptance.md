# C05-04.M05.04 - aceptacion positiva integradora de ligereza

Autora: PRIMARY DEV 04, Task ID 6aeacb7820ac8191b7d768a6f33dfa09. Unica rama dev, Scope Lock STRICT, PLAN fila 857. Riesgo R2 por nueva CI GitHub Actions, sin cambiar runtime.

## A0 - alcance y rollback

Se agrega Playwright/Chromium E2E real para verificar conjuntamente directorio sintetico 200, listado 25, alternancia compacta, los dos presets de branding locales, ausencia de solicitudes duplicadas, cero HTTP de escritura y viewport 320/1280; no duplica componente ni activa módulos. Incluye respuesta 200 vacía, denegación sintética 403 y error 503 opaco. Rollback por git revert normal; se mantienen intactas API, JWT, RBAC, MySQL, la cuenta y el plan.

## A1/A2 - aserciones falsables y seguridad

Caso positivo: 25 registros y estado exito con GET exactamente uno aunque se alternen ambos controles; ver 25 filas, ausencia de overflow. Casos de frontera y error: 0 filas con mensaje honesto, 403 sin surface ni detalles y 503 sin éxito falso ni fuga del proveedor. Todos fallan ante regresiones reales. Autorización y tenant HTTP son fixtures: no constituyen PASS de seguridad JWT/RBAC/MySQL productiva, ni certifican permisos de otra empresa.

## A3/A4 - CI real

Angular lint/build y cuatro tests Chromium se ejecutan realmente en GitHub-hosted, con grupo concurrency por SHA y cancel-in-progress=false. Requerir completed/success del workflow dirigido, Scope Lock, V5 y checks pertinentes SHA exacto; in_progress, cancelled, failed, skipped no son PASS. LCP/CLS de usuarios reales NO_MEDIDO. No se realizaron tests MySQL/EF o proveedores porque no hay diff en persistencia, contrato ni datos, NO_APLICA motivado.

## A5 y handoff

Revisor independiente debe ser otra de las cinco Tasks Javier, con inspección de código/fixtures, logs y SHA. Registrar estado real en PLAN/COLA/DAG/BITACORA bajo candado V17. Sin QA/main/PROD ni credenciales nuevas.
