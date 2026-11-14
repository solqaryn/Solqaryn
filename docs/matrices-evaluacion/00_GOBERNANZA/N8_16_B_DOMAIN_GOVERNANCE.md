# N8.16.B — DOMAIN / ownership y padre→hijo

Estado: `LISTO_REAL`

Baseline de entrada: `43d95efa72e11842f7750a5c40ee6e101e0fedbb`.

## Regla de ownership

Cada `MATRIX_ID` tiene exactamente un `CONTRACT_OWNER` y un `DATA_OWNER`; pueden coincidir, pero no quedar vacíos en una matriz material. La jerarquía usa `PARENT_MATRIX_ID` y relaciones explícitas, nunca posición de carpetas o filas como identidad.

## Padres de dominio

Los nueve bounded areas certificados en N8.15.B son los padres lógicos de gobierno:

- `IDENTITY_ACCESS`
- `PRODUCT_CATALOG`
- `CUSTOMERS_COMMERCIAL`
- `PURCHASES_SUPPLIERS`
- `INVENTARIO_LOGISTICA`
- `CASH_BANKS`
- `FINANCE_ACCOUNTING`
- `BI_REPORTING`
- `GOV_CONFIG_INTEGRATIONS`

Un contrato cross-domain conserva un owner primario y declara `DEPENDS_ON_MATRIX_IDS`/`DEPENDS_ON_DOMAINS`; no se duplica la matriz en cada dominio consumidor.

## Regla uno-a-uno

`1 contrato material = 1 MATRIX_ID = 1 entrada canónica de catálogo`.

Alias de ruta, títulos alternos, accesos desde menú distintos o reutilización del mismo componente no crean un segundo ID si el contrato material es el mismo. Si una misma implementación sirve contratos distintos por datos/permisos/efectos, se registran IDs distintos y se comparte `IMPLEMENTATION_REF`.

## Feature groups y descubrimiento

Los 48 feature roots certificados en N8.15.H se registrarán en el catálogo como `FEATURE_GROUP` con ID estable. El shell raíz se registra como `SHELL`. Total de grupos/shell gobernados al inicio: **49**.

Esto no oculta contratos hijos. N8.17 debe evaluar rutas/screens/dialogs/widgets/shared primitives dentro de cada group. Flujo obligatorio:

`DISCOVERED -> materiality review -> assign MATRIX_ID -> MATERIAL -> matrix implementation/certification`.

No existe estado válido `MATERIAL_WITHOUT_ID`.

## Relaciones permitidas

- `PARENT_OF` / `CHILD_OF`
- `DEPENDS_ON`
- `SHARES_IMPLEMENTATION_WITH`
- `SUPERSEDES`
- `SPLIT_FROM`
- `MERGED_FROM`
- `ALIASES`

Las relaciones padre/hijo deben formar DAG. Un alias nunca habilita dos owners de contrato.

## Ownership de reglas críticas

- reglas de negocio, autorización, tenant, estados, totales e integridad: backend/domain owner;
- matriz UI documenta interacción/estado/permiso esperado, pero no se vuelve autoridad de negocio;
- persistencia/migraciones: owner de Infrastructure/DB bajo el caso de uso;
- logging/auditoría/observabilidad: owner transversal declarado, enlazado a contratos que generan eventos.

## Duplicados y renombrado

Antes de crear un ID nuevo se compara `DOMAIN + CONTRACT_KIND + SEMANTIC_PURPOSE + PRIMARY_ROUTE/SURFACE + PRIMARY_EFFECT`. Si coincide materialmente con un ID existente, se usa el existente y se agrega alias/ref. El orden del catálogo puede cambiar libremente sin renumerar.

## REVIEW_FIRST

P0=0, P1=0. La gobernanza evita duplicados por alias, evita parentage implícito y preserva backend authority. No se declaró material ningún child interno sin ID.

## Resultado

`N8.16.B = LISTO_REAL`.

Siguiente dependency-valid: `N8.16.C — DB_MIG`.
