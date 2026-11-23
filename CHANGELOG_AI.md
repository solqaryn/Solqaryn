## 2026-11-23 — DEV05 V30 C01-03.M04.04: validación 422 conserva formulario editable
- Entregable funcional Angular: feedback seguro dentro del formulario ante errores HTTP 400/422, sin perder datos ni imposibilitar la corrección. 401/403 y 5xx mantienen sus bloqueos y recuperación explícita.
- E2E dirigido: corregir las expectativas obsoletas 403/500 (run fallido 45256221308) y añadir 422 editable sin exponer detalles internos. La nueva CI exact-SHA permanece pendiente, sin afirmar PASS.
- SKREF-03 Impeccable 2149fcce y SKREF-10 Angular 68089669 consultadas desde origen y pin oficiales. V17 H239 VALIDANDO e I DEV05, A5 pendiente; sin zoom nativo Chrome 200%, ANTES históricos, LISTO, QA/main/PROD. Rollback normal.

