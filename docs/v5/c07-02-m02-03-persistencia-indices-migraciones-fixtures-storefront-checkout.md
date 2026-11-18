# SOLQARYN V17 — C07-02.M02.03: persistencia conceptual, índices, reversibilidad y fixtures de storefront y checkout

**Microtarea:** `C07-02.M02.03` · **PLAN_MAESTRO:** fila 1054 · **Responsable:** PRIMARY DEV 02 — JAVIER_CHATGPT `6af16bb9492c8191b8e8c7d606f11507` · **Fecha:** 2026-11-15 · **Riesgo actual:** R0 documental sin ejecución · **Repositorio:** `solqaryn/Solqaryn@dev` · **Scope Lock:** STRICT.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y estado vigente PLAN/COLA/DAG. Alcance exacto: **definir** persistencia, índices, migraciones reversibles y datos de prueba futuros para tienda, carrito, checkout, pedidos y confirmaciones, sin crear tablas, clases EF, esquema, DDL, datos, ventas, cobros, reservas, endpoints, workers o integraciones activas. Todas las estructuras siguientes son **PROPUESTAS_NO_IMPLEMENTADAS**, sujetas a diseño final y CI R2/MySQL 8.4 efímero antes de cambios reales.

## 1. Fuentes y precondiciones

- M01.06 `docs/v5/c07-02-m01-06-gates-rollback-liberacion-storefront-checkout.md` — blob `122674cd481553a8bf2da3d420f379604211d9f1`; 16 puertas, 12 pares futuros, A5 independiente de DEV03.
- M02.01 `docs/v5/c07-02-m02-01-entidades-ownership-invariantes-storefront-checkout.md` — blob `f223040f96c6c45c02d8f1a37db86f2df2ab3a53`; E01–E16, INV01–INV20, 12 pares futuros. Su A5 es independiente del scope actual y no presume implementación.
- M02.02 `docs/v5/c07-02-m02-02-api-dto-errores-validaciones-storefront-checkout.md` — blob `ac0aabf6a85ad893e1be0a8ed2291d7eb75e0be1`; operaciones/DTO/error/validaciones conceptuales, **sin endpoints reales**.
- La ruta existente `POST tienda/checkout/validar` se trata como **cotización/validación**. No demuestra `OrdenComercial`, PSP, cargo, stock reservado o confirmación durable. Las entidades actuales del ERP deben inspeccionarse al implementar para evitar duplicados y efectos cruzados.
- Antes de CLAIM V17: `PLAN H1054=PENDIENTE/I vacío`, `COLA G1054/O1054=SI`, `DAG F1015=VALIDADO/G1015=SI`, exclusividad y namedRange `SQV17_C07_02_M02_03` ID `2000001054` para H:I. Una puerta documental SI no permite operar MySQL.

## 2. Doce agrupaciones lógicas propuestas; no son migraciones ni tablas existentes

| Grupo | Datos futuros mínimos | Owner/límite y protección |
| --- | --- | --- |
| D01 TiendaPublicada | empresa, tienda, estado, versión, rango UTC | Empresa servidor; no se infiere de Host enviado |
| D02 HostTiendaVerificado | empresa, tienda, host canónico verificado, titular, vigencia y versión histórica | Titularidad global exclusiva del host ACTIVO entre todas las empresas; no confiar en Host del cliente ni permitir fallback a B |
| D03 ProductoPublicable | empresa, producto/variante ERP, estado, snapshot publicable | No duplicar inventario ni publicar costo |
| D04 CarritoCliente | empresa, actor/sesión opaca, vencimiento, versión | Anónimo de A no lee carrito B |
| D05 LineaCarrito | empresa, carrito, variante, cantidad, versión precio | Cantidad positiva limitada; sin reserva de stock |
| D06 CotizacionCheckout | empresa, actor, huella líneas, subtotal/impuesto/moneda servidor, TTL | Cotización efímera, **no venta/pago** |
| D07 IntentoCheckout | empresa, actor, operación, idempotencyKey hash, payload hash, expira | A/B nunca comparten clave/resultados |
| D08 OrdenFutura | empresa, actor, estado, versión, correlación, moneda | Propuesta: durable solo tras aprobación y transacción |
| D09 MovimientoReservaStock | empresa, orden, variante, cantidad, estado, compensación | No tocar stock sin autorización y rollback probado |
| D10 IntentoPago | empresa, orden, proveedor, referencia tokenizada, estado | Nunca PAN ni clave HMAC; PSP no activo |
| D11 EventoPagoReconocido | empresa, proveedor, eventId, nonce hash, sello UTC, firma verificada | Callback inválido/replay no cambia estado |
| D12 AuditoriaOperacion | empresa, actor, recurso, resultado, razón, traceId, tiempo | Nunca JWT, PII excesiva ni secretos |

**Frontera de persistencia:** `EmpresaId` no nullable en registros nuevos, con FK compuesta por Empresa cuando corresponda. `Cliente/Venta.EmpresaId=NULL` históricos quedan `SIN_OWNER_CONFIRMADO`, sin adopción arbitraria. No introducir `Order`, `Payment` o `Credit` duplicados si existen conceptos ERP consolidados; el mapeo exige código vigente y aprobación R2. `CreditoCliente` no se reutiliza para PSP, puntos ni pago por inferencia.

## 3. Doce índices y restricciones candidatas con oráculos

| Índice lógico candidato | Clave futura propuesta | Caso de integridad / frontera |
| --- | --- | --- |
| I01 | UNIQUE(clave_host_activo_global) sobre registro verificado, valor NULL para inactivos | Host canónico ACTIVO con un solo titular global; colisiones A/B y dos versiones activas fallan |
| I02 | (empresa, tienda, estado_publicacion) | A no lista borradores ni elementos B |
| I03 | (empresa, tienda, producto, variante) | referencia cruzada a variante B rechazada |
| I04 | UNIQUE(empresa, carrito, linea_id) | una línea no se duplica en retry |
| I05 | (empresa, actor_hash, expira_utc) | expiración elimina solo A, sin exponer B |
| I06 | UNIQUE(empresa, actor_hash, operacion, clave_hash) | idempotencia A/B con keys iguales aislada |
| I07 | UNIQUE(empresa, orden_numero) | número repetido no crea doble orden |
| I08 | (empresa, orden, estado, version) | CAS evita transición concurrente |
| I09 | UNIQUE(empresa, orden, variante, reserva_version) | reserva duplicada o carrera rechazada |
| I10 | UNIQUE(empresa, proveedor, referencia_externa_hash) | callback duplicado no cobra dos veces |
| I11 | UNIQUE(empresa, proveedor, evento_hash, nonce_hash) | replay/nonce vencido se descarta |
| I12 | (empresa, recurso, fecha_utc, correlacion) | auditoría tenant-bound sin datos sensibles |

Estos son **índices candidatos**, no SQL revisado ni benchmarks ejecutados. Su selectividad, tamaño, unicidad y viabilidad deben medirse en MySQL 8.4 efímero con `EXPLAIN`, índices actuales e integridad referencial real. Un UNIQUE de clave sin empresa o una FK que permita `EmpresaId=NULL` daría falsas garantías: exigir negativos específicos antes de DDL. No modificar `Venta`, inventario, IVA ni reglas contables ahora.

### 3.1 Corrección P2 de A5: titularidad global de host (R0 documental)

El antiguo I01 no impedía que A y B reclamen simultáneamente el mismo host ni dos versiones activas del mismo tenant. El A5_FAIL P2 histórico sigue vigente hasta revisión independiente posterior a esta reparación.

- **Invariante:** host canónico verificado y activo tiene exactamente un titular global `EmpresaId` y una sola versión de publicación activa. Versiones retiradas son históricas; no autorizan fallback A/B.
- **Fuente confiable:** canonicalización determinista (minúsculas, punto terminal, IDNA, puerto y wildcard). Resolver por ingress/proxy autorizado con prueba vigente de dominio. `Host`/`X-Forwarded-Host` enviados por cliente no son autoridad; ante ambigüedad, rechazar.
- **Propuesta MySQL 8.4 sin DDL:** registro global de host verificado con clave derivada `clave_host_activo_global` = host canónico solo cuando activo/verificado, `NULL` en caso contrario; índice UNIQUE global sin `EmpresaId` en la clave única. MySQL admite múltiples NULL en UNIQUE: comprobar este detalle, FKs de titular y versiones históricas en integración efímera antes de implementar. Un índice sólo `UNIQUE(empresa,host,version)` no demuestra exclusividad global.
- **Concurrencia:** activación por transacción/CAS serializable sobre host tras verificar titularidad y versión; revocación auditada antes de liberar. Nunca upsert que cambie titular por Host no confiable ni caché de tenant previo.
- **Oráculos adversariales FUTUROS_NO_EJECUTADOS:** A activo + B mismo host => DENY; dos versiones activas A => DENY; A retira v1 y activa v2 => una activa; carrera A/B => un solo ganador; Unicode/IDNA ambiguo, wildcard solapado, host falsificado, proxy no confiable y caché obsoleta => DENY.
- **Gates futuros R2:** MySQL 8.4 efímero con Oracle EF 10, Category=Integration real, colisiones/concurrencia, EXPLAIN, Down/restore, HTTP JWT/RBAC tenant A/B negativos y CI exact-SHA. Se ejecutaron 0/6 oráculos runtime de titularidad en esta revisión; métricas NO_MEDIDO. Sin cambios MySQL, PSP, DNS, QA/main/PROD.

## 4. Plan futuro M01–M08 de cambio reversible (ningún paso ejecutado)

| Paso | Cambio futuro propuesto | Check de seguridad y rollback |
| --- | --- | --- |
| M01 | Snapshot y respaldo de esquema/datos sintéticos | Restore íntegro en MySQL 8.4 aislado antes de avanzar |
| M02 | Mapear entidades ERP/EF 10 existentes y ownership real | No duplicar tablas; `EmpresaId=NULL` en cuarentena |
| M03 | Crear tablas aditivas y FKs tenant compuestas | Esquema inicial reversible sin tocar ventas |
| M04 | Incorporar I01 de titularidad global y restantes índices tenant-bound | Probar colisión A/B, versiones activas, concurrencia y rollback en MySQL 8.4 aislado |
| M05 | Migrar datos sintéticos de prueba, sin datos productivos | Reconciliar conteos/checksums A/B |
| M06 | Implementar transacción orden/stock/idempotencia de ensayo | Carrera 2+ workers, fracaso parcial y reversión |
| M07 | Integrar PSP/HMAC sólo tras permiso/sandbox contractual | Firma falsa, expiración, replay y rotación negativa |
| M08 | Probar Down/restore y observabilidad de fallas | Comparar FKs, estados y no duplicación tras rollback |

**No ejecutar** el plan hasta que el gate M03/R2 correspondiente autorice código real y CI de MySQL 8.4 Oracle provider, con test Category=Integration **real**. Un build o job de migraciones sin ejecutar tests de DB no acredita M01–M08. No acceder Aiven real para probar esto.

## 5. Doce fixtures sintéticos y futuros pares P/N — NO_EJECUTADOS

| Fixture | Positivo futuro | Negativo / límite que debe fallar |
| --- | --- | --- |
| F01 | host canónico verificado con titular A activo liga sólo tienda A | B reclama ese host, doble versión activa, spoof o Host no confiable se rechazan |
| F02 | variante publicada A visible | borrador o variante B no se muestra |
| F03 | cantidad 1 y stock vigente cotizan | cero, negativo, exceso y overflow fallan |
| F04 | precio/IVA/moneda tomados servidor | monto falsificado en cliente no modifica |
| F05 | carrito A conserva líneas propias | actor anónimo B no lee A |
| F06 | cotización vigente responde sin escrituras | sólo validar no crea orden ni reserva |
| F07 | clave idempotente replay retorna efecto único | reintento concurrente no crea doble orden |
| F08 | dos empresas usan igual key sin colisión | respuesta de B no se reutiliza en A |
| F09 | reserva autorizada y stock suficiente concilian | sobreventa y fallo parcial revierten |
| F10 | evento PSP auténtico se reconoce una vez | firma falsa, nonce repetido o TTL vencido falla |
| F11 | rol autorizado consulta orden propia | JWT revocado/rol incorrecto/tenant B denegado |
| F12 | auditoría sin secretos y UI móvil honesta | PII/JWT en logs o redirect falso no confirma pago |

**Ejecución y métricas reales de esta microtarea:** `0/12` fixtures y `0/12` pares runtime ejecutados, `NO_MEDIDO` para MySQL, xUnit, Playwright, latencia, throughput o transacciones. Los escenarios son especificaciones y jamás se etiquetan PASS.

## 6. Matriz de seguridad, impacto, calidad y reversión

- **Tenant/identidad:** backend resuelve Empresa, host confiable, cliente/actor y propiedad de recurso; las claves cache, índices y auditoría incluyen ámbito A/B. El cliente no manda autoridad en host, slug, `EmpresaId`, cookie, JWT no validado o UI.
- **Integridad/finanzas:** separar cotización de orden, reserva, factura y cobro; idempotencia, monto/IVA de servidor, transacción consistente, reconciliación y compensación. Nunca declarar pago por redirect o callback sin firma.
- **Privacidad:** no PAN, JWT, HMAC key, secretos PSP, referencias sin tokenizar ni PII sensible en tablas de auditoría o logs. Rate limits y retención requieren decisión.
- **Superficies no tocadas:** Domain, Application, API, Infrastructure, clases EF/migrations, MySQL, Angular, rutas, pipelines, jobs, secretos, Aiven, datos, QA/main/PROD. Solo documentación contractual; no altera arquitectura efectiva.
- **A0:** diff limitado a este Markdown+CHANGELOG, prueba de predecesores y lock V17 por ID; **A1:** doce fixtures positivos/negativos definidos sin ejecución; **A2:** tenant A/B, JWT/RBAC, PSP/replay/consentimiento y denegaciones explícitas de diseño; **A3:** pruebas runtime .NET/EF/MySQL/HTTP/Angular/Playwright `NO_APLICA` por cambio R0 documental; **A4:** `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA causal o equivalencia íntegra, PLAN/COLA/DAG/BITACORA readback; **A5:** otra Task Javier revisa de forma independiente y puede rechazar.
- **Rollback documental:** commit de reversión ordinario de los dos cambios bajo HEAD nuevo, preservando modificaciones de otros autores, BITACORA y candados; cero retrocesos de datos. En futura DB, Down/restore y checksums reales previos a cualquier aprobación.

**Estado de salida R0:** especificación técnica conceptual, NO_CERTIFICADO_RUNTIME; el A5 independiente de esta unidad queda `PENDIENTE` hasta revisión verdadera. No se ha ejecutado ninguna migración ni prueba MySQL, registrado pedido, reservado stock, enviado dinero, llamado PSP o liberado QA/main/PROD.
