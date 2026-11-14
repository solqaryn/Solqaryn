# SOLQARYN V5 — C07-04.M01.05: criterios de aceptación de pedidos, pagos y documentos

**Microtarea:** C07-04.M01.05, PLAN_MAESTRO fila 1122. **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-14. **Rama:** `dev`; **repo:** `solqaryn/Solqaryn`; **PROJECT_SCOPE_LOCK=STRICT**. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`).

## Objetivo limitado y estado de evidencia

Definir aceptación **observable futura**, positiva y negativa, para cotizaciones, pedidos, ventas, facturas, caja, cobros, anulaciones, documentos/exportaciones y conciliación. Se conserva el ERP vigente, sin modificar código, entidades, datos, proveedores, pasarelas, fiscalidad, inventario o autorizaciones. **Esta preparación NO EJECUTA las pruebas que especifica**. La mera existencia de `Idempotency-Key`, atributos de autorización o `EmpresaId` en clases no certifica efectos durables, aislamiento de extremo a extremo ni fiscalidad. Toda decisión D01–D08 de M01.04 permanece **PENDIENTE**.

## Contratos anteriores y fuentes trazables

| Unidad | Documento | SHA blob | Evidencia acotada |
| --- | --- | --- | --- |
| M01.01 | `docs/v5/c07-04-m01-01-alcance-pedidos-pagos-documentos.md` | `2efabd1f6b2444e605ca49cf1cce34ec4bce23d8` | NR01–NR12 y exclusiones |
| M01.02 | `docs/v5/c07-04-m01-02-actores-interfaces-pedidos-pagos-documentos.md` | `9c5c0aba4694ffbbb6a4bfacfa683330de814f1d` | A01–A12/I01–I12/B01–B08 |
| M01.03 | `docs/v5/c07-04-m01-03-inventario-contratos-codigo-pedidos-pagos.md` | `654815da8a0f9b4d60c45a033eed0430bf89d317` | 12 fuentes y G01–G10 |
| M01.04 | `docs/v5/c07-04-m01-04-riesgos-supuestos-decisiones-pedidos-pagos.md` | `74c4a64fe021a5f88fe712bf6cfcfa0d8f928d92` | R01–R12, S01–S08, D01–D08 PENDIENTES |

Los documentos identifican código ya existente, incluidos `PedidoVenta.cs` (`12c265e9b3dbd6d27883d52e3b2a0b05783eb0cf`), `FacturaPago.cs` (`c35f5e678a2a267f8dd56fedefd24450ae36ee7b`), `PagosOnlineController.cs` (`99962b2957f672a6dd435895daffaba7333f8a6b`) y `FacturacionFiscalController.cs` (`6dd92895e2299feda3737a1a6eb28a3d960bcf7e`). Los blobs proceden del inventario M01.03 y M01.04: son **evidencia selectiva de código**, no prueba transaccional ni auditoría exhaustiva de seguridad.

## Matriz P01–P14 / N01–N14 — FUTUROS NO_EJECUTADOS

| Escenario | Aceptación positiva (futura) | Denegación negativa (futura) | Oráculo / fixture |
| --- | --- | --- | --- |
| 01: Cotización aceptada | **P01** Una cotización de A aceptada se convierte una vez en pedido A | **N01** Cotización no aceptada o de B se rechaza sin mutaciones | estado, EmpresaId, cliente, auditoría |
| 02: Pedido idempotente | **P02** Crear pedido con clave/fingerprint válido retorna ID estable | **N02** Repetir clave con payload distinto falla sin nuevo pedido | Idempotency-Key, fingerprint, unicidad MySQL |
| 03: Confirmación y stock | **P03** Confirmación permitida reserva/consume exactamente lo autorizado | **N03** Doble confirmación o concurrencia no descuenta stock dos veces | transacción y conflicto controlado |
| 04: Facturas propias | **P04** Factura de A retiene snapshot de emisor/cliente/precios/impuestos | **N04** Factura de B o legacy NULL no es global ni accesible desde A | tenant A/B y filas legacy NULL |
| 05: Pago parcial | **P05** Pago aplicado refleja saldo y monto recibido/cambio correcto | **N05** Importe negativo o cambio contado como pagado falla | moneda, redondeo, invariantes saldo |
| 06: Permisos para cobro | **P06** Usuario activo con permiso Aplicar registra pago en su empresa | **N06** Usuario sin permiso o de empresa B no aplica pago | JWT/RBAC + objeto y tenant backend |
| 07: Anulación | **P07** Anulación autorizada registra motivo y compensación reversible | **N07** Anulación sin permiso/motivo no borra historia ni altera saldo | auditoría + UnitOfWork + reversión |
| 08: Idempotencia online | **P08** Intento de pago usa clave única por tenant y factura | **N08** Callback repetido no crea dos cobros o conciliaciones | providerEventId, fingerprint, consulta y DB |
| 09: Callback externo | **P09** Firma válida de proveedor autorizado permite reconciliar intento | **N09** Callback anónimo, firma inválida o tenant ajeno no paga | sandbox autorizado, sin PSP live |
| 10: Fiscalidad | **P10** RTN, sucursal y emisor de empresa autorizada son coherentes | **N10** RTN inválido o sucursal de B deniega emisión | firma y política fiscal, sandbox si autorizado |
| 11: Exportación | **P11** PDF/exportación propia contiene campos permitidos según rol | **N11** URL/ID de factura ajena o PII no autorizada se niega | A/B, TTL, redacción y revocación |
| 12: Consistencia de venta | **P12** Pedido, venta y factura pasan por estados trazados sin parcialidad | **N12** Fallo intermedio no deja factura/stock/pago corruptos | MySQL efímera, rollback, logs |
| 13: Snapshot histórico | **P13** Cambio catálogo no altera documento emitido o anulaciones pasadas | **N13** Recalcular impuestos/precios antiguos viola inmutabilidad | hash snapshot pre/post y auditoría |
| 14: Reintento/observabilidad | **P14** Correlación por tenant/actor/operación respeta idempotencia | **N14** Logs filtran tokens, PII, PAN/CVV o generan doble efecto | redacción, métricas, trazabilidad |

**Ningún P/N representa PASS ejecutado.** Los resultados futuros deberán registrar método, entrada, esperado, real, run, SHA, estado, evidencia A/B y motivo de fallo cuando corresponda.

## Condiciones para pruebas causales posteriores

- **Aislamiento:** fixtures tenant A/B, usuarios con/sin permiso, sucursal ajena, objetos válidos/ajenos y `EmpresaId=NULL` legado. Ningún NULL concede privilegio global.
- **Idempotencia:** misma clave+mismo fingerprint, misma clave+payload diferente, reintento concurrente, doble confirmación, eventos de proveedor repetidos, transacciones abortadas.
- **Moneda y fiscal:** impuesto, total, pago aplicado, cambio, saldo, RTN, sucursal y snapshots históricos. Verificar reconciliación bajo carreras.
- **Integración:** pruebas .NET 10 xUnit/HTTP de permisos, EF Core/MySQL 8.4 efímero con servicio, rollback/aditividad; Playwright de UI solo cuando el alcance toque cliente. No usar Aiven real ni cobros live.
- **Seguridad y auditoría:** JWT/RBAC/tenant backend, firmas PSP solo si proveedor autorizado, PII/PAN/CVV/secrets fuera de logs y DTO, PDF/exportación por propiedad y TTL.
- **Gates:** ninguna prueba `queued`, `in_progress`, `cancelled`, `skipped` o `failure` es PASS requerido. Cada implementación causará su propia CI exact-SHA.

## Alcance, validación documental, A5 y reversión

- En este trabajo el DAG `DAG_TECNICO!F1083/G1083=VALIDADO/SI` y COLA G/O=SI autorizan M01.05 **documental**, no la liberación M01.06, JWT/RBAC, migraciones DB, proveedores, pagos, QA/main/PROD.
- **A0:** fuente V5 y blobs de los cuatro contratos; **A1:** catorce pares de aceptación medibles; **A2:** denegaciones de tenant, permisos, fraude, replay, PII, fiscal y rollback especificadas pero no ejecutadas.
- **A3:** suites de .NET/Angular/MySQL/Playwright y proveedores `NO_APLICA` al diff Markdown exclusivamente, nunca PASS inventado.
- **A4:** commit fast-forward y CAS en `dev`; checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` deben estar `completed/success` en SHA publicado; readback PLAN/COLA/BITACORA.
- **A5:** revisión independiente por otra Task Javier distinta del autor, A5 pendiente tras LISTO_DEV. No promueve QA/main/PROD.
- **Cambio:** solo este documento y sección autocontenida de `CHANGELOG_AI.md`; sin arquitectura transversal, endpoints, SQL, DTO runtime, events, webhooks, JWT/RBAC, secretos o entorno.
- **Rollback:** `git revert` regular y nuevo evento de compensación en BITACORA; nunca editar historia ni force-push.
