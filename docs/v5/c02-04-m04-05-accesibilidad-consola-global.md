# SOLQARYN V17 — C02-04.M04.05 accesibilidad de consola global
Autor PRIMARY DEV 03 JAVIER; TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3, Scope Lock STRICT, dev y PLAN fila 420, V17 lock 2000000420.

## A0 / R2 y rollback
Consola global /plataforma/consola-global-v5 ya protegida por plataformaGuard. Solo se añaden pruebas E2E y se amplía el workflow SHA-aislado ya existente. Sin modificar componente, rutas, permisos JWT/RBAC, IAM, tenants, datos, endpoints, base MySQL, migraciones ni operación de producción. Rollback git revert, sin force-push.

## A1, A2 y A3 dirigidas
Cinco Playwright/Chromium: anónimo, 403 sin payload privado, 320/375/768/1280 sin overflow, navegación etiquetada, keyboard Tab/foco con blanco 44px y contraste real calculado >=4.5:1 del texto sobre tarjeta. Las pruebas fallan ante acceso no autorizado, pérdida de semántica, problemas responsive o contraste. La sesión de test es sintética (no JWT real); no se certifica MFA, grants, revocación backend, tenant A/B HTTP ni auditoría productiva. Integración pertinente: Angular build/lint y Chromium GitHub-hosted; MySQL/EF/proveedores NO_APLICA a este diff.

## A4 y A5
CI exact SHA y logs obligatorios, Scope Lock/V5, readback PLAN/COLA/DAG/BITACORA y revisión A5 por otra Task Javier. Nunca usar skipped/cancelled/queued como PASS. Sin QA/main/PROD.
