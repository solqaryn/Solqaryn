# SOLQARYN V5 — C07-01.M02.04: configuración tipada, ámbitos y flags de catálogo universal

**ID:** C07-01.M02.04 · **Plan:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) · **PLAN_MAESTRO:** fila 1019 · **Autor:** PRIMARY DEV 02, Task `6af16bb9492c8191b8e8c7d606f11507` · **Rama:** `solqaryn/Solqaryn@dev` · **Scope:** STRICT · **Fecha:** 2026-11-15.

## Alcance, predecesores y límites

Diseño exclusivamente **documental** de opciones, límites de configuración y conmutadores del futuro catálogo universal: productos, servicios, variantes, categorías, impuestos, precios, stock e integración de storefront/ERP. Hereda los gates K01–K14 de `docs/v5/c07-01-m01-06-gates-rollback-liberacion-catalogo-universal.md`. No crea configuración activa, nuevos flags del runtime, servicios, endpoints, DTO compilables, MySQL/EF, JWT, secretos, Angular, workers ni proveedores. No significa que las opciones aquí enumeradas existan hoy.

Los datos legacy `Producto.EmpresaId` y `ProductoVariante.EmpresaId` pueden ser NULL; `Categoria` puede carecer de `EmpresaId` declarada. **No existe una opción de configuración capaz de reinterpretar NULL como acceso público.** Tenant A/B y autorización efectiva sólo se resuelven por backend con contexto seguro. El storefront se vincula a un host/tenant verificado por servidor y no al `empresaId` enviado por navegador.

## Ámbitos jerárquicos propuestos

| Ámbito | Quién controla | Ejemplos | Precedencia y restricción |
| --- | --- | --- | --- |
| PLATFORM_SECURITY | Configuración autorizada del operador platform, fuera de cliente tenant | aislamiento obligatorio, política de errores, cuotas máximas | Prevalece sobre todos los demás; un tenant no la rebaja |
| DEPLOYMENT_ENV | Pipeline autorizado por ambiente, secretos fuera de Git | endpoint interno, límites de memoria, modo diagnóstico acotado | DEV no habilita QA/main/PROD; nunca exponer secrets al frontend |
| TENANT_CATALOG | ADMIN de tenant propio sujeto al ceiling de plataforma | activar tipos de catálogo permitidos, defaults de presentación | No altera pertenencia, RBAC, impuesto legal, precio histórico ni cuotas máximas |
| STOREFRONT_PUBLIC | Sólo proyección de host verificado por servidor | mostrar imagen publicada, orden y filtros de catálogo | No puede habilitar borrador, costos, inventario privado ni ítems de otro tenant |

La precedencia efectiva es `PLATFORM_SECURITY (invariante) -> DEPLOYMENT_ENV -> TENANT_CATALOG validado -> STOREFRONT_PUBLIC restringido`. No hay merge ciego de objetos configurables ni carga de código/plantillas arbitrarias; toda opción se valida y normaliza del lado servidor.

## Catálogo de opciones tipadas propuestas

| ID | Opción (tipo conceptual) | Default seguro | Validación / amenaza |
| --- | --- | --- | --- |
| CFG-01 | `CatalogEnabled: bool` | false en nuevas instalaciones | Nunca activa publicación sin revisión tenant |
| CFG-02 | `AllowedItemTypes: set<PRODUCT,SERVICE>` | conjunto vacío | No permitir tipos desconocidos, SKU de servicio inventariable por defecto |
| CFG-03 | `VariantMaxPerParent: int` | 0 hasta política aprobada | Positivo y menor que techo platform; anti-explosión |
| CFG-04 | `AttributeAllowlist: set<string>` | vacío | Longitud, claves permitidas, no scripts/HTML |
| CFG-05 | `CategoryAssociationMode: enum` | `DENY_UNVERIFIED` | Nunca inferir `Categoria.EmpresaId` ausente |
| CFG-06 | `LegacyNullOwnershipPolicy: enum` | `QUARANTINE` | Prohibido `NULL_MEANS_PUBLIC` |
| CFG-07 | `AllowedCurrencies: set<ISO4217>` | vacío | Moneda admitida por servidor, conversión explícita |
| CFG-08 | `PriceValidityMaxDays: int` | 0 sin política | No reescribir factura/pedido/snapshot |
| CFG-09 | `TaxRuleMode: enum` | `SERVER_VALIDATED` | Impuesto legal no controlado por cliente |
| CFG-10 | `PublicProjectionEnabled: bool` | false | Requiere binding host y publicación aprobados |
| CFG-11 | `PublicFieldsAllowlist: set<field>` | campos mínimos | Prohibir costo, draft, PII, stock interno, owner |
| CFG-12 | `StockAvailabilityMode: enum` | `REQUIRE_SERVER_PROOF` | Stock transaccional por sucursal/variante |
| CFG-13 | `PageSizeMax: int` | límite conservador | Paginación server-side y cuotas |
| CFG-14 | `OperationRateLimit: int/duration` | techo platform | Política por tenant, usuario y host sin fuga |
| CFG-15 | `IdempotencyTtlSeconds: int` | deshabilitado sin store fiable | TTL acotado y namespace tenant+actor+operación |
| CFG-16 | `PublishedMediaPolicy: enum` | `APPROVED_ONLY` | Media con owner propio, no URL externa arbitraria |
| CFG-17 | `FeatureVersion: semver` | versión actual explícita | Invalidar cambios incompatibles, rollout reversible |
| CFG-18 | `AuditRetentionPolicy: enum` | `SERVER_GOVERNED` | No permitir que tenant suprima evidencia obligatoria |

**Estos nombres son propuestas, no propiedades existentes.** Valores productivos, presupuestos y límites numéricos deberán decidirse con evidencia, riesgo, UI y pruebas, sin usar cifras inventadas como configuración efectiva.

## Reglas verificables antes de activar cualquier opción

1. Resolver contexto tenant y rol actual en servidor; jamás aceptar valores de autoridad desde localStorage, query, host header sin binding o body JSON.
2. Una opción tenant sólo puede reducir superficies respecto a `PLATFORM_SECURITY`; nunca deshabilitar RBAC, auditoría, comprobación de tenant o sanitización.
3. Defaults deben ser fail-closed cuando faltan datos, catálogo legacy está ambiguo o una categoría no tiene modelo de propiedad verificado.
4. Toda escritura requiere versión/CAS y auditoría de actor, estado previo, nuevo y correlación; no guardar JWT/secretos/PII en logs de configuración.
5. Desactivación no elimina ventas, facturas, catálogo histórico, reservas, stock o pedidos; estado de lectura histórica se mantiene.
6. Cambiar precio, impuestos o moneda no reinterpreta snapshots de documentos emitidos.
7. Flags de producto público se habilitan sólo si host, ítem, variante, media y precio vigentes tienen dueño confirmado.
8. El frontend puede ocultar UI, pero la autorización real sigue en API; endpoints niegan operaciones aun si el cliente envía un flag.
9. Serialización de configuración para Angular no incluye controles internos ni secretos; no permitir expresiones o código arbitrario.
10. La publicación de flags se ensaya con regresión de catálogo/ERP y reversión por tenant; sin revisión técnica no hay rollout.
11. Los límites se aplican en servidor, protegen consultas/stock y no permiten DoS mediante paginación masiva.
12. Revocación de membresía invalida derechos para nuevas operaciones con independencia de cache/UI.
13. El ambiente DEV no se presenta como verificación en QA o PROD; se requieren autorizaciones separadas.
14. Un cambio de provider, DNS, R2, Cloudflare o infraestructura no se activa desde este documento.

## Matriz de aceptación futura: escenarios positivos/negativos NO EJECUTADOS

| Caso | Futuro positivo | Futuro negativo |
| --- | --- | --- |
| PN01 | ADMIN A reduce opciones propias | A no cambia reglas globales ni tenant B |
| PN02 | Tipo PRODUCT autorizado | Tipo desconocido/servicio con stock auto-creado rechazado |
| PN03 | Variante A bajo padre A | Variante B bajo padre A denegada |
| PN04 | Categoría con relación real aprobada | Categoría sin ownership verificado denegada |
| PN05 | Legacy categorizado explícitamente | NULL legacy no se publica automáticamente |
| PN06 | Precio vigente calculado por servidor | Precio enviado por cliente no altera histórico |
| PN07 | Host A resuelve proyección de A | Host A no ve borrador/costo/tenant B |
| PN08 | Cambios versionados y auditados | Cambio con ETag obsoleto no se aplica |
| PN09 | Cuotas aplicadas por actor propio | Config tenant no excede techo platform |
| PN10 | Revocación de rol bloquea escritura | JWT antiguo/flag frontend no reautoriza |
| PN11 | Medio aprobado se publica | URL/imagen B no se vincula a A |
| PN12 | Desactivar catálogo preserva historial | Desactivación no elimina facturas/pedidos |
| PN13 | Fallo de config resulta no-publicación | Fallo no genera defaults permisivos |
| PN14 | Paginación normal y estable | Escaneo abusivo obtiene error seguro |

**NO EJECUTADOS:** estos 28 resultados son oráculos futuros, no evidencia de ejecución de tests. Implementación .NET exige tests xUnit; cambios Angular requieren compilación/Playwright; EF/MySQL requiere CI MySQL 8.4 efímero, todo exact SHA.

## Gobierno A0–A5, observabilidad y rollback

A0 revisa plan vigente, M01.06 y arquitectura. A1 verifica que la tabla sea coherente con operaciones de catálogo y compatibilidad ERP, sin atribuir ejecución funcional. A2 exige denegación A/B, roles, NULL y secretos. A3 comprueba fronteras contractuales; para Markdown sin runtime, xUnit/MySQL/Angular `NO_APLICA` fundamentado, **no PASS**. A4 requiere commit dev fast-forward con documento + sección autocontenida de CHANGELOG, Scope Lock y verificador V5 `completed/success` del SHA causal, readback PLAN/COLA/DAG/BITACORA. A5 sólo puede acreditarlo otra Task Javier distinta del autor mediante revisión del diff y evidencias.

Rollback de esta entrega documental: `git revert` aditivo y corrección de contratos, sin borrar BITACORA. Rollback de flags futuros deberá conservar historia, versionado y vigencia; no modificar main/QA/PROD ni infraestructura. Nada aquí concede autorización para activar flags, migraciones o exportaciones.
