# Catálogo canónico de matrices

Gobierno: `N8.16 / v2`
Baseline de inventario: `N8.15.H` (`158` registros ancla arquitectónicos certificados).

## Reglas

- `MATRIX_ID` es estable e inmutable y no depende de fila/orden visual.
- `1 contrato material = 1 MATRIX_ID = 1 fila canónica`.
- `FEATURE_GROUP` es contenedor de descubrimiento. Sus screens/dialogs/widgets/primitives internos sólo pasan a `MATERIAL` después de materiality review y asignación previa de un ID propio.
- Por definición de gobierno, `MATERIAL_WITHOUT_ID` es inválido.
- Aliases no crean nuevas identidades; splits/merges/supersession se documentan explícitamente.
- La columna `STATUS` de este catálogo raíz expresa **materialidad/descubrimiento** (`MATERIAL | DISCOVERY_CONTAINER`); no es el estado de ciclo de vida de la matriz.
- El ciclo canónico independiente es `MATRIX_STATE`: `BASELINE_CREATED -> INVENTORY_COMPLETE -> SPEC_COMPLETE -> IMPLEMENTATION_REVIEWED -> CERTIFIED`. `CERTIFIED` exige evidencia material y no se obtiene por la mera existencia de documentación.

## Catálogo inicial gobernado

Conteo exacto: **49 contract roots** = 1 shell + 48 feature roots inventariados físicamente en N8.15.E/H.

| MATRIX_ID | DOMAIN | CONTRACT_KIND | IMPLEMENTATION_REF | PARENT_MATRIX_ID | STATUS | CLASSIFICATION |
|---|---|---|---|---|---|---|
| VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | GOV_CONFIG_INTEGRATIONS | SHELL | `frontend/src/app/app.component.*`; `frontend/src/app/app.routes.ts` | ROOT | MATERIAL | KEEP |
| VAEP-MX::INVENTORY_LOGISTICS::ALMACENES | INVENTORY_LOGISTICS | FEATURE_GROUP | `frontend/src/app/features/almacenes` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::FINANCE_ACCOUNTING::ASIENTOS_CONTABLES | FINANCE_ACCOUNTING | FEATURE_GROUP | `frontend/src/app/features/asientos-contables` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::GOV_CONFIG_INTEGRATIONS::AUDITORIA | GOV_CONFIG_INTEGRATIONS | FEATURE_GROUP | `frontend/src/app/features/auditoria` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CASH_BANKS::CAJA | CASH_BANKS | FEATURE_GROUP | `frontend/src/app/features/caja` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::GOV_CONFIG_INTEGRATIONS::CARGAS_MASIVAS | GOV_CONFIG_INTEGRATIONS | FEATURE_GROUP | `frontend/src/app/features/cargas-masivas` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::PRODUCT_CATALOG::CATALOGOS_PRODUCTO | PRODUCT_CATALOG | FEATURE_GROUP | `frontend/src/app/features/catalogos-producto` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::PRODUCT_CATALOG::CATEGORIAS | PRODUCT_CATALOG | FEATURE_GROUP | `frontend/src/app/features/categorias` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::BI_REPORTING::CENTRO_REPORTES | BI_REPORTING | FEATURE_GROUP | `frontend/src/app/features/centro-reportes` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::FINANCE_ACCOUNTING::CENTROS_COSTO | FINANCE_ACCOUNTING | FEATURE_GROUP | `frontend/src/app/features/centros-costo` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CUSTOMERS_COMMERCIAL::CLIENTES | CUSTOMERS_COMMERCIAL | FEATURE_GROUP | `frontend/src/app/features/clientes` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::PURCHASES_SUPPLIERS::COMPRAS | PURCHASES_SUPPLIERS | FEATURE_GROUP | `frontend/src/app/features/compras` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::GOV_CONFIG_INTEGRATIONS::CONFIGURACION | GOV_CONFIG_INTEGRATIONS | FEATURE_GROUP | `frontend/src/app/features/configuracion` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CUSTOMERS_COMMERCIAL::COSTOS_ENVIO | CUSTOMERS_COMMERCIAL | FEATURE_GROUP | `frontend/src/app/features/costos-envio` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CUSTOMERS_COMMERCIAL::COTIZACIONES | CUSTOMERS_COMMERCIAL | FEATURE_GROUP | `frontend/src/app/features/cotizaciones` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CASH_BANKS::CUENTAS_BANCARIAS | CASH_BANKS | FEATURE_GROUP | `frontend/src/app/features/cuentas-bancarias` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::FINANCE_ACCOUNTING::CUENTAS_POR_COBRAR | FINANCE_ACCOUNTING | FEATURE_GROUP | `frontend/src/app/features/cuentas-por-cobrar` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::BI_REPORTING::DASHBOARD | BI_REPORTING | FEATURE_GROUP | `frontend/src/app/features/dashboard` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CUSTOMERS_COMMERCIAL::DESCUENTOS | CUSTOMERS_COMMERCIAL | FEATURE_GROUP | `frontend/src/app/features/descuentos` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::FINANCE_ACCOUNTING::ESTADOS_FINANCIEROS | FINANCE_ACCOUNTING | FEATURE_GROUP | `frontend/src/app/features/estados-financieros` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CUSTOMERS_COMMERCIAL::FACTURAS | CUSTOMERS_COMMERCIAL | FEATURE_GROUP | `frontend/src/app/features/facturas` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::FINANCE_ACCOUNTING::FINANZAS | FINANCE_ACCOUNTING | FEATURE_GROUP | `frontend/src/app/features/finanzas` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::FINANCE_ACCOUNTING::IMPUESTOS | FINANCE_ACCOUNTING | FEATURE_GROUP | `frontend/src/app/features/impuestos` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::INVENTORY_LOGISTICS::INVENTARIO | INVENTORY_LOGISTICS | FEATURE_GROUP | `frontend/src/app/features/inventario` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::IDENTITY_ACCESS::LOGIN | IDENTITY_ACCESS | FEATURE_GROUP | `frontend/src/app/features/login` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CASH_BANKS::METODOS_PAGO | CASH_BANKS | FEATURE_GROUP | `frontend/src/app/features/metodos-pago` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::PURCHASES_SUPPLIERS::ORDENES_COMPRA | PURCHASES_SUPPLIERS | FEATURE_GROUP | `frontend/src/app/features/ordenes-compra` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CUSTOMERS_COMMERCIAL::PEDIDOS_VENTA | CUSTOMERS_COMMERCIAL | FEATURE_GROUP | `frontend/src/app/features/pedidos-venta` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::IDENTITY_ACCESS::PERFIL | IDENTITY_ACCESS | FEATURE_GROUP | `frontend/src/app/features/perfil` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::FINANCE_ACCOUNTING::PERIODOS_CONTABLES | FINANCE_ACCOUNTING | FEATURE_GROUP | `frontend/src/app/features/periodos-contables` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::IDENTITY_ACCESS::PERMISOS | IDENTITY_ACCESS | FEATURE_GROUP | `frontend/src/app/features/permisos` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::FINANCE_ACCOUNTING::PLAN_CUENTAS | FINANCE_ACCOUNTING | FEATURE_GROUP | `frontend/src/app/features/plan-cuentas` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::INVENTORY_LOGISTICS::PREPARACIONES_PEDIDO_VENTA | INVENTORY_LOGISTICS | FEATURE_GROUP | `frontend/src/app/features/preparaciones-pedido-venta` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::PRODUCT_CATALOG::PRODUCTOS | PRODUCT_CATALOG | FEATURE_GROUP | `frontend/src/app/features/productos` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::PURCHASES_SUPPLIERS::PROVEEDORES | PURCHASES_SUPPLIERS | FEATURE_GROUP | `frontend/src/app/features/proveedores` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::PURCHASES_SUPPLIERS::RECEPCIONES_COMPRA | PURCHASES_SUPPLIERS | FEATURE_GROUP | `frontend/src/app/features/recepciones-compra` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::BI_REPORTING::RENTABILIDAD | BI_REPORTING | FEATURE_GROUP | `frontend/src/app/features/rentabilidad` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::BI_REPORTING::REPORTES_ADMINISTRATIVOS | BI_REPORTING | FEATURE_GROUP | `frontend/src/app/features/reportes-administrativos` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::BI_REPORTING::REPORTES_COMPRAS | BI_REPORTING | FEATURE_GROUP | `frontend/src/app/features/reportes-compras` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::BI_REPORTING::REPORTES_INVENTARIO | BI_REPORTING | FEATURE_GROUP | `frontend/src/app/features/reportes-inventario` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::BI_REPORTING::REPORTES_VENTAS | BI_REPORTING | FEATURE_GROUP | `frontend/src/app/features/reportes-ventas` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::IDENTITY_ACCESS::ROLES | IDENTITY_ACCESS | FEATURE_GROUP | `frontend/src/app/features/roles` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::PURCHASES_SUPPLIERS::SOLICITUDES_COMPRA | PURCHASES_SUPPLIERS | FEATURE_GROUP | `frontend/src/app/features/solicitudes-compra` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::INVENTORY_LOGISTICS::SUCURSALES | INVENTORY_LOGISTICS | FEATURE_GROUP | `frontend/src/app/features/sucursales` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CUSTOMERS_COMMERCIAL::TIPO_CLIENTES | CUSTOMERS_COMMERCIAL | FEATURE_GROUP | `frontend/src/app/features/tipo-clientes` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::INVENTORY_LOGISTICS::UBICACIONES_ALMACEN | INVENTORY_LOGISTICS | FEATURE_GROUP | `frontend/src/app/features/ubicaciones-almacen` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::IDENTITY_ACCESS::USUARIOS | IDENTITY_ACCESS | FEATURE_GROUP | `frontend/src/app/features/usuarios` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CUSTOMERS_COMMERCIAL::STOREFRONT | CUSTOMERS_COMMERCIAL | FEATURE_GROUP | `frontend/src/app/features/storefront` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |
| VAEP-MX::CUSTOMERS_COMMERCIAL::VENTAS | CUSTOMERS_COMMERCIAL | FEATURE_GROUP | `frontend/src/app/features/ventas` | VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL | DISCOVERY_CONTAINER | KEEP |

## Estado de matriz canónico en N8.16.H

El mapping de ciclo de vida es exacto y separado de `STATUS`: **los 49/49 contract roots anteriores tienen `MATRIX_STATE = INVENTORY_COMPLETE` en N8.16.H**. La identidad, ownership, parentage, implementación raíz y materialidad/discovery están inventariados, pero N8.17 todavía debe materializar y especificar los contratos hijos de screen/dialog/widget/shared primitive antes de cualquier avance a `SPEC_COMPLETE`.

Distribución exacta en este corte:

| MATRIX_STATE | Conteo |
|---|---:|
| BASELINE_CREATED | 0 |
| INVENTORY_COMPLETE | 49 |
| SPEC_COMPLETE | 0 |
| IMPLEMENTATION_REVIEWED | 0 |
| CERTIFIED | 0 |

Esta asignación es 1:1 y determinística porque su universo es exactamente el conjunto de 49 `MATRIX_ID` de la tabla anterior. Cualquier contrato hijo que N8.17 confirme como material debe agregarse primero al catálogo con `MATRIX_ID`, `PARENT_MATRIX_ID`, materialidad y `MATRIX_STATE` propios; nunca hereda por implícito el estado del feature group.

## Reconciliación con N8.15

Los **158 registros ancla** certificados por N8.15.H son unidades de arquitectura/ownership, no 158 contratos UI. Su composición exacta es: 4 backend source layers + 9 bounded/domain areas + 90 controller files + 48 frontend feature roots + 2 migration roots + 1 DbContext + 3 middleware files + 1 Angular route table principal = 158. N8.16 gobierna como contract roots UI únicamente el shell y los 48 feature roots físicamente inventariados: **49**. No se fabrican `MATRIX_ID` para controllers, migration roots, source layers, middleware o DbContext sólo por ser anchors arquitectónicos.

La correspondencia relevante en este corte es:

- 48/48 frontend feature roots de N8.15.H -> 48/48 `FEATURE_GROUP` con `MATRIX_ID` estable;
- 1/1 Angular shell/route root -> 1/1 `SHELL` con `MATRIX_ID` estable;
- total contract roots gobernados -> **49/49 con ID**;
- anchors arquitectónicos no-UI restantes -> **109**, preservados como evidencia de arquitectura/ownership y no promovidos artificialmente a contrato UI.

## Exactitud y materialidad

- filas con `MATRIX_ID`: **49/49**;
- IDs duplicados: **0**;
- IDs basados en row/index: **0**;
- contract roots inventariados sin ID: **0**;
- contratos hijos certificados como `MATERIAL` sin ID: **0**;
- contract roots sin `MATRIX_STATE` fijado: **0**;
- `MATRIX_STATE` fuera de la secuencia canónica: **0**.

N8.17 puede ampliar este catálogo con contratos hijos materialmente confirmados. La ampliación es aditiva, conserva IDs existentes y sólo avanza estados con evidencia causal.
