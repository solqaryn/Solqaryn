# ERP-N3.2 — Pedidos de venta — Auditoría y preflight

## Estado

**N3.2.A — LISTO por inspección dirigida.**

Este documento es exclusivamente preflight. No crea todavía dominio, persistencia, API ni frontend de PedidoVenta.

## Objetivo rector

Materializar en N3.2 un **PedidoVenta** como documento comercial propio, originable desde una Cotización, sin confundirlo con la `Venta` legacy ni adelantar stock, facturación, cobro, despacho u otras responsabilidades de puntos posteriores.

Fuente rectora: Plan Maestro ERP V5 / N3.2 — Pedidos.

## Evidencia fresca del repositorio

### Cotización ya existe y es la dependencia inmediata

ERP-N3.1 quedó cerrado antes de abrir N3.2. El agregado `Cotizacion` mantiene snapshots de cliente/producto y lifecycle:

`Borrador → Enviada → Aceptada/Rechazada → Convertida`.

La conversión ya es una transición explícita del agregado, pero N3.1 no materializa todavía un PedidoVenta. N3.2 debe definir cómo se crea el nuevo documento sin convertir a Cotización en segunda autoridad del Pedido.

### PedidoVenta no existe todavía

La búsqueda dirigida del repositorio por `PedidoVenta` y `Pedido` no devolvió una entidad, repositorio, servicio, controller ni frontend canónico de pedidos. Por tanto N3.2 no debe intentar reconciliar una implementación paralela oculta: debe introducir el agregado de forma explícita en B y persistirlo recién en C.

### `Venta` legacy no es PedidoVenta

`Venta` ya existe como documento/transacción histórica con número de venta, cliente, estado documental/pago, método de pago, importes, descuentos, impuestos, costo de envío, detalles y factura. N3.2 no debe renombrar ni reciclar esa entidad como PedidoVenta porque mezclaría responsabilidades y rompería el límite funcional del Plan Maestro.

## Alcance de N3.2

### Dentro de alcance

- crear un agregado `PedidoVenta` independiente;
- permitir origen opcional desde `Cotizacion` únicamente cuando el contrato de B lo autorice;
- conservar snapshots comerciales necesarios para que el Pedido no dependa de cambios posteriores en cliente/productos;
- definir identidad, lifecycle, invariantes, idempotencia y reglas de edición en N3.2.B;
- persistir el documento y su relación opcional con Cotización en N3.2.C;
- Application/API, frontend, RBAC/auditoría/seguridad, QA y certificación en D–H.

### Fuera de alcance de N3.2.A

- reservar/descontar stock;
- crear movimientos de Kardex;
- facturar o cobrar;
- crear movimientos financieros;
- implementar despacho/entrega;
- introducir scoring comercial, crédito del cliente o reglas de fulfillment no demostradas;
- alterar la semántica histórica de `Venta`.

Si una de estas responsabilidades resulta necesaria para un punto posterior, debe entrar por su MICROTAREA/gate correspondiente, no por el preflight.

## Contrato de origen desde Cotización

N3.2.B debe resolver de forma explícita y fail-closed las siguientes decisiones, sin asumirlas en este preflight:

1. si `PedidoVenta.CotizacionId` será nullable y qué estado exacto de Cotización permite crear el Pedido;
2. si la creación del Pedido provoca o no la transición `Cotizacion.Convertir`; si la provoca, ambas mutaciones deben ser atómicas en Application y no por efectos laterales del dominio;
3. qué campos se copian como snapshot desde Cotización y cuáles son editables posteriormente;
4. cómo se impide generar accidentalmente más de un Pedido desde la misma Cotización si el negocio exige 1:1;
5. lifecycle exacto de PedidoVenta y qué transiciones pertenecen a N3.2 frente a módulos posteriores.

Hasta B, esas decisiones permanecen **DECISION_PENDING**, no se documentan como hechos.

## Dependencias y superficies afectadas previstas

### Dominio

Posibles archivos nuevos, sujetos a B:

- `backend/src/Domain/Entities/PedidoVenta.cs`;
- `backend/src/Domain/Entities/PedidoVentaDetalle.cs`;
- enum(s) de estado estrictamente necesarios;
- pruebas de dominio N3.2.

### Persistencia

Reservada a C:

- configuraciones EF;
- `DbSet` sólo si el patrón actual lo requiere explícitamente;
- migración canónica;
- snapshot EF;
- preflight/postcheck/rollback;
- FKs restrictivas hacia Cliente/Cotización/Producto/Variante cuando el contrato B las confirme.

### Application/API

Reservada a D:

- DTOs/filtros/paginación;
- repository/service;
- transacciones y locking en mutaciones sensibles;
- idempotencia;
- controller/API;
- DI y ProblemDetails.

### Frontend

Reservada a E:

- listado, detalle y create/edit;
- creación desde Cotización cuando el contrato exista;
- rutas/navegación/RBAC;
- loading/error/vacío/responsive/accesibilidad.

## Riesgos detectados

### R1 — Doble autoridad Pedido/Venta

**Severidad:** alta.

Mitigación: PedidoVenta debe ser agregado propio. `Venta` legacy permanece intacta hasta que un punto rector posterior defina una transición/conversión explícita.

### R2 — Doble conversión de Cotización

**Severidad:** alta.

Mitigación: B debe fijar cardinalidad/idempotencia y D debe serializar la creación desde Cotización si existe riesgo concurrente.

### R3 — Snapshots incompletos

**Severidad:** media.

Mitigación: reutilizar el patrón probado por Cotización para no depender de nombres/datos mutables del cliente o producto después del pedido.

### R4 — Adelantar stock/facturación

**Severidad:** alta.

Mitigación: N3.2 no crea mutaciones físicas/financieras salvo evidencia rectora explícita. Cualquier efecto posterior debe quedar en su parent/microtarea correspondiente.

## Estrategia de rollback

N3.2.A es sólo documental; rollback = revertir este documento si el Plan Maestro demuestra una interpretación distinta.

Para B–H:

- cambios de dominio/API deberán ser forward-correctable;
- C deberá definir rollback de persistencia fail-closed, nunca DROP destructivo improvisado con datos existentes;
- no modificar `main`, Producción, secretos ni infraestructura.

## Criterios de aceptación de N3.2.A

- [x] dependencia N3.1.H identificada y cerrada antes de promover A;
- [x] ausencia de PedidoVenta canónico confirmada por búsqueda dirigida;
- [x] `Venta` legacy identificada como entidad distinta;
- [x] Cotización identificada como origen potencial, no como autoridad del Pedido;
- [x] alcance/fuera de alcance documentados;
- [x] riesgos y decisiones pendientes explícitos;
- [x] superficies B–H identificadas sin adelantar implementación;
- [x] estrategia de rollback proporcional documentada.

## Handoff

**Siguiente parent:** `N3.2.B — Pedidos de venta / Dominio y contratos`.

B debe materializar únicamente el agregado, detalles, lifecycle/invariantes y contratos estrictamente necesarios. No debe introducir EF/migraciones, controller ni frontend.