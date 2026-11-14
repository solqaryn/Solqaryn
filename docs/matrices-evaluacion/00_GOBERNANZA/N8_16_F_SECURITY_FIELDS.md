# N8.16.F — SEC_AUDIT / campos obligatorios

Estado: `LISTO_REAL`

Baseline: `aaa9fc75a85f2c73983d3826fba668e03d08a175`.

Toda matriz material declara explícitamente, con `N/A:<reason>` cuando no aplique: `AUTHN_REQUIRED`, `AUTHZ_POLICY_OR_PERMISSION`, `RBAC_MODULE_ACTION`, `TENANT_SCOPE`, `DATA_OWNER_SCOPE`, `AUDIT_EVENTS`, `PII_CLASSIFICATION`, `SECRET_HANDLING`, `LOG_REDACTION`, `RATE_LIMIT_POLICY`, `CORS_EXPOSURE`, `CSRF_OR_BROWSER_RISK`, `INPUT_TRUST_BOUNDARY`, `OUTPUT_EXPOSURE`, `HEALTH_READINESS_IMPACT`, `OBSERVABILITY_SIGNALS`, `SECURITY_HEADERS_OR_CLIENT_POLICY` y `SECURITY_TEST_REFS`.

Reglas:

- frontend guards/hidden controls se documentan como UX enforcement, nunca como autoridad exclusiva;
- autorización, tenant, reglas críticas e invariantes deben tener backend enforcement o hallazgo explícito;
- secretos se referencian por nombre/config key, nunca por valor;
- PII/logging exige indicar redacción/minimización cuando aplique;
- interfaces públicas deben declarar por qué son públicas y qué datos/acciones quedan expuestos;
- ausencia de un control aplicable debe quedar como finding, no como `N/A` genérico.

REVIEW_FIRST: P0=0, P1=0. La definición endurece trazabilidad sin tocar auth/config/runtime.

`N8.16.F = LISTO_REAL`. Siguiente: `N8.16.G — TEST_CI`.
