# SOLQARYN V5 — C07-01.M02.01: entidades, ownership e invariantes del catálogo universal

**Autor:** PRIMARY DEV 01 — JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO/COLA/DAG_TECNICO, Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Fecha:** 2026-11-15.

## 1. Contrato de admisión y no-alcance

Esta microtarea **documental M02.01** define entidades conceptuales, ownership, relaciones y reglas de invariantes para productos, servicios, variantes, categorías, atributos, precios, impuestos, medios y existencias, compatibles con ERP actual. **No se crean clases, tablas, índices, migraciones, endpoints, UI, webhooks, proveedores, permisos, secretos ni datos reales.**

El contrato previo `C07-01.M01.06` se publicó en SHA `4664458f1263d91d49c63c66b09bf559f466dc9a` con documento blob `497a938fdc475d625e8edb28af7677972bd7bd85`; recibió A5 independiente de Task Javier distinta del autor, sin autoaprobación. Tras ese readback, `C07-01.M02.01` quedó PLAN PENDIENTE, owner vacío, DAG F=VALIDADO/G=SI y COLA G/O=SI. Una sucesora M02 exige nuevo preflight propio; M03 runtime, M01/M06 terminales, QA/main/PROD y migraciones conservan gates separados.

`C07-01.M01.03` documenta que `Producto.EmpresaId` y `ProductoVariante.EmpresaId` pueden ser NULL en datos legados, y `Categoria` no declara `EmpresaId` propio. Nada de ello concede visibilidad pública ni autoridad a un tenant. La vinculación efectiva debe cotejar servicios, repositorios, permisos, mapeos y configuración EF **antes** de futuras implementaciones. No duplicar identidad `Empresa`, `UsuarioEmpresa` ni estructura comercial histórica.

## 2. Entidades y relaciones conceptuales — NO CREADAS

| Elemento | Clave/propietario conceptual | Límite y relación |
| --- | --- | --- |
| CatálogoEmpresa | Empresa autenticada + versión | Ámbito lógico; no duplica Empresa ni membresía |
| Artículo | ID opaco, empresa efectiva, tipo Producto/Servicio | Ref. compatible con Producto existente; un tipo no da permiso |
| VarianteArtículo | ID, padre y versión | Padre y variante comparten empresa y ciclo de vida |
| ClasificaciónArtículo | Categoría + asociación validada | No inferir tenant desde categoría global o sin EmpresaId |
| AtributoDefinido | Código, tipo, catálogo permitido | Valores tipados; evitar expresiones arbitrarias |
| ValorAtributo | Artículo/variante y atributo autorizado | Misma empresa, cardinalidad y versión del padre |
| PrecioConVigencia | Empresa, artículo, moneda, intervalo | No altera snapshot contable o precio de venta histórica |
| PolíticaImpositiva | Regla fiscal aprobada/versionada | No aceptar impuesto calculado por navegador |
| ExistenciaSucursal | Empresa, ubicación, artículo | Servicio no inventariable; no mezclar sucursales/empresas |
| MedioArtículo | Ref. opaca, empresa, visibilidad | Sin token privado ni permisos por mera URL |
| PublicaciónCanal | Empresa, host/canal verificado, estado | Sólo atributos públicos, sin costo o borradores |
| AuditoríaCambio | Actor, ámbito, correlación, versión | Inmutable/saneada, sin JWT, HMAC o PII innecesaria |

Cardinalidades propuestas: Empresa 1:N Artículo; Artículo 1:N Variante; Artículo N:M Clasificación mediante asociación comprobada; Artículo/Variante 1:N Precio; Artículo/Variante 1:N ExistenciaSucursal; PublicaciónCanal N:M Artículo con visibilidad explícita. Estas relaciones **no certifican** un esquema EF o transacciones existentes.

## 3. Invariantes que deberá probar una implementación futura

1. **CAT01:** empresa efectiva obtenida de autenticación, membresía y autorización servidor, jamás de `EmpresaId`, slug, host o cabecera cliente.
2. **CAT02:** RBAC específico para lectura privada, edición, publicación, precios y existencias, revalidado en cada operación.
3. **CAT03:** actor A no puede consultar, editar, publicar, reservar ni enlazar artículo/variante de B.
4. **CAT04:** `EmpresaId=NULL` legado requiere clasificación explícita y deny-by-default; no se transforma en catálogo público.
5. **CAT05:** variante/padre comparten propietario; prohibir asociaciones cruzadas o reasignación sin autorización.
6. **CAT06:** categoría sin EmpresaId directo no implica propiedad global; se valida la asociación aplicable.
7. **CAT07:** servicio sin inventario no crea stock, reserva, conteo físico ni corrección de existencias.
8. **CAT08:** sucursal/almacén pertenece a empresa efectiva; unidades de B no satisfacen órdenes de A.
9. **CAT09:** precio/moneda/impuesto/descuento se determinan o validan en servidor con precisión decimal; no aceptar monto del navegador.
10. **CAT10:** modificaciones actuales no reescriben precios o impuestos confirmados de facturas, pedidos y asientos históricos.
11. **CAT11:** modificaciones concurrentes requieren versión esperada o transacción equivalente; conflicto se rechaza sin overwrite.
12. **CAT12:** borrado lógico/despublicación preserva referencias comerciales y auditoría; no cascada física destructiva.
13. **CAT13:** unicidad de SKU se analiza por alcance autorizado; colisiones se deniegan sin asumir globalidad.
14. **CAT14:** caché y búsqueda incluyen empresa, canal, permisos y versión; no reutilizar respuestas entre tenants.
15. **CAT15:** tienda pública expone DTO mínimo permitido y host verificado, nunca costo, borrador o token interno.
16. **CAT16:** media asociada verifica ownership/estado; URL externa y nombre de archivo no constituyen autorización.
17. **CAT17:** ausencia, revocación o recurso cross-tenant retorna errores saneados no enumerativos.
18. **CAT18:** trazas y métricas protegen datos sensibles y separan empresa, actor, correlación y operación.
19. **CAT19:** cambios de contratos requieren compatibilidad API/Application/Domain/Infrastructure/Angular y rollback verificable.
20. **CAT20:** futuras escrituras .NET10/EF/MySQL8.4/Angular exigen pruebas reales positivas/negativas, A/B, CI causal y Scope Lock exact-SHA.

## 4. Casos futuros positivos/negativos — NO EJECUTADOS

| Positivo futuro | Negativo de rechazo obligatorio |
| --- | --- |
| P01 A consulta artículo autorizado de A | N01 A consulta artículo B por ID |
| P02 Variante A pertenece a padre A | N02 Variante B se adjunta a A |
| P03 NULL legado queda bajo política restrictiva | N03 NULL se publica globalmente |
| P04 Clasificación se aplica por asociación validada | N04 Categoría ajena evita RBAC |
| P05 Servicio no inventariable se muestra correctamente | N05 Servicio altera stock físico |
| P06 Precio/impuesto válido se calcula en servidor | N06 Cliente cambia moneda/importe final |
| P07 Factura histórica conserva snapshot | N07 Precio nuevo reescribe factura vieja |
| P08 Stock A se limita a sucursal A | N08 Consulta de A recibe existencias B |
| P09 Versión correcta permite modificar | N09 Versión vieja pisa commit reciente |
| P10 Host de A muestra DTO público de A | N10 Host de A expone borrador B |
| P11 Revocar membresía deniega edición | N11 JWT antiguo mantiene privilegio |
| P12 Despublicación conserva historia | N12 Borrado físico rompe referencias |

P01–P12/N01–N12 son **oráculos de implementación futura**, no tests xUnit, MySQL, Playwright, HTTP o evidencia de ausencia de vulnerabilidades ejecutadas por esta entrega documental.

## 5. Evidencia proporcional A0–A5 y reversión

**A0:** Skill/HEAD/Scope Lock y fuentes V5, DAG/COLA/owner CAS y diff solo este Markdown + sección autocontenida de `CHANGELOG_AI.md`. **A1:** coherencia documental de doce entidades, veinte invariantes y doce pares de escenarios futuros; no confundir especificación con ejecución. **A2:** revisar tenant A/B, NULL legado, categoría sin EmpresaId, JWT/RBAC, precios históricos, stock y privacidad, sin otorgar autorización. **A3:** referencias Domain/Application/Infrastructure/API/Angular y compatibilidad ERP a nivel contrato; CI runtime, MySQL y browser NO_APLICA al diff Markdown, nunca PASS. **A4:** publicar fast-forward exclusivo sobre HEAD actual; comprobar SHA, blobs, diff, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact-SHA, con readback PLAN/COLA/DAG/BITACORA. Checks required queued/failed/cancelled/skipped no constituyen PASS.

**A5:** Task Javier diferente del autor DEV01 revisa diff, SHA, fuentes, seguridad y evidencia y registra PASS/FAIL independiente; LISTO_DEV documental por sí solo no libera M03, QA/main/PROD ni migraciones.

**Rollback:** corrección o `git revert` aditivo de Markdown/CHANGELOG del SHA causal, sin reescritura de historia ni borrar BITACORA. Cualquier cambio futuro de datos necesita backup, ensayo de reversión y conservación de snapshot contable en MySQL efímero antes de habilitación; el documento presente no modifica datos reales.
