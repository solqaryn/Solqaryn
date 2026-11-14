# Fase 7 complementaria — Validación integral y cierre

Fecha de certificación: 29 de julio de 2026.

Rama certificada: `Desarrollo`.

Pull Request oficial: `#2 — Desarrollo -> main`.

Esta fase pertenece al **ciclo funcional complementario 2026**. No sustituye ni modifica la Fase 7 histórica de envío de correo registrada en el plan original.

## Objetivo

Ejecutar una validación transversal de Solqaryn sobre una base MySQL 8.4 temporal y descartable, verificando conjuntamente facturación, impuestos, descuentos, envío, variantes, inventario, compras, ventas, pagos, PDF, correo, cargas masivas, permisos, auditoría, responsive, sesión, migraciones y concurrencia.

## Correcciones funcionales cerradas

### Desglose fiscal

Se corrigió el cálculo para conservar la composición fiscal histórica de un impuesto incluido en el precio cuando existe un descuento.

Caso certificado:

```text
Importe bruto:       L. 300.00
Subtotal/base:       L. 191.30
ISV incluido:        L.  28.70
Costo de envío:      L.  80.00
Descuento:          -L.  20.00
Total:               L. 280.00
```

El descuento se presenta y aplica como componente separado del subtotal y del impuesto incluido.

### Numeración concurrente

Se eliminó la generación basada en `COUNT + 1` para ventas y facturas.

La numeración definitiva deriva del identificador autoincremental asignado por MySQL dentro de una transacción. Se utiliza un número temporal único únicamente durante la inserción inicial.

Formato preservado:

```text
VEN-000001
FAC-000001
```

La aceptación confirmó cuatro ventas concurrentes con números de factura únicos y persistentes.

### Auditoría de ventas

La creación de la venta registra un snapshot seguro con número, importes, descuento, impuesto, costo de envío, exoneración, motivo y total.

No se registran secretos, tokens ni credenciales.

## Matriz funcional certificada

La suite permanente `frontend/e2e/fase7-validacion-integral.spec.ts` verificó:

1. Producto con tres variantes: 4 blancas, 3 negras y 3 rojas; stock total 10.
2. Costo de envío predeterminado de L. 80.00.
3. Edición del costo de envío desde Angular.
4. Un solo costo predeterminado activo.
5. Desactivación de un costo alternativo.
6. Factura combinada con subtotal, ISV, descuento, envío y total exactos.
7. Pago parcial de L. 100.00 y saldo de L. 180.00.
8. Pago final y saldo cero.
9. PDF A4 válido.
10. Factura con varios productos y un único costo de envío.
11. Anulación de venta y reversión de inventario.
12. Venta de dos unidades blancas: 4/3/3 -> 2/3/3; total 8.
13. Bloqueo de sobreventa por variante.
14. Compra de dos unidades rojas y consolidación correcta.
15. Restitución de la variante blanca al anular.
16. Exoneración de envío únicamente con motivo.
17. Auditoría de la exoneración.
18. Confirmaciones concurrentes con numeración única.
19. Carga masiva inválida con referencias inexistentes y cantidad negativa.
20. Códigos estructurados `PRODUCTO_NO_EXISTE`, `ENTERO_INVALIDO` y `STOCK_CON_HISTORIAL`.
21. Informe CSV de errores con valores originales.
22. Bloqueo de confirmación cuando la carga contiene errores.
23. Registro de la carga en Auditoría.

La suite integral también ejecutó las pruebas permanentes de catálogos, filtros, interfaz, responsive, variantes, imágenes, cargas masivas, impresión, reportes administrativos, correo SMTP aislado, sesión, aislamiento de usuarios, permisos, navegación y contraste.

## Certificación

Commit funcional certificado:

```text
183696e3b25904172ca2857e193a9d6fc04961b6
```

Ejecuciones:

```text
Desarrollo - Compilación y pruebas
Run: 30464538356
Resultado: success

Desarrollo - aceptación funcional integral
Run: 30464538385
Resultado: success

Fase 2 - Auditoría de configuración y dependencias
Run: 30464538838
Resultado: success
```

Resultado Playwright:

```text
75 pruebas totales
75 aprobadas
0 fallos
```

Artefacto:

```text
Nombre: desarrollo-aceptacion-integral
ID: 8729297367
SHA-256: 67b159329b0f56cf84fbe8e469da59f8ac737e10214c2c06559e79747776e507
```

## Validaciones técnicas aprobadas

- backend Release y pruebas unitarias;
- frontend, lint y compilación productiva;
- higiene del repositorio;
- Docker y aislamiento de entornos;
- migraciones EF Core sobre MySQL 8.4 descartable;
- conversión de producto legado a variante;
- snapshot EF sin cambios pendientes;
- SQL forward no destructivo;
- API y Angular locales en GitHub Actions;
- SMTP efímero con reintento y PDF adjunto;
- navegación sin errores de JavaScript;
- responsive y contraste en temas claro y oscuro;
- sesión por inactividad y renovación de token;
- permisos, aislamiento y respuestas 403/404.

## Base de datos

No se generó una migración nueva en esta fase.

La certificación ejecutó todas las migraciones existentes en una base MySQL 8.4 temporal y descartable. No se aplicó ninguna migración contra Producción.

## Restricciones preservadas

- `main` no fue modificada.
- No se crearon ramas.
- El PR #2 permanece abierto y en borrador.
- No se realizó merge ni auto-merge.
- No se desplegó ni modificó Producción.
- No se modificaron variables, credenciales, dominios, bases, servicios ni activos productivos.

## Estado

```text
FASE 7 COMPLEMENTARIA — COMPLETADA Y CERTIFICADA
```
