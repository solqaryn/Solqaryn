# C08-04.M05.01 - regla unitaria reproducible de capabilities

Autor: PRIMARY DEV 04, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Riesgo R1: se agregan tests y un workflow de CI, sin modificar runtime de producto, endpoints, JWT, RBAC, tenant, EF/MySQL ni produccion. PLAN fila 1286, candado V17 2000001286, exclusivamente solqaryn/Solqaryn@dev.

## A0 — objetivo, alcance y rollback
El contrato de vista sectorial declara cuatro claves unicas: reservas, membresias, catalogo y sitios. La vista es ilustrativa, no constituye la activacion de modulos ni permisos de servidor. Cambios: una suite Angular TestBed/Vitest, workflow SHA-aislado, este documento y CHANGELOG. Rollback: git revert normal, sin force push.

## A1 — aserciones capaces de fallar
Positivo: cuatro claves ordenadas, unicas y finalidades expresas; vacio/exito solo presentan tarjetas etiquetadas No conectado. Negativo: cargando, error y sin_permiso eliminan las tarjetas y acciones; transicion desde exito revoca previews. Frontera/adversarial: un titulo HTML hostil se muestra como texto, no crea IMG/SCRIPT/IFRAME y no persiste bajo denegacion.

## A2 y A3 — alcance real
Unit testing Angular con TestBed; no hay simulacion presentada como autorizacion backend. Prohibido inferir JWT/RBAC/tenant A/B reales desde estos tests. MySQL/EF/proveedor real NO_APLICA: no hay cambios de persistencia. Playwright ya fue acreditado en M04 para la misma vista; M05.01 agrega solamente contrato unitario. No se certifican metricas Core Web Vitals; NO_MEDIDO.

## A4 y A5
Exigir GitHub Actions real con cuatro Vitest, lint/build Angular, checks pertinentes V5 y Scope Lock en SHA exacto (o descendiente causal auditado). Mientras CI siga QUEUED/IN_PROGRESS mantener VALIDANDO y owner. A5 solo por otra Task Javier con revision independiente de codigo, requisitos y pruebas. No QA/main/PROD ni datos reales.
