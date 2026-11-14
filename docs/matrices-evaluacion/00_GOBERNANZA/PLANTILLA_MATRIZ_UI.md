# Plantilla canónica — Matriz de evaluación de contrato

Versión de gobierno: `N8.16 / v2`

## Identidad estable
- MATRIX_ID: `VAEP-MX::<DOMAIN_SLUG>::<CONTRACT_SLUG>`
- MATRIX_CHANGE_ID:
- MATRIX_VERSION:
- PARENT_MATRIX_ID: `ROOT | <MATRIX_ID>`
- CONTRACT_KIND: `FEATURE_GROUP | SCREEN | BUSINESS_DIALOG | EMBEDDED_INTERACTIVE | SHELL | SHARED_PRIMITIVE`
- DOMAIN:
- PROCESS:
- SUBPROCESS:
- CONTRACT_OWNER:
- DATA_OWNER:
- IMPLEMENTATION_STATUS: `DISCOVERED | MATERIAL | IMPLEMENTED | CERTIFIED | SUPERSEDED`
- MATRIX_STATE: `BASELINE_CREATED | LEVANTAMIENTO_COMPLETO | SPEC_COMPLETE | IMPLEMENTATION_REVIEWED | CERTIFIED`
- CLASSIFICATION: `KEEP | CONSOLIDATE | DEPRECATE | REMOVE_SAFE | UNKNOWN`
- DEPENDS_ON_MATRIX_IDS:
- ALIASES:
- SUPERSEDES / SPLIT_FROM / MERGED_FROM:

> `IMPLEMENTATION_STATUS` expresa materialidad/implementación del contrato y no sustituye el ciclo canónico `MATRIX_STATE`. `MATRIX_STATE` sólo avanza con evidencia causal; `CERTIFIED` exige evidencia material. `MATRIX_ID` es inmutable y jamás se deriva de fila, índice, orden visual, menú o backlog. Un candidato no puede pasar a `MATERIAL` sin ID registrado en `CATALOGO_MATRICES.md`.

## Objetivo y flujo
- Propósito de negocio:
- Actor(es)/rol(es):
- Precondiciones:
- Entrada:
- Flujo feliz:
- Alternativas:
- Salida:
- SIDE_EFFECTS:
- WRITE_IDEMPOTENCY:
- CONCURRENCY_RULE:
- FAILURE_ROLLBACK:

## Contrato frontend / UX
- PRIMARY_ROUTE_OR_SURFACE:
- ROUTE_ALIASES:
- COMPONENT_REFS:
- FORM_REFS:
- DIALOG_REFS:
- WIDGET_REFS:
- MENU_SHELL_REFS:
- SHARED_PRIMITIVE_REFS:
- FRONTEND_SERVICE_REFS:
- INPUTS_OUTPUTS:
- STATE_MODEL:
- INTERACTIONS:
- VALIDATIONS:
- LOADING_STATE:
- EMPTY_STATE:
- ERROR_STATE:
- DISABLED_STATE:
- OFFLINE_OR_RETRY_STATE:
- SUCCESS_FEEDBACK:
- ACCESSIBILITY_CONTRACT:
- RESPONSIVE_NOTES:

## Contrato de datos / DB / migraciones
- DATA_ENTITIES:
- DB_TABLES:
- DB_FIELDS:
- RELATIONSHIPS:
- DB_CONTEXT_REF:
- MIGRATION_REFS:
- FK_CONSTRAINTS:
- UNIQUE_CHECK_CONSTRAINTS:
- INDEX_REFS:
- NULLABILITY_DEFAULTS:
- SEED_FIXTURE_REFS:
- TRANSACTION_BOUNDARY:
- INTEGRITY_RULES:
- TENANT_PARTITION_RULE:
- DATA_CLASSIFICATION:
- RETENTION_DELETION_RULE:
- READ_SCOPE:

| Campo | Fuente | Tipo | Requerido | Default | Autollenado | Calculado servidor | Visible | Editable | Permiso ver | Permiso editar | Sensible/máscara | Validación UX | Validación backend | Constraint BD | Auditoría |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

## Backend / API / integraciones
- API_ROUTE:
- HTTP_METHOD:
- CONTROLLER_ACTION:
- REQUEST_DTO:
- RESPONSE_DTO:
- VALIDATOR:
- APPLICATION_USE_CASE:
- DOMAIN_RULES:
- REPOSITORY:
- INFRASTRUCTURE_ADAPTER:
- BACKGROUND_JOB:
- INTEGRATION_PROVIDER:
- CONFIG_KEYS:
- ERROR_CONTRACT:
- OBSERVABILITY_SIGNALS:
- IMPLEMENTATION_REFS:

## Seguridad / RBAC / tenant / auditoría
- AUTHN_REQUIRED:
- AUTHZ_POLICY_OR_PERMISSION:
- RBAC_MODULE_ACTION:
- TENANT_SCOPE:
- DATA_OWNER_SCOPE:
- AUDIT_EVENTS:
- PII_CLASSIFICATION:
- SECRET_HANDLING:
- LOG_REDACTION:
- RATE_LIMIT_POLICY:
- CORS_EXPOSURE:
- CSRF_OR_BROWSER_RISK:
- INPUT_TRUST_BOUNDARY:
- OUTPUT_EXPOSURE:
- HEALTH_READINESS_IMPACT:
- SECURITY_HEADERS_OR_CLIENT_POLICY:
- SECURITY_TEST_REFS:

> Frontend guards, hidden controls y menu visibility son enforcement UX, nunca la única autoridad de seguridad. Secretos se referencian por key/nombre, nunca por valor.

## Acciones
| Acción | Visible con | Ejecutable con | Confirmación | Backend autoritativo | Resultado | AUDIT_EVENT |
|---|---|---|---|---|---|---|

## Evidencia y gates
- STATIC_REFERENCE_EVIDENCE:
- UNIT_TEST_REFS:
- INTEGRATION_CONTRACT_TEST_REFS:
- E2E_REFS:
- SECURITY_TEST_REFS:
- ACCESSIBILITY_EVIDENCE:
- CI_RUN_REFS:
- RECEIPT_REF:
- REVIEW_FIRST: `P0=<n>; P1=<n>`

## Dictamen
- Implementación coincide con matriz: `PASS | FAIL | NOT_TESTED`
- Estado final permitido: `CERTIFIED` sólo con evidencia material.
- Campos no aplicables deben usar `N/A:<reason>`; vacío no cuenta como cobertura certificada.
