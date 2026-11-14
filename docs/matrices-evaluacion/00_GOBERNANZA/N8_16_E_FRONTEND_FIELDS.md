# N8.16.E — FRONTEND_UX / campos obligatorios

Estado: `LISTO_REAL`

Baseline: `804138502132426d056d2cd4baf5346cfd8eec0e`.

Toda matriz de interfaz material declara: `CONTRACT_KIND`, `PRIMARY_ROUTE_OR_SURFACE`, `ROUTE_ALIASES`, `COMPONENT_REFS`, `FORM_REFS`, `DIALOG_REFS`, `WIDGET_REFS`, `MENU_SHELL_REFS`, `SHARED_PRIMITIVE_REFS`, `FRONTEND_SERVICE_REFS`, `INPUTS_OUTPUTS`, `STATE_MODEL`, `INTERACTIONS`, `VALIDATIONS`, `LOADING_STATE`, `EMPTY_STATE`, `ERROR_STATE`, `DISABLED_STATE`, `OFFLINE_OR_RETRY_STATE`, `SUCCESS_FEEDBACK`, `ACCESSIBILITY_CONTRACT`, `RESPONSIVE_NOTES` y `IMPLEMENTATION_REFS`.

`SCREEN`, `BUSINESS_DIALOG`, `EMBEDDED_INTERACTIVE`, `SHELL`, `SHARED_PRIMITIVE` y `FEATURE_GROUP` son tipos canónicos. Un componente visual puramente decorativo no requiere matriz independiente; si adquiere datos, permisos, estado material, interacción o efectos, pasa por materiality review y recibe `MATRIX_ID` antes de certificarse.

Ruta, menú y guard son referencias de UX; no sustituyen autorización ni validaciones backend. Los aliases de ruta no duplican identidad si el contrato material es uno.

REVIEW_FIRST: P0=0, P1=0. No se modificaron rutas/componentes ni UX productiva.

`N8.16.E = LISTO_REAL`. Siguiente: `N8.16.F — SEC_AUDIT`.
