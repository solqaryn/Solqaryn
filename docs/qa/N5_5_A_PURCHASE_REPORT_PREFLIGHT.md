# N5.5.A — Reportes de compras · auditoría y preflight

Autoridad operativa: `docs/VAEP_AUTHORITY.md`.

## Objetivo

Preparar N5.5 sin adelantar implementación de B-H. El alcance del Plan Maestro es analizar **proveedor, precio, variación, recepción, devoluciones y cumplimiento** usando únicamente autoridades ya persistidas y contratos existentes.

## Autoridades confirmadas

### Orden y precio comprometido

`OrdenCompra` es el compromiso comercial con proveedor. Conserva `ProveedorId`, moneda, fecha esperada, líneas, cantidades, precios, descuentos e impuestos. Aprobarla no representa recepción física ni factura. Sus detalles son la referencia documental de lo ordenado y del precio comprometido.

### Recepción física

`RecepcionCompra` es la autoridad del evento físico contra una `OrdenCompra` aprobada. Sus detalles distinguen cantidades recibidas/aceptadas, dañadas, faltantes y sobrantes y preservan la relación con la línea de orden. El stock físico sólo cambia por recepción real; no por aprobar la orden ni registrar una factura.

### Factura de proveedor

`FacturaProveedor` es un documento independiente ligado a `ProveedorId` y `OrdenCompraId`; sus detalles referencian `OrdenCompraDetalleId`. El documento final certificado no asume una relación directa factura↔recepción. La conciliación estricta Orden→Recepción→Factura pertenece al contrato de three-way match ya separado y no debe reconstruirse mediante heurísticas.

### Devoluciones

`DevolucionProveedor` persiste linaje explícito hacia proveedor, orden, recepción, factura y, por línea, hacia `RecepcionCompraDetalleId` y `OrdenCompraDetalleId`. Cantidades e importes son snapshots documentales; no se debe reconstruir el origen por coincidencias de texto, fecha o importe.

### Cumplimiento observable

`EvaluacionProveedor` conserva hechos observables por recepción: proveedor, orden, recepción, fecha esperada, fecha real y cantidades ordenada, aceptada, dañada y sobrante. El contrato canónico prohíbe inferir scoring, ranking, pesos, umbrales, SLA comerciales o fórmulas de calificación no implementadas.

## Capacidades source-backed para N5.5

N5.5 puede construir reportes factuales a partir de estas autoridades:

- proveedor: identidad persistida desde la orden/documentos relacionados;
- precio comprometido: snapshot de línea de `OrdenCompraDetalle`;
- precio facturado: snapshot de línea de `FacturaProveedor` cuando el contrato de línea asociado lo permita;
- recepción: cantidades y fechas materializadas por `RecepcionCompra`;
- devoluciones: cantidades/importes y linaje persistido de `DevolucionProveedor`;
- cumplimiento factual: diferencia temporal entre fecha esperada y fecha real y cantidades observables de `EvaluacionProveedor`.

## Contratos aún no autorizados a inventar

N5.5.A no decide fórmulas de negocio que no estén ya expresadas. Deben pasar a N5.5.B como decisiones explícitas o permanecer no expuestas:

1. porcentaje de variación de precio y su denominador;
2. asignación de descuentos/impuestos de cabecera a nivel de línea si no existe snapshot persistido equivalente;
3. definición agregada de “cumplimiento” más allá de fechas/cantidades observables;
4. score/ranking/pesos/semáforos/SLA de proveedor;
5. mezcla de `Compra` legacy con las autoridades ERP modernas;
6. inferencia de una relación factura↔recepción que no esté persistida/autorizada.

Una variación absoluta sólo puede exponerse si B fija claramente los dos snapshots comparados y su signo. No se presenta como KPI contractual hasta esa decisión.

## Dependencias y orden

- N5.5.B debe fijar DTOs, agrupaciones, filtros, invariantes y nombres de medidas usando las autoridades anteriores.
- N5.5.C sólo introduce persistencia/migración si B demuestra un gap real. Si las consultas pueden derivarse de datos existentes, C debe cerrarse N/A con evidencia, no crear tablas redundantes.
- N5.5.D implementa servicios/API únicamente después de B/C.
- N5.5.E/F/G/H permanecen dependency-gated.

## Riesgos P0/P1 a prevenir

- doble autoridad al reutilizar `Compra` legacy como fuente del nuevo reporte;
- mezclar compromiso, recepción física y factura como si fueran el mismo evento;
- inflar cumplimiento con scoring inventado;
- comparar precios sin moneda/snapshot/semántica compatibles;
- contar devoluciones sin respetar su linaje documental;
- ampliar alcance de datos o permisos mediante filtros de reportes;
- crear persistencia o CI redundante sólo para ocupar lanes.

## Criterios de aceptación para N5.5.B

B debe producir un contrato pequeño y verificable que indique, para cada métrica/campo:

- autoridad persistida exacta;
- unidad/moneda y semántica;
- filtros/agrupaciones permitidos;
- tratamiento de ausencia de dato;
- reglas que siguen explícitamente fuera de alcance;
- pruebas de contrato necesarias.

Ninguna fórmula no respaldada por código/datos/decisión funcional puede pasar como hecho confirmado.

## Estrategia de pruebas

Una vez exista implementación material:

- unit/contract para filtros, agrupación y fórmulas autorizadas;
- casos de orden sin recepción, recepción parcial, devolución y proveedor sin evaluación;
- seguridad `Compras/Ver` y data-scope fail-closed;
- integración MySQL sólo cuando la consulta/persistencia real lo requiera;
- frontend/E2E sólo cuando exista una superficie UI nueva;
- regresión que demuestre que el reporte no muta stock, Kardex, factura, devolución ni evaluación.

## Rollback

N5.5.A no modifica código funcional ni datos. Un cambio posterior se revierte únicamente con changeset forward en `Desarrollo` y gates proporcionales. No usar `main`, Producción, secretos, force-push ni merge de PR #2.

## Decisión de preflight

`N5.5.A` queda **materialmente preflight-complete**: existen autoridades suficientes para avanzar a contratos de dominio en B, pero las fórmulas/semánticas no respaldadas quedan explícitamente prohibidas hasta resolución. No se requiere una implementación funcional en A y no se fabrican tareas adicionales para ocupar J1–J6.
