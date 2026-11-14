# N8.16.D — BACKEND_API / trazabilidad obligatoria

Estado: `LISTO_REAL`

Baseline: `796816ab1dfc9efa6d471d7e7a60f1e1e9092c17`.

Cada matriz material debe poder trazar su backend sin depender de conocimiento tribal. Campos obligatorios, con `N/A:<reason>` cuando corresponda: `API_ROUTE`, `HTTP_METHOD`, `CONTROLLER_ACTION`, `REQUEST_DTO`, `RESPONSE_DTO`, `VALIDATOR`, `APPLICATION_USE_CASE`, `DOMAIN_RULES`, `REPOSITORY`, `INFRASTRUCTURE_ADAPTER`, `BACKGROUND_JOB`, `INTEGRATION_PROVIDER`, `CONFIG_KEYS`, `DEPENDENCY_MATRIX_IDS`, `ERROR_CONTRACT`, `SIDE_EFFECTS`, `AUDIT_EVENTS`, `OBSERVABILITY_SIGNALS` y `IMPLEMENTATION_REFS`.

La matriz identifica la cadena esperada `UI/consumer -> endpoint -> DTO/validator -> use case/service -> domain -> repo/adapter -> persistence/integration`. No crea segunda autoridad ni obliga capas artificiales cuando un contrato no las usa.

Jobs, middleware, webhooks e integraciones externas se documentan por referencia material y configuración no secreta; nunca se copian secretos a la matriz.

REVIEW_FIRST: P0=0, P1=0. No se modificaron endpoints, DI ni runtime.

`N8.16.D = LISTO_REAL`. Siguiente: `N8.16.E — FRONTEND_UX`.
