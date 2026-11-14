# Certificación N2.3 — Recepción de mercancía

## Dictamen
**N2.3 funcionalmente certificado para transición documental H**, sujeto únicamente al cierre del rollup N2.3.H con sus revisiones documentales independientes H.2/H.3 y la reconciliación final de `TASKS.md` / `CHANGELOG_AI.md`.

No existen P0/P1 conocidos abiertos en el baseline funcional certificado.

## Baseline funcional
- Commit funcional: `8b8b95ce0573653452cee7ca5024d82bdb184d88`.
- M13 integral: `#32320525485` — SUCCESS.
- N2.3 frontend/E2E: `#32320525445` — SUCCESS, 7/7 Playwright.
- N2.3 unit frontend: `#32320525478` — SUCCESS.
- Revisión independiente G.3 Jules C: 36/36 pruebas focalizadas RecepcionCompra PASS, cero P0/P1; evidencia integrada en `docs/qa/N2_3_G_INDEPENDENT_REVIEW_JULES_C.md`.

## Cobertura funcional certificada
- RecepcionCompra separada de OrdenCompra y de la futura FacturaProveedor.
- Creación idempotente mediante `Idempotency-Key` y fingerprint SHA-256.
- Edición exclusivamente en Borrador.
- OrdenCompra obligatoriamente Aprobada.
- Recepciones parciales y múltiples con control acumulado por línea.
- Diferencias de recibo: recibida, aceptada, dañada, faltante y sobrante.
- Validación de almacén y ubicación.
- Materialización de `StockFisico` solo al confirmar y solo por cantidad aceptada.
- Kardex tipado de confirmación y anulación.
- Reversión controlada al anular.
- Bloqueo de anulación cuando existen movimientos posteriores relacionados.
- RBAC relacional Compras/Ver, Crear, Editar, Confirmar y Anular, sin bypass administrativo.
- Auditoría estricta y correlation-id saneado.
- Frontend con shell/listado/filtros/formulario/detalle/acciones y regresión E2E.

## Evidencia de implementación
El controlador `RecepcionesCompraController` expone listado, detalle, saldo de orden, crear, editar, confirmar y anular bajo autorización y permisos específicos. El servicio `RecepcionCompraService` aplica idempotencia, transacciones, bloqueo del agregado, validación de saldos, materialización de existencias, Kardex y auditoría estricta.

La entidad `RecepcionCompra` implementa el ciclo `Borrador → Recibida → Anulada`, exige motivo de anulación y preserva clave/fingerprint de idempotencia de forma atómica.

## Seguridad
Las mutaciones críticas requieren usuario autenticado válido y permisos persistidos. La auditoría usa un snapshot reducido que no incluye observaciones libres ni claves de idempotencia. Los errores de dominio/conflicto no justifican bypass de autorización ni modificación manual de inventario.

## Integridad transaccional
Confirmación y anulación se ejecutan en transacción. La confirmación valida nuevamente la OrdenCompra y la recepción acumulada antes de aumentar stock. La anulación comprueba movimientos posteriores antes de revertir existencia/Kardex.

## QA y CI
El gate M13 del baseline funcional terminó SUCCESS incluyendo:
- backend y pruebas;
- MySQL y migraciones;
- frontend lint/build;
- higiene/dependencias;
- seguridad HTTP;
- runtime Angular/API;
- Playwright integral;
- dictamen automatizado.

Los intentos históricos CANCELLED antes de Playwright no se utilizaron como PASS; fueron supersedidos por la ejecución terminal verde.

## Hallazgo ambiental histórico Jules C
La revisión Jules C informó fallos del suite global por indisponibilidad de MySQL en su sandbox. No se clasificaron como defecto de RecepcionCompra. La evidencia GitHub M13 causal posterior ejecutó correctamente backend/MySQL/migraciones, neutralizando ese P2 ambiental para el cierre del módulo.

## Rollback
El rollback funcional de una recepción materializada es la operación de anulación, siempre que el guard de movimientos posteriores permita una reversión segura. No se autoriza borrar Kardex, alterar stock por DML improvisado ni forzar anulación cuando existe historia posterior.

## Pendientes exclusivos del rollup H
Para marcar N2.3.H y N2.3 como cerrados deben completarse:
1. H.1: documentación canónica y reconciliación final.
2. H.2: review independiente Jules A sobre API/contratos/seguridad.
3. H.3: review independiente Jules C sobre operación/migración/rollback.
4. Clasificar y resolver cualquier P0/P1 real que aparezca en H.2/H.3.
5. Sincronizar `TASKS.md`, `CHANGELOG_AI.md`, tablero y checkpoint VAEP.

Hasta completar esos cinco puntos, este documento certifica el **baseline funcional**, no sustituye el estado de cierre del padre H.