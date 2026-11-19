# SOLQARYN V17 — C03-04.M04.05 accesibilidad de respuesta a incidentes

**Scope:** fila 564 PLAN_MAESTRO, LOCK SQV17_C03_04_M04_05 / 2000000564. **Owner/autor:** PRIMARY DEV03 6adc2e4bd8a48191ac7d8641ed95a1a3. **Riesgo:** R2 por contenido de incidentes, permisos y límites de confianza. **Rama:** solqaryn/Solqaryn@dev.

## A0 — alcance y rollback
Se mejora la superficie privada existente sin cambiar rutas, guards, backend, datos ni providers. El enlace de salto visible al enfocar lleva al título con tabindex=-1; el enlace de retorno aumenta a 44px; el layout adapta listas a móviles de 320px, envuelve texto largo y respeta tokens Material en contraste normal, forced-colors y prefers-contrast. Se conserva la guía protegida como solo lectura: cero formularios, acciones de restauración, endpoints o secretos. Rollback: git revert normal del changeset, tras leer HEAD.

## A1/A2/A3 — verificaciones pertinentes
Cuatro Vitest validan navegación semántica, anónimo, tenant empresarial rechazado y revocación. Cuatro pruebas Playwright Chromium comprueban teclado/anchor, viewport 320/375/768/1440, forced-colors + reduced-motion y 403 sintético. Se conserva el workflow anterior M04.01 para regresión. Los tests sintéticos NO acreditan JWT, BOLA, multitenant backend A/B, MySQL, restauración, RTO/RPO ni mediciones productivas; ningún backend/EF/proveedor cambia, por lo que pruebas de integración MySQL son NO_APLICA al diff.

## A4/A5 — evidencia exigida
GitHub-hosted debe ejecutar Vitest, Chromium, lint/build, Scope Lock y V5 en SOURCE_SHA exacto (o descendiente con blobs y dependencias equivalentes). Si hay queued/in_progress/failure, conservar VALIDANDO, ID, owner, RUN/sha y readback. LISTO solo tras checks requeridos completed/success y PLAN/COLA/BITACORA readback. A5 exclusivamente por otra Task Javier con revisión independiente; nunca autocertificar.

## Corrección causal V20 — A5_FAIL P2 / 18 noviembre 2026

DEV05 (TASK_ID 6adc2e55f47c81919f6374e561687076) corrige exclusivamente el **oráculo de pruebas**, no la UI ni el runtime de respuesta a incidentes. El A5_FAIL original se conserva. En Chromium, el skip-link se localiza mediante pulsaciones Tab reales desde el foco inicial, sin .focus(); Enter debe mover el foco al H1 dentro de la ruta SPA. El contraste en forced-colors se calcula a partir de luminancia sRGB de color y fondo computados, con umbral >= 3:1 y rechazo de valores transparentes/no parseables. Los casos 403, responsive y Vitest existentes permanecen sin degradación.

Riesgo R2 por superficie privada, aunque el diff solo afecta pruebas. Exigir nuevo SHA con 4/4 Playwright, 4/4 Vitest, lint/build, Scope Lock y V5 pertinentes. El job de MySQL no aplica a este diff sin cambios de EF/DB. JWT firmado/tenant A-B reales siguen NO_MEDIDO: los fixtures sintéticos no autorizan producción ni certifican seguridad backend. Revisión A5 futura por una Task Javier distinta de DEV03 autor y DEV05 reparador. Rollback git revert sin force-push.
