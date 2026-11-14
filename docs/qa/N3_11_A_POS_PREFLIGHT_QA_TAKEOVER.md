# N3.11.A — POS — Auditoría y preflight canónico

## Estado

`LISTO_REAL` por QA takeover de ChatGPT/VAEP, condicionado únicamente a este preflight; no implementa producto ni promueve N3.11.B por sí mismo.

## CURRENT_CONFIRMED_FACT

- La autoridad comercial existente continúa en `Venta`/`VentasController`, con flujo Borrador, cálculo, confirmación y anulación.
- Existen interfaces frontend separadas para crear, listar y consultar ventas.
- Las devoluciones de cliente y notas de crédito de cliente ya son capacidades separadas; no deben duplicarse dentro de POS.
- Existe resolución de productos por código/barcode mediante el servicio de escáner y un componente frontend de entrada por código.
- Los controllers de ventas/facturación usan autenticación y permisos explícitos; cualquier POS debe reutilizar ese modelo fail-closed y no introducir bypasses.

## REUSE_CANDIDATES

- Venta como autoridad transaccional/comercial.
- Resolución existente de producto por código/barcode.
- Permisos y auditoría existentes de Ventas/Facturación cuando correspondan.
- Capacidades existentes de devolución y nota de crédito, evitando una segunda implementación POS de esos dominios.

## MISSING_OR_NOT_DISTINCT_TODAY

- No existe una superficie POS/venta-rápida autoritativa distinta del flujo de Venta ya existente.
- No existe contrato POS certificado para suspensión/reanudación de ticket, pago combinado/split, reimpresión POS especializada ni estrategia de impresión ESC/POS.
- Caja/sesión de caja no debe inventarse dentro de N3.11: el roadmap mantiene Caja como capacidad posterior separada.

## DECISION_PENDING

- Si POS reutiliza íntegramente Borrador→Confirmada o introduce una UX de una sola pantalla sin alterar lifecycle de dominio.
- Política de cliente de mostrador/Consumidor Final.
- Contrato exacto de pagos múltiples/combinados y cálculo de cambio.
- Semántica exacta de suspensión/reanudación de una venta.
- Formato/canal de impresión y reimpresión POS.
- Cualquier integración con caja física/sesiones, que no se autoriza por inferencia desde este preflight.

## RIESGOS / REGLAS DE DISEÑO

- No crear un segundo agregado comercial que compita con `Venta`.
- No saltar stock/Kardex/facturación/auditoría ya gobernados por la confirmación de Venta.
- No duplicar devoluciones ni notas de crédito dentro del POS.
- Mantener transiciones, permisos y auditoría fail-closed.
- Mantener `WORK_CAN_PIPELINE__PROMOTION_CANNOT`: N3.11.B solo se promueve después de cerrar A.

## PRUEBAS / ROLLBACK A CONSIDERAR EN HIJOS POSTERIORES

- Escaneo repetido/rápido de códigos, cantidades y variantes.
- Pagos combinados, cambio y errores de red/reintento cuando exista contrato aprobado.
- Seguridad de permisos del cajero y ausencia de bypass administrativo implícito.
- Reimpresión/suspensión únicamente cuando exista semántica aprobada.
- Compatibilidad con el flujo Venta existente y rollback de cualquier migración futura.

## REVIEW_FIRST

- Jules A #1071: `REJECTED / NOT_INTEGRATED` como autoridad de POS porque analiza CréditoCliente y no el scope N3.11.A.
- Jules D #1081: `PASS / EVIDENCE_ONLY / RELEASED / NOT_INTEGRATED`; su preflight POS fue revisado físicamente y usado únicamente como evidencia.

## DoD N3.11.A

- Alcance/fuera de alcance, reuse, gaps, dependencias, riesgos, pruebas y rollback definidos sin inventar contratos.
- DECISION_PENDING aislado para N3.11.B+.
- Dependencia N3.10.H satisfecha por cierre `500f45dc16b95bfd2de35fb9250e028b8e72fd9c`.
- P0 atribuible conocido = 0.
- P1 atribuible conocido = 0.
