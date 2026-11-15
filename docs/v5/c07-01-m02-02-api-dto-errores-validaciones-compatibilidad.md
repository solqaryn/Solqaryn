# SOLQARYN V5 — C07-01.M02.02: contratos API/DTO, validaciones, errores y compatibilidad del catálogo universal

**ID institucional:** C07-01.M02.02 · **Unidad:** C07-01 Catálogo universal / M02 Contratos y modelo · **Autor:** PRIMARY DEV 02 (Task `6af16bb9492c8191b8e8c7d606f11507`) · **Rama:** `solqaryn/Solqaryn@dev` · **Scope Lock:** STRICT · **Fecha:** 2026-11-15.

**Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO fila 1017, COLA fila 1017 y DAG_TECNICO fila 978. Contrato anterior: `docs/v5/c07-01-m01-06-gates-rollback-liberacion-catalogo-universal.md`. Los controles K01–K14 de M01.06 siguen vigentes.

## 1. Alcance y frontera de autoridad

Este documento **propone** contratos para el catálogo comercial universal: productos, servicios, variantes, categorías, atributos, impuestos, listas de precios y su exposición privada/pública. **No crea ni modifica endpoints, DTO compilables, entidades, mapeos EF, tablas, migraciones, stock, facturas, jobs, eventos, publicaciones ni permisos.** Los nombres de operaciones de las tablas son identificadores de contrato futuros, **no rutas existentes certificadas**.

La frontera es `Angular/cliente -> API con JWT y autorización servidor -> Application -> Domain -> Infrastructure -> MySQL`. Tenant, miembro, rol y tienda/host se derivan de contexto autenticado y binding aprobado por servidor, **nunca de un `empresaId`, `storeKey` o header controlado por el cliente**. El catálogo legacy requiere compatibilidad no destructiva: `Producto.EmpresaId` y `ProductoVariante.EmpresaId` admiten valores NULL en parte del modelo heredado; `Categoria` no declara necesariamente `EmpresaId`. Ningún NULL se interpreta como catálogo global público ni permite copiar un registro a otra empresa.

**Roles conceptuales:** PLATFORM (gobierno exclusivo), TENANT_ADMIN (administración de catálogo en tenant propio), CATALOG_EDITOR (edición acotada), CATALOG_VIEWER (lectura privada) y STOREFRONT_PUBLIC (proyección mínima autorizada por host). Son perfiles propuestos, NO afirmaciones sobre nombres concretos vigentes de RBAC. Toda implementación exige verificar roles reales, revocación y permisos por acción.

## 2. Matriz de operaciones propuestas (sin rutas ejecutables)

| Contrato | Entrada conceptual | Respuesta mínima | Autorización y invariantes |
| --- | --- | --- | --- |
| CAT-API-01 Listar catálogo privado | Filtros de texto/tipo/estado, cursor opaco, límite acotado | Referencias de catálogo, vigencia y versión | Membresía vigente del tenant; paginación y filtros en servidor; no enumerar B desde A |
| CAT-API-02 Obtener detalle privado | ID opaco de ítem | Ítem, variantes propias, estado y ETag | Verificar propiedad efectiva item-variante-categoría, incluso si legacy tiene NULL |
| CAT-API-03 Crear borrador | Tipo, nombre, descripción saneada y atributos allowlisted | ID servidor, estado DRAFT, versión inicial | ADMIN/EDITOR habilitado; tenant fijado en servidor; idempotencia scoped |
| CAT-API-04 Actualizar borrador | ID y ETag/versión, patch allowlisted | Nueva versión y campos seguros | CAS optimista; no cambiar tenant, costo histórico ni snapshots emitidos |
| CAT-API-05 Consultar variantes | ID producto, cursor y filtros allowlisted | Variantes/SKU/atributos propios | Padrehijo del mismo ámbito; rechazo de SKU ajeno o parent de B |
| CAT-API-06 Asociar categoría/impuesto | Referencia aprobada y vigencia | Asociación validada/versionada | Resolver alcance real de categoría/impuesto; no suponer `Categoria.EmpresaId` |
| CAT-API-07 Cotizar/publicar precio | Referencia, moneda y fecha efectiva | Cotización/snapshot con versión, impuestos y vigencia | Recalcular en servidor; no aceptar impuesto/precio autoritativo del cliente |
| CAT-API-08 Exponer proyección storefront | Host verificado y filtros públicos | DTO con título, imagen autorizada, disponibilidad publicable y precio válido | Host->tenant en servidor; jamás costo, borrador, inventario interno, PII ni tenant B |
| CAT-API-09 Cambiar estado/publicar | ID, ETag, intención y razón | Estado resultante y versión | Permisos específicos; aceptación de política de stock/impuestos y rollback |
| CAT-API-10 Consultar existencia reservable | Ítem/variante/sucursal propia | Disponible/indisponible autorizado, no detalles de B | Servicios no inventariables; cantidades/costos internos sólo en privado |
| CAT-API-11 Desactivar/archivar | ID, versión, motivo | Tombstone lógico y versión | No borrar historial de facturas/pedidos, stock ni relaciones referidas |
| CAT-API-12 Consultar compatibilidad ERP | IDs legado y tipo | Clasificación: propio, compartido aprobado, desconocido/cuarentena | Mapa explícito y auditoría; NULL, asociaciones incompletas y colisiones denegadas |

Estas doce operaciones son **contratos objetivo**, no se presupone equivalencia 1:1 con controladores existentes. Antes de crearlas se inventariarán rutas/servicios actuales, sus consumidores, DTO existentes y coexistencia compatible de versiones, evitando romper pantallas ERP.

## 3. DTO propuestos y fuente de cada campo

| Perfil | Campos permitidos conceptuales | Campos prohibidos o controlados |
| --- | --- | --- |
| `CatalogItemCreateRequest` | tipo enum, nombre saneado, descripción, atributos allowlisted, token idempotente | `EmpresaId`, `TenantId`, `OwnerId`, estado de publicación forzado, costo histórico |
| `CatalogItemPatchRequest` | cambios explícitos permitidos, versión esperada, razón | cambio de ownership, ID externo como autoridad, precios históricos, stock sin transacción |
| `CatalogItemPrivateResponse` | ID, tipo, estado, versión, fechas UTC, relaciones propias, campos de negocio autorizados | secretos, tokens JWT, PII no necesaria, entidades ajenas |
| `CatalogVariantResponse` | ID, parent ID, SKU propio, atributos validados, estado y versión | parent/tenant distinto, SKU global no autorizado |
| `CatalogPriceQuoteResponse` | moneda, impuesto/tasa vigente validada, precio público, vigencia, snapshot/version | precio del cliente como verdad, mutable invoice/order snapshot |
| `CatalogStorefrontResponse` | slug público, nombre y media permitida, precio publicable, estado público | EmpresaId interno, costo, datos de inventario exactos, borradores, identificadores privados, trazas de usuario |
| `CatalogLegacyClassification` | ID interno, estado `OWNED/APPROVED_SHARED/QUARANTINED/UNKNOWN`, motivo seguro | autoasignación a tenant actual, clasificación por NULL=public |

**Reglas de compatibilidad:** los DTO privados y públicos son proyecciones distintas; un endpoint público no serializa entidades EF completas; no se expone la categoría como tenant-scoped hasta validar sus relaciones existentes. Un identificador presentado por el cliente sólo selecciona candidato para validación, no confiere acceso. Los cambios aditivos incorporarán versionado, mapeos explícitos y manejo de consumidores Angular/ERP; sin romper nombres actuales sin inventario de uso.

## 4. Validaciones y errores seguros

| Código conceptual | Condición que lo produce | Comportamiento exigido |
| --- | --- | --- |
| CAT-ERR-01 | JWT ausente/inválido en operación privada | `401`, sin datos, identidad no derivada del request |
| CAT-ERR-02 | Rol/membresía revocado o permiso insuficiente | `403`, sin detalles de entidad |
| CAT-ERR-03 | Recurso inexistente o ajeno | `404` no enumerativo / `403` según política existente, sin filtración A/B |
| CAT-ERR-04 | NULL legacy sin clasificación autorizada | Denegación por defecto; cuarentena y observabilidad sin PII |
| CAT-ERR-05 | Variante, atributo o SKU enlazado a parent ajeno | `422` / conflicto seguro sin persistir |
| CAT-ERR-06 | Categoría/impuesto no asociable o con ámbito desconocido | `422`, sin inferir tenant desde categoría |
| CAT-ERR-07 | ETag/version obsoleto | `409` o `412`, estado inalterado |
| CAT-ERR-08 | Duplicado de solicitud con payload distinto | `409` y no reutilizar respuesta de otra operación |
| CAT-ERR-09 | Precios/impuestos fuera de política/moneda/fecha | `422`, cotización servidora inalterada |
| CAT-ERR-10 | Solicitud de reserva para servicio no inventariable | `422` o contrato de no inventariable explícito |
| CAT-ERR-11 | Host no verificado o tienda no publicada | `404`, sin exponer comercio interno |
| CAT-ERR-12 | Solicitud demasiado grande/paginación abusiva | `400` / `429`, cuotas por tenant/actor/host |
| CAT-ERR-13 | Payload contiene autoridad forjada (EmpresaId, privilegios, costo interno) | Rechazar campo, nunca aceptarlo silenciosamente |
| CAT-ERR-14 | Fallo interno/proveedor o datos inconsistentes | Error genérico y correlationId seguro; nunca JWT, SQL, secretos ni PII |

Los códigos y HTTP status son propuestas sujetas al contrato de errores realmente vigente, que debe inspeccionarse antes del M03. No se autoriza degradar permisos existentes ni establecer `200` ficticio ante fallas de dependencia.

## 5. Invariantes de negocio y decisiones pendientes

1. **OWN-01:** `TenantId` efectivo proviene de auth/host server-side; actor A no lee/modifica producto ni variante B.
2. **OWN-02:** parent/variant y media están en el mismo ámbito; NULL legacy exige clasificación comprobada.
3. **OWN-03:** categorías/impuestos se asignan mediante vínculo permitido y reglas vigentes, no presuponer columna tenant inexistente.
4. **ERP-01:** factura/pedido y sus snapshots financieros son inmutables; cambios de precio/IVA afectan vigencias nuevas, no transacciones ya emitidas.
5. **ERP-02:** stock por sucursal y variante respeta pertenencia y transacción; servicios no crean stock ficticio.
6. **ERP-03:** borrador/archivado no aparece en proyección pública; el DTO público oculta costo y atributos internos.
7. **SEC-01:** revocación de membresía o cambio de rol se revalida en servidor incluso con JWT no expirado.
8. **OPS-01:** idempotencia se limita por tenant + actor + operación + payload hash + TTL, con límites y no exposición cruzada.
9. **OPS-02:** soft-delete, timestamps UTC, correlación y auditoría mínima no alteran datos históricos.
10. **COMP-01:** migración de consumidores ERP se ensaya con rutas actuales; no se promete compatibilidad de serialización por un diseño Markdown.

**Pendiente para M03:** confirmar modelo real de `Categoria`, mapeos EF, procedimientos de precio/impuesto, API y consumidores Angular; decidir asociaciones cross-tenant únicamente con autorización explícita y pruebas. No establecer una migración DDL por este contrato.

## 6. Ensayos futuros — NO EJECUTADOS

| Caso | Positivo esperado | Negativo esperado |
| --- | --- | --- |
| PN01 | ADMIN de A ve producto A | ADMIN de A no enumera B |
| PN02 | Variante A enlaza producto A | Variante B no se agrega al producto A |
| PN03 | Categoría permitida por asociación comprobada | Categoría no verificada no adquiere tenant implícito |
| PN04 | Impuesto vigente aplicado por servidor | Cliente no altera IVA/tasa |
| PN05 | Precio/version vigente genera cotización | Precio forjado no cambia factura histórica |
| PN06 | Stock sucursal A correcto | B y servicios no inventariables no contaminan stock |
| PN07 | Host A expone DTO mínimo publicado | Host A no ve borrador/costo/B |
| PN08 | Cambios con ETag vigente | ETag obsoleto rechaza sin mutación |
| PN09 | Idempotencia idéntica devuelve respuesta scoped | Repetir key con payload o tenant distinto no la reutiliza |
| PN10 | Actor con membresía vigente accede | Membresía revocada deniega en servidor |
| PN11 | Legacy NULL categorizado por procedimiento aprobado | NULL no se interpreta como global y queda en cuarentena |
| PN12 | Error correlacionado genérico | SQL/JWT/PII no aparece en cuerpo, log ni proyección |

Las 24 expectativas son **criterios para pruebas posteriores, NO pruebas ejecutadas**. Para M03 .NET requieren xUnit con positivos/negativos y CI de seguridad/auth exact SHA; persistencia real exige integración MySQL 8.4 efímero y rollback de migración; UI exige Angular/Playwright según diff. No atribuir PASS por escribir estos casos.

## 7. A0–A5 y aceptación de esta entrega

- **A0:** cotejo del Plan V5, M01.06, stack y compatibilidad; alcance documentado y sin cambios de runtime.
- **A1:** comprobar que operaciones/DTO/errores cubren funcionalidad y negativos por inspección de esta matriz; NO_APLICA ejecución HTTP/funcional al diff Markdown.
- **A2:** revisar A/B, NULL legacy, JWT/RBAC, categorías, precio/stock, roles revocados, PII/secretos; documentado fail-closed, no certificación de implementación.
- **A3:** cotejar coherencia con capas y contratos M01; NO_APLICA tests EF/MySQL/Angular/runtime cuando sólo cambia Markdown.
- **A4:** requiere commit fast-forward en `dev`, archivo Markdown + `CHANGELOG_AI.md` exclusivamente, verificación de Scope Lock y procedencia V5 `completed/success` sobre SHA exacto, readbacks de PLAN/COLA/DAG/BITACORA; hasta entonces `VALIDANDO`, sin certificación.
- **A5:** **pendiente de revisión real de otra de las cinco Tasks Javier**, con diff, SHA, gates y seguridad; no autoprobación ni QA/main/PROD.

**Rollback:** revertir de forma aditiva el commit documental si se demuestra incorrecto, preservando BITACORA y propietarios. Los futuros cambios de API/DB requieren plan de reversión propio, ensayo con fixtures y evidencia. Ninguna aprobación aquí autoriza publicación, migraciones, VPS, Cloudflare, secrets o datos productivos.
