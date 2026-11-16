# SOLQARYN V17 — C03-04.M04.04 verificación de navegación y doble acción

**Unidad:** C03-04.M04.04, PLAN_MAESTRO fila 563. **Autor:** DEV02 Javier, `6af16bb9492c8191b8e8c7d606f11507`. **Rama:** `solqaryn/Solqaryn@dev`; **candado:** `SQV17_C03_04_M04_04` / `2000000563`; **riesgo:** R2 por pantalla administrativa privada, aunque los cambios son exclusivamente pruebas y documentación.

## A0: alcance y fronteras

Verificar el contrato vigente de Respuesta a Incidentes: ruta lazy protegida por `plataformaGuard`, retorno navegable a `/plataforma`, ausencia de formularios o comandos de restore/revoke, mensajes genéricos sin secreto y prevención de doble petición de revalidación. No ampliar operaciones del producto, controles de permisos, endpoints, API/DTO, servicios backend, JWT real, RBAC, MySQL/EF, migraciones o integraciones. El navegador usa sesiones simuladas; no se atribuye autenticación real de API. Rollback: `git revert` convencional del commit.

## A1-A3: pruebas causales y límites

Vitest: doble activación directa del botón produce una única solicitud pendiente y mantiene oculta la guía; después del éxito muestra cuatro decisiones. El link de retorno apunta a `/plataforma`, no existen formularios ni submit, y la revocación reactiva retira los controles. Chromium: navegación positiva; otro caso simula HTTP 429 retardado, intenta doble click, exige un solo GET, oculta decisiones y no muestra diagnóstico interno. Se cubren positivos, negativos, frontera de concurrencia y error. 403, 503, responsive y accesibilidad siguen en suites preexistentes. A2: pruebas sintéticas de rechazo y ocultamiento; NO certifican JWT ni tenant A/B en API real. A3: Playwright/Chromium real en GitHub-hosted CI contra API simulada, sin integración real de MySQL, proveedor, backup ni restore; NO_APLICA a este diff de pruebas exclusivamente.

## A4-A5: publicación, evidencia y autoridad

Exigir commit fast-forward condicionado a HEAD de dev, `verify-solqaryn-project-scope`, procedencia V5, workflow dirigido `v17-c03-04-m04-01-incidentes.yml` con Vitest+Chromium, frontend/seguridad aplicables y aceptación integral causal. `QUEUED`, `IN_PROGRESS`, `SKIPPED`, `CANCELLED` y `FAILURE` obligatorios no son PASS. Registrar SHA, RUN_ID externo, evidencias y readback de PLAN/COLA/DAG/BITACORA. LISTO DEV sólo con A0-A4 completos; A5_PENDIENTE corresponde a otra Task Javier. Nunca QA/main/PROD.
