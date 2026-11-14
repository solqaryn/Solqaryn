# ADR ERP-N2.2 — Autoridad documental de OrdenCompra

## Estado

Aceptada para ERP-N2.2.

## Contexto

Solqaryn ya dispone de `SolicitudCompra` como necesidad aprobable y de `Compra` como flujo transaccional histórico. ERP-N2 requiere separar explícitamente solicitud, orden, recepción y factura de proveedor para evitar que un único agregado termine controlando compromiso comercial, inventario y obligación financiera a la vez.

La OrdenCompra debe soportar proveedor, moneda, condiciones, fecha esperada, líneas, descuentos, impuestos, observaciones, aprobación, cancelación e idempotencia durable. Al mismo tiempo, N2.3 establece que el stock aumenta por recepción de mercancía, no por la aprobación de la orden.

## Decisión

`OrdenCompra` es un agregado documental independiente y la autoridad del compromiso comercial aprobado con el proveedor.

Se adopta:

1. identidad y lifecycle propios de `OrdenCompra`;
2. vínculo opcional con `SolicitudCompra`, conservando ambas identidades;
3. snapshots de proveedor y producto/variante suficientes para preservar el compromiso histórico;
4. persistencia separada en `OrdenesCompra`/`OrdenCompraDetalles`;
5. `Idempotency-Key` durable + fingerprint SHA-256 para creación;
6. permisos relacionales por operación;
7. auditoría transaccional de mutaciones;
8. ausencia deliberada de efectos de stock, Kardex, costeo y finanzas en N2.2.

## Autoridades por documento

| Concepto | Autoridad |
| --- | --- |
| Necesidad interna de compra | `SolicitudCompra` — N2.1 |
| Compromiso comercial con proveedor | `OrdenCompra` — N2.2 |
| Mercancía efectivamente recibida | `RecepcionCompra` — N2.3 |
| Documento/factura del proveedor | `FacturaProveedor` — N2.4 |
| Conciliación Orden/Recepción/Factura | Three-way match — N2.5 |
| Stock físico | `ExistenciaVariante`, modificado por eventos físicos autorizados |

## Consecuencias positivas

- una aprobación comercial no altera inventario prematuramente;
- recepción parcial/múltiple puede modelarse después sin reescribir la orden;
- la factura puede diferir de lo ordenado sin destruir la historia comercial;
- el three-way match dispone de tres fuentes independientes;
- el audit trail conserva quién solicitó, quién ordenó, quién recibió y qué facturó el proveedor;
- la idempotencia de creación evita documentos duplicados por retry de red.

## Costes y trade-offs

- existen más entidades, tablas, endpoints y estados que en un modelo monolítico;
- N2.3/N2.4 deben mantener relaciones explícitas con las líneas de orden;
- los reportes deben distinguir `ordenado`, `recibido` y `facturado`;
- no puede inferirse stock desde el estado `Aprobada` de la orden.

Se acepta esta complejidad porque elimina ambigüedad de autoridad y habilita ERP empresarial auditable.

## Alternativas rechazadas

### Reutilizar `Compra` como OrdenCompra

Rechazada. `Compra` arrastra semántica transaccional histórica y mezclaría compromiso comercial con efectos económicos/físicos.

### Convertir `SolicitudCompra` aprobada en OrdenCompra

Rechazada. La solicitud contiene una necesidad/estimación; una orden puede negociar proveedor, precios, moneda, impuestos, condiciones y fechas distintas. Deben conservarse ambos documentos.

### Aumentar stock al aprobar la orden

Rechazada. El stock sólo debe aumentar cuando existe evidencia física de recepción en N2.3.

### Crear automáticamente recepción/factura al aprobar

Rechazada. Impediría recepciones parciales/múltiples, diferencias, daños, faltantes y facturas divergentes.

## Reglas de evolución

- N2.3 puede referenciar `OrdenCompra` y sus líneas, pero no debe convertir la orden en el registro de stock.
- N2.4 puede referenciar Orden/Recepción, preservando factura independiente.
- N2.5 compara fuentes; no debe corregirlas silenciosamente.
- cualquier cambio futuro que haga que `AprobarAsync` modifique `ExistenciaVariante`, Kardex o finanzas contradice este ADR y requiere una decisión arquitectónica explícita.

## Evidencia

Baseline funcional certificado: `b4d477e2de25077c459d02b479968c93c93bc910`.

Migración canónica: `20260818204700_N2_2_OrdenCompraPersistencia` (`adff03723b4336b570328179e468e8470e611b95`).

Regresión final: Development `#32218997006` y M13 `#32218996978` SUCCESS.