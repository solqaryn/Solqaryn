# ERP-N3.6 — Devoluciones de cliente — Auditoría y preflight

**Estado:** LISTO — cierre N3.6.A reconciliado contra HEAD posterior a N3.5.H  
**Owner de cierre:** ChatGPT/VAEP v3.26 Closure Governor  
**Rama autorizada:** `Desarrollo`

## Objetivo

Preparar el alcance verificable de N3.6 antes de introducir dominio o persistencia. N3.6.A queda cerrado únicamente como auditoría/preflight: no crea contratos futuros por inferencia ni adelanta N3.6.B.

## Hechos confirmados en el HEAD vigente

1. No existe actualmente una entidad o agregado `DevolucionCliente` en el repositorio.
2. `Venta` es la autoridad comercial de la venta y mantiene `ClienteId`, snapshots del cliente, estado documental/pago, importes y una relación opcional a `Factura`.
3. `Factura` está ligada obligatoriamente a `VentaId`, conserva snapshots comerciales, estado, moneda, total, pagos y saldo pendiente.
4. `DevolucionProveedor` existe como patrón independiente para devoluciones upstream, pero sus reglas no se trasladan automáticamente a devoluciones de cliente.
5. El alcance rector de N3.6 exige soportar devolución total/parcial, cambio, reintegro y crédito a favor, pero la semántica exacta de esos efectos debe definirse en N3.6.B y siguientes.

## Límites fail-closed

Hasta N3.6.B no se asume ni implementa ninguna de estas decisiones:

- que una devolución total cancele automáticamente la factura;
- que una devolución parcial modifique directamente la factura existente;
- que una devolución cree obligatoriamente una nota de crédito;
- que un cambio de igual precio tenga efecto financiero neto cero en todos los casos;
- que un reintegro deba impactar una caja o método de pago concreto;
- que un crédito a favor deba persistirse directamente sobre `Cliente`;
- lifecycle, cardinalidades, idempotencia, política de inventario, efectos de stock/Kardex, contabilidad, impuestos o CxC no demostrados todavía;
- endpoints, permisos RBAC, DTOs o códigos HTTP futuros.

## Decisiones que N3.6.B debe cerrar explícitamente

1. **Documento origen:** relación obligatoria u opcional con `Venta` y/o `Factura`.
2. **Elegibilidad:** estados de venta/factura desde los que una devolución puede originarse.
3. **Lifecycle:** estados, transiciones y reglas de anulación/reversión.
4. **Detalle físico:** identificación de líneas/productos/variantes, cantidades devueltas y límites acumulados frente a la venta original.
5. **Tipo de resolución:** devolución, cambio, reintegro y crédito a favor; si se modelan como tipo, operación separada o combinación controlada.
6. **Efectos posteriores:** qué parent debe materializar stock/Kardex, ajuste de saldo, nota de crédito o movimiento financiero.
7. **Idempotencia y concurrencia:** necesidad de clave/fingerprint durable y locks para evitar doble devolución o sobredevolución.
8. **Auditoría/RBAC:** operaciones críticas y grants requeridos, sin reutilizar módulos/permisos por analogía sin evidencia.
9. **Rollback/data-safety:** reglas de reversión histórica y preservación de documentos ya emitidos.

## Riesgos P0/P1 a evitar

- doble incremento físico de stock por reintento;
- devolución acumulada superior a la cantidad originalmente vendida;
- reintegro o crédito duplicado;
- alteración destructiva de una factura histórica;
- cancelación automática de factura/venta sin contrato explícito;
- mezclar devolución a proveedor con devolución de cliente;
- autoridad financiera duplicada entre devolución, factura y futuros documentos de crédito;
- bypass de permisos o auditoría en operaciones con efecto físico/financiero.

## Estrategia de validación por etapas

- **N3.6.B:** pruebas puras de dominio para invariantes y lifecycle una vez definidos.
- **N3.6.C:** migración MySQL, snapshot/paridad, constraints, idempotencia y rollback/data-safety.
- **N3.6.D:** Application/API, transacción, locking, auditoría, ProblemDetails y tests contractuales.
- **N3.6.E:** frontend/UX, permisos de UI, loading/error/empty, accesibilidad y E2E aplicable.
- **N3.6.F:** RBAC/auditoría/seguridad/observabilidad.
- **N3.6.G:** regresión causal y CI aplicable.
- **N3.6.H:** documentación/certificación final, TASKS/CHANGELOG solo con cierre real.

## Criterio de salida de N3.6.A — CUMPLIDO

N3.6.A puede cerrarse únicamente después de que `N3.5.H=LISTO`, este preflight sea reconciliado contra el HEAD vigente y las decisiones todavía no demostradas permanezcan explícitamente `DECISION_PENDING` para N3.6.B.

Cierre ejecutado después de la publicación certificada de N3.5.H en `4296e72b8b5a87ef4e779e3ec6f8af083e396374`: el preflight fue releído contra `Desarrollo`, no apareció una entidad/agregado `DevolucionCliente`, y todas las decisiones no demostradas continúan expresamente reservadas para N3.6.B. No hay delta de producto, persistencia, API ni frontend en N3.6.A.

## Evidencia inspeccionada

- `backend/src/Domain/Entities/Venta.cs`
- `backend/src/Domain/Entities/Factura.cs`
- `backend/src/Domain/Entities/DevolucionProveedor.cs`
- búsqueda dirigida fresca sin resultados para `class DevolucionCliente`
- Issue Jules A #596 / artifact `9577507511`, usado únicamente como evidencia de closure-readiness; no se integra su documento stale porque declaraba N3.5.H todavía abierto.
- cierre N3.5.H `4296e72b8b5a87ef4e779e3ec6f8af083e396374`, diff exacto `CHANGELOG_AI.md +17/-0`.

**Resultado:** `N3.6.A=LISTO`. Siguiente tarea dependency-valid: `N3.6.B — Dominio/contratos`, que debe resolver explícitamente los `DECISION_PENDING` anteriores con pruebas puras antes de persistencia.
