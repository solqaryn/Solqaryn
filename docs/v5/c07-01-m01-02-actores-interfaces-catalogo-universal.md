# SOLQARYN V5 — C07-01.M01.02: actores, interfaces y limites del catalogo universal

**ID:** C07-01.M01.02, PLAN_MAESTRO fila 1011. **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Repositorio:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. Tipo: **preparacion documental**, no implementacion ni certificacion de servicios universales.

## Base contrastada y limitaciones

El ERP contiene controladores, servicio de productos, variantes, filtros Angular, tienda y DTO publicos. C07-01 busca integrar productos, servicios, variantes, categorias, impuestos, atributos y precios **sin duplicar capacidades actuales ni perder ownership**. C07-01.M01.01 tiene autor distinto, se encuentra bajo su propio flujo de validacion y no se asume su cierre; se inspecciona su documento publicado como contrato preliminar, no como autorizacion. Esta M01.02 usa el DAG individual VALIDADO/SI para identificar interfaces; **no desbloquea M01.06, migraciones, servicio nuevo, IAM o despliegue**.

Fuentes de codigo con blobs Git de dev (lectura dirigida, no auditoria exhaustiva):
- `docs/v5/c07-01-m01-01-alcance-catalogo-universal.md` — `4f91db421fe4c5e7ad5e0ea080035eabe8f8c1dd`: alcance y matriz preliminar no certificados por esta entrega.
- `backend/src/API/Controllers/ProductosController.cs` — `8b9e1f0600dd7ffe255552f237688ff5a8367fad`: `[Authorize]` y ruta `productos`, no demuestra todas las policies por endpoint.
- `backend/src/Application/Services/ProductoService.cs` — `045230de73cd1b4785b62a1b8c811c87b3d7fbd0`: casos de uso y repositorios existentes.
- `frontend/src/app/services/producto.service.ts` — `b51b4fb4a6595d6af83d1eb67da6e7321eed1db6`: DTO/filtros HTTP Angular, no autoridad de tenant.
- `frontend/src/app/features/productos/productos-list.component.ts` — `7022fb627469f3f304b7b93ac6708baf544fd87e`: listado y filtros Angular; no acredita A/B por sí solo.
- `frontend/src/app/features/productos/producto-variantes.component.ts` — `1c11ac61af62a4c43d531514292f5f6fdf3825f0`: componente de variantes.
- `backend/src/API/Controllers/TiendaController.cs` — `39a23ddb94b24077bbce79de1b965781b70ca37a`: tienda publica y rate limiting, no CRUD administrativo.
- `frontend/src/app/features/storefront/storefront.models.ts` — `de45fc1e9c0e3d1b98bd560b6e8006200d3a2b08`: variantes/precios/imagenes publicos.

M01.03 debe leer Domain, EF, permisos y consumidores restantes. No se deduce que categorias sean propias por `EmpresaId` sin inspeccion de sus asociaciones, ni que `EmpresaId=NULL` de producto legacy permita acceso global.

## A01–A12: actores, entradas y salidas autorizadas de diseno

| ID | Actor | Entrada/autoridad | Salida o denegacion |
| --- | --- | --- | --- |
| A01 | Visitante de tienda | Host/binding publico verificado | Ver oferta publica del tenant, no campos internos |
| A02 | Cliente comprador | Sesion/orden validada en backend | Cotizacion propia, sin rol ERP |
| A03 | Operador de productos | JWT/RBAC y membresia actual | Gestionar productos de su empresa |
| A04 | Administrador empresarial | Permisos vigentes en tenant | Configurar catalogo dentro de su ambito |
| A05 | Gestor de sucursal | Sucursal y permiso comprobados | Ver stock autorizado; sin inventario ajeno |
| A06 | Editor de variantes | Producto padre y SKU propios | Gestionar variante sin enlazar otro tenant |
| A07 | Gestor de precios | Permiso comercial y reglas de vigencia | Actualizar precios con auditoria |
| A08 | Responsable tributario | Reglas y periodo fiscal vigentes | Impuestos y snapshots consistentes |
| A09 | Responsable de servicios futuro | Contratos M02/M03 aun pendientes | No asumir un modulo universal implementado |
| A10 | Integracion externa futura | Credenciales limitadas y cuota | Acceso minimo, no cuenta privilegiada |
| A11 | Soporte plataforma | Permiso explicito trazable | Diagnostico sin bypass de RBAC |
| A12 | Crawler publico | Rutas publicadas y host permitido | Indexar solo productos visibles y datos autorizados |

## I01–I12: superficies y contratos existentes o FUTUROS

| ID | Interfaz | Estado/fuente | Contrato esperado y fallo seguro |
| --- | --- | --- | --- |
| I01 | ProductosController | API privada de productos | Entrada: JWT/DTO/permiso; salida: producto propio o rechazo |
| I02 | ProductoService | Casos de uso Application | Entrada: invariantes/empresa; salida: mutacion validada o rollback |
| I03 | ProductoVariante y formulario | ERP variantes | Entrada: producto padre + atributos; salida SKU/stock propio o conflicto |
| I04 | Categoria | Clasificacion ERP | Entrada: referencia de categoria; no presumir ownership si entidad no tiene EmpresaId |
| I05 | Impuesto | Tributacion ERP | Entrada: regla/tasa/vigencia; salida snapshot historico consistente |
| I06 | Stock por sucursal | Inventario ERP | Entrada: sucursal/autorizacion; salida disponibilidad autorizada |
| I07 | producto.service.ts | Cliente HTTP Angular privado | Entrada: filtros/paginacion; salida UI; no es seguridad servidor |
| I08 | productos-list.component.ts | Listado Angular | Entrada: filtros/vista; salida estados accesibles, no autoridad |
| I09 | TiendaController | Storefront publico | Entrada: host/storeKey/lineas; salida oferta filtrada sin costo interno |
| I10 | storefront.models.ts | DTO publico variante/oferta | Entrada: precio/imagen ofrecido; servidor decide cobro real |
| I11 | Catalogo de servicios futuro | No endpoint acreditado en este alcance | Requiere contratos de duracion, disponibilidad y no-stock |
| I12 | Auditoria/cambios futuros | Sin dispatcher autorizado | Salida: trazabilidad sin secretos ni datos cruzados |

## B01–B10: fronteras de confianza y compatibilidad ERP

- **B01:** B01 producto y variante deben pertenecer al mismo tenant; NULL historico no concede acceso global.
- **B02:** B02 categoria no se supone tenant-scoped sin leer consultas; proteger referencias.
- **B03:** B03 precios, impuestos y snapshots historicos no se recalculan arbitrariamente.
- **B04:** B04 stock de sucursal nunca se agrega a otra empresa.
- **B05:** B05 tienda publica no expone costos ni operaciones internas.
- **B06:** B06 precio o EmpresaId de navegador nunca es autoridad.
- **B07:** B07 servicio futuro no se contabiliza como mercaderia por error.
- **B08:** B08 media e imagenes requieren propiedad y enlace permitido.
- **B09:** B09 RBAC de ERP se comprueba en API y no en guard visual.
- **B10:** B10 control plane requiere permiso explicito auditado.

## Oraculos FUTUROS NO_EJECUTADOS

**Positivos P01–P08:** P01 A con JWT y permiso ve producto A; P02 variante del mismo producto y tenant valida SKU; P03 tienda A sirve solo oferta publicada de A; P04 paginacion/filtros accesibles a 320px; P05 stock de sucursal propia se comprueba server-side; P06 impuesto/precio/promocion se calcula en backend con snapshot; P07 checkout valida lineas con stock y precio real; P08 rollback normal preserva historia ERP.

**Negativos N01–N12:** N01 B no lee producto A; N02 variante de A no se enlaza a B; N03 sin RBAC no hay CRUD; N04 SKU conflictivo se rechaza; N05 draft no aparece publico; N06 tienda no expone costo privado; N07 header/slug falso no cambia tenant; N08 precio cliente no altera servidor; N09 imagen de otro tenant no se sirve; N10 stock ajeno no se ajusta; N11 servicio sin contrato no genera inventario ni cobro; N12 log/error no filtra PII, JWT ni secretos. Todos pendientes de pruebas HTTP/xUnit/EF/MySQL/Playwright y evidencia SHA, **no PASS de runtime por la existencia de esta tabla**.

## Analisis de impacto, A0–A5 y reversion

Afectacion real: solo este Markdown `docs/v5/` y encabezado con viñetas juntas en `CHANGELOG_AI.md`. Sin cambios de API/DTO, Domain/Application/Infrastructure, Angular, persistencia, tenant/JWT/RBAC, precios/stock, base productiva, worker/eventos, observabilidad runtime, secretos ni servicios de pago. Ninguna actualizacion arquitectonica.

**A0:** revision estatica de fuentes, tabla A/I/B y procedencia. **A1:** correspondencia de P01–P08 con contratos propuestos, NO_EJECUTADOS. **A2:** limites y N01–N12 como criterios negativos, NO_EJECUTADOS. **A3:** trazabilidad documental sobre fuentes actuales, runtime NO_APLICA a dos Markdown. **A4:** fast-forward CAS, diff de dos archivos, Scope Lock y verificador CHANGELOG con `completed/success` exact SHA, y PLAN/COLA/BITACORA readback. **A5:** solo otra Task Javier distinta del autor, sin permiso de QA/main/PROD.

**Rollback:** `git revert <SHA_PUBLICADO>` normal, BITACORA aditiva. En particular, no dar por terminado M01.01 ajeno ni liberar cierre M01.06, migraciones, IAM/tenant o eventos por este documento.
