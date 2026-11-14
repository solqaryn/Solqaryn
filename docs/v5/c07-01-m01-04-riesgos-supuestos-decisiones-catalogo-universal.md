# SOLQARYN V5 — C07-01.M01.04 — Riesgos, supuestos y decisiones pendientes del catálogo universal

**14 noviembre 2026.** Contrato documental de preparación M01, fuente institucional STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (C07-01). No implementa componentes, migraciones, autorización, eventos ni proveedor. Los riesgos son **posibilidades por investigar**, nunca vulnerabilidades comprobadas. No modifica el esquema ERP ni publica un catálogo nuevo.

## Fuentes y trazabilidad

- `C07-01.M01.01`: `docs/v5/c07-01-m01-01-alcance-catalogo-universal.md`, blob `4f91db421fe4c5e7ad5e0ea080035eabe8f8c1dd`, matriz de ownership, compatibilidad y doce invariantes.
- `C07-01.M01.02`: `docs/v5/c07-01-m01-02-actores-interfaces-catalogo-universal.md`, blob `30e4d0c677112bf490cc937ba6700cd531d29376`, actores, interfaces y fronteras de confianza.
- `C07-01.M01.03`: `docs/v5/c07-01-m01-03-inventario-fuentes-catalogo-universal.md`, blob `8fa23417014077135ba6366f89635291f78e2a4f`, 17 fuentes de Domain, Application, Infrastructure, API y Angular con brechas G01–G08.
- Evidencia directa: `Producto.EmpresaId` y `ProductoVariante.EmpresaId` admiten `NULL` histórico; `Categoria.cs` no declara `EmpresaId`, lo que obliga a inspeccionar filtros/relaciones antes de atribuir aislamiento. `Impuesto.cs` tiene snapshots comerciales; `PublicStoreBinding` liga host/storeKey a empresa. **No basta esta inspección para certificar o reprobar los flujos reales.**

## Riesgos R01–R12 (hipótesis y controles verificables)

| ID | Riesgo plausible | Control propuesto y prueba obligatoria futura |
| --- | --- | --- |
| R01 | Registro histórico `EmpresaId=NULL` interpretado como global | Filtrado fail-closed y cuarentena; consultas A/B y caso NULL negado |
| R02 | Variante vinculada a producto de empresa distinta | Comprobar padre+tenant en aplicación, repositorio y EF; prueba de cruce de IDs |
| R03 | Categoría/atributo compartido accidentalmente entre tenants | Identificar ownership explícito, autorización y rutas; probar categoría ajena |
| R04 | Impuesto cruzado o tasa vigente incorrecta | Tasa por fecha/jurisdicción y tenant, snapshots inmutables; prueba positiva y negativa |
| R05 | Cambiar precio/costo con efecto retroactivo en documentos históricos | Snapshot inmutable y reglas de liquidación; prueba de no regresión facturación |
| R06 | Stock divergente por variantes inactivas/soft-delete | Invariantes de cantidad consolidada e inventario; casos con variantes inactivas |
| R07 | Exposición pública de costo, PII o stock privado | DTO público mínimo, denegación y pruebas host A/B sobre tienda |
| R08 | Host o caché de tienda resuelve catálogo de otra empresa | Binding host→empresa confiable, partición e invalidación de caché; prueba host A/B |
| R09 | JWT válido pero membresía/rol empresarial revocado | Revalidación backend actual en cada acceso; tests positivos/negativos de revocación |
| R10 | SKU/código de barras duplicado o enumeración ajena | Restricciones justificadas por scope y errores no enumerables; pruebas concurrentes |
| R11 | Imagen de variante convertida en portada general ajena | Preservar distinción galería producto vs variante; pruebas de principal y ownership |
| R12 | Concurrencia en actualización stock/venta y rollback parcial | UnitOfWork/locks reales y observabilidad, rollback consistente; integración MySQL efímera |

**Clasificación:** R01–R12 son hipótesis de diseño. Ninguna se marca como fallo real, CVE, explotación o reparación ya realizada. No se solicitan privilegios adicionales ni cambio de datos.

## Supuestos falsables S01–S08

| ID | Supuesto provisional | Cómo se refuta o valida |
| --- | --- | --- |
| S01 | El producto histórico puede conservar `EmpresaId=NULL` hasta reconciliación | Inventario de datos anónimos y prueba de denegación total en rutas actuales |
| S02 | Las variantes heredan el tenant del producto padre | Consulta EF, creación y asociación entre empresas A/B |
| S03 | La categoría puede tener alcance global o compartido controlado | Contrato funcional de ownership + filtro/constraint implementado, no inferir de la clase |
| S04 | El impuesto tiene vigencia y contexto suficiente para cada venta | Validar `ImpuestoService`, facturas, transacciones y snapshots |
| S05 | Servicio no inventariable es compatible con ERP actual | Recorrer `TipoInventario`, precio y flujo de pedidos sin movimientos de stock |
| S06 | Host/storeKey son único punto de identidad pública | Verificar `TiendaBootstrap`, resolver, fallback y caché, incluso host desconocido |
| S07 | Los guards UI reflejan permisos pero no los sustituyen | Inspección `RequierePermiso` y tests con HTTP forjado directamente |
| S08 | Las alteraciones futuras serán aditivas y reversibles | Migration dry-run/rollback en CI con MySQL 8.4 efímero, solamente tras gate autorizado |

La falsación de un supuesto desencadena revisión de alcance y una corrección independiente; **no** licencia saltarse la fase.

## Decisiones pendientes D01–D08

| ID | Decisión sin autorización en esta unidad | Evidencia necesaria antes de resolver |
| --- | --- | --- |
| D01 | Modelo de pertenencia de categoría/atributo y política de compartición | Reglas comerciales aprobadas, matriz A/B y EF |
| D02 | Reconciliación de `EmpresaId=NULL` y antigüedad | Inventario reversible, plan de migración, no modificar datos ahora |
| D03 | Unicidad SKU por empresa/sucursal o catálogo global | Casos de inventario, negocio, índices y migración de colisiones |
| D04 | Política de precios/promoción/moneda/vigencia por canal | Contratos de venta e impuestos y snapshots |
| D05 | Semántica de publicación pública/privada de producto/variante | Binding host, autorización y políticas de DTO/caché |
| D06 | Catálogo de servicios y tipos no inventariables | Semántica comercial con no-regresión ERP |
| D07 | Política de rollback y auditoría de cambios de catálogo | Eventos auditables, versión, compensación y no destrucción de historias |
| D08 | Criterio de salida funcional de C07-01 M01.06 | Evidencias A0–A5 independientes y dependencias terminales **LISTO**, nunca solo VALIDANDO provisional |

Cada decisión sigue **PENDIENTE** y no se interpreta como aprobación de arquitectura, producción, pagos, mapas, integración externa o migraciones.

## Oráculos de aceptación futuros, no ejecutados

**P01–P06:** gestión de producto propio A; variante propia A; categoría válida; tasa/snapshot consistente; tienda pública host A muestra A; reversión aditiva preserva facturas históricas. **N01–N12:** empresa A consulta B; variante B ligada a producto A; categoría ajena; impuesto ajeno; costo expuesto a tienda; NULL legado publicable; SKU ajeno; galería ajena; host inválido; caché A/B cruzado; rol revocado; venta que recalcula snapshot histórico. Todos los escenarios están **NO_EJECUTADOS**.

## Impacto y auditoría proporcional

- **Dependencias:** DAG `C07-01.M01.04` F=VALIDADO/G=SI, COLA G/O=SI, PLAN PENDIENTE sin owner al claim; doce familias de preparaciones M01.01–05 de C01/C02/C05 **60/60 LISTO**. Sólo fase M01 documental; M01.06 sigue gate NO.
- **Alcance técnico:** únicamente este `.md` y sección autocontenida de `CHANGELOG_AI.md`; sin clases, APIs, DTO, base de datos, Angular, UI, migraciones, secrets, infraestructura ni eventos.
- **A0:** documento, fuentes Git y dependencias; **A1:** consistencia R/S/D y oráculos futuros; **A2:** privacidad, controles JWT/RBAC/tenant como riesgos futuros, no runtime probado; **A3:** NO_APLICA xUnit, MySQL y Playwright para cambios Markdown; **A4:** diff fast-forward exact-SHA, checks Scope Lock/CHG y readback institucional.
- **A5:** revisión independiente por otra Task Javier sobre blob/commit original. Nunca revisar trabajo propio.
- **Rollback:** revert de commit normal con registro compensatorio nuevo; no reescribir BITACORA ni saltar seguridad.
