# SOLQARYN V17 — C11-04.M05.05: caso negativo de observabilidad privada ante 429

**Microtarea:** C11-04.M05.05. **Autor:** DEV04 JAVIER (TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`). **Riesgo:** R2 conservador por aislamiento/denegación; exclusivamente prueba browser y documentación. **Rama:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT, V17 lock `2000001722`.

## A0 — cambio, requisitos y no-alcance
Añadir una prueba Playwright negativa al archivo actual `frontend/e2e/c11-04-m04-01-observabilidad.spec.ts`, con documentación y sección autocontenida de CHANGELOG. No tocar controlador, servicio, DB, JWT/RBAC, Angular runtime, proveedor, integración real, dominio, seguridad global ni infraestructura.

## A1 — aserciones reales y fallables que exige el contrato
- **Control positivo:** GET sintético de sesión 200 permite inicialmente cuatro tarjetas de guía privada.
- **Negativo nuevo:** una revalidación posterior recibe HTTP 429 con el payload `SECRET_TENANT_B_429`; se ocultan todas las tarjetas, el branding privado y cualquier texto del payload, aparece aviso de error seguro y el botón se restablece.
- **Límite/concurrencia:** sólo dos GET en total, sin doble solicitud, retries implícitos ni fallback a métricas, alert dispatch o exporter.
- **Casos existentes:** el mismo fichero conserva negativos 403/503, navegación con Tab real, límites responsive 320/768/1280, ausencia de exporter y éxito 200 M05.04. Si una condición se rompe, la aserción Chromium falla.

## A2 — seguridad
El 429 simulado impide filtración de mensaje de error de supuesto tenant B y mantiene denegación por defecto. Token y respuestas se generan con `page.addInitScript` y `page.route`: **NO prueban JWT auténtico, aislamiento server-side, HTTP real ni revocación en un proveedor**. La UI no se usa como autoridad de RBAC.

## A3 — pruebas pertinentes
Se requiere GitHub-hosted real con lint/build Angular, Vitest y Playwright Chromium dirigido, check `C11-04.M04.01 - Vitest y Chromium de observabilidad`. MySQL 8.4, migraciones Oracle EF, SMTP, proveedores y despliegues: NO_APLICA, porque esta microtarea es sólo un test browser.

## A4–A5 y rollback
Exigir SHA exacto o blobs idénticos demostrados, Scope Lock, procedencia V5, tipado Angular, suite Chromium positiva/negativa con cero fallos, y readback PLAN/COLA/DAG/BITACORA. Si la CI está queued/in_progress, conservar VALIDANDO bajo DEV04 y no afirmar PASS. LISTO_DEV sólo tras A0–A4; A5 exige revisión real por otra Task Javier y puede rechazar. Rollback mediante `git revert` normal, nunca force-push; QA/main/PROD sin autorización.
