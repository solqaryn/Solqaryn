# C04-01.M04.02 — estados de suscripción Angular en Chromium (V22)

PLAN D597 exige implementar estados de carga, vacío, error, éxito y permisos. La UI existente `SuscripcionSaaSCardComponent` ya implementa estados visuales y guards; este changeset test-only verifica esos estados reales en Angular servido localmente bajo Chromium GitHub-hosted, sin reemplazar componentes ni introducir pagos. Alcance R2 conservador por contexto SaaS.

Cuatro tests verifican: espera real de GET retrasado y spinner sin datos anticipados, respuesta 404 vacía con alta denegada y cero escrituras, error HTTP 503 de límites con reintento recuperado, y 403 por tenant/rol redirigido a login sin exponer plan. Son fixtures HTTP y sesión sintética NO JWT firmado; el navegador, Angular, estados y acciones sí son reales. No atribuir MySQL 8.4, API de negocio, autorización productiva ni dispositivo físico a estos casos; esas integraciones pertenecen a otros scopes.

A0 diff focalizado test/workflow/doc/CHANGELOG; A1–A3 pruebas positivas/negativas y estados reales por Chromium al completar CI; A4 Project Scope Lock y V5 exact-SHA, Angular/TypeScript/build y test dirigido SUCCESS. Un test o check SKIPPED/QUEUED/IN_PROGRESS no es PASS. A5 por otra Task Javier después de LISTO, manteniendo author DEV05. Solo dev, sin QA/main/PROD. Rollback revert aditivo.
