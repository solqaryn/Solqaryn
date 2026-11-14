# Fase 2D — Certificación de redondeo y distribución de centavos en facturación

Fecha: 2026-08-07

## Objetivo

Certificar la regla monetaria del plan maestro de Solqaryn sin duplicar implementación ya existente en `Desarrollo`:

- redondeo monetario explícito a 2 decimales con `MidpointRounding.AwayFromZero`;
- subtotal calculado por línea antes de sumar el documento;
- costo de envío incorporado exactamente una vez;
- distribución determinista de descuentos e impuestos entre líneas;
- persistencia de la distribución histórica en `FacturaDetalle`;
- conciliación exacta al centavo entre líneas y encabezado.

## Implementación verificada

### `CalculoService`

`CalculoService` centraliza la política monetaria mediante:

```csharp
private static decimal RedondearMoneda(decimal valor) =>
    Math.Round(valor, 2, MidpointRounding.AwayFromZero);
```

Cada subtotal de línea se calcula y redondea antes de formar el total del documento. Los impuestos incluidos se extraen del importe sujeto, los impuestos adicionales se agregan al total y el envío se trata como un único componente de la venta.

La política comercial histórica de Solqaryn se conserva: `ImporteBruto` representa el total comercial de la venta antes del descuento y el componente de productos se obtiene descontando el envío del importe bruto. No se modifica esta semántica para evitar romper facturas y pruebas existentes.

### `FacturaDetalleDistribuidor`

La distribución por línea trabaja exclusivamente sobre el snapshot monetario ya calculado de la factura; no vuelve a consultar configuraciones vigentes de impuestos, descuentos o envío.

Distribuye proporcionalmente:

- importe de productos;
- descuento;
- impuesto incluido;
- impuesto adicional.

Cuando el redondeo proporcional produce un residuo, este se aplica de forma determinista a la línea con mayor base monetaria; en empate se utiliza el menor índice del orden original.

Se persisten por línea:

```text
FacturaDetalle.Descuento
FacturaDetalle.Impuesto
FacturaDetalle.Subtotal
FacturaDetalle.TotalLinea
```

`FacturaRepository.AddAsync` ejecuta `FacturaDetalleDistribuidor.Aplicar(factura)` antes de agregar la factura al `DbContext`, por lo que la distribución queda almacenada como snapshot histórico.

## Invariantes verificadas

La implementación rechaza la persistencia si no se cumple cualquiera de estas condiciones:

```text
Σ DescuentoLinea = Factura.Descuento
Σ ImpuestoLinea = Factura.Impuesto
Σ TotalLinea + Factura.CostoEnvio = Factura.Total
Factura.Total >= 0
```

El envío no se distribuye en cada producto y no se multiplica por el número de líneas: se suma una sola vez a la conciliación final del documento.

## Pruebas específicas

`CalculoServiceTests` cubre, entre otros escenarios:

- impuesto incluido sin doble suma;
- impuesto adicional;
- descuento antes del impuesto adicional;
- total comercial L. 300 con envío L. 80 e ISV incluido 15%;
- descuento L. 20 con total final L. 280;
- redondeo de cada línea antes de sumar (`0.335 + 0.335 = 0.68`);
- compras con dos líneas `10.005`, resultando `20.02` bajo la política `AwayFromZero`.

`FacturaDetalleDistribuidorTests` cubre:

- envío L. 80 + impuesto incluido L. 28.70 + descuento L. 20, total L. 280;
- impuesto adicional incorporado en `TotalLinea`;
- impuesto mixto incluido/adicional;
- residuo de L. 0.01 distribuido determinísticamente: `0.34 + 0.33 + 0.33`;
- rechazo de snapshot fiscal inconsistente.

`VentaPrecisionMonetariaValidatorTests` garantiza que los precios enviados por creación/actualización de venta no excedan dos decimales.

## Evidencia automatizada de referencia

El candidato funcional que contiene esta implementación fue validado en el ciclo integral previo:

```text
Commit funcional: c5942990a36287ccb476c66f6f73c7d361d9eca3
Backend Release: 201/201 pruebas no-integración aprobadas
MySQL CI: 8.4.11 aprobado
Playwright integral: 87/87 aprobado
Build frontend: aprobado
```

Los commits documentales posteriores no modificaron la lógica monetaria aquí certificada.

## Dictamen

```text
FASE 2D: COMPLETADA
REDONDEO AWAY-FROM-ZERO: APROBADO
REDONDEO POR LÍNEA: APROBADO
DISTRIBUCIÓN DE CENTAVOS: APROBADA
PERSISTENCIA HISTÓRICA POR LÍNEA: APROBADA
ENVÍO SUMADO UNA SOLA VEZ: APROBADO
CONCILIACIÓN AL CENTAVO: APROBADA
REGRESIONES BLOQUEANTES CONOCIDAS: 0
```

## Gobernanza

- Rama de trabajo: `Desarrollo`.
- `main` permanece congelada.
- PR #2 permanece abierto y en borrador.
- No se autoriza merge ni auto-merge.
- Producción no forma parte de esta certificación y no fue modificada.