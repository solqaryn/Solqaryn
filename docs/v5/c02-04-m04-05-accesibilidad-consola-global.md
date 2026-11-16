# SOLQARYN V17 — C02-04.M04.05 accesibilidad de consola global
Autor PRIMARY DEV 03 JAVIER; TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3, Scope Lock STRICT, dev y PLAN fila 420, V17 lock 2000000420.

## A0 / R2 y rollback
Consola global /plataforma/consola-global-v5 ya protegida por plataformaGuard. Solo se añaden pruebas E2E y se amplía el workflow SHA-aislado ya existente. Sin modificar componente, rutas, permisos JWT/RBAC, IAM, tenants, datos, endpoints, base MySQL, migraciones ni operación de producción. Rollback git revert, sin force-push.

## A1, A2 y A3 dirigidas
Cinco Playwright/Chromium: anónimo, 403 sin payload privado, 320/375/768/1280 sin overflow, navegación etiquetada, keyboard Tab/foco con blanco 44px y contraste real calculado >=4.5:1 del texto sobre tarjeta. Las pruebas fallan ante acceso no autorizado, pérdida de semántica, problemas responsive o contraste. La sesión de test es sintética (no JWT real); no se certifica MFA, grants, revocación backend, tenant A/B HTTP ni auditoría productiva. Integración pertinente: Angular build/lint y Chromium GitHub-hosted; MySQL/EF/proveedores NO_APLICA a este diff.

## A4 y A5
CI exact SHA y logs obligatorios, Scope Lock/V5, readback PLAN/COLA/DAG/BITACORA y revisión A5 por otra Task Javier. Nunca usar skipped/cancelled/queued como PASS. Sin QA/main/PROD.

## Reparación de A5_FAIL P2: ruta de teclado real (16 de noviembre de 2026)

La revisión independiente detectó que el test inicial empleaba `await link.focus()` sobre el enlace de retorno, lo que podía ocultar que fuera inalcanzable desde una secuencia de Tab real. No se comprobó una vulnerabilidad efectiva de acceso o un fallo de usuario final; la brecha era la calidad de la prueba.

La regresión sustituye el foco forzado por una búsqueda acotada de 100 pulsaciones Tab desde el foco inicial, exige que el enlace sea alcanzable, mantiene la transición Tab a Revalidar y sus propiedades de foco/blanco mínimo de 44 px, vuelve mediante Shift+Tab y activa el enlace con Enter verificando que abandona la ruta. Este assert es capaz de fallar si el enlace se retira del orden de tabulación o Enter no navega. Los cuatro escenarios restantes de denegación, labels, responsive y ratio de contraste >=4.5:1 se conservan.

R2 por ruta de consola global privada; cambia sólo pruebas, documento y CHANGELOG, no implementación, IAM/JWT/RBAC, tenant, API, MySQL ni producción. Requiere Vitest y Chromium GitHub-hosted del SHA reparado, Scope Lock/V5 y A5 nueva por otra Task Javier, preservando el A5_FAIL histórico. Rollback `git revert` ordinario, dev exclusivamente.
