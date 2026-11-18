# C14-01.M04.05 — Accesibilidad responsive y teclado

## A0 / riesgo R1
Cuatro E2E Playwright de login publico y rechazo anonimo de QA privado. Nuevos archivos de test, workflow, evidencia y changelog. Sin cambiar codigo UI compartido en revision DEV05, API, JWT/RBAC/tenant, MySQL, secretos ni QA/main/PROD. Reversion git revert normal.

## A1 funcional positivo, negativo, frontera y error
Anchos 320/375/768/1280 sin overflow; nombres accesibles para campos; submit invalido bloqueado. Tab real desde inicio, foco visible, hitbox al menos 44 px, Space muestra/oculta contrasena sin POST. Contraste del titulo >=4.5 WCAG AA con cota inferior conservadora calculada contra fondo negro/blanco si el panel es translúcido. Un color CSS no reconocido causa fallo (no SKIP). Ruta QA privada anonima redirige sin revelar panel.

## A2 seguridad / no-alcance
Sin mocks HTTP, JWT sintético, credenciales ni datos tenant. El rechazo de Angular no certifica autoridad backend; comprobar JWT/RBAC/tenant vivo es NO_APLICA a este changeset (no cambia esa capa). No se aprueba QA/main/PROD.

## A3 integración causal
CI GitHub-hosted Angular 22.2.1 + Chromium real usando frontend en 127.0.0.1:4200 y cuatro pruebas. MySQL/EF y API autenticada NO_APLICA: no se modifican. Este incremento no pretende certificacion global de todos los flujos ERP.

## A4/A5 y rollback
Exigir Scope Lock y V5 y job Chromium completed/success exact-SHA; QUEUED/IN_PROGRESS/SKIPPED/CANCELLED no PASS. H=VALIDANDO hasta pruebas y aceptación suficientes; A5 revisión independiente de otra Task Javier. Readback en PLAN/COLA/DAG y BITACORA, liberar V17 solo tras prueba causal. Git revert de dev sin force-push.
