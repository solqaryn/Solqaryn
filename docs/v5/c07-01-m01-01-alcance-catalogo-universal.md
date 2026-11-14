# SOLQARYN V5 — C07-01.M01.01 — Alcance y no regresión del catálogo universal

**Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, bloque C07-01 | Catálogo universal, fase M01 | Alcance y preparación. **Fecha:** 2026-11-14. **Tipo:** contrato de análisis documental; NO es implementación de catálogo nuevo ni liberación M01.06/M06.

## 1. Objetivo verificable

Definir la frontera de compatibilidad para productos, servicios, variantes, categorías, impuestos, atributos y precios de un futuro catálogo universal con ownership empresarial explícito. La entrega requerida de M01 es una matriz de ownership y compatibilidad con el ERP existente; esta microtarea fija el alcance, las exclusiones, las invariantes y el método de evidencia, sin certificar todavía el conjunto completo de M01. Se preservan las capacidades actuales mientras la evolución universal sigue pendiente de fases y auditorías posteriores.

**Resultado de C07-01.M01.01:** un contrato de alcance que impide tratar la existencia de clases actuales como prueba de aislamiento multitenant universal o de disponibilidad comercial nueva. La evidencia de implementación real se exige después en código, pruebas positivas/negativas, migraciones sólo si autorizadas y CI exact-SHA.

## 2. Código real contrastado (blobs en dev, corte 2026-11-14)

| Fuente actual | Blob verificado | Observación delimitada |
| --- | --- | --- |
| `backend/src/Domain/Entities/Producto.cs` | `19632ce1aebe6a9cbc4379523367531852ee3e7e` | `EmpresaId` nullable por compatibilidad histórica; nombre, precio, costo, categoría, cantidades, galerías y variantes; no convierte NULL en acceso permitido |
| `backend/src/Domain/Entities/ProductoVariante.cs` | `de0dce026a147bb52ad46c952a2e9231015c195f` | `EmpresaId` nullable histórico, SKU, precio opcional, dimensiones, stock y controles de lote/serie/vencimiento opt-in |
| `backend/src/Domain/Entities/Categoria.cs` | `45e02b38473c89b944aeb02b5affcf65beb51f51` | Clase sin `EmpresaId` declarado: NO presumir categoría aislada por tenant sin inspeccionar consultas y otras capas |
| `backend/src/Domain/Entities/Impuesto.cs` | `f55d1e68b951ddb8a3daa3b9482f76b433d2470d` | Reglas de tasa, prioridad, período y snapshots; NO asumir tributación universal por empresa sin rastreo de asociaciones |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | Raíz de identidad comercial, empresa activa/inactiva; no sustituye membership, roles y guards de aplicación |
| `backend/src/Domain/Entities/PublicStoreBinding.cs` | `dae03bcd963281d74e1d02e460934ab0783d39d5` | Binding público host/storeKey/empresa; no implica autorización a consultar inventario, precio ni impuestos de otras empresas |

Superficies existentes identificadas, **no revisadas exhaustivamente en esta microtarea**: `ProductosController.cs`, `ProductoVariantesController.cs`, `CategoriasController.cs`, `ImpuestosController.cs`, `TiendaController.cs`, `TiendaBindingController.cs`, y `frontend/src/app/features/{productos,categorias,inventario,impuestos,storefront}`. M01.03 debe contrastar flujos, endpoints, pruebas, EF y consumidores concretos antes de derivar conclusiones.

## 3. Matriz preliminar de ownership y compatibilidad

| Recurso / flujo | Autoridad esperada y frontera | Compatibilidad que debe conservarse | Prueba futura obligatoria |
| --- | --- | --- | --- |
| Producto | Empresa activa autorizada por servidor; NULL legado queda en cuarentena | ERP de productos, precios, costos, desactivación y documentos históricos | A lee A; B no lee/escribe A; NULL no es global |
| Variante y atributo | Empresa válida Y producto padre del mismo tenant | Variantes, SKU, dimensiones, stock consolidado, lote/serie opt-in | Rechazar parent tenant distinto, SKU ajeno, fuga en imágenes |
| Categoría | Resolver alcance real en EF/consultas antes de atribuir ownership | Navegación y clasificación existentes | Evitar categoría ajena o enlazado cruzado; sin inventar `EmpresaId` |
| Impuesto/precio | Reglas vigentes y snapshots históricos; autoridad y período comprobados | Facturación, descuentos, tasas históricas y costo de ERP | No recalcular factura histórica por política nueva; impedir tasa ajena |
| Tienda pública | Resolver binding host/storeKey a una sola empresa y limitar proyecciones | Storefront sin exponer costo, datos privados ni otro catálogo | Host A no revela inventario privado de B ni IDs inferibles |
| Servicio no inventariable | Diferenciar explícitamente de mercadería y transacciones físicas | `TipoInventario` y procesos ERP existentes | Servicio no genera stock inventariable por error |
| Publicación y auditoría | Backend decide RBAC/tenant, registra actor y correlación | Guards UI como UX, no fuente única de autorización | JWT revocado, rol insuficiente, usuario bloqueado: denegación |
| Integraciones externas | Ningún proveedor nuevo habilitado en M01 | Compatibilidad Cloudinary/ventas/ERP existente | No activar API, webhooks, pagos, deploy o dominios por documentación |

Esta matriz **es propuesta de verificación**, no afirmación de que todas las autoridades ya estén implementadas. El diseño universal exigirá contratos explícitos antes de modificar persistencia o rutas.

## 4. Invariantes que no pueden degradarse

- I01. Cada mutación consulta empresa, usuario, membresía y rol vigentes del lado servidor; ocultar un botón no autoriza.
- I02. Un identificador de empresa enviado por cliente no desplaza el tenant autenticado ni el contexto confiable.
- I03. Productos/variantes históricos con `EmpresaId=NULL` no se interpretan como recursos globales disponibles para cualquiera.
- I04. Una variante no puede referenciar producto o dimensión de otra empresa; resolver el alcance de categorías e impuestos antes de writes.
- I05. Precio, costo, descuento, impuestos y snapshots transaccionales históricos conservan consistencia y trazabilidad.
- I06. No convertir servicios en movimientos físicos ni alterar inventario, lotes, series o vencimientos sin su política.
- I07. El stock de variantes activas y el stock consolidado no se equiparan sin atender reglas de variantes inactivas ya presentes.
- I08. Proyección pública usa binding verificado y DTO mínimo; nunca exponer costos, PII, JWT ni inventario privado.
- I09. Categorías, atributos, impuestos y URLs se filtran en el servidor y se prueban con empresas A/B disjuntas.
- I10. Cambios aditivos y reversibles; sin eliminación física ni reescritura de facturas/pedidos.
- I11. Errores y validación son deterministas, sin enumeración de recursos de otro tenant.
- I12. Auditoría registra evento, actor, contexto y evidencias sin secretos y con lectura autorizada.

## 5. Escenarios de aceptación FUTUROS — no ejecutados

**Positivos P01–P06:** crear/ver producto propio con tenant A autorizado; manejar variantes/stock propio sin divergencia; consultar categoría válida; aplicar tasa/precio vigente; renderizar tienda de host A sólo con publicaciones de A; conservar lectura de documentos históricos y snapshots.

**Negativos N01–N12:** forzar `EmpresaId=B` desde usuario A; consultar variante B; enlazar producto A a categoría ajena; usar impuesto ajeno o fuera de vigencia; exponer costo por storefront; cambiar SKU de B; tratar `EmpresaId=NULL` como público; registrar venta con tenant mixto; revocar rol durante sesión; usar host no vinculado; filtrar datos privados por error 404/403; alterar snapshot de factura existente. Todos: **NO_EJECUTADOS**. Son oráculos exigibles para fases de código, no PASS de backend/MySQL/Playwright en este documento.

## 6. Dependencias, impacto, pruebas y rollback

- **DAG:** `DAG_TECNICO!A971:J971` F=VALIDADO/G=SI; lista de preparaciones `C01-01..04`, `C02-01..04` y `C05-01..04`, todas M01.01–05 comprobadas **60/60 LISTO**. Esto habilita **sólo preparación documental**; no libera gateways, cierres terminales, JWT/RBAC/tenant, migraciones ni QA/PROD.
- **Alcance del changeset:** únicamente este Markdown y una sección autocontenida en `CHANGELOG_AI.md`; ninguna clase, API, DTO, migración, ruta Angular, servicio o configuración operativa cambia.
- **A0:** cotejo de fuente, estado, árbol Git, exclusiones y dependencia; **A1:** revisión positiva de alcance y compatibilidad en documento; **A2:** revisión de negativos multitenant y privacidad, no prueba en runtime; **A3:** NO_APLICA xUnit/Playwright/MySQL para un Markdown; **A4:** publicación fast-forward con Scope Lock y verificador V5 exact-SHA y readback institucional.
- **A5:** requiere revisor de otra Task Javier distinta de la autora; `LISTO_DEV` no autoriza otros entornos.
- **Rollback:** `git revert` no destructivo del commit documental, luego reconciliar PLAN/COLA/BITACORA mediante evento compensatorio nuevo; jamás reescribir auditoría histórica.

## 7. Decisiones fuera de esta unidad

La jerarquía definitiva de categoría, reglas multiempresa de impuesto, política de variantes por tenant, precios multimoneda, publicación/SEO y tratamiento de legado NULL requieren trazado causal y decisiones de fases posteriores. Sin evidencia no se declara plataforma universal ejecutable, endpoint nuevo, corrección de seguridad completa ni métrica de rendimiento.
