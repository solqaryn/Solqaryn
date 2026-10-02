# N8.15.E — FRONTEND_UX inventario

Estado: `LISTO_REAL`

Baseline de entrada: `844d3982bd9baddacf5b02dc0c5a274f581089ff`.

## Roots UI actuales

`frontend/src/app` contiene el shell (`app.component.*`), bootstrap/config (`app.config.ts`), routing (`app.routes.ts` + route specs), `core/`, `features/`, `services/` y `shared/`.

`frontend/src/app/features` fue enumerado físicamente. Incluye bounded UI de almacenes, contabilidad, auditoría, caja, cargas masivas, catálogos de producto, categorías, reportes, centros de costo, clientes, compras, configuración, costos de envío, cotizaciones, cuentas bancarias y las demás áreas ERP mapeadas en N8.15.B.

## Rutas y guard contract

`app.routes.ts` es la entrada principal. Las rutas ERP autenticadas usan `authGuard` + `permisoGuard` con metadata `data.modulo/data.accion`; algunas features agregan route arrays propios (por ejemplo ajustes de inventario, facturas proveedor y pedidos de venta).

Existe además storefront público `SOLQARYN/**` y `login` fuera del guard ERP. Esa exposición no se trata como defecto por sí sola: las operaciones y datos sensibles deben seguir protegidos por API/backend.

## Candidatos de contrato UI

Clasificación para N8.16/N8.17:

- `SCREEN`: cada ruta navegable material (`dashboard`, listas, detalles, forms, reportes, caja, inventario, compras, ventas, configuración, storefront, etc.).
- `BUSINESS_DIALOG`: diálogos/modales que ejecutan una decisión o cambio de negocio propio; se inventarían dentro de cada feature, no por nombre genérico.
- `EMBEDDED_INTERACTIVE`: forms/widgets independientes embebidos en shells/detalles.
- `SHELL`: `app.component`, sidebar/header/navigation, route shells y guards visuales.
- `SHARED_PRIMITIVE`: componentes reutilizables bajo `shared/` o `core/` con interacción/contrato propio.

Los archivos decorativos/SCSS sin datos, permisos o interacción material no reciben matriz independiente.

## Duplicados/aliases/orphans candidatos

1. `catalogos-producto` reutiliza una UI común parametrizada para colores/tallas/marcas/modelos; se clasifica `KEEP` como shared pattern y evita cuatro implementaciones separadas.
2. List/form/detail del mismo aggregate son contratos `SCREEN` distintos si son rutas distintas, no duplicados automáticos.
3. Route specs en `frontend/src/app/*.spec.ts` y routes feature son evidencia de uso; no se clasifican orphan por no aparecer directamente en menú.
4. Storefront `SOLQARYN` comparte catálogo/comercial con ERP pero es un canal/UI bounded area distinto; ownership backend compartido, UI `KEEP`.
5. Cualquier component/route sin referencia confirmada queda `UNKNOWN` hasta N8.15.G; `REMOVE_SAFE=0` en E.

## Menú, servicios y autoridad

- Menú/shell deben mapearse a las mismas capacidades/permissions que `app.routes.ts`; discrepancias son candidatos de deuda para N8.16/N8.18, no autorización para eliminar.
- `frontend/src/app/services` y `core` son clientes/helpers; no son autoridad de reglas de negocio.
- `authGuard`/`permisoGuard` son UX/navigation enforcement; backend debe revalidar authz, tenant, invariantes, estados y totales.

## REVIEW_FIRST

P0=0, P1=0. No se modificó UI, rutas ni guards. Se preservó la distinción público/ERP y se evitó clasificar como removible cualquier componente sin referencia exhaustivamente validada.

## Resultado

`N8.15.E = LISTO_REAL`.

Siguiente dependency-valid: `N8.15.F — SEC_AUDIT`.
