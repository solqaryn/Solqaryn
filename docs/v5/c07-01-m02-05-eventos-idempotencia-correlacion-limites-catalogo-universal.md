# SOLQARYN V5 — C07-01.M02.05: eventos futuros, idempotencia, correlación y límites de catálogo universal

**ID:** C07-01.M02.05 · **Fuente:** Plan V5 FINAL Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` · **PLAN_MAESTRO:** fila 1020 · **Autor:** PRIMARY DEV 02 Task `6af16bb9492c8191b8e8c7d606f11507` · **Rama:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT, 2026-11-15.

## Alcance documental; prohibición de efectos

Este contrato describe un catálogo de eventos **hipotéticos e inertes** para productos, servicios, variantes, categorías, atributos, impuestos, precios, disponibilidad y exposición pública, con coexistencia segura con ERP legacy. **NO implementa bus, outbox, webhook, cola, worker, dispatcher, trigger, endpoint, base de datos, mensajería, cambio de esquema, payload de red o ejecución externa.** Cada evento se confirmará contra el modelo y los endpoints actuales antes de instrumentarlo en una futura M03/M05; no se garantiza que exista ahora.

Fuentes: M01.06 del catálogo universal con controles K01–K14; contratos M02 de entidades, API, persistencia y configuración sólo donde estén disponibles/validados. `Producto.EmpresaId` / `ProductoVariante.EmpresaId` legacy pueden ser NULL y `Categoria` no declara necesariamente `EmpresaId`; un evento con propiedad no acreditada debe ser rechazado o puesto en cuarentena, nunca distribuirse globalmente.

## Sobre de evento propuesto (no desplegado)

```text
EventEnvelopeV1 {
  eventId: UUIDv7/UUID aprobado por implementación;
  schemaVersion: version fija;
  eventType: nombre allowlisted;
  occurredAtUtc: fecha UTC creada por servidor;
  tenantScopeId: empresa derivada y validada por backend;
  actorScope: ID seudonimizado autorizado o SYSTEM verificado;
  aggregateType, aggregateId, aggregateVersion: datos de recurso propio;
  correlationId: opaco, no autoritativo;
  causationId: ID de evento/acción previa validada;
  idempotencyKeyHash: hash servidor con namespace de tenant+operación;
  payload: proyección mínima, sin secretos, PII, costo ni tokens;
}
```

**No se acepta tenant ni permisos del envelope entrante como autoridad.** En el caso de eventos públicos, el binding host/tenant se resuelve en servidor y se publica sólo un DTO público mínimo. Integridad, firma/HMAC, replay y TTL se diseñarán en un trabajo futuro, sin activar secretos ni canales ahora.

## Doce eventos futuros y garantías

| ID | Evento conceptual | Emisor propuesto | Datos mínimos/condición |
| --- | --- | --- | --- |
| EVT-01 | `CatalogDraftCreated` | Application tras transacción confirmada | ID, tipo, versión, tenant verificado; nunca exposición pública |
| EVT-02 | `CatalogItemUpdated` | Application | ID, diff allowlisted, versión; no snapshot histórico |
| EVT-03 | `CatalogVariantChanged` | Application | parent/variante A y versión; parent cross-tenant prohibido |
| EVT-04 | `CatalogCategoryLinked` | Application | vínculo validado sin asumir `Categoria.EmpresaId` |
| EVT-05 | `CatalogAttributeSchemaChanged` | Administración autorizada | version de esquema, sin valores arbitrarios ejecutables |
| EVT-06 | `CatalogTaxPolicyRebound` | Dominio autorizado | versión y vigencia futura, sin recálculo de facturas cerradas |
| EVT-07 | `CatalogPriceVersionPublished` | Servicio de precios | ID/version/vigencia; precio por servidor, snapshots intactos |
| EVT-08 | `CatalogItemPublished` | Application | item/host aprobados, no draft/costo/PII |
| EVT-09 | `CatalogItemUnpublished` | Application | versión y razón segura; invalidación futura scoped |
| EVT-10 | `CatalogStockAvailabilityChanged` | Servicio inventario | identificador propio y disponibilidad resumida, sin cantidades B |
| EVT-11 | `CatalogLegacyItemQuarantined` | Proceso autorizado de revisión | ID pseudónimo, clasificación y razón acotada, NULL no público |
| EVT-12 | `CatalogItemArchived` | Application | ID, tombstone/version, sin borrar documentos históricos |

Ninguno implica despliegue ni entrega garantizada; cada futuro consumidor debe verificar tenant, permisos, versión de contrato y replay. No autorizar evento comercial para generar pedido, cobrar o cambiar stock sin una operación/transacción certificada por separado.

## Diez invariantes de idempotencia, límite y entrega futura

1. Namespace de clave = `tenant efectivo + actor + tipo operación + aggregate + hash canónico de payload`; la misma clave en B **nunca** devuelve resultado de A.
2. Escritura de agregado y registro de evento/outbox futuro debe ser atómica en MySQL si se implementa; sin transacción verificada no emitir evento externo.
3. Reintentos se limitan por política, con backoff y TTL; no bucles infinitos ni efectos secundarios duplicados.
4. Misma clave/payload devuelve resultado semántico equivalente; misma clave con payload distinto genera conflicto seguro y auditoría mínima.
5. Consumidores validan `eventId`, versión de schema, tenant y versión de agregado; replay/duplicado se descarta antes de mutación.
6. `correlationId` se usa para observabilidad, **nunca** identidad, autorización o aislamiento.
7. No transmitir costo interno, JWT, refresh tokens, secretos HMAC, PII ni atributos de otros tenants; errores/logs son saneados.
8. Orden parcial por aggregate/version; no asumir orden global entre sucursales, productos o tenants.
9. Límite por tenant, actor y consumidor y circuito de backpressure; límite platform nunca editable a la baja por tenant.
10. Rollback conserva consistencia de precios/IVA/snapshots, inventario y pedidos; `CatalogItemArchived` no borra historial.

**Política de fallo futuro:** si falta ownership, el item legacy es NULL sin clasificación, falla validación de host, hay versión obsoleta o el proveedor no está disponible, **no publicar ni conceder acceso**. Registrar sólo un motivo seguro y correlationId. No crear un canal alternativo para saltar errores.

## Matriz de consumidores futuros (sin runtime)

| Consumidor objetivo | Permitido conceptualmente | No permitido |
| --- | --- | --- |
| ERP interno | Proyección autenticada propia para catálogo/stock | Mutación de facturas históricas o stock B |
| Storefront público | Revalidación de host e invalidación de caché scoped | Payload privado, producto draft, costo, catálogo A/B cruzado |
| Auditoría | IDs y transiciones mínimas con retención controlada | JWT, PII sensible, secreto de provider |
| Reportes | Agregados propios versionados | Mezclar snapshots de tenant B o reescribir venta emitida |
| Integración futura | Contrato versionado, autenticado y cuota explícita | Webhook sin firma/TTL/replay ni scope tenant |

## Aceptación futura — pares POS/NEG NO EJECUTADOS

| Caso | Futuro positivo | Futuro negativo |
| --- | --- | --- |
| PN01 | Cambio producto A emite un evento scoped A | Consumidor B no recibe payload A |
| PN02 | Reintento idéntico evita duplicado | Nueva carga con misma clave provoca conflicto |
| PN03 | Variante A referencia parent A | Variante B enlazada a parent A se rechaza |
| PN04 | Precio/IVA vigente se versiona | Factura emitida no se recalcula |
| PN05 | Host A invalida sólo caché A | Host B no purga catálogo A |
| PN06 | Revocación de rol anula publicación | JWT antiguo no autoriza emitir |
| PN07 | Legacy NULL se clasifica y cuarentena | NULL no se publica globalmente |
| PN08 | Orden por agregado se preserva | Mensaje viejo no pisa versión nueva |
| PN09 | Auditoría mínima conserva trazas permitidas | Logs/eventos no filtran secretos o PII |
| PN10 | Servicio no inventariable informa estado permitido | Evento no crea stock automático |
| PN11 | Falla proveedor se reintenta con techo | No hay publicación sin transacción/outbox |
| PN12 | Archivo lógico conserva historial | Archivo no elimina pedido/factura referida |

**FUTUROS_NO_EJECUTADOS:** no son pruebas de integración ni evidencias de que exista bus/outbox/dispositivo externo. Las pruebas futuras requieren xUnit/Integration de MySQL 8.4 efímero, checks de seguridad/JWT/RBAC exact SHA y pruebas de replay/doble entrega antes de una implementación real.

## Riesgos, dependencias, A0–A5 y rollback

- **Componentes futuros:** Domain agregados + Application transacciones/DTO + Infrastructure outbox/proveedor + API/Angular consumidores; *actual*: sólo docs/v5 y CHANGELOG_AI.md.
- **Dependencias:** M01.06 contrato de límites; M02.01–04 según estados actuales, A5 independientes y DAG real antes M02.06. No suponer que estos documentos implementan nada.
- **Datos y seguridad:** sin nuevas tablas, transacciones ni migración. Propuesta de evento con tenant A/B server-side, idempotencia/replay scoped y no-PII. No activación ni secreto en repo.
- **A0:** cotejar plan y límites K01–K14. **A1:** verificación conceptual de eventos/escenarios; ejecución runtime `NO_APLICA`. **A2:** rechazo de acceso cruzado/NULL/replay y protección de secretos mediante revisión documental; NO certifica implementación. **A3:** revisar consistencia con dominios y consumidores; pruebas de proveedor/MySQL `NO_APLICA` al diff Markdown. **A4:** commit fast-forward dev y Scope Lock + verificador V5 `completed/success` del SHA causal; BITACORA y readbacks. **A5:** otra Task Javier distinta del autor debe revisar diff, SHA y riesgos.
- **Rollback documental:** revertir commit de documento/CHANGELOG de forma aditiva, sin borrar BITACORA; futura retirada de un evento operativo exigirá drenaje/conciliación y rollback transaccional ensayado con datos sintéticos.
- **Prohibiciones:** no QA/main/PROD, DNS/R2/Hostinger reales, secrets, compras, cambios de Tasks o envío externo de eventos.
