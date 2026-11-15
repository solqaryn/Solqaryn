# SOLQARYN V5 — C07-04.M02.01: modelo conceptual, ownership e invariantes de pedidos y pagos

**Unidad:** C07-04.M02.01 · **Autor:** PRIMARY DEV 06 / JAVIER_CHATGPT, TASK_ID 6af16bbad26881918c283abe06e1f32a · **Rama:** solqaryn/Solqaryn@dev · **PROJECT_SCOPE_LOCK=STRICT** · **Fecha:** 14 noviembre 2026 (America/Tegucigalpa).

## 1. Autoridad y alcance

Fuente única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; PLAN_MAESTRO C07-04.M02.01 (fila 1124), COLA G/O y DAG_TECNICO F/G. Dependencia exigida: C07-04.M01.01–06 LISTO con A5 independiente, sin inferir permiso sobre M03, pagos reales, migraciones ni QA/main/PROD. M01.06 obtuvo A5 de otra Task Javier sobre SHA 482d5ba0034aa94460b386569b0af4a12180da8b. M02.01 PENDIENTE sin owner, COLA G/O=SI y DAG F=VALIDADO/G=SI antes de claim.

**Resultado:** modelo lógico de entidades/ownership para implementación futura, no cambio de tablas MySQL, entidades EF, API, cálculos reales, impuestos, credenciales PSP ni comportamiento de producción. Las pruebas son oráculos futuros, NO EJECUTADAS.

## 2. Código vigente inspeccionado

- backend/src/Domain/Entities/Cotizacion.cs: EmpresaId nullable, ClienteId, detalles, snapshots y transiciones de estado. EmpresaId NULL histórico no autoriza lectura global.
- backend/src/Domain/Entities/Factura.cs: EmpresaId nullable, VentaId, número, fecha de emisión, datos de emisor y cliente snapshot.
- backend/src/Infrastructure/Persistence/CommercialTenantSalesDocumentGuard.cs: validación de cotizaciones, PedidoVenta y líneas contra ownership persistido, no contra encabezados del cliente.
- backend/src/Infrastructure/Persistence/CommercialTenantInvoiceWriteGuard.cs: factura y detalle vinculados al propietario persistido; NULL histórico en cuarentena.

Domain ← Application ← Infrastructure y API como borde, con EF Core 10, proveedor Oracle MySql.EntityFrameworkCore y JWT/RBAC. El guard de persistencia no sustituye autorización API/Application ni prueba de integridad transaccional MySQL. Una relación entre cotización, pedido, venta, factura, pago, caja o stock sólo se acepta con identidad tenant verificada por servidor.

## 3. Modelo de entidades lógicas, sin nuevas tablas

| Clave | Objeto | Relación/ownership esperado |
| --- | --- | --- |
| MD01 | Cotización | Empresa → Cliente → Cotizacion → detalles; borrador editable, aceptación auditada |
| MD02 | PedidoVenta | Empresa → Cliente → PedidoVenta → líneas; no relacionar productos de otro tenant |
| MD03 | Venta | Empresa → Venta y detalles; estados permitidos por dominio |
| MD04 | Factura | Empresa → Venta → Factura; emisor/RTN/correlativo y snapshot fiscal coherentes |
| MD05 | FacturaDetalle | Factura → detalle/producto; owner derivado del padre persistido |
| MD06 | Pago aplicado | Empresa → factura/venta → pago; saldo e idempotencia coherentes |
| MD07 | Caja/turno | Empresa → sucursal/caja/operador; aislamiento por empresa y ubicación |
| MD08 | Reserva/stock | Empresa → producto/variante → reserva; sin doble descuento |
| MD09 | Cancelación | Documento → motivo/aprobador/compensación; historia fiscal sin hard-delete |
| MD10 | Registro auditoría | Actor verificado → before/after + correlación sin secretos |
| MD11 | Exportación | Documento padre → control de propiedad y TTL/revocación |
| MD12 | Identidad idempotente | Empresa + operación + key + fingerprint; replay conflictivo falla |

MD06–MD12 son **objetos conceptuales** y no afirman que existan tablas o clases con esos nombres. No se certifica cumplimiento bancario o fiscal.

## 4. Invariantes OV01–OV14

1. **OV01:** identidad comercial derivada de JWT/membresía verificadas por backend, nunca tenant_id, EmpresaId, slug, URL, cookie o header aportado por cliente.
2. **OV02:** padre/hijo deben compartir propietario persistido, incluso con EF navigation fixup y escrituras parciales.
3. **OV03:** EmpresaId NULL heredado queda en cuarentena, sin concesión heurística de tenant.
4. **OV04:** cotización aceptada/conversión no crea dos pedidos por reintento.
5. **OV05:** transiciones de pedido/venta/factura siguen máquina válida, con reversión explícita auditada.
6. **OV06:** monto, descuento, impuesto, moneda, total y saldo usan decimales y reglas de redondeo definidas; inconsistencias se rechazan.
7. **OV07:** factura emitida conserva snapshot fiscal del emisor y cliente; cambios posteriores no alteran historial.
8. **OV08:** sólo actor con permiso específico y membresía vigente aplica pago/cancelación; rol revocado deniega.
9. **OV09:** key idempotente queda ligada al fingerprint, tenant y operación; mismo key + payload diferente da conflicto sin efecto.
10. **OV10:** reserva, cobro y factura no confirman parcialmente si falla operación del agregado; demostrar con MySQL efímero en fase real.
11. **OV11:** cancelación guarda compensación y motivo sin duplicar devoluciones ni borrar documentos referenciados.
12. **OV12:** exportación y adjuntos verifican propiedad por solicitud y caducidad/revocación de URL.
13. **OV13:** logs redactan PII, JWT, PAN/CVV y secretos; no reflejar tenant B en errores de A.
14. **OV14:** CI fail, Scope Lock, A5 requerida o dependencia pendiente no libera siguiente fase ni cambia G/O por imposición.

## 5. Pares de aceptación futura PN01–PN10 — NO EJECUTADOS

| ID | Positivo futuro | Negativo fail-closed |
| --- | --- | --- |
| PN01 | Cotización y cliente de A generan un pedido A una vez | Cliente de B en pedido A se rechaza sin escritura |
| PN02 | Factura A enlaza Venta A e ítems A | Factura B con Venta A no revela ni modifica datos B |
| PN03 | Pago único actualiza saldo/caja coherentes | Mismo key con importe distinto no provoca segundo cargo |
| PN04 | Reserva descuenta stock una sola vez | Carreras simultáneas no duplican descuento |
| PN05 | Cancelación autorizada registra motivo y compensación | Rol revocado o sin permiso no puede cancelar |
| PN06 | Exportación sólo incluye documentos propios | Tenant B o URL vencida recibe denegación sin bytes |
| PN07 | RTN/emisor/sucursal verificados alimentan snapshot | Datos fiscales de otro tenant bloquean emisión |
| PN08 | Documento legado reconciliado con migración auditada | EmpresaId NULL no se atribuye desde heurística cliente |
| PN09 | Reconciliación decimal no pierde centavos | Redondeo divergente impide cargo parcial |
| PN10 | PSP firmado/autorizado reconcilia recepción | Callback inválido/repetido no cobra ni cambia estado |

Pruebas reales futuras: xUnit, HTTP positivo/negativo, MySQL efímero 8.4 y carreras con fixtures tenant A/B. Nombrarlas no significa PASS y no se habilitan proveedores.

## 6. Dependencias, validación y rollback

M02.02–M02.05 diseñan API/DTO, persistencia, configuración, eventos conceptuales por su propio DAG independiente; M02.06 exige M02.01–05 LISTO+A5. M03 exige código auténtico y CI dirigida. Las decisiones comerciales/fiscales D01–D08 de M01 permanecen abiertas.

**A0:** cotejar predecesores, fuente, MD01–MD12 y OV01–OV14; **A1:** comprobar coherencia de PN01–PN10 documental, sin ejecución; **A2:** preservar límites tenant A/B, JWT/RBAC, idempotencia, fiscalidad y secretos; **A3:** inspeccionar fuentes Git y contratos, runtime .NET/Angular/MySQL NO_APLICA a diff sólo Markdown; **A4:** publicación dev fast-forward/expected HEAD, diff de documento+CHANGELOG, Scope Lock y V5 completed/success exact-SHA, PLAN/COLA/BITACORA readback; **A5:** Task Javier distinta de autora.

**Rollback:** git revert normal del commit documental con evento de compensación append-only en BITACORA y check exact-SHA. Sin force-push, QA/main/PROD, PSP, cobros, SQL, credenciales, webhook o dispatcher.
