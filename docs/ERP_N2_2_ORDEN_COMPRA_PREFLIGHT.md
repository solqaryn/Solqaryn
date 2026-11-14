# ERP-N2.2 — OrdenCompra — Auditoría y preflight

## Estado

**Microtarea:** N2.2.A — Auditoría y preflight.

**Baseline inspeccionado:** `781b315854693cf5d8c36c7eeafb9acd26a7ccf9` en `Desarrollo`.

**Dependencia:** N2.1.H `LISTO`.

## 1. Alcance rector

ERP-N2.2 debe crear `OrdenCompra` con proveedor, condiciones, moneda, impuestos, descuentos, fecha esperada, detalles, observaciones y aprobación.

La OrdenCompra debe representar el compromiso comercial aprobado con el proveedor, no la recepción física ni la factura del proveedor.

## 2. Estado real del repositorio

La inspección dirigida confirma que actualmente **no existe** una entidad `OrdenCompra` en Domain.

Sí existen:

- `Compra` / `CompraDetalle` / `CompraDocumento`;
- `SolicitudCompra` / `SolicitudCompraDetalle`;
- `CompraService` y `SolicitudCompraService`;
- infraestructura y flujos ya certificados para SolicitudCompra.

`Compra` hereda `ConfirmableEntity`, contiene estado/pago/método de pago/totales y pertenece al flujo transaccional existente. No debe convertirse en `OrdenCompra`, porque eso mezclaría el compromiso de compra con la posterior materialización económica/física.

`SolicitudCompra` es un agregado independiente y aprobado en N2.1. Su aprobación no crea implícitamente una compra ni modifica stock.

## 3. Fronteras obligatorias con ERP-N2

### Dentro de N2.2

- agregado `OrdenCompra` independiente;
- vínculo trazable opcional/expreso con una `SolicitudCompra` aprobada;
- proveedor y snapshots necesarios;
- condiciones comerciales y moneda;
- impuestos y descuentos de la orden;
- fecha esperada;
- líneas/detalles ordenados;
- observaciones;
- ciclo de borrador/aprobación y metadatos de decisión;
- consulta, edición controlada, aprobación/cancelación según contrato que se concrete en N2.2.B;
- persistencia, API, RBAC, auditoría y frontend en las microtareas posteriores de N2.2.

### Fuera de N2.2

- aumentar o disminuir existencias;
- generar Kardex por la sola aprobación de una orden;
- recepciones totales/parciales/múltiples: N2.3;
- crear o contabilizar FacturaProveedor: N2.4;
- three-way match Orden/Recepción/Factura: N2.5;
- cuentas por pagar/tesorería/contabilidad que correspondan a fases posteriores;
- modificar el scope reservado de Jules.

La frontera de stock es estricta: el Plan Maestro indica que el stock aumenta por **RecepcionCompra** en N2.3, no por OrdenCompra.

## 4. Decisión arquitectónica de preflight

N2.2.B debe introducir `OrdenCompra` y `OrdenCompraDetalle` como modelos propios, derivados de la infraestructura de auditoría común y sin heredar semántica que dispare efectos de `Compra`.

El vínculo con `SolicitudCompra` debe ser trazable, pero no debe borrar la identidad de ninguno de los dos documentos. Una solicitud aprobada puede ser fuente de una orden; la orden conserva sus propias condiciones comerciales y snapshots porque pueden diferir del estimado solicitado.

El lifecycle definitivo se congelará en N2.2.B con pruebas contractuales. En este preflight se exige, como mínimo, separar:

- estado editable de borrador;
- estado sometido/aprobado para compromiso comercial;
- estado terminal o cancelado de forma explícita;
- prohibición de mutaciones de líneas/condiciones una vez aprobada salvo operación empresarial expresamente modelada.

No se reutilizará `EstadoDocumento` de forma automática sin demostrar compatibilidad semántica; se prefiere enum propio de OrdenCompra si las transiciones del proceso lo requieren.

## 5. Impacto esperado por capas

### Domain

- `Entities/OrdenCompra.cs`;
- `Entities/OrdenCompraDetalle.cs`;
- enum de estado específico si se confirma en N2.2.B;
- invariantes de cantidades, importes, fechas y lifecycle.

### Application

- contratos DTO/request/response;
- interfaces repository/service;
- servicio de aplicación con concurrencia transaccional para transiciones;
- validación de `SolicitudCompraId` si se usa como origen;
- snapshots de proveedor/producto/variante y totales deterministas.

### Infrastructure

- configuración EF, índices y constraints;
- repositorio y locks necesarios;
- migración MySQL forward-only + snapshot;
- rollback documentado y fail-closed.

### API

- controller/rutas dedicadas `/ordenes-compra`;
- endpoints de consulta y lifecycle;
- RBAC relacional específico;
- ProblemDetails/correlation-id/auditoría según infraestructura vigente.

### Frontend

Se implementará en N2.2.E, no en esta microtarea: listado, filtros, creación/edición, detalle, lifecycle, rutas y permisos.

## 6. Riesgos técnicos identificados

1. **Colisión semántica con `Compra`:** reutilizarla rompería la separación futura Orden/Recepción/Factura.
2. **Efectos prematuros de inventario:** una aprobación de orden no puede invocar stock/Kardex/costeo.
3. **Doble autoridad documental:** SolicitudCompra y OrdenCompra deben conservar identidades y estados propios.
4. **Concurrencia de aprobación:** dos decisiones simultáneas deben serializarse/fallar cerrado.
5. **Totales no reproducibles:** impuestos, descuentos, moneda y precios deben persistir suficientes snapshots/reglas para reconstruir el compromiso aprobado.
6. **Evolución N2.3/N2.4/N2.5:** las claves y detalles deben permitir relacionar recepción y factura sin reescribir la OrdenCompra.
7. **Auditoría/RBAC:** aprobar/cancelar requiere permisos explícitos y evidencia transaccional.

## 7. Estrategia de pruebas

N2.2 deberá incluir, como mínimo:

- pruebas contractuales de independencia respecto de `Compra`;
- invariantes de cantidades/precios/descuentos/impuestos/moneda/fecha esperada;
- transición válida e inválida del lifecycle sin mutación parcial;
- trazabilidad opcional desde SolicitudCompra aprobada;
- rechazo de origen inválido/no aprobado cuando corresponda;
- concurrencia/idempotencia de aprobación/cancelación;
- persistencia/migración MySQL y constraints;
- API/RBAC/auditoría/correlation-id;
- regresión que demuestre que aprobar OrdenCompra no cambia `ExistenciaVariante`, Kardex, costeo ni finanzas;
- E2E frontend en N2.2.E/G.

## 8. Rollback y migración

La persistencia de N2.2.C será aditiva y forward-only. El rollback debe eliminar únicamente objetos de OrdenCompra cuando sea seguro y no reinterpretar registros existentes de `Compra` como órdenes. No habrá DDL/DML en Producción durante esta fase.

## 9. Criterio de salida de N2.2.A

Preflight **APTO** para avanzar a N2.2.B cuando:

- N2.1.H permanezca `LISTO`;
- `Desarrollo` siga siendo la única rama de trabajo;
- no exista una implementación concurrente de OrdenCompra fuera del scope autorizado;
- N2.2.B implemente únicamente dominio/contratos y congele el lifecycle antes de persistencia/DDL de N2.2.C.

## 10. Handoff exacto

Siguiente microtarea: **N2.2.B — Dominio y contratos de OrdenCompra**.

Primera acción: crear agregado/detalle y estado contractual independiente, incluyendo vínculo trazable con SolicitudCompra y condiciones comerciales, con pruebas dirigidas que congelen la ausencia de efectos sobre inventario/Kardex/costeo/finanzas.
