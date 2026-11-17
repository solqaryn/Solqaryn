# C08-04.M05.06 — regresion, rendimiento y accesibilidad sectorial

**Autor:** PRIMARY DEV 02, Task ID 6af16bb9492c8191b8e8c7d606f11507. **Repositorio:** solqaryn/Solqaryn, **rama:** dev, **PROJECT_SCOPE_LOCK:** STRICT. **Riesgo:** R2 por tenant, denegaciones y navegacion protegida. No se modifica codigo de runtime, backend, JWT, MySQL ni privilegios.

## A0: alcance, dependencias, impacto y rollback
Ejecutar regresiones reales en Chromium de la vista Angular privada /extensiones-sectoriales: responsive 320/375/768/1280, tarjetas informativas, Tab real con foco visible, 403/503, ausencia de solicitudes mutadoras y API de modulos. Dependencias existentes: Angular standalone, ruta privada y guard, fixture de tenant-context y navegador. No cambia interfaces API/DTO, Domain, Application, Infrastructure, JWT, RBAC, roles, datos, migraciones ni proveedores. Revertir por git revert ordinario de test, workflow, documento y CHANGELOG, preservando BITACORA.

## A1: pruebas falsables
- Positivo: cuatro tarjetas Reservas/Membresias/Catalogo/Sitios por rubro con etiqueta No conectado y estado vacio; cero POST, PUT, PATCH, DELETE y solicitudes de modulos.
- Frontera: 320, 375, 768, 1280 px sin desbordamiento horizontal ni tarjetas fuera; no formularios, botones o indicadores de activacion.
- Accesibilidad: Tab real desde foco inicial a Volver al panel, anillo visible y objetivo de 44 px. Sin invocar focus().
- Negativo/error: 403 y 503 sinteticos redirigen a login, ocultan tarjetas y diagnosticos y no emiten mutaciones.

## A2 y A3: evidencia real y limites
Chromium, Angular local, npm lint y build son reales. Las respuestas de tenant-context se simulan con Playwright page.route: solo se acredita aislamiento y denegaciones de UX, no JWT middleware, RBAC/tenant A/B real, proveedor live o MySQL Oracle EF. A3 MySQL, API HTTP real y proveedor NO_APLICA a este diff exclusivamente test-only; no se debe certificar el servicio global. Rendimiento SLO, CWV, p95, p99 y costos: NO_MEDIDO por ausencia de muestra, periodo, denominador y serie verificable.

## A4, A5 y continuidad
GitHub-hosted workflow dirigido C08-04.M05.06 Angular build y Chromium regresion en SHA publicado; exigir Scope Lock, V5, build/frontend, backend, seguridad, acceptance cuando aplique, con status completed y conclusion success en SHA exacto o equivalencia probada con blobs. Queued/in_progress/skipped/cancelled/failed NO son PASS. Si CI sigue corriendo, mantener VALIDANDO, owner y RUN/SHA, con handoff. Estados mediante V17 addNamedRange, CAS, BITACORA y readback de PLAN/COLA/DAG. A5 solo Task Javier distinta del autor. Sin QA, main, PROD ni secretos.
