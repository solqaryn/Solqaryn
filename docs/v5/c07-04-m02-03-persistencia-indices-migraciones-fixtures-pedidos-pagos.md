# SOLQARYN V5 — C07-04.M02.03: contrato de persistencia, índices, migraciones y fixtures de pedidos/pagos

**Autor:** PRIMARY DEV 06, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Repo y rama:** solqaryn/Solqaryn@dev. **PROJECT_SCOPE_LOCK=STRICT**. **Fecha:** 2026-11-14 (America/Tegucigalpa). **Modalidad:** contrato documental, NO es migración, ALTER, SQL ejecutado, endpoint ni pago.

## 1. Fuentes y dependencia

Fuente única STANDARD V5 FINAL, Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA, y Sheet PLAN_MAESTRO/COLA/DAG. C07-04.M02.03 fila 1126: PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI antes del claim. M01.01–06 LISTO con A5 independiente. M02.01/.02 están diseñando entidades y API de forma independiente y sus estados no suplantan el gate de M02.03.

Fuentes del código: backend/src/Domain/Entities/Cotizacion.cs, Factura.cs; backend/src/Infrastructure/Persistence/CommercialTenantSalesDocumentGuard.cs y CommercialTenantInvoiceWriteGuard.cs; infraestructura EF Core 10 + MySql.EntityFrameworkCore 10.0.9, MySQL 8.4 como destino de integración. EmpresaId nullable existente para relaciones comerciales admite legacy NULL, pero estos NULL **no se adjudican ni abren lectura**. Cualquier modelo relacional propuesto deberá compararse con el esquema real, snapshot de migraciones y motor Oracle antes de generar DDL.

## 2. Modelo persistente lógico PD01–PD12 (NO aplicado)

| ID | Dato o asociación conceptual | Regla de escritura/lectura |
| --- | --- | --- |
| PD01 | Cotizacion (EmpresaId, ClienteId, Estado, Version) | Cliente y cabecera misma empresa, estados válidos |
| PD02 | CotizacionDetalle (CotizacionId, ProductoId, Version) | Parent y producto con pertenencia verificable |
| PD03 | PedidoVenta (EmpresaId, ClienteId, OrigenCotizacionId, Estado) | No duplicar pedido por conversión/reintento |
| PD04 | PedidoDetalle (PedidoId, ProductoId, VarianteId, Cantidad) | Dueño derivado del pedido, stock de su tenant |
| PD05 | Venta (EmpresaId, PedidoId, Estado, Totales) | Relación estable, sin implicar tabla nueva |
| PD06 | Factura (EmpresaId, VentaId, EmisorSnapshot, Numero) | Unicidad fiscal condicionada por jurisdicción/sucursal |
| PD07 | FacturaDetalle (FacturaId, ProductoId, Monto) | Owner heredado, sin acceso de otro tenant |
| PD08 | PagoAplicado (EmpresaId, FacturaId/VentaId, Monto) | Idempotencia y conciliación de saldo |
| PD09 | CajaTurno (EmpresaId, SucursalId, OperadorId, Estado) | Sin contabilizar pagos cross-tenant/cross-caja |
| PD10 | ReservaStock (EmpresaId, PedidoId, ProductoId, Cantidad) | Una reserva por transición autorizada |
| PD11 | RegistroIdempotencia (EmpresaId, Operacion, KeyHash, Fingerprint) | Key + fingerprint coherentes y TTL auditado |
| PD12 | AuditoriaOperacion (EmpresaId, Actor, Evento, CorrelationId) | Sin JWT, PAN/CVV o PII innecesaria |

PD08–PD12 son **estructuras lógicas a evaluar**, no clases/tablas existentes acreditadas. Nombres finales, FKs, cardinalidades, nullability y separación por sucursal dependen de inventario real M03 y autorización expresa.

## 3. Índices físicos hipotéticos IX01–IX10, no ejecutados

1. IX01 Cotizacion(EmpresaId, ClienteId, Estado) para lectura filtrada.
2. IX02 CotizacionDetalle(CotizacionId, ProductoId) como asociación padre-línea.
3. IX03 PedidoVenta(EmpresaId, ClienteId, Estado) para listas operativas.
4. IX04 PedidoDetalle(PedidoId, ProductoId, VarianteId) bajo integridad de propietario.
5. IX05 Factura(EmpresaId, NumeroFactura, SucursalId) sólo si negocio define unicidad fiscal.
6. IX06 FacturaDetalle(FacturaId, ProductoId) sin permitir join cross-tenant.
7. IX07 PagoAplicado(EmpresaId, FacturaId, Estado) si existe relación real.
8. IX08 ReservaStock(EmpresaId, ProductoId, PedidoId, Estado) con concurrencia verificada.
9. IX09 Idempotencia(EmpresaId, Operacion, KeyHash) único tras diseño y sizing.
10. IX10 Auditoria(EmpresaId, CorrelationId, FechaUtc) para trazas redactadas.

No duplicar índices ya presentes, ni agregar columnas inexistentes por inferencia. Requeridos antes de materializar: EXPLAIN, cardinalidad, costos y consulta contra INFORMATION_SCHEMA del **servicio MySQL efímero**, no base Aiven productiva.

## 4. Migración/rollback reversible propuesta MG01–MG08

- MG01: inventariar migraciones EF/versiones, índices únicos, restricciones y casos NULL históricos; snapshot reproducible.
- MG02: crear datos sintéticos A/B + legado NULL de sólo prueba; jamás utilizar datos personales reales.
- MG03: planificar esquema expand aditivo, con FKs compuestas tenant + ID cuando sean compatibles con proveedor Oracle.
- MG04: dry-run de aplicación en MySQL 8.4 efímero y pruebas de duplicados/constraints.
- MG05: reconciliar datos legados explícitamente con revisión humana/autorización de owner; no asignar NULL por heurística.
- MG06: demostrar lectura dual durante transición y rechazo fail-closed a cross-tenant.
- MG07: ensayar revert de nueva capacidad mediante código compatible + restauración probada, sin borrar historia contable.
- MG08: medir locks, índices, rollback e integridad antes de cualquier plan de despliegue; sin QA/main/PROD.

## 5. Fixtures positivos y negativos FX01–FX12 — FUTUROS_NO_EJECUTADOS

| Fixture | Prueba positiva propuesta | Negativa que debe fallar cerrado |
| --- | --- | --- |
| FX01 | Empresa A y Cliente A crean Cotizacion A | Cliente B en Cotizacion A |
| FX02 | Cotización A tiene detalle de producto A | Variante B en detalle A |
| FX03 | Conversión única genera Pedido A | Dos conversiones simultáneas/replay |
| FX04 | Pedido A asocia Venta A | Venta B asociada al pedido A |
| FX05 | Factura A asocia Venta A e impuestos coherentes | Factura B con Venta A |
| FX06 | Pago A reduce saldo A una vez | Replay key con diferente importe |
| FX07 | Caja A acepta operador con rol activo | Operador revocado o de B |
| FX08 | Reserva A descuenta stock una vez | Carrera provoca saldo negativo |
| FX09 | Documento anulado conserva historia | Hard-delete de factura con pagos |
| FX10 | Legado NULL permanece aislado | Backfill heurístico lo adjudica a A |
| FX11 | Exportación A contiene sólo metadatos A | B recibe datos o bytes A |
| FX12 | Rollback de fixture deja integridad estable | Rollback deja pago/reserva parcial |

## 6. Contrato de seguridad, pruebas y cierre

Las fronteras tenant JWT/RBAC no se delegan a DTO o UI. La escritura requiere transacción, permiso vigente, parent/child persistidos, scope A/B y auditoría redactada. Los escenarios FX01–FX12 deberán ejecutarse posteriormente con EF/MySQL efímero, transacciones y concurrencia real; este documento no constituye PASS de prueba de seguridad o base de datos.

**A0** cotejar fuente, ID, MD/PD, 10 índices y 8 pasos de migración, ausencia de secretos; **A1** revisión de PN/FX futuros sin ejecución; **A2** tenant A/B, roles, replay y reversión analizados; **A3** comparar fuentes reales EF/Domain y sin SQL ejecutado; CI runtime NO_APLICA a diffs exclusivamente Markdown; **A4** commit fast-forward a dev, Scope Lock y V5 encabezados/procedencia exact SHA SUCCESS, PLAN/COLA/DAG/BITACORA readback; **A5** otro TASK_ID Javier distinto de DEV06.

M02.06 requiere M02.01–05 LISTO+A5 para su DAG propio. No autoriza IAM, pagos, PSP, migraciones, QA/main/PROD. **Rollback documental:** git revert normal con evento compensatorio append-only, sin force-push/reset y sin eliminar historia.
