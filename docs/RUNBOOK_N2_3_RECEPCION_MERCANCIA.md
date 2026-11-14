# Runbook N2.3 — Recepción de mercancía

## Propósito
Guía operativa para validar, diagnosticar y recuperar el flujo RecepcionCompra sin alterar datos productivos ni romper la trazabilidad de inventario.

## Invariantes que no deben romperse
1. Solo una OrdenCompra `Aprobada` puede recibir mercancía.
2. Un borrador no afecta stock ni Kardex.
3. Solo `CantidadAceptada` incrementa `StockFisico`.
4. La cantidad aceptada acumulada de todas las recepciones vigentes no puede superar `CantidadOrdenada` de la línea.
5. Confirmar y anular son operaciones transaccionales.
6. Una anulación debe revertir exactamente la materialización de la recepción y su Kardex tipado.
7. Si existen movimientos de inventario posteriores relacionados, la recepción no puede anularse automáticamente.
8. La creación usa `Idempotency-Key` + fingerprint y no debe duplicar una recepción ante reintentos.
9. Las mutaciones requieren permisos relacionales y usuario autenticado válido.
10. RecepcionCompra no debe generar factura/pago/asiento contable.

## Validación previa a despliegue
Ejecutar los gates normales del repositorio en un entorno de desarrollo/CI. Para el baseline funcional certificado de N2.3 se exigieron como mínimo:
- compilación backend Release y pruebas;
- arranque MySQL, migraciones y comprobaciones de persistencia;
- instalación/lint/build frontend;
- unit tests frontend de RecepcionCompra;
- Playwright E2E del flujo RecepcionCompra;
- controles de seguridad HTTP, higiene y dependencias;
- M13 completo.

No declarar una migración o un E2E como aprobado si el job fue cancelado antes de ejecutar el paso correspondiente.

## Smoke funcional seguro
En un entorno no productivo:
1. disponer de una OrdenCompra Aprobada con saldo pendiente;
2. consultar `GET /recepciones-compra/ordenes/{id}/saldo`;
3. crear un borrador con `POST /recepciones-compra` y una `Idempotency-Key` nueva;
4. repetir exactamente la misma creación con la misma clave y confirmar que no aparece un duplicado;
5. verificar que el borrador todavía no modificó stock;
6. confirmar mediante `POST /recepciones-compra/{id}/confirmar`;
7. verificar estado Recibida, existencia física y movimiento de Kardex correlacionado;
8. crear, si aplica, una segunda recepción parcial y verificar que el acumulado nunca supera la orden;
9. probar denegación RBAC con un usuario sin grant correspondiente;
10. para probar anulación, usar datos aislados sin movimientos posteriores y validar la reversión exacta.

## Diagnóstico de creación duplicada
Revisar en este orden:
- encabezado `Idempotency-Key` enviado por cliente;
- normalización de la clave;
- existencia previa por la misma clave;
- fingerprint del payload;
- violación de `UX_RecepcionesCompra_IdempotencyKey`.

La recuperación correcta de una carrera es leer la recepción ya persistida y comprobar fingerprint. Nunca eliminar la restricción única para “resolver” el conflicto.

## Diagnóstico de sobre-recepción
Si Confirmar responde con regla de negocio por exceso:
1. consultar el saldo actual de la OrdenCompra;
2. identificar la línea y su `CantidadOrdenada`;
3. sumar recepciones previamente materializadas vigentes;
4. revisar la `CantidadAceptada` de la recepción actual;
5. corregir el borrador; no manipular manualmente acumulados ni stock.

## Diagnóstico de stock/Kardex
Cuando una confirmación aparentemente no coincide con stock:
- verificar estado de RecepcionCompra;
- revisar sus detalles y `CantidadAceptada`;
- revisar almacén/ubicación física;
- localizar los movimientos de Kardex originados por la recepción;
- confirmar que la transacción terminó completamente;
- no ejecutar compensaciones manuales mientras el estado transaccional sea incierto.

Una recepción en Borrador no debe tener materialización de stock. Una recepción Recibida debe conservar trazabilidad de la transición aplicada.

## Anulación y rollback funcional
El rollback funcional normal es `POST /recepciones-compra/{id}/anular` con motivo explícito. Antes de revertir, el servicio bloquea la recepción y verifica si existen movimientos posteriores relacionados.

Si el guard detecta movimientos posteriores, la anulación se detiene. No forzar DML manual ni borrar Kardex. El caso debe resolverse mediante una estrategia de inventario trazable (por ejemplo, ajustes/compensaciones autorizadas) definida por el proceso operativo.

## Rollback de despliegue
Si un despliegue introduce una regresión:
- detener el avance del release;
- conservar evidencia de logs/correlation-id y del commit desplegado;
- revertir el cambio aplicativo mediante el mecanismo estándar del repositorio, sin force-push;
- para cambios de esquema, aplicar únicamente la estrategia de rollback explícitamente probada para la migración correspondiente; no ejecutar DDL/DML improvisado;
- volver a ejecutar gates de backend, migración y RecepcionCompra antes de promover.

## Observabilidad y auditoría
Las acciones Crear, Editar, Confirmar y Anular registran auditoría estricta en módulo Compras. Los snapshots no deben filtrar Observaciones ni `Idempotency-Key`. Usar el correlation-id saneado de la infraestructura para unir API/log/auditoría.

## Recuperación de CI
Distinguir siempre:
- `FAILURE` causal: inspeccionar el job/paso exacto y corregir solo la causa reproducible;
- `CANCELLED` antes del paso objetivo: no equivale a fallo funcional ni PASS; relanzar/revalidar;
- fallo ambiental MySQL: validar disponibilidad/migraciones antes de editar lógica RecepcionCompra;
- fallo E2E: confirmar que Angular/API realmente arrancaron antes de atribuirlo al test.

## Baseline certificado
N2.3 funcional fue certificado sobre `8b8b95ce0573653452cee7ca5024d82bdb184d88` con M13 `#32320525485` SUCCESS completo, N2.3 frontend CI `#32320525445` SUCCESS (7/7 E2E) y unit frontend `#32320525478` SUCCESS.