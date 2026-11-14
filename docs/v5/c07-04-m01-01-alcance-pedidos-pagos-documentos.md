# SOLQARYN V5 — C07-04.M01.01 — Alcance, exclusiones y no-regresión de pedidos, pagos y documentos

**Fecha:** 14 de noviembre de 2026. **Naturaleza:** contrato de preparación documental M01 (no implementación). **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, bloque `C07-04 | Pedidos, pagos y documentos`. **Salida exigida de M01:** inventario y alcance de pedidos, cotizaciones, facturas, caja, pagos, anulaciones y exportaciones; control de idempotencia, reversión, permisos y evidencia fiscal. Los escenarios siguientes son **criterios de pruebas futuras NO_EJECUTADAS** y no certifican comportamiento productivo.

## Objetivo y frontera de responsabilidad

Preparar un contrato verificable para conectar, sin duplicar transacciones ni romper el ERP vigente, cotización aceptada, pedido, venta, factura, pago, caja y documentos fiscales. La futura implementación deberá conservar relaciones, transacciones, auditoría, identificación del tenant y snapshots históricos. Este documento describe lo que debe verificarse; **no declara una nueva integración funcional, una nueva caja ni la emisión fiscal certificada**.

**Ámbito M01:** identificar flujos y propietarios, registrar no-regresión y supuestos, inventariar contratos existentes, preparar pruebas A/B y rollback. **Fuera del ámbito:** tocar entidades/servicios/API, actualizar esquema MySQL/EF, activar procesadores de pago, autorizar cancelaciones reales, generar facturas reales, alterar comprobantes históricos, disparar notificaciones, worker/webhook, cambios de JWT/RBAC/tenant, secretos, proveedor fiscal, QA/main/PROD o despliegue.

## Evidencia verificada en el árbol Git de dev

| Fuente actual y blob SHA | Observación limitada a código |
| --- | --- |
| `backend/src/Domain/Entities/Cotizacion.cs` — `28d682b83d5b7d895aceb0ce57b56aa3ea3843e8` | Estado Borrador/Enviada/Aceptada/Convertida, snapshot de cliente y `EmpresaId` nullable (legacy en cuarentena); conversión requiere aceptación. |
| `backend/src/Domain/Entities/PedidoVenta.cs` — `12c265e9b3dbd6d27883d52e3b2a0b05783eb0cf` | Pedido separado de venta legacy; `EmpresaId` nullable, clave/fingerprint de idempotencia, estados y creación desde cotización aceptada. |
| `backend/src/Domain/Entities/Factura.cs` — `8c59b9a939cc9439efa5fd09777d9b0dfb19de66` | Snapshots de emisor/cliente, totales, impuesto, descuentos, pagos, estado y anulación; `EmpresaId` nullable histórico. |
| `backend/src/Domain/Entities/FacturaPago.cs` — `c35f5e678a2a267f8dd56fedefd24450ae36ee7b` | Importe aplicado, monto recibido, cambio, snapshot del medio de pago, datos de anulación y propiedad tenant nullable. |
| `backend/src/Domain/Entities/PagoOnline.cs` — `5063b0dc667583f7403fbeec879b519b6ca216d2` | Intento provider-agnostic con `EmpresaId` obligatorio, hash de Idempotency-Key, factura, proveedor, estado y referencia externa; sin credenciales en la entidad. |
| `backend/src/API/Controllers/FacturasController.cs` — `8a70f59763749896c3662a365ec9e8ffe47dc72a` | Rutas protegidas con `[Authorize]` y `RequierePermiso` para ver, aplicar y anular pagos; auditoría registrada en controladores observados. |

La presencia de controles en estas fuentes **no basta** para certificar todas las consultas EF, reglas de caja, aislamiento A/B, idempotencia durable, conciliación del proveedor o conformidad fiscal. Esas pruebas quedan abiertas para las unidades de implementación/pruebas pertinentes. Las entidades y controladores citados son **fuentes actuales**, no artefactos creados por esta microtarea.

## Doce invariantes de no-regresión NR01–NR12

| ID | Invariante obligatorio antes de habilitar integración |
| --- | --- |
| NR01 | Cada operación autenticada resuelve tenant y pertenencia al objeto en backend; un ID ajeno no se consulta ni muta con un JWT válido de otra empresa. |
| NR02 | `EmpresaId=NULL` heredado no se interpreta como permiso global; inventariar y aislar históricos antes de cualquier migración. |
| NR03 | La cotización no se convierte a pedido si no está aceptada; pedido no implica venta/cargo/stock/contabilidad hasta que el flujo explícito lo autorice. |
| NR04 | Repetir idéntica solicitud de confirmación o cobro no duplica pedidos, ventas, facturas, pagos ni asientos; misma clave y payload distinto se rechaza. |
| NR05 | La suma de pagos aplicados, el cambio entregado y el saldo siguen invariantes de conciliación; el cambio no aumenta el monto pagado. |
| NR06 | Las transiciones de anulaciones y reversiones tienen permisos específicos, motivo obligatorio, trazabilidad y tratamiento de efectos reversibles, sin borrado físico de historia. |
| NR07 | La numeración, fechas, moneda, impuestos, cliente, emisor y precios aplicados mantienen snapshots históricos; cambios posteriores a catálogos no reescriben comprobantes emitidos. |
| NR08 | Un pago online se vincula al tenant y factura correctos y a un evento de proveedor deduplicado; callbacks no verificados o repetidos no aplican doble efecto. |
| NR09 | Las salidas PDF/exportación aplican los mismos permisos y filtros por tenant que los endpoints; enlaces públicos sólo exponen el alcance explícito autorizado. |
| NR10 | Las operaciones concurrentes sobre un pedido o factura se serializan o fallan con conflicto controlado, sin estado parcialmente aplicado; rollback y reintento dejan auditoría coherente. |
| NR11 | La lógica de inventario y reservas conserva cantidades y no permite consumo fuera de un flujo de venta confirmado; ventas/anulaciones no alteran stock duplicadamente. |
| NR12 | Ninguna interfaz Angular, UI de caja ni estado externo sustituye JWT/RBAC backend, validación de pertenencia y autorización de operaciones sensibles. |

## Condiciones de aceptación futuras (oráculos NO_EJECUTADOS)

**Positivos P01–P06:** (P01) cotización propia aceptada se transforma una vez; (P02) pedido confirmado propio queda trazado sin efectos no autorizados; (P03) factura propia conserva snapshots; (P04) un pago legítimo aplica importe/saldo sin duplicado; (P05) anulación autorizada deja motivo y compensación; (P06) exportación propia revela sólo documentos autorizados.

**Negativos N01–N14:** (N01) tenant A pide factura B; (N02) cotización `EmpresaId=NULL` se expone; (N03) cotización no aceptada se convierte; (N04) replay misma clave/payload diferente; (N05) dos callbacks iguales aplican doble cobro; (N06) un evento de proveedor sin validación acredita pago; (N07) cambiar factura de B con usuario A; (N08) pago sin permiso `Aplicar`; (N09) anulación sin permiso `Anular`; (N10) anulación sin motivo; (N11) PDF ajeno accesible por ID/link manipulado; (N12) cambio se agrega al `TotalPagado`; (N13) ejecución concurrente produce stock o saldo doble; (N14) catálogo actualizado reescribe snapshot histórico. **Todos NO_EJECUTADOS**: se requieren xUnit, pruebas HTTP positivas y negativas, MySQL efímero y Playwright donde aplique, en microtareas posteriores autorizadas.

## Dependencias, controles, observabilidad y rollback

- **DAG técnico:** `C07-04.M01.01` con `F=VALIDADO/G=SI` en preflight; `PLAN_MAESTRO!H/I` y `COLA!G/O` se leen por ID antes de reclamar. La preparación M01 no sustituye gates terminales M01.06, aprobación A5 ni autorización de ejecución sobre infraestructura.
- **Impacto:** exclusivamente este archivo y sección autocontenida en `CHANGELOG_AI.md`; no cambio Domain, Application, Infrastructure, API, frontend, contratos ejecutables, datos, esquema ni arquitectura canónica.
- **A0:** revisar alcance/archivos/blob y ausencia de secretos; **A1:** coherencia documental de NR01–NR12, P01–P06 y N01–N14; **A2:** fronteras JWT/RBAC/tenancy A/B y fail-closed especificadas, NO prueba runtime; **A3:** xUnit, EF/MySQL, HTTP y Playwright **NO_APLICA** a diff Markdown, no se atribuye PASS; **A4:** publicar fast-forward `dev`, diff revisado, Scope Lock y provenance V5 `completed/success` en SHA exacto, readback PLAN/COLA/BITACORA; **A5:** otra Task Javier distinta del autor.
- **Observabilidad futura:** correlación por pedido/factura/tenant, eventos deduplicados, identidad del actor, transiciones, referencia del proveedor y motivo de compensación sin PII/secreto en logs.
- **Rollback documental:** commit `git revert` normal bajo gobernanza, nuevo evento compensatorio en BITACORA, sin reescribir hechos históricos. Los cambios runtime futuros exigirán rollback transaccional diseñado y validado de forma separada.

**Límite de certificación:** aprobar esta preparación no autoriza JWT/RBAC nuevo, pagos reales, migraciones, conectores fiscales, webhooks, workers, cambios de infraestructura ni promociones QA/main/PROD.
