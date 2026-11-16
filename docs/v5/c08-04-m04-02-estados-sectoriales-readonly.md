# SOLQARYN V18 — C08-04.M04.02 estados de presentación sectorial seguros

**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Repositorio:** solqaryn/Solqaryn@dev; **Scope Lock:** STRICT. **PLAN:** fila 1281, V17 lock `SQV17_C08_04_M04_02` / `2000001281`. **Riesgo:** R1, Angular UI sin nuevo origen de datos.

## A0 — Alcance, interfaces, dependencias y reversión

Se mantiene la ruta privada `/extensiones-sectoriales` creada en M04.01 y su `authGuard` existente. La pantalla ahora ofrece un contrato visual de cinco estados: `cargando`, `vacio`, `error`, `exito` y `sin_permiso`, con área de estado accesible y `aria-busy`. Estados restrictivos no renderizan catálogo; vacío y éxito solo muestran los cuatro ejemplos **No conectado**, jamás autorización.

El estado inicial veraz es `vacio`. El signal modela exclusivamente presentación: ningún estado procede actualmente de API o grants; éxito significa vista informativa, NO autorización del servidor, conexión de módulos o permiso de un tenant. Una integración futura deberá verificar JWT/RBAC/tenant/capabilities en backend antes de cambiar estados operativos; esta unidad no implementa esa integración.

No hay nuevas rutas, DTO, HTTP, EF/MySQL, migración, secretos, RBAC, activaciones, telemetría de clientes ni dependencia npm. Modifica el componente Angular y pruebas, documentación y workflow dirigido. El cambio es reversible con `git revert` normal, sin force-push.

## A1/A2/A3 — Evidencia causal requerida

Vitest angular real comprueba estado vacío, cinco mensajes, ocultación en carga/error/sin_permiso, transición éxito→denegación→vacío, ausencia de acciones operativas y no filtración de diagnóstico. La CI propia ejecuta también los tres escenarios Playwright/Chromium existentes M04.01: anónimo, denegación tenant sintética 403, vista a 320/1280 y cero solicitudes de módulos. Requiere job GitHub-hosted **completed/success** SHA exacto; pruebas escritas no son PASS.

A2: el guard de sesión/tenant preexistente sigue siendo indispensable; los cambios de signal frontend nunca conceden autoridad. No se ejecutan pruebas HTTP reales de JWT/RBAC/aislamiento A/B, ni se certifican proveedores: **NO_APLICA a implementación exclusivamente visual sin datos privados**. A3 sí requiere Chromium real por cambio visible en pantalla; integración MySQL/EF y proveedor **NO_APLICA**, porque no se modificaron ni conectaron.

## A4 y A5 — Handoff

Revisar diff, HEAD fast-forward, blobs del componente/pruebas, Scope Lock y procedencia V5 en SHA publicado; Vitest, Angular lint/build, Chromium real y checks pertinentes deben finalizar SUCCESS. Mientras estén QUEUED/IN_PROGRESS, PLAN permanece VALIDANDO con owner y SHA/RUN exactos, sin falso LISTO. BITACORA A:Z, PLAN/COLA/DAG requieren readback. A5 independiente siempre otra Task Javier, nunca DEV04. Sin cambios QA/main/PROD.
