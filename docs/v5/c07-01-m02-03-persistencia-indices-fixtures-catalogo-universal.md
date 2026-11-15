# SOLQARYN V5 — C07-01.M02.03: persistencia, índices y fixtures de catálogo universal

**Autor:** PRIMARY DEV 01 — JAVIER; Task `6adbb9fff1548191a1bd5b30d9e9984a`. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO (Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`). **Fecha:** 2026-11-15.

## Alcance y dependencias

Diseño **estrictamente documental** de alternativas de persistencia, índices, constraints, migración y fixtures para productos, servicios, variantes, categorías, atributos, precios, impuestos, existencias y publicación multicanal. **No crea tablas, SQL, entidades EF, migraciones, API, UI, proveedores, claves, permisos ni datos.**

`C07-01.M01.06` publicó gates en commit `4664458f1263d91d49c63c66b09bf559f466dc9a` y obtuvo A5 independiente. El DAG propio de `C07-01.M02.03` era F=VALIDADO/G=SI y COLA G/O=SI, PLAN PENDIENTE sin owner al claim. `C07-01.M02.01` definió el modelo conceptual en SHA `e857a7c38e7a6188bfdcb710d5cfa08add766b45`, sin usar su A5 pendiente como prerrequisito: M02.03 es independiente por DAG. La actualización de M02.02 por DEV02 se respeta sin pisar su propiedad.

La auditoría M01.03 registró `Producto.EmpresaId` y `ProductoVariante.EmpresaId` posiblemente NULL por legado y `Categoria` sin `EmpresaId` propio. No inferir acceso público ni rellenar esos NULL con una empresa arbitraria. Antes de diseñar migraciones reales contrastar configuraciones EF, contratos de Producto/Variante/Categoria, servicios de precios/stock y snapshots de factura/pedido.

## Proyecciones y restricciones candidatas — NO CREADAS

| Proyección conceptual | Claves/relación a examinar | Regla de denegación |
| --- | --- | --- |
| CatálogoEmpresa | EmpresaId autenticada + versión | No duplica Empresa/UsuarioEmpresa |
| ArtículoCatalogable | Empresa efectiva + ID + tipo | Rechazar artículo B desde A |
| Variante | Padre + empresa + SKU + versión | Padre A no acepta variante B |
| AsociaciónCategoría | Producto + categoría + política | Categoria sin EmpresaId no concede acceso |
| AtributoTipado | Definición/versionado + tipo | Valor desconocido o incompatible se rechaza |
| ValorVariante | Variante + atributo + valor | Prohibir herencia cross-tenant |
| PrecioVigente | Empresa, artículo, moneda, rango UTC | Impedir solapamiento/retroactividad |
| ReglaFiscal | Autoridad/versionado/ámbito | Rechazar impuesto del cliente |
| ExistenciaSucursal | Empresa, sucursal, artículo/variante | No contabilizar servicio ni stock de B |
| PublicaciónCanal | Host verificado, empresa, artículo | No mostrar costo/borrador privado |
| MedioPublicable | Empresa, recurso opaco, visibilidad | URL ajena no es autorización |
| EvidenciaAuditoría | Empresa, actor, recurso, correlación | Nunca incluir JWT/HMAC/PII sensible |

Las proyecciones son **ideas sujetas a reconciliación con tablas existentes**, no clases nuevas ni certificación del esquema MySQL actual. Un índice compuesto o unique sólo se aprueba tras revisar cardinalidades, colaciones, duplicados históricos, soft delete, tamaño y costo de escrituras.

## Índices y constraints a evaluar en futuro M03

1. `(EmpresaId,Tipo,Estado,Id)` para paginación privada sin mezclar A/B.
2. `(AmbitoAutorizado,SkuNormalizado)` unique condicionado a política y duplicados existentes.
3. `(Empresa,ProductoId,VarianteId)` relación padre-hijo y versión concurrente.
4. `(ProductoId,CategoriaId,RelacionVersion)` asociación validada aun sin EmpresaId en Categoria.
5. `(Empresa,Articulo,Moneda,DesdeUtc,HastaUtc)` y detección de tarifas solapadas.
6. `(Empresa,Sucursal,Articulo,Version)` para existencias sin confusión interempresa.
7. `(Empresa,CanalVerificado,Articulo,Estado)` para publicación y revocación.
8. `(Variante,AtributoDefinido,ValorNormalizado)` con tipos y cardinalidad acotados.
9. `(Empresa,Articulo,Visibilidad,Orden)` para medios vinculados seguros.
10. `(Empresa,Recurso,OcurridoUtc,Correlacion)` para auditoría no enumerativa.
11. `(Empresa,Operacion,IdempotencyKey,PayloadDigest)` para rechazo de claves ambiguas.
12. `(IdLegado,EstadoClasificacion)` para cuarentena de ownership NULL.

**No ejecutar DDL** ni presentar esta lista como decisión sobre columnas reales. MySQL 8.4 efímero, migraciones EF10 versionadas y muestras sintéticas son requisitos de CI en una fase futura. La unicidad por empresa y el manejo de NULL/soft-delete deben probarse antes de aplicar constraints sobre históricos.

## Invariantes de integridad y backfill

**DB01** empresa efectiva deriva de JWT/membresía/RBAC del servidor, no ID ni host del cliente. **DB02** NULL legado queda restringido hasta adjudicación probada, nunca público por defecto. **DB03** categoría sin EmpresaId no implica propiedad global. **DB04** variante y padre conservan empresa, versión y FK coherente. **DB05** precio/impuesto presente no modifica snapshots históricos. **DB06** servicio no crea existencias físicas. **DB07** sucursal B no satisface stock de A. **DB08** SKU/atributo/moneda ambiguos o repetidos se rechazan. **DB09** cambios concurrentes deben usar expectedVersion/transacción. **DB10** migración futura debe ser aditiva/expand-contract y reversible sin pérdida. **DB11** auditoría sin tokens, URL firmada ni PII. **DB12** backfill interrumpido reanuda idempotentemente sin generar duplicados o abrir lectura ajena.

## Fixtures sintéticos FUTUROS_NO_EJECUTADOS

| Positivo exigido | Negativo obligatorio |
| --- | --- |
| F01 Producto A pertenece a A | N01 B consulta o edita A |
| F02 Variante A coincide con padre A | N02 Variante B se adjunta a A |
| F03 NULL legado queda restringido | N03 NULL se publica global |
| F04 Categoría asignada por relación válida | N04 Categoria sin EmpresaId salta RBAC |
| F05 Precio con moneda/vigencia validada | N05 Solapamiento reescribe factura |
| F06 SKU único en ámbito autorizado | N06 Colisión oculta entre variantes |
| F07 Servicio no inventariable no tiene stock | N07 Servicio agrega unidades físicas |
| F08 Sucursal A consulta sólo stock A | N08 Stock B satisface pedido A |
| F09 Medio A enlazado a producto A | N09 URL de B se publica en A |
| F10 Host A emite DTO público de A | N10 Caché cruza host o tenant |
| F11 ExpectedVersion correcto actualiza | N11 Escritura vieja pisa versión nueva |
| F12 Backfill reanudable conserva estado | N12 Retry duplica filas o pierde referencias |

Son **expectativas documentadas**, no fixtures implementados, casos xUnit/HTTP/MySQL/Playwright ejecutados ni evidencia de cobros o cambios de datos reales.

## A0–A5, CI causal, seguridad y rollback

**A0:** Skill/HEAD, fuente, Scope Lock y owner CAS; dos Markdown (este documento + sección autocontenida CHANGELOG). **A1:** cotejar 12 proyecciones, 12 índices, 12 reglas DB y 12 pares futuros sin simular tests. **A2:** revisar tenant A/B, categorías/NULL legado, seguridad JWT/RBAC, invariantes de precio/stock y privacidad en diseño fail-closed. **A3:** compatibilidad Domain/Application/Infrastructure/API/Angular a nivel contrato; compilaciones, SQL, EF/MySQL y navegador son `NO_APLICA` al diff documental, nunca PASS. **A4:** publicar fast-forward/CAS en dev, comparar commits/blobs/diff, requerir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success para SHA causal y readbacks PLAN/COLA/DAG/BITACORA; CANCELLED/SKIPPED/IN_PROGRESS jamás PASS requerido.

**A5:** revisión independiente por otra de las cinco Tasks Javier, nunca el autor. LISTO_DEV documental no habilita migración, M03 IAM, QA/main/PROD, dominios, datos ni servicios pagos.

**Rollback documental:** commit aditivo o `git revert` normal sin reescribir historia ni BITACORA. **Rollback futuro NO REALIZADO:** expand-contract, backup y reversión ensayados con MySQL 8.4 efímero y snapshots de facturas/pedidos, antes de solicitar autorización de cambio real.
