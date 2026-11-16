# SOLQARYN V17 — C03-04.M04.05 accesibilidad de respuesta a incidentes

**Scope:** fila 564 PLAN_MAESTRO, LOCK SQV17_C03_04_M04_05 / 2000000564. **Owner/autor:** PRIMARY DEV03 6adc2e4bd8a48191ac7d8641ed95a1a3. **Riesgo:** R2 por contenido de incidentes, permisos y límites de confianza. **Rama:** solqaryn/Solqaryn@dev.

## A0 — alcance y rollback
Se mejora la superficie privada existente sin cambiar rutas, guards, backend, datos ni providers. El enlace de salto visible al enfocar lleva al título con tabindex=-1; el enlace de retorno aumenta a 44px; el layout adapta listas a móviles de 320px, envuelve texto largo y respeta tokens Material en contraste normal, forced-colors y prefers-contrast. Se conserva la guía protegida como solo lectura: cero formularios, acciones de restauración, endpoints o secretos. Rollback: git revert normal del changeset, tras leer HEAD.

## A1/A2/A3 — verificaciones pertinentes
Cuatro Vitest validan navegación semántica, anónimo, tenant empresarial rechazado y revocación. Cuatro pruebas Playwright Chromium comprueban teclado/anchor, viewport 320/375/768/1440, forced-colors + reduced-motion y 403 sintético. Se conserva el workflow anterior M04.01 para regresión. Los tests sintéticos NO acreditan JWT, BOLA, multitenant backend A/B, MySQL, restauración, RTO/RPO ni mediciones productivas; ningún backend/EF/proveedor cambia, por lo que pruebas de integración MySQL son NO_APLICA al diff.

## A4/A5 — evidencia exigida
GitHub-hosted debe ejecutar Vitest, Chromium, lint/build, Scope Lock y V5 en SOURCE_SHA exacto (o descendiente con blobs y dependencias equivalentes). Si hay queued/in_progress/failure, conservar VALIDANDO, ID, owner, RUN/sha y readback. LISTO solo tras checks requeridos completed/success y PLAN/COLA/BITACORA readback. A5 exclusivamente por otra Task Javier con revisión independiente; nunca autocertificar.
